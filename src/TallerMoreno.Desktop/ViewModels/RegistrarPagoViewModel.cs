/**
 * @file RegistrarPagoViewModel.cs
 * @brief Lógica del registro de un pago a un proveedor o de un abono de un cliente.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TallerMoreno.Core;
using TallerMoreno.Core.Services;
using TallerMoreno.Desktop.Services;

namespace TallerMoreno.Desktop.ViewModels
{
    /**
     * @brief Pide cuánto se pagó y cómo, y muestra cuánto queda debiendo.
     *
     * Sirve para los dos lados: lo que el taller le paga a un proveedor y lo que un cliente le abona al taller.
     */
    public partial class RegistrarPagoViewModel : ObservableObject
    {
        /** Ventana del formulario. */
        private readonly Window _window;
        /** Guarda el pago con el valor, el método y las notas. */
        private readonly Action<decimal, string, string> _guardar;

        /** Título, como "Pagarle a Autopartes El Sol". */
        public string Titulo { get; }
        /** Lo que se debe antes de este pago. */
        public decimal Deuda { get; }
        /** Texto con lo que se debe, como "Le debes $150.000". */
        public string TextoDeuda { get; }
        /** Métodos de pago. */
        public string[] MetodosPago => VentaService.MetodosPago;

        /** Valor del pago. */
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Queda))]
        private decimal? valor;

        /** Método de pago elegido. */
        [ObservableProperty]
        private string metodoPago = VentaService.MetodosPago[0];

        /** Notas. */
        [ObservableProperty]
        private string notas;

        /** Error de validación o de guardado. */
        [ObservableProperty]
        private string errorMessage;

        /** Lo que queda debiendo después de este pago. */
        public string Queda
        {
            get
            {
                decimal queda = Deuda - (Valor ?? 0);
                return queda > 0 ? $"Después de este pago quedan {Formato.Moneda(queda)} por pagar."
                     : queda == 0 ? "Con este pago queda al día."
                     : $"Es {Formato.Moneda(-queda)} más de lo que se debe.";
            }
        }

        /** Guarda el pago. */
        public IRelayCommand GuardarCommand { get; }
        /** Cierra sin guardar. */
        public IRelayCommand CancelarCommand { get; }

        /**
         * @brief Crea el formulario con todo lo que se debe como valor propuesto.
         * @param window Ventana del formulario
         * @param titulo Título
         * @param textoDeuda Lo que se debe, en palabras
         * @param deuda Lo que se debe
         * @param guardar Guarda el pago; lanza una excepción con el mensaje si no se puede
         */
        public RegistrarPagoViewModel(Window window, string titulo, string textoDeuda, decimal deuda, Action<decimal, string, string> guardar)
        {
            _window = window;
            _guardar = guardar;
            Titulo = titulo;
            TextoDeuda = textoDeuda;
            Deuda = deuda;
            Valor = deuda;
            GuardarCommand = new RelayCommand(Guardar);
            CancelarCommand = new RelayCommand(() => _window.Close(false));
        }

        /**
         * @brief Guarda el pago y cierra.
         */
        private void Guardar()
        {
            ErrorMessage = null;
            try
            {
                _guardar(Valor ?? 0, MetodoPago, Notas);
                _window.Close(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = Errores.Mensaje(ex);
            }
        }
    }
}
