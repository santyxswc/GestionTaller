/**
 * @file IDbConnectionFactory.cs
 * @brief Contrato para obtener conexiones a la base de datos.
 * @author Santiago Caicedo
 */
using System.Data;

namespace TallerMoreno.Core.Abstractions
{
    /**
     * @brief Crea conexiones nuevas; cada repositorio abre y cierra la suya por operación.
     */
    public interface IDbConnectionFactory
    {
        /**
         * @brief Crea una conexión nueva (cerrada).
         * @return Conexión lista para usar con Dapper
         */
        IDbConnection Crear();
    }
}
