/**
 * @file SqliteConnectionFactory.cs
 * @brief Fábrica de conexiones a SQLite.
 * @author Santiago Caicedo
 */
using System.Data;
using Microsoft.Data.Sqlite;
using TallerMoreno.Core.Abstractions;

namespace TallerMoreno.Infrastructure.Database
{
    /**
     * @brief Crea conexiones al archivo de la base de datos con las llaves foráneas activas.
     */
    public sealed class SqliteConnectionFactory : IDbConnectionFactory
    {
        /** Cadena de conexión. */
        private readonly string _cadena;

        /** Registra los conversores de Dapper una sola vez. */
        static SqliteConnectionFactory() => ConfiguracionDapper.Registrar();

        /**
         * @brief Crea la fábrica.
         * @param rutaBaseDatos Ruta del archivo .db (se crea si no existe)
         */
        public SqliteConnectionFactory(string rutaBaseDatos)
        {
            RutaBaseDatos = rutaBaseDatos;
            _cadena = new SqliteConnectionStringBuilder
            {
                DataSource = rutaBaseDatos,
                ForeignKeys = true,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();
        }

        /** Ruta del archivo de la base de datos. */
        public string RutaBaseDatos { get; }

        /**
         * @brief Crea una conexión nueva (cerrada).
         * @return Conexión lista para usar con Dapper
         */
        public IDbConnection Crear() => new SqliteConnection(_cadena);
    }
}
