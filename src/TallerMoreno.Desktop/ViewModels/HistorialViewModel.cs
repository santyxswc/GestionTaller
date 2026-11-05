/**
 * @file HistorialViewModel.cs
 * @brief Lógica del historial de ventas.
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
     * @brief Ventas por periodo con sus totales; el administrador además ve la ganancia, lo más vendido,
     * puede anular ventas y descargarlas en Excel.
     */
    public partial class HistorialViewModel : ObservableObject
    {
        /** Ventas. */
        private readonly VentaService _ventas;
        /** Reportes (administrador). */
        private readonly ReporteService _reportes;
        /** Excel. */
        private readonly ExportacionService _exportacion;
        /** Devoluciones, para registrarlas desde el detalle de una venta. */
        private readonly DevolucionService _devoluciones;
        /** Usuario de la sesión. */
        private readonly ISesionUsuario _sesion;
        /** Avisos breves. */
        private readonly Avisos _avisos;

        /** Chips de periodo. */
        public string[] Periodos => Core.Models.Periodo.Todos;
        /** Ventas del periodo. */
        public ObservableCollection<Venta> Ventas { get; } = new();
        /** Cifras del periodo. */
        public ObservableCollection<Dato> Cifras { get; } = new();
        /** Repuestos más vendidos del periodo (administrador). */
        public ObservableCollection<ItemRanking> MasVendidos { get; } = new();
        /** Indica si se muestran las opciones de administrador. */
        public bool EsAdmin => _sesion.EsAdmin;
        /** Indica si no hay ventas en el periodo. */
        public bool SinResultados => Ventas.Count == 0;

        /** Periodo elegido. */
        [ObservableProperty]
        private string periodo = Core.Models.Periodo.Hoy;

        /** Texto del buscador; filtra mientras se escribe. */
        [ObservableProperty]
        private string busqueda;

        /** Venta seleccionada en la tabla. */
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(VerCommand), nameof(AnularCommand))]
        private Venta seleccionada;

        /** Abre el detalle de la venta seleccionada. */
        public IAsyncRelayCommand VerCommand { get; }
        /** Anula la venta seleccionada (administrador). */
        public IAsyncRelayCommand AnularCommand { get; }
        /** Descarga las ventas del periodo en Excel (administrador). */
        public IAsyncRelayCommand ExcelCommand { get; }

        /**
         * @brief Crea los comandos y carga las ventas de hoy.
         * @param ventas Ventas
         * @param reportes Reportes
         * @param exportacion Excel
         * @param devoluciones Devoluciones
         * @param sesion Usuario de la sesión
         * @param avisos Avisos breves
         */
        public HistorialViewModel(VentaService ventas, ReporteService reportes, ExportacionService exportacion,
                                  DevolucionService devoluciones, ISesionUsuario sesion, Avisos avisos)
        {
            _ventas = ventas;
            _reportes = reportes;
            _exportacion = exportacion;
            _devoluciones = devoluciones;
            _sesion = sesion;
            _avisos = avisos;

            VerCommand = new AsyncRelayCommand(Ver, () => Seleccionada != null);
            AnularCommand = new AsyncRelayCommand(Anular, () => Seleccionada is { Anulada: false, Devoluciones: 0 });
            ExcelCommand = new AsyncRelayCommand(Excel);
            Cargar();
        }

        /** @brief Recarga al cambiar de periodo. */
        partial void OnPeriodoChanged(string value) => Cargar();
        /** @brief Filtra al escribir. */
        partial void OnBusquedaChanged(string value) => Cargar();

        /**
         * @brief Lee las ventas del periodo y calcula las cifras.
         */
        private void Cargar()
        {
            try
            {
                string p = Periodo ?? Core.Models.Periodo.Hoy;
                Ventas.Clear();
                foreach (var v in _ventas.Buscar(p, Busqueda))
                    Ventas.Add(v);
                OnPropertyChanged(nameof(SinResultados));

                var validas = Ventas.Where(v => !v.Anulada).ToList();
                Cifras.Clear();
                Cifras.Add(new Dato("Vendido", Formato.Moneda(validas.Sum(v => v.Total))));
                Cifras.Add(new Dato("Ventas", Formato.Numero(validas.Count)));
                Cifras.Add(new Dato("Repuestos", Formato.Numero(validas.Sum(v => v.Unidades))));
                if (EsAdmin)
                    Cifras.Add(new Dato("Ganancia", Formato.Moneda(validas.Sum(v => v.Ganancia))));

                MasVendidos.Clear();
                if (EsAdmin)
                    foreach (var item in _reportes.TopRepuestos(p).Take(5))
                        MasVendidos.Add(item);
            }
            catch (Exception ex)
            {
                _avisos.Error(Errores.Mensaje(ex));
            }
        }

        /**
         * @brief Abre el detalle de la venta seleccionada; desde ahí se puede registrar una devolución.
         */
        private async Task Ver()
        {
            int id = Seleccionada.Id;
            if (await DocumentoView.MostrarVenta(_ventas, _devoluciones, id))
            {
                _avisos.Exito($"Devolución de la venta N° {id} guardada.");
                Cargar();
            }
        }

        /**
         * @brief Anula la venta seleccionada, previa confirmación, y devuelve las unidades al inventario.
         */
        private async Task Anular()
        {
            var v = Seleccionada;
            if (!await Dialogs.Confirm($"¿Anular la venta N° {v.Id} por {Formato.Moneda(v.Total)}? Los repuestos vuelven al inventario.",
                                       "Anular venta", "Anular venta"))
                return;
            try
            {
                _ventas.Anular(v.Id);
                _avisos.Exito($"Venta N° {v.Id} anulada. Los repuestos volvieron al inventario.");
                Cargar();
            }
            catch (Exception ex)
            {
                await Dialogs.Error(Errores.Mensaje(ex));
            }
        }

        /**
         * @brief Pregunta dónde guardar, genera el Excel del periodo y lo abre.
         */
        private async Task Excel()
        {
            string p = Periodo ?? Core.Models.Periodo.Hoy;
            string ruta = await Archivos.GuardarComo(ExportacionService.NombreArchivo("Ventas"), Archivos.Excel);
            if (ruta == null) return;
            try
            {
                int n = await Task.Run(() => _exportacion.ExportarVentas(p, ruta));
                _avisos.Exito($"Ventas descargadas: {n}.");
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
