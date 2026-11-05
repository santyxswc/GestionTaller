/**
 * @file EntradaService.cs
 * @brief Reglas de negocio de las entradas de mercancía.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Abstractions;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Sesion;

namespace TallerMoreno.Core.Services
{
    /**
     * @brief Registro de la mercancía que llega de los proveedores.
     *
     * Cada entrada suma las unidades al inventario y deja el costo de compra
     * de cada repuesto en el último precio pagado.
     */
    public class EntradaService
    {
        /** Acceso a datos de entradas. */
        private readonly IEntradaRepository _repo;
        /** Usuario de la sesión. */
        private readonly ISesionUsuario _sesion;

        /**
         * @brief Crea el servicio con sus dependencias.
         * @param repo Acceso a datos
         * @param sesion Usuario de la sesión (permisos)
         */
        public EntradaService(IEntradaRepository repo, ISesionUsuario sesion)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
        }

        /**
         * @brief Busca entradas de un periodo.
         * @param periodo Uno de Periodo.Todos
         * @return Entradas, de la más reciente a la más antigua
         */
        public IEnumerable<Entrada> Buscar(string periodo)
        {
            var (desde, hasta) = Periodo.Rango(periodo, DateTime.Now);
            return _repo.Buscar(desde, hasta, null);
        }

        /**
         * @brief Busca una entrada con sus líneas.
         * @param id Número de la entrada
         * @return Entrada con Items cargado, o null
         */
        public Entrada ObtenerDetalle(int id)
        {
            var entrada = _repo.GetById(id);
            if (entrada != null)
                entrada.Items = _repo.GetItems(id);
            return entrada;
        }

        /**
         * @brief Valida y registra la mercancía recibida a nombre del usuario de la sesión.
         * @param proveedorId Proveedor
         * @param facturaProveedor Número de la factura del proveedor (opcional)
         * @param lineas Repuestos, cantidades y costo unitario; los repetidos se unen
         * @param pagado Lo que se le pagó al proveedor al recibir, o null si se pagó toda la factura;
         *        lo que falte queda como deuda con el proveedor
         * @return Número de la entrada
         */
        public int Registrar(int proveedorId, string facturaProveedor, IEnumerable<LineaDocumento> lineas, decimal? pagado = null)
        {
            _sesion.ExigirSesion();
            if (proveedorId <= 0)
                throw new ArgumentException("Selecciona el proveedor.");

            var agrupadas = (lineas ?? Enumerable.Empty<LineaDocumento>())
                .GroupBy(l => l.RepuestoId)
                .Select(g => new LineaDocumento
                {
                    RepuestoId = g.Key,
                    Cantidad = g.Sum(l => l.Cantidad),
                    PrecioUnitario = g.Last().PrecioUnitario
                })
                .ToList();

            if (agrupadas.Count == 0)
                throw new ArgumentException("Agrega al menos un repuesto.");
            if (agrupadas.Any(l => l.Cantidad <= 0))
                throw new ArgumentException("Las cantidades deben ser mayores que cero.");
            if (agrupadas.Any(l => l.PrecioUnitario < 0))
                throw new ArgumentException("El costo no puede ser negativo.");

            decimal total = agrupadas.Sum(l => l.PrecioUnitario * l.Cantidad);
            if (pagado < 0)
                throw new ArgumentException("Lo pagado no puede ser negativo.");
            if (pagado > total)
                throw new ArgumentException($"Lo pagado no puede ser mayor que el total de la factura ({Formato.Moneda(total)}).");

            return _repo.Insert(proveedorId, _sesion.UsuarioId,
                                string.IsNullOrWhiteSpace(facturaProveedor) ? null : facturaProveedor.Trim(), agrupadas,
                                pagado ?? total);
        }
    }
}
