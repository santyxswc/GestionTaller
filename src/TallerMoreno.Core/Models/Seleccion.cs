/**
 * @file Seleccion.cs
 * @brief Opción de una lista desplegable.
 * @author Santiago Caicedo
 */
namespace TallerMoreno.Core.Models
{
    /**
     * @brief Par identificador y nombre para listas desplegables (cliente, proveedor...).
     */
    public class Seleccion
    {
        /** Identificador del registro. */
        public int Id { get; set; }
        /** Texto que se muestra. */
        public string Nombre { get; set; }

        /**
         * @brief Texto que muestra la lista desplegable.
         * @return El nombre
         */
        public override string ToString() => Nombre;
    }
}
