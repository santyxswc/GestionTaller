/**
 * @file LoginViewModel.cs
 * @brief Lógica de la ventana de inicio de sesión y del primer uso.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TallerMoreno.Core.Services;
using TallerMoreno.Desktop.Services;
using TallerMoreno.Desktop.Views;

namespace TallerMoreno.Desktop.ViewModels
{
    /**
     * @brief Autentica al usuario o, la primera vez, crea la cuenta del dueño; luego abre la ventana principal.
     */
    public partial class LoginViewModel : ObservableObject
    {
        /** Reglas de negocio de usuarios. */
        private readonly UsuarioService _usuarios;
        /** Creación de ventanas y secciones. */
        private readonly FabricaVistas _vistas;
        /** Último usuario recordado. */
        private readonly Preferencias _preferencias;
        /** Ventana de inicio de sesión. */
        private readonly Window _window;

        /** Indica que no hay usuarios y hay que crear la cuenta del dueño. */
        public bool PrimerUso { get; }

        /** Nombre completo del dueño (solo primer uso). */
        [ObservableProperty]
        private string nombre;

        /** Usuario escrito. */
        [ObservableProperty]
        private string username;

        /** Contraseña escrita. */
        [ObservableProperty]
        private string password;

        /** Contraseña repetida (solo primer uso). */
        [ObservableProperty]
        private string repetir;

        /** Mensaje de error para mostrar en la ventana. */
        [ObservableProperty]
        private string errorMessage;

        /** Comando del botón Entrar o Crear cuenta. */
        public IRelayCommand EntrarCommand { get; }

        /**
         * @brief Crea el ViewModel de la ventana de inicio de sesión.
         * @param window Ventana, que se cierra al entrar
         * @param usuarios Reglas de negocio de usuarios
         * @param vistas Creación de ventanas y secciones
         * @param preferencias Último usuario recordado
         */
        public LoginViewModel(Window window, UsuarioService usuarios, FabricaVistas vistas, Preferencias preferencias)
        {
            _usuarios = usuarios;
            _vistas = vistas;
            _preferencias = preferencias;
            _window = window;
            EntrarCommand = new RelayCommand(Entrar);

            try
            {
                PrimerUso = !_usuarios.HayUsuarios();
            }
            catch (Exception ex)
            {
                ErrorMessage = "No se pudo abrir la base de datos: " + ex.Message;
            }
            if (!PrimerUso)
                Username = _preferencias.UltimoUsuario;
        }

        /** Indica si el campo de usuario ya viene escrito (para poner el cursor en la contraseña). */
        public bool UsuarioRecordado => !PrimerUso && !string.IsNullOrEmpty(Username);

        /**
         * @brief Verifica los datos, entra y abre la ventana principal.
         */
        private void Entrar()
        {
            ErrorMessage = null;
            try
            {
                if (PrimerUso)
                {
                    if (Password != Repetir)
                    {
                        ErrorMessage = "Las contraseñas no coinciden.";
                        return;
                    }
                    _usuarios.CrearPrimerAdministrador(Nombre, Username, Password);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrEmpty(Password))
                    {
                        ErrorMessage = "Escribe tu usuario y tu contraseña.";
                        return;
                    }
                    if (_usuarios.IniciarSesion(Username, Password) == null)
                    {
                        ErrorMessage = "Usuario o contraseña incorrectos.";
                        Password = "";
                        return;
                    }
                }
                _preferencias.UltimoUsuario = Username.Trim().ToLowerInvariant();

                var (main, _) = _vistas.Ventana<MainWindow, MainWindowViewModel>();
                if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    desktop.MainWindow = main;
                main.Show();
                _window.Close();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
    }
}
