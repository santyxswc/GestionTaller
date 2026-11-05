/**
 * @file Devolucion.cs
 * @brief Modelo de devolución o garantía.
 * @author Santiago Caicedo
 */
namespace TallerMoreno.Core.Models
{
    /**
     * @brief Repuesto que un cliente le devolvió al taller (devolucion_cliente) o que el taller le devolvió
     * a un proveedor (devolucion_proveedor), por garantía u otro motivo.
     */
    public class Devolucion
    {
        /** Se entregó otro repuesto igual. */
        public const string Cambio = "Cambio";
        /** Se devolvió la plata. */
        public const string Dinero = "Dinero";
        /** Se rebajó de lo que se debe. */
        public const string Descuento = "Descuento";
        /** El proveedor todavía no responde (solo devoluciones a proveedores). */
        public const string Pendiente = "Pendiente";

        /** Motivo: el repuesto salió malo. */
        public const string MotivoGarantia = "Garantía";

        /** Motivos de un cliente. */
        public static readonly string[] MotivosCliente = { MotivoGarantia, "No le sirvió", "Otro" };
        /** Motivos con un proveedor. */
        public static readonly string[] MotivosProveedor = { MotivoGarantia, "Llegó dañado", "Pedido equivocado", "Otro" };

        /** Identificador. */
        public int Id { get; set; }
        /** Fecha y hora. */
        public DateTime Fecha { get; set; }
        /** Venta de la que sale (solo devoluciones de clientes). */
        public int? VentaId { get; set; }
        /** Proveedor al que se le devolvió (solo devoluciones a proveedores). */
        public int? ProveedorId { get; set; }
        /** Repuesto. */
        public int RepuestoId { get; set; }
        /** Nombre del repuesto. */
        public string RepuestoNombre { get; set; }
        /** Unidades devueltas. */
        public int Cantidad { get; set; }
        /** Garantía, No le sirvió, Llegó dañado... */
        public string Motivo { get; set; }
        /** Cambio, Dinero, Descuento o Pendiente. */
        public string Resultado { get; set; }
        /** Plata devuelta o rebajada; 0 en un cambio o si está pendiente. */
        public decimal Valor { get; set; }
        /** Si el repuesto entró al inventario (cliente) o salió de él (proveedor). */
        public bool MovioInventario { get; set; }
        /** Notas. */
        public string Notas { get; set; }
        /** Quién la registró. */
        public string UsuarioNombre { get; set; }

        /** Indica si el proveedor todavía no ha respondido. */
        public bool EsPendiente => Resultado == Pendiente;

        /**
         * @brief Qué se hizo, en palabras de cada lado.
         * @param deProveedor true si es una devolución a un proveedor
         * @param conValor true para decir cuánta plata (donde no hay una columna con el valor)
         * @return Como "se le cambió por otro" o "te devolvió $ 30.000"
         */
        public string TextoResultado(bool deProveedor, bool conValor = true)
        {
            string plata = conValor ? $" {Formato.Moneda(Valor)}" : "";
            return Resultado switch
            {
                Cambio => deProveedor ? "te lo cambió" : "se le cambió por otro",
                Dinero => deProveedor ? (conValor ? $"te devolvió{plata}" : "te devolvió la plata")
                                      : (conValor ? $"se le devolvieron{plata}" : "se le devolvió la plata"),
                Descuento => deProveedor ? $"te rebajó{plata} de la deuda" : $"se le rebajaron{plata} de la deuda",
                _ => "esperando respuesta"
            };
        }
    }
}
