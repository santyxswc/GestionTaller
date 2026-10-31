/**
 * @file ISesionUsuario.cs
 * @brief Contrato de la sesión del usuario.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Models;

namespace TallerMoreno.Core.Sesion
{
    /**
     * @brief Usuario autenticado y reglas de acceso.
     */
    public interface ISesionUsuario
    {
        /** Usuario de la sesión actual; null si no hay sesión. */
        Usuario Usuario { get; }
        /** Identificador del usuario de la sesión, o 0. */
        int UsuarioId { get; }
        /** Momento en que se inició la sesión. */
        DateTime InicioSesion { get; }
        /** Indica si hay una sesión iniciada. */
        bool Activa { get; }
        /** Indica si el usuario de la sesión es administrador. */
        bool EsAdmin { get; }

        /**
         * @brief Abre la sesión de un usuario.
         * @param usuario Usuario autenticado
         */
        void Iniciar(Usuario usuario);

        /** @brief Cierra la sesión actual. */
        void Cerrar();

        /**
         * @brief Verifica que haya una sesión iniciada.
         * @exception UnauthorizedAccessException Si no hay sesión
         */
        void ExigirSesion();

        /**
         * @brief Verifica que el usuario de la sesión sea administrador.
         * @param accion Acción que se intenta, para el mensaje de error ("cambiar precios")
         * @exception UnauthorizedAccessException Si no hay sesión o el usuario no es administrador
         */
        void ExigirAdmin(string accion);
    }
}
