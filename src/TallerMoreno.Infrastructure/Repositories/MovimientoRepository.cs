/**
 * @file MovimientoRepository.cs
 * @brief Acceso a datos de los movimientos del inventario.
 * @author Santiago Caicedo
 */
using System.Data;
using Dapper;
using TallerMoreno.Core;
using TallerMoreno.Core.Abstractions;
using TallerMoreno.Core.Models;
using TallerMoreno.Infrastructure.Database;

namespace TallerMoreno.Infrastructure.Repositories
{
    /**
     * @brief Consultas de la tabla movimiento_inventario.
     */
    public class MovimientoRepository : IMovimientoRepository
    {
        /** Máximo de movimientos que devuelve una búsqueda. */
        private const int Maximo = 1000;

        /** Fábrica de conexiones a la base de datos. */
        private readonly IDbConnectionFactory _conexiones;

        /**
         * @brief Crea el repositorio.
         * @param conexiones Fábrica de conexiones a la base de datos
         */
        public MovimientoRepository(IDbConnectionFactory conexiones) => _conexiones = conexiones;

        /**
         * @brief Busca movimientos, del más reciente al más antiguo.
         * @param desde Fecha inicial (incluida), o null
         * @param hasta Fecha final (excluida), o null
         * @param texto Repuesto, tipo o referencia (sin importar tildes ni mayúsculas); vacío no filtra
         * @param repuestoId Solo los de este repuesto, o null
         * @return Movimientos que coinciden (máximo 1000)
         */
        public IEnumerable<MovimientoInventario> Buscar(DateTime? desde, DateTime? hasta, string texto, int? repuestoId)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                var movimientos = db.Query<MovimientoInventario>(@"
                    SELECT m.id AS Id, m.fecha AS Fecha, m.repuesto_id AS RepuestoId, r.nombre AS RepuestoNombre,
                           m.tipo AS Tipo, m.cantidad AS Cantidad, m.stock_resultante AS StockResultante,
                           m.referencia AS Referencia, u.nombre AS UsuarioNombre
                    FROM movimiento_inventario m
                    JOIN repuesto r ON r.id = m.repuesto_id
                    LEFT JOIN usuario u ON u.id = m.usuario_id
                    WHERE " + Sql.FiltroFecha("m.fecha") + @" AND (@RepuestoId IS NULL OR m.repuesto_id = @RepuestoId)
                    ORDER BY m.id DESC",
                    new { Desde = Sql.Fecha(desde), Hasta = Sql.Fecha(hasta), RepuestoId = repuestoId });
                return movimientos.Where(m => Texto.Coincide(texto, m.RepuestoNombre, m.Tipo, m.Referencia, m.UsuarioNombre))
                                  .Take(Maximo).ToList();
            }
        }
    }
}
