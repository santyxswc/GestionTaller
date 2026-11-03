/**
 * @file Program.cs
 * @brief Punto de entrada de la aplicación.
 * @author Santiago Caicedo
 *
 * Aplicación de escritorio de Taller Moreno hecha con Avalonia.
 */
using System.Globalization;
using Avalonia;

namespace TallerMoreno.Desktop
{
    /**
     * @brief Arranque de la aplicación de escritorio.
     */
    internal static class Program
    {
        /**
         * @brief Configura la cultura es-CO e inicia Avalonia.
         * @param args Argumentos de la línea de comandos
         */
        [STAThread]
        public static void Main(string[] args)
        {
            // Dos ventanas sobre la misma base de datos confunden a quien vende: si ya está abierto,
            // se trae al frente el que hay en vez de abrir otro.
            using var unico = new Mutex(true, @"Local\TallerMoreno.Desktop", out bool esElPrimero);
            if (!esElPrimero)
            {
                InstanciaUnica.MostrarLaQueYaEstaAbierta();
                return;
            }

            var cultura = TallerMoreno.Core.Formato.Cultura;
            CultureInfo.DefaultThreadCurrentCulture = cultura;
            CultureInfo.DefaultThreadCurrentUICulture = cultura;
            CultureInfo.CurrentCulture = cultura;
            CultureInfo.CurrentUICulture = cultura;

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Services.Errores.Registrar(ex, "El programa se cerró por un error");
                Environment.ExitCode = 1;
            }
        }

        /**
         * @brief Configura Avalonia con la fuente del sistema.
         * @return Constructor de la aplicación
         */
        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}
