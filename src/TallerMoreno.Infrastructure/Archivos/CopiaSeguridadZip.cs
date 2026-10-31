/**
 * @file CopiaSeguridadZip.cs
 * @brief Copia de seguridad de la base de datos y las fotos.
 * @author Santiago Caicedo
 */
using System.IO.Compression;
using Microsoft.Data.Sqlite;
using TallerMoreno.Core.Abstractions;

namespace TallerMoreno.Infrastructure.Archivos
{
    /**
     * @brief Crea un .zip con una copia consistente de la base de datos y la carpeta de fotos.
     *
     * La base de datos se copia con la API de respaldo de SQLite, que funciona aunque la
     * aplicación esté abierta. Para restaurar basta con descomprimir el .zip en la carpeta de datos.
     */
    public sealed class CopiaSeguridadZip : ICopiaSeguridad
    {
        /** Ruta del archivo de la base de datos. */
        private readonly string _rutaBaseDatos;
        /** Carpeta de las fotos. */
        private readonly string _carpetaFotos;

        /**
         * @brief Crea el servicio de copias.
         * @param rutaBaseDatos Ruta del archivo .db
         * @param carpetaFotos Carpeta de las fotos
         */
        public CopiaSeguridadZip(string rutaBaseDatos, string carpetaFotos)
        {
            _rutaBaseDatos = rutaBaseDatos;
            _carpetaFotos = carpetaFotos;
        }

        /**
         * @brief Crea la copia de seguridad.
         * @param rutaZip Archivo .zip de destino (se reemplaza si existe)
         */
        public void Crear(string rutaZip)
        {
            string temporal = Path.Combine(Path.GetTempPath(), $"taller-{Guid.NewGuid():N}.db");
            try
            {
                using (var origen = new SqliteConnection($"Data Source={_rutaBaseDatos};Mode=ReadOnly"))
                using (var destino = new SqliteConnection($"Data Source={temporal};Pooling=False"))
                {
                    origen.Open();
                    destino.Open();
                    origen.BackupDatabase(destino);
                }

                if (File.Exists(rutaZip))
                    File.Delete(rutaZip);
                using (var zip = ZipFile.Open(rutaZip, ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(temporal, Path.GetFileName(_rutaBaseDatos));
                    if (Directory.Exists(_carpetaFotos))
                        foreach (string foto in Directory.EnumerateFiles(_carpetaFotos))
                            zip.CreateEntryFromFile(foto, "fotos/" + Path.GetFileName(foto));
                }
            }
            finally
            {
                File.Delete(temporal);
            }
        }
    }
}
