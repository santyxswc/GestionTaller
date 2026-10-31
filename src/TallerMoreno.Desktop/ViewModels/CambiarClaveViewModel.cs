/**
 * @file CambiarClaveViewModel.cs
 * @brief Lógica del cambio de contraseña.
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
     * @brief Cambia la contraseña propia, o la de otro usuario si el administrador la restablece.
     */
    public partial class CambiarClaveViewModel : ObservableObject
    {
        /** Usuarios. */
        private readonly UsuarioService _usuarios;
        /** Ventana del formulario. */
        private readonly Window _window;
        /** Usuario al que se le restablece la contraseña, o null para la propia. */
        private Usuario _otro;

        /** Contraseña actual (solo para la propia). */
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
        private string actual;

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

        /** Título de la ventana. */
        public string Titulo => _otro == null ? "Cambiar mi contraseña" : $"Nueva contraseña para {_otro.Nombre}";
        /** Indica si se pide la contraseña actual. */
        public bool PideActual => _otro == null;

        /** Guarda la contraseña. */
        public IRelayCommand GuardarCommand { get; }
        /** Cierra sin guardar. */
        public IRelayCommand CancelCommand { get; }

        /**
         * @brief Crea el formulario para la contraseña propia.
         * @param window Ventana del formulario
         * @param usuarios Usuarios
         */
        public CambiarClaveViewModel(Window window, UsuarioService usuarios)
        {
            _window = window;
            _usuarios = usuarios;
            GuardarCommand = new RelayCommand(Guardar, PuedeGuardar);
            CancelCommand = new RelayCommand(() => _window.Close(false));
        }

        /**
         * @brief Cambia el formulario para restablecer la contraseña de otro usuario (administrador).
         * @param usuario Usuario
         */
        public void ParaRestablecer(Usuario usuario)
        {
            _otro = usuario;
            OnPropertyChanged(nameof(Titulo));
            OnPropertyChanged(nameof(PideActual));
            GuardarCommand.NotifyCanExecuteChanged();
        }

        /**
         * @brief Indica si están los datos necesarios.
         * @return true si se puede guardar
         */
        private bool PuedeGuardar() =>
            (!PideActual || !string.IsNullOrEmpty(Actual)) && !string.IsNullOrEmpty(Nueva) && !string.IsNullOrEmpty(Repetir);

        /**
         * @brief Valida que coincidan, guarda y cierra.
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
                if (_otro == null)
                    _usuarios.CambiarClave(Actual, Nueva);
                else
                    _usuarios.RestablecerClave(_otro.Id, Nueva);
                _window.Close(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
    }
}
