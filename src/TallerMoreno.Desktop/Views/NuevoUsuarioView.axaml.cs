/**
 * @file NuevoUsuarioView.axaml.cs
 * @brief Formulario de usuario nuevo.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;

namespace TallerMoreno.Desktop.Views
{
    /**
     * @brief Formulario para crear una cuenta.
     */
    public partial class NuevoUsuarioView : Window
    {
        /**
         * @brief Crea la ventana y deja el cursor en el nombre.
         */
        public NuevoUsuarioView()
        {
            InitializeComponent();
            Opened += (_, _) => txtNombre.Focus();
        }
    }
}
