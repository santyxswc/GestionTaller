/**
 * @file ClienteRepository.cs
 * @brief Acceso a datos de clientes.
 * @author Santiago Caicedo
 */
using System.Data;
using Dapper;
using TallerMoreno.Core;
using TallerMoreno.Core.Abstractions;
using TallerMoreno.Core.Models;
using TallerMoreno.Infrastructure.Database;

namespace TallerMoreno.Infrastructure.Repositories
{
    /**
     * @brief Consultas y cambios de la tabla cliente.
     */
    public class ClienteRepository : IClienteRepository
    {
        /** Fábrica de conexiones a la base de datos. */
        private readonly IDbConnectionFactory _conexiones;

        /** Clientes con el total, el número y la fecha de sus compras no anuladas. */
        private const string SelectSql = @"
            SELECT c.id AS Id, c.nombre AS Nombre, c.documento AS Documento, c.telefono AS Telefono,
                   c.placa AS Placa, c.vehiculo AS Vehiculo, c.notas AS Notas,
                   COALESCE(SUM(v.total), 0) AS TotalComprado,
                   COUNT(v.id) AS NumeroCompras,
                   MAX(v.fecha) AS UltimaCompra
            FROM cliente c
            LEFT JOIN venta v ON v.cliente_id = c.id AND v.anulada = 0";

        /**
         * @brief Crea el repositorio.
         * @param conexiones Fábrica de conexiones a la base de datos
         */
        public ClienteRepository(IDbConnectionFactory conexiones) => _conexiones = conexiones;

        /**
         * @brief Busca clientes con lo que ha comprado cada uno.
         * @param texto Nombre, documento, teléfono o placa (sin importar tildes ni mayúsculas); vacío devuelve todos
         * @return Clientes ordenados por nombre
         */
        public IEnumerable<Cliente> Buscar(string texto)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                var clientes = db.Query<Cliente>(SelectSql + " GROUP BY c.id ORDER BY c.nombre COLLATE NOCASE");
                return clientes.Where(c => Texto.Coincide(texto, c.Nombre, c.Documento, c.Telefono, c.Placa, c.Vehiculo)).ToList();
            }
        }

        /**
         * @brief Busca un cliente con su resumen de compras.
         * @param id Identificador
         * @return Cliente, o null si no existe
         */
        public Cliente GetById(int id)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.QueryFirstOrDefault<Cliente>(SelectSql + " WHERE c.id = @Id GROUP BY c.id", new { Id = id });
        }

        /**
         * @brief Clientes para la lista de una venta.
         * @return Identificador y nombre con la placa
         */
        public IEnumerable<Seleccion> GetParaSeleccion()
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Query<Seleccion>(@"
                    SELECT id AS Id,
                           nombre || CASE WHEN COALESCE(placa, '') <> '' THEN ' · ' || placa ELSE '' END AS Nombre
                    FROM cliente ORDER BY nombre COLLATE NOCASE").ToList();
        }

        /**
         * @brief Registra un cliente.
         * @param c Datos del cliente
         * @return Identificador asignado
         */
        public int Insert(Cliente c)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                db.Open();
                db.Execute(@"INSERT INTO cliente (nombre, documento, telefono, placa, vehiculo, notas)
                             VALUES (@Nombre, @Documento, @Telefono, @Placa, @Vehiculo, @Notas)", Parametros(c));
                return Sql.UltimoId(db);
            }
        }

        /**
         * @brief Actualiza un cliente.
         * @param c Cliente con los datos nuevos
         * @return true si se actualizó
         */
        public bool Update(Cliente c)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Execute(@"UPDATE cliente
                                    SET nombre = @Nombre, documento = @Documento, telefono = @Telefono,
                                        placa = @Placa, vehiculo = @Vehiculo, notas = @Notas
                                    WHERE id = @Id", Parametros(c)) > 0;
        }

        /**
         * @brief Cuenta las ventas del cliente, incluidas las anuladas.
         * @param id Cliente
         * @return Número de ventas
         */
        public int ContarVentas(int id)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.ExecuteScalar<int>("SELECT COUNT(*) FROM venta WHERE cliente_id = @Id", new { Id = id });
        }

        /**
         * @brief Elimina un cliente.
         * @param id Cliente
         * @return true si se eliminó
         */
        public bool Delete(int id)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Execute("DELETE FROM cliente WHERE id = @Id", new { Id = id }) > 0;
        }

        /**
         * @brief Parámetros de las consultas de alta y cambio.
         * @param c Cliente
         * @return Objeto anónimo con los valores
         */
        private static object Parametros(Cliente c) => new { c.Id, c.Nombre, c.Documento, c.Telefono, c.Placa, c.Vehiculo, c.Notas };
    }
}
