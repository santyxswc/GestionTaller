/**
 * @file App.axaml.cs
 * @brief Aplicación Avalonia y raíz de composición.
 * @author Santiago Caicedo
 */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TallerMoreno.Core.Abstractions;
using TallerMoreno.Desktop.Converters;
using TallerMoreno.Desktop.Services;
using TallerMoreno.Desktop.ViewModels;
using TallerMoreno.Desktop.Views;
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
                .AgregarTaller(carpetaDatos)
                .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

            servicios.GetRequiredService<InicializadorBaseDatos>().Inicializar();
            Convertidores.Fotos = servicios.GetRequiredService<IAlmacenFotos>();

            return servicios.GetRequiredService<FabricaVistas>().Ventana<LoginView, LoginViewModel>().Ventana;
        }
    }
}
