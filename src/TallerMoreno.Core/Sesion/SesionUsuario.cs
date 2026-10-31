/**
 * @file SesionUsuario.cs
 * @brief Sesión del usuario que está usando la aplicación.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Models;

namespace TallerMoreno.Core.Sesion
{
    /**
     * @brief Usuario autenticado en la aplicación y reglas de acceso.
     *
     * La inician y la cierran UsuarioService.IniciarSesion y UsuarioService.CerrarSesion.
     * Hay una sola instancia en la aplicación; los servicios la usan para validar permisos
     * y para dejar a nombre de quien quedan las ventas y movimientos.
     */
    public sealed class SesionUsuario : ISesionUsuario
    {
        /** Usuario de la sesión actual; null si no hay sesión. */
        public Usuario Usuario { get; private set; }

        /** Momento en que se inició la sesión. */
        public DateTime InicioSesion { get; private set; }

        /** Identificador del usuario de la sesión, o 0. */
        public int UsuarioId => Usuario?.Id ?? 0;
        /** Indica si hay una sesión iniciada. */
        public bool Activa => Usuario != null;
        /** Indica si el usuario de la sesión es administrador. */
        public bool EsAdmin => Usuario?.EsAdmin == true;

        /**
         * @brief Abre la sesión de un usuario.
         * @param usuario Usuario autenticado
         */
        public void Iniciar(Usuario usuario)
        {
            Usuario = usuario;
            InicioSesion = DateTime.Now;
        }

        /**
         * @brief Cierra la sesión actual.
         */
        public void Cerrar() => Usuario = null;

        /**
         * @brief Verifica que haya una sesión iniciada.
         * @exception UnauthorizedAccessException Si no hay sesión
         */
        public void ExigirSesion()
        {
            if (!Activa)
                throw new UnauthorizedAccessException("Debes iniciar sesión para realizar esta operación.");
        }

        /**
         * @brief Verifica que el usuario de la sesión sea administrador.
         * @param accion Acción que se intenta, para el mensaje de error ("cambiar precios")
         * @exception UnauthorizedAccessException Si no hay sesión o el usuario no es administrador
         */
        public void ExigirAdmin(string accion)
        {
            ExigirSesion();
            if (!EsAdmin)
                throw new UnauthorizedAccessException($"Solo el administrador puede {accion}.");
        }
    }
}
