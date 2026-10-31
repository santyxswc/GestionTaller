/**
 * @file UsuariosViewModel.cs
 * @brief Lógica de la sección de usuarios.
 * @author Santiago Caicedo
 */
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Services;
using TallerMoreno.Desktop.Services;
using TallerMoreno.Desktop.Views;

namespace TallerMoreno.Desktop.ViewModels
{
    /**
     * @brief Cuentas del taller (solo administrador): crear, activar o desactivar y restablecer contraseñas.
     */
    public partial class UsuariosViewModel : ObservableObject
    {
        /** Usuarios. */
        private readonly UsuarioService _usuarios;
        /** Creación de ventanas. */
        private readonly FabricaVistas _vistas;
        /** Avisos breves. */
        private readonly Avisos _avisos;

        /** Cuentas. */
        public ObservableCollection<Usuario> Usuarios { get; } = new();

        /** Cuenta seleccionada. */
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CambiarEstadoCommand), nameof(RestablecerCommand))]
        [NotifyPropertyChangedFor(nameof(TextoEstado))]
        private Usuario seleccionado;

        /** Texto del botón de estado según la cuenta seleccionada. */
        public string TextoEstado => Seleccionado is { Activo: false } ? "Activar" : "Desactivar";

        /** Crea una cuenta. */
        public IAsyncRelayCommand NuevoCommand { get; }
        /** Activa o desactiva la cuenta seleccionada. */
        public IAsyncRelayCommand CambiarEstadoCommand { get; }
        /** Pone una contraseña nueva a la cuenta seleccionada. */
        public IAsyncRelayCommand RestablecerCommand { get; }

        /**
         * @brief Crea los comandos y carga las cuentas.
         * @param usuarios Usuarios
         * @param vistas Creación de ventanas
         * @param avisos Avisos breves
         */
        public UsuariosViewModel(UsuarioService usuarios, FabricaVistas vistas, Avisos avisos)
        {
            _usuarios = usuarios;
            _vistas = vistas;
            _avisos = avisos;

            NuevoCommand = new AsyncRelayCommand(Nuevo);
            CambiarEstadoCommand = new AsyncRelayCommand(CambiarEstado, () => Seleccionado != null);
            RestablecerCommand = new AsyncRelayCommand(Restablecer, () => Seleccionado != null);
            Cargar();
        }

        /**
         * @brief Vuelve a leer las cuentas.
         * @param seleccionarId Cuenta que queda seleccionada
         */
        private void Cargar(int seleccionarId = 0)
        {
            try
            {
                Usuarios.Clear();
                foreach (var u in _usuarios.ObtenerUsuarios())
                    Usuarios.Add(u);
                Seleccionado = Usuarios.FirstOrDefault(u => u.Id == seleccionarId);
            }
            catch (Exception ex)
            {
                _avisos.Error(ex.Message);
            }
        }

        /**
         * @brief Abre el formulario de cuenta nueva.
         */
        private async Task Nuevo()
        {
            var (ventana, vm) = _vistas.Ventana<NuevoUsuarioView, NuevoUsuarioViewModel>();
            if (!await Dialogs.ShowForm(ventana)) return;
            _avisos.Exito($"{vm.Creado.Nombre} ya puede entrar con el usuario «{vm.Creado.Login}».");
            Cargar(vm.Creado.Id);
        }

        /**
         * @brief Activa o desactiva la cuenta seleccionada.
         */
        private async Task CambiarEstado()
        {
            var u = Seleccionado;
            if (u.Activo && !await Dialogs.Confirm($"{u.Nombre} no podrá entrar hasta que vuelvas a activar su cuenta.",
                                                   "Desactivar usuario", "Desactivar"))
                return;
            try
            {
                _usuarios.CambiarEstado(u.Id, !u.Activo);
                _avisos.Exito(u.Activo ? $"{u.Nombre} quedó desactivado." : $"{u.Nombre} quedó activo.");
                Cargar(u.Id);
            }
            catch (Exception ex)
            {
                await Dialogs.Error(ex.Message);
            }
        }

        /**
         * @brief Abre el formulario para poner una contraseña nueva a la cuenta seleccionada.
         */
        private async Task Restablecer()
        {
            var u = Seleccionado;
            var (ventana, vm) = _vistas.Ventana<CambiarClaveView, CambiarClaveViewModel>();
            vm.ParaRestablecer(u);
            if (await Dialogs.ShowForm(ventana))
                _avisos.Exito($"La contraseña de {u.Nombre} quedó cambiada.");
        }
    }
}
