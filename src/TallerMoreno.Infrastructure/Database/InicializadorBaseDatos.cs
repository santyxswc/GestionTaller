/**
 * @file InicializadorBaseDatos.cs
 * @brief Creación de las tablas de la base de datos.
 * @author Santiago Caicedo
 */
using System.Data;
using Dapper;
using TallerMoreno.Core.Abstractions;

namespace TallerMoreno.Infrastructure.Database
{
    /**
     * @brief Crea las tablas la primera vez que se abre la aplicación.
     *
     * No hay que instalar ni configurar nada: si el archivo de la base de datos no existe
     * se crea vacío con todas sus tablas. PRAGMA user_version guarda la versión del esquema
     * y cada versión nueva se aplica encima de la anterior sin tocar los datos.
     */
    public sealed class InicializadorBaseDatos
    {
        /** Versión actual del esquema. */
        public const int VersionEsquema = 4;

        /** Fábrica de conexiones a la base de datos. */
        private readonly IDbConnectionFactory _conexiones;

        /**
         * @brief Crea el inicializador.
         * @param conexiones Fábrica de conexiones a la base de datos
         */
        public InicializadorBaseDatos(IDbConnectionFactory conexiones) => _conexiones = conexiones;

        /**
         * @brief Versión del esquema guardada en la base de datos.
         * @return 0 si la base de datos es nueva
         */
        public int VersionGuardada()
        {
            using (IDbConnection db = _conexiones.Crear())
                return db.ExecuteScalar<int>("PRAGMA user_version;");
        }

        /**
         * @brief Crea las tablas que falten y aplica las versiones nuevas del esquema.
         * @exception InvalidOperationException Si la base de datos es de una versión más nueva del programa
         */
        public void Inicializar()
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                db.Open();
                db.Execute("PRAGMA journal_mode = WAL;");
                int version = db.ExecuteScalar<int>("PRAGMA user_version;");
                if (version > VersionEsquema)
                    throw new InvalidOperationException(MensajeVersionNueva);
                if (version == VersionEsquema)
                    return;

                using (var tran = db.BeginTransaction())
                {
                    if (version < 1)
                        db.Execute(EsquemaV1, transaction: tran);
                    if (version < 2)
                        db.Execute(EsquemaV2, transaction: tran);
                    if (version < 3)
                        db.Execute(EsquemaV3, transaction: tran);
                    if (version < 4)
                    {
                        AgregarColumna(db, tran, "cliente", "kilometraje", "INTEGER");
                        AgregarColumna(db, tran, "cliente", "kilometraje_fecha", "TEXT");
                        AgregarColumna(db, tran, "venta", "kilometraje", "INTEGER");
                        db.Execute(EsquemaV4, transaction: tran);
                    }
                    db.Execute($"PRAGMA user_version = {VersionEsquema};", transaction: tran);
                    tran.Commit();
                }
            }
        }

        /**
         * @brief Agrega una columna a una tabla si todavía no la tiene, así la versión se puede volver a aplicar.
         * @param db Conexión abierta
         * @param tran Transacción en curso
         * @param tabla Tabla
         * @param columna Columna nueva
         * @param tipo Tipo de la columna
         */
        private static void AgregarColumna(IDbConnection db, IDbTransaction tran, string tabla, string columna, string tipo)
        {
            bool existe = db.Query<string>($"SELECT name FROM pragma_table_info('{tabla}')", transaction: tran)
                            .Any(n => string.Equals(n, columna, StringComparison.OrdinalIgnoreCase));
            if (!existe)
                db.Execute($"ALTER TABLE {tabla} ADD COLUMN {columna} {tipo};", transaction: tran);
        }

        /** Mensaje cuando la base de datos la creó una versión más nueva del programa. */
        public const string MensajeVersionNueva =
            "Estos datos son de una versión más nueva de Taller Moreno. Instala la última versión del programa.";

        /**
         * Versión 4: kilometraje del carro de cada cliente y devoluciones o garantías de clientes y a proveedores.
         *
         * Una devolución de cliente sale de una venta. "Cambio" entrega otro repuesto igual, "Dinero" le devuelve
         * la plata y "Descuento" se la rebaja de lo que debe. A un proveedor también se le puede dejar "Pendiente"
         * mientras responde por la garantía.
         */
        private const string EsquemaV4 = @"
CREATE TABLE IF NOT EXISTS devolucion_cliente (
    id                 INTEGER PRIMARY KEY AUTOINCREMENT,
    fecha              TEXT    NOT NULL,
    venta_id           INTEGER NOT NULL REFERENCES venta(id),
    cliente_id         INTEGER REFERENCES cliente(id),
    repuesto_id        INTEGER NOT NULL REFERENCES repuesto(id),
    cantidad           INTEGER NOT NULL CHECK (cantidad > 0),
    motivo             TEXT    NOT NULL,
    resultado          TEXT    NOT NULL CHECK (resultado IN ('Cambio', 'Dinero', 'Descuento')),
    valor              INTEGER NOT NULL DEFAULT 0 CHECK (valor >= 0),
    vuelve_inventario  INTEGER NOT NULL DEFAULT 0,
    usuario_id         INTEGER REFERENCES usuario(id),
    notas              TEXT
);

CREATE TABLE IF NOT EXISTS devolucion_proveedor (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    fecha            TEXT    NOT NULL,
    proveedor_id     INTEGER NOT NULL REFERENCES proveedor(id) ON DELETE CASCADE,
    repuesto_id      INTEGER NOT NULL REFERENCES repuesto(id),
    cantidad         INTEGER NOT NULL CHECK (cantidad > 0),
    motivo           TEXT    NOT NULL,
    resultado        TEXT    NOT NULL CHECK (resultado IN ('Cambio', 'Dinero', 'Descuento', 'Pendiente')),
    valor            INTEGER NOT NULL DEFAULT 0 CHECK (valor >= 0),
    sale_inventario  INTEGER NOT NULL DEFAULT 0,
    resuelta_fecha   TEXT,
    usuario_id       INTEGER REFERENCES usuario(id),
    notas            TEXT
);

CREATE INDEX IF NOT EXISTS ix_devolucion_cliente_venta ON devolucion_cliente(venta_id);
CREATE INDEX IF NOT EXISTS ix_devolucion_cliente ON devolucion_cliente(cliente_id);
CREATE INDEX IF NOT EXISTS ix_devolucion_proveedor ON devolucion_proveedor(proveedor_id);
";

        /**
         * Versión 3: pagos a proveedores y abonos de clientes, para saber cuánto se debe.
         * Lo que ya estaba registrado se toma como pagado completo, así nadie amanece debiendo.
         * Los INSERT no repiten pagos si la versión se vuelve a aplicar.
         */
        private const string EsquemaV3 = @"
CREATE TABLE IF NOT EXISTS pago_proveedor (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    fecha         TEXT    NOT NULL,
    proveedor_id  INTEGER NOT NULL REFERENCES proveedor(id) ON DELETE CASCADE,
    entrada_id    INTEGER REFERENCES entrada(id) ON DELETE CASCADE,
    valor         INTEGER NOT NULL CHECK (valor > 0),
    metodo_pago   TEXT,
    usuario_id    INTEGER REFERENCES usuario(id),
    notas         TEXT
);

CREATE TABLE IF NOT EXISTS abono_cliente (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    fecha         TEXT    NOT NULL,
    cliente_id    INTEGER NOT NULL REFERENCES cliente(id) ON DELETE CASCADE,
    venta_id      INTEGER REFERENCES venta(id) ON DELETE CASCADE,
    valor         INTEGER NOT NULL CHECK (valor > 0),
    metodo_pago   TEXT,
    usuario_id    INTEGER REFERENCES usuario(id),
    notas         TEXT
);

CREATE INDEX IF NOT EXISTS ix_pago_proveedor ON pago_proveedor(proveedor_id);
CREATE INDEX IF NOT EXISTS ix_pago_proveedor_entrada ON pago_proveedor(entrada_id);
CREATE INDEX IF NOT EXISTS ix_abono_cliente ON abono_cliente(cliente_id);
CREATE INDEX IF NOT EXISTS ix_abono_cliente_venta ON abono_cliente(venta_id);

INSERT INTO pago_proveedor (fecha, proveedor_id, entrada_id, valor, usuario_id)
SELECT e.fecha, e.proveedor_id, e.id, e.total, e.usuario_id
FROM entrada e
WHERE e.total > 0 AND NOT EXISTS (SELECT 1 FROM pago_proveedor g WHERE g.entrada_id = e.id);

INSERT INTO abono_cliente (fecha, cliente_id, venta_id, valor, metodo_pago, usuario_id)
SELECT v.fecha, v.cliente_id, v.id, v.total, v.metodo_pago, v.usuario_id
FROM venta v
WHERE v.cliente_id IS NOT NULL AND v.total > 0 AND NOT EXISTS (SELECT 1 FROM abono_cliente a WHERE a.venta_id = v.id);
";

        /** Versión 2: ajustes generales, como el código de recuperación del dueño. */
        private const string EsquemaV2 = @"
CREATE TABLE IF NOT EXISTS ajuste (
    clave  TEXT PRIMARY KEY,
    valor  TEXT NOT NULL
);
";

        /** Tablas e índices (versión 1). Los valores en pesos se guardan como enteros. */
        private const string EsquemaV1 = @"
CREATE TABLE IF NOT EXISTS usuario (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    nombre          TEXT    NOT NULL,
    login           TEXT    NOT NULL UNIQUE,
    rol             TEXT    NOT NULL CHECK (rol IN ('Administrador', 'Mecánico')),
    activo          INTEGER NOT NULL DEFAULT 1,
    hash            TEXT    NOT NULL,
    sal             TEXT    NOT NULL,
    ultimo_ingreso  TEXT,
    creado          TEXT    NOT NULL DEFAULT (datetime('now', 'localtime'))
);

CREATE TABLE IF NOT EXISTS cliente (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    nombre     TEXT NOT NULL,
    documento  TEXT,
    telefono   TEXT,
    placa      TEXT,
    vehiculo   TEXT,
    notas      TEXT,
    creado     TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
);

CREATE TABLE IF NOT EXISTS proveedor (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    nombre     TEXT NOT NULL,
    nit        TEXT,
    contacto   TEXT,
    telefono   TEXT,
    correo     TEXT,
    direccion  TEXT,
    notas      TEXT,
    creado     TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
);

CREATE TABLE IF NOT EXISTS repuesto (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    codigo         TEXT    UNIQUE,
    nombre         TEXT    NOT NULL,
    categoria      TEXT    NOT NULL,
    marca          TEXT,
    precio_compra  INTEGER NOT NULL DEFAULT 0 CHECK (precio_compra >= 0),
    precio_venta   INTEGER NOT NULL CHECK (precio_venta > 0),
    stock          INTEGER NOT NULL DEFAULT 0 CHECK (stock >= 0),
    stock_minimo   INTEGER NOT NULL DEFAULT 2 CHECK (stock_minimo >= 0),
    ubicacion      TEXT,
    foto           TEXT,
    proveedor_id   INTEGER REFERENCES proveedor(id) ON DELETE SET NULL,
    descripcion    TEXT,
    creado         TEXT    NOT NULL DEFAULT (datetime('now', 'localtime'))
);

CREATE TABLE IF NOT EXISTS venta (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    fecha          TEXT    NOT NULL,
    cliente_id     INTEGER REFERENCES cliente(id),
    usuario_id     INTEGER NOT NULL REFERENCES usuario(id),
    metodo_pago    TEXT    NOT NULL,
    total          INTEGER NOT NULL,
    anulada        INTEGER NOT NULL DEFAULT 0,
    anulada_fecha  TEXT,
    anulada_por    INTEGER REFERENCES usuario(id),
    notas          TEXT
);

CREATE TABLE IF NOT EXISTS venta_detalle (
    venta_id         INTEGER NOT NULL REFERENCES venta(id) ON DELETE CASCADE,
    repuesto_id      INTEGER NOT NULL REFERENCES repuesto(id),
    cantidad         INTEGER NOT NULL CHECK (cantidad > 0),
    precio_unitario  INTEGER NOT NULL,
    costo_unitario   INTEGER NOT NULL,
    subtotal         INTEGER NOT NULL,
    PRIMARY KEY (venta_id, repuesto_id)
);

CREATE TABLE IF NOT EXISTS entrada (
    id                 INTEGER PRIMARY KEY AUTOINCREMENT,
    fecha              TEXT    NOT NULL,
    proveedor_id       INTEGER NOT NULL REFERENCES proveedor(id),
    usuario_id         INTEGER NOT NULL REFERENCES usuario(id),
    factura_proveedor  TEXT,
    total              INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS entrada_detalle (
    entrada_id      INTEGER NOT NULL REFERENCES entrada(id) ON DELETE CASCADE,
    repuesto_id     INTEGER NOT NULL REFERENCES repuesto(id),
    cantidad        INTEGER NOT NULL CHECK (cantidad > 0),
    costo_unitario  INTEGER NOT NULL,
    subtotal        INTEGER NOT NULL,
    PRIMARY KEY (entrada_id, repuesto_id)
);

CREATE TABLE IF NOT EXISTS movimiento_inventario (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    fecha             TEXT    NOT NULL,
    repuesto_id       INTEGER NOT NULL REFERENCES repuesto(id) ON DELETE CASCADE,
    tipo              TEXT    NOT NULL,
    cantidad          INTEGER NOT NULL,
    stock_resultante  INTEGER NOT NULL,
    referencia        TEXT,
    usuario_id        INTEGER REFERENCES usuario(id)
);

CREATE INDEX IF NOT EXISTS ix_venta_fecha ON venta(fecha);
CREATE INDEX IF NOT EXISTS ix_venta_cliente ON venta(cliente_id);
CREATE INDEX IF NOT EXISTS ix_venta_detalle_repuesto ON venta_detalle(repuesto_id);
CREATE INDEX IF NOT EXISTS ix_entrada_fecha ON entrada(fecha);
CREATE INDEX IF NOT EXISTS ix_entrada_proveedor ON entrada(proveedor_id);
CREATE INDEX IF NOT EXISTS ix_entrada_detalle_repuesto ON entrada_detalle(repuesto_id);
CREATE INDEX IF NOT EXISTS ix_movimiento_repuesto ON movimiento_inventario(repuesto_id, fecha);
CREATE INDEX IF NOT EXISTS ix_movimiento_fecha ON movimiento_inventario(fecha);
";
    }
}
