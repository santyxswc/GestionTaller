/**
 * @file IExportadorExcel.cs
 * @brief Contrato de la exportación a Excel.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Models;

namespace TallerMoreno.Core.Abstractions
{
    /**
     * @brief Escribe listados en archivos de Excel (.xlsx).
     */
    public interface IExportadorExcel
    {
        /**
         * @brief Exporta el inventario con stock, precios y valor en bodega.
         * @param repuestos Repuestos a exportar
         * @param ruta Archivo .xlsx de destino
         */
        void ExportarInventario(IEnumerable<Repuesto> repuestos, string ruta);

        /**
         * @brief Exporta un listado de ventas.
         * @param ventas Ventas a exportar
         * @param titulo Título de la hoja, como "Ventas · Este mes"
         * @param ruta Archivo .xlsx de destino
         */
        void ExportarVentas(IEnumerable<Venta> ventas, string titulo, string ruta);

        /**
         * @brief Exporta los clientes con lo que ha comprado cada uno.
         * @param clientes Clientes a exportar
         * @param ruta Archivo .xlsx de destino
         */
        void ExportarClientes(IEnumerable<Cliente> clientes, string ruta);
    }
}
