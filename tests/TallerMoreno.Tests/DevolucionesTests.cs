/**
 * @file DevolucionesTests.cs
 * @brief Pruebas de devoluciones, garantías y kilometraje.
 * @author Santiago Caicedo
 */
using Microsoft.Data.Sqlite;
using TallerMoreno.Core.Models;
using TallerMoreno.Infrastructure.Database;

namespace TallerMoreno.Tests
{
    /**
     * @brief Lo que devuelven los clientes, lo que se le devuelve a un proveedor y el kilometraje de los carros.
     */
    public class DevolucionesTests
    {
        [Fact]
        public void Cliente_GarantiaConCambio_SacaOtroDelInventario_YNoTocaLaPlata()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 20, precio: 350000);
            int cliente = e.Clientes.Crear(new Cliente { Nombre = "Carlos Pérez" });
            int venta = e.Ventas.Vender(cliente, "Efectivo", new[] { Entorno.Linea(bateria, 2) });

            e.Devoluciones.DevolverDeCliente(venta, bateria, 1, Devolucion.MotivoGarantia, Devolucion.Cambio, 0, vuelveInventario: false, "No carga");

            Assert.Equal(17, e.Repuestos.ObtenerPorId(bateria).Stock);
            var c = e.Clientes.ObtenerPorId(cliente);
            Assert.Equal(700000, c.TotalComprado);
            Assert.Equal(0, c.Deuda);
            Assert.Equal(1, c.Devoluciones);
            var detalle = e.Ventas.ObtenerDetalle(venta);
            Assert.Equal("Devolución", detalle.Estado);
            Assert.Equal(1, detalle.Items.Single().Devueltas);
            var d = e.Devoluciones.DeCliente(cliente).Single();
            Assert.Equal("No carga", d.Notas);
            Assert.Equal("se le cambió por otro", d.TextoResultado(false));
            Assert.Contains(e.Repuestos.Movimientos(bateria), m => m.Tipo == MovimientoInventario.TipoGarantia && m.Cantidad == -1);
        }

        [Fact]
        public void Cliente_DevolverLaPlata_ElRepuestoVuelveAlInventario_YBajaLoComprado()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 20, precio: 350000);
            int cliente = e.Clientes.Crear(new Cliente { Nombre = "Carlos Pérez" });
            int venta = e.Ventas.Vender(cliente, "Efectivo", new[] { Entorno.Linea(bateria, 2) });

            e.Devoluciones.DevolverDeCliente(venta, bateria, 1, "No le sirvió", Devolucion.Dinero, 350000, vuelveInventario: true);

            Assert.Equal(19, e.Repuestos.ObtenerPorId(bateria).Stock);
            var c = e.Clientes.ObtenerPorId(cliente);
            Assert.Equal(350000, c.TotalComprado);
            Assert.Equal(350000, c.TotalPagado);
            Assert.Equal(0, c.Deuda);
        }

        [Fact]
        public void Cliente_Fiado_SeRebajaDeLaDeuda_YNoSeLeDevuelvePlataQueNoPago()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 20, precio: 350000);
            int cliente = e.Clientes.Crear(new Cliente { Nombre = "Carlos Pérez" });
            int venta = e.Ventas.Vender(cliente, "Efectivo", new[] { Entorno.Linea(bateria, 2) }, abono: 0);

            Assert.Throws<ArgumentException>(() =>
                e.Devoluciones.DevolverDeCliente(venta, bateria, 1, "No le sirvió", Devolucion.Dinero, 350000, true));
            e.Devoluciones.DevolverDeCliente(venta, bateria, 1, "No le sirvió", Devolucion.Descuento, 350000, true);

            Assert.Equal(350000, e.Clientes.ObtenerPorId(cliente).Deuda);
        }

        [Fact]
        public void Cliente_NoSePuedeDevolverMasDeLoVendido_NiMasPlata_NiAnularDespues()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 20, precio: 350000);
            int filtro = e.NuevoRepuesto("Filtro de aceite", stock: 5, precio: 25000);
            int cliente = e.Clientes.Crear(new Cliente { Nombre = "Carlos Pérez" });
            int venta = e.Ventas.Vender(cliente, "Efectivo", new[] { Entorno.Linea(bateria, 2) });
            int mostrador = e.Ventas.Vender(null, "Efectivo", new[] { Entorno.Linea(bateria, 1) });

            Assert.Throws<ArgumentException>(() => e.Devoluciones.DevolverDeCliente(venta, bateria, 3, "Garantía", Devolucion.Cambio, 0, false));
            Assert.Throws<ArgumentException>(() => e.Devoluciones.DevolverDeCliente(venta, filtro, 1, "Garantía", Devolucion.Cambio, 0, false));
            Assert.Throws<ArgumentException>(() => e.Devoluciones.DevolverDeCliente(venta, bateria, 1, "Garantía", Devolucion.Dinero, 400000, false));
            Assert.Throws<ArgumentException>(() => e.Devoluciones.DevolverDeCliente(venta, bateria, 1, "", Devolucion.Cambio, 0, false));
            Assert.Throws<ArgumentException>(() => e.Devoluciones.DevolverDeCliente(mostrador, bateria, 1, "Garantía", Devolucion.Descuento, 1000, false));

            e.Devoluciones.DevolverDeCliente(venta, bateria, 2, "Garantía", Devolucion.Cambio, 0, false);
            Assert.Throws<ArgumentException>(() => e.Devoluciones.DevolverDeCliente(venta, bateria, 1, "Garantía", Devolucion.Cambio, 0, false));
            Assert.Throws<InvalidOperationException>(() => e.Ventas.Anular(venta));

            e.Devoluciones.DevolverDeCliente(mostrador, bateria, 1, "No le sirvió", Devolucion.Dinero, 350000, true);
            Assert.Single(e.Devoluciones.DeVenta(mostrador));
        }

        [Fact]
        public void Cliente_CambioSinUnidadesEnInventario_NoSeGuarda()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 1, precio: 350000);
            int cliente = e.Clientes.Crear(new Cliente { Nombre = "Carlos Pérez" });
            int venta = e.Ventas.Vender(cliente, "Efectivo", new[] { Entorno.Linea(bateria, 1) });

            Assert.Throws<InvalidOperationException>(() => e.Devoluciones.DevolverDeCliente(venta, bateria, 1, "Garantía", Devolucion.Cambio, 0, false));
            Assert.Empty(e.Devoluciones.DeVenta(venta));
            Assert.Equal(0, e.Repuestos.ObtenerPorId(bateria).Stock);
        }

        [Fact]
        public void Cliente_BorrarDevolucion_DeshaceElInventario_YSoloElDueño()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 20, precio: 350000);
            int cliente = e.Clientes.Crear(new Cliente { Nombre = "Carlos Pérez" });
            int venta = e.Ventas.Vender(cliente, "Efectivo", new[] { Entorno.Linea(bateria, 2) });
            int id = e.Devoluciones.DevolverDeCliente(venta, bateria, 1, "Garantía", Devolucion.Cambio, 0, vuelveInventario: true);
            Assert.Equal(18, e.Repuestos.ObtenerPorId(bateria).Stock);

            e.Devoluciones.EliminarDeCliente(id);

            Assert.Equal(18, e.Repuestos.ObtenerPorId(bateria).Stock);
            Assert.Empty(e.Devoluciones.DeCliente(cliente));
            e.Ventas.Anular(venta);
            Assert.Equal(20, e.Repuestos.ObtenerPorId(bateria).Stock);
        }

        [Fact]
        public void Proveedor_GarantiaPendiente_LuegoLaCambia()
        {
            using var e = new Entorno();
            int filtro = e.NuevoRepuesto("Filtro de aceite", stock: 0);
            int prov = e.Proveedores.Crear(new Proveedor { Nombre = "Autopartes El Sol" });
            e.Entradas.Registrar(prov, null, new[] { Entorno.Linea(filtro, 10, 20000) });

            int id = e.Devoluciones.DevolverAProveedor(prov, filtro, 2, Devolucion.MotivoGarantia, Devolucion.Pendiente, 0, saleInventario: true);

            Assert.Equal(8, e.Repuestos.ObtenerPorId(filtro).Stock);
            var p = e.Proveedores.ObtenerPorId(prov);
            Assert.Equal(1, p.Pendientes);
            Assert.Equal("1 · 1 pendiente", p.DevolucionesTexto);

            e.Devoluciones.Resolver(id, Devolucion.Cambio, 0, "Llegaron nuevos");

            Assert.Equal(10, e.Repuestos.ObtenerPorId(filtro).Stock);
            p = e.Proveedores.ObtenerPorId(prov);
            Assert.Equal(0, p.Pendientes);
            Assert.Equal(200000, p.TotalComprado);
            Assert.Equal("Llegaron nuevos", e.Devoluciones.ObtenerDeProveedor(id).Notas);
            Assert.Throws<ArgumentException>(() => e.Devoluciones.Resolver(id, Devolucion.Cambio, 0));
        }

        [Fact]
        public void Proveedor_RebajaDeLaDeuda_O_DevuelvePlata()
        {
            using var e = new Entorno();
            int filtro = e.NuevoRepuesto("Filtro de aceite", stock: 0);
            int credito = e.Proveedores.Crear(new Proveedor { Nombre = "Autopartes El Sol" });
            int contado = e.Proveedores.Crear(new Proveedor { Nombre = "Repuestos La 30" });
            e.Entradas.Registrar(credito, null, new[] { Entorno.Linea(filtro, 10, 20000) }, pagado: 0);
            e.Entradas.Registrar(contado, null, new[] { Entorno.Linea(filtro, 10, 20000) });

            Assert.Throws<ArgumentException>(() => e.Devoluciones.DevolverAProveedor(credito, filtro, 1, "Llegó dañado", Devolucion.Dinero, 20000, true));
            Assert.Throws<ArgumentException>(() => e.Devoluciones.DevolverAProveedor(contado, filtro, 1, "Llegó dañado", Devolucion.Descuento, 20000, true));
            Assert.Throws<ArgumentException>(() => e.Devoluciones.DevolverAProveedor(credito, filtro, 1, "Llegó dañado", Devolucion.Descuento, 300000, true));

            e.Devoluciones.DevolverAProveedor(credito, filtro, 1, "Llegó dañado", Devolucion.Descuento, 20000, true);
            e.Devoluciones.DevolverAProveedor(contado, filtro, 2, "Pedido equivocado", Devolucion.Dinero, 40000, true);

            var p = e.Proveedores.ObtenerPorId(credito);
            Assert.Equal(180000, p.Deuda);
            var q = e.Proveedores.ObtenerPorId(contado);
            Assert.Equal(160000, q.TotalComprado);
            Assert.Equal(160000, q.TotalPagado);
            Assert.Equal(0, q.Deuda);
            Assert.Equal(17, e.Repuestos.ObtenerPorId(filtro).Stock);
        }

        [Fact]
        public void Proveedor_RepuestoQueDevolvioUnCliente_NoSaleDelInventario()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 5, precio: 350000);
            int prov = e.Proveedores.Crear(new Proveedor { Nombre = "Autopartes El Sol" });
            int cliente = e.Clientes.Crear(new Cliente { Nombre = "Carlos Pérez" });
            int venta = e.Ventas.Vender(cliente, "Efectivo", new[] { Entorno.Linea(bateria, 1) });
            e.Devoluciones.DevolverDeCliente(venta, bateria, 1, "Garantía", Devolucion.Cambio, 0, vuelveInventario: false);
            Assert.Equal(3, e.Repuestos.ObtenerPorId(bateria).Stock);

            e.Devoluciones.DevolverAProveedor(prov, bateria, 1, "Garantía", Devolucion.Cambio, 0, saleInventario: false);

            Assert.Equal(4, e.Repuestos.ObtenerPorId(bateria).Stock);
            Assert.Throws<InvalidOperationException>(() => e.Devoluciones.DevolverAProveedor(prov, bateria, 9, "Garantía", Devolucion.Pendiente, 0, true));
            Assert.Throws<InvalidOperationException>(() => e.Repuestos.Eliminar(bateria));
        }

        [Fact]
        public void Proveedor_BorrarDevolucion_DeshaceElInventario()
        {
            using var e = new Entorno();
            int filtro = e.NuevoRepuesto("Filtro de aceite", stock: 10);
            int prov = e.Proveedores.Crear(new Proveedor { Nombre = "Autopartes El Sol" });
            int id = e.Devoluciones.DevolverAProveedor(prov, filtro, 3, "Garantía", Devolucion.Pendiente, 0, saleInventario: true);
            Assert.Equal(7, e.Repuestos.ObtenerPorId(filtro).Stock);

            e.Devoluciones.EliminarAProveedor(id);

            Assert.Equal(10, e.Repuestos.ObtenerPorId(filtro).Stock);
            Assert.Empty(e.Devoluciones.DeProveedor(prov));
        }

        [Fact]
        public void Kilometraje_SeAnotaAlVender_YEnElCliente()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 20);
            int cliente = e.Clientes.Crear(new Cliente { Nombre = "Carlos Pérez", Placa = "abc 123", Kilometraje = 80000 });
            var c = e.Clientes.ObtenerPorId(cliente);
            Assert.Equal(80000, c.Kilometraje);
            Assert.NotNull(c.KilometrajeFecha);
            Assert.Equal("80.000 km", c.KilometrajeTexto);

            int venta = e.Ventas.Vender(cliente, "Efectivo", new[] { Entorno.Linea(bateria, 1) }, kilometraje: 85500);
            int mostrador = e.Ventas.Vender(null, "Efectivo", new[] { Entorno.Linea(bateria, 1) }, kilometraje: 1000);

            Assert.Equal(85500, e.Clientes.ObtenerPorId(cliente).Kilometraje);
            Assert.Equal(85500, e.Ventas.ObtenerDetalle(venta).Kilometraje);
            Assert.Null(e.Ventas.ObtenerDetalle(mostrador).Kilometraje);

            c = e.Clientes.ObtenerPorId(cliente);
            c.Kilometraje = 90000;
            e.Clientes.Actualizar(c);
            Assert.Equal(90000, e.Clientes.ObtenerPorId(cliente).Kilometraje);
            c.Kilometraje = null;
            e.Clientes.Actualizar(c);
            Assert.Null(e.Clientes.ObtenerPorId(cliente).KilometrajeFecha);

            c.Kilometraje = -5;
            Assert.Throws<ArgumentException>(() => e.Clientes.Actualizar(c));
            Assert.Throws<ArgumentException>(() => e.Ventas.Vender(cliente, "Efectivo", new[] { Entorno.Linea(bateria, 1) }, kilometraje: -1));
        }

        [Fact]
        public void Migracion_DesdeLaVersion3_ConservaLosDatos_YSePuedeRepetir()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 10, precio: 350000);
            int cliente = e.Clientes.Crear(new Cliente { Nombre = "Carlos Pérez" });
            e.Ventas.Vender(cliente, "Efectivo", new[] { Entorno.Linea(bateria, 2) }, abono: 100000);

            // Así quedan los datos de la versión 1.3: sin kilometraje ni devoluciones.
            using (var db = new SqliteConnection($"Data Source={e.RutaBaseDatos}"))
            {
                db.Open();
                using var cmd = db.CreateCommand();
                cmd.CommandText = @"DROP TABLE devolucion_cliente; DROP TABLE devolucion_proveedor;
                                    ALTER TABLE cliente DROP COLUMN kilometraje; ALTER TABLE cliente DROP COLUMN kilometraje_fecha;
                                    ALTER TABLE venta DROP COLUMN kilometraje; PRAGMA user_version = 3;";
                cmd.ExecuteNonQuery();
            }
            SqliteConnection.ClearAllPools();

            e.Inicializador.Inicializar();
            e.Inicializador.Inicializar();

            Assert.Equal(InicializadorBaseDatos.VersionEsquema, e.Inicializador.VersionGuardada());
            var c = e.Clientes.ObtenerPorId(cliente);
            Assert.Equal(600000, c.Deuda);
            Assert.Null(c.Kilometraje);
            Assert.Equal(0, c.Devoluciones);
        }
    }
}
