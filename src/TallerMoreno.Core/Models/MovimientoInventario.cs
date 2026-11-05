/**
 * @file MovimientoInventario.cs
 * @brief Modelo de movimiento del inventario.
 * @author Santiago Caicedo
 */
namespace TallerMoreno.Core.Models
{
    /**
     * @brief Cambio en las unidades de un repuesto (movimiento_inventario).
     *
     * Cada venta, entrada, ajuste o anulación deja un movimiento con el stock que quedó,
     * para saber siempre por qué el inventario tiene las unidades que tiene.
     */
    public class MovimientoInventario
    {
        /** Repuesto registrado con unidades iniciales. */
        public const string TipoAlta = "Alta";
        /** Unidades que salieron por una venta. */
        public const string TipoVenta = "Venta";
        /** Unidades que llegaron de un proveedor. */
        public const string TipoEntrada = "Entrada";
        /** Corrección manual del conteo. */
        public const string TipoAjuste = "Ajuste";
        /** Unidades devueltas al anular una venta. */
        public const string TipoAnulacion = "Anulación";
        /** Unidades que un cliente devolvió y volvieron al inventario. */
        public const string TipoDevolucion = "Devolución";
        /** Unidades cambiadas por garantía: salen hacia el cliente o llegan del proveedor. */
        public const string TipoGarantia = "Garantía";
        /** Unidades que se le devolvieron a un proveedor. */
        public const string TipoDevolucionProveedor = "Devolución a proveedor";

        /** Identificador. */
        public int Id { get; set; }
        /** Fecha y hora. */
        public DateTime Fecha { get; set; }
        /** Repuesto. */
        public int RepuestoId { get; set; }
        /** Nombre del repuesto. */
        public string RepuestoNombre { get; set; }
        /** Alta, Venta, Entrada, Ajuste o Anulación. */
        public string Tipo { get; set; }
        /** Unidades: positivas si entran, negativas si salen. */
        public int Cantidad { get; set; }
        /** Stock que quedó después del movimiento. */
        public int StockResultante { get; set; }
        /** Venta, entrada o motivo del ajuste. */
        public string Referencia { get; set; }
        /** Nombre de quien hizo el movimiento. */
        public string UsuarioNombre { get; set; }

        /** Cantidad con signo, como "+5" o "-1". */
        public string CantidadTexto => Cantidad > 0 ? $"+{Cantidad}" : Cantidad.ToString();
    }
}
