/**
 * @file Archivos.cs
 * @brief Elegir fotos, guardar archivos y abrirlos.
 * @author Santiago Caicedo
 */
using System.Diagnostics;
using Avalonia.Platform.Storage;

namespace TallerMoreno.Desktop.Services
{
    /**
     * @brief Ventanas del sistema para elegir y guardar archivos.
     */
    public static class Archivos
    {
        /** Tipo de archivo de Excel. */
        public static readonly FilePickerFileType Excel = new("Libro de Excel") { Patterns = ["*.xlsx"] };
        /** Tipo de archivo comprimido. */
        public static readonly FilePickerFileType Zip = new("Archivo comprimido") { Patterns = ["*.zip"] };

        /**
         * @brief Pide una imagen.
         * @return Archivo elegido, o null si se canceló
         */
        public static async Task<IStorageFile> ElegirImagen()
        {
            var almacen = Dialogs.Owner?.StorageProvider;
            if (almacen == null) return null;
            var archivos = await almacen.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Elige la foto del repuesto",
                AllowMultiple = false,
                FileTypeFilter = [FilePickerFileTypes.ImageAll]
            });
            return archivos.FirstOrDefault();
        }

        /**
         * @brief Pregunta dónde guardar un archivo.
         * @param nombreSugerido Nombre propuesto
         * @param tipo Tipo de archivo
         * @return Ruta elegida, o null si se canceló
         */
        public static async Task<string> GuardarComo(string nombreSugerido, FilePickerFileType tipo)
        {
            var almacen = Dialogs.Owner?.StorageProvider;
            if (almacen == null) return null;
            var carpeta = await almacen.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
            var archivo = await almacen.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Guardar",
                SuggestedFileName = nombreSugerido,
                SuggestedStartLocation = carpeta,
                DefaultExtension = Path.GetExtension(nombreSugerido).TrimStart('.'),
                FileTypeChoices = [tipo],
                ShowOverwritePrompt = true
            });
            return archivo?.TryGetLocalPath();
        }

        /**
         * @brief Abre un archivo con el programa predeterminado (Excel, explorador...).
         * @param ruta Ruta del archivo
         */
        public static void Abrir(string ruta)
        {
            try
            {
                Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                // Sin programa asociado no se abre, pero el archivo ya quedó guardado.
            }
        }
    }
}
