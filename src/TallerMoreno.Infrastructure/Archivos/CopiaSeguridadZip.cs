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
     * aplicación esté abierta. Restaurar usa la misma API en sentido contrario, así SQLite
     * reemplaza los datos sin que queden restos del archivo -wal anterior.
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

        /**
         * @brief Revisa la copia y reemplaza con ella la base de datos y las fotos.
         *
         * Las fotos de la copia se agregan sin borrar las actuales: tienen nombres únicos,
         * así que las que sobren no molestan y no se pierde nada si algo falla a medias.
         * @param rutaZip Archivo .zip creado con Crear
         * @exception InvalidOperationException Si el archivo no es una copia válida
         */
        public void Restaurar(string rutaZip)
        {
            string temporal = Path.Combine(Path.GetTempPath(), $"taller-restaurar-{Guid.NewGuid():N}");
            try
            {
                try
                {
                    ZipFile.ExtractToDirectory(rutaZip, temporal);
                }
                catch (InvalidDataException)
                {
                    throw new InvalidOperationException(NoEsCopia);
                }

                string baseDatos = Path.Combine(temporal, Path.GetFileName(_rutaBaseDatos));
                if (!File.Exists(baseDatos))
                    throw new InvalidOperationException(NoEsCopia);

                using (var origen = new SqliteConnection($"Data Source={baseDatos};Pooling=False"))
                {
                    origen.Open();
                    Revisar(origen);
                    using (var destino = new SqliteConnection($"Data Source={_rutaBaseDatos};Pooling=False"))
                    {
                        destino.Open();
                        origen.BackupDatabase(destino);
                    }
                }

                string fotos = Path.Combine(temporal, "fotos");
                if (Directory.Exists(fotos))
                {
                    Directory.CreateDirectory(_carpetaFotos);
                    foreach (string foto in Directory.EnumerateFiles(fotos))
                        File.Copy(foto, Path.Combine(_carpetaFotos, Path.GetFileName(foto)), overwrite: true);
                }
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                try
                {
                    if (Directory.Exists(temporal))
                        Directory.Delete(temporal, recursive: true);
                }
                catch (IOException)
                {
                    // La carpeta temporal la limpia Windows después.
                }
            }
        }

        /** Mensaje cuando el archivo elegido no es una copia de Taller Moreno. */
        private const string NoEsCopia = "Ese archivo no es una copia de seguridad de Taller Moreno.";

        /**
         * @brief Comprueba que la base de datos de la copia esté sana y sea de Taller Moreno.
         * @param db Conexión abierta a la base de datos de la copia
         * @exception InvalidOperationException Si está dañada, no es del programa o es de una versión más nueva
         */
        private static void Revisar(SqliteConnection db)
        {
            using var cmd = db.CreateCommand();
            try
            {
                cmd.CommandText = "PRAGMA quick_check;";
                if (cmd.ExecuteScalar() as string != "ok")
                    throw new InvalidOperationException("La copia de seguridad está dañada. Prueba con otra más antigua.");
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN ('usuario', 'repuesto', 'venta');";
                if ((long)cmd.ExecuteScalar() != 3)
                    throw new InvalidOperationException(NoEsCopia);
            }
            catch (SqliteException)
            {
                throw new InvalidOperationException(NoEsCopia);
            }

            cmd.CommandText = "PRAGMA user_version;";
            if ((long)cmd.ExecuteScalar() > Database.InicializadorBaseDatos.VersionEsquema)
                throw new InvalidOperationException(Database.InicializadorBaseDatos.MensajeVersionNueva);
        }
    }
}
