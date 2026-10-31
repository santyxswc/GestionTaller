/**
 * @file ComposicionServicios.cs
 * @brief Registro de dependencias de la aplicación.
 * @author Santiago Caicedo
 */
using Microsoft.Extensions.DependencyInjection;
using TallerMoreno.Core.Services;
using TallerMoreno.Core.Sesion;
using TallerMoreno.Desktop.Services;
using TallerMoreno.Infrastructure;

namespace TallerMoreno.Desktop
{
    /**
     * @brief Registro de las dependencias de la aplicación.
     */
    public static class ComposicionServicios
    {
        /**
         * @brief Registra la infraestructura, los servicios de negocio y las utilidades de la interfaz.
         * @param servicios Colección donde se registran
         * @param carpetaDatos Carpeta de datos de appsettings.json; vacía usa la de por defecto
         * @return La misma colección, para encadenar
         */
        public static IServiceCollection AgregarTaller(this IServiceCollection servicios, string carpetaDatos)
        {
            servicios.AgregarInfraestructura(carpetaDatos);

            servicios.AddSingleton<ISesionUsuario, SesionUsuario>();
            servicios.AddSingleton<UsuarioService>();
            servicios.AddSingleton<RepuestoService>();
            servicios.AddSingleton<ClienteService>();
            servicios.AddSingleton<ProveedorService>();
            servicios.AddSingleton<VentaService>();
            servicios.AddSingleton<EntradaService>();
            servicios.AddSingleton<ReporteService>();
            servicios.AddSingleton<ExportacionService>();

            servicios.AddSingleton(new Preferencias(string.IsNullOrWhiteSpace(carpetaDatos) ? RegistroInfraestructura.CarpetaPorDefecto : carpetaDatos));
            servicios.AddSingleton<Avisos>();
            servicios.AddSingleton<FabricaVistas>();

            return servicios;
        }
    }
}
