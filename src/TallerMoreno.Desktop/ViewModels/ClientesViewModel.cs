/**
 * @file ClientesViewModel.cs
 * @brief Lógica de la sección de clientes.
 * @author Santiago Caicedo
 */
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TallerMoreno.Core;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Services;
using TallerMoreno.Core.Sesion;
using TallerMoreno.Desktop.Services;
using TallerMoreno.Desktop.Views;

namespace TallerMoreno.Desktop.ViewModels
{
    /**
     * @brief Clientes con lo que ha comprado cada uno: buscar, registrar, editar y ver sus compras.
     */
    public partial class ClientesViewModel : ObservableObject
    {
        /** Clientes. */
        private readonly ClienteService _clientes;
        /** Ventas, para abrir el detalle de una compra. */
        private readonly VentaService _ventas;
        /** Excel. */
        private readonly ExportacionService _exportacion;
        /** Usuario de la sesión. */
        private readonly ISesionUsuario _sesion;
        /** Creación de ventanas. */
        private readonly FabricaVistas _vistas;
        /** Avisos breves. */
        private readonly Avisos _avisos;

        /** Clientes que se muestran, del que más compra al que menos. */
        public ObservableCollection<Cliente> Clientes { get; } = new();
        /** Indica si se muestran las acciones de administrador. */
        public bool EsAdmin => _sesion.EsAdmin;
        /** Indica si no hay clientes que mostrar. */
        public bool SinResultados => Clientes.Count == 0;

        /** Texto del buscador; filtra mientras se escribe. */
        [ObservableProperty]
        private string busqueda;

        /** Cliente seleccionado en la tabla. */
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EditarCommand), nameof(ComprasCommand), nameof(EliminarCommand))]
        private Cliente seleccionado;

        /** Resumen bajo el título. */
        [ObservableProperty]
        private string resumen;

        /** Registra un cliente. */
        public IAsyncRelayCommand NuevoCommand { get; }
        /** Edita el cliente seleccionado. */
        public IAsyncRelayCommand EditarCommand { get; }
        /** Muestra las compras del cliente seleccionado. */
        public IAsyncRelayCommand ComprasCommand { get; }
        /** Elimina el cliente seleccionado (administrador). */
        public IAsyncRelayCommand EliminarCommand { get; }
        /** Descarga los clientes en Excel. */
        public IAsyncRelayCommand ExcelCommand { get; }

        /**
         * @brief Crea los comandos y carga los clientes.
         * @param clientes Clientes
         * @param ventas Ventas
         * @param exportacion Excel
         * @param sesion Usuario de la sesión
         * @param vistas Creación de ventanas
         * @param avisos Avisos breves
         */
        public ClientesViewModel(ClienteService clientes, VentaService ventas, ExportacionService exportacion,
                                 ISesionUsuario sesion, FabricaVistas vistas, Avisos avisos)
        {
            _clientes = clientes;
            _ventas = ventas;
            _exportacion = exportacion;
            _sesion = sesion;
            _vistas = vistas;
            _avisos = avisos;

            bool haySeleccion() => Seleccionado != null;
            NuevoCommand = new AsyncRelayCommand(Nuevo);
            EditarCommand = new AsyncRelayCommand(Editar, haySeleccion);
            ComprasCommand = new AsyncRelayCommand(VerCompras, haySeleccion);
            EliminarCommand = new AsyncRelayCommand(Eliminar, haySeleccion);
            ExcelCommand = new AsyncRelayCommand(Excel);
            Cargar();
        }

        /** @brief Filtra al escribir. */
        partial void OnBusquedaChanged(string value) => Cargar();

        /**
         * @brief Vuelve a leer los clientes y arma el resumen.
         * @param seleccionarId Cliente que queda seleccionado, si sigue en la lista
         */
        private void Cargar(int seleccionarId = 0)
        {
            try
            {
                Clientes.Clear();
                foreach (var c in _clientes.Buscar(Busqueda).OrderByDescending(c => c.TotalComprado).ThenBy(c => c.Nombre))
                    Clientes.Add(c);
                Seleccionado = Clientes.FirstOrDefault(c => c.Id == seleccionarId);
                OnPropertyChanged(nameof(SinResultados));

                var todos = _clientes.Buscar().ToList();
                Resumen = $"{todos.Count} clientes · {Formato.Moneda(todos.Sum(c => c.TotalComprado))} en compras";
            }
            catch (Exception ex)
            {
                _avisos.Error(ex.Message);
            }
        }

        /**
         * @brief Abre el formulario de cliente nuevo.
         */
        private async Task Nuevo()
        {
            var (ventana, vm) = _vistas.Ventana<AgregarEditarClienteView, AgregarEditarClienteViewModel>();
            if (!await Dialogs.ShowForm(ventana)) return;
            _avisos.Exito($"{vm.Form.Nombre} quedó registrado.");
            Cargar(vm.Form.Id);
        }

        /**
         * @brief Abre el formulario con los datos del cliente seleccionado.
         */
        private async Task Editar()
        {
            var (ventana, vm) = _vistas.Ventana<AgregarEditarClienteView, AgregarEditarClienteViewModel>();
            vm.LoadFromModel(Seleccionado);
            if (!await Dialogs.ShowForm(ventana)) return;
            _avisos.Exito("Cambios guardados.");
            Cargar(vm.Form.Id);
        }

        /**
         * @brief Muestra las compras del cliente; al tocar una se abre su detalle.
         */
        private async Task VerCompras()
        {
            var c = Seleccionado;
            var compras = _clientes.Compras(c.Id).ToList();
            var validas = compras.Where(v => !v.Anulada).ToList();

            string contacto = string.Join(" · ", new[] { c.Telefono, c.Placa, c.Vehiculo }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var ventana = new ListaDocumentosView();
            ventana.DataContext = new ListaDocumentosViewModel(ventana, AbrirVenta)
            {
                Titulo = c.Nombre,
                Subtitulo = string.IsNullOrEmpty(contacto) ? "Compras del cliente" : contacto,
                Cifras =
                [
                    new Dato("Total comprado", Formato.Moneda(validas.Sum(v => v.Total))),
                    new Dato("Compras", Formato.Numero(validas.Count)),
                    new Dato("Última compra", validas.Count == 0 ? "—" : validas.Max(v => v.Fecha).ToString("d MMM yyyy", Formato.Cultura))
                ],
                Filas = compras.Select(v => new FilaDocumento
                {
                    Id = v.Id,
                    Fecha = v.Fecha,
                    Descripcion = $"{v.MetodoPago} · vendió {v.UsuarioNombre}",
                    Unidades = v.Unidades,
                    Total = v.Total,
                    Estado = v.Estado
                }).ToList(),
                TextoVacio = "Este cliente todavía no tiene compras."
            };
            await Dialogs.ShowForm(ventana);
        }

        /**
         * @brief Abre el detalle de una venta.
         * @param id Venta
         */
        private async Task AbrirVenta(int id)
        {
            var venta = _ventas.ObtenerDetalle(id);
            if (venta != null)
                await DocumentoView.Mostrar(venta);
        }

        /**
         * @brief Elimina el cliente seleccionado, previa confirmación.
         */
        private async Task Eliminar()
        {
            var c = Seleccionado;
            if (!await Dialogs.Confirm($"¿Eliminar a {c.Nombre}? Esto no se puede deshacer.", "Eliminar cliente", "Eliminar"))
                return;
            try
            {
                _clientes.Eliminar(c.Id);
                _avisos.Exito($"{c.Nombre} fue eliminado.");
                Cargar();
            }
            catch (Exception ex)
            {
                await Dialogs.Error(ex.Message);
            }
        }

        /**
         * @brief Pregunta dónde guardar, genera el Excel y lo abre.
         */
        private async Task Excel()
        {
            string ruta = await Archivos.GuardarComo(ExportacionService.NombreArchivo("Clientes"), Archivos.Excel);
            if (ruta == null) return;
            try
            {
                int n = await Task.Run(() => _exportacion.ExportarClientes(ruta));
                _avisos.Exito($"Clientes descargados: {n}.");
                Archivos.Abrir(ruta);
            }
            catch (IOException)
            {
                await Dialogs.Error("No se pudo guardar el archivo. Si está abierto en Excel, ciérralo e inténtalo de nuevo.");
            }
            catch (Exception ex)
            {
                await Dialogs.Error(ex.Message);
            }
        }
    }
}
