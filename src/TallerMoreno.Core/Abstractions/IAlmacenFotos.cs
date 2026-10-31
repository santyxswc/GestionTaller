/**
 * @file IAlmacenFotos.cs
 * @brief Contrato del almacenamiento de las fotos de los repuestos.
 * @author Santiago Caicedo
 */
namespace TallerMoreno.Core.Abstractions
{
    /**
     * @brief Guarda las fotos como archivos; en la base de datos solo queda el nombre del archivo.
     */
    public interface IAlmacenFotos
    {
        /**
         * @brief Guarda una foto con un nombre nuevo.
         * @param contenido Imagen ya reducida (PNG o JPG)
         * @param extension Extensión con punto, como ".png"
         * @return Nombre del archivo guardado
         */
        string Guardar(byte[] contenido, string extension);

        /**
         * @brief Ruta completa de una foto.
         * @param nombre Nombre del archivo
         * @return Ruta en disco, o null si el nombre está vacío
         */
        string Ruta(string nombre);

        /**
         * @brief Borra una foto; no hace nada si no existe.
         * @param nombre Nombre del archivo
         */
        void Eliminar(string nombre);
    }
}
