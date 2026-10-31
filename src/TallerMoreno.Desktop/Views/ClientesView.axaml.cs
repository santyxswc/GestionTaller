/**
 * @file ClientesView.axaml.cs
 * @brief Sección de clientes.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;
using TallerMoreno.Desktop.ViewModels;

namespace TallerMoreno.Desktop.Views
{
    /**
     * @brief Tabla de clientes; doble toque en una fila muestra sus compras.
     */
    public partial class ClientesView : UserControl
    {
        /**
         * @brief Crea la sección.
         */
        public ClientesView()
        {
            InitializeComponent();
            tabla.DoubleTapped += (_, _) =>
            {
                if (DataContext is ClientesViewModel vm && vm.ComprasCommand.CanExecute(null))
                    vm.ComprasCommand.Execute(null);
            };
        }
    }
}
