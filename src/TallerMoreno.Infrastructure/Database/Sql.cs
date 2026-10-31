/**
 * @file Sql.cs
 * @brief Utilidades comunes de las consultas.
 * @author Santiago Caicedo
 */
using System.Data;
using Dapper;
using TallerMoreno.Core;

namespace TallerMoreno.Infrastructure.Database
{
    /**
     * @brief Fechas como texto, filtros de periodo y registro de movimientos.
     */
    internal static class Sql
    {
        /** Condición de periodo sobre una columna de fecha; usa los parámetros @Desde y @Hasta. */
        public static string FiltroFecha(string columna) =>
            $"(@Desde IS NULL OR {columna} >= @Desde) AND (@Hasta IS NULL OR {columna} < @Hasta)";

        /**
         * @brief Convierte una fecha opcional al texto de la base de datos.
         * @param fecha Fecha, o null
         * @return Texto, o null
         */
        public static string Fecha(DateTime? fecha) => fecha.HasValue ? Formato.ParaBaseDatos(fecha.Value) : null;

        /**
         * @brief Fecha y hora actual como texto de la base de datos.
         * @return Texto como "2026-10-25 14:30:00"
         */
        public static string Ahora() => Formato.ParaBaseDatos(DateTime.Now);

        /**
         * @brief Identificador de la última fila insertada en la conexión.
         * @param db Conexión
         * @param tran Transacción, o null
         * @return Identificador
         */
        public static int UltimoId(IDbConnection db, IDbTransaction tran = null) =>
            (int)db.ExecuteScalar<long>("SELECT last_insert_rowid();", transaction: tran);

        /**
         * @brief Registra un movimiento con el stock que quedó en el repuesto.
         * @param db Conexión abierta
         * @param tran Transacción en curso
         * @param fecha Fecha del movimiento (texto)
         * @param repuestoId Repuesto
         * @param tipo Tipo de movimiento
         * @param cantidad Unidades con signo
         * @param referencia Venta, entrada o motivo
         * @param usuarioId Usuario que lo hizo
         */
        public static void RegistrarMovimiento(IDbConnection db, IDbTransaction tran, string fecha, int repuestoId,
                                               string tipo, int cantidad, string referencia, int usuarioId)
        {
            db.Execute(@"
                INSERT INTO movimiento_inventario (fecha, repuesto_id, tipo, cantidad, stock_resultante, referencia, usuario_id)
                VALUES (@Fecha, @RepuestoId, @Tipo, @Cantidad, (SELECT stock FROM repuesto WHERE id = @RepuestoId), @Referencia, @UsuarioId)",
                new { Fecha = fecha, RepuestoId = repuestoId, Tipo = tipo, Cantidad = cantidad, Referencia = referencia, UsuarioId = usuarioId > 0 ? usuarioId : (int?)null },
                tran);
        }
    }
}
