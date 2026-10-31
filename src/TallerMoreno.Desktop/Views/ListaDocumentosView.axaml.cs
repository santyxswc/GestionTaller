/**
 * @file ListaDocumentosView.axaml.cs
 * @brief Ventana con las compras de un cliente o las entradas de un proveedor.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;
using TallerMoreno.Desktop.ViewModels;

namespace TallerMoreno.Desktop.Views
{
    /**
     * @brief Lista de documentos; doble toque en una fila abre su detalle.
     */
    public partial class ListaDocumentosView : Window
    {
        /**
         * @brief Crea la ventana.
         */
        public ListaDocumentosView()
        {
            InitializeComponent();
            tabla.DoubleTapped += (_, _) =>
            {
                if (DataContext is ListaDocumentosViewModel vm && tabla.SelectedItem is FilaDocumento fila)
                    vm.AbrirCommand.Execute(fila);
            };
        }
    }
}
