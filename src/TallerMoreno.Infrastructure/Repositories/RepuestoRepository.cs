/**
 * @file RepuestoRepository.cs
 * @brief Acceso a datos del inventario.
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
     * @brief Consultas y cambios de la tabla repuesto.
     */
    public class RepuestoRepository : IRepuestoRepository
    {
        /** Fábrica de conexiones a la base de datos. */
        private readonly IDbConnectionFactory _conexiones;

        /** Columnas de un repuesto con el nombre de su proveedor. */
        private const string SelectSql = @"
            SELECT r.id AS Id, r.codigo AS Codigo, r.nombre AS Nombre, r.categoria AS Categoria, r.marca AS Marca,
                   r.precio_compra AS PrecioCompra, r.precio_venta AS PrecioVenta, r.stock AS Stock,
                   r.stock_minimo AS StockMinimo, r.ubicacion AS Ubicacion, r.foto AS Foto,
                   r.proveedor_id AS ProveedorId, p.nombre AS ProveedorNombre, r.descripcion AS Descripcion
            FROM repuesto r
            LEFT JOIN proveedor p ON p.id = r.proveedor_id";

        /**
         * @brief Crea el repositorio.
         * @param conexiones Fábrica de conexiones a la base de datos
         */
        public RepuestoRepository(IDbConnectionFactory conexiones) => _conexiones = conexiones;

        /**
         * @brief Busca repuestos por nombre, código o marca y por categoría.
         * @param texto Texto a buscar (sin importar tildes ni mayúsculas); vacío no filtra
         * @param categoria Categoría; vacía no filtra
         * @return Repuestos ordenados por nombre
         */
        public IEnumerable<Repuesto> Buscar(string texto, string categoria)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                var repuestos = db.Query<Repuesto>(
                    SelectSql + " WHERE (@Categoria IS NULL OR r.categoria = @Categoria) ORDER BY r.nombre COLLATE NOCASE",
                    new { Categoria = string.IsNullOrWhiteSpace(categoria) ? null : categoria });
                return repuestos.Where(r => Texto.Coincide(texto, r.Nombre, r.Codigo, r.Marca, r.Ubicacion)).ToList();
            }
        }

        /**
         * @brief Busca un repuesto.
         * @param id Identificador
         * @return Repuesto, o null si no existe
         */
        public Repuesto GetById(int id)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.QueryFirstOrDefault<Repuesto>(SelectSql + " WHERE r.id = @Id", new { Id = id });
        }

        /**
         * @brief Indica si otro repuesto ya tiene ese código.
         * @param codigo Código a comprobar
         * @param exceptoId Repuesto que se está editando (0 al crear)
         * @return true si el código está en uso
         */
        public bool ExisteCodigo(string codigo, int exceptoId)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.ExecuteScalar<int>("SELECT COUNT(*) FROM repuesto WHERE codigo = @Codigo AND id <> @Id",
                                             new { Codigo = codigo, Id = exceptoId }) > 0;
        }

        /**
         * @brief Registra un repuesto y, si tiene unidades, su movimiento de alta.
         * @param r Datos del repuesto
         * @param usuarioId Usuario que lo registra
         * @return Identificador asignado
         */
        public int Insert(Repuesto r, int usuarioId)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                db.Open();
                using (var tran = db.BeginTransaction())
                {
                    db.Execute(@"
                        INSERT INTO repuesto (codigo, nombre, categoria, marca, precio_compra, precio_venta, stock,
                                              stock_minimo, ubicacion, foto, proveedor_id, descripcion)
                        VALUES (@Codigo, @Nombre, @Categoria, @Marca, @PrecioCompra, @PrecioVenta, @Stock,
                                @StockMinimo, @Ubicacion, @Foto, @ProveedorId, @Descripcion)", Parametros(r), tran);
                    int id = Sql.UltimoId(db, tran);

                    if (r.Stock > 0)
                        Sql.RegistrarMovimiento(db, tran, Sql.Ahora(), id, MovimientoInventario.TipoAlta, r.Stock, "Inventario inicial", usuarioId);

                    tran.Commit();
                    return id;
                }
            }
        }

        /**
         * @brief Actualiza los datos de un repuesto sin tocar su stock.
         * @param r Repuesto con los datos nuevos
         * @return true si se actualizó
         */
        public bool Update(Repuesto r)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                int filas = db.Execute(@"
                    UPDATE repuesto
                    SET codigo = @Codigo, nombre = @Nombre, categoria = @Categoria, marca = @Marca,
                        precio_compra = @PrecioCompra, precio_venta = @PrecioVenta, stock_minimo = @StockMinimo,
                        ubicacion = @Ubicacion, foto = @Foto, proveedor_id = @ProveedorId, descripcion = @Descripcion
                    WHERE id = @Id", Parametros(r));
                return filas > 0;
            }
        }

        /**
         * @brief Cambia el stock a un conteo nuevo y registra el ajuste.
         * @param id Repuesto
         * @param nuevoStock Unidades contadas
         * @param motivo Motivo del ajuste
         * @param usuarioId Usuario que ajusta
         * @return Diferencia entre el conteo nuevo y el anterior
         */
        public int AjustarStock(int id, int nuevoStock, string motivo, int usuarioId)
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                db.Open();
                using (var tran = db.BeginTransaction())
                {
                    int? actual = db.ExecuteScalar<int?>("SELECT stock FROM repuesto WHERE id = @Id", new { Id = id }, tran);
                    if (actual == null)
                        throw new InvalidOperationException("El repuesto ya no existe.");

                    int diferencia = nuevoStock - actual.Value;
                    if (diferencia != 0)
                    {
                        db.Execute("UPDATE repuesto SET stock = @Stock WHERE id = @Id", new { Id = id, Stock = nuevoStock }, tran);
                        Sql.RegistrarMovimiento(db, tran, Sql.Ahora(), id, MovimientoInventario.TipoAjuste, diferencia, motivo, usuarioId);
                    }
                    tran.Commit();
                    return diferencia;
                }
            }
        }

        /**
         * @brief Cuenta las ventas, entradas y devoluciones a proveedores en las que aparece el repuesto.
         * @param id Repuesto
         * @return Número de líneas
         */
        public int ContarUsos(int id)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.ExecuteScalar<int>(@"
                    SELECT (SELECT COUNT(*) FROM venta_detalle WHERE repuesto_id = @Id)
                         + (SELECT COUNT(*) FROM entrada_detalle WHERE repuesto_id = @Id)
                         + (SELECT COUNT(*) FROM devolucion_proveedor WHERE repuesto_id = @Id)", new { Id = id });
        }

        /**
         * @brief Elimina el repuesto; sus movimientos se borran en cascada.
         * @param id Repuesto
         * @return true si se eliminó
         */
        public bool Delete(int id)
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.Execute("DELETE FROM repuesto WHERE id = @Id", new { Id = id }) > 0;
        }

        /**
         * @brief Parámetros de las consultas de alta y cambio.
         * @param r Repuesto
         * @return Objeto anónimo con los valores
         */
        private static object Parametros(Repuesto r) => new
        {
            r.Id,
            r.Codigo,
            r.Nombre,
            r.Categoria,
            r.Marca,
            r.PrecioCompra,
            r.PrecioVenta,
            r.Stock,
            r.StockMinimo,
            r.Ubicacion,
            r.Foto,
            r.ProveedorId,
            r.Descripcion
        };
    }
}
