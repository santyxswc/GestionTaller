/**
 * @file ExportacionService.cs
 * @brief Descarga de listados en Excel y copias de seguridad.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Abstractions;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Sesion;

namespace TallerMoreno.Core.Services
{
    /**
     * @brief Genera los archivos de Excel del inventario, ventas y clientes, y la copia de seguridad.
     */
    public class ExportacionService
    {
        /** Acceso a datos del inventario. */
        private readonly IRepuestoRepository _repuestos;
        /** Acceso a datos de ventas. */
        private readonly IVentaRepository _ventas;
        /** Acceso a datos de clientes. */
        private readonly IClienteRepository _clientes;
        /** Escritura de archivos de Excel. */
        private readonly IExportadorExcel _excel;
        /** Copias de seguridad. */
        private readonly ICopiaSeguridad _copia;
        /** Usuario de la sesión. */
        private readonly ISesionUsuario _sesion;

        /**
         * @brief Crea el servicio con sus dependencias.
         * @param repuestos Acceso a datos del inventario
         * @param ventas Acceso a datos de ventas
         * @param clientes Acceso a datos de clientes
         * @param excel Escritura de archivos de Excel
         * @param copia Copias de seguridad
         * @param sesion Usuario de la sesión (permisos)
         */
        public ExportacionService(IRepuestoRepository repuestos, IVentaRepository ventas, IClienteRepository clientes,
                                  IExportadorExcel excel, ICopiaSeguridad copia, ISesionUsuario sesion)
        {
            _repuestos = repuestos ?? throw new ArgumentNullException(nameof(repuestos));
            _ventas = ventas ?? throw new ArgumentNullException(nameof(ventas));
            _clientes = clientes ?? throw new ArgumentNullException(nameof(clientes));
            _excel = excel ?? throw new ArgumentNullException(nameof(excel));
            _copia = copia ?? throw new ArgumentNullException(nameof(copia));
            _sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
        }

        /**
         * @brief Nombre sugerido para un archivo, con la fecha de hoy.
         * @param que Contenido, como "Inventario"
         * @param extension Extensión con punto
         * @return Nombre como "Inventario Taller Moreno 2026-10-25.xlsx"
         */
        public static string NombreArchivo(string que, string extension = ".xlsx") =>
            $"{que} Taller Moreno {DateTime.Now:yyyy-MM-dd}{extension}";

        /**
         * @brief Exporta todo el inventario con las unidades de este momento.
         * @param ruta Archivo .xlsx de destino
         * @return Número de repuestos exportados
         */
        public int ExportarInventario(string ruta)
        {
            _sesion.ExigirSesion();
            var repuestos = _repuestos.Buscar(null, null).ToList();
            _excel.ExportarInventario(repuestos, ruta);
            return repuestos.Count;
        }

        /**
         * @brief Exporta las ventas de un periodo. Solo administrador.
         * @param periodo Uno de Periodo.Todos
         * @param ruta Archivo .xlsx de destino
         * @return Número de ventas exportadas
         */
        public int ExportarVentas(string periodo, string ruta)
        {
            _sesion.ExigirAdmin("descargar las ventas");
            var (desde, hasta) = Periodo.Rango(periodo, DateTime.Now);
            var ventas = _ventas.Buscar(desde, hasta, null, null).ToList();
            _excel.ExportarVentas(ventas, $"Ventas · {periodo}", ruta);
            return ventas.Count;
        }

        /**
         * @brief Exporta los clientes con lo que ha comprado cada uno.
         * @param ruta Archivo .xlsx de destino
         * @return Número de clientes exportados
         */
        public int ExportarClientes(string ruta)
        {
            _sesion.ExigirSesion();
            var clientes = _clientes.Buscar(null).ToList();
            _excel.ExportarClientes(clientes, ruta);
            return clientes.Count;
        }

        /**
         * @brief Guarda la base de datos y las fotos en un .zip. Solo administrador.
         * @param rutaZip Archivo .zip de destino
         */
        public void CrearCopiaSeguridad(string rutaZip)
        {
            _sesion.ExigirAdmin("hacer copias de seguridad");
            _copia.Crear(rutaZip);
        }

        /**
         * @brief Reemplaza los datos actuales por los de una copia de seguridad. Solo administrador.
         * @param rutaZip Archivo .zip de la copia
         */
        public void RestaurarCopiaSeguridad(string rutaZip)
        {
            _sesion.ExigirAdmin("restaurar copias de seguridad");
            _copia.Restaurar(rutaZip);
        }
    }
}
