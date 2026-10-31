/**
 * @file RegistroInfraestructura.cs
 * @brief Registro de las implementaciones de acceso a datos y archivos.
 * @author Santiago Caicedo
 */
using Microsoft.Extensions.DependencyInjection;
using TallerMoreno.Core.Abstractions;
using TallerMoreno.Infrastructure.Archivos;
using TallerMoreno.Infrastructure.Database;
using TallerMoreno.Infrastructure.Repositories;
using TallerMoreno.Infrastructure.Seguridad;

namespace TallerMoreno.Infrastructure
{
    /**
     * @brief Registra la base de datos SQLite, los repositorios, las fotos, el Excel y las copias.
     *
     * Todo vive en una carpeta de datos: taller.db y la subcarpeta fotos.
     */
    public static class RegistroInfraestructura
    {
        /** Nombre del archivo de la base de datos. */
        public const string ArchivoBaseDatos = "taller.db";

        /**
         * @brief Carpeta de datos por defecto: %LOCALAPPDATA%\TallerMoreno.
         *
         * No se usa una carpeta sincronizada (OneDrive, Dropbox) porque copiar la base de datos
         * mientras está abierta puede dañarla; para eso está la copia de seguridad.
         */
        public static string CarpetaPorDefecto =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TallerMoreno");

        /**
         * @brief Registra las implementaciones de los contratos de Core.
         * @param servicios Colección donde se registran
         * @param carpetaDatos Carpeta de datos; vacía usa CarpetaPorDefecto
         * @return La misma colección, para encadenar
         */
        public static IServiceCollection AgregarInfraestructura(this IServiceCollection servicios, string carpetaDatos = null)
        {
            string carpeta = string.IsNullOrWhiteSpace(carpetaDatos) ? CarpetaPorDefecto : carpetaDatos;
            Directory.CreateDirectory(carpeta);
            string rutaBaseDatos = Path.Combine(carpeta, ArchivoBaseDatos);
            string carpetaFotos = Path.Combine(carpeta, "fotos");

            servicios.AddSingleton<IDbConnectionFactory>(new SqliteConnectionFactory(rutaBaseDatos));
            servicios.AddSingleton<InicializadorBaseDatos>();
            servicios.AddSingleton<IHasherClaves, HasherPbkdf2>();
            servicios.AddSingleton<IAlmacenFotos>(new AlmacenFotos(carpetaFotos));
            servicios.AddSingleton<IExportadorExcel, ExportadorExcel>();
            servicios.AddSingleton<ICopiaSeguridad>(new CopiaSeguridadZip(rutaBaseDatos, carpetaFotos));

            servicios.AddSingleton<IUsuarioRepository, UsuarioRepository>();
            servicios.AddSingleton<IRepuestoRepository, RepuestoRepository>();
            servicios.AddSingleton<IClienteRepository, ClienteRepository>();
            servicios.AddSingleton<IProveedorRepository, ProveedorRepository>();
            servicios.AddSingleton<IVentaRepository, VentaRepository>();
            servicios.AddSingleton<IEntradaRepository, EntradaRepository>();
            servicios.AddSingleton<IMovimientoRepository, MovimientoRepository>();
            servicios.AddSingleton<IReporteRepository, ReporteRepository>();
            return servicios;
        }
    }
}
