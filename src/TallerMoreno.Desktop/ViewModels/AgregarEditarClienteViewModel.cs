/**
 * @file AgregarEditarClienteViewModel.cs
 * @brief Lógica del formulario de cliente.
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
     * @brief Crea o edita un cliente; solo el nombre es obligatorio.
     */
    public partial class AgregarEditarClienteViewModel : ObservableObject
    {
        /** Clientes. */
        private readonly ClienteService _service;
        /** Ventana del formulario. */
        private readonly Window _window;
        /** Cliente que se edita. */
        private Cliente _form;

        /**
         * @brief Cliente que se edita; al guardar uno nuevo queda con su Id.
         */
        public Cliente Form
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
                    OnPropertyChanged(nameof(Kilometraje));
                }
            }
        }

        /**
         * @brief Kilometraje del cliente como número decimal, que es lo que usa el campo numérico.
         */
        public decimal? Kilometraje
        {
            get => Form?.Kilometraje;
            set
            {
                if (Form != null)
                    Form.Kilometraje = value.HasValue ? (int)Math.Round(value.Value) : null;
            }
        }

        /** Error de validación o de guardado. */
        [ObservableProperty]
        private string errorMessage;

        /** Título de la ventana. */
        public string Titulo => Form?.Id > 0 ? "Editar cliente" : "Nuevo cliente";

        /** Guarda; se habilita cuando hay nombre. */
        public RelayCommand SaveCommand { get; }
        /** Cierra sin guardar. */
        public IRelayCommand CancelCommand { get; }

        /**
         * @brief Crea el formulario vacío.
         * @param window Ventana del formulario
         * @param service Clientes
         */
        public AgregarEditarClienteViewModel(Window window, ClienteService service)
        {
            _service = service;
            _window = window;
            SaveCommand = new RelayCommand(Save, () => !string.IsNullOrWhiteSpace(Form?.Nombre));
            CancelCommand = new RelayCommand(() => _window.Close(false));
            Form = new Cliente();
        }

        /**
         * @brief Carga un cliente existente para editarlo.
         * @param c Cliente
         */
        public void LoadFromModel(Cliente c)
        {
            if (c != null)
                Form = c.Copiar();
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
