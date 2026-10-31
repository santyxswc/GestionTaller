/**
 * @file ProveedorRepository.cs
 * @brief Acceso a datos de proveedores.
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
     * @brief Consultas y cambios de la tabla proveedor.
     */
    public class ProveedorRepository : IProveedorRepository
    {
        /** Fábrica de conexiones a la base de datos. */
        private readonly IDbConnectionFactory _conexiones;

        /** Proveedores con el total, el número y la fecha de sus entradas. */
        private const string SelectSql = @"
            SELECT p.id AS Id, p.nombre AS Nombre, p.nit AS Nit, p.contacto AS Contacto, p.telefono AS Telefono,
                   p.correo AS Correo, p.direccion AS Direccion, p.notas AS Notas,
                   COALESCE(SUM(e.total), 0) AS TotalComprado,
                   COUNT(e.id) AS NumeroEntradas,
                   MAX(e.fecha) AS UltimaEntrada
            FROM proveedor p
            LEFT JOIN entrada e ON e.proveedor_id = p.id";

        /**
         * @brief Crea el repositorio.
         * @param conexiones Fábrica de conexiones a la base de datos
         */
        public ProveedorRepository(IDbConnectionFactory conexiones) => _conexiones = conexiones;

        /**
         * @brief Busca proveedores con lo que se le ha comprado a cada uno.
         * @param texto Nombre, NIT, contacto o teléfono (sin importar tildes ni mayúsculas); vacío devuelve todos
         * @return Proveedores ordenados por nombre
         */
        public IEnumerable<Proveedor> Buscar(string texto)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                var proveedores = db.Query<Proveedor>(SelectSql + " GROUP BY p.id ORDER BY p.nombre COLLATE NOCASE");
                return proveedores.Where(p => Texto.Coincide(texto, p.Nombre, p.Nit, p.Contacto, p.Telefono)).ToList();
            }
        }

        /**
         * @brief Busca un proveedor con su resumen de compras.
         * @param id Identificador
         * @return Proveedor, o null si no existe
         */
        public Proveedor GetById(int id)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.QueryFirstOrDefault<Proveedor>(SelectSql + " WHERE p.id = @Id GROUP BY p.id", new { Id = id });
        }

        /**
         * @brief Proveedores para las listas desplegables.
         * @return Identificador y nombre
         */
        public IEnumerable<Seleccion> GetParaSeleccion()
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Query<Seleccion>("SELECT id AS Id, nombre AS Nombre FROM proveedor ORDER BY nombre COLLATE NOCASE").ToList();
        }

        /**
         * @brief Registra un proveedor.
         * @param p Datos del proveedor
         * @return Identificador asignado
         */
        public int Insert(Proveedor p)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                db.Open();
                db.Execute(@"INSERT INTO proveedor (nombre, nit, contacto, telefono, correo, direccion, notas)
                             VALUES (@Nombre, @Nit, @Contacto, @Telefono, @Correo, @Direccion, @Notas)", Parametros(p));
                return Sql.UltimoId(db);
            }
        }

        /**
         * @brief Actualiza un proveedor.
         * @param p Proveedor con los datos nuevos
         * @return true si se actualizó
         */
        public bool Update(Proveedor p)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Execute(@"UPDATE proveedor
                                    SET nombre = @Nombre, nit = @Nit, contacto = @Contacto, telefono = @Telefono,
                                        correo = @Correo, direccion = @Direccion, notas = @Notas
                                    WHERE id = @Id", Parametros(p)) > 0;
        }

        /**
         * @brief Cuenta las entradas de mercancía del proveedor.
         * @param id Proveedor
         * @return Número de entradas
         */
        public int ContarEntradas(int id)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.ExecuteScalar<int>("SELECT COUNT(*) FROM entrada WHERE proveedor_id = @Id", new { Id = id });
        }

        /**
         * @brief Elimina un proveedor; los repuestos que lo tenían quedan sin proveedor habitual.
         * @param id Proveedor
         * @return true si se eliminó
         */
        public bool Delete(int id)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Execute("DELETE FROM proveedor WHERE id = @Id", new { Id = id }) > 0;
        }

        /**
         * @brief Parámetros de las consultas de alta y cambio.
         * @param p Proveedor
         * @return Objeto anónimo con los valores
         */
        private static object Parametros(Proveedor p) => new { p.Id, p.Nombre, p.Nit, p.Contacto, p.Telefono, p.Correo, p.Direccion, p.Notas };
    }
}
