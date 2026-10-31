/**
 * @file Texto.cs
 * @brief Búsqueda de texto sin importar mayúsculas ni tildes.
 * @author Santiago Caicedo
 */
using System.Globalization;

namespace TallerMoreno.Core
{
    /**
     * @brief Comparaciones de texto para los buscadores.
     *
     * "bateria" encuentra "Batería" y "BATERÍA": así nadie tiene que acordarse de las tildes.
     */
    public static class Texto
    {
        /** Opciones que ignoran mayúsculas y tildes. */
        private const CompareOptions Opciones = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

        /**
         * @brief Indica si alguno de los campos contiene el texto buscado.
         * @param buscado Texto escrito en el buscador; vacío coincide con todo
         * @param campos Campos del registro (pueden ser null)
         * @return true si hay coincidencia
         */
        public static bool Coincide(string buscado, params string[] campos)
        {
            if (string.IsNullOrWhiteSpace(buscado))
                return true;
            buscado = buscado.Trim();
            return campos.Any(c => c != null && Formato.Cultura.CompareInfo.IndexOf(c, buscado, Opciones) >= 0);
        }
    }
}
