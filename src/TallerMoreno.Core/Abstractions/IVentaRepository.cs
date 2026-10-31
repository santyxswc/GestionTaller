/**
 * @file IVentaRepository.cs
 * @brief Contrato del repositorio de ventas.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Models;

namespace TallerMoreno.Core.Abstractions
{
    /**
     * @brief Registro y consulta de ventas.
     */
    public interface IVentaRepository
    {
        /**
         * @brief Registra una venta completa y descuenta el stock en una transacción.
         * @param clienteId Cliente, o null para cliente de mostrador
         * @param usuarioId Usuario que vende
         * @param metodoPago Método de pago
         * @param notas Notas de la venta
         * @param lineas Repuestos y cantidades (sin repetidos)
         * @return Número de la venta
         * @exception InvalidOperationException Si un repuesto no existe o no alcanza el stock; no se guarda nada
         */
        int Insert(int? clienteId, int usuarioId, string metodoPago, string notas, IEnumerable<LineaDocumento> lineas);

        /**
         * @brief Busca una venta sin sus líneas.
         * @param id Número de la venta
         * @return Venta, o null si no existe
         */
        Venta GetById(int id);

        /**
         * @brief Obtiene las líneas de una venta.
         * @param id Número de la venta
         * @return Lineas con el nombre de cada repuesto
         */
        List<LineaDocumento> GetItems(int id);

        /**
         * @brief Busca ventas, de la más reciente a la más antigua.
         * @param desde Fecha inicial (incluida), o null
         * @param hasta Fecha final (excluida), o null
         * @param texto Cliente, placa, vendedor o número; vacío no filtra
         * @param clienteId Solo las de este cliente, o null
         * @return Ventas que coinciden
         */
        IEnumerable<Venta> Buscar(DateTime? desde, DateTime? hasta, string texto, int? clienteId);

        /**
         * @brief Anula una venta y devuelve sus unidades al inventario en una transacción.
         * @param id Número de la venta
         * @param usuarioId Usuario que anula
         * @exception InvalidOperationException Si la venta no existe o ya estaba anulada
         */
        void Anular(int id, int usuarioId);
    }
}
