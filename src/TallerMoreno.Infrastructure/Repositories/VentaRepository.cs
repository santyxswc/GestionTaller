/**
 * @file VentaRepository.cs
 * @brief Acceso a datos de ventas.
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
     * @brief Registro, consulta y anulación de ventas.
     */
    public class VentaRepository : IVentaRepository
    {
        /** Fábrica de conexiones a la base de datos. */
        private readonly IDbConnectionFactory _conexiones;

        /** Ventas con cliente, vendedor, unidades y ganancia. */
        private const string SelectSql = @"
            SELECT v.id AS Id, v.fecha AS Fecha, v.cliente_id AS ClienteId, c.nombre AS ClienteNombre,
                   c.placa AS ClientePlaca, v.usuario_id AS UsuarioId, u.nombre AS UsuarioNombre,
                   v.metodo_pago AS MetodoPago, v.total AS Total, v.anulada AS Anulada, v.notas AS Notas,
                   v.kilometraje AS Kilometraje,
                   (SELECT COUNT(*) FROM devolucion_cliente dc WHERE dc.venta_id = v.id) AS Devoluciones,
                   (SELECT COALESCE(SUM(d.cantidad), 0) FROM venta_detalle d WHERE d.venta_id = v.id) AS Unidades,
                   CASE WHEN v.cliente_id IS NULL THEN v.total
                        ELSE (SELECT COALESCE(SUM(a.valor), 0) FROM abono_cliente a WHERE a.venta_id = v.id) END AS Abonado,
                   v.total - (SELECT COALESCE(SUM(d.costo_unitario * d.cantidad), 0)
                              FROM venta_detalle d WHERE d.venta_id = v.id) AS Ganancia
            FROM venta v
            LEFT JOIN cliente c ON c.id = v.cliente_id
            JOIN usuario u ON u.id = v.usuario_id";

        /**
         * @brief Datos de un repuesto que se leen al vender.
         */
        private sealed class RepuestoVenta
        {
            /** Identificador. */
            public int Id { get; set; }
            /** Nombre. */
            public string Nombre { get; set; }
            /** Precio de venta. */
            public decimal PrecioVenta { get; set; }
            /** Costo. */
            public decimal PrecioCompra { get; set; }
            /** Unidades disponibles. */
            public int Stock { get; set; }
        }

        /**
         * @brief Crea el repositorio.
         * @param conexiones Fábrica de conexiones a la base de datos
         */
        public VentaRepository(IDbConnectionFactory conexiones) => _conexiones = conexiones;

        /**
         * @brief Registra una venta completa y descuenta el stock en una transacción.
         * @param clienteId Cliente, o null para cliente de mostrador
         * @param usuarioId Usuario que vende
         * @param metodoPago Método de pago
         * @param notas Notas de la venta
         * @param lineas Repuestos y cantidades (sin repetidos)
         * @param abono Lo que el cliente paga ahora si la venta se fía, o null si paga todo
         * @param kilometraje Kilometraje del carro del cliente, o null
         * @return Número de la venta
         * @exception InvalidOperationException Si un repuesto no existe, no alcanza el stock o el abono pasa
         *            del total; no se guarda nada
         *
         * Si llega el kilometraje, queda como el último conocido del cliente.
         * Los precios y costos se toman de la base de datos en el momento de la venta. El descuento
         * se hace con "stock >= cantidad" en la misma sentencia, así el stock nunca queda negativo.
         * Si la venta tiene cliente, lo que paga queda como abono de esa venta: así se sabe cuánto debe.
         */
        public int Insert(int? clienteId, int usuarioId, string metodoPago, string notas, IEnumerable<LineaDocumento> lineas, decimal? abono,
                          int? kilometraje = null)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                db.Open();
                using (var tran = db.BeginTransaction())
                {
                    var detalle = new List<LineaDocumento>();
                    foreach (var linea in lineas)
                    {
                        var r = db.QueryFirstOrDefault<RepuestoVenta>(@"
                            SELECT id AS Id, nombre AS Nombre, precio_venta AS PrecioVenta,
                                   precio_compra AS PrecioCompra, stock AS Stock
                            FROM repuesto WHERE id = @Id", new { Id = linea.RepuestoId }, tran)
                            ?? throw new InvalidOperationException("Uno de los repuestos ya no existe en el inventario.");
                        if (r.Stock < linea.Cantidad)
                            throw new InvalidOperationException($"No alcanza el stock de {r.Nombre}: quedan {r.Stock} y se piden {linea.Cantidad}.");

                        detalle.Add(new LineaDocumento
                        {
                            RepuestoId = r.Id,
                            Nombre = r.Nombre,
                            Cantidad = linea.Cantidad,
                            PrecioUnitario = r.PrecioVenta,
                            CostoUnitario = r.PrecioCompra,
                            Subtotal = r.PrecioVenta * linea.Cantidad
                        });
                    }

                    decimal total = detalle.Sum(d => d.Subtotal);
                    if (abono > total)
                        throw new InvalidOperationException($"El abono no puede ser mayor que el total de la venta ({Formato.Moneda(total)}).");

                    string fecha = Sql.Ahora();
                    db.Execute(@"INSERT INTO venta (fecha, cliente_id, usuario_id, metodo_pago, total, notas, kilometraje)
                                 VALUES (@Fecha, @ClienteId, @UsuarioId, @MetodoPago, @Total, @Notas, @Kilometraje)",
                               new { Fecha = fecha, ClienteId = clienteId, UsuarioId = usuarioId, MetodoPago = metodoPago, Total = total, Notas = notas, Kilometraje = kilometraje },
                               tran);
                    int id = Sql.UltimoId(db, tran);

                    if (clienteId.HasValue && kilometraje.HasValue)
                        db.Execute("UPDATE cliente SET kilometraje = @Km, kilometraje_fecha = @Fecha WHERE id = @Id",
                                   new { Km = kilometraje, Fecha = fecha, Id = clienteId }, tran);

                    decimal pagado = abono ?? total;
                    if (clienteId.HasValue && pagado > 0)
                        db.Execute(@"INSERT INTO abono_cliente (fecha, cliente_id, venta_id, valor, metodo_pago, usuario_id)
                                     VALUES (@Fecha, @ClienteId, @VentaId, @Valor, @MetodoPago, @UsuarioId)",
                                   new { Fecha = fecha, ClienteId = clienteId, VentaId = id, Valor = pagado, MetodoPago = metodoPago, UsuarioId = usuarioId }, tran);

                    foreach (var d in detalle)
                    {
                        db.Execute(@"INSERT INTO venta_detalle (venta_id, repuesto_id, cantidad, precio_unitario, costo_unitario, subtotal)
                                     VALUES (@VentaId, @RepuestoId, @Cantidad, @PrecioUnitario, @CostoUnitario, @Subtotal)",
                                   new { VentaId = id, d.RepuestoId, d.Cantidad, d.PrecioUnitario, d.CostoUnitario, d.Subtotal }, tran);

                        int filas = db.Execute("UPDATE repuesto SET stock = stock - @Cantidad WHERE id = @Id AND stock >= @Cantidad",
                                               new { Id = d.RepuestoId, d.Cantidad }, tran);
                        if (filas != 1)
                            throw new InvalidOperationException($"No alcanza el stock de {d.Nombre}.");

                        Sql.RegistrarMovimiento(db, tran, fecha, d.RepuestoId, MovimientoInventario.TipoVenta, -d.Cantidad, $"Venta N° {id}", usuarioId);
                    }

                    tran.Commit();
                    return id;
                }
            }
        }

        /**
         * @brief Busca una venta sin sus líneas.
         * @param id Número de la venta
         * @return Venta, o null si no existe
         */
        public Venta GetById(int id)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.QueryFirstOrDefault<Venta>(SelectSql + " WHERE v.id = @Id", new { Id = id });
        }

        /**
         * @brief Obtiene las líneas de una venta.
         * @param id Número de la venta
         * @return Lineas con el nombre de cada repuesto
         */
        public List<LineaDocumento> GetItems(int id)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Query<LineaDocumento>(@"
                    SELECT d.repuesto_id AS RepuestoId, r.codigo AS Codigo, r.nombre AS Nombre, d.cantidad AS Cantidad,
                           d.precio_unitario AS PrecioUnitario, d.costo_unitario AS CostoUnitario, d.subtotal AS Subtotal,
                           (SELECT COALESCE(SUM(dc.cantidad), 0) FROM devolucion_cliente dc
                            WHERE dc.venta_id = d.venta_id AND dc.repuesto_id = d.repuesto_id) AS Devueltas
                    FROM venta_detalle d
                    JOIN repuesto r ON r.id = d.repuesto_id
                    WHERE d.venta_id = @Id
                    ORDER BY r.nombre COLLATE NOCASE", new { Id = id }).ToList();
        }

        /**
         * @brief Busca ventas, de la más reciente a la más antigua.
         * @param desde Fecha inicial (incluida), o null
         * @param hasta Fecha final (excluida), o null
         * @param texto Cliente, placa, vendedor o número (sin importar tildes ni mayúsculas); vacío no filtra
         * @param clienteId Solo las de este cliente, o null
         * @return Ventas que coinciden
         */
        public IEnumerable<Venta> Buscar(DateTime? desde, DateTime? hasta, string texto, int? clienteId)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                var ventas = db.Query<Venta>(
                    SelectSql + " WHERE " + Sql.FiltroFecha("v.fecha") + " AND (@ClienteId IS NULL OR v.cliente_id = @ClienteId) ORDER BY v.id DESC",
                    new { Desde = Sql.Fecha(desde), Hasta = Sql.Fecha(hasta), ClienteId = clienteId });
                return ventas.Where(v => Texto.Coincide(texto, v.Cliente, v.ClientePlaca, v.UsuarioNombre, v.MetodoPago, v.Id.ToString())).ToList();
            }
        }

        /**
         * @brief Anula una venta y devuelve sus unidades al inventario en una transacción.
         * @param id Número de la venta
         * @param usuarioId Usuario que anula
         * @exception InvalidOperationException Si la venta no existe o ya estaba anulada
         */
        public void Anular(int id, int usuarioId)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                db.Open();
                using (var tran = db.BeginTransaction())
                {
                    bool? anulada = db.ExecuteScalar<bool?>("SELECT anulada FROM venta WHERE id = @Id", new { Id = id }, tran);
                    if (anulada == null)
                        throw new InvalidOperationException($"La venta N° {id} no existe.");
                    if (anulada.Value)
                        throw new InvalidOperationException($"La venta N° {id} ya estaba anulada.");
                    if (db.ExecuteScalar<int>("SELECT COUNT(*) FROM devolucion_cliente WHERE venta_id = @Id", new { Id = id }, tran) > 0)
                        throw new InvalidOperationException($"La venta N° {id} tiene devoluciones registradas; no se puede anular.");

                    string fecha = Sql.Ahora();
                    db.Execute("UPDATE venta SET anulada = 1, anulada_fecha = @Fecha, anulada_por = @UsuarioId WHERE id = @Id",
                               new { Id = id, Fecha = fecha, UsuarioId = usuarioId }, tran);

                    var lineas = db.Query<LineaDocumento>(
                        "SELECT repuesto_id AS RepuestoId, cantidad AS Cantidad FROM venta_detalle WHERE venta_id = @Id",
                        new { Id = id }, tran).ToList();
                    foreach (var l in lineas)
                    {
                        db.Execute("UPDATE repuesto SET stock = stock + @Cantidad WHERE id = @Id", new { Id = l.RepuestoId, l.Cantidad }, tran);
                        Sql.RegistrarMovimiento(db, tran, fecha, l.RepuestoId, MovimientoInventario.TipoAnulacion, l.Cantidad, $"Venta N° {id} anulada", usuarioId);
                    }

                    tran.Commit();
                }
            }
        }
    }
}
