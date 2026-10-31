/**
 * @file LoginView.axaml.cs
 * @brief Ventana de inicio de sesión.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;
using TallerMoreno.Desktop.ViewModels;

namespace TallerMoreno.Desktop.Views
{
    /**
     * @brief Ventana de inicio de sesión y de primer uso.
     */
    public partial class LoginView : Window
    {
        /**
         * @brief Crea la ventana y pone el cursor donde hay que escribir.
         */
        public LoginView()
        {
            InitializeComponent();
            Opened += (_, _) =>
            {
                var vm = DataContext as LoginViewModel;
                (vm?.PrimerUso == true ? txtNombre : vm?.UsuarioRecordado == true ? txtClave : txtUsuario).Focus();
            };
        }
    }
}
