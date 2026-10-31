/**
 * @file EntradaViewModel.cs
 * @brief Lógica del registro de mercancía recibida.
 * @author Santiago Caicedo
 */
using System.Collections.ObjectModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TallerMoreno.Core;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Services;
using TallerMoreno.Desktop.Services;
using TallerMoreno.Desktop.Views;

namespace TallerMoreno.Desktop.ViewModels
{
    /**
     * @brief Repuesto recibido, con cantidad y costo.
     */
    public class LineaEntrada
    {
        /** Repuesto. */
        public Repuesto Repuesto { get; init; }
        /** Unidades recibidas. */
        public int Cantidad { get; init; }
        /** Costo por unidad. */
        public decimal Costo { get; init; }
        /** Cantidad por costo. */
        public decimal Subtotal => Cantidad * Costo;
    }

    /**
     * @brief Suma al inventario lo que llegó de un proveedor.
     */
    public partial class EntradaViewModel : ObservableObject
    {
        /** Entradas de mercancía. */
        private readonly EntradaService _entradas;
        /** Proveedores. */
        private readonly ProveedorService _proveedores;
        /** Creación de ventanas. */
        private readonly FabricaVistas _vistas;
        /** Avisos breves. */
        private readonly Avisos _avisos;
        /** Ventana del formulario. */
        private readonly Window _window;

        /** Repuestos del inventario. */
        public List<Repuesto> Repuestos { get; }
        /** Repuestos recibidos. */
        public ObservableCollection<LineaEntrada> Lineas { get; } = new();

        /** Proveedores. */
        [ObservableProperty]
        private List<Seleccion> proveedores;

        /** Proveedor elegido. */
        [ObservableProperty]
        private Seleccion proveedor;

        /** Número de la factura del proveedor. */
        [ObservableProperty]
        private string factura;

        /** Repuesto que se va a agregar. */
        [ObservableProperty]
        private Repuesto repuesto;

        /** Unidades que se van a agregar. */
        [ObservableProperty]
        private decimal? cantidad = 1;

        /** Costo por unidad de lo que se va a agregar. */
        [ObservableProperty]
        private decimal? costo;

        /** Total de la entrada. */
        [ObservableProperty]
        private decimal total;

        /** Error de validación o de guardado. */
        [ObservableProperty]
        private string errorMessage;

        /** Filtro de repuestos sin tildes ni mayúsculas. */
        public AutoCompleteFilterPredicate<object> FiltroRepuestos { get; } =
            (texto, item) => item is Repuesto r && Texto.Coincide(texto, r.Nombre, r.Codigo, r.Marca);

        /** Filtro de proveedores sin tildes ni mayúsculas. */
        public AutoCompleteFilterPredicate<object> FiltroProveedores { get; } =
            (texto, item) => item is Seleccion s && Texto.Coincide(texto, s.Nombre);

        /** Agrega el repuesto elegido. */
        public IRelayCommand AgregarCommand { get; }
        /** Quita una línea. */
        public IRelayCommand<LineaEntrada> QuitarCommand { get; }
        /** Registra un proveedor nuevo. */
        public IAsyncRelayCommand NuevoProveedorCommand { get; }
        /** Guarda la entrada. */
        public IRelayCommand GuardarCommand { get; }
        /** Cierra sin guardar. */
        public IRelayCommand CancelarCommand { get; }

        /**
         * @brief Carga proveedores y repuestos.
         * @param window Ventana del formulario
         * @param entradas Entradas de mercancía
         * @param proveedores Proveedores
         * @param repuestos Inventario
         * @param vistas Creación de ventanas
         * @param avisos Avisos breves
         */
        public EntradaViewModel(Window window, EntradaService entradas, ProveedorService proveedores, RepuestoService repuestos,
                                FabricaVistas vistas, Avisos avisos)
        {
            _window = window;
            _entradas = entradas;
            _proveedores = proveedores;
            _vistas = vistas;
            _avisos = avisos;
            Proveedores = _proveedores.ParaSeleccion().ToList();
            Repuestos = repuestos.Buscar().ToList();

            AgregarCommand = new RelayCommand(Agregar);
            QuitarCommand = new RelayCommand<LineaEntrada>(l => { Lineas.Remove(l); ActualizarTotal(); });
            NuevoProveedorCommand = new AsyncRelayCommand(NuevoProveedor);
            GuardarCommand = new RelayCommand(Guardar, () => Lineas.Count > 0);
            CancelarCommand = new RelayCommand(() => _window.Close(false));
        }

        /**
         * @brief Deja elegido un proveedor (al abrir desde Proveedores).
         * @param id Proveedor
         */
        public void ElegirProveedor(int id) => Proveedor = Proveedores.FirstOrDefault(p => p.Id == id);

        /**
         * @brief Propone el último costo al elegir un repuesto.
         * @param value Repuesto elegido
         */
        partial void OnRepuestoChanged(Repuesto value)
        {
            if (value != null)
                Costo = value.PrecioCompra;
        }

        /**
         * @brief Agrega el repuesto elegido con su cantidad y costo.
         */
        private void Agregar()
        {
            ErrorMessage = null;
            if (Repuesto == null)
            {
                ErrorMessage = "Busca y elige el repuesto que llegó.";
                return;
            }
            int unidades = (int)(Cantidad ?? 0);
            if (unidades <= 0)
            {
                ErrorMessage = "La cantidad debe ser mayor que cero.";
                return;
            }

            var existente = Lineas.FirstOrDefault(l => l.Repuesto.Id == Repuesto.Id);
            var linea = new LineaEntrada { Repuesto = Repuesto, Cantidad = unidades + (existente?.Cantidad ?? 0), Costo = Costo ?? 0 };
            if (existente != null)
                Lineas[Lineas.IndexOf(existente)] = linea;
            else
                Lineas.Add(linea);

            Repuesto = null;
            Cantidad = 1;
            Costo = null;
            ActualizarTotal();
        }

        /**
         * @brief Recalcula el total y habilita Guardar.
         */
        private void ActualizarTotal()
        {
            Total = Lineas.Sum(l => l.Subtotal);
            GuardarCommand.NotifyCanExecuteChanged();
        }

        /**
         * @brief Abre el formulario de proveedor y deja elegido el nuevo.
         */
        private async Task NuevoProveedor()
        {
            var (ventana, vm) = _vistas.Ventana<AgregarEditarProveedorView, AgregarEditarProveedorViewModel>();
            if (!await Dialogs.ShowForm(ventana)) return;
            Proveedores = _proveedores.ParaSeleccion().ToList();
            ElegirProveedor(vm.Form.Id);
        }

        /**
         * @brief Registra la entrada, suma las unidades y cierra.
         */
        private void Guardar()
        {
            ErrorMessage = null;
            try
            {
                var lineas = Lineas.Select(l => new LineaDocumento { RepuestoId = l.Repuesto.Id, Cantidad = l.Cantidad, PrecioUnitario = l.Costo });
                int numero = _entradas.Registrar(Proveedor?.Id ?? 0, Factura, lineas);
                _avisos.Exito($"Entrada N° {numero}: {Lineas.Sum(l => l.Cantidad)} unidades sumadas al inventario.");
                _window.Close(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
    }
}
