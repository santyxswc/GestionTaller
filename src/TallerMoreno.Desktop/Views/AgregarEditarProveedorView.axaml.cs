/**
 * @file AgregarEditarProveedorView.axaml.cs
 * @brief Formulario de proveedor.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;

namespace TallerMoreno.Desktop.Views
{
    /**
     * @brief Formulario para crear o editar un proveedor.
     */
    public partial class AgregarEditarProveedorView : Window
    {
        /**
         * @brief Crea la ventana y deja el cursor en el nombre.
         */
        public AgregarEditarProveedorView()
        {
            InitializeComponent();
            Opened += (_, _) => txtNombre.Focus();
        }
    }
}
