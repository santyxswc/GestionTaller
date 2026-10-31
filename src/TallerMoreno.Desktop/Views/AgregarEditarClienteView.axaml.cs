/**
 * @file AgregarEditarClienteView.axaml.cs
 * @brief Formulario de cliente.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;

namespace TallerMoreno.Desktop.Views
{
    /**
     * @brief Formulario para crear o editar un cliente.
     */
    public partial class AgregarEditarClienteView : Window
    {
        /**
         * @brief Crea la ventana y deja el cursor en el nombre.
         */
        public AgregarEditarClienteView()
        {
            InitializeComponent();
            Opened += (_, _) => txtNombre.Focus();
        }
    }
}
