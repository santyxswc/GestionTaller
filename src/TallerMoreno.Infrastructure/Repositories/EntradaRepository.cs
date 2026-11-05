/**
 * @file EntradaRepository.cs
 * @brief Acceso a datos de entradas de mercancía.
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
     * @brief Registro y consulta de la mercancía recibida de los proveedores.
     */
    public class EntradaRepository : IEntradaRepository
    {
        /** Fábrica de conexiones a la base de datos. */
        private readonly IDbConnectionFactory _conexiones;

        /** Entradas con proveedor, usuario y unidades. */
        private const string SelectSql = @"
            SELECT e.id AS Id, e.fecha AS Fecha, e.proveedor_id AS ProveedorId, p.nombre AS ProveedorNombre,
                   e.factura_proveedor AS FacturaProveedor, e.usuario_id AS UsuarioId, u.nombre AS UsuarioNombre,
                   e.total AS Total,
                   (SELECT COALESCE(SUM(d.cantidad), 0) FROM entrada_detalle d WHERE d.entrada_id = e.id) AS Unidades,
                   (SELECT COALESCE(SUM(g.valor), 0) FROM pago_proveedor g WHERE g.entrada_id = e.id) AS Pagado
            FROM entrada e
            JOIN proveedor p ON p.id = e.proveedor_id
            JOIN usuario u ON u.id = e.usuario_id";

        /**
         * @brief Crea el repositorio.
         * @param conexiones Fábrica de conexiones a la base de datos
         */
        public EntradaRepository(IDbConnectionFactory conexiones) => _conexiones = conexiones;

        /**
         * @brief Registra una entrada, suma el stock y actualiza el costo de cada repuesto en una transacción.
         * @param proveedorId Proveedor
         * @param usuarioId Usuario que recibe
         * @param facturaProveedor Número de la factura del proveedor
         * @param lineas Repuestos, cantidades y costos (sin repetidos)
         * @param pagado Lo que se le pagó al proveedor al recibir; el resto queda como deuda
         * @return Número de la entrada
         * @exception InvalidOperationException Si un repuesto no existe; no se guarda nada
         *
         * Un costo de 0 deja el costo anterior del repuesto. Si el repuesto no tenía proveedor
         * habitual, queda con este.
         */
        public int Insert(int proveedorId, int usuarioId, string facturaProveedor, IEnumerable<LineaDocumento> lineas, decimal pagado)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                db.Open();
                using (var tran = db.BeginTransaction())
                {
                    var detalle = lineas.ToList();
                    string fecha = Sql.Ahora();
                    db.Execute(@"INSERT INTO entrada (fecha, proveedor_id, usuario_id, factura_proveedor, total)
                                 VALUES (@Fecha, @ProveedorId, @UsuarioId, @Factura, @Total)",
                               new { Fecha = fecha, ProveedorId = proveedorId, UsuarioId = usuarioId, Factura = facturaProveedor, Total = detalle.Sum(d => d.PrecioUnitario * d.Cantidad) },
                               tran);
                    int id = Sql.UltimoId(db, tran);

                    foreach (var d in detalle)
                    {
                        int filas = db.Execute(@"
                            UPDATE repuesto
                            SET stock = stock + @Cantidad,
                                precio_compra = CASE WHEN @Costo > 0 THEN @Costo ELSE precio_compra END,
                                proveedor_id = COALESCE(proveedor_id, @ProveedorId)
                            WHERE id = @Id",
                            new { Id = d.RepuestoId, d.Cantidad, Costo = d.PrecioUnitario, ProveedorId = proveedorId }, tran);
                        if (filas != 1)
                            throw new InvalidOperationException("Uno de los repuestos ya no existe en el inventario.");

                        db.Execute(@"INSERT INTO entrada_detalle (entrada_id, repuesto_id, cantidad, costo_unitario, subtotal)
                                     VALUES (@EntradaId, @RepuestoId, @Cantidad, @Costo, @Subtotal)",
                                   new { EntradaId = id, d.RepuestoId, d.Cantidad, Costo = d.PrecioUnitario, Subtotal = d.PrecioUnitario * d.Cantidad }, tran);

                        string referencia = string.IsNullOrEmpty(facturaProveedor) ? $"Entrada N° {id}" : $"Entrada N° {id} · factura {facturaProveedor}";
                        Sql.RegistrarMovimiento(db, tran, fecha, d.RepuestoId, MovimientoInventario.TipoEntrada, d.Cantidad, referencia, usuarioId);
                    }

                    if (pagado > 0)
                        db.Execute(@"INSERT INTO pago_proveedor (fecha, proveedor_id, entrada_id, valor, usuario_id)
                                     VALUES (@Fecha, @ProveedorId, @EntradaId, @Valor, @UsuarioId)",
                                   new { Fecha = fecha, ProveedorId = proveedorId, EntradaId = id, Valor = pagado, UsuarioId = usuarioId }, tran);

                    tran.Commit();
                    return id;
                }
            }
        }

        /**
         * @brief Busca una entrada sin sus líneas.
         * @param id Número de la entrada
         * @return Entrada, o null si no existe
         */
        public Entrada GetById(int id)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.QueryFirstOrDefault<Entrada>(SelectSql + " WHERE e.id = @Id", new { Id = id });
        }

        /**
         * @brief Obtiene las líneas de una entrada.
         * @param id Número de la entrada
         * @return Lineas con el nombre de cada repuesto
         */
        public List<LineaDocumento> GetItems(int id)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Query<LineaDocumento>(@"
                    SELECT d.repuesto_id AS RepuestoId, r.codigo AS Codigo, r.nombre AS Nombre, d.cantidad AS Cantidad,
                           d.costo_unitario AS PrecioUnitario, d.costo_unitario AS CostoUnitario, d.subtotal AS Subtotal
                    FROM entrada_detalle d
                    JOIN repuesto r ON r.id = d.repuesto_id
                    WHERE d.entrada_id = @Id
                    ORDER BY r.nombre COLLATE NOCASE", new { Id = id }).ToList();
        }

        /**
         * @brief Busca entradas, de la más reciente a la más antigua.
         * @param desde Fecha inicial (incluida), o null
         * @param hasta Fecha final (excluida), o null
         * @param proveedorId Solo las de este proveedor, o null
         * @return Entradas que coinciden
         */
        public IEnumerable<Entrada> Buscar(DateTime? desde, DateTime? hasta, int? proveedorId)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Query<Entrada>(
                    SelectSql + " WHERE " + Sql.FiltroFecha("e.fecha") + " AND (@ProveedorId IS NULL OR e.proveedor_id = @ProveedorId) ORDER BY e.id DESC",
                    new { Desde = Sql.Fecha(desde), Hasta = Sql.Fecha(hasta), ProveedorId = proveedorId }).ToList();
        }
    }
}
