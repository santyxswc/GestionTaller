/**
 * @file App.axaml.cs
 * @brief Aplicación Avalonia y raíz de composición.
 * @author Santiago Caicedo
 */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TallerMoreno.Core.Abstractions;
using TallerMoreno.Desktop.Converters;
using TallerMoreno.Desktop.Services;
using TallerMoreno.Desktop.ViewModels;
using TallerMoreno.Desktop.Views;
using TallerMoreno.Infrastructure.Archivos;
using TallerMoreno.Infrastructure.Database;

namespace TallerMoreno.Desktop
{
    /**
     * @brief Carga los estilos, compone los servicios, prepara la base de datos y abre el inicio de sesión.
     */
    public partial class App : Application
    {
        /**
         * @brief Carga App.axaml (estilos y recursos).
         */
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /**
         * @brief Atrapa los errores no controlados y abre el inicio de sesión, o un aviso si no se puede arrancar.
         */
        public override void OnFrameworkInitializationCompleted()
        {
            Errores.AtraparNoControlados();

            Window principal;
            try
            {
                principal = Arrancar();
            }
            catch (Exception ex)
            {
                // Sin base de datos no hay programa: se explica qué pasó en vez de cerrarse sin avisar.
                principal = Dialogs.ErrorAlArrancar(
                    Errores.Mensaje(ex) + "\n\nEl detalle quedó anotado en:\n" + Errores.RutaRegistro);
            }

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
                desktop.MainWindow = principal;
            }

            base.OnFrameworkInitializationCompleted();
        }

        /**
         * @brief Lee la configuración, compone los servicios, prepara la base de datos y crea el inicio de sesión.
         * @return Ventana de inicio de sesión
         */
        private static Window Arrancar()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            string carpetaDatos = configuration["Datos:Carpeta"];
            Errores.UsarCarpeta(carpetaDatos);

            var servicios = new ServiceCollection()
                .AgregarTaller(carpetaDatos, configuration["Copias:CarpetaExtra"])
                .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

            var inicializador = servicios.GetRequiredService<InicializadorBaseDatos>();
            var copias = servicios.GetRequiredService<CopiasAutomaticas>();
            int version = inicializador.VersionGuardada();
            if (version > 0 && version < InicializadorBaseDatos.VersionEsquema)
                copias.HacerAntesDe("antes-de-actualizar");
            inicializador.Inicializar();
            Convertidores.Fotos = servicios.GetRequiredService<IAlmacenFotos>();

            ProgramarCopiasDiarias(copias);

            return servicios.GetRequiredService<FabricaVistas>().Ventana<LoginView, LoginViewModel>().Ventana;
        }

        /** Revisa cada hora si ya se hizo la copia del día, por si el computador queda prendido varios días. */
        private static DispatcherTimer _relojCopias;

        /**
         * @brief Hace la copia del día al abrir el programa y vuelve a revisar cada hora.
         *
         * Corre en segundo plano para no demorar la entrada; si falla, queda anotado en errores.log.
         * @param copias Copias diarias
         */
        private static void ProgramarCopiasDiarias(CopiasAutomaticas copias)
        {
            void Revisar() => Task.Run(() =>
            {
                try
                {
                    copias.HacerSiFalta(DateTime.Today);
                    if (copias.ErrorCarpetaExtra != null)
                        Errores.Registrar(copias.ErrorCarpetaExtra, "No se pudo dejar la copia diaria en la carpeta extra");
                }
                catch (Exception ex)
                {
                    Errores.Registrar(ex, "Copia de seguridad diaria");
                }
            });

            Revisar();
            _relojCopias = new DispatcherTimer { Interval = TimeSpan.FromHours(1) };
            _relojCopias.Tick += (_, _) => Revisar();
            _relojCopias.Start();
        }
    }
}
