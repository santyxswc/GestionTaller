/**
 * @file InventarioViewModel.cs
 * @brief Lógica del inventario.
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
     * @brief Inventario en tarjetas con foto: buscar, ver lo que hay que pedir, recibir mercancía y descargar en Excel.
     */
    public partial class InventarioViewModel : ObservableObject
    {
        /** Inventario. */
        private readonly RepuestoService _repuestos;
        /** Excel. */
        private readonly ExportacionService _exportacion;
        /** Usuario de la sesión. */
        private readonly ISesionUsuario _sesion;
        /** Creación de ventanas. */
        private readonly FabricaVistas _vistas;
        /** Avisos breves. */
        private readonly Avisos _avisos;

        /** Chips de categoría. */
        public List<string> Categorias { get; } = [VenderViewModel.TodasLasCategorias, .. Repuesto.Categorias];
        /** Repuestos que se muestran. */
        public ObservableCollection<Repuesto> Repuestos { get; } = new();
        /** Indica si se muestran las acciones de administrador. */
        public bool EsAdmin => _sesion.EsAdmin;

        /** Texto del buscador; filtra mientras se escribe. */
        [ObservableProperty]
        private string busqueda;

        /** Categoría elegida. */
        [ObservableProperty]
        private string categoria = VenderViewModel.TodasLasCategorias;

        /** true para ver solo lo que hay que pedir. */
        [ObservableProperty]
        private bool soloPorPedir;

        /** Resumen bajo el título. */
        [ObservableProperty]
        private string resumen;

        /** Indica si no hay repuestos que mostrar. */
        public bool SinResultados => Repuestos.Count == 0;

        /** Abre el detalle de un repuesto. */
        public IAsyncRelayCommand<Repuesto> AbrirCommand { get; }
        /** Registra un repuesto (administrador). */
        public IAsyncRelayCommand NuevoCommand { get; }
        /** Registra mercancía que llegó. */
        public IAsyncRelayCommand LlegoMercanciaCommand { get; }
        /** Descarga el inventario en Excel. */
        public IAsyncRelayCommand ExcelCommand { get; }

        /**
         * @brief Crea los comandos y carga el inventario.
         * @param repuestos Inventario
         * @param exportacion Excel
         * @param sesion Usuario de la sesión
         * @param vistas Creación de ventanas
         * @param avisos Avisos breves
         */
        public InventarioViewModel(RepuestoService repuestos, ExportacionService exportacion, ISesionUsuario sesion, FabricaVistas vistas, Avisos avisos)
        {
            _repuestos = repuestos;
            _exportacion = exportacion;
            _sesion = sesion;
            _vistas = vistas;
            _avisos = avisos;

            AbrirCommand = new AsyncRelayCommand<Repuesto>(Abrir);
            NuevoCommand = new AsyncRelayCommand(Nuevo);
            LlegoMercanciaCommand = new AsyncRelayCommand(LlegoMercancia);
            ExcelCommand = new AsyncRelayCommand(Excel);
            Cargar();
        }

        /** @brief Filtra al escribir. */
        partial void OnBusquedaChanged(string value) => Cargar();
        /** @brief Filtra al cambiar de categoría. */
        partial void OnCategoriaChanged(string value) => Cargar();
        /** @brief Filtra al activar o quitar "Por pedir". */
        partial void OnSoloPorPedirChanged(bool value) => Cargar();

        /**
         * @brief Vuelve a leer el inventario con los filtros elegidos y arma el resumen.
         */
        private void Cargar()
        {
            try
            {
                string cat = Categoria == VenderViewModel.TodasLasCategorias ? null : Categoria;
                Repuestos.Clear();
                foreach (var r in _repuestos.Buscar(Busqueda, cat, SoloPorPedir))
                    Repuestos.Add(r);
                OnPropertyChanged(nameof(SinResultados));

                var todos = _repuestos.Buscar().ToList();
                string texto = $"{todos.Count} repuestos · {todos.Sum(r => r.Stock)} unidades · {todos.Count(r => r.StockBajo)} por pedir";
                if (EsAdmin)
                    texto += $" · {Formato.Moneda(todos.Sum(r => r.ValorInventario))} en bodega";
                Resumen = texto;
            }
            catch (Exception ex)
            {
                _avisos.Error(Errores.Mensaje(ex));
            }
        }

        /**
         * @brief Abre el detalle y recarga al cerrar (pudo cambiar).
         * @param r Repuesto
         */
        private async Task Abrir(Repuesto r)
        {
            if (r == null) return;
            var (ventana, _) = _vistas.Ventana<RepuestoDetalleView, RepuestoDetalleViewModel>(r);
            await Dialogs.ShowForm(ventana);
            Cargar();
        }

        /**
         * @brief Abre el formulario de repuesto nuevo.
         */
        private async Task Nuevo()
        {
            var (ventana, vm) = _vistas.Ventana<AgregarEditarRepuestoView, AgregarEditarRepuestoViewModel>();
            if (await Dialogs.ShowForm(ventana))
            {
                _avisos.Exito($"{vm.Form.Nombre} quedó en el inventario.");
                Cargar();
            }
        }

        /**
         * @brief Abre el registro de mercancía recibida.
         */
        private async Task LlegoMercancia()
        {
            var (ventana, _) = _vistas.Ventana<EntradaView, EntradaViewModel>();
            if (await Dialogs.ShowForm(ventana))
                Cargar();
        }

        /**
         * @brief Pregunta dónde guardar, genera el Excel y lo abre.
         */
        private async Task Excel()
        {
            string ruta = await Archivos.GuardarComo(ExportacionService.NombreArchivo("Inventario"), Archivos.Excel);
            if (ruta == null) return;
            try
            {
                int n = await Task.Run(() => _exportacion.ExportarInventario(ruta));
                _avisos.Exito($"Inventario descargado: {n} repuestos.");
                Archivos.Abrir(ruta);
            }
            catch (IOException)
            {
                await Dialogs.Error("No se pudo guardar el archivo. Si está abierto en Excel, ciérralo e inténtalo de nuevo.");
            }
            catch (Exception ex)
            {
                await Dialogs.Error(Errores.Mensaje(ex));
            }
        }
    }
}
