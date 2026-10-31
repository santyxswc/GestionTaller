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
     * para futuras migraciones.
     */
    public sealed class InicializadorBaseDatos
    {
        /** Versión actual del esquema. */
        public const int VersionEsquema = 1;

        /** Fábrica de conexiones a la base de datos. */
        private readonly IDbConnectionFactory _conexiones;

        /**
         * @brief Crea el inicializador.
         * @param conexiones Fábrica de conexiones a la base de datos
         */
        public InicializadorBaseDatos(IDbConnectionFactory conexiones) => _conexiones = conexiones;

        /**
         * @brief Crea las tablas que falten.
         */
        public void Inicializar()
        {
            using (IDbConnection db = _conexiones.Crear())
            {
                db.Open();
                db.Execute("PRAGMA journal_mode = WAL;");
                int version = db.ExecuteScalar<int>("PRAGMA user_version;");
                if (version >= VersionEsquema)
                    return;

                using (var tran = db.BeginTransaction())
                {
                    db.Execute(Esquema, transaction: tran);
                    db.Execute($"PRAGMA user_version = {VersionEsquema};", transaction: tran);
                    tran.Commit();
                }
            }
        }

        /** Tablas e índices (versión 1). Los valores en pesos se guardan como enteros. */
        private const string Esquema = @"
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
