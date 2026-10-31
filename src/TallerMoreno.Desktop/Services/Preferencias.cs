/**
 * @file Preferencias.cs
 * @brief Datos que la aplicación recuerda entre usos.
 * @author Santiago Caicedo
 */
namespace TallerMoreno.Desktop.Services
{
    /**
     * @brief Recuerda el último usuario que inició sesión, para no tener que escribirlo cada vez.
     */
    public sealed class Preferencias
    {
        /** Archivo donde se guarda el último usuario. */
        private readonly string _archivoUsuario;

        /**
         * @brief Crea las preferencias.
         * @param carpetaDatos Carpeta de datos de la aplicación
         */
        public Preferencias(string carpetaDatos)
        {
            CarpetaDatos = carpetaDatos;
            _archivoUsuario = Path.Combine(carpetaDatos, "ultimo-usuario.txt");
        }

        /** Carpeta de datos de la aplicación. */
        public string CarpetaDatos { get; }

        /** Último usuario que entró, o vacío. */
        public string UltimoUsuario
        {
            get
            {
                try
                {
                    return File.Exists(_archivoUsuario) ? File.ReadAllText(_archivoUsuario).Trim() : "";
                }
                catch (IOException)
                {
                    return "";
                }
            }
            set
            {
                try
                {
                    File.WriteAllText(_archivoUsuario, value ?? "");
                }
                catch (IOException)
                {
                    // Recordar el usuario es una comodidad; si falla, se escribe a mano la próxima vez.
                }
            }
        }
    }
}
