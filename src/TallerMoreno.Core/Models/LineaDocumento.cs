/**
 * @file LineaDocumento.cs
 * @brief Línea de una venta o de una entrada de mercancía.
 * @author Santiago Caicedo
 */
namespace TallerMoreno.Core.Models
{
    /**
     * @brief Repuesto, cantidad y precio de una venta (venta_detalle) o de una entrada (entrada_detalle).
     */
    public class LineaDocumento
    {
        /** Identificador del repuesto. */
        public int RepuestoId { get; set; }
        /** Código del repuesto. */
        public string Codigo { get; set; }
        /** Nombre del repuesto. */
        public string Nombre { get; set; }
        /** Unidades. */
        public int Cantidad { get; set; }
        /** Precio por unidad: de venta en una venta, de compra en una entrada. */
        public decimal PrecioUnitario { get; set; }
        /** Costo por unidad en el momento de la venta (para calcular la ganancia). */
        public decimal CostoUnitario { get; set; }
        /** Cantidad por precio unitario. */
        public decimal Subtotal { get; set; }
    }
}
