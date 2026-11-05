/**
 * @file DevolucionViewModel.cs
 * @brief Lógica del registro de una devolución o garantía.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TallerMoreno.Core;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Services;
using TallerMoreno.Desktop.Services;

namespace TallerMoreno.Desktop.ViewModels
{
    /**
     * @brief Registra lo que devolvió un cliente, lo que se le devolvió a un proveedor, o lo que respondió
     * el proveedor a una garantía pendiente.
     */
    public partial class DevolucionViewModel : ObservableObject
    {
        /** Qué se hace con la ventana. */
        private enum Modo
        {
            /** Un cliente devuelve algo de una venta. */
            Cliente,
            /** Se le devuelve algo a un proveedor. */
            Proveedor,
            /** El proveedor respondió una garantía pendiente. */
            Resolver
        }

        /** Ventana del formulario. */
        private readonly Window _window;
        /** Devoluciones. */
        private readonly DevolucionService _devoluciones;
        /** Modo de la ventana. */
        private readonly Modo _modo;
        /** Venta (modo cliente) o proveedor (modo proveedor) o devolución (modo resolver). */
        private readonly int _documentoId;
        /** Textos de las opciones de qué se hizo y su código. */
        private readonly Dictionary<string, string> _resultados;

        /** Título. */
        public string Titulo { get; }
        /** Texto bajo el título. */
        public string Subtitulo { get; }
        /** Indica si se elige el repuesto de una venta. */
        public bool EligeDeVenta => _modo == Modo.Cliente;
        /** Indica si se busca el repuesto en todo el inventario. */
        public bool EligeDelInventario => _modo == Modo.Proveedor;
        /** Indica si se piden repuesto, cantidad, motivo e inventario (no al responder una garantía). */
        public bool PideDetalle => _modo != Modo.Resolver;
        /** Repuestos de la venta que todavía se pueden devolver. */
        public List<LineaDocumento> Lineas { get; } = new();
        /** Repuestos del inventario. */
        public List<Repuesto> Repuestos { get; } = new();
        /** Motivos. */
        public string[] Motivos { get; }
        /** Opciones de qué se hizo. */
        public List<string> Resultados { get; }
        /** Texto de la casilla del inventario. */
        public string TextoInventario { get; }
        /** Explicación bajo la casilla del inventario. */
        public string AyudaInventario { get; }
        /** Indica si se puede borrar la devolución (administrador, al responder una garantía). */
        public bool PuedeBorrar { get; }
        /** Pregunta sobre lo que se hizo. */
        public string EtiquetaResultado => _modo == Modo.Cliente ? "¿Qué se le dio al cliente?" : "¿Qué hizo el proveedor?";
        /** Texto del botón de guardar. */
        public string TextoGuardar => _modo == Modo.Resolver ? "Guardar respuesta" : "Guardar devolución";

        /** Filtro de repuestos sin tildes ni mayúsculas. */
        public AutoCompleteFilterPredicate<object> FiltroRepuestos { get; } =
            (texto, item) => item is Repuesto r && Texto.Coincide(texto, r.Nombre, r.Codigo, r.Marca);

        /** Repuesto elegido de la venta. */
        [ObservableProperty]
        private LineaDocumento linea;

        /** Repuesto elegido del inventario. */
        [ObservableProperty]
        private Repuesto repuesto;

        /** Unidades. */
        [ObservableProperty]
        private decimal? cantidad = 1;

        /** Motivo elegido. */
        [ObservableProperty]
        private string motivo;

        /** Qué se hizo, como "Se le cambió por otro". */
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PideValor), nameof(EtiquetaValor), nameof(AyudaResultado))]
        private string resultado;

        /** Plata devuelta o rebajada. */
        [ObservableProperty]
        private decimal? valor;

        /** Si el repuesto entra al inventario (cliente) o sale de él (proveedor). */
        [ObservableProperty]
        private bool mueveInventario;

        /** Notas. */
        [ObservableProperty]
        private string notas;

        /** Error de validación o de guardado. */
        [ObservableProperty]
        private string errorMessage;

        /** Código del resultado elegido. */
        private string Codigo => Resultado != null && _resultados.TryGetValue(Resultado, out var c) ? c : null;
        /** Indica si se pide cuánta plata. */
        public bool PideValor => Codigo is Devolucion.Dinero or Devolucion.Descuento;
        /** Etiqueta del valor. */
        public string EtiquetaValor => Codigo == Devolucion.Descuento ? "Cuánto se rebaja de la deuda" : "Cuánta plata";
        /** Explicación de lo que pasa con lo elegido. */
        public string AyudaResultado => Codigo switch
        {
            Devolucion.Cambio when _modo == Modo.Cliente => "Se saca otro igual del inventario para entregárselo.",
            Devolucion.Cambio => "El repuesto nuevo que te mandó se suma al inventario.",
            Devolucion.Pendiente => "Queda pendiente; cuando el proveedor responda, tócala dos veces en sus entradas y devoluciones.",
            _ => ""
        };

        /** Guarda. */
        public IRelayCommand GuardarCommand { get; }
        /** Borra la devolución (administrador). */
        public IAsyncRelayCommand BorrarCommand { get; }
        /** Cierra sin guardar. */
        public IRelayCommand CancelarCommand { get; }

        /**
         * @brief Crea el formulario.
         * @param window Ventana
         * @param devoluciones Devoluciones
         * @param modo Qué se hace
         * @param documentoId Venta, proveedor o devolución
         * @param titulo Título
         * @param subtitulo Texto bajo el título
         * @param puedeBorrar Si se muestra el botón de borrar
         */
        private DevolucionViewModel(Window window, DevolucionService devoluciones, Modo modo, int documentoId, string titulo, string subtitulo,
                                    bool puedeBorrar = false)
        {
            _window = window;
            _devoluciones = devoluciones;
            _modo = modo;
            _documentoId = documentoId;
            Titulo = titulo;
            Subtitulo = subtitulo;
            PuedeBorrar = puedeBorrar;

            bool cliente = modo == Modo.Cliente;
            Motivos = cliente ? Devolucion.MotivosCliente : Devolucion.MotivosProveedor;
            _resultados = cliente
                ? new() { ["Se le cambió por otro"] = Devolucion.Cambio, ["Se le devolvió la plata"] = Devolucion.Dinero, ["Se le rebajó de lo que debe"] = Devolucion.Descuento }
                : new() { ["Me lo cambió"] = Devolucion.Cambio, ["Me devolvió la plata"] = Devolucion.Dinero, ["Me lo rebajó de la deuda"] = Devolucion.Descuento };
            if (modo == Modo.Proveedor)
                _resultados["Todavía no responde"] = Devolucion.Pendiente;
            Resultados = _resultados.Keys.ToList();
            Resultado = Resultados[0];
            Motivo = Motivos[0];

            TextoInventario = cliente ? "El repuesto que devolvió está bueno y vuelve al inventario" : "Sale de mi inventario";
            AyudaInventario = cliente
                ? "Déjala sin marcar si salió malo: así no se vende otra vez."
                : "Desmárcala si es un repuesto malo que te devolvió un cliente y que no volvió al inventario.";
            MueveInventario = !cliente;

            GuardarCommand = new RelayCommand(Guardar);
            BorrarCommand = new AsyncRelayCommand(Borrar);
            CancelarCommand = new RelayCommand(() => _window.Close(false));
        }

        /**
         * @brief Devolución de un cliente.
         * @param window Ventana
         * @param devoluciones Devoluciones
         * @param venta Venta con sus líneas
         * @return Formulario
         */
        public static DevolucionViewModel DeVenta(Window window, DevolucionService devoluciones, Venta venta)
        {
            var vm = new DevolucionViewModel(window, devoluciones, Modo.Cliente, venta.Id, $"Devolución de la venta N° {venta.Id}",
                                             $"{venta.Cliente} · {venta.Fecha:dd/MM/yyyy}");
            vm.Lineas.AddRange(venta.Items.Where(l => l.PorDevolver > 0));
            vm.Linea = vm.Lineas.FirstOrDefault();
            return vm;
        }

        /**
         * @brief Devolución a un proveedor.
         * @param window Ventana
         * @param devoluciones Devoluciones
         * @param proveedor Proveedor
         * @param repuestos Repuestos del inventario
         * @return Formulario
         */
        public static DevolucionViewModel AProveedor(Window window, DevolucionService devoluciones, Proveedor proveedor, IEnumerable<Repuesto> repuestos)
        {
            var vm = new DevolucionViewModel(window, devoluciones, Modo.Proveedor, proveedor.Id, $"Devolver a {proveedor.Nombre}",
                                             "Por garantía, porque llegó dañado o porque no era lo que pediste.");
            vm.Repuestos.AddRange(repuestos);
            return vm;
        }

        /**
         * @brief Respuesta del proveedor a una garantía pendiente.
         * @param window Ventana
         * @param devoluciones Devoluciones
         * @param d Devolución pendiente
         * @param proveedor Nombre del proveedor
         * @param esAdmin Si puede borrarla
         * @return Formulario
         */
        public static DevolucionViewModel Resolver(Window window, DevolucionService devoluciones, Devolucion d, string proveedor, bool esAdmin) =>
            new(window, devoluciones, Modo.Resolver, d.Id, $"¿Qué respondió {proveedor}?",
                $"{d.RepuestoNombre} × {d.Cantidad} · {d.Motivo} · enviado el {d.Fecha:dd/MM/yyyy}", esAdmin);

        /** @brief Propone el valor de la línea elegida. */
        partial void OnLineaChanged(LineaDocumento value) => ProponerValor();
        /** @brief Propone el valor del repuesto elegido. */
        partial void OnRepuestoChanged(Repuesto value) => ProponerValor();
        /** @brief Propone el valor de las unidades. */
        partial void OnCantidadChanged(decimal? value) => ProponerValor();

        /**
         * @brief Un repuesto que el cliente devuelve por garantía salió malo y no vuelve al inventario.
         * @param value Motivo
         */
        partial void OnMotivoChanged(string value)
        {
            if (_modo == Modo.Cliente)
                MueveInventario = value != Devolucion.MotivoGarantia;
        }

        /**
         * @brief Pone como valor lo que se pagó por esas unidades.
         */
        private void ProponerValor()
        {
            decimal precio = Linea?.PrecioUnitario ?? Repuesto?.PrecioCompra ?? 0;
            if (precio > 0)
                Valor = precio * Math.Max(1, Cantidad ?? 1);
        }

        /**
         * @brief Guarda y cierra; si algo falla, muestra el mensaje.
         */
        private void Guardar()
        {
            ErrorMessage = null;
            try
            {
                int unidades = (int)Math.Round(Cantidad ?? 0);
                decimal plata = Valor ?? 0;
                switch (_modo)
                {
                    case Modo.Cliente:
                        if (Linea == null)
                            throw new ArgumentException("Elige el repuesto que devolvió.");
                        _devoluciones.DevolverDeCliente(_documentoId, Linea.RepuestoId, unidades, Motivo, Codigo, plata, MueveInventario, Notas);
                        break;
                    case Modo.Proveedor:
                        if (Repuesto == null)
                            throw new ArgumentException("Elige el repuesto que le devolviste.");
                        _devoluciones.DevolverAProveedor(_documentoId, Repuesto.Id, unidades, Motivo, Codigo, plata, MueveInventario, Notas);
                        break;
                    default:
                        _devoluciones.Resolver(_documentoId, Codigo, plata, Notas);
                        break;
                }
                _window.Close(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = Errores.Mensaje(ex);
            }
        }

        /**
         * @brief Borra la garantía pendiente, previa confirmación, y devuelve las unidades al inventario.
         */
        private async Task Borrar()
        {
            if (!await Dialogs.Confirm("¿Borrar esta devolución? Si las unidades salieron del inventario, vuelven a entrar.", "Borrar devolución", "Borrar"))
                return;
            try
            {
                _devoluciones.EliminarAProveedor(_documentoId);
                _window.Close(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = Errores.Mensaje(ex);
            }
        }
    }
}
