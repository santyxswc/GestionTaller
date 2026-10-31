/**
 * @file Convertidores.cs
 * @brief Conversores de datos para las vistas.
 * @author Santiago Caicedo
 */
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using TallerMoreno.Core.Abstractions;

namespace TallerMoreno.Desktop.Converters
{
    /**
     * @brief Conversores que se usan desde XAML con x:Static.
     */
    public static class Convertidores
    {
        /** Almacén de fotos; lo asigna App al arrancar. */
        public static IAlmacenFotos Fotos { get; set; }

        /** Nombre de archivo de foto a imagen; el parámetro es el ancho en píxeles (por defecto 280). */
        public static readonly IValueConverter Foto = new ConvertidorFoto();

        /**
         * @brief Categoría de repuesto a una foto real de referencia, para cuando el repuesto no tiene foto.
         *
         * Las fotos van dentro del programa (Assets/Categorias) y sus créditos están en docs/CREDITOS-IMAGENES.md.
         */
        public static readonly IValueConverter ImagenCategoria = new FuncValueConverter<string, Bitmap>(ImagenDe);

        /** Fotos de categoría ya cargadas, por nombre de archivo. */
        private static readonly Dictionary<string, Bitmap> _categorias = new();

        /**
         * @brief Carga la foto de referencia de una categoría.
         * @param categoria Nombre de la categoría
         * @return Foto de la categoría, o la de "Otros" si no tiene una propia
         */
        private static Bitmap ImagenDe(string categoria)
        {
            string archivo = categoria switch
            {
                "Baterías" => "baterias",
                "Llantas" => "llantas",
                "Aceites y lubricantes" => "aceites",
                "Filtros" => "filtros",
                "Frenos" => "frenos",
                "Suspensión" => "suspension",
                "Motor" => "motor",
                "Eléctricos" => "electricos",
                "Luces" => "luces",
                "Correas" => "correas",
                "Refrigeración" => "refrigeracion",
                "Accesorios" => "accesorios",
                _ => "otros"
            };
            if (!_categorias.TryGetValue(archivo, out var imagen))
            {
                using var recurso = AssetLoader.Open(new Uri($"avares://TallerMoreno/Assets/Categorias/{archivo}.jpg"));
                imagen = new Bitmap(recurso);
                _categorias[archivo] = imagen;
            }
            return imagen;
        }

        /**
         * @brief Carga las fotos reducidas al ancho pedido y las guarda en memoria.
         *
         * Cada nombre de foto es único (se cambia al reemplazar la foto), así que la memoria
         * nunca muestra una foto vieja.
         */
        private sealed class ConvertidorFoto : IValueConverter
        {
            /** Máximo de imágenes en memoria. */
            private const int Maximo = 400;
            /** Imágenes ya cargadas por nombre y ancho. */
            private readonly Dictionary<string, Bitmap> _cache = new();

            /**
             * @brief Convierte el nombre de la foto en imagen.
             * @return Imagen, o null si no hay foto o no se puede leer
             */
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is not string nombre || string.IsNullOrWhiteSpace(nombre) || Fotos == null)
                    return null;

                int ancho = int.TryParse(parameter as string, out int a) ? a : 280;
                string clave = nombre + "|" + ancho;
                if (_cache.TryGetValue(clave, out var guardada))
                    return guardada;

                string ruta = Fotos.Ruta(nombre);
                if (ruta == null || !File.Exists(ruta))
                    return null;
                try
                {
                    using var archivo = File.OpenRead(ruta);
                    var imagen = Bitmap.DecodeToWidth(archivo, ancho, BitmapInterpolationMode.HighQuality);
                    if (_cache.Count >= Maximo)
                        _cache.Clear();
                    _cache[clave] = imagen;
                    return imagen;
                }
                catch (Exception ex) when (ex is IOException or ArgumentException or InvalidOperationException)
                {
                    return null;
                }
            }

            /**
             * @brief No se usa: las fotos son de solo lectura en las vistas.
             */
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
                throw new NotSupportedException();
        }
    }
}
