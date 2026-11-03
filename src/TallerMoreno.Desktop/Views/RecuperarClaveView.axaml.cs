/**
 * @file RecuperarClaveView.axaml.cs
 * @brief Ventana para recuperar la contraseña del dueño.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;

namespace TallerMoreno.Desktop.Views
{
    /**
     * @brief Formulario con el código de recuperación y la contraseña nueva.
     */
    public partial class RecuperarClaveView : Window
    {
        /**
         * @brief Crea la ventana y pone el cursor en el código.
         */
        public RecuperarClaveView()
        {
            InitializeComponent();
            Opened += (_, _) => txtCodigo.Focus();
        }
    }
}
