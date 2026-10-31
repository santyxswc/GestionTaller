/**
 * @file MainWindow.axaml.cs
 * @brief Ventana principal con el menú lateral.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;

namespace TallerMoreno.Desktop.Views
{
    /**
     * @brief Ventana principal con el menú lateral; FabricaVistas le asigna su ViewModel.
     */
    public partial class MainWindow : Window
    {
        /**
         * @brief Crea la ventana.
         */
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
