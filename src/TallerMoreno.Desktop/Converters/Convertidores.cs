/**
 * @file Convertidores.cs
 * @brief Conversores de datos para las vistas.
 * @author Santiago Caicedo
 */
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using TallerMoreno.Core;
using TallerMoreno.Core.Abstractions;

namespace TallerMoreno.Desktop.Converters
{
    /**
     * @brief Conversores que se usan desde XAML con x:Static.
     *
     * Ningún repuesto se queda sin imagen: se muestra su foto si se puede leer; si no tiene,
     * o el archivo se borró o está dañado, se muestra una foto real de referencia elegida por
     * su nombre y su categoría.
     */
    public static class Convertidores
    {
        /** Almacén de fotos; lo asigna App al arrancar. */
        public static IAlmacenFotos Fotos { get; set; }

        /** Ancho por defecto al que se reducen las fotos, en píxeles. */
        private const int AnchoPorDefecto = 280;
        /** Máximo de fotos propias en memoria. */
        private const int MaximoEnMemoria = 400;

        /** Nombre de archivo de foto a imagen; el parámetro es el ancho en píxeles (por defecto 280). */
        public static readonly IValueConverter Foto =
            new FuncValueConverter<string, string, Bitmap>((nombre, ancho) => CargarFoto(nombre, Ancho(ancho)));

        /** Indica si la foto propia del repuesto existe y se puede leer. */
        public static readonly IValueConverter FotoLegible =
            new FuncValueConverter<string, bool>(nombre => CargarFoto(nombre, AnchoPorDefecto) != null);

        /** Lo contrario de FotoLegible: hay que mostrar la foto de referencia. */
        public static readonly IValueConverter SinFotoLegible =
            new FuncValueConverter<string, bool>(nombre => CargarFoto(nombre, AnchoPorDefecto) == null);

        /**
         * @brief Nombre y categoría del repuesto a una foto real de referencia.
         *
         * Se usa con un MultiBinding de Nombre y Categoría, en ese orden; FotoReferencia decide cuál.
         * Las fotos van dentro del programa (Assets/Categorias) y sus créditos están en docs/CREDITOS-IMAGENES.md.
         */
        public static readonly IMultiValueConverter ImagenReferencia = new ConvertidorReferencia();

        /** Fotos de referencia ya cargadas, por nombre de archivo. */
        private static readonly Dictionary<string, Bitmap> _referencias = new();
        /** Fotos propias ya cargadas, por nombre y ancho; null si no se pudieron leer. */
        private static readonly Dictionary<string, Bitmap> _fotos = new();

        /**
         * @brief Carga una foto de referencia; si no se puede, usa la de "otros".
         * @param archivo Nombre del archivo en Assets/Categorias, sin extensión
         * @return Foto, o null solo si ni siquiera la de "otros" se puede cargar
         */
        public static Bitmap CargarReferencia(string archivo)
        {
            if (_referencias.TryGetValue(archivo, out var imagen))
                return imagen;
            try
            {
                using var recurso = AssetLoader.Open(new Uri($"avares://TallerMoreno/Assets/Categorias/{archivo}.jpg"));
                imagen = new Bitmap(recurso);
            }
            catch (Exception ex) when (archivo != FotoReferencia.Otros)
            {
                Services.Errores.Registrar(ex, $"Foto de referencia '{archivo}'");
                imagen = CargarReferencia(FotoReferencia.Otros);
            }
            _referencias[archivo] = imagen;
            return imagen;
        }

        /**
         * @brief Carga la foto propia de un repuesto reducida al ancho pedido.
         *
         * Cada nombre de foto es único (se cambia al reemplazar la foto), así que la memoria
         * nunca muestra una foto vieja. También se recuerda cuando una foto no se pudo leer.
         * @param nombre Nombre del archivo de la foto
         * @param ancho Ancho en píxeles
         * @return Imagen, o null si no hay foto, el archivo no está o no se puede leer
         */
        private static Bitmap CargarFoto(string nombre, int ancho)
        {
            if (string.IsNullOrWhiteSpace(nombre) || Fotos == null)
                return null;

            string clave = nombre + "|" + ancho;
            if (_fotos.TryGetValue(clave, out var guardada))
                return guardada;

            Bitmap imagen = null;
            string ruta = Fotos.Ruta(nombre);
            if (ruta != null && File.Exists(ruta))
            {
                try
                {
                    using var archivo = File.OpenRead(ruta);
                    imagen = Bitmap.DecodeToWidth(archivo, ancho, BitmapInterpolationMode.HighQuality);
                }
                catch (Exception ex) when (ex is IOException or ArgumentException or InvalidOperationException
                                               or UnauthorizedAccessException or NotSupportedException)
                {
                    // Foto dañada o de un formato que no se puede leer: se muestra la de referencia.
                }
                catch (Exception ex) when (ex.GetType().Name == "SKException" || ex is NullReferenceException)
                {
                    // El decodificador de imágenes también falla así con archivos dañados.
                }
            }

            if (_fotos.Count >= MaximoEnMemoria)
                _fotos.Clear();
            _fotos[clave] = imagen;
            return imagen;
        }

        /**
         * @brief Lee el ancho pedido en el ConverterParameter.
         * @param parametro Texto con el ancho, o null
         * @return Ancho en píxeles
         */
        private static int Ancho(string parametro) =>
            int.TryParse(parametro, out int ancho) && ancho > 0 ? ancho : AnchoPorDefecto;

        /**
         * @brief Elige la foto de referencia por nombre y categoría.
         *
         * Se leen los valores por posición: si el nombre viene vacío, la categoría no se corre de lugar.
         */
        private sealed class ConvertidorReferencia : IMultiValueConverter
        {
            /**
             * @brief Convierte [Nombre, Categoría] en la foto de referencia.
             * @return Foto de referencia
             */
            public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
            {
                string nombre = values.Count > 0 ? values[0] as string : null;
                string categoria = values.Count > 1 ? values[1] as string : null;
                return CargarReferencia(FotoReferencia.Elegir(nombre, categoria));
            }
        }
    }
}
