/**
 * @file RecuperarClaveViewModel.cs
 * @brief Lógica para poner una contraseña nueva con el código de recuperación.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TallerMoreno.Core.Services;
using TallerMoreno.Desktop.Services;

namespace TallerMoreno.Desktop.ViewModels
{
    /**
     * @brief El dueño escribe su usuario, el código anotado y una contraseña nueva.
     */
    public partial class RecuperarClaveViewModel : ObservableObject
    {
        /** Usuarios. */
        private readonly UsuarioService _usuarios;
        /** Ventana del formulario. */
        private readonly Window _window;

        /** Usuario del dueño. */
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
        private string usuario;

        /** Código de recuperación anotado. */
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
        private string codigo;

        /** Contraseña nueva. */
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
        private string nueva;

        /** Contraseña nueva repetida. */
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
        private string repetir;

        /** Error de validación o de guardado. */
        [ObservableProperty]
        private string errorMessage;

        /** Código nuevo que entrega el programa al recuperar, para mostrarlo después. */
        public string CodigoNuevo { get; private set; }

        /** Pone la contraseña nueva. */
        public IRelayCommand GuardarCommand { get; }
        /** Cierra sin cambiar nada. */
        public IRelayCommand CancelCommand { get; }

        /**
         * @brief Crea el formulario.
         * @param window Ventana del formulario
         * @param usuarios Usuarios
         * @param preferencias Último usuario, para dejarlo escrito
         */
        public RecuperarClaveViewModel(Window window, UsuarioService usuarios, Preferencias preferencias)
        {
            _window = window;
            _usuarios = usuarios;
            // Los comandos van primero: escribir Usuario avisa a GuardarCommand.
            GuardarCommand = new RelayCommand(Guardar, PuedeGuardar);
            CancelCommand = new RelayCommand(() => _window.Close(false));
            Usuario = preferencias.UltimoUsuario;
        }

        /**
         * @brief Indica si están todos los datos.
         * @return true si se puede guardar
         */
        private bool PuedeGuardar() =>
            !string.IsNullOrWhiteSpace(Usuario) && !string.IsNullOrWhiteSpace(Codigo)
            && !string.IsNullOrEmpty(Nueva) && !string.IsNullOrEmpty(Repetir);

        /**
         * @brief Comprueba el código, pone la contraseña nueva y cierra.
         */
        private void Guardar()
        {
            if (Nueva != Repetir)
            {
                ErrorMessage = "Las contraseñas nuevas no coinciden.";
                return;
            }
            try
            {
                CodigoNuevo = _usuarios.RecuperarClave(Usuario, Codigo, Nueva);
                _window.Close(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = Errores.Mensaje(ex);
            }
        }
    }
}
