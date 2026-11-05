/**
 * @file Formato.cs
 * @brief Formato de moneda, números y fechas de la aplicación.
 * @author Santiago Caicedo
 */
using System.Globalization;

namespace TallerMoreno.Core
{
    /**
     * @brief Formatea valores con la cultura es-CO (pesos colombianos).
     *
     * Se usa en los textos que se guardan en la base de datos, para que no dependan
     * del idioma del sistema operativo.
     */
    public static class Formato
    {
        /** Cultura de la aplicación: español de Colombia. */
        public static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");

        /** Formato con el que se guardan las fechas en la base de datos (ordenable como texto). */
        public const string FechaBaseDatos = "yyyy-MM-dd HH:mm:ss";

        /**
         * @brief Formatea un valor como moneda sin decimales.
         * @param valor Valor en pesos
         * @return Texto como "$ 8.500"
         */
        public static string Moneda(decimal valor) => valor.ToString("C0", Cultura);

        /**
         * @brief Formatea un número con separador de miles.
         * @param valor Valor a formatear
         * @return Texto como "8.500"
         */
        public static string Numero(decimal valor) => valor.ToString("N0", Cultura);

        /**
         * @brief Formatea un kilometraje.
         * @param km Kilómetros, o null
         * @return Texto como "85.000 km", o vacío si no hay
         */
        public static string Kilometraje(int? km) => km.HasValue ? $"{Numero(km.Value)} km" : "";

        /**
         * @brief Convierte una fecha al texto que se guarda en la base de datos.
         * @param fecha Fecha local
         * @return Texto como "2026-10-25 14:30:00"
         */
        public static string ParaBaseDatos(DateTime fecha) => fecha.ToString(FechaBaseDatos, CultureInfo.InvariantCulture);
    }
}
