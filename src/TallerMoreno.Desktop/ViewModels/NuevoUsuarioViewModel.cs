/**
 * @file NuevoUsuarioViewModel.cs
 * @brief Lógica del formulario de usuario nuevo.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Services;

namespace TallerMoreno.Desktop.ViewModels
{
    /**
     * @brief Crea la cuenta de un mecánico o de otro administrador.
     */
    public partial class NuevoUsuarioViewModel : ObservableObject
    {
        /** Usuarios. */
        private readonly UsuarioService _usuarios;
        /** Ventana del formulario. */
        private readonly Window _window;

        /** Roles. */
        public string[] Roles => Usuario.Roles;

        /** Nombre completo. */
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
        private string nombre;

        /** Nombre de usuario para entrar. */
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
        private string login;

        /** Contraseña inicial. */
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
        private string clave;

        /** Rol. */
        [ObservableProperty]
        private string rol = Usuario.RolMecanico;

        /** Error de validación o de guardado. */
        [ObservableProperty]
        private string errorMessage;

        /** Usuario creado, después de guardar. */
        public Usuario Creado { get; private set; }

        /** Crea la cuenta. */
        public IRelayCommand GuardarCommand { get; }
        /** Cierra sin guardar. */
        public IRelayCommand CancelCommand { get; }

        /**
         * @brief Crea el formulario.
         * @param window Ventana del formulario
         * @param usuarios Usuarios
         */
        public NuevoUsuarioViewModel(Window window, UsuarioService usuarios)
        {
            _window = window;
            _usuarios = usuarios;
            GuardarCommand = new RelayCommand(Guardar, () =>
                !string.IsNullOrWhiteSpace(Nombre) && !string.IsNullOrWhiteSpace(Login) && !string.IsNullOrEmpty(Clave));
            CancelCommand = new RelayCommand(() => _window.Close(false));
        }

        /**
         * @brief Crea la cuenta y cierra; si falla, muestra el error.
         */
        private void Guardar()
        {
            try
            {
                Creado = _usuarios.CrearUsuario(Nombre, Login, Clave, Rol);
                _window.Close(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
    }
}
