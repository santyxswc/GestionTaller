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
        [NotifyCanExecuteChangedFor(nameof(EditarCommand), nameof(ComprasCommand), nameof(EliminarCommand), nameof(AbonoCommand))]
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
        /** Registra un abono del cliente seleccionado, si debe. */
        public IAsyncRelayCommand AbonoCommand { get; }

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
            AbonoCommand = new AsyncRelayCommand(AbonarSeleccionado, () => Seleccionado?.TieneDeuda == true);
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
                decimal deuda = todos.Where(c => c.TieneDeuda).Sum(c => c.Deuda);
                Resumen = $"{todos.Count} clientes · {Formato.Moneda(todos.Sum(c => c.TotalComprado))} en compras"
                        + (deuda > 0 ? $" · te deben {Formato.Moneda(deuda)}" : " · nadie te debe");
            }
            catch (Exception ex)
            {
                _avisos.Error(Errores.Mensaje(ex));
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
         * @brief Muestra las compras y abonos del cliente; se vuelve a abrir si abona o se borra un abono.
         */
        private async Task VerCompras()
        {
            int id = Seleccionado.Id;
            while (await MostrarCompras(id)) { }
            Cargar(id);
        }

        /**
         * @brief Abre la lista con las compras y abonos del cliente; al tocar una compra se abre su detalle.
         * @param id Cliente
         * @return true si cambió algo y hay que abrirla otra vez
         */
        private async Task<bool> MostrarCompras(int id)
        {
            var c = _clientes.ObtenerPorId(id);
            if (c == null) return false;
            var compras = _clientes.Compras(id).ToList();
            var abonos = _clientes.Abonos(id).Where(a => a.DocumentoId == null).ToList();

            string contacto = string.Join(" · ", new[] { c.Telefono, c.Placa, c.Vehiculo }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var filas = compras.Select(v => new FilaDocumento
            {
                Id = v.Id,
                Fecha = v.Fecha,
                Descripcion = DescripcionCompra(v),
                Unidades = v.Unidades,
                Total = v.Total,
                Estado = v.Anulada ? v.Estado : v.Fiada ? "Fiada" : ""
            }).Concat(abonos.Select(a => new FilaDocumento
            {
                Id = a.Id,
                EsPago = true,
                Fecha = a.Fecha,
                Descripcion = FilaDocumento.DescripcionPago("Abono", a),
                Total = a.Valor,
                Estado = "Abono"
            })).OrderByDescending(f => f.Fecha).ToList();

            var ventana = new ListaDocumentosView();
            var vm = new ListaDocumentosViewModel(ventana, AbrirFila, c.TieneDeuda ? () => Abonar(c) : null)
            {
                Titulo = c.Nombre,
                Subtitulo = string.IsNullOrEmpty(contacto) ? "Compras y abonos del cliente" : contacto,
                Cifras =
                [
                    new Dato("Total comprado", Formato.Moneda(c.TotalComprado)),
                    new Dato("Ha pagado", Formato.Moneda(c.TotalPagado)),
                    new Dato("Te debe", c.TieneDeuda ? Formato.Moneda(c.Deuda) : c.Deuda < 0 ? c.DeudaTexto : "Nada"),
                    new Dato("Compras", Formato.Numero(c.NumeroCompras))
                ],
                Filas = filas,
                TextoVacio = "Este cliente todavía no tiene compras.",
                TextoAccion = c.TieneDeuda ? "💵  Registrar abono" : null
            };
            ventana.DataContext = vm;
            await Dialogs.ShowForm(ventana);
            return vm.Recargar;
        }

        /**
         * @brief Texto de una compra en la lista: cómo pagó, quién vendió y cuánto abonó si se fió.
         * @param v Venta
         * @return Descripción
         */
        private static string DescripcionCompra(Venta v)
        {
            if (v.Fiada && !v.Anulada)
                return v.Abonado > 0 ? $"{v.MetodoPago} · abonó {Formato.Moneda(v.Abonado)} al comprar" : "Sin abono al comprar";
            return $"{v.MetodoPago} · vendió {v.UsuarioNombre}";
        }

        /**
         * @brief Abre el detalle de una compra, o borra un abono si es el administrador.
         * @param fila Fila tocada
         * @return true si se borró un abono
         */
        private async Task<bool> AbrirFila(FilaDocumento fila)
        {
            if (!fila.EsPago)
            {
                var venta = _ventas.ObtenerDetalle(fila.Id);
                if (venta != null)
                    await DocumentoView.Mostrar(venta);
                return false;
            }

            if (!EsAdmin)
                return false;
            if (!await Dialogs.Confirm($"¿Borrar el abono de {Formato.Moneda(fila.Total)} del {fila.Fecha:dd/MM/yyyy}? Lo que debía vuelve a aparecer.",
                                       "Borrar abono", "Borrar"))
                return false;
            try
            {
                _clientes.EliminarAbono(fila.Id);
                _avisos.Exito("Abono borrado.");
                return true;
            }
            catch (Exception ex)
            {
                await Dialogs.Error(Errores.Mensaje(ex));
                return false;
            }
        }

        /**
         * @brief Registra un abono del cliente seleccionado.
         */
        private async Task AbonarSeleccionado()
        {
            int id = Seleccionado.Id;
            if (await Abonar(Seleccionado))
                Cargar(id);
        }

        /**
         * @brief Pide cuánto abonó el cliente y lo registra.
         * @param c Cliente
         * @return true si se registró el abono
         */
        private async Task<bool> Abonar(Cliente c)
        {
            var ventana = new RegistrarPagoView();
            ventana.DataContext = new RegistrarPagoViewModel(ventana, $"Abono de {c.Nombre}", $"Te debe {Formato.Moneda(c.Deuda)}.", c.Deuda,
                (valor, metodo, notas) => _clientes.RegistrarAbono(c.Id, valor, metodo, notas));
            if (!await Dialogs.ShowForm(ventana))
                return false;
            var actual = _clientes.ObtenerPorId(c.Id);
            _avisos.Exito(actual?.TieneDeuda == true
                ? $"Abono guardado. {c.Nombre} todavía debe {Formato.Moneda(actual.Deuda)}."
                : $"Abono guardado. {c.Nombre} quedó al día.");
            return true;
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
                await Dialogs.Error(Errores.Mensaje(ex));
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
                await Dialogs.Error(Errores.Mensaje(ex));
            }
        }
    }
}
