/**
 * @file DocumentoView.axaml.cs
 * @brief Ventana de detalle de una venta o entrada.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Services;
using TallerMoreno.Desktop.Services;
using TallerMoreno.Desktop.ViewModels;

namespace TallerMoreno.Desktop.Views
{
    /**
     * @brief Detalle de una venta o de una entrada de mercancía.
     */
    public partial class DocumentoView : Window
    {
        /**
         * @brief Crea la ventana.
         */
        public DocumentoView()
        {
            InitializeComponent();
        }

        /**
         * @brief Muestra el detalle de una venta con sus devoluciones y el botón para registrar una.
         * @param ventas Ventas
         * @param devoluciones Devoluciones
         * @param ventaId Venta
         * @return true si se registró una devolución
         */
        public static async Task<bool> MostrarVenta(VentaService ventas, DevolucionService devoluciones, int ventaId)
        {
            var venta = ventas.ObtenerDetalle(ventaId);
            if (venta == null)
                return false;

            bool sePuedeDevolver = !venta.Anulada && venta.Items.Any(l => l.PorDevolver > 0);
            var ventana = new DocumentoView();
            ventana.DataContext = DocumentoViewModel.DeVenta(ventana, venta, devoluciones.DeVenta(ventaId),
                sePuedeDevolver ? () => Devolver(devoluciones, venta) : null);
            return await Dialogs.ShowForm(ventana);
        }

        /**
         * @brief Abre el registro de lo que el cliente devolvió de una venta.
         * @param devoluciones Devoluciones
         * @param venta Venta con sus líneas
         * @return true si se guardó
         */
        private static Task<bool> Devolver(DevolucionService devoluciones, Venta venta)
        {
            var ventana = new DevolucionView();
            ventana.DataContext = DevolucionViewModel.DeVenta(ventana, devoluciones, venta);
            return Dialogs.ShowForm(ventana);
        }

        /**
         * @brief Muestra el detalle de una entrada.
         * @param entrada Entrada con sus líneas
         * @return Tarea que termina al cerrar
         */
        public static Task Mostrar(Entrada entrada)
        {
            var ventana = new DocumentoView();
            ventana.DataContext = DocumentoViewModel.DeEntrada(ventana, entrada);
            return Dialogs.ShowForm(ventana);
        }
    }
}
