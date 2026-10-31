/**
 * @file ClientesYReportesTests.cs
 * @brief Pruebas de clientes, reportes, permisos y exportaciones.
 * @author Santiago Caicedo
 */
using ClosedXML.Excel;
using TallerMoreno.Core.Models;

namespace TallerMoreno.Tests
{
    /**
     * @brief Cuánto compra cada cliente, totales, permisos de los mecánicos y archivos generados.
     */
    public class ClientesYReportesTests
    {
        [Fact]
        public void Cliente_SumaSusComprasSinContarLasAnuladas()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 10, precio: 350000);
            int cliente = e.Clientes.Crear(new Cliente { Nombre = "Carlos Pérez", Placa = "abc 123" });

            e.Ventas.Vender(cliente, "Efectivo", new[] { Entorno.Linea(bateria, 1) });
            e.Ventas.Vender(cliente, "Transferencia", new[] { Entorno.Linea(bateria, 2) });
            int anulada = e.Ventas.Vender(cliente, "Efectivo", new[] { Entorno.Linea(bateria, 1) });
            e.Ventas.Anular(anulada);

            var c = e.Clientes.ObtenerPorId(cliente);
            Assert.Equal("ABC123", c.Placa);
            Assert.Equal(2, c.NumeroCompras);
            Assert.Equal(1050000, c.TotalComprado);
            Assert.NotNull(c.UltimaCompra);
            Assert.Equal(3, e.Clientes.Compras(cliente).Count());
        }

        [Fact]
        public void Venta_SinCliente_EsDeMostrador()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto();

            int venta = e.Ventas.Vender(null, "Efectivo", new[] { Entorno.Linea(bateria, 1) });

            Assert.Equal(Venta.ClienteMostrador, e.Ventas.ObtenerDetalle(venta).Cliente);
        }

        [Fact]
        public void Resumen_CalculaTotalUnidadesYGanancia()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 10, precio: 350000, costo: 280000);
            int filtro = e.NuevoRepuesto("Filtro", stock: 10, precio: 25000, costo: 15000);

            e.Ventas.Vender(null, "Efectivo", new[] { Entorno.Linea(bateria, 1), Entorno.Linea(filtro, 2) });

            var hoy = e.Reportes.VentasDeHoy();
            Assert.Equal(1, hoy.NumeroVentas);
            Assert.Equal(400000, hoy.Total);
            Assert.Equal(3, hoy.Unidades);
            Assert.Equal(90000, hoy.Ganancia);
            Assert.Equal("Batería 12V 45Ah", e.Reportes.TopRepuestos(Periodo.Hoy).Last().Nombre);
        }

        [Fact]
        public void Mecanico_PuedeVenderPeroNoCambiarPreciosNiAnular()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 5);
            e.Usuarios.CrearUsuario("Juan Mecánico", "juan", "1234", Usuario.RolMecanico);
            e.Usuarios.CerrarSesion();
            Assert.NotNull(e.Usuarios.IniciarSesion(" JUAN ", "1234"));

            int venta = e.Ventas.Vender(null, "Efectivo", new[] { Entorno.Linea(bateria, 1) });

            Assert.Equal(4, e.Repuestos.ObtenerPorId(bateria).Stock);
            Assert.Throws<UnauthorizedAccessException>(() => e.Ventas.Anular(venta));
            Assert.Throws<UnauthorizedAccessException>(() => e.Repuestos.Actualizar(e.Repuestos.ObtenerPorId(bateria)));
            Assert.Throws<UnauthorizedAccessException>(() => e.Reportes.Resumen(Periodo.Hoy));
        }

        [Fact]
        public void Login_ConClaveIncorrecta_DevuelveNull()
        {
            using var e = new Entorno();
            e.Usuarios.CerrarSesion();

            Assert.Null(e.Usuarios.IniciarSesion("moreno", "otra"));
            Assert.False(e.Sesion.Activa);
        }

        [Fact]
        public void PrimerAdministrador_SoloSePuedeCrearUnaVez()
        {
            using var e = new Entorno();

            Assert.True(e.Usuarios.HayUsuarios());
            Assert.Throws<InvalidOperationException>(() => e.Usuarios.CrearPrimerAdministrador("Otro", "otro", "1234"));
        }

        [Fact]
        public void CuentaDesactivada_NoPuedeEntrar()
        {
            using var e = new Entorno();
            var juan = e.Usuarios.CrearUsuario("Juan", "juan", "1234", Usuario.RolMecanico);
            e.Usuarios.CambiarEstado(juan.Id, false);
            e.Usuarios.CerrarSesion();

            Assert.Throws<InvalidOperationException>(() => e.Usuarios.IniciarSesion("juan", "1234"));
        }

        [Fact]
        public void ExcelInventario_TieneUnaFilaPorRepuestoConSuStockActual()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 20);
            e.NuevoRepuesto("Bombillo H4", stock: 2, precio: 15000, costo: 8000);
            e.Ventas.Vender(null, "Efectivo", new[] { Entorno.Linea(bateria, 1) });
            string ruta = Path.Combine(e.Carpeta, "inventario.xlsx");

            int filas = e.Exportacion.ExportarInventario(ruta);

            Assert.Equal(2, filas);
            using var libro = new XLWorkbook(ruta);
            var hoja = libro.Worksheet("Inventario");
            Assert.Equal("Repuesto", hoja.Cell(4, 2).GetString());
            Assert.Equal("Batería 12V 45Ah", hoja.Cell(5, 2).GetString());
            Assert.Equal(19, hoja.Cell(5, 7).GetValue<int>());
            Assert.Equal("Poco stock", hoja.Cell(6, 9).GetString());
        }

        [Fact]
        public void ExcelVentasYClientes_SeGeneran()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto();
            int cliente = e.Clientes.Crear(new Cliente { Nombre = "Ana" });
            e.Ventas.Vender(cliente, "Efectivo", new[] { Entorno.Linea(bateria, 1) });

            Assert.Equal(1, e.Exportacion.ExportarVentas(Periodo.Hoy, Path.Combine(e.Carpeta, "ventas.xlsx")));
            Assert.Equal(1, e.Exportacion.ExportarClientes(Path.Combine(e.Carpeta, "clientes.xlsx")));
        }

        [Fact]
        public void CopiaSeguridad_IncluyeBaseDeDatosYFotos()
        {
            using var e = new Entorno();
            e.Repuestos.Crear(new Repuesto { Nombre = "Llanta", Categoria = "Llantas", PrecioVenta = 1 }, new byte[] { 1 });
            string zip = Path.Combine(e.Carpeta, "copia.zip");

            e.Exportacion.CrearCopiaSeguridad(zip);

            using var archivo = System.IO.Compression.ZipFile.OpenRead(zip);
            Assert.Contains(archivo.Entries, x => x.FullName == "taller.db");
            Assert.Contains(archivo.Entries, x => x.FullName.StartsWith("fotos/"));
        }

        [Fact]
        public void Periodo_Semana_EmpiezaElLunes()
        {
            var (desde, hasta) = Periodo.Rango(Periodo.Semana, new DateTime(2026, 10, 25, 15, 0, 0));

            Assert.Equal(new DateTime(2026, 10, 19), desde);
            Assert.Equal(new DateTime(2026, 10, 26), hasta);
        }
    }
}
