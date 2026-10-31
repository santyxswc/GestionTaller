/**
 * @file DocumentoView.axaml.cs
 * @brief Ventana de detalle de una venta o entrada.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;
using TallerMoreno.Core.Models;
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
         * @brief Muestra el detalle de una venta.
         * @param venta Venta con sus líneas
         * @return Tarea que termina al cerrar
         */
        public static Task Mostrar(Venta venta)
        {
            var ventana = new DocumentoView();
            ventana.DataContext = DocumentoViewModel.DeVenta(ventana, venta);
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
