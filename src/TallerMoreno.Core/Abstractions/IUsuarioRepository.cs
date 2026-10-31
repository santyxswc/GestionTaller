/**
 * @file IUsuarioRepository.cs
 * @brief Contrato del repositorio de usuarios.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Models;

namespace TallerMoreno.Core.Abstractions
{
    /**
     * @brief Acceso a datos de usuarios.
     */
    public interface IUsuarioRepository
    {
        /**
         * @brief Cuenta los usuarios registrados.
         * @return Número de usuarios
         */
        int Contar();

        /**
         * @brief Obtiene todos los usuarios ordenados por nombre.
         * @return Usuarios
         */
        IEnumerable<Usuario> GetAll();

        /**
         * @brief Busca un usuario por su nombre de usuario.
         * @param login Nombre de usuario en minúsculas
         * @return Usuario, o null si no existe
         */
        Usuario GetByLogin(string login);

        /**
         * @brief Busca un usuario.
         * @param id Identificador
         * @return Usuario, o null si no existe
         */
        Usuario GetById(int id);

        /**
         * @brief Registra un usuario.
         * @param u Datos del usuario con el hash de su contraseña
         * @return Identificador asignado
         */
        int Insert(Usuario u);

        /**
         * @brief Cambia la contraseña.
         * @param id Usuario
         * @param hash Hash nuevo
         * @param sal Sal nueva
         */
        void ActualizarClave(int id, string hash, string sal);

        /**
         * @brief Activa o desactiva una cuenta.
         * @param id Usuario
         * @param activo Nuevo estado
         */
        void ActualizarEstado(int id, bool activo);

        /**
         * @brief Guarda la fecha del último inicio de sesión.
         * @param id Usuario
         */
        void RegistrarIngreso(int id);
    }
}
