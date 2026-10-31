/**
 * @file ProveedoresView.axaml.cs
 * @brief Sección de proveedores.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;
using TallerMoreno.Desktop.ViewModels;

namespace TallerMoreno.Desktop.Views
{
    /**
     * @brief Tabla de proveedores; doble toque en una fila muestra sus entradas.
     */
    public partial class ProveedoresView : UserControl
    {
        /**
         * @brief Crea la sección.
         */
        public ProveedoresView()
        {
            InitializeComponent();
            tabla.DoubleTapped += (_, _) =>
            {
                if (DataContext is ProveedoresViewModel vm && vm.EntradasCommand.CanExecute(null))
                    vm.EntradasCommand.Execute(null);
            };
        }
    }
}
