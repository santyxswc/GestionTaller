/**
 * @file FotoReferenciaTests.cs
 * @brief Pruebas de la foto de referencia de los repuestos sin foto.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core;

namespace TallerMoreno.Tests
{
    /**
     * @brief El nombre manda sobre la categoría; la categoría se usa si el nombre no dice qué es.
     */
    public class FotoReferenciaTests
    {
        [Theory]
        [InlineData("Bateria", "Eléctricos", "baterias")]
        [InlineData("BATERÍA 12V 45Ah", "Otros", "baterias")]
        [InlineData("Filtro de aceite", "Aceites y lubricantes", "filtros")]
        [InlineData("Aceite de motor 20W50", "Motor", "aceites")]
        [InlineData("Líquido de frenos DOT 4", "Aceites y lubricantes", "frenos")]
        [InlineData("Pastillas delanteras", "Otros", "frenos")]
        [InlineData("Amortiguador trasero", "Otros", "suspension")]
        [InlineData("Kit de distribución", "Motor", "correas")]
        [InlineData("Bombillo H4", "Eléctricos", "luces")]
        [InlineData("Alternador", "Otros", "electricos")]
        [InlineData("Bujía NGK", "Eléctricos", "motor")]
        [InlineData("Llanta 175/70 R13", "Accesorios", "llantas")]
        [InlineData("Tapete de caucho", "Otros", "accesorios")]
        [InlineData("Repuesto (Batería)", "Otros", "baterias")]
        [InlineData("Batería-12V", "Otros", "baterias")]
        [InlineData("Kit/Pastillas", "Otros", "frenos")]
        public void ElNombreDecideLaFoto(string nombre, string categoria, string esperada) =>
            Assert.Equal(esperada, FotoReferencia.Elegir(nombre, categoria));

        [Theory]
        [InlineData("Repuesto XYZ-200", "Frenos", "frenos")]
        [InlineData("Cosa rara", "Categoría inventada", "otros")]
        [InlineData(null, "Llantas", "llantas")]
        [InlineData("Parte 123", null, "otros")]
        public void SinPalabrasConocidas_UsaLaCategoria(string nombre, string categoria, string esperada) =>
            Assert.Equal(esperada, FotoReferencia.Elegir(nombre, categoria));

        [Fact]
        public void CadaFotoPosibleExisteEnElPrograma()
        {
            string carpeta = CarpetaFotos();
            foreach (string foto in FotoReferencia.Todas)
                Assert.True(File.Exists(Path.Combine(carpeta, foto + ".jpg")), $"Falta Assets/Categorias/{foto}.jpg");
        }

        [Fact]
        public void CadaCategoriaTieneSuPropiaFoto()
        {
            foreach (string categoria in Core.Models.Repuesto.Categorias.Where(c => c != "Otros"))
                Assert.NotEqual(FotoReferencia.Otros, FotoReferencia.Elegir("Parte 123", categoria));
        }

        /**
         * @brief Busca la carpeta Assets/Categorias del programa subiendo desde la carpeta de las pruebas.
         * @return Ruta de la carpeta
         */
        private static string CarpetaFotos()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                string ruta = Path.Combine(dir.FullName, "src", "TallerMoreno.Desktop", "Assets", "Categorias");
                if (Directory.Exists(ruta))
                    return ruta;
            }
            throw new DirectoryNotFoundException("No se encontró src/TallerMoreno.Desktop/Assets/Categorias");
        }

        [Fact]
        public void SoloCoincideAlComienzoDeUnaPalabra()
        {
            // "Sprint" contiene "rin" pero no es un rin: no debe salir la llanta.
            Assert.Equal("otros", FotoReferencia.Elegir("Emblema Sprint", "Otros"));
        }
    }
}
