/**
 * @file AjustarStockViewModel.cs
 * @brief Lógica del ajuste de unidades.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Services;
using TallerMoreno.Desktop.Services;

namespace TallerMoreno.Desktop.ViewModels
{
    /**
     * @brief Corrige las unidades de un repuesto con lo que se contó en la bodega.
     */
    public partial class AjustarStockViewModel : ObservableObject
    {
        /** Motivos frecuentes, para elegir con un toque. */
        public static readonly string[] MotivosFrecuentes = { "Conteo de bodega", "Daño o pérdida", "Corrección de error", "Uso en el taller" };

        /** Inventario. */
        private readonly RepuestoService _service;
        /** Ventana del formulario. */
        private readonly Window _window;

        /** Repuesto que se ajusta. */
        public Repuesto Repuesto { get; }
        /** Motivos frecuentes. */
        public string[] Motivos => MotivosFrecuentes;

        /** Unidades contadas. */
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Diferencia))]
        private decimal? nuevoStock;

        /** Motivo del ajuste. */
        [ObservableProperty]
        private string motivo = MotivosFrecuentes[0];

        /** Error de validación o de guardado. */
        [ObservableProperty]
        private string errorMessage;

        /** Diferencia con las unidades actuales, como "+2" o "-1". */
        public string Diferencia
        {
            get
            {
                int d = (int)(NuevoStock ?? 0) - Repuesto.Stock;
                return d == 0 ? "Sin cambios" : d > 0 ? $"+{d} unidades" : $"{d} unidades";
            }
        }

        /** Guarda el ajuste. */
        public IRelayCommand GuardarCommand { get; }
        /** Cierra sin guardar. */
        public IRelayCommand CancelarCommand { get; }

        /**
         * @brief Crea el formulario con las unidades actuales.
         * @param window Ventana del formulario
         * @param repuesto Repuesto a ajustar
         * @param service Inventario
         */
        public AjustarStockViewModel(Window window, Repuesto repuesto, RepuestoService service)
        {
            _window = window;
            _service = service;
            Repuesto = repuesto;
            NuevoStock = repuesto.Stock;
            GuardarCommand = new RelayCommand(Guardar);
            CancelarCommand = new RelayCommand(() => _window.Close(false));
        }

        /**
         * @brief Guarda el ajuste y cierra.
         */
        private void Guardar()
        {
            try
            {
                _service.AjustarStock(Repuesto.Id, (int)(NuevoStock ?? -1), Motivo);
                _window.Close(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = Errores.Mensaje(ex);
            }
        }
    }
}
