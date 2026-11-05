/**
 * @file Pago.cs
 * @brief Modelo de pago a un proveedor o abono de un cliente.
 * @author Santiago Caicedo
 */
namespace TallerMoreno.Core.Models
{
    /**
     * @brief Dinero que el taller le paga a un proveedor o que un cliente le abona al taller.
     *
     * Lo pagado al recibir la mercancía o al vender queda unido a esa entrada o venta (DocumentoId);
     * los pagos que se hacen después, para ir bajando la deuda, no tienen documento.
     */
    public class Pago
    {
        /** Identificador. */
        public int Id { get; set; }
        /** Fecha y hora. */
        public DateTime Fecha { get; set; }
        /** Valor pagado. */
        public decimal Valor { get; set; }
        /** Efectivo, transferencia...; puede faltar en los pagos hechos al recibir mercancía. */
        public string MetodoPago { get; set; }
        /** Notas. */
        public string Notas { get; set; }
        /** Quién lo registró. */
        public string UsuarioNombre { get; set; }
        /** Entrada o venta a la que pertenece, o null si es un pago posterior. */
        public int? DocumentoId { get; set; }
    }
}
