/**
 * @file InventarioTests.cs
 * @brief Pruebas del conteo dinámico del inventario.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Models;

namespace TallerMoreno.Tests
{
    /**
     * @brief Ventas, entradas, anulaciones y ajustes cambian el stock y dejan su movimiento.
     */
    public class InventarioTests
    {
        [Fact]
        public void Vender_UnaBateriaDeVeinte_QuedanDiecinueve()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 20);

            e.Ventas.Vender(null, "Efectivo", new[] { Entorno.Linea(bateria, 1) });

            Assert.Equal(19, e.Repuestos.ObtenerPorId(bateria).Stock);
        }

        [Fact]
        public void Vender_SinStockSuficiente_NoGuardaNadaNiDescuenta()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 2);
            int filtro = e.NuevoRepuesto("Filtro de aceite", stock: 10, precio: 25000, costo: 15000);

            var error = Assert.Throws<InvalidOperationException>(() =>
                e.Ventas.Vender(null, "Efectivo", new[] { Entorno.Linea(filtro, 1), Entorno.Linea(bateria, 3) }));

            Assert.Contains("quedan 2", error.Message);
            Assert.Equal(2, e.Repuestos.ObtenerPorId(bateria).Stock);
            Assert.Equal(10, e.Repuestos.ObtenerPorId(filtro).Stock);
            Assert.Empty(e.Ventas.Buscar(Periodo.Todo));
        }

        [Fact]
        public void Vender_LineasRepetidas_SeUnenEnUnaSola()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 5);

            int venta = e.Ventas.Vender(null, "Efectivo", new[] { Entorno.Linea(bateria, 1), Entorno.Linea(bateria, 2) });

            var detalle = e.Ventas.ObtenerDetalle(venta);
            Assert.Single(detalle.Items);
            Assert.Equal(3, detalle.Items[0].Cantidad);
            Assert.Equal(1050000, detalle.Total);
            Assert.Equal(2, e.Repuestos.ObtenerPorId(bateria).Stock);
        }

        [Fact]
        public void Anular_DevuelveLasUnidadesYNoSePuedeAnularDosVeces()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 20);
            int venta = e.Ventas.Vender(null, "Efectivo", new[] { Entorno.Linea(bateria, 4) });

            e.Ventas.Anular(venta);

            Assert.Equal(20, e.Repuestos.ObtenerPorId(bateria).Stock);
            Assert.True(e.Ventas.ObtenerDetalle(venta).Anulada);
            Assert.Throws<InvalidOperationException>(() => e.Ventas.Anular(venta));
        }

        [Fact]
        public void Entrada_SumaStockActualizaCostoYAsignaProveedor()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 1, costo: 280000);
            int proveedor = e.Proveedores.Crear(new Proveedor { Nombre = "Baterías MAC" });

            e.Entradas.Registrar(proveedor, "F-123", new[] { Entorno.Linea(bateria, 10, 290000) });

            var r = e.Repuestos.ObtenerPorId(bateria);
            Assert.Equal(11, r.Stock);
            Assert.Equal(290000, r.PrecioCompra);
            Assert.Equal(proveedor, r.ProveedorId);
            Assert.Equal(2900000, e.Proveedores.ObtenerPorId(proveedor).TotalComprado);
        }

        [Fact]
        public void Movimientos_RegistranCadaCambioConElStockQueQuedo()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 20);
            int proveedor = e.Proveedores.Crear(new Proveedor { Nombre = "Baterías MAC" });

            int venta = e.Ventas.Vender(null, "Efectivo", new[] { Entorno.Linea(bateria, 1) });
            e.Entradas.Registrar(proveedor, null, new[] { Entorno.Linea(bateria, 5, 0) });
            e.Repuestos.AjustarStock(bateria, 22, "Conteo de fin de mes");
            e.Ventas.Anular(venta);

            var movimientos = e.Repuestos.Movimientos(bateria).Reverse().ToList();
            Assert.Equal(new[] { "Alta", "Venta", "Entrada", "Ajuste", "Anulación" }, movimientos.Select(m => m.Tipo));
            Assert.Equal(new[] { 20, -1, 5, -2, 1 }, movimientos.Select(m => m.Cantidad));
            Assert.Equal(new[] { 20, 19, 24, 22, 23 }, movimientos.Select(m => m.StockResultante));
            Assert.Equal(23, e.Repuestos.ObtenerPorId(bateria).Stock);
        }

        [Fact]
        public void Buscar_IgnoraTildesYMayusculas()
        {
            using var e = new Entorno();
            e.NuevoRepuesto("Batería 12V 45Ah");
            e.NuevoRepuesto("Pastillas de freno", precio: 90000, costo: 60000);

            var encontrados = e.Repuestos.Buscar("BATERIA").ToList();

            Assert.Single(encontrados);
            Assert.Equal("Batería 12V 45Ah", encontrados[0].Nombre);
        }

        [Fact]
        public void SoloPocoStock_MuestraLoQueHayQuePedir()
        {
            using var e = new Entorno();
            e.NuevoRepuesto("Batería", stock: 20);
            e.NuevoRepuesto("Bombillo H4", stock: 3, precio: 15000, costo: 8000);

            var porPedir = e.Repuestos.Buscar(soloPocoStock: true).ToList();

            Assert.Equal("Bombillo H4", Assert.Single(porPedir).Nombre);
        }

        [Fact]
        public void Eliminar_RepuestoVendido_NoSePermite()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto();
            e.Ventas.Vender(null, "Efectivo", new[] { Entorno.Linea(bateria, 1) });

            Assert.Throws<InvalidOperationException>(() => e.Repuestos.Eliminar(bateria));
        }

        [Fact]
        public void Codigo_Repetido_NoSePermite()
        {
            using var e = new Entorno();
            e.Repuestos.Crear(new Repuesto { Codigo = "bt-45", Nombre = "Batería", Categoria = "Baterías", PrecioVenta = 1 });

            var error = Assert.Throws<InvalidOperationException>(() =>
                e.Repuestos.Crear(new Repuesto { Codigo = "BT-45 ", Nombre = "Otra", Categoria = "Baterías", PrecioVenta = 1 }));
            Assert.Contains("BT-45", error.Message);
        }

        [Fact]
        public void Foto_SeGuardaYSeReemplazaSinDejarArchivosSobrantes()
        {
            using var e = new Entorno();
            byte[] imagen = { 1, 2, 3 };
            int id = e.Repuestos.Crear(new Repuesto { Nombre = "Llanta", Categoria = "Llantas", PrecioVenta = 200000 }, imagen);
            string carpetaFotos = Path.Combine(e.Carpeta, "fotos");
            string primera = e.Repuestos.ObtenerPorId(id).Foto;
            Assert.True(File.Exists(Path.Combine(carpetaFotos, primera)));

            e.Repuestos.Actualizar(e.Repuestos.ObtenerPorId(id), new byte[] { 4, 5 });

            string segunda = e.Repuestos.ObtenerPorId(id).Foto;
            Assert.NotEqual(primera, segunda);
            Assert.Equal(new[] { segunda }, Directory.GetFiles(carpetaFotos).Select(Path.GetFileName));
        }

        [Fact]
        public void Actualizar_NoCambiaElStock()
        {
            using var e = new Entorno();
            int id = e.NuevoRepuesto(stock: 20);
            var r = e.Repuestos.ObtenerPorId(id);
            r.Stock = 999;
            r.PrecioVenta = 360000;

            e.Repuestos.Actualizar(r);

            var guardado = e.Repuestos.ObtenerPorId(id);
            Assert.Equal(20, guardado.Stock);
            Assert.Equal(360000, guardado.PrecioVenta);
        }
    }
}
