/**
 * @file DevolucionRepository.cs
 * @brief Acceso a datos de devoluciones y garantías.
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
     * @brief Devoluciones de clientes y a proveedores; cada una deja sus movimientos de inventario.
     */
    public class DevolucionRepository : IDevolucionRepository
    {
        /** Fábrica de conexiones a la base de datos. */
        private readonly IDbConnectionFactory _conexiones;

        /** Devoluciones de clientes con el nombre del repuesto y de quien la registró. */
        private const string SelectCliente = @"
            SELECT d.id AS Id, d.fecha AS Fecha, d.venta_id AS VentaId, d.repuesto_id AS RepuestoId, r.nombre AS RepuestoNombre,
                   d.cantidad AS Cantidad, d.motivo AS Motivo, d.resultado AS Resultado, d.valor AS Valor,
                   d.vuelve_inventario AS MovioInventario, d.notas AS Notas, u.nombre AS UsuarioNombre
            FROM devolucion_cliente d
            JOIN repuesto r ON r.id = d.repuesto_id
            LEFT JOIN usuario u ON u.id = d.usuario_id";

        /** Devoluciones a proveedores con el nombre del repuesto y de quien la registró. */
        private const string SelectProveedor = @"
            SELECT d.id AS Id, d.fecha AS Fecha, d.proveedor_id AS ProveedorId, d.repuesto_id AS RepuestoId, r.nombre AS RepuestoNombre,
                   d.cantidad AS Cantidad, d.motivo AS Motivo, d.resultado AS Resultado, d.valor AS Valor,
                   d.sale_inventario AS MovioInventario, d.notas AS Notas, u.nombre AS UsuarioNombre
            FROM devolucion_proveedor d
            JOIN repuesto r ON r.id = d.repuesto_id
            LEFT JOIN usuario u ON u.id = d.usuario_id";

        /**
         * @brief Fila de una devolución con lo necesario para deshacerla.
         */
        private sealed class Guardada
        {
            /** Venta (cliente) o proveedor. */
            public int Documento { get; set; }
            /** Repuesto. */
            public int RepuestoId { get; set; }
            /** Unidades. */
            public int Cantidad { get; set; }
            /** Resultado. */
            public string Resultado { get; set; }
            /** Si movió el inventario. */
            public bool Movio { get; set; }
        }

        /**
         * @brief Crea el repositorio.
         * @param conexiones Fábrica de conexiones a la base de datos
         */
        public DevolucionRepository(IDbConnectionFactory conexiones) => _conexiones = conexiones;

        /** @copydoc IDevolucionRepository.DeCliente */
        public IEnumerable<Devolucion> DeCliente(int clienteId)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Query<Devolucion>(SelectCliente + " WHERE d.cliente_id = @Id ORDER BY d.fecha DESC, d.id DESC", new { Id = clienteId }).ToList();
        }

        /** @copydoc IDevolucionRepository.DeVenta */
        public IEnumerable<Devolucion> DeVenta(int ventaId)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Query<Devolucion>(SelectCliente + " WHERE d.venta_id = @Id ORDER BY d.fecha DESC, d.id DESC", new { Id = ventaId }).ToList();
        }

        /** @copydoc IDevolucionRepository.DeProveedor */
        public IEnumerable<Devolucion> DeProveedor(int proveedorId)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Query<Devolucion>(SelectProveedor + " WHERE d.proveedor_id = @Id ORDER BY d.fecha DESC, d.id DESC", new { Id = proveedorId }).ToList();
        }

        /** @copydoc IDevolucionRepository.GetDeProveedor */
        public Devolucion GetDeProveedor(int id)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.QueryFirstOrDefault<Devolucion>(SelectProveedor + " WHERE d.id = @Id", new { Id = id });
        }

        /** @copydoc IDevolucionRepository.InsertDeCliente */
        public int InsertDeCliente(int ventaId, int usuarioId, int repuestoId, int cantidad, string motivo, string resultado,
                                   decimal valor, bool vuelveInventario, string notas)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                db.Open();
                using (var tran = db.BeginTransaction())
                {
                    bool? anulada = db.ExecuteScalar<bool?>("SELECT anulada FROM venta WHERE id = @Id", new { Id = ventaId }, tran);
                    if (anulada == null)
                        throw new InvalidOperationException($"La venta N° {ventaId} no existe.");
                    if (anulada.Value)
                        throw new InvalidOperationException($"La venta N° {ventaId} está anulada.");
                    long? clienteId = db.ExecuteScalar<long?>("SELECT cliente_id FROM venta WHERE id = @Id", new { Id = ventaId }, tran);

                    int vendidas = db.ExecuteScalar<int>(
                        "SELECT COALESCE(SUM(cantidad), 0) FROM venta_detalle WHERE venta_id = @VentaId AND repuesto_id = @RepuestoId",
                        new { VentaId = ventaId, RepuestoId = repuestoId }, tran);
                    int devueltas = db.ExecuteScalar<int>(
                        "SELECT COALESCE(SUM(cantidad), 0) FROM devolucion_cliente WHERE venta_id = @VentaId AND repuesto_id = @RepuestoId",
                        new { VentaId = ventaId, RepuestoId = repuestoId }, tran);
                    if (vendidas == 0)
                        throw new InvalidOperationException("Ese repuesto no está en la venta.");
                    if (cantidad > vendidas - devueltas)
                        throw new InvalidOperationException($"De ese repuesto solo quedan {vendidas - devueltas} unidades por devolver en la venta N° {ventaId}.");

                    string fecha = Sql.Ahora();
                    if (vuelveInventario)
                    {
                        db.Execute("UPDATE repuesto SET stock = stock + @Cantidad WHERE id = @Id", new { Id = repuestoId, Cantidad = cantidad }, tran);
                        Sql.RegistrarMovimiento(db, tran, fecha, repuestoId, MovimientoInventario.TipoDevolucion, cantidad,
                                                $"Devolución de la venta N° {ventaId}", usuarioId);
                    }
                    if (resultado == Devolucion.Cambio)
                    {
                        Descontar(db, tran, repuestoId, cantidad, "para cambiárselo al cliente");
                        Sql.RegistrarMovimiento(db, tran, fecha, repuestoId, MovimientoInventario.TipoGarantia, -cantidad,
                                                $"Cambio de la venta N° {ventaId}", usuarioId);
                    }

                    db.Execute(@"INSERT INTO devolucion_cliente (fecha, venta_id, cliente_id, repuesto_id, cantidad, motivo, resultado, valor, vuelve_inventario, usuario_id, notas)
                                 VALUES (@Fecha, @VentaId, @ClienteId, @RepuestoId, @Cantidad, @Motivo, @Resultado, @Valor, @Vuelve, @UsuarioId, @Notas)",
                               new
                               {
                                   Fecha = fecha,
                                   VentaId = ventaId,
                                   ClienteId = clienteId,
                                   RepuestoId = repuestoId,
                                   Cantidad = cantidad,
                                   Motivo = motivo,
                                   Resultado = resultado,
                                   Valor = valor,
                                   Vuelve = vuelveInventario,
                                   UsuarioId = usuarioId,
                                   Notas = notas
                               }, tran);
                    int id = Sql.UltimoId(db, tran);
                    tran.Commit();
                    return id;
                }
            }
        }

        /** @copydoc IDevolucionRepository.InsertAProveedor */
        public int InsertAProveedor(int proveedorId, int usuarioId, int repuestoId, int cantidad, string motivo, string resultado,
                                    decimal valor, bool saleInventario, string notas)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                db.Open();
                using (var tran = db.BeginTransaction())
                {
                    string proveedor = db.ExecuteScalar<string>("SELECT nombre FROM proveedor WHERE id = @Id", new { Id = proveedorId }, tran)
                        ?? throw new InvalidOperationException("El proveedor ya no existe.");
                    if (db.ExecuteScalar<int>("SELECT COUNT(*) FROM repuesto WHERE id = @Id", new { Id = repuestoId }, tran) == 0)
                        throw new InvalidOperationException("Ese repuesto ya no existe en el inventario.");

                    string fecha = Sql.Ahora();
                    if (saleInventario)
                    {
                        Descontar(db, tran, repuestoId, cantidad, "para devolver");
                        Sql.RegistrarMovimiento(db, tran, fecha, repuestoId, MovimientoInventario.TipoDevolucionProveedor, -cantidad,
                                                $"Devuelto a {proveedor}", usuarioId);
                    }
                    if (resultado == Devolucion.Cambio)
                        EntraCambio(db, tran, fecha, repuestoId, cantidad, proveedor, usuarioId);

                    db.Execute(@"INSERT INTO devolucion_proveedor (fecha, proveedor_id, repuesto_id, cantidad, motivo, resultado, valor, sale_inventario, resuelta_fecha, usuario_id, notas)
                                 VALUES (@Fecha, @ProveedorId, @RepuestoId, @Cantidad, @Motivo, @Resultado, @Valor, @Sale, @Resuelta, @UsuarioId, @Notas)",
                               new
                               {
                                   Fecha = fecha,
                                   ProveedorId = proveedorId,
                                   RepuestoId = repuestoId,
                                   Cantidad = cantidad,
                                   Motivo = motivo,
                                   Resultado = resultado,
                                   Valor = valor,
                                   Sale = saleInventario,
                                   Resuelta = resultado == Devolucion.Pendiente ? null : fecha,
                                   UsuarioId = usuarioId,
                                   Notas = notas
                               }, tran);
                    int id = Sql.UltimoId(db, tran);
                    tran.Commit();
                    return id;
                }
            }
        }

        /** @copydoc IDevolucionRepository.Resolver */
        public void Resolver(int id, int usuarioId, string resultado, decimal valor, string notas)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                db.Open();
                using (var tran = db.BeginTransaction())
                {
                    var d = LeerAProveedor(db, tran, id);
                    if (d.Resultado != Devolucion.Pendiente)
                        throw new InvalidOperationException("Esa garantía ya tiene respuesta del proveedor.");

                    string fecha = Sql.Ahora();
                    if (resultado == Devolucion.Cambio)
                    {
                        string proveedor = db.ExecuteScalar<string>("SELECT nombre FROM proveedor WHERE id = @Id", new { Id = d.Documento }, tran);
                        EntraCambio(db, tran, fecha, d.RepuestoId, d.Cantidad, proveedor, usuarioId);
                    }
                    db.Execute(@"UPDATE devolucion_proveedor SET resultado = @Resultado, valor = @Valor, resuelta_fecha = @Fecha,
                                        notas = COALESCE(@Notas, notas)
                                 WHERE id = @Id",
                               new { Id = id, Resultado = resultado, Valor = valor, Fecha = fecha, Notas = notas }, tran);
                    tran.Commit();
                }
            }
        }

        /** @copydoc IDevolucionRepository.DeleteDeCliente */
        public void DeleteDeCliente(int id, int usuarioId)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                db.Open();
                using (var tran = db.BeginTransaction())
                {
                    var d = db.QueryFirstOrDefault<Guardada>(@"
                        SELECT venta_id AS Documento, repuesto_id AS RepuestoId, cantidad AS Cantidad, resultado AS Resultado, vuelve_inventario AS Movio
                        FROM devolucion_cliente WHERE id = @Id", new { Id = id }, tran)
                        ?? throw new InvalidOperationException("Esa devolución ya no existe.");

                    string fecha = Sql.Ahora();
                    if (d.Movio)
                    {
                        Descontar(db, tran, d.RepuestoId, d.Cantidad, "que entraron con esta devolución; ya se vendieron");
                        Sql.RegistrarMovimiento(db, tran, fecha, d.RepuestoId, MovimientoInventario.TipoDevolucion, -d.Cantidad,
                                                $"Devolución de la venta N° {d.Documento} borrada", usuarioId);
                    }
                    if (d.Resultado == Devolucion.Cambio)
                    {
                        db.Execute("UPDATE repuesto SET stock = stock + @Cantidad WHERE id = @Id", new { Id = d.RepuestoId, d.Cantidad }, tran);
                        Sql.RegistrarMovimiento(db, tran, fecha, d.RepuestoId, MovimientoInventario.TipoGarantia, d.Cantidad,
                                                $"Cambio de la venta N° {d.Documento} borrado", usuarioId);
                    }
                    db.Execute("DELETE FROM devolucion_cliente WHERE id = @Id", new { Id = id }, tran);
                    tran.Commit();
                }
            }
        }

        /** @copydoc IDevolucionRepository.DeleteAProveedor */
        public void DeleteAProveedor(int id, int usuarioId)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                db.Open();
                using (var tran = db.BeginTransaction())
                {
                    var d = LeerAProveedor(db, tran, id);
                    string fecha = Sql.Ahora();
                    if (d.Resultado == Devolucion.Cambio)
                    {
                        Descontar(db, tran, d.RepuestoId, d.Cantidad, "que llegaron con este cambio; ya se vendieron");
                        Sql.RegistrarMovimiento(db, tran, fecha, d.RepuestoId, MovimientoInventario.TipoGarantia, -d.Cantidad,
                                                "Cambio del proveedor borrado", usuarioId);
                    }
                    if (d.Movio)
                    {
                        db.Execute("UPDATE repuesto SET stock = stock + @Cantidad WHERE id = @Id", new { Id = d.RepuestoId, d.Cantidad }, tran);
                        Sql.RegistrarMovimiento(db, tran, fecha, d.RepuestoId, MovimientoInventario.TipoDevolucionProveedor, d.Cantidad,
                                                "Devolución a proveedor borrada", usuarioId);
                    }
                    db.Execute("DELETE FROM devolucion_proveedor WHERE id = @Id", new { Id = id }, tran);
                    tran.Commit();
                }
            }
        }

        /**
         * @brief Lee una devolución a proveedor dentro de una transacción.
         * @param db Conexión abierta
         * @param tran Transacción en curso
         * @param id Devolución
         * @return Datos para resolverla o deshacerla
         * @exception InvalidOperationException Si no existe
         */
        private static Guardada LeerAProveedor(IDbConnection db, IDbTransaction tran, int id) =>
            db.QueryFirstOrDefault<Guardada>(@"
                SELECT proveedor_id AS Documento, repuesto_id AS RepuestoId, cantidad AS Cantidad, resultado AS Resultado, sale_inventario AS Movio
                FROM devolucion_proveedor WHERE id = @Id", new { Id = id }, tran)
            ?? throw new InvalidOperationException("Esa devolución ya no existe.");

        /**
         * @brief Suma al inventario las unidades que el proveedor mandó para cambiar.
         * @param db Conexión abierta
         * @param tran Transacción en curso
         * @param fecha Fecha del movimiento
         * @param repuestoId Repuesto
         * @param cantidad Unidades
         * @param proveedor Nombre del proveedor
         * @param usuarioId Usuario
         */
        private static void EntraCambio(IDbConnection db, IDbTransaction tran, string fecha, int repuestoId, int cantidad, string proveedor, int usuarioId)
        {
            db.Execute("UPDATE repuesto SET stock = stock + @Cantidad WHERE id = @Id", new { Id = repuestoId, Cantidad = cantidad }, tran);
            Sql.RegistrarMovimiento(db, tran, fecha, repuestoId, MovimientoInventario.TipoGarantia, cantidad, $"Cambio de {proveedor}", usuarioId);
        }

        /**
         * @brief Resta unidades sin dejar el stock negativo.
         * @param db Conexión abierta
         * @param tran Transacción en curso
         * @param repuestoId Repuesto
         * @param cantidad Unidades
         * @param para Final del mensaje de error, como "para devolver"
         * @exception InvalidOperationException Si no alcanzan las unidades
         */
        private static void Descontar(IDbConnection db, IDbTransaction tran, int repuestoId, int cantidad, string para)
        {
            int filas = db.Execute("UPDATE repuesto SET stock = stock - @Cantidad WHERE id = @Id AND stock >= @Cantidad",
                                   new { Id = repuestoId, Cantidad = cantidad }, tran);
            if (filas == 1) return;
            var r = db.QueryFirst<(string Nombre, long Stock)>("SELECT nombre, stock FROM repuesto WHERE id = @Id", new { Id = repuestoId }, tran);
            throw new InvalidOperationException($"De {r.Nombre} hay {r.Stock} en el inventario; no alcanzan las {cantidad} unidades {para}.");
        }
    }
}
