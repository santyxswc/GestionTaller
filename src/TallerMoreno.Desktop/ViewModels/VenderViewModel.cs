/**
 * @file VenderViewModel.cs
 * @brief Lógica de la pantalla de venta.
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
     * @brief Repuesto agregado a la venta, con su cantidad.
     */
    public partial class LineaCarrito : ObservableObject
    {
        /** Repuesto con su precio y stock. */
        public Repuesto Repuesto { get; init; }

        /** Unidades que se llevan. */
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Subtotal))]
        private int cantidad = 1;

        /** Precio por cantidad. */
        public decimal Subtotal => Repuesto.PrecioVenta * Cantidad;
    }

    /**
     * @brief Arma la venta con toques: buscar, tocar el repuesto, elegir el pago y cobrar.
     */
    public partial class VenderViewModel : ObservableObject
    {
        /** Opción de chip que muestra todas las categorías. */
        public const string TodasLasCategorias = "Todas";

        /** Reglas de negocio de ventas. */
        private readonly VentaService _ventas;
        /** Inventario. */
        private readonly RepuestoService _repuestos;
        /** Clientes. */
        private readonly ClienteService _clientes;
        /** Creación de ventanas. */
        private readonly FabricaVistas _vistas;

        /** Avisos breves. */
        public Avisos Avisos { get; }

        /** Chips de categoría. */
        public List<string> Categorias { get; } = [TodasLasCategorias, .. Repuesto.Categorias];
        /** Métodos de pago. */
        public string[] MetodosPago => VentaService.MetodosPago;
        /** Repuestos que se muestran. */
        public ObservableCollection<Repuesto> Repuestos { get; } = new();
        /** Repuestos que se van a vender. */
        public ObservableCollection<LineaCarrito> Carrito { get; } = new();
        /** Clientes registrados. */
        [ObservableProperty]
        private List<Seleccion> clientes;

        /** Texto del buscador; filtra mientras se escribe. */
        [ObservableProperty]
        private string busqueda;

        /** Categoría elegida. */
        [ObservableProperty]
        private string categoria = TodasLasCategorias;

        /** Cliente de la venta; null es cliente de mostrador. */
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TieneCliente))]
        private Seleccion cliente;

        /** Método de pago elegido. */
        [ObservableProperty]
        private string metodoPago = VentaService.MetodosPago[0];

        /** Total a cobrar. */
        [ObservableProperty]
        private decimal total;

        /** Unidades en la venta. */
        [ObservableProperty]
        private int unidades;

        /** Indica si hay un cliente elegido. */
        public bool TieneCliente => Cliente != null;
        /** Indica si la venta tiene repuestos. */
        public bool HayLineas => Carrito.Count > 0;
        /** Indica si no hay repuestos que mostrar. */
        public bool SinResultados => Repuestos.Count == 0;

        /** Filtro de clientes sin tildes ni mayúsculas. */
        public AutoCompleteFilterPredicate<object> FiltroClientes { get; } =
            (texto, item) => item is Seleccion s && Texto.Coincide(texto, s.Nombre);

        /** Agrega una unidad de un repuesto. */
        public IRelayCommand<Repuesto> AgregarCommand { get; }
        /** Suma una unidad a una línea. */
        public IRelayCommand<LineaCarrito> SumarCommand { get; }
        /** Resta una unidad a una línea (y la quita si llega a cero). */
        public IRelayCommand<LineaCarrito> RestarCommand { get; }
        /** Quita una línea. */
        public IRelayCommand<LineaCarrito> QuitarCommand { get; }
        /** Vacía la venta. */
        public IRelayCommand VaciarCommand { get; }
        /** Registra un cliente nuevo y lo deja elegido. */
        public IAsyncRelayCommand NuevoClienteCommand { get; }
        /** Vuelve a cliente de mostrador. */
        public IRelayCommand QuitarClienteCommand { get; }
        /** Registra la venta. */
        public IRelayCommand CobrarCommand { get; }

        /**
         * @brief Crea los comandos y carga repuestos y clientes.
         * @param ventas Reglas de negocio de ventas
         * @param repuestos Inventario
         * @param clientes Clientes
         * @param vistas Creación de ventanas
         * @param avisos Avisos breves
         */
        public VenderViewModel(VentaService ventas, RepuestoService repuestos, ClienteService clientes, FabricaVistas vistas, Avisos avisos)
        {
            _ventas = ventas;
            _repuestos = repuestos;
            _clientes = clientes;
            _vistas = vistas;
            Avisos = avisos;

            AgregarCommand = new RelayCommand<Repuesto>(Agregar);
            SumarCommand = new RelayCommand<LineaCarrito>(l => Agregar(l?.Repuesto));
            RestarCommand = new RelayCommand<LineaCarrito>(Restar);
            QuitarCommand = new RelayCommand<LineaCarrito>(Quitar);
            VaciarCommand = new RelayCommand(Vaciar);
            NuevoClienteCommand = new AsyncRelayCommand(NuevoCliente);
            QuitarClienteCommand = new RelayCommand(() => Cliente = null);
            CobrarCommand = new RelayCommand(Cobrar, () => HayLineas);

            Carrito.CollectionChanged += (_, _) => ActualizarTotales();
            CargarClientes();
            Filtrar();
        }

        /** @brief Filtra al escribir. */
        partial void OnBusquedaChanged(string value) => Filtrar();

        /** @brief Filtra al cambiar de categoría. */
        partial void OnCategoriaChanged(string value) => Filtrar();

        /**
         * @brief Vuelve a leer el inventario con el texto y la categoría elegidos.
         */
        private void Filtrar()
        {
            try
            {
                string cat = Categoria == TodasLasCategorias ? null : Categoria;
                Repuestos.Clear();
                foreach (var r in _repuestos.Buscar(Busqueda, cat))
                    Repuestos.Add(r);
                OnPropertyChanged(nameof(SinResultados));
            }
            catch (Exception ex)
            {
                Avisos.Error(ex.Message);
            }
        }

        /**
         * @brief Carga la lista de clientes.
         */
        private void CargarClientes()
        {
            try
            {
                Clientes = _clientes.ParaSeleccion().ToList();
            }
            catch (Exception ex)
            {
                Avisos.Error(ex.Message);
            }
        }

        /**
         * @brief Agrega una unidad sin pasar del stock disponible.
         * @param r Repuesto tocado
         */
        private void Agregar(Repuesto r)
        {
            if (r == null) return;
            var linea = Carrito.FirstOrDefault(l => l.Repuesto.Id == r.Id);
            int enVenta = linea?.Cantidad ?? 0;
            if (enVenta + 1 > r.Stock)
            {
                Avisos.Error(r.Agotado ? $"{r.Nombre} está agotado." : $"Solo hay {r.Stock} unidades de {r.Nombre}.");
                return;
            }

            if (linea == null)
                Carrito.Add(new LineaCarrito { Repuesto = r });
            else
                linea.Cantidad++;
            ActualizarTotales();
        }

        /**
         * @brief Resta una unidad; si queda en cero, quita la línea.
         * @param linea Línea
         */
        private void Restar(LineaCarrito linea)
        {
            if (linea == null) return;
            if (linea.Cantidad <= 1)
                Carrito.Remove(linea);
            else
                linea.Cantidad--;
            ActualizarTotales();
        }

        /**
         * @brief Quita una línea.
         * @param linea Línea
         */
        private void Quitar(LineaCarrito linea)
        {
            if (linea != null)
                Carrito.Remove(linea);
        }

        /**
         * @brief Deja la venta vacía y con cliente de mostrador.
         */
        private void Vaciar()
        {
            Carrito.Clear();
            Cliente = null;
            MetodoPago = MetodosPago[0];
        }

        /**
         * @brief Recalcula el total y habilita Cobrar.
         */
        private void ActualizarTotales()
        {
            Total = Carrito.Sum(l => l.Subtotal);
            Unidades = Carrito.Sum(l => l.Cantidad);
            OnPropertyChanged(nameof(HayLineas));
            CobrarCommand.NotifyCanExecuteChanged();
        }

        /**
         * @brief Abre el formulario de cliente y deja elegido el cliente nuevo.
         */
        private async Task NuevoCliente()
        {
            var (ventana, vm) = _vistas.Ventana<AgregarEditarClienteView, AgregarEditarClienteViewModel>();
            if (!await Dialogs.ShowForm(ventana)) return;
            CargarClientes();
            Cliente = Clientes.FirstOrDefault(c => c.Id == vm.Form.Id);
        }

        /**
         * @brief Registra la venta, avisa el total y deja todo listo para la siguiente.
         */
        private void Cobrar()
        {
            try
            {
                var lineas = Carrito.Select(l => new LineaDocumento { RepuestoId = l.Repuesto.Id, Cantidad = l.Cantidad });
                int numero = _ventas.Vender(Cliente?.Id, MetodoPago, lineas);
                Avisos.Exito($"Venta N° {numero} registrada · {Formato.Moneda(Total)} en {MetodoPago.ToLower()}");
                Vaciar();
                Filtrar();
            }
            catch (Exception ex)
            {
                Avisos.Error(ex.Message);
                Filtrar();
            }
        }
    }
}
