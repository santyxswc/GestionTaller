/**
 * @file Periodo.cs
 * @brief Periodos para filtrar ventas, entradas y reportes.
 * @author Santiago Caicedo
 */
namespace TallerMoreno.Core.Models
{
    /**
     * @brief Periodos de tiempo que se eligen con un solo botón.
     */
    public static class Periodo
    {
        /** Desde las 00:00 de hoy. */
        public const string Hoy = "Hoy";
        /** Desde el lunes de esta semana. */
        public const string Semana = "Esta semana";
        /** Desde el día 1 de este mes. */
        public const string Mes = "Este mes";
        /** Desde el 1 de enero de este año. */
        public const string Anio = "Este año";
        /** Sin límite de fechas. */
        public const string Todo = "Todo";

        /** Periodos en el orden en que se muestran. */
        public static readonly string[] Todos = { Hoy, Semana, Mes, Anio, Todo };

        /**
         * @brief Calcula el rango de fechas de un periodo.
         * @param periodo Uno de Todos
         * @param ahora Fecha de referencia (normalmente DateTime.Now)
         * @return Desde (incluido) y hasta (excluido); null si no hay límite
         */
        public static (DateTime? Desde, DateTime? Hasta) Rango(string periodo, DateTime ahora)
        {
            DateTime hoy = ahora.Date;
            DateTime manana = hoy.AddDays(1);
            return periodo switch
            {
                Hoy => (hoy, manana),
                Semana => (hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7)), manana),
                Mes => (new DateTime(hoy.Year, hoy.Month, 1), manana),
                Anio => (new DateTime(hoy.Year, 1, 1), manana),
                _ => (null, null)
            };
        }
    }
}
