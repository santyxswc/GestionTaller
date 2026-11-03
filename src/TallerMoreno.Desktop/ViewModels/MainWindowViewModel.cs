/**
 * @file MainWindowViewModel.cs
 * @brief Lógica de la ventana principal.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TallerMoreno.Core.Services;
using TallerMoreno.Core.Sesion;
using TallerMoreno.Desktop.Services;
using TallerMoreno.Desktop.Views;
using TallerMoreno.Infrastructure.Archivos;
using TallerMoreno.Infrastructure.Database;

namespace TallerMoreno.Desktop.ViewModels
{
    /**
     * @brief Navegación entre las cinco secciones, datos del usuario, copia de seguridad y salida.
     */
    public partial class MainWindowViewModel : ObservableObject
    {
        /** Reglas de negocio de usuarios. */
        private readonly UsuarioService _usuarios;
        /** Excel y copias de seguridad. */
        private readonly ExportacionService _exportacion;
        /** Usuario de la sesión. */
        private readonly ISesionUsuario _sesion;
        /** Creación de ventanas y secciones. */
        private readonly FabricaVistas _vistas;
        /** Ventana principal. */
        private readonly Window _window;

        /** Vista que se muestra en el área de contenido. */
        [ObservableProperty]
        private Control currentView;

        /** Sección abierta, para resaltarla en el menú. */
        [ObservableProperty]
        private string seccionActual;

        /** Avisos que se muestran en la parte baja. */
        public Avisos Avisos { get; }
        /** Indica si se muestran las opciones de administrador. */
        public bool EsAdmin => _sesion.EsAdmin;
        /** Nombre del usuario de la sesión. */
        public string NombreUsuario => _sesion.Usuario?.Nombre ?? "";
        /** Rol del usuario de la sesión. */
        public string Rol => _sesion.Usuario?.Rol ?? "";
        /** Inicial del nombre, para el círculo del usuario. */
        public string Inicial => string.IsNullOrEmpty(NombreUsuario) ? "?" : NombreUsuario[..1].ToUpper();

        /** Abre una sección por su nombre. */
        public IRelayCommand<string> IrCommand { get; }
        /** Cierra la sesión y vuelve al inicio de sesión. */
        public IAsyncRelayCommand SalirCommand { get; }
        /** Abre el cambio de contraseña. */
        public IAsyncRelayCommand CambiarClaveCommand { get; }
        /** Guarda una copia de seguridad (solo administrador). */
        public IAsyncRelayCommand CopiaSeguridadCommand { get; }
        /** Reemplaza los datos por los de una copia (solo administrador). */
        public IAsyncRelayCommand RestaurarCommand { get; }
        /** Crea un código de recuperación nuevo (solo administrador). */
        public IAsyncRelayCommand CodigoRecuperacionCommand { get; }

        /** Copias diarias y copias antes de restaurar. */
        private readonly CopiasAutomaticas _copias;
        /** Migración de la base de datos, por si la copia es de una versión anterior. */
        private readonly InicializadorBaseDatos _inicializador;

        /**
         * @brief Crea los comandos y abre Vender, que es lo que más se usa.
         * @param window Ventana principal
         * @param usuarios Reglas de negocio de usuarios
         * @param exportacion Excel y copias de seguridad
         * @param sesion Usuario de la sesión
         * @param vistas Creación de ventanas y secciones
         * @param avisos Avisos breves
         * @param copias Copias diarias
         * @param inicializador Migración de la base de datos
         */
        public MainWindowViewModel(Window window, UsuarioService usuarios, ExportacionService exportacion,
                                   ISesionUsuario sesion, FabricaVistas vistas, Avisos avisos,
                                   CopiasAutomaticas copias, InicializadorBaseDatos inicializador)
        {
            _usuarios = usuarios;
            _exportacion = exportacion;
            _sesion = sesion;
            _vistas = vistas;
            _window = window;
            _copias = copias;
            _inicializador = inicializador;
            Avisos = avisos;

            IrCommand = new RelayCommand<string>(Ir);
            SalirCommand = new AsyncRelayCommand(Salir);
            CambiarClaveCommand = new AsyncRelayCommand(CambiarClave);
            CopiaSeguridadCommand = new AsyncRelayCommand(CopiaSeguridad);
            RestaurarCommand = new AsyncRelayCommand(Restaurar);
            CodigoRecuperacionCommand = new AsyncRelayCommand(CodigoRecuperacion);

            Ir("Vender");
        }

        /**
         * @brief Cambia la sección del área de contenido.
         * @param seccion Vender, Inventario, Clientes, Proveedores, Historial o Usuarios
         */
        private void Ir(string seccion)
        {
            try
            {
                CurrentView = seccion switch
                {
                    "Inventario" => _vistas.Seccion<InventarioView, InventarioViewModel>(),
                    "Clientes" => _vistas.Seccion<ClientesView, ClientesViewModel>(),
                    "Proveedores" => _vistas.Seccion<ProveedoresView, ProveedoresViewModel>(),
                    "Historial" => _vistas.Seccion<HistorialView, HistorialViewModel>(),
                    "Usuarios" when EsAdmin => _vistas.Seccion<UsuariosView, UsuariosViewModel>(),
                    _ => _vistas.Seccion<VenderView, VenderViewModel>()
                };
                SeccionActual = seccion;
            }
            catch (Exception ex)
            {
                Avisos.Error(Errores.Mensaje(ex));
            }
        }

        /**
         * @brief Cierra la sesión y abre el inicio de sesión para el siguiente usuario.
         */
        private Task Salir()
        {
            _usuarios.CerrarSesion();
            var (login, _) = _vistas.Ventana<LoginView, LoginViewModel>();
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = login;
            login.Show();
            _window.Close();
            return Task.CompletedTask;
        }

        /**
         * @brief Abre el formulario para cambiar la contraseña propia.
         */
        private async Task CambiarClave()
        {
            var (ventana, _) = _vistas.Ventana<CambiarClaveView, CambiarClaveViewModel>();
            if (await Dialogs.ShowForm(ventana))
                Avisos.Exito("Tu contraseña quedó cambiada.");
        }

        /**
         * @brief Pregunta dónde guardar y crea el .zip con la base de datos y las fotos.
         */
        private async Task CopiaSeguridad()
        {
            string ruta = await Archivos.GuardarComo(ExportacionService.NombreArchivo("Copia de seguridad", ".zip"), Archivos.Zip);
            if (ruta == null) return;
            try
            {
                await Task.Run(() => _exportacion.CrearCopiaSeguridad(ruta));
                Avisos.Exito("Copia de seguridad guardada. Guárdala también en una USB o en la nube.");
            }
            catch (Exception ex)
            {
                await Dialogs.Error(Errores.Mensaje(ex));
            }
        }

        /**
         * @brief Pide una copia, guarda primero cómo están los datos ahora, la restaura y vuelve al inicio de sesión.
         *
         * Después de restaurar los usuarios pueden ser otros, por eso hay que entrar de nuevo.
         */
        private async Task Restaurar()
        {
            string ruta = await Archivos.ElegirCopia(_copias.Carpeta);
            if (ruta == null) return;
            if (!await Dialogs.Confirm(
                    $"Los datos del programa volverán a como estaban en la copia «{Path.GetFileName(ruta)}». "
                    + "Lo que se haya hecho después de esa copia se quita.\n\n"
                    + "Antes de restaurar se guarda una copia de cómo está todo ahora, por si te equivocas de archivo.",
                    "¿Restaurar esta copia?", "Restaurar"))
                return;

            try
            {
                string respaldo = await Task.Run(() =>
                {
                    string antes = _copias.HacerAntesDe("antes-de-restaurar");
                    _exportacion.RestaurarCopiaSeguridad(ruta);
                    _inicializador.Inicializar();
                    return antes;
                });
                await Dialogs.Info(
                    "La copia quedó restaurada. Entra de nuevo con tu usuario.\n\n"
                    + $"Lo que había antes quedó guardado en:\n{respaldo}",
                    "Copia restaurada");
                await Salir();
            }
            catch (Exception ex)
            {
                await Dialogs.Error(Errores.Mensaje(ex));
            }
        }

        /**
         * @brief Crea un código de recuperación nuevo y lo muestra para anotarlo.
         */
        private async Task CodigoRecuperacion()
        {
            if (!await Dialogs.Confirm(
                    "Se crea un código nuevo y el que tenías anotado deja de servir.",
                    "¿Crear un código de recuperación nuevo?", "Crear código nuevo"))
                return;
            try
            {
                await Dialogs.CodigoRecuperacion(_usuarios.GenerarCodigoRecuperacion());
            }
            catch (Exception ex)
            {
                await Dialogs.Error(Errores.Mensaje(ex));
            }
        }
    }
}
