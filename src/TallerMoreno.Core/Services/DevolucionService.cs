/**
 * @file DevolucionService.cs
 * @brief Reglas de negocio de las devoluciones y garantías.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Abstractions;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Sesion;

namespace TallerMoreno.Core.Services
{
    /**
     * @brief Devoluciones de clientes y a proveedores.
     *
     * Un cliente devuelve algo de una venta: se le cambia por otro igual, se le devuelve la plata o se le
     * rebaja de lo que debe; si el repuesto está bueno vuelve al inventario. Al proveedor se le devuelve un
     * repuesto y él lo cambia, devuelve la plata o la rebaja de la deuda; mientras responde queda pendiente.
     */
    public class DevolucionService
    {
        /** Devoluciones. */
        private readonly IDevolucionRepository _repo;
        /** Ventas, para saber qué se vendió y a qué precio. */
        private readonly IVentaRepository _ventas;
        /** Clientes, para lo que ha pagado y lo que debe. */
        private readonly IClienteRepository _clientes;
        /** Proveedores, para lo que se les ha pagado y lo que se les debe. */
        private readonly IProveedorRepository _proveedores;
        /** Usuario de la sesión. */
        private readonly ISesionUsuario _sesion;

        /**
         * @brief Crea el servicio con sus dependencias.
         * @param repo Devoluciones
         * @param ventas Ventas
         * @param clientes Clientes
         * @param proveedores Proveedores
         * @param sesion Usuario de la sesión (permisos)
         */
        public DevolucionService(IDevolucionRepository repo, IVentaRepository ventas, IClienteRepository clientes,
                                 IProveedorRepository proveedores, ISesionUsuario sesion)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _ventas = ventas ?? throw new ArgumentNullException(nameof(ventas));
            _clientes = clientes ?? throw new ArgumentNullException(nameof(clientes));
            _proveedores = proveedores ?? throw new ArgumentNullException(nameof(proveedores));
            _sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
        }

        /**
         * @brief Devoluciones de un cliente.
         * @param clienteId Cliente
         * @return Devoluciones, de la más reciente a la más antigua
         */
        public IEnumerable<Devolucion> DeCliente(int clienteId) => _repo.DeCliente(clienteId);

        /**
         * @brief Devoluciones de una venta.
         * @param ventaId Venta
         * @return Devoluciones, de la más reciente a la más antigua
         */
        public IEnumerable<Devolucion> DeVenta(int ventaId) => _repo.DeVenta(ventaId);

        /**
         * @brief Devoluciones a un proveedor.
         * @param proveedorId Proveedor
         * @return Devoluciones, de la más reciente a la más antigua
         */
        public IEnumerable<Devolucion> DeProveedor(int proveedorId) => _repo.DeProveedor(proveedorId);

        /**
         * @brief Busca una devolución a un proveedor.
         * @param id Devolución
         * @return Devolución, o null
         */
        public Devolucion ObtenerDeProveedor(int id) => _repo.GetDeProveedor(id);

        /**
         * @brief Registra lo que un cliente devolvió de una venta.
         * @param ventaId Venta
         * @param repuestoId Repuesto devuelto
         * @param cantidad Unidades
         * @param motivo Garantía, No le sirvió...
         * @param resultado Cambio, Dinero o Descuento
         * @param valor Plata devuelta o rebajada; no se usa en un cambio
         * @param vuelveInventario Si el repuesto devuelto está bueno y vuelve al inventario
         * @param notas Notas (opcional)
         * @return Identificador
         * @exception ArgumentException Si faltan datos o el valor no es válido
         * @exception InvalidOperationException Si se pasa de lo vendido o no hay stock para el cambio
         */
        public int DevolverDeCliente(int ventaId, int repuestoId, int cantidad, string motivo, string resultado, decimal valor,
                                     bool vuelveInventario, string notas = null)
        {
            _sesion.ExigirSesion();
            var venta = _ventas.GetById(ventaId) ?? throw new ArgumentException("La venta ya no existe.");
            if (venta.Anulada)
                throw new ArgumentException($"La venta N° {ventaId} está anulada.");
            var linea = _ventas.GetItems(ventaId).FirstOrDefault(l => l.RepuestoId == repuestoId)
                ?? throw new ArgumentException("Elige el repuesto que devolvió.");
            if (cantidad <= 0)
                throw new ArgumentException("Escribe cuántas unidades devolvió.");
            if (cantidad > linea.PorDevolver)
                throw new ArgumentException(linea.PorDevolver == 0
                    ? $"{linea.Nombre} ya se devolvió completo."
                    : $"De {linea.Nombre} se pueden devolver {linea.PorDevolver} unidades.");
            motivo = Obligatorio(motivo, "Elige el motivo de la devolución.");

            decimal maximo = linea.PrecioUnitario * cantidad;
            valor = Math.Round(valor);
            switch (resultado)
            {
                case Devolucion.Cambio:
                    valor = 0;
                    break;
                case Devolucion.Dinero:
                    ValorPositivo(valor, "Escribe cuánta plata se le devolvió.");
                    if (valor > maximo)
                        throw new ArgumentException($"Por esas unidades pagó {Formato.Moneda(maximo)}; no se le puede devolver más.");
                    if (venta.ClienteId is int idCliente)
                    {
                        var c = _clientes.GetById(idCliente);
                        if (c != null && valor > c.TotalPagado)
                            throw new ArgumentException($"{c.Nombre} solo ha pagado {Formato.Moneda(c.TotalPagado)}. Rebájaselo de lo que debe.");
                    }
                    break;
                case Devolucion.Descuento:
                    var cliente = venta.ClienteId is int id ? _clientes.GetById(id) : null;
                    if (cliente == null)
                        throw new ArgumentException("Esta venta no tiene cliente registrado; no hay deuda que rebajar.");
                    if (cliente.Deuda <= 0)
                        throw new ArgumentException($"{cliente.Nombre} no debe nada; elige devolverle la plata.");
                    ValorPositivo(valor, "Escribe cuánto se le rebaja de la deuda.");
                    if (valor > maximo)
                        throw new ArgumentException($"Por esas unidades se cobró {Formato.Moneda(maximo)}; no se le puede rebajar más.");
                    if (valor > cliente.Deuda)
                        throw new ArgumentException($"{cliente.Nombre} debe {Formato.Moneda(cliente.Deuda)}; no se le puede rebajar más.");
                    break;
                default:
                    throw new ArgumentException("Elige qué se le dio al cliente.");
            }

            return _repo.InsertDeCliente(ventaId, _sesion.UsuarioId, repuestoId, cantidad, motivo, resultado, valor, vuelveInventario, Limpiar(notas));
        }

        /**
         * @brief Registra un repuesto devuelto a un proveedor.
         * @param proveedorId Proveedor
         * @param repuestoId Repuesto
         * @param cantidad Unidades
         * @param motivo Garantía, Llegó dañado...
         * @param resultado Cambio, Dinero, Descuento o Pendiente
         * @param valor Plata devuelta o rebajada; no se usa en un cambio ni si está pendiente
         * @param saleInventario Si las unidades salen del inventario (no, si es algo que un cliente devolvió malo)
         * @param notas Notas (opcional)
         * @return Identificador
         * @exception ArgumentException Si faltan datos o el valor no es válido
         * @exception InvalidOperationException Si no hay stock para sacar
         */
        public int DevolverAProveedor(int proveedorId, int repuestoId, int cantidad, string motivo, string resultado, decimal valor,
                                      bool saleInventario, string notas = null)
        {
            _sesion.ExigirSesion();
            var p = _proveedores.GetById(proveedorId) ?? throw new ArgumentException("El proveedor ya no existe.");
            if (repuestoId <= 0)
                throw new ArgumentException("Elige el repuesto que le devolviste.");
            if (cantidad <= 0)
                throw new ArgumentException("Escribe cuántas unidades le devolviste.");
            motivo = Obligatorio(motivo, "Elige el motivo de la devolución.");
            valor = resultado == Devolucion.Pendiente ? 0 : ValidarRespuesta(p, resultado, valor);
            return _repo.InsertAProveedor(proveedorId, _sesion.UsuarioId, repuestoId, cantidad, motivo, resultado, valor, saleInventario, Limpiar(notas));
        }

        /**
         * @brief Anota lo que respondió el proveedor a una garantía pendiente.
         * @param id Devolución
         * @param resultado Cambio, Dinero o Descuento
         * @param valor Plata devuelta o rebajada; no se usa en un cambio
         * @param notas Notas nuevas (opcional)
         * @exception ArgumentException Si ya no está pendiente o el valor no es válido
         */
        public void Resolver(int id, string resultado, decimal valor, string notas = null)
        {
            _sesion.ExigirSesion();
            var d = _repo.GetDeProveedor(id) ?? throw new ArgumentException("Esa devolución ya no existe.");
            if (!d.EsPendiente)
                throw new ArgumentException("Esa garantía ya tiene respuesta del proveedor.");
            var p = _proveedores.GetById(d.ProveedorId ?? 0) ?? throw new ArgumentException("El proveedor ya no existe.");
            valor = ValidarRespuesta(p, resultado, valor);
            _repo.Resolver(id, _sesion.UsuarioId, resultado, valor, Limpiar(notas));
        }

        /**
         * @brief Borra una devolución de cliente mal registrada y deshace su efecto. Solo administrador.
         * @param id Devolución
         */
        public void EliminarDeCliente(int id)
        {
            _sesion.ExigirAdmin("borrar devoluciones");
            _repo.DeleteDeCliente(id, _sesion.UsuarioId);
        }

        /**
         * @brief Borra una devolución a proveedor mal registrada y deshace su efecto. Solo administrador.
         * @param id Devolución
         */
        public void EliminarAProveedor(int id)
        {
            _sesion.ExigirAdmin("borrar devoluciones");
            _repo.DeleteAProveedor(id, _sesion.UsuarioId);
        }

        /**
         * @brief Valida lo que respondió el proveedor y devuelve el valor que se guarda.
         * @param p Proveedor
         * @param resultado Cambio, Dinero o Descuento
         * @param valor Valor escrito
         * @return Valor redondeado; 0 en un cambio
         */
        private static decimal ValidarRespuesta(Proveedor p, string resultado, decimal valor)
        {
            valor = Math.Round(valor);
            switch (resultado)
            {
                case Devolucion.Cambio:
                    return 0;
                case Devolucion.Dinero:
                    ValorPositivo(valor, "Escribe cuánta plata te devolvió.");
                    if (valor > p.TotalPagado)
                        throw new ArgumentException($"A {p.Nombre} le has pagado {Formato.Moneda(p.TotalPagado)}; no te puede devolver más. Si te lo rebajó de la deuda, elige esa opción.");
                    return valor;
                case Devolucion.Descuento:
                    if (p.Deuda <= 0)
                        throw new ArgumentException($"No le debes nada a {p.Nombre}; elige que te devolvió la plata.");
                    ValorPositivo(valor, "Escribe cuánto te rebajó de la deuda.");
                    if (valor > p.Deuda)
                        throw new ArgumentException($"A {p.Nombre} le debes {Formato.Moneda(p.Deuda)}; no te puede rebajar más.");
                    return valor;
                default:
                    throw new ArgumentException("Elige qué hizo el proveedor.");
            }
        }

        /**
         * @brief Exige un valor mayor que cero.
         * @param valor Valor
         * @param mensaje Mensaje si no lo es
         */
        private static void ValorPositivo(decimal valor, string mensaje)
        {
            if (valor <= 0)
                throw new ArgumentException(mensaje);
        }

        /**
         * @brief Exige un texto.
         * @param texto Texto
         * @param mensaje Mensaje si falta
         * @return Texto sin espacios de más
         */
        private static string Obligatorio(string texto, string mensaje) =>
            string.IsNullOrWhiteSpace(texto) ? throw new ArgumentException(mensaje) : texto.Trim();

        /**
         * @brief Quita los espacios de más de un texto opcional.
         * @param texto Texto
         * @return Texto limpio, o null si está vacío
         */
        private static string Limpiar(string texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    }
}
