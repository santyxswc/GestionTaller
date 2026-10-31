/**
 * @file Venta.cs
 * @brief Modelo de venta.
 * @author Santiago Caicedo
 */
namespace TallerMoreno.Core.Models
{
    /**
     * @brief Venta de repuestos (venta) con los nombres del cliente y de quien vendió.
     */
    public class Venta
    {
        /** Nombre que se muestra cuando la venta no tiene cliente registrado. */
        public const string ClienteMostrador = "Cliente de mostrador";

        /** Número de la venta. */
        public int Id { get; set; }
        /** Fecha y hora. */
        public DateTime Fecha { get; set; }
        /** Cliente registrado, o null si fue de mostrador. */
        public int? ClienteId { get; set; }
        /** Nombre del cliente registrado. */
        public string ClienteNombre { get; set; }
        /** Placa del vehículo del cliente. */
        public string ClientePlaca { get; set; }
        /** Usuario que hizo la venta. */
        public int UsuarioId { get; set; }
        /** Nombre de quien vendió. */
        public string UsuarioNombre { get; set; }
        /** Efectivo, transferencia... */
        public string MetodoPago { get; set; }
        /** Total cobrado. */
        public decimal Total { get; set; }
        /** Total menos el costo de los repuestos. */
        public decimal Ganancia { get; set; }
        /** Unidades vendidas. */
        public int Unidades { get; set; }
        /** Indica si se anuló (el stock se devolvió al inventario). */
        public bool Anulada { get; set; }
        /** Notas. */
        public string Notas { get; set; }
        /** Lineas; solo se cargan al pedir el detalle. */
        public List<LineaDocumento> Items { get; set; }

        /** Nombre del cliente o "Cliente de mostrador". */
        public string Cliente => string.IsNullOrWhiteSpace(ClienteNombre) ? ClienteMostrador : ClienteNombre;
        /** "Anulada" o vacío. */
        public string Estado => Anulada ? "Anulada" : "";
    }
}
