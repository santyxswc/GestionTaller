/**
 * @file ClienteService.cs
 * @brief Reglas de negocio de los clientes.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Abstractions;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Sesion;

namespace TallerMoreno.Core.Services
{
    /**
     * @brief Registro de clientes y consulta de lo que ha comprado cada uno.
     */
    public class ClienteService
    {
        /** Acceso a datos de clientes. */
        private readonly IClienteRepository _repo;
        /** Acceso a datos de ventas (historial del cliente). */
        private readonly IVentaRepository _ventas;
        /** Usuario de la sesión. */
        private readonly ISesionUsuario _sesion;

        /**
         * @brief Crea el servicio con sus dependencias.
         * @param repo Acceso a datos de clientes
         * @param ventas Acceso a datos de ventas
         * @param sesion Usuario de la sesión (permisos)
         */
        public ClienteService(IClienteRepository repo, IVentaRepository ventas, ISesionUsuario sesion)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _ventas = ventas ?? throw new ArgumentNullException(nameof(ventas));
            _sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
        }

        /**
         * @brief Busca clientes con lo que ha comprado cada uno.
         * @param texto Nombre, documento, teléfono o placa; vacío devuelve todos
         * @return Clientes
         */
        public IEnumerable<Cliente> Buscar(string texto = null) => _repo.Buscar(texto?.Trim());

        /**
         * @brief Busca un cliente.
         * @param id Identificador
         * @return Cliente, o null
         */
        public Cliente ObtenerPorId(int id) => _repo.GetById(id);

        /**
         * @brief Clientes para elegir en una venta.
         * @return Identificador y nombre con la placa
         */
        public IEnumerable<Seleccion> ParaSeleccion() => _repo.GetParaSeleccion();

        /**
         * @brief Compras de un cliente, de la más reciente a la más antigua.
         * @param id Cliente
         * @return Ventas del cliente, incluidas las anuladas
         */
        public IEnumerable<Venta> Compras(int id) => _ventas.Buscar(null, null, null, id);

        /**
         * @brief Valida y registra un cliente. Lo puede hacer cualquier usuario, por ejemplo durante una venta.
         * @param c Datos del cliente
         * @return Identificador asignado
         */
        public int Crear(Cliente c)
        {
            _sesion.ExigirSesion();
            Validar(c);
            c.Id = _repo.Insert(c);
            return c.Id;
        }

        /**
         * @brief Valida y actualiza un cliente.
         * @param c Cliente con los datos nuevos
         * @return true si se actualizó
         */
        public bool Actualizar(Cliente c)
        {
            _sesion.ExigirSesion();
            if (c == null || c.Id <= 0)
                throw new ArgumentException("Cliente inválido.");
            Validar(c);
            return _repo.Update(c);
        }

        /**
         * @brief Elimina un cliente que no tiene compras. Solo administrador.
         * @param id Cliente
         * @return true si se eliminó
         * @exception InvalidOperationException Si tiene ventas
         */
        public bool Eliminar(int id)
        {
            _sesion.ExigirAdmin("eliminar clientes");
            int ventas = _repo.ContarVentas(id);
            if (ventas > 0)
                throw new InvalidOperationException($"No se puede eliminar porque tiene {ventas} compra(s) registradas.");
            return _repo.Delete(id);
        }

        /**
         * @brief Abonos de un cliente, incluidos los hechos al comprar.
         * @param id Cliente
         * @return Abonos, del más reciente al más antiguo
         */
        public IEnumerable<Pago> Abonos(int id) => _repo.Abonos(id);

        /**
         * @brief Registra un abono de un cliente que quedó debiendo.
         * @param clienteId Cliente
         * @param valor Valor abonado
         * @param metodoPago Efectivo, transferencia...
         * @param notas Notas (opcional)
         * @return Identificador del abono
         * @exception ArgumentException Si el valor no es válido o pasa de lo que debe
         */
        public int RegistrarAbono(int clienteId, decimal valor, string metodoPago, string notas = null)
        {
            _sesion.ExigirSesion();
            var c = _repo.GetById(clienteId) ?? throw new ArgumentException("El cliente ya no existe.");
            valor = Math.Round(valor);
            if (valor <= 0)
                throw new ArgumentException("Escribe cuánto abonó.");
            if (c.Deuda <= 0)
                throw new ArgumentException($"{c.Nombre} no debe nada.");
            if (valor > c.Deuda)
                throw new ArgumentException($"{c.Nombre} debe {Formato.Moneda(c.Deuda)}; el abono no puede ser mayor.");
            return _repo.InsertAbono(clienteId, _sesion.UsuarioId, valor,
                                     string.IsNullOrWhiteSpace(metodoPago) ? null : metodoPago.Trim(),
                                     string.IsNullOrWhiteSpace(notas) ? null : notas.Trim());
        }

        /**
         * @brief Borra un abono registrado por error; lo que debía vuelve a aparecer. Solo administrador.
         * @param abonoId Abono
         * @exception InvalidOperationException Si el abono ya no existe
         */
        public void EliminarAbono(int abonoId)
        {
            _sesion.ExigirAdmin("borrar abonos");
            if (!_repo.DeleteAbono(abonoId))
                throw new InvalidOperationException("Ese abono ya no existe.");
        }

        /**
         * @brief Valida y limpia los datos de un cliente.
         * @param c Cliente
         */
        private static void Validar(Cliente c)
        {
            ArgumentNullException.ThrowIfNull(c);
            if (string.IsNullOrWhiteSpace(c.Nombre))
                throw new ArgumentException("Escribe el nombre del cliente.");
            c.Nombre = c.Nombre.Trim();
            c.Placa = string.IsNullOrWhiteSpace(c.Placa) ? null : c.Placa.Trim().ToUpperInvariant().Replace(" ", "");
            VentaService.ValidarKilometraje(c.Kilometraje);
        }
    }
}
