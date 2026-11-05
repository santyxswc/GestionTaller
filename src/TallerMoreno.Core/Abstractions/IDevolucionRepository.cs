/**
 * @file IDevolucionRepository.cs
 * @brief Contrato del repositorio de devoluciones y garantías.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Models;

namespace TallerMoreno.Core.Abstractions
{
    /**
     * @brief Devoluciones de clientes y a proveedores, con su efecto en el inventario.
     */
    public interface IDevolucionRepository
    {
        /**
         * @brief Devoluciones de un cliente.
         * @param clienteId Cliente
         * @return Devoluciones, de la más reciente a la más antigua
         */
        IEnumerable<Devolucion> DeCliente(int clienteId);

        /**
         * @brief Devoluciones de una venta.
         * @param ventaId Venta
         * @return Devoluciones, de la más reciente a la más antigua
         */
        IEnumerable<Devolucion> DeVenta(int ventaId);

        /**
         * @brief Devoluciones a un proveedor.
         * @param proveedorId Proveedor
         * @return Devoluciones, de la más reciente a la más antigua
         */
        IEnumerable<Devolucion> DeProveedor(int proveedorId);

        /**
         * @brief Busca una devolución a un proveedor.
         * @param id Devolución
         * @return Devolución, o null
         */
        Devolucion GetDeProveedor(int id);

        /**
         * @brief Registra lo que un cliente devolvió de una venta y mueve el inventario en una transacción.
         * @param ventaId Venta
         * @param usuarioId Usuario que la registra
         * @param repuestoId Repuesto devuelto
         * @param cantidad Unidades
         * @param motivo Motivo
         * @param resultado Cambio, Dinero o Descuento
         * @param valor Plata devuelta o rebajada
         * @param vuelveInventario Si el repuesto devuelto está bueno y vuelve al inventario
         * @param notas Notas
         * @return Identificador
         * @exception InvalidOperationException Si se pasa de lo vendido o no hay stock para el cambio
         */
        int InsertDeCliente(int ventaId, int usuarioId, int repuestoId, int cantidad, string motivo, string resultado,
                            decimal valor, bool vuelveInventario, string notas);

        /**
         * @brief Registra un repuesto devuelto a un proveedor y mueve el inventario en una transacción.
         * @param proveedorId Proveedor
         * @param usuarioId Usuario que la registra
         * @param repuestoId Repuesto
         * @param cantidad Unidades
         * @param motivo Motivo
         * @param resultado Cambio, Dinero, Descuento o Pendiente
         * @param valor Plata devuelta o rebajada
         * @param saleInventario Si las unidades salen del inventario
         * @param notas Notas
         * @return Identificador
         * @exception InvalidOperationException Si el repuesto no existe o no hay stock
         */
        int InsertAProveedor(int proveedorId, int usuarioId, int repuestoId, int cantidad, string motivo, string resultado,
                             decimal valor, bool saleInventario, string notas);

        /**
         * @brief Anota la respuesta del proveedor a una garantía pendiente.
         * @param id Devolución
         * @param usuarioId Usuario que la registra
         * @param resultado Cambio, Dinero o Descuento
         * @param valor Plata devuelta o rebajada
         * @param notas Notas nuevas, o null para dejar las que tenía
         * @exception InvalidOperationException Si ya no está pendiente
         */
        void Resolver(int id, int usuarioId, string resultado, decimal valor, string notas);

        /**
         * @brief Borra una devolución de cliente y deshace su efecto en el inventario.
         * @param id Devolución
         * @param usuarioId Usuario que la borra
         * @exception InvalidOperationException Si no existe o ya no están las unidades que entraron
         */
        void DeleteDeCliente(int id, int usuarioId);

        /**
         * @brief Borra una devolución a proveedor y deshace su efecto en el inventario.
         * @param id Devolución
         * @param usuarioId Usuario que la borra
         * @exception InvalidOperationException Si no existe o ya no están las unidades que llegaron
         */
        void DeleteAProveedor(int id, int usuarioId);
    }
}
