/**
 * @file ReporteRepository.cs
 * @brief Consultas de los reportes de ventas.
 * @author Santiago Caicedo
 */
using System.Data;
using Dapper;
using TallerMoreno.Core.Abstractions;
using TallerMoreno.Core.Models;
using TallerMoreno.Infrastructure.Database;

namespace TallerMoreno.Infrastructure.Repositories
{
    /**
     * @brief Totales y rankings de las ventas no anuladas.
     */
    public class ReporteRepository : IReporteRepository
    {
        /** Fábrica de conexiones a la base de datos. */
        private readonly IDbConnectionFactory _conexiones;

        /** Condición común: ventas no anuladas dentro del periodo. */
        private static readonly string Filtro = "v.anulada = 0 AND " + Sql.FiltroFecha("v.fecha");

        /**
         * @brief Crea el repositorio.
         * @param conexiones Fábrica de conexiones a la base de datos
         */
        public ReporteRepository(IDbConnectionFactory conexiones) => _conexiones = conexiones;

        /**
         * @brief Totales de un periodo.
         * @param desde Fecha inicial (incluida), o null
         * @param hasta Fecha final (excluida), o null
         * @return Total, ventas, unidades y ganancia
         */
        public ResumenVentas Resumen(DateTime? desde, DateTime? hasta)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                var p = Parametros(desde, hasta);
                var resumen = db.QueryFirst<ResumenVentas>(
                    "SELECT COUNT(*) AS NumeroVentas, COALESCE(SUM(v.total), 0) AS Total FROM venta v WHERE " + Filtro, p);
                var (unidades, costo) = db.QueryFirst<(long, long)>(@"
                    SELECT COALESCE(SUM(d.cantidad), 0), COALESCE(SUM(d.costo_unitario * d.cantidad), 0)
                    FROM venta_detalle d JOIN venta v ON v.id = d.venta_id WHERE " + Filtro, p);
                resumen.Unidades = (int)unidades;
                resumen.Ganancia = resumen.Total - costo;
                return resumen;
            }
        }

        /**
         * @brief Repuestos más vendidos por unidades.
         * @param desde Fecha inicial (incluida), o null
         * @param hasta Fecha final (excluida), o null
         * @param cantidad Número de filas
         * @return Repuesto, unidades y total
         */
        public IEnumerable<ItemRanking> TopRepuestos(DateTime? desde, DateTime? hasta, int cantidad)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Query<ItemRanking>(@"
                    SELECT r.nombre AS Nombre, SUM(d.cantidad) AS Cantidad, SUM(d.subtotal) AS Total
                    FROM venta_detalle d
                    JOIN venta v ON v.id = d.venta_id
                    JOIN repuesto r ON r.id = d.repuesto_id
                    WHERE " + Filtro + @"
                    GROUP BY r.id ORDER BY Cantidad DESC, Total DESC LIMIT @N",
                    new { Desde = Sql.Fecha(desde), Hasta = Sql.Fecha(hasta), N = cantidad }).ToList();
        }

        /**
         * @brief Clientes registrados que más han comprado.
         * @param desde Fecha inicial (incluida), o null
         * @param hasta Fecha final (excluida), o null
         * @param cantidad Número de filas
         * @return Cliente, número de compras y total
         */
        public IEnumerable<ItemRanking> TopClientes(DateTime? desde, DateTime? hasta, int cantidad)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Query<ItemRanking>(@"
                    SELECT c.nombre AS Nombre, COUNT(*) AS Cantidad, SUM(v.total) AS Total
                    FROM venta v JOIN cliente c ON c.id = v.cliente_id
                    WHERE " + Filtro + @"
                    GROUP BY c.id ORDER BY Total DESC LIMIT @N",
                    new { Desde = Sql.Fecha(desde), Hasta = Sql.Fecha(hasta), N = cantidad }).ToList();
        }

        /**
         * @brief Ventas de cada usuario.
         * @param desde Fecha inicial (incluida), o null
         * @param hasta Fecha final (excluida), o null
         * @return Usuario, número de ventas y total
         */
        public IEnumerable<ItemRanking> VentasPorUsuario(DateTime? desde, DateTime? hasta)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Query<ItemRanking>(@"
                    SELECT u.nombre AS Nombre, COUNT(*) AS Cantidad, SUM(v.total) AS Total
                    FROM venta v JOIN usuario u ON u.id = v.usuario_id
                    WHERE " + Filtro + @"
                    GROUP BY u.id ORDER BY Total DESC", Parametros(desde, hasta)).ToList();
        }

        /**
         * @brief Parámetros del filtro de periodo.
         * @param desde Fecha inicial, o null
         * @param hasta Fecha final, o null
         * @return Objeto anónimo con @Desde y @Hasta
         */
        private static object Parametros(DateTime? desde, DateTime? hasta) => new { Desde = Sql.Fecha(desde), Hasta = Sql.Fecha(hasta) };
    }
}
