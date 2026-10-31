/**
 * @file IReporteRepository.cs
 * @brief Contrato de las consultas de reportes.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Models;

namespace TallerMoreno.Core.Abstractions
{
    /**
     * @brief Totales y rankings de las ventas no anuladas.
     */
    public interface IReporteRepository
    {
        /**
         * @brief Totales de un periodo.
         * @param desde Fecha inicial (incluida), o null
         * @param hasta Fecha final (excluida), o null
         * @return Total, ventas, unidades y ganancia
         */
        ResumenVentas Resumen(DateTime? desde, DateTime? hasta);

        /**
         * @brief Repuestos más vendidos por unidades.
         * @param desde Fecha inicial (incluida), o null
         * @param hasta Fecha final (excluida), o null
         * @param cantidad Número de filas
         * @return Repuesto, unidades y total
         */
        IEnumerable<ItemRanking> TopRepuestos(DateTime? desde, DateTime? hasta, int cantidad);

        /**
         * @brief Clientes registrados que más han comprado.
         * @param desde Fecha inicial (incluida), o null
         * @param hasta Fecha final (excluida), o null
         * @param cantidad Número de filas
         * @return Cliente, número de compras y total
         */
        IEnumerable<ItemRanking> TopClientes(DateTime? desde, DateTime? hasta, int cantidad);

        /**
         * @brief Ventas de cada usuario.
         * @param desde Fecha inicial (incluida), o null
         * @param hasta Fecha final (excluida), o null
         * @return Usuario, número de ventas y total
         */
        IEnumerable<ItemRanking> VentasPorUsuario(DateTime? desde, DateTime? hasta);
    }
}
