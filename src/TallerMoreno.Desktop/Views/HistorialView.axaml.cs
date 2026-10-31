/**
 * @file HistorialView.axaml.cs
 * @brief Sección del historial de ventas.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;
using TallerMoreno.Desktop.ViewModels;

namespace TallerMoreno.Desktop.Views
{
    /**
     * @brief Ventas por periodo; doble toque en una fila abre su detalle.
     */
    public partial class HistorialView : UserControl
    {
        /**
         * @brief Crea la sección.
         */
        public HistorialView()
        {
            InitializeComponent();
            tabla.DoubleTapped += (_, _) =>
            {
                if (DataContext is HistorialViewModel vm && vm.VerCommand.CanExecute(null))
                    vm.VerCommand.Execute(null);
            };
        }
    }
}
