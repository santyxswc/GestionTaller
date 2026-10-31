/**
 * @file ProveedoresViewModel.cs
 * @brief Lógica de la sección de proveedores.
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
     * @brief Proveedores con lo que se les ha comprado: registrar, editar, ver entradas y recibir mercancía.
     */
    public partial class ProveedoresViewModel : ObservableObject
    {
        /** Proveedores. */
        private readonly ProveedorService _proveedores;
        /** Entradas, para abrir el detalle de una. */
        private readonly EntradaService _entradas;
        /** Usuario de la sesión. */
        private readonly ISesionUsuario _sesion;
        /** Creación de ventanas. */
        private readonly FabricaVistas _vistas;
        /** Avisos breves. */
        private readonly Avisos _avisos;

        /** Proveedores que se muestran. */
        public ObservableCollection<Proveedor> Proveedores { get; } = new();
        /** Indica si se muestran las acciones de administrador. */
        public bool EsAdmin => _sesion.EsAdmin;
        /** Indica si no hay proveedores que mostrar. */
        public bool SinResultados => Proveedores.Count == 0;

        /** Texto del buscador; filtra mientras se escribe. */
        [ObservableProperty]
        private string busqueda;

        /** Proveedor seleccionado en la tabla. */
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EditarCommand), nameof(EntradasCommand), nameof(EliminarCommand))]
        private Proveedor seleccionado;

        /** Resumen bajo el título. */
        [ObservableProperty]
        private string resumen;

        /** Registra un proveedor. */
        public IAsyncRelayCommand NuevoCommand { get; }
        /** Edita el proveedor seleccionado. */
        public IAsyncRelayCommand EditarCommand { get; }
        /** Muestra las entradas del proveedor seleccionado. */
        public IAsyncRelayCommand EntradasCommand { get; }
        /** Elimina el proveedor seleccionado (administrador). */
        public IAsyncRelayCommand EliminarCommand { get; }
        /** Registra mercancía que llegó, con el proveedor seleccionado ya elegido. */
        public IAsyncRelayCommand LlegoMercanciaCommand { get; }

        /**
         * @brief Crea los comandos y carga los proveedores.
         * @param proveedores Proveedores
         * @param entradas Entradas de mercancía
         * @param sesion Usuario de la sesión
         * @param vistas Creación de ventanas
         * @param avisos Avisos breves
         */
        public ProveedoresViewModel(ProveedorService proveedores, EntradaService entradas, ISesionUsuario sesion,
                                    FabricaVistas vistas, Avisos avisos)
        {
            _proveedores = proveedores;
            _entradas = entradas;
            _sesion = sesion;
            _vistas = vistas;
            _avisos = avisos;

            bool haySeleccion() => Seleccionado != null;
            NuevoCommand = new AsyncRelayCommand(Nuevo);
            EditarCommand = new AsyncRelayCommand(Editar, haySeleccion);
            EntradasCommand = new AsyncRelayCommand(VerEntradas, haySeleccion);
            EliminarCommand = new AsyncRelayCommand(Eliminar, haySeleccion);
            LlegoMercanciaCommand = new AsyncRelayCommand(LlegoMercancia);
            Cargar();
        }

        /** @brief Filtra al escribir. */
        partial void OnBusquedaChanged(string value) => Cargar();

        /**
         * @brief Vuelve a leer los proveedores y arma el resumen.
         * @param seleccionarId Proveedor que queda seleccionado, si sigue en la lista
         */
        private void Cargar(int seleccionarId = 0)
        {
            try
            {
                Proveedores.Clear();
                foreach (var p in _proveedores.Buscar(Busqueda))
                    Proveedores.Add(p);
                Seleccionado = Proveedores.FirstOrDefault(p => p.Id == seleccionarId);
                OnPropertyChanged(nameof(SinResultados));

                var todos = _proveedores.Buscar().ToList();
                Resumen = $"{todos.Count} proveedores · {Formato.Moneda(todos.Sum(p => p.TotalComprado))} en mercancía recibida";
            }
            catch (Exception ex)
            {
                _avisos.Error(ex.Message);
            }
        }

        /**
         * @brief Abre el formulario de proveedor nuevo.
         */
        private async Task Nuevo()
        {
            var (ventana, vm) = _vistas.Ventana<AgregarEditarProveedorView, AgregarEditarProveedorViewModel>();
            if (!await Dialogs.ShowForm(ventana)) return;
            _avisos.Exito($"{vm.Form.Nombre} quedó registrado.");
            Cargar(vm.Form.Id);
        }

        /**
         * @brief Abre el formulario con los datos del proveedor seleccionado.
         */
        private async Task Editar()
        {
            var (ventana, vm) = _vistas.Ventana<AgregarEditarProveedorView, AgregarEditarProveedorViewModel>();
            vm.LoadFromModel(Seleccionado);
            if (!await Dialogs.ShowForm(ventana)) return;
            _avisos.Exito("Cambios guardados.");
            Cargar(vm.Form.Id);
        }

        /**
         * @brief Muestra las entradas del proveedor; al tocar una se abre su detalle.
         */
        private async Task VerEntradas()
        {
            var p = Seleccionado;
            var entradas = _proveedores.Entradas(p.Id).ToList();

            string contacto = string.Join(" · ", new[] { p.Contacto, p.Telefono, p.Correo }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var ventana = new ListaDocumentosView();
            ventana.DataContext = new ListaDocumentosViewModel(ventana, AbrirEntrada)
            {
                Titulo = p.Nombre,
                Subtitulo = string.IsNullOrEmpty(contacto) ? "Mercancía recibida" : contacto,
                Cifras =
                [
                    new Dato("Total comprado", Formato.Moneda(entradas.Sum(e => e.Total))),
                    new Dato("Entradas", Formato.Numero(entradas.Count)),
                    new Dato("Última entrada", entradas.Count == 0 ? "—" : entradas.Max(e => e.Fecha).ToString("d MMM yyyy", Formato.Cultura))
                ],
                Filas = entradas.Select(e => new FilaDocumento
                {
                    Id = e.Id,
                    Fecha = e.Fecha,
                    Descripcion = string.IsNullOrEmpty(e.FacturaProveedor)
                        ? $"Recibió {e.UsuarioNombre}"
                        : $"Factura {e.FacturaProveedor} · recibió {e.UsuarioNombre}",
                    Unidades = e.Unidades,
                    Total = e.Total,
                    Estado = ""
                }).ToList(),
                TextoVacio = "Todavía no ha llegado mercancía de este proveedor."
            };
            await Dialogs.ShowForm(ventana);
        }

        /**
         * @brief Abre el detalle de una entrada.
         * @param id Entrada
         */
        private async Task AbrirEntrada(int id)
        {
            var entrada = _entradas.ObtenerDetalle(id);
            if (entrada != null)
                await DocumentoView.Mostrar(entrada);
        }

        /**
         * @brief Abre el registro de mercancía con el proveedor seleccionado.
         */
        private async Task LlegoMercancia()
        {
            var (ventana, vm) = _vistas.Ventana<EntradaView, EntradaViewModel>();
            int id = Seleccionado?.Id ?? 0;
            if (id > 0)
                vm.ElegirProveedor(id);
            if (await Dialogs.ShowForm(ventana))
                Cargar(id);
        }

        /**
         * @brief Elimina el proveedor seleccionado, previa confirmación.
         */
        private async Task Eliminar()
        {
            var p = Seleccionado;
            if (!await Dialogs.Confirm($"¿Eliminar a {p.Nombre}? Esto no se puede deshacer.", "Eliminar proveedor", "Eliminar"))
                return;
            try
            {
                _proveedores.Eliminar(p.Id);
                _avisos.Exito($"{p.Nombre} fue eliminado.");
                Cargar();
            }
            catch (Exception ex)
            {
                await Dialogs.Error(ex.Message);
            }
        }
    }
}
