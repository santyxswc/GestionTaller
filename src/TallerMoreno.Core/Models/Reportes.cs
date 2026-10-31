/**
 * @file Reportes.cs
 * @brief Modelos de los reportes de ventas.
 * @author Santiago Caicedo
 */
namespace TallerMoreno.Core.Models
{
    /**
     * @brief Totales de las ventas no anuladas de un periodo.
     */
    public class ResumenVentas
    {
        /** Total vendido. */
        public decimal Total { get; set; }
        /** Número de ventas. */
        public int NumeroVentas { get; set; }
        /** Unidades vendidas. */
        public int Unidades { get; set; }
        /** Total vendido menos el costo de los repuestos. */
        public decimal Ganancia { get; set; }
        /** Total dividido entre el número de ventas. */
        public decimal VentaPromedio => NumeroVentas == 0 ? 0 : Math.Round(Total / NumeroVentas, 0);
    }

    /**
     * @brief Fila de un ranking (repuestos más vendidos, mejores clientes...).
     */
    public class ItemRanking
    {
        /** Nombre del repuesto, cliente o usuario. */
        public string Nombre { get; set; }
        /** Unidades o número de ventas. */
        public int Cantidad { get; set; }
        /** Total en pesos. */
        public decimal Total { get; set; }
    }
}
