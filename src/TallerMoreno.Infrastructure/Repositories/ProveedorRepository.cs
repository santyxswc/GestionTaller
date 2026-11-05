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

        /** Proveedores con el total, el número y la fecha de sus entradas, y lo que se les ha pagado. */
        private const string SelectSql = @"
            SELECT p.id AS Id, p.nombre AS Nombre, p.nit AS Nit, p.contacto AS Contacto, p.telefono AS Telefono,
                   p.correo AS Correo, p.direccion AS Direccion, p.notas AS Notas,
                   (SELECT COALESCE(SUM(e.total), 0) FROM entrada e WHERE e.proveedor_id = p.id) AS TotalComprado,
                   (SELECT COUNT(*) FROM entrada e WHERE e.proveedor_id = p.id) AS NumeroEntradas,
                   (SELECT MAX(e.fecha) FROM entrada e WHERE e.proveedor_id = p.id) AS UltimaEntrada,
                   (SELECT COALESCE(SUM(g.valor), 0) FROM pago_proveedor g WHERE g.proveedor_id = p.id) AS TotalPagado
            FROM proveedor p";

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
                var proveedores = db.Query<Proveedor>(SelectSql + " ORDER BY p.nombre COLLATE NOCASE");
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
                return db.QueryFirstOrDefault<Proveedor>(SelectSql + " WHERE p.id = @Id", new { Id = id });
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
         * @brief Pagos hechos a un proveedor, incluidos los hechos al recibir mercancía.
         * @param proveedorId Proveedor
         * @return Pagos, del más reciente al más antiguo
         */
        public IEnumerable<Pago> Pagos(int proveedorId)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Query<Pago>(@"
                    SELECT g.id AS Id, g.fecha AS Fecha, g.valor AS Valor, g.metodo_pago AS MetodoPago, g.notas AS Notas,
                           u.nombre AS UsuarioNombre, g.entrada_id AS DocumentoId
                    FROM pago_proveedor g
                    LEFT JOIN usuario u ON u.id = g.usuario_id
                    WHERE g.proveedor_id = @Id
                    ORDER BY g.fecha DESC, g.id DESC", new { Id = proveedorId }).ToList();
        }

        /**
         * @brief Registra un pago a un proveedor que no pertenece a una entrada.
         * @param proveedorId Proveedor
         * @param usuarioId Usuario que lo registra
         * @param valor Valor pagado
         * @param metodoPago Efectivo, transferencia...
         * @param notas Notas
         * @return Identificador del pago
         */
        public int InsertPago(int proveedorId, int usuarioId, decimal valor, string metodoPago, string notas)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                db.Open();
                db.Execute(@"INSERT INTO pago_proveedor (fecha, proveedor_id, valor, metodo_pago, usuario_id, notas)
                             VALUES (@Fecha, @ProveedorId, @Valor, @MetodoPago, @UsuarioId, @Notas)",
                           new { Fecha = Sql.Ahora(), ProveedorId = proveedorId, Valor = valor, MetodoPago = metodoPago, UsuarioId = usuarioId, Notas = notas });
                return Sql.UltimoId(db);
            }
        }

        /**
         * @brief Borra un pago.
         * @param pagoId Pago
         * @return true si se borró
         */
        public bool DeletePago(int pagoId)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Execute("DELETE FROM pago_proveedor WHERE id = @Id", new { Id = pagoId }) > 0;
        }

        /**
         * @brief Parámetros de las consultas de alta y cambio.
         * @param p Proveedor
         * @return Objeto anónimo con los valores
         */
        private static object Parametros(Proveedor p) => new { p.Id, p.Nombre, p.Nit, p.Contacto, p.Telefono, p.Correo, p.Direccion, p.Notas };
    }
}
