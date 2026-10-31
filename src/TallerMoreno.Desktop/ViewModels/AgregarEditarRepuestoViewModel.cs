/**
 * @file AgregarEditarRepuestoViewModel.cs
 * @brief Lógica del formulario de repuesto.
 * @author Santiago Caicedo
 */
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Services;
using TallerMoreno.Desktop.Converters;
using TallerMoreno.Desktop.Services;

namespace TallerMoreno.Desktop.ViewModels
{
    /**
     * @brief Crea o edita un repuesto con su foto y valida el formulario mientras se escribe.
     */
    public partial class AgregarEditarRepuestoViewModel : ObservableObject
    {
        /** Opción de la lista para dejar el repuesto sin proveedor. */
        private static readonly Seleccion SinProveedor = new() { Id = 0, Nombre = "Sin proveedor" };

        /** Inventario. */
        private readonly RepuestoService _service;
        /** Ventana del formulario. */
        private readonly Window _window;
        /** Repuesto que se edita. */
        private Repuesto _form;
        /** Foto nueva ya reducida, o null si no se cambió. */
        private byte[] _fotoNueva;
        /** true si se quitó la foto actual. */
        private bool _quitarFoto;

        /**
         * @brief Repuesto que se edita en el formulario; al cambiar se reevalúa Guardar.
         */
        public Repuesto Form
        {
            get => _form;
            set
            {
                if (_form != null) _form.PropertyChanged -= Form_PropertyChanged;
                if (SetProperty(ref _form, value))
                {
                    if (_form != null) _form.PropertyChanged += Form_PropertyChanged;
                    SaveCommand?.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(Titulo));
                    OnPropertyChanged(nameof(IsEditMode));
                }
            }
        }

        /** Categorías. */
        public string[] Categorias => Repuesto.Categorias;
        /** Proveedores, con la opción "Sin proveedor". */
        public List<Seleccion> Proveedores { get; }

        /** Proveedor habitual elegido. */
        [ObservableProperty]
        private Seleccion proveedor = SinProveedor;

        /** Vista previa de la foto. */
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TieneFoto))]
        private Bitmap foto;

        /** Error de validación o de guardado. */
        [ObservableProperty]
        private string errorMessage;

        /** Indica si hay foto para mostrar. */
        public bool TieneFoto => Foto != null;
        /** Indica si se está editando un repuesto existente. */
        public bool IsEditMode => Form?.Id > 0;
        /** Título de la ventana. */
        public string Titulo => IsEditMode ? "Editar repuesto" : "Nuevo repuesto";

        /** Guarda; se habilita cuando el formulario es válido. */
        public RelayCommand SaveCommand { get; }
        /** Cierra sin guardar. */
        public IRelayCommand CancelCommand { get; }
        /** Elige una foto. */
        public IAsyncRelayCommand ElegirFotoCommand { get; }
        /** Quita la foto. */
        public IRelayCommand QuitarFotoCommand { get; }

        /**
         * @brief Crea el formulario vacío.
         * @param window Ventana del formulario
         * @param service Inventario
         * @param proveedores Proveedores (para la lista)
         */
        public AgregarEditarRepuestoViewModel(Window window, RepuestoService service, ProveedorService proveedores)
        {
            _service = service;
            _window = window;
            Proveedores = [SinProveedor, .. proveedores.ParaSeleccion()];

            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(() => _window.Close(false));
            ElegirFotoCommand = new AsyncRelayCommand(ElegirFoto);
            QuitarFotoCommand = new RelayCommand(QuitarFoto);
            Form = new Repuesto { Categoria = Repuesto.Categorias[^1] };
        }

        /**
         * @brief Carga un repuesto existente para editarlo.
         * @param r Repuesto
         */
        public void LoadFromModel(Repuesto r)
        {
            if (r == null) return;
            Form = r.Copiar();
            Proveedor = Proveedores.FirstOrDefault(p => p.Id == r.ProveedorId) ?? SinProveedor;
            Foto = Convertidores.Foto.Convert(r.Foto, typeof(Bitmap), "520", null) as Bitmap;
        }

        /**
         * @brief Reevalúa Guardar cuando cambia un campo.
         * @param sender Modelo
         * @param e Campo que cambió
         */
        private void Form_PropertyChanged(object sender, PropertyChangedEventArgs e) => SaveCommand.NotifyCanExecuteChanged();

        /**
         * @brief Indica si los campos obligatorios son válidos.
         * @return true si se puede guardar
         */
        private bool CanSave() =>
            Form != null && !string.IsNullOrWhiteSpace(Form.Nombre) && !string.IsNullOrWhiteSpace(Form.Categoria)
            && Form.PrecioVenta > 0 && Form.PrecioCompra >= 0 && Form.Stock >= 0 && Form.StockMinimo >= 0;

        /**
         * @brief Pide una imagen, la reduce y la muestra.
         */
        private async Task ElegirFoto()
        {
            var archivo = await Archivos.ElegirImagen();
            if (archivo == null) return;
            try
            {
                await using var stream = await archivo.OpenReadAsync();
                _fotoNueva = await Task.Run(() => Fotos.Reducir(stream));
                _quitarFoto = false;
                using var memoria = new MemoryStream(_fotoNueva);
                Foto = new Bitmap(memoria);
                ErrorMessage = null;
            }
            catch (Exception ex)
            {
                Errores.Registrar(ex, "Elegir foto");
                ErrorMessage = "No se pudo usar esa imagen. Prueba con otra foto (JPG o PNG).";
            }
        }

        /**
         * @brief Quita la foto (se borra al guardar).
         */
        private void QuitarFoto()
        {
            _fotoNueva = null;
            _quitarFoto = true;
            Foto = null;
        }

        /**
         * @brief Guarda y cierra; si falla, muestra el error.
         */
        private void Save()
        {
            try
            {
                Form.ProveedorId = Proveedor?.Id > 0 ? Proveedor.Id : null;
                if (IsEditMode)
                    _service.Actualizar(Form, _fotoNueva, _quitarFoto, ".jpg");
                else
                    _service.Crear(Form, _fotoNueva, ".jpg");
                _window.Close(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = Errores.Mensaje(ex);
            }
        }
    }
}
