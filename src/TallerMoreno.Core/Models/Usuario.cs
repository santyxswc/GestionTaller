/**
 * @file Usuario.cs
 * @brief Modelo de usuario del sistema.
 * @author Santiago Caicedo
 */
namespace TallerMoreno.Core.Models
{
    /**
     * @brief Cuenta que inicia sesión en la aplicación (usuario).
     */
    public class Usuario
    {
        /** Rol con acceso completo: precios, inventario, reportes y usuarios. */
        public const string RolAdministrador = "Administrador";
        /** Rol de quien vende y recibe mercancía en el taller. */
        public const string RolMecanico = "Mecánico";

        /** Roles que se pueden asignar. */
        public static readonly string[] Roles = { RolMecanico, RolAdministrador };

        /** Identificador. */
        public int Id { get; set; }
        /** Nombre completo para mostrar. */
        public string Nombre { get; set; }
        /** Nombre de usuario con el que inicia sesión, en minúsculas. */
        public string Login { get; set; }
        /** Administrador o Mecánico. */
        public string Rol { get; set; }
        /** Indica si puede iniciar sesión. */
        public bool Activo { get; set; }
        /** Fecha del último inicio de sesión. */
        public DateTime? UltimoIngreso { get; set; }

        /** Hash PBKDF2 de la contraseña (Base64). Nunca se muestra. */
        public string Hash { get; set; }
        /** Sal del hash (Base64). */
        public string Sal { get; set; }

        /** Indica si tiene rol de administrador. */
        public bool EsAdmin => Rol == RolAdministrador;
        /** Activo o Inactivo. */
        public string Estado => Activo ? "Activo" : "Inactivo";
    }
}
