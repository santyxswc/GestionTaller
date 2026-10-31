/**
 * @file App.axaml.cs
 * @brief Aplicación Avalonia y raíz de composición.
 * @author Santiago Caicedo
 */
using Avalonia;
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
         * @brief Lee appsettings.json (opcional), crea las tablas si faltan y abre el inicio de sesión.
         */
        public override void OnFrameworkInitializationCompleted()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            var servicios = new ServiceCollection()
                .AgregarTaller(configuration["Datos:Carpeta"])
                .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

            servicios.GetRequiredService<InicializadorBaseDatos>().Inicializar();
            Convertidores.Fotos = servicios.GetRequiredService<IAlmacenFotos>();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnLastWindowClose;
                desktop.MainWindow = servicios.GetRequiredService<FabricaVistas>()
                    .Ventana<LoginView, LoginViewModel>().Ventana;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
