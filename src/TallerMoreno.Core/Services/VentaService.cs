/**
 * @file VentaService.cs
 * @brief Reglas de negocio de las ventas.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Abstractions;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Sesion;

namespace TallerMoreno.Core.Services
{
    /**
     * @brief Registro, consulta y anulación de ventas.
     *
     * Al vender se descuentan las unidades del inventario; al anular se devuelven.
     */
    public class VentaService
    {
        /** Métodos de pago aceptados. */
        public static readonly string[] MetodosPago = { "Efectivo", "Transferencia", "Nequi / Daviplata", "Tarjeta" };

        /** Acceso a datos de ventas. */
        private readonly IVentaRepository _repo;
        /** Usuario de la sesión. */
        private readonly ISesionUsuario _sesion;

        /**
         * @brief Crea el servicio con sus dependencias.
         * @param repo Acceso a datos
         * @param sesion Usuario de la sesión (permisos)
         */
        public VentaService(IVentaRepository repo, ISesionUsuario sesion)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
        }

        /**
         * @brief Busca ventas.
         * @param periodo Uno de Periodo.Todos
         * @param texto Cliente, placa, vendedor o número; vacío no filtra
         * @return Ventas, de la más reciente a la más antigua
         */
        public IEnumerable<Venta> Buscar(string periodo, string texto = null)
        {
            var (desde, hasta) = Periodo.Rango(periodo, DateTime.Now);
            return _repo.Buscar(desde, hasta, texto?.Trim(), null);
        }

        /**
         * @brief Busca una venta con sus líneas.
         * @param id Número de la venta
         * @return Venta con Items cargado, o null
         */
        public Venta ObtenerDetalle(int id)
        {
            var venta = _repo.GetById(id);
            if (venta != null)
                venta.Items = _repo.GetItems(id);
            return venta;
        }

        /**
         * @brief Valida y registra una venta a nombre del usuario de la sesión.
         * @param clienteId Cliente registrado, o null para cliente de mostrador
         * @param metodoPago Método de pago
         * @param lineas Repuestos y cantidades; los repetidos se unen en una línea
         * @param notas Notas de la venta (opcional)
         * @return Número de la venta
         * @exception ArgumentException Si faltan datos o hay cantidades no válidas
         * @exception InvalidOperationException Si no hay stock suficiente
         */
        public int Vender(int? clienteId, string metodoPago, IEnumerable<LineaDocumento> lineas, string notas = null)
        {
            _sesion.ExigirSesion();
            if (string.IsNullOrWhiteSpace(metodoPago))
                throw new ArgumentException("Selecciona cómo paga el cliente.");

            var agrupadas = (lineas ?? Enumerable.Empty<LineaDocumento>())
                .GroupBy(l => l.RepuestoId)
                .Select(g => new LineaDocumento { RepuestoId = g.Key, Cantidad = g.Sum(l => l.Cantidad) })
                .ToList();

            if (agrupadas.Count == 0)
                throw new ArgumentException("Agrega al menos un repuesto a la venta.");
            if (agrupadas.Any(l => l.Cantidad <= 0))
                throw new ArgumentException("Las cantidades deben ser mayores que cero.");

            return _repo.Insert(clienteId > 0 ? clienteId : null, _sesion.UsuarioId, metodoPago,
                                string.IsNullOrWhiteSpace(notas) ? null : notas.Trim(), agrupadas);
        }

        /**
         * @brief Anula una venta y devuelve las unidades al inventario. Solo administrador.
         * @param id Número de la venta
         */
        public void Anular(int id)
        {
            _sesion.ExigirAdmin("anular ventas");
            _repo.Anular(id, _sesion.UsuarioId);
        }
    }
}
