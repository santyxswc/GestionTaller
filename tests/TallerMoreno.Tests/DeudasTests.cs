/**
 * @file DeudasTests.cs
 * @brief Pruebas de lo que se les debe a los proveedores y lo que deben los clientes.
 * @author Santiago Caicedo
 */
using Microsoft.Data.Sqlite;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Services;
using TallerMoreno.Infrastructure.Database;

namespace TallerMoreno.Tests
{
    /**
     * @brief Pagos parciales a proveedores, ventas fiadas, abonos y la migración de los datos que ya había.
     */
    public class DeudasTests
    {
        [Fact]
        public void Proveedor_PagoParcialAlRecibir_QuedaDebiendoElResto()
        {
            using var e = new Entorno();
            int filtro = e.NuevoRepuesto("Filtro de aceite", stock: 0);
            int prov = e.Proveedores.Crear(new Proveedor { Nombre = "Autopartes El Sol" });

            e.Entradas.Registrar(prov, "F-1", new[] { Entorno.Linea(filtro, 10, 20000) }, pagado: 50000);

            var p = e.Proveedores.ObtenerPorId(prov);
            Assert.Equal(200000, p.TotalComprado);
            Assert.Equal(50000, p.TotalPagado);
            Assert.Equal(150000, p.Deuda);
            Assert.True(e.Proveedores.Entradas(prov).Single().QuedoDebiendo);
        }

        [Fact]
        public void Proveedor_SinDecirCuantoSePago_QuedaPagadoCompleto()
        {
            using var e = new Entorno();
            int filtro = e.NuevoRepuesto("Filtro de aceite", stock: 0);
            int prov = e.Proveedores.Crear(new Proveedor { Nombre = "Autopartes El Sol" });

            e.Entradas.Registrar(prov, null, new[] { Entorno.Linea(filtro, 2, 20000) });

            var p = e.Proveedores.ObtenerPorId(prov);
            Assert.Equal(0, p.Deuda);
            Assert.Equal("Al día", p.DeudaTexto);
        }

        [Fact]
        public void Proveedor_PagosPosteriores_BajanLaDeuda_YNoPuedenPasarse()
        {
            using var e = new Entorno();
            int filtro = e.NuevoRepuesto("Filtro de aceite", stock: 0);
            int prov = e.Proveedores.Crear(new Proveedor { Nombre = "Autopartes El Sol" });
            e.Entradas.Registrar(prov, null, new[] { Entorno.Linea(filtro, 10, 20000) }, pagado: 0);

            e.Proveedores.RegistrarPago(prov, 120000, "Transferencia", "Primer abono");
            Assert.Throws<ArgumentException>(() => e.Proveedores.RegistrarPago(prov, 90000, "Efectivo"));
            Assert.Throws<ArgumentException>(() => e.Proveedores.RegistrarPago(prov, 0, "Efectivo"));
            e.Proveedores.RegistrarPago(prov, 80000, "Efectivo");

            var p = e.Proveedores.ObtenerPorId(prov);
            Assert.Equal(0, p.Deuda);
            Assert.Equal(200000, p.TotalPagado);
            Assert.Throws<ArgumentException>(() => e.Proveedores.RegistrarPago(prov, 1000, "Efectivo"));

            var pagos = e.Proveedores.Pagos(prov).ToList();
            Assert.Equal(2, pagos.Count);
            Assert.All(pagos, g => Assert.Null(g.DocumentoId));
            Assert.Contains(pagos, g => g.Notas == "Primer abono" && g.MetodoPago == "Transferencia" && g.UsuarioNombre == "Don Moreno");
        }

        [Fact]
        public void Proveedor_PagadoMayorQueLaFactura_NoSeGuarda()
        {
            using var e = new Entorno();
            int filtro = e.NuevoRepuesto("Filtro de aceite", stock: 0);
            int prov = e.Proveedores.Crear(new Proveedor { Nombre = "Autopartes El Sol" });

            Assert.Throws<ArgumentException>(() => e.Entradas.Registrar(prov, null, new[] { Entorno.Linea(filtro, 1, 20000) }, pagado: 30000));
            Assert.Throws<ArgumentException>(() => e.Entradas.Registrar(prov, null, new[] { Entorno.Linea(filtro, 1, 20000) }, pagado: -1));
            Assert.Equal(0, e.Repuestos.ObtenerPorId(filtro).Stock);
        }

        [Fact]
        public void Proveedor_BorrarUnPago_VuelveLaDeuda_SoloAdministrador()
        {
            using var e = new Entorno();
            int filtro = e.NuevoRepuesto("Filtro de aceite", stock: 0);
            int prov = e.Proveedores.Crear(new Proveedor { Nombre = "Autopartes El Sol" });
            e.Entradas.Registrar(prov, null, new[] { Entorno.Linea(filtro, 5, 20000) }, pagado: 0);
            int pago = e.Proveedores.RegistrarPago(prov, 40000, "Efectivo");

            e.Usuarios.CrearUsuario("Juan Mecánico", "juan", "1234", Usuario.RolMecanico);
            e.Usuarios.CerrarSesion();
            e.Usuarios.IniciarSesion("juan", "1234");
            Assert.Throws<UnauthorizedAccessException>(() => e.Proveedores.EliminarPago(pago));
            e.Usuarios.CerrarSesion();
            e.Usuarios.IniciarSesion("moreno", "clave123");

            e.Proveedores.EliminarPago(pago);
            Assert.Equal(100000, e.Proveedores.ObtenerPorId(prov).Deuda);
        }

        [Fact]
        public void Cliente_VentaFiada_DebeLoQueNoAbono()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 10, precio: 350000);
            int cliente = e.Clientes.Crear(new Cliente { Nombre = "Carlos Pérez" });

            int venta = e.Ventas.Vender(cliente, "Efectivo", new[] { Entorno.Linea(bateria, 1) }, abono: 100000);
            e.Ventas.Vender(cliente, "Efectivo", new[] { Entorno.Linea(bateria, 1) });

            var c = e.Clientes.ObtenerPorId(cliente);
            Assert.Equal(700000, c.TotalComprado);
            Assert.Equal(450000, c.TotalPagado);
            Assert.Equal(250000, c.Deuda);
            var v = e.Ventas.ObtenerDetalle(venta);
            Assert.True(v.Fiada);
            Assert.Equal(100000, v.Abonado);
            Assert.Equal("Efectivo", v.MetodoPago);
        }

        [Fact]
        public void Cliente_FiadoSinAbono_QuedaComoFiado()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 10, precio: 350000);
            int cliente = e.Clientes.Crear(new Cliente { Nombre = "Carlos Pérez" });

            int venta = e.Ventas.Vender(cliente, "Efectivo", new[] { Entorno.Linea(bateria, 1) }, abono: 0);

            Assert.Equal(VentaService.MetodoFiado, e.Ventas.ObtenerDetalle(venta).MetodoPago);
            Assert.Equal(350000, e.Clientes.ObtenerPorId(cliente).Deuda);
        }

        [Fact]
        public void Fiar_SinCliente_OAbonoMayorQueElTotal_NoSeGuarda()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 10, precio: 350000);
            int cliente = e.Clientes.Crear(new Cliente { Nombre = "Carlos Pérez" });

            Assert.Throws<ArgumentException>(() => e.Ventas.Vender(null, "Efectivo", new[] { Entorno.Linea(bateria, 1) }, abono: 0));
            Assert.Throws<InvalidOperationException>(() => e.Ventas.Vender(cliente, "Efectivo", new[] { Entorno.Linea(bateria, 1) }, abono: 400000));
            Assert.Equal(10, e.Repuestos.ObtenerPorId(bateria).Stock);
            Assert.Empty(e.Clientes.Compras(cliente));
        }

        [Fact]
        public void Cliente_Abonos_BajanLaDeuda_YNoPuedenPasarse()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 10, precio: 350000);
            int cliente = e.Clientes.Crear(new Cliente { Nombre = "Carlos Pérez" });
            e.Ventas.Vender(cliente, "Efectivo", new[] { Entorno.Linea(bateria, 1) }, abono: 50000);

            e.Clientes.RegistrarAbono(cliente, 100000, "Nequi / Daviplata");
            Assert.Throws<ArgumentException>(() => e.Clientes.RegistrarAbono(cliente, 300000, "Efectivo"));
            e.Clientes.RegistrarAbono(cliente, 200000, "Efectivo");

            Assert.Equal(0, e.Clientes.ObtenerPorId(cliente).Deuda);
            Assert.Throws<ArgumentException>(() => e.Clientes.RegistrarAbono(cliente, 1000, "Efectivo"));
            Assert.Equal(2, e.Clientes.Abonos(cliente).Count(a => a.DocumentoId == null));
        }

        [Fact]
        public void Cliente_AnularVentaFiada_QuitaLaDeuda()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 10, precio: 350000);
            int cliente = e.Clientes.Crear(new Cliente { Nombre = "Carlos Pérez" });
            int venta = e.Ventas.Vender(cliente, "Efectivo", new[] { Entorno.Linea(bateria, 1) }, abono: 100000);

            e.Ventas.Anular(venta);

            var c = e.Clientes.ObtenerPorId(cliente);
            Assert.Equal(0, c.TotalComprado);
            Assert.Equal(0, c.TotalPagado);
            Assert.Equal(0, c.Deuda);
        }

        [Fact]
        public void VentaDeMostrador_NoDejaDeuda()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 10, precio: 350000);

            int venta = e.Ventas.Vender(null, "Efectivo", new[] { Entorno.Linea(bateria, 1) });

            var v = e.Ventas.ObtenerDetalle(venta);
            Assert.False(v.Fiada);
            Assert.Equal(350000, v.Abonado);
        }

        [Fact]
        public void Migracion_LoQueYaEstabaRegistrado_QuedaPagado()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 10, precio: 350000);
            int cliente = e.Clientes.Crear(new Cliente { Nombre = "Carlos Pérez" });
            int prov = e.Proveedores.Crear(new Proveedor { Nombre = "Autopartes El Sol" });
            e.Ventas.Vender(cliente, "Transferencia", new[] { Entorno.Linea(bateria, 2) });
            e.Entradas.Registrar(prov, null, new[] { Entorno.Linea(bateria, 3, 280000) });

            // Así quedan los datos de la versión 1.2: sin tablas de pagos.
            using (var db = new SqliteConnection($"Data Source={e.RutaBaseDatos}"))
            {
                db.Open();
                using var cmd = db.CreateCommand();
                cmd.CommandText = "DROP TABLE pago_proveedor; DROP TABLE abono_cliente; PRAGMA user_version = 2;";
                cmd.ExecuteNonQuery();
            }
            SqliteConnection.ClearAllPools();

            e.Inicializador.Inicializar();
            e.Inicializador.Inicializar();

            Assert.Equal(InicializadorBaseDatos.VersionEsquema, e.Inicializador.VersionGuardada());
            var c = e.Clientes.ObtenerPorId(cliente);
            Assert.Equal(700000, c.TotalComprado);
            Assert.Equal(0, c.Deuda);
            Assert.Equal("Transferencia", e.Clientes.Abonos(cliente).Single().MetodoPago);
            Assert.Equal(0, e.Proveedores.ObtenerPorId(prov).Deuda);
            Assert.Single(e.Proveedores.Pagos(prov));
        }
    }
}
