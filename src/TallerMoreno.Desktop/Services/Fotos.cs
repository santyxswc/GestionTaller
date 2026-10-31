/**
 * @file Fotos.cs
 * @brief Reducción de las fotos antes de guardarlas.
 * @author Santiago Caicedo
 */
using SkiaSharp;

namespace TallerMoreno.Desktop.Services
{
    /**
     * @brief Endereza y reduce las fotos a JPG para que el inventario cargue rápido.
     *
     * Una foto de celular pesa varios MB; reducida a 900 px queda en unos 100 KB y se ve
     * igual de bien en las tarjetas y en el detalle.
     */
    public static class Fotos
    {
        /** Lado mayor máximo en píxeles. */
        private const int LadoMaximo = 900;
        /** Calidad JPG. */
        private const int Calidad = 85;

        /**
         * @brief Lee una imagen, la endereza según su orientación EXIF y la reduce.
         * @param origen Imagen original
         * @return Bytes de la imagen JPG
         * @exception InvalidOperationException Si el archivo no es una imagen válida
         */
        public static byte[] Reducir(Stream origen)
        {
            using var memoria = new MemoryStream();
            origen.CopyTo(memoria);
            memoria.Position = 0;

            using var codec = SKCodec.Create(memoria) ?? throw new InvalidOperationException("El archivo no es una imagen válida.");
            using var original = SKBitmap.Decode(codec) ?? throw new InvalidOperationException("No se pudo leer la imagen.");
            using var derecha = Enderezar(original, codec.EncodedOrigin);

            float escala = Math.Min(1f, (float)LadoMaximo / Math.Max(derecha.Width, derecha.Height));
            var info = new SKImageInfo(Math.Max(1, (int)(derecha.Width * escala)), Math.Max(1, (int)(derecha.Height * escala)));
            using var reducida = escala < 1f ? derecha.Resize(info, SKFilterQuality.High) : derecha.Copy();
            using var imagen = SKImage.FromBitmap(reducida);
            using var datos = imagen.Encode(SKEncodedImageFormat.Jpeg, Calidad);
            return datos.ToArray();
        }

        /**
         * @brief Gira la imagen según cómo se tomó la foto.
         * @param bmp Imagen decodificada
         * @param origen Orientación EXIF
         * @return Imagen derecha (nueva, el llamador la libera)
         */
        private static SKBitmap Enderezar(SKBitmap bmp, SKEncodedOrigin origen)
        {
            int grados = origen switch
            {
                SKEncodedOrigin.BottomRight => 180,
                SKEncodedOrigin.RightTop => 90,
                SKEncodedOrigin.LeftBottom => 270,
                _ => 0
            };
            if (grados == 0)
                return bmp.Copy();

            bool lados = grados != 180;
            var girada = new SKBitmap(lados ? bmp.Height : bmp.Width, lados ? bmp.Width : bmp.Height);
            using var lienzo = new SKCanvas(girada);
            lienzo.Translate(girada.Width / 2f, girada.Height / 2f);
            lienzo.RotateDegrees(grados);
            lienzo.Translate(-bmp.Width / 2f, -bmp.Height / 2f);
            lienzo.DrawBitmap(bmp, 0, 0);
            return girada;
        }
    }
}
