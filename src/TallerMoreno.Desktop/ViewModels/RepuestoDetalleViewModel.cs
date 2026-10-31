/**
 * @file RepuestoDetalleViewModel.cs
 * @brief Lógica del detalle de un repuesto.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Services;
using TallerMoreno.Core.Sesion;
using TallerMoreno.Desktop.Services;
using TallerMoreno.Desktop.Views;

namespace TallerMoreno.Desktop.ViewModels
{
    /**
     * @brief Foto grande, datos, unidades y su historial; el administrador puede editar, ajustar o eliminar.
     */
    public partial class RepuestoDetalleViewModel : ObservableObject
    {
        /** Inventario. */
        private readonly RepuestoService _repuestos;
        /** Usuario de la sesión. */
        private readonly ISesionUsuario _sesion;
        /** Creación de ventanas. */
        private readonly FabricaVistas _vistas;
        /** Avisos breves. */
        private readonly Avisos _avisos;
        /** Ventana del detalle. */
        private readonly Window _window;
        /** Identificador del repuesto. */
        private readonly int _id;

        /** Repuesto con sus datos actuales. */
        [ObservableProperty]
        private Repuesto repuesto;

        /** Cambios de stock, del más reciente al más antiguo. */
        [ObservableProperty]
        private List<MovimientoInventario> movimientos;

        /** Indica si se muestran las acciones de administrador. */
        public bool EsAdmin => _sesion.EsAdmin;

        /** Abre el formulario de edición. */
        public IAsyncRelayCommand EditarCommand { get; }
        /** Abre el ajuste de stock. */
        public IAsyncRelayCommand AjustarCommand { get; }
        /** Elimina el repuesto. */
        public IAsyncRelayCommand EliminarCommand { get; }
        /** Cierra la ventana. */
        public IRelayCommand CerrarCommand { get; }

        /**
         * @brief Crea el detalle y carga los datos actuales del repuesto.
         * @param window Ventana del detalle
         * @param repuesto Repuesto tocado en el inventario
         * @param repuestos Inventario
         * @param sesion Usuario de la sesión
         * @param vistas Creación de ventanas
         * @param avisos Avisos breves
         */
        public RepuestoDetalleViewModel(Window window, Repuesto repuesto, RepuestoService repuestos, ISesionUsuario sesion,
                                        FabricaVistas vistas, Avisos avisos)
        {
            _window = window;
            _id = repuesto.Id;
            _repuestos = repuestos;
            _sesion = sesion;
            _vistas = vistas;
            _avisos = avisos;

            EditarCommand = new AsyncRelayCommand(Editar);
            AjustarCommand = new AsyncRelayCommand(Ajustar);
            EliminarCommand = new AsyncRelayCommand(Eliminar);
            CerrarCommand = new RelayCommand(() => _window.Close(true));
            Cargar();
        }

        /**
         * @brief Lee el repuesto y sus movimientos.
         */
        private void Cargar()
        {
            Repuesto = _repuestos.ObtenerPorId(_id);
            Movimientos = _repuestos.Movimientos(_id).ToList();
        }

        /**
         * @brief Abre el formulario con el repuesto y recarga al guardar.
         */
        private async Task Editar()
        {
            var (ventana, vm) = _vistas.Ventana<AgregarEditarRepuestoView, AgregarEditarRepuestoViewModel>();
            vm.LoadFromModel(Repuesto);
            if (await Dialogs.ShowForm(ventana))
            {
                Cargar();
                _avisos.Exito("Cambios guardados.");
            }
        }

        /**
         * @brief Abre el ajuste de stock y recarga al guardar.
         */
        private async Task Ajustar()
        {
            var (ventana, _) = _vistas.Ventana<AjustarStockView, AjustarStockViewModel>(Repuesto);
            if (await Dialogs.ShowForm(ventana))
            {
                Cargar();
                _avisos.Exito($"Stock de {Repuesto.Nombre}: {Repuesto.Stock} unidades.");
            }
        }

        /**
         * @brief Pide confirmación y elimina el repuesto.
         */
        private async Task Eliminar()
        {
            if (!await Dialogs.Confirm($"Se borrará {Repuesto.Nombre} del inventario. Esto no se puede deshacer.", "¿Eliminar repuesto?", "Eliminar"))
                return;
            try
            {
                _repuestos.Eliminar(_id);
                _avisos.Exito($"{Repuesto.Nombre} se eliminó del inventario.");
                _window.Close(true);
            }
            catch (Exception ex)
            {
                await Dialogs.Error(Errores.Mensaje(ex));
            }
        }
    }
}
