/**
 * @file Errores.cs
 * @brief Mensajes claros para los errores y registro de los inesperados.
 * @author Santiago Caicedo
 */
using System.Reflection;
using System.Text;
using Avalonia.Threading;
using Microsoft.Data.Sqlite;

namespace TallerMoreno.Desktop.Services
{
    /**
     * @brief Traduce cualquier error a un mensaje que entienda quien usa el programa y anota los inesperados.
     *
     * Las reglas del negocio (Core) ya traen su mensaje en español y se muestran tal cual. Lo demás
     * (base de datos ocupada, disco lleno, archivo abierto en Excel, fallos del programa) se explica
     * con palabras sencillas y queda anotado en errores.log, en la carpeta de datos, para poder
     * revisarlo después sin depender de lo que el usuario recuerde.
     */
    public static class Errores
    {
        /** Nombre del archivo de registro. */
        public const string ArchivoRegistro = "errores.log";

        /** Tamaño a partir del cual el registro se renombra a errores.anterior.log. */
        private const long TamanoMaximo = 1024 * 1024;

        /** Evita que dos hilos escriban el registro a la vez. */
        private static readonly object Candado = new();

        /** Carpeta donde se guarda el registro. */
        private static string _carpeta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TallerMoreno");

        /** Ruta completa del registro. */
        public static string RutaRegistro => Path.GetFullPath(Path.Combine(_carpeta, ArchivoRegistro));

        /**
         * @brief Cambia la carpeta del registro a la carpeta de datos configurada.
         * @param carpeta Carpeta de datos; vacía deja la de por defecto
         */
        public static void UsarCarpeta(string carpeta)
        {
            if (!string.IsNullOrWhiteSpace(carpeta))
                _carpeta = carpeta;
        }

        /**
         * @brief Atrapa los errores que nadie atrapó para que el programa no se cierre sin avisar.
         *
         * Los del hilo de la ventana se muestran y el programa sigue abierto. Los de otros hilos
         * solo se pueden anotar: .NET cierra el programa después de este evento.
         */
        public static void AtraparNoControlados()
        {
            Dispatcher.UIThread.UnhandledException += (_, e) =>
            {
                e.Handled = true;
                Mostrar(e.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                Registrar(e.ExceptionObject as Exception, "Error no controlado; el programa se cerró");
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                Registrar(e.Exception, "Tarea en segundo plano");
                e.SetObserved();
            };
        }

        /**
         * @brief Mensaje para mostrar al usuario; anota el error si no es una regla del negocio.
         * @param ex Error ocurrido
         * @return Texto claro, sin detalles técnicos
         */
        public static string Mensaje(Exception ex)
        {
            if (EsReglaDelNegocio(ex))
                return ex.Message;

            Registrar(ex);
            return ex switch
            {
                SqliteException { SqliteErrorCode: 5 or 6 } =>
                    "La base de datos está ocupada en este momento. Espera unos segundos e inténtalo de nuevo.",
                SqliteException { SqliteErrorCode: 13 } =>
                    "El disco está lleno. Libera espacio en el computador e inténtalo de nuevo.",
                SqliteException { SqliteErrorCode: 11 or 26 } =>
                    "La base de datos está dañada. Cierra el programa y restaura la última copia de seguridad.",
                SqliteException { SqliteErrorCode: 8 or 14 } =>
                    "No se pudo abrir la base de datos. Revisa que la carpeta de datos exista y no sea de solo lectura.",
                IOException io when EsDiscoLleno(io) =>
                    "El disco está lleno. Libera espacio en el computador e inténtalo de nuevo.",
                IOException =>
                    "No se pudo leer o guardar un archivo. Si está abierto en otro programa, ciérralo e inténtalo de nuevo.",
                UnauthorizedAccessException =>
                    "Windows no dejó usar ese archivo o carpeta. Prueba con otra carpeta.",
                _ => "Ocurrió un error inesperado y quedó anotado. Si se repite, avísale al encargado del programa."
            };
        }

        /**
         * @brief Muestra el mensaje del error en una ventana.
         * @param ex Error ocurrido
         */
        public static async void Mostrar(Exception ex)
        {
            string mensaje = Mensaje(ex);
            try
            {
                await Dialogs.Error(mensaje);
            }
            catch (Exception otro)
            {
                // Si ni siquiera se puede abrir la ventana, al menos queda anotado.
                Registrar(otro, "No se pudo mostrar el error");
            }
        }

        /**
         * @brief Anota un error en errores.log con la fecha, la versión y el detalle técnico.
         * @param ex Error ocurrido
         * @param contexto Dónde pasó, si se sabe
         */
        public static void Registrar(Exception ex, string contexto = null)
        {
            if (ex == null) return;
            try
            {
                var texto = new StringBuilder()
                    .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                    .Append(" | v").Append(Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3))
                    .Append(contexto == null ? "" : " | " + contexto)
                    .AppendLine()
                    .AppendLine(ex.ToString())
                    .AppendLine();

                lock (Candado)
                {
                    Directory.CreateDirectory(_carpeta);
                    var archivo = new FileInfo(RutaRegistro);
                    if (archivo.Exists && archivo.Length > TamanoMaximo)
                        File.Move(archivo.FullName, Path.Combine(_carpeta, "errores.anterior.log"), overwrite: true);
                    File.AppendAllText(RutaRegistro, texto.ToString(), Encoding.UTF8);
                }
            }
            catch (Exception)
            {
                // Si no se puede escribir el registro no hay a dónde más anotarlo; el programa sigue.
            }
        }

        /**
         * @brief Indica si el error es una regla del programa con su mensaje ya escrito para el usuario.
         *
         * Core lanza ArgumentException, InvalidOperationException o UnauthorizedAccessException con
         * mensajes en español; se reconocen porque salen de código de TallerMoreno. Los mismos tipos
         * lanzados por .NET traen mensajes técnicos y se tratan como inesperados.
         * @param ex Error ocurrido
         * @return true si el mensaje se puede mostrar tal cual
         */
        private static bool EsReglaDelNegocio(Exception ex) =>
            (ex is (ArgumentException and not ArgumentNullException and not ArgumentOutOfRangeException)
                or InvalidOperationException
                or UnauthorizedAccessException)
            && ex.TargetSite?.DeclaringType?.Namespace?.StartsWith("TallerMoreno", StringComparison.Ordinal) == true;

        /**
         * @brief Indica si el error de archivo se debe a que el disco se llenó.
         * @param ex Error de archivo
         * @return true si no hay espacio en el disco
         */
        private static bool EsDiscoLleno(IOException ex) =>
            (ex.HResult & 0xFFFF) is 0x27 or 0x70;
    }
}
