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
using TallerMoreno.Core.Sesion;
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
        /** Usuario de la sesión, para saber si entró un administrador. */
        private readonly ISesionUsuario _sesion;
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
        public IAsyncRelayCommand EntrarCommand { get; }
        /** Abre la recuperación de la contraseña con el código. */
        public IAsyncRelayCommand OlvideClaveCommand { get; }

        /**
         * @brief Crea el ViewModel de la ventana de inicio de sesión.
         * @param window Ventana, que se cierra al entrar
         * @param usuarios Reglas de negocio de usuarios
         * @param vistas Creación de ventanas y secciones
         * @param preferencias Último usuario recordado
         * @param sesion Usuario de la sesión
         */
        public LoginViewModel(Window window, UsuarioService usuarios, FabricaVistas vistas, Preferencias preferencias,
                              ISesionUsuario sesion)
        {
            _usuarios = usuarios;
            _vistas = vistas;
            _preferencias = preferencias;
            _sesion = sesion;
            _window = window;
            EntrarCommand = new AsyncRelayCommand(Entrar);
            OlvideClaveCommand = new AsyncRelayCommand(OlvideClave);

            try
            {
                PrimerUso = !_usuarios.HayUsuarios();
            }
            catch (Exception ex)
            {
                ErrorMessage = Errores.Mensaje(ex);
            }
            if (!PrimerUso)
                Username = _preferencias.UltimoUsuario;
        }

        /** Indica si el campo de usuario ya viene escrito (para poner el cursor en la contraseña). */
        public bool UsuarioRecordado => !PrimerUso && !string.IsNullOrEmpty(Username);

        /**
         * @brief Abre el formulario de recuperación y, si sale bien, muestra el código nuevo.
         */
        private async Task OlvideClave()
        {
            var (ventana, vm) = _vistas.Ventana<RecuperarClaveView, RecuperarClaveViewModel>();
            if (!await Dialogs.ShowForm(ventana))
                return;
            Username = vm.Usuario?.Trim().ToLowerInvariant();
            Password = "";
            ErrorMessage = null;
            await Dialogs.CodigoRecuperacion(vm.CodigoNuevo);
        }

        /**
         * @brief Verifica los datos, entra y abre la ventana principal.
         *
         * Si entra un administrador y todavía no hay código de recuperación (primer uso o datos
         * de la versión 1.0), se crea uno y se muestra antes de seguir.
         */
        private async Task Entrar()
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

                if (_sesion.EsAdmin && !_usuarios.TieneCodigoRecuperacion())
                    await Dialogs.CodigoRecuperacion(_usuarios.GenerarCodigoRecuperacion());

                var (main, _) = _vistas.Ventana<MainWindow, MainWindowViewModel>();
                if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    desktop.MainWindow = main;
                main.Show();
                _window.Close();
            }
            catch (Exception ex)
            {
                ErrorMessage = Errores.Mensaje(ex);
            }
        }
    }
}
