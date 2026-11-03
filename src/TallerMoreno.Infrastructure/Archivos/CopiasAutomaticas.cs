/**
 * @file CopiasAutomaticas.cs
 * @brief Copias de seguridad diarias sin que nadie tenga que acordarse.
 * @author Santiago Caicedo
 */
using System.Globalization;
using TallerMoreno.Core.Abstractions;

namespace TallerMoreno.Infrastructure.Archivos
{
    /**
     * @brief Hace una copia al día en la carpeta "copias" y guarda solo las más recientes.
     *
     * Si se configuró una carpeta extra (una USB o una carpeta de OneDrive), la copia también
     * se deja allí: así un daño del disco no se lleva los datos y sus copias a la vez. Sincronizar
     * el .zip terminado es seguro; lo que no se debe sincronizar es la base de datos abierta.
     */
    public sealed class CopiasAutomaticas
    {
        /** Copias diarias que se conservan. */
        public const int CopiasGuardadas = 14;

        /** Prefijo de las copias diarias. */
        private const string Prefijo = "diaria-";

        /** Copias de seguridad. */
        private readonly ICopiaSeguridad _copia;
        /** Carpeta extra opcional donde también se dejan las copias. */
        private readonly string _carpetaExtra;
        /** Evita que dos copias se hagan a la vez. */
        private readonly SemaphoreSlim _candado = new(1, 1);

        /**
         * @brief Crea el servicio.
         * @param copia Copias de seguridad
         * @param carpeta Carpeta de las copias
         * @param carpetaExtra Carpeta adicional, o null
         */
        public CopiasAutomaticas(ICopiaSeguridad copia, string carpeta, string carpetaExtra)
        {
            _copia = copia;
            Carpeta = Path.GetFullPath(carpeta);
            _carpetaExtra = string.IsNullOrWhiteSpace(carpetaExtra) ? null : Path.GetFullPath(carpetaExtra);
        }

        /** Carpeta de las copias. */
        public string Carpeta { get; }

        /** Último problema al dejar la copia en la carpeta extra (USB desconectada, etc.), o null. */
        public Exception ErrorCarpetaExtra { get; private set; }

        /**
         * @brief Hace la copia del día si todavía no existe y borra las más viejas.
         * @param hoy Fecha de hoy
         * @return Ruta de la copia creada, o null si ya existía
         */
        public string HacerSiFalta(DateTime hoy)
        {
            _candado.Wait();
            try
            {
                string ruta = Path.Combine(Carpeta, $"{Prefijo}{hoy:yyyy-MM-dd}.zip");
                if (File.Exists(ruta))
                    return null;

                Escribir(ruta);
                CopiarAExtra(ruta);
                BorrarViejas(Carpeta);
                if (_carpetaExtra != null && Directory.Exists(_carpetaExtra))
                    BorrarViejas(_carpetaExtra);
                return ruta;
            }
            finally
            {
                _candado.Release();
            }
        }

        /**
         * @brief Hace una copia con nombre propio antes de un cambio grande (restaurar, actualizar).
         *
         * Estas copias no se borran solas.
         * @param motivo Parte del nombre, como "antes-de-restaurar"
         * @return Ruta de la copia
         */
        public string HacerAntesDe(string motivo)
        {
            _candado.Wait();
            try
            {
                string ruta = Path.Combine(Carpeta, $"{motivo}-{DateTime.Now:yyyy-MM-dd-HHmmss}.zip");
                Escribir(ruta);
                return ruta;
            }
            finally
            {
                _candado.Release();
            }
        }

        /**
         * @brief Crea la copia con un nombre temporal y la renombra al terminar.
         *
         * Si el programa se cierra a mitad, no queda un .zip incompleto con el nombre del día.
         * @param ruta Ruta final
         */
        private void Escribir(string ruta)
        {
            Directory.CreateDirectory(Carpeta);
            string temporal = ruta + ".tmp";
            _copia.Crear(temporal);
            File.Move(temporal, ruta, overwrite: true);
        }

        /**
         * @brief Deja la copia también en la carpeta extra; si no está disponible, lo anota y sigue.
         * @param ruta Copia recién creada
         */
        private void CopiarAExtra(string ruta)
        {
            if (_carpetaExtra == null)
                return;
            try
            {
                Directory.CreateDirectory(_carpetaExtra);
                File.Copy(ruta, Path.Combine(_carpetaExtra, Path.GetFileName(ruta)), overwrite: true);
                ErrorCarpetaExtra = null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ErrorCarpetaExtra = ex;
            }
        }

        /**
         * @brief Borra las copias diarias más viejas y deja las CopiasGuardadas más recientes.
         * @param carpeta Carpeta de copias
         */
        private static void BorrarViejas(string carpeta)
        {
            var viejas = Directory.EnumerateFiles(carpeta, Prefijo + "*.zip")
                .Where(r => DateTime.TryParseExact(Path.GetFileNameWithoutExtension(r)[Prefijo.Length..], "yyyy-MM-dd",
                                                   CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                .OrderByDescending(r => Path.GetFileName(r), StringComparer.Ordinal)
                .Skip(CopiasGuardadas);
            foreach (string vieja in viejas)
            {
                try
                {
                    File.Delete(vieja);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // Si una copia vieja está en uso se borra la próxima vez.
                }
            }
        }
    }
}
