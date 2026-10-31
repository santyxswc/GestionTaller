/**
 * @file Entorno.cs
 * @brief Base de datos temporal con todos los servicios para las pruebas.
 * @author Santiago Caicedo
 */
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Services;
using TallerMoreno.Core.Sesion;
using TallerMoreno.Infrastructure;
using TallerMoreno.Infrastructure.Database;

namespace TallerMoreno.Tests
{
    /**
     * @brief Crea una carpeta de datos temporal, la base de datos y la sesión del dueño.
     *
     * Cada prueba usa su propio entorno, así no comparten datos.
     */
    public sealed class Entorno : IDisposable
    {
        /** Contenedor de dependencias. */
        private readonly ServiceProvider _servicios;

        /** Carpeta temporal de datos. */
        public string Carpeta { get; } = Path.Combine(Path.GetTempPath(), "taller-pruebas-" + Guid.NewGuid().ToString("N"));

        /**
         * @brief Crea la base de datos y entra como administrador.
         */
        public Entorno()
        {
            var coleccion = new ServiceCollection().AgregarInfraestructura(Carpeta);
            coleccion.AddSingleton<ISesionUsuario, SesionUsuario>();
            coleccion.AddSingleton<UsuarioService>();
            coleccion.AddSingleton<RepuestoService>();
            coleccion.AddSingleton<ClienteService>();
            coleccion.AddSingleton<ProveedorService>();
            coleccion.AddSingleton<VentaService>();
            coleccion.AddSingleton<EntradaService>();
            coleccion.AddSingleton<ReporteService>();
            coleccion.AddSingleton<ExportacionService>();
            _servicios = coleccion.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

            _servicios.GetRequiredService<InicializadorBaseDatos>().Inicializar();
            Usuarios.CrearPrimerAdministrador("Don Moreno", "moreno", "clave123");
        }

        /** Sesión del usuario. */
        public ISesionUsuario Sesion => _servicios.GetRequiredService<ISesionUsuario>();
        /** Usuarios. */
        public UsuarioService Usuarios => _servicios.GetRequiredService<UsuarioService>();
        /** Inventario. */
        public RepuestoService Repuestos => _servicios.GetRequiredService<RepuestoService>();
        /** Clientes. */
        public ClienteService Clientes => _servicios.GetRequiredService<ClienteService>();
        /** Proveedores. */
        public ProveedorService Proveedores => _servicios.GetRequiredService<ProveedorService>();
        /** Ventas. */
        public VentaService Ventas => _servicios.GetRequiredService<VentaService>();
        /** Entradas de mercancía. */
        public EntradaService Entradas => _servicios.GetRequiredService<EntradaService>();
        /** Reportes. */
        public ReporteService Reportes => _servicios.GetRequiredService<ReporteService>();
        /** Excel y copias. */
        public ExportacionService Exportacion => _servicios.GetRequiredService<ExportacionService>();

        /**
         * @brief Registra un repuesto de prueba.
         * @param nombre Nombre
         * @param stock Unidades iniciales
         * @param precio Precio de venta
         * @param costo Precio de compra
         * @return Identificador
         */
        public int NuevoRepuesto(string nombre = "Batería 12V 45Ah", int stock = 20, decimal precio = 350000, decimal costo = 280000) =>
            Repuestos.Crear(new Repuesto
            {
                Nombre = nombre,
                Categoria = "Baterías",
                PrecioVenta = precio,
                PrecioCompra = costo,
                Stock = stock,
                StockMinimo = 3
            });

        /**
         * @brief Línea de venta o de entrada.
         * @param repuestoId Repuesto
         * @param cantidad Unidades
         * @param precio Precio unitario (solo para entradas)
         * @return Línea
         */
        public static LineaDocumento Linea(int repuestoId, int cantidad, decimal precio = 0) =>
            new() { RepuestoId = repuestoId, Cantidad = cantidad, PrecioUnitario = precio };

        /**
         * @brief Cierra las conexiones y borra la carpeta temporal.
         */
        public void Dispose()
        {
            _servicios.Dispose();
            SqliteConnection.ClearAllPools();
            try
            {
                Directory.Delete(Carpeta, recursive: true);
            }
            catch (IOException)
            {
                // Windows puede tardar en soltar el archivo; la carpeta temporal se limpia sola después.
            }
        }
    }
}
