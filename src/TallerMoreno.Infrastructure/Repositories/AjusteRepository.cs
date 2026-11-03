/**
 * @file AjusteRepository.cs
 * @brief Acceso a los ajustes generales.
 * @author Santiago Caicedo
 */
using System.Data;
using Dapper;
using TallerMoreno.Core.Abstractions;

namespace TallerMoreno.Infrastructure.Repositories
{
    /**
     * @brief Consultas y cambios de la tabla ajuste.
     */
    public class AjusteRepository : IAjusteRepository
    {
        /** Fábrica de conexiones a la base de datos. */
        private readonly IDbConnectionFactory _conexiones;

        /**
         * @brief Crea el repositorio.
         * @param conexiones Fábrica de conexiones a la base de datos
         */
        public AjusteRepository(IDbConnectionFactory conexiones) => _conexiones = conexiones;

        /**
         * @brief Lee un ajuste.
         * @param clave Nombre del ajuste
         * @return Valor, o null si no existe
         */
        public string Obtener(string clave)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.QueryFirstOrDefault<string>("SELECT valor FROM ajuste WHERE clave = @Clave", new { Clave = clave });
        }

        /**
         * @brief Crea o reemplaza un ajuste.
         * @param clave Nombre del ajuste
         * @param valor Valor
         */
        public void Guardar(string clave, string valor)
        {
            using (IDbConnection db = _conexiones.Crear())
                db.Execute("INSERT INTO ajuste (clave, valor) VALUES (@Clave, @Valor) ON CONFLICT(clave) DO UPDATE SET valor = excluded.valor",
                           new { Clave = clave, Valor = valor });
        }
    }
}
