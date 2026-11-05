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

        /**
         * Clientes con el total, el número y la fecha de sus compras no anuladas, y lo que han abonado.
         * Lo abonado al comprar en una venta que después se anuló no cuenta: se le devolvió con la venta.
         * Lo devuelto en plata o rebajado de la deuda se resta de lo comprado; la plata devuelta, también de lo pagado.
         */
        private const string SelectSql = @"
            SELECT c.id AS Id, c.nombre AS Nombre, c.documento AS Documento, c.telefono AS Telefono,
                   c.placa AS Placa, c.vehiculo AS Vehiculo, c.notas AS Notas,
                   c.kilometraje AS Kilometraje, c.kilometraje_fecha AS KilometrajeFecha,
                   (SELECT COALESCE(SUM(v.total), 0) FROM venta v WHERE v.cliente_id = c.id AND v.anulada = 0)
                 - (SELECT COALESCE(SUM(d.valor), 0) FROM devolucion_cliente d
                    WHERE d.cliente_id = c.id AND d.resultado IN ('Dinero', 'Descuento')) AS TotalComprado,
                   (SELECT COUNT(*) FROM venta v WHERE v.cliente_id = c.id AND v.anulada = 0) AS NumeroCompras,
                   (SELECT MAX(v.fecha) FROM venta v WHERE v.cliente_id = c.id AND v.anulada = 0) AS UltimaCompra,
                   (SELECT COALESCE(SUM(a.valor), 0) FROM abono_cliente a
                    LEFT JOIN venta v ON v.id = a.venta_id
                    WHERE a.cliente_id = c.id AND COALESCE(v.anulada, 0) = 0)
                 - (SELECT COALESCE(SUM(d.valor), 0) FROM devolucion_cliente d
                    WHERE d.cliente_id = c.id AND d.resultado = 'Dinero') AS TotalPagado,
                   (SELECT COUNT(*) FROM devolucion_cliente d WHERE d.cliente_id = c.id) AS Devoluciones
            FROM cliente c";

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
                var clientes = db.Query<Cliente>(SelectSql + " ORDER BY c.nombre COLLATE NOCASE");
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
                return db.QueryFirstOrDefault<Cliente>(SelectSql + " WHERE c.id = @Id", new { Id = id });
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
                db.Execute(@"INSERT INTO cliente (nombre, documento, telefono, placa, vehiculo, notas, kilometraje, kilometraje_fecha)
                             VALUES (@Nombre, @Documento, @Telefono, @Placa, @Vehiculo, @Notas, @Kilometraje,
                                     CASE WHEN @Kilometraje IS NULL THEN NULL ELSE @Fecha END)", Parametros(c));
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
                                        placa = @Placa, vehiculo = @Vehiculo, notas = @Notas,
                                        kilometraje = @Kilometraje,
                                        kilometraje_fecha = CASE WHEN @Kilometraje IS NULL THEN NULL
                                                                 WHEN kilometraje IS @Kilometraje THEN kilometraje_fecha
                                                                 ELSE @Fecha END
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
         * @brief Abonos de un cliente, incluidos los hechos al comprar.
         * @param clienteId Cliente
         * @return Abonos, del más reciente al más antiguo
         */
        public IEnumerable<Pago> Abonos(int clienteId)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Query<Pago>(@"
                    SELECT g.id AS Id, g.fecha AS Fecha, g.valor AS Valor, g.metodo_pago AS MetodoPago, g.notas AS Notas,
                           u.nombre AS UsuarioNombre, g.venta_id AS DocumentoId
                    FROM abono_cliente g
                    LEFT JOIN usuario u ON u.id = g.usuario_id
                    WHERE g.cliente_id = @Id
                    ORDER BY g.fecha DESC, g.id DESC", new { Id = clienteId }).ToList();
        }

        /**
         * @brief Registra un abono de un cliente que no pertenece a una venta.
         * @param clienteId Cliente
         * @param usuarioId Usuario que lo recibe
         * @param valor Valor abonado
         * @param metodoPago Efectivo, transferencia...
         * @param notas Notas
         * @return Identificador del abono
         */
        public int InsertAbono(int clienteId, int usuarioId, decimal valor, string metodoPago, string notas)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                db.Open();
                db.Execute(@"INSERT INTO abono_cliente (fecha, cliente_id, valor, metodo_pago, usuario_id, notas)
                             VALUES (@Fecha, @ClienteId, @Valor, @MetodoPago, @UsuarioId, @Notas)",
                           new { Fecha = Sql.Ahora(), ClienteId = clienteId, Valor = valor, MetodoPago = metodoPago, UsuarioId = usuarioId, Notas = notas });
                return Sql.UltimoId(db);
            }
        }

        /**
         * @brief Borra un abono.
         * @param abonoId Abono
         * @return true si se borró
         */
        public bool DeleteAbono(int abonoId)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Execute("DELETE FROM abono_cliente WHERE id = @Id", new { Id = abonoId }) > 0;
        }

        /**
         * @brief Parámetros de las consultas de alta y cambio.
         * @param c Cliente
         * @return Objeto anónimo con los valores
         */
        private static object Parametros(Cliente c) =>
            new { c.Id, c.Nombre, c.Documento, c.Telefono, c.Placa, c.Vehiculo, c.Notas, c.Kilometraje, Fecha = Sql.Ahora() };
    }
}
