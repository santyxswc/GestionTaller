/**
 * @file AlmacenFotos.cs
 * @brief Fotos de los repuestos guardadas en una carpeta.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Abstractions;

namespace TallerMoreno.Infrastructure.Archivos
{
    /**
     * @brief Guarda cada foto con un nombre único en la carpeta de fotos.
     *
     * Solo se aceptan nombres sin carpeta, para que un nombre guardado en la base de datos
     * nunca apunte fuera de la carpeta de fotos.
     */
    public sealed class AlmacenFotos : IAlmacenFotos
    {
        /** Extensiones de imagen aceptadas. */
        private static readonly string[] Extensiones = { ".png", ".jpg", ".jpeg", ".webp", ".bmp" };

        /** Carpeta de las fotos. */
        private readonly string _carpeta;

        /**
         * @brief Crea el almacén y la carpeta si no existe.
         * @param carpeta Carpeta de las fotos
         */
        public AlmacenFotos(string carpeta)
        {
            _carpeta = carpeta;
            Directory.CreateDirectory(carpeta);
        }

        /**
         * @brief Guarda una foto con un nombre nuevo.
         * @param contenido Imagen ya reducida
         * @param extension Extensión con punto, como ".png"
         * @return Nombre del archivo guardado
         */
        public string Guardar(byte[] contenido, string extension)
        {
            ArgumentNullException.ThrowIfNull(contenido);
            extension = (extension ?? "").ToLowerInvariant();
            if (!Extensiones.Contains(extension))
                throw new ArgumentException("El archivo no es una imagen compatible (PNG, JPG, WEBP o BMP).");

            string nombre = Guid.NewGuid().ToString("N") + extension;
            File.WriteAllBytes(Path.Combine(_carpeta, nombre), contenido);
            return nombre;
        }

        /**
         * @brief Ruta completa de una foto.
         * @param nombre Nombre del archivo
         * @return Ruta en disco, o null si el nombre está vacío o no es válido
         */
        public string Ruta(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre) || nombre != Path.GetFileName(nombre))
                return null;
            return Path.Combine(_carpeta, nombre);
        }

        /**
         * @brief Borra una foto; no hace nada si no existe o está en uso.
         * @param nombre Nombre del archivo
         */
        public void Eliminar(string nombre)
        {
            string ruta = Ruta(nombre);
            if (ruta == null) return;
            try
            {
                File.Delete(ruta);
            }
            catch (IOException)
            {
                // La foto puede estar abierta en otra ventana; queda como archivo sobrante sin efecto.
            }
        }
    }
}
