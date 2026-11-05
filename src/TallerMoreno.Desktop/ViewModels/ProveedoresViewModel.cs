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
        [NotifyCanExecuteChangedFor(nameof(EditarCommand), nameof(EntradasCommand), nameof(EliminarCommand), nameof(PagarCommand))]
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
        /** Registra un pago al proveedor seleccionado, si se le debe. */
        public IAsyncRelayCommand PagarCommand { get; }

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
            PagarCommand = new AsyncRelayCommand(PagarSeleccionado, () => Seleccionado?.TieneDeuda == true);
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
                decimal deuda = todos.Where(p => p.TieneDeuda).Sum(p => p.Deuda);
                Resumen = $"{todos.Count} proveedores · {Formato.Moneda(todos.Sum(p => p.TotalComprado))} en mercancía recibida"
                        + (deuda > 0 ? $" · les debes {Formato.Moneda(deuda)}" : " · no le debes a nadie");
            }
            catch (Exception ex)
            {
                _avisos.Error(Errores.Mensaje(ex));
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
         * @brief Muestra las entradas y pagos del proveedor; se vuelve a abrir si se paga o se borra un pago.
         */
        private async Task VerEntradas()
        {
            int id = Seleccionado.Id;
            while (await MostrarEntradas(id)) { }
            Cargar(id);
        }

        /**
         * @brief Abre la lista con las entradas y pagos del proveedor; al tocar una entrada se abre su detalle.
         * @param id Proveedor
         * @return true si cambió algo y hay que abrirla otra vez
         */
        private async Task<bool> MostrarEntradas(int id)
        {
            var p = _proveedores.ObtenerPorId(id);
            if (p == null) return false;
            var entradas = _proveedores.Entradas(id).ToList();
            var pagos = _proveedores.Pagos(id).Where(g => g.DocumentoId == null).ToList();

            string contacto = string.Join(" · ", new[] { p.Contacto, p.Telefono, p.Correo }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var filas = entradas.Select(e => new FilaDocumento
            {
                Id = e.Id,
                Fecha = e.Fecha,
                Descripcion = DescripcionEntrada(e),
                Unidades = e.Unidades,
                Total = e.Total,
                Estado = e.QuedoDebiendo ? "A crédito" : ""
            }).Concat(pagos.Select(g => new FilaDocumento
            {
                Id = g.Id,
                EsPago = true,
                Fecha = g.Fecha,
                Descripcion = FilaDocumento.DescripcionPago("Pago", g),
                Total = g.Valor,
                Estado = "Pago"
            })).OrderByDescending(f => f.Fecha).ToList();

            var ventana = new ListaDocumentosView();
            var vm = new ListaDocumentosViewModel(ventana, AbrirFila, p.TieneDeuda ? () => Pagar(p) : null)
            {
                Titulo = p.Nombre,
                Subtitulo = string.IsNullOrEmpty(contacto) ? "Mercancía recibida y pagos" : contacto,
                Cifras =
                [
                    new Dato("Total comprado", Formato.Moneda(p.TotalComprado)),
                    new Dato("Le has pagado", Formato.Moneda(p.TotalPagado)),
                    new Dato("Le debes", p.TieneDeuda ? Formato.Moneda(p.Deuda) : "Nada"),
                    new Dato("Entradas", Formato.Numero(p.NumeroEntradas))
                ],
                Filas = filas,
                TextoVacio = "Todavía no ha llegado mercancía de este proveedor.",
                TextoAccion = p.TieneDeuda ? "💵  Registrar pago" : null
            };
            ventana.DataContext = vm;
            await Dialogs.ShowForm(ventana);
            return vm.Recargar;
        }

        /**
         * @brief Texto de una entrada en la lista: factura y quién recibió, o cuánto se pagó si se quedó debiendo.
         * @param e Entrada
         * @return Descripción
         */
        private static string DescripcionEntrada(Entrada e)
        {
            string pago = !e.QuedoDebiendo ? $"recibió {e.UsuarioNombre}"
                        : e.Pagado > 0 ? $"pagaste {Formato.Moneda(e.Pagado)} al recibir"
                        : "sin pagar al recibir";
            return string.IsNullOrEmpty(e.FacturaProveedor) ? char.ToUpper(pago[0]) + pago[1..] : $"Factura {e.FacturaProveedor} · {pago}";
        }

        /**
         * @brief Abre el detalle de una entrada, o borra un pago si es el administrador.
         * @param fila Fila tocada
         * @return true si se borró un pago
         */
        private async Task<bool> AbrirFila(FilaDocumento fila)
        {
            if (!fila.EsPago)
            {
                var entrada = _entradas.ObtenerDetalle(fila.Id);
                if (entrada != null)
                    await DocumentoView.Mostrar(entrada);
                return false;
            }

            if (!EsAdmin)
                return false;
            if (!await Dialogs.Confirm($"¿Borrar el pago de {Formato.Moneda(fila.Total)} del {fila.Fecha:dd/MM/yyyy}? Lo que se debía vuelve a aparecer.",
                                       "Borrar pago", "Borrar"))
                return false;
            try
            {
                _proveedores.EliminarPago(fila.Id);
                _avisos.Exito("Pago borrado.");
                return true;
            }
            catch (Exception ex)
            {
                await Dialogs.Error(Errores.Mensaje(ex));
                return false;
            }
        }

        /**
         * @brief Registra un pago al proveedor seleccionado.
         */
        private async Task PagarSeleccionado()
        {
            int id = Seleccionado.Id;
            if (await Pagar(Seleccionado))
                Cargar(id);
        }

        /**
         * @brief Pide cuánto se le pagó al proveedor y lo registra.
         * @param p Proveedor
         * @return true si se registró el pago
         */
        private async Task<bool> Pagar(Proveedor p)
        {
            var ventana = new RegistrarPagoView();
            ventana.DataContext = new RegistrarPagoViewModel(ventana, $"Pagarle a {p.Nombre}", $"Le debes {Formato.Moneda(p.Deuda)}.", p.Deuda,
                (valor, metodo, notas) => _proveedores.RegistrarPago(p.Id, valor, metodo, notas));
            if (!await Dialogs.ShowForm(ventana))
                return false;
            var actual = _proveedores.ObtenerPorId(p.Id);
            _avisos.Exito(actual?.TieneDeuda == true
                ? $"Pago guardado. A {p.Nombre} todavía le debes {Formato.Moneda(actual.Deuda)}."
                : $"Pago guardado. Quedaste al día con {p.Nombre}.");
            return true;
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
                await Dialogs.Error(Errores.Mensaje(ex));
            }
        }
    }
}
