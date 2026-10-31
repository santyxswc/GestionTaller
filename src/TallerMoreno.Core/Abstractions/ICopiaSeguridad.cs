/**
 * @file ICopiaSeguridad.cs
 * @brief Contrato de las copias de seguridad.
 * @author Santiago Caicedo
 */
namespace TallerMoreno.Core.Abstractions
{
    /**
     * @brief Copia la base de datos y las fotos en un solo archivo.
     */
    public interface ICopiaSeguridad
    {
        /**
         * @brief Crea la copia de seguridad.
         * @param rutaZip Archivo .zip de destino
         */
        void Crear(string rutaZip);
    }
}
