/**
 * @file UsuarioRepository.cs
 * @brief Acceso a datos de usuarios.
 * @author Santiago Caicedo
 */
using System.Data;
using Dapper;
using TallerMoreno.Core.Abstractions;
using TallerMoreno.Core.Models;
using TallerMoreno.Infrastructure.Database;

namespace TallerMoreno.Infrastructure.Repositories
{
    /**
     * @brief Consultas y cambios de la tabla usuario.
     */
    public class UsuarioRepository : IUsuarioRepository
    {
        /** Fábrica de conexiones a la base de datos. */
        private readonly IDbConnectionFactory _conexiones;

        /** Columnas de un usuario. */
        private const string SelectSql = @"
            SELECT id AS Id, nombre AS Nombre, login AS Login, rol AS Rol, activo AS Activo,
                   ultimo_ingreso AS UltimoIngreso, hash AS Hash, sal AS Sal
            FROM usuario";

        /**
         * @brief Crea el repositorio.
         * @param conexiones Fábrica de conexiones a la base de datos
         */
        public UsuarioRepository(IDbConnectionFactory conexiones) => _conexiones = conexiones;

        /**
         * @brief Cuenta los usuarios registrados.
         * @return Número de usuarios
         */
        public int Contar()
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.ExecuteScalar<int>("SELECT COUNT(*) FROM usuario");
        }

        /**
         * @brief Obtiene todos los usuarios ordenados por nombre.
         * @return Usuarios
         */
        public IEnumerable<Usuario> GetAll()
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Query<Usuario>(SelectSql + " ORDER BY nombre COLLATE NOCASE").ToList();
        }

        /**
         * @brief Busca un usuario por su nombre de usuario.
         * @param login Nombre de usuario en minúsculas
         * @return Usuario, o null si no existe
         */
        public Usuario GetByLogin(string login)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.QueryFirstOrDefault<Usuario>(SelectSql + " WHERE login = @Login", new { Login = login });
        }

        /**
         * @brief Busca un usuario.
         * @param id Identificador
         * @return Usuario, o null si no existe
         */
        public Usuario GetById(int id)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.QueryFirstOrDefault<Usuario>(SelectSql + " WHERE id = @Id", new { Id = id });
        }

        /**
         * @brief Registra un usuario.
         * @param u Datos del usuario con el hash de su contraseña
         * @return Identificador asignado
         */
        public int Insert(Usuario u)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                db.Open();
                db.Execute(@"INSERT INTO usuario (nombre, login, rol, activo, hash, sal)
                             VALUES (@Nombre, @Login, @Rol, @Activo, @Hash, @Sal)", u);
                return Sql.UltimoId(db);
            }
        }

        /**
         * @brief Cambia la contraseña.
         * @param id Usuario
         * @param hash Hash nuevo
         * @param sal Sal nueva
         */
        public void ActualizarClave(int id, string hash, string sal)
        {
            using (IDbConnection db = _conexiones.Crear())
                db.Execute("UPDATE usuario SET hash = @Hash, sal = @Sal WHERE id = @Id", new { Id = id, Hash = hash, Sal = sal });
        }

        /**
         * @brief Activa o desactiva una cuenta.
         * @param id Usuario
         * @param activo Nuevo estado
         */
        public void ActualizarEstado(int id, bool activo)
        {
            using (IDbConnection db = _conexiones.Crear())
                db.Execute("UPDATE usuario SET activo = @Activo WHERE id = @Id", new { Id = id, Activo = activo });
        }

        /**
         * @brief Guarda la fecha del último inicio de sesión.
         * @param id Usuario
         */
        public void RegistrarIngreso(int id)
        {
            using (IDbConnection db = _conexiones.Crear())
                db.Execute("UPDATE usuario SET ultimo_ingreso = @Ahora WHERE id = @Id", new { Id = id, Ahora = Sql.Ahora() });
        }
    }
}
