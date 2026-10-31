/**
 * @file Convertidores.cs
 * @brief Conversores de datos para las vistas.
 * @author Santiago Caicedo
 */
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
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

        /** Categoría de repuesto a un ícono para cuando no hay foto. */
        public static readonly IValueConverter IconoCategoria = new FuncValueConverter<string, string>(categoria => categoria switch
        {
            "Baterías" => "🔋",
            "Llantas" => "🚗",
            "Aceites y lubricantes" => "🛢️",
            "Filtros" => "🌀",
            "Frenos" => "🛑",
            "Suspensión" => "🔩",
            "Motor" => "⚙️",
            "Eléctricos" => "⚡",
            "Luces" => "💡",
            "Correas" => "➰",
            "Refrigeración" => "❄️",
            "Accesorios" => "🧰",
            _ => "📦"
        });

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
