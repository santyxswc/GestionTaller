/**
 * @file AgregarEditarProveedorViewModel.cs
 * @brief Lógica del formulario de proveedor.
 * @author Santiago Caicedo
 */
using System.ComponentModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Services;
using TallerMoreno.Desktop.Services;

namespace TallerMoreno.Desktop.ViewModels
{
    /**
     * @brief Crea o edita un proveedor; solo el nombre es obligatorio.
     */
    public partial class AgregarEditarProveedorViewModel : ObservableObject
    {
        /** Proveedores. */
        private readonly ProveedorService _service;
        /** Ventana del formulario. */
        private readonly Window _window;
        /** Proveedor que se edita. */
        private Proveedor _form;

        /**
         * @brief Proveedor que se edita; al guardar uno nuevo queda con su Id.
         */
        public Proveedor Form
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
                }
            }
        }

        /** Error de validación o de guardado. */
        [ObservableProperty]
        private string errorMessage;

        /** Título de la ventana. */
        public string Titulo => Form?.Id > 0 ? "Editar proveedor" : "Nuevo proveedor";

        /** Guarda; se habilita cuando hay nombre. */
        public RelayCommand SaveCommand { get; }
        /** Cierra sin guardar. */
        public IRelayCommand CancelCommand { get; }

        /**
         * @brief Crea el formulario vacío.
         * @param window Ventana del formulario
         * @param service Proveedores
         */
        public AgregarEditarProveedorViewModel(Window window, ProveedorService service)
        {
            _service = service;
            _window = window;
            SaveCommand = new RelayCommand(Save, () => !string.IsNullOrWhiteSpace(Form?.Nombre));
            CancelCommand = new RelayCommand(() => _window.Close(false));
            Form = new Proveedor();
        }

        /**
         * @brief Carga un proveedor existente para editarlo.
         * @param p Proveedor
         */
        public void LoadFromModel(Proveedor p)
        {
            if (p != null)
                Form = p.Copiar();
        }

        /**
         * @brief Reevalúa Guardar cuando cambia un campo.
         * @param sender Modelo
         * @param e Campo que cambió
         */
        private void Form_PropertyChanged(object sender, PropertyChangedEventArgs e) => SaveCommand.NotifyCanExecuteChanged();

        /**
         * @brief Guarda y cierra; si falla, muestra el error.
         */
        private void Save()
        {
            try
            {
                if (Form.Id > 0)
                    _service.Actualizar(Form);
                else
                    _service.Crear(Form);
                _window.Close(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = Errores.Mensaje(ex);
            }
        }
    }
}
