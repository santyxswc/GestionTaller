/**
 * @file ConfiguracionDapper.cs
 * @brief Conversión de fechas entre C# y SQLite.
 * @author Santiago Caicedo
 */
using System.Data;
using System.Globalization;
using Dapper;
using TallerMoreno.Core;

namespace TallerMoreno.Infrastructure.Database
{
    /**
     * @brief Registra en Dapper cómo leer y escribir fechas.
     *
     * SQLite no tiene tipo fecha: se guardan como texto "yyyy-MM-dd HH:mm:ss", que se ordena
     * y compara bien como texto y no depende del idioma del sistema.
     */
    internal static class ConfiguracionDapper
    {
        /** Evita registrar dos veces. */
        private static int _registrado;

        /**
         * @brief Registra el conversor de fechas (solo la primera vez).
         */
        public static void Registrar()
        {
            if (Interlocked.Exchange(ref _registrado, 1) == 1)
                return;
            SqlMapper.AddTypeHandler(new ManejadorFecha());
        }

        /**
         * @brief Convierte DateTime en texto al guardar y texto en DateTime al leer.
         */
        private sealed class ManejadorFecha : SqlMapper.TypeHandler<DateTime>
        {
            /**
             * @brief Lee una fecha guardada como texto.
             * @param value Valor de la columna
             * @return Fecha
             */
            public override DateTime Parse(object value) => value switch
            {
                DateTime fecha => fecha,
                string texto => DateTime.Parse(texto, CultureInfo.InvariantCulture),
                _ => Convert.ToDateTime(value, CultureInfo.InvariantCulture)
            };

            /**
             * @brief Escribe una fecha como texto.
             * @param parameter Parámetro de la consulta
             * @param value Fecha
             */
            public override void SetValue(IDbDataParameter parameter, DateTime value)
            {
                parameter.DbType = DbType.String;
                parameter.Value = Formato.ParaBaseDatos(value);
            }
        }
    }
}
