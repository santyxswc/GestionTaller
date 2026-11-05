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
        /** Total de la venta. */
        public decimal Total { get; set; }
        /** Total menos el costo de los repuestos. */
        public decimal Ganancia { get; set; }
        /** Lo que el cliente pagó al comprar; en las ventas de mostrador es todo el total. */
        public decimal Abonado { get; set; }
        /** Indica si se fió: el cliente quedó debiendo parte de la venta. */
        public bool Fiada => ClienteId.HasValue && Abonado < Total;
        /** Unidades vendidas. */
        public int Unidades { get; set; }
        /** Indica si se anuló (el stock se devolvió al inventario). */
        public bool Anulada { get; set; }
        /** Notas. */
        public string Notas { get; set; }
        /** Kilometraje del carro cuando se hizo la venta, o null. */
        public int? Kilometraje { get; set; }
        /** Número de devoluciones de esta venta. */
        public int Devoluciones { get; set; }
        /** Lineas; solo se cargan al pedir el detalle. */
        public List<LineaDocumento> Items { get; set; }

        /** Nombre del cliente o "Cliente de mostrador". */
        public string Cliente => string.IsNullOrWhiteSpace(ClienteNombre) ? ClienteMostrador : ClienteNombre;
        /** "Anulada", "Devolución" o vacío. */
        public string Estado => Anulada ? "Anulada" : Devoluciones > 0 ? "Devolución" : "";
    }
}
