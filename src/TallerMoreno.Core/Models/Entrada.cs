/**
 * @file Entrada.cs
 * @brief Modelo de entrada de mercancía.
 * @author Santiago Caicedo
 */
namespace TallerMoreno.Core.Models
{
    /**
     * @brief Compra de repuestos a un proveedor que suma unidades al inventario (entrada).
     */
    public class Entrada
    {
        /** Número de la entrada. */
        public int Id { get; set; }
        /** Fecha y hora. */
        public DateTime Fecha { get; set; }
        /** Proveedor. */
        public int ProveedorId { get; set; }
        /** Nombre del proveedor. */
        public string ProveedorNombre { get; set; }
        /** Número de la factura del proveedor. */
        public string FacturaProveedor { get; set; }
        /** Usuario que registró la entrada. */
        public int UsuarioId { get; set; }
        /** Nombre de quien la registró. */
        public string UsuarioNombre { get; set; }
        /** Total de la factura. */
        public decimal Total { get; set; }
        /** Lo que se pagó al recibir la mercancía. */
        public decimal Pagado { get; set; }
        /** Indica si se quedó debiendo parte de la factura al recibirla. */
        public bool QuedoDebiendo => Pagado < Total;
        /** Unidades recibidas. */
        public int Unidades { get; set; }
        /** Lineas; solo se cargan al pedir el detalle. */
        public List<LineaDocumento> Items { get; set; }
    }
}
