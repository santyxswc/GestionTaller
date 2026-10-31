/**
 * @file UsuarioService.cs
 * @brief Reglas de negocio de los usuarios y del inicio de sesión.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Abstractions;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Sesion;

namespace TallerMoreno.Core.Services
{
    /**
     * @brief Inicio y cierre de sesión, primer administrador y administración de cuentas.
     */
    public class UsuarioService
    {
        /** Longitud mínima de una contraseña. */
        public const int LongitudMinimaClave = 4;

        /** Acceso a datos de usuarios. */
        private readonly IUsuarioRepository _repo;
        /** Hash de contraseñas. */
        private readonly IHasherClaves _hasher;
        /** Usuario de la sesión. */
        private readonly ISesionUsuario _sesion;

        /**
         * @brief Crea el servicio con sus dependencias.
         * @param repo Acceso a datos
         * @param hasher Hash de contraseñas
         * @param sesion Usuario de la sesión
         */
        public UsuarioService(IUsuarioRepository repo, IHasherClaves hasher, ISesionUsuario sesion)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
            _sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
        }

        /**
         * @brief Indica si ya existe al menos un usuario.
         * @return false en el primer uso de la aplicación
         */
        public bool HayUsuarios() => _repo.Contar() > 0;

        /**
         * @brief Crea la cuenta del dueño en el primer uso e inicia su sesión.
         * @param nombre Nombre completo
         * @param login Nombre de usuario
         * @param clave Contraseña
         * @return Usuario creado
         * @exception InvalidOperationException Si ya hay usuarios
         */
        public Usuario CrearPrimerAdministrador(string nombre, string login, string clave)
        {
            if (HayUsuarios())
                throw new InvalidOperationException("Ya existe un administrador. Inicia sesión con tu usuario.");

            var usuario = Registrar(nombre, login, clave, Usuario.RolAdministrador);
            _sesion.Iniciar(usuario);
            _repo.RegistrarIngreso(usuario.Id);
            return usuario;
        }

        /**
         * @brief Comprueba usuario y contraseña e inicia la sesión.
         * @param login Nombre de usuario
         * @param clave Contraseña
         * @return Usuario autenticado, o null si los datos no son correctos
         * @exception InvalidOperationException Si la cuenta está desactivada
         */
        public Usuario IniciarSesion(string login, string clave)
        {
            var usuario = _repo.GetByLogin(Normalizar(login));
            if (usuario == null || !_hasher.Verificar(clave, usuario.Hash, usuario.Sal))
                return null;
            if (!usuario.Activo)
                throw new InvalidOperationException("Tu cuenta está desactivada. Habla con el administrador del taller.");

            _sesion.Iniciar(usuario);
            _repo.RegistrarIngreso(usuario.Id);
            return usuario;
        }

        /**
         * @brief Cierra la sesión actual.
         */
        public void CerrarSesion() => _sesion.Cerrar();

        /**
         * @brief Obtiene todos los usuarios. Solo administrador.
         * @return Usuarios
         */
        public IEnumerable<Usuario> ObtenerUsuarios()
        {
            _sesion.ExigirAdmin("ver los usuarios");
            return _repo.GetAll();
        }

        /**
         * @brief Crea una cuenta. Solo administrador.
         * @param nombre Nombre completo
         * @param login Nombre de usuario
         * @param clave Contraseña inicial
         * @param rol Administrador o Mecánico
         * @return Usuario creado
         */
        public Usuario CrearUsuario(string nombre, string login, string clave, string rol)
        {
            _sesion.ExigirAdmin("crear usuarios");
            if (!Usuario.Roles.Contains(rol))
                throw new ArgumentException("Selecciona el rol.");
            return Registrar(nombre, login, clave, rol);
        }

        /**
         * @brief Activa o desactiva una cuenta. Solo administrador.
         * @param id Usuario
         * @param activo Nuevo estado
         * @exception InvalidOperationException Si el administrador intenta desactivarse a sí mismo
         */
        public void CambiarEstado(int id, bool activo)
        {
            _sesion.ExigirAdmin("activar o desactivar usuarios");
            if (!activo && id == _sesion.UsuarioId)
                throw new InvalidOperationException("No puedes desactivar tu propia cuenta.");
            _repo.ActualizarEstado(id, activo);
        }

        /**
         * @brief Pone una contraseña nueva a otro usuario. Solo administrador.
         * @param id Usuario
         * @param clave Contraseña nueva
         */
        public void RestablecerClave(int id, string clave)
        {
            _sesion.ExigirAdmin("restablecer contraseñas");
            ValidarClave(clave);
            var (hash, sal) = _hasher.Crear(clave);
            _repo.ActualizarClave(id, hash, sal);
        }

        /**
         * @brief Cambia la contraseña del usuario de la sesión.
         * @param actual Contraseña actual
         * @param nueva Contraseña nueva
         * @exception InvalidOperationException Si la contraseña actual no es correcta
         */
        public void CambiarClave(string actual, string nueva)
        {
            _sesion.ExigirSesion();
            var usuario = _repo.GetById(_sesion.UsuarioId);
            if (usuario == null || !_hasher.Verificar(actual, usuario.Hash, usuario.Sal))
                throw new InvalidOperationException("La contraseña actual no es correcta.");
            ValidarClave(nueva);
            var (hash, sal) = _hasher.Crear(nueva);
            _repo.ActualizarClave(usuario.Id, hash, sal);
        }

        /**
         * @brief Valida los datos y guarda un usuario nuevo.
         * @param nombre Nombre completo
         * @param login Nombre de usuario
         * @param clave Contraseña
         * @param rol Rol
         * @return Usuario creado
         */
        private Usuario Registrar(string nombre, string login, string clave, string rol)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("Escribe el nombre.");
            login = Normalizar(login);
            if (string.IsNullOrEmpty(login) || login.Any(char.IsWhiteSpace))
                throw new ArgumentException("Escribe un usuario sin espacios.");
            if (_repo.GetByLogin(login) != null)
                throw new InvalidOperationException($"Ya existe el usuario '{login}'.");
            ValidarClave(clave);

            var (hash, sal) = _hasher.Crear(clave);
            var usuario = new Usuario { Nombre = nombre.Trim(), Login = login, Rol = rol, Activo = true, Hash = hash, Sal = sal };
            usuario.Id = _repo.Insert(usuario);
            return usuario;
        }

        /**
         * @brief Valida la longitud de una contraseña.
         * @param clave Contraseña
         */
        private static void ValidarClave(string clave)
        {
            if (string.IsNullOrEmpty(clave) || clave.Length < LongitudMinimaClave)
                throw new ArgumentException($"La contraseña debe tener al menos {LongitudMinimaClave} caracteres.");
        }

        /**
         * @brief Quita espacios y pasa a minúsculas el nombre de usuario.
         * @param login Nombre de usuario escrito
         * @return Nombre de usuario normalizado
         */
        private static string Normalizar(string login) => (login ?? "").Trim().ToLowerInvariant();
    }
}
