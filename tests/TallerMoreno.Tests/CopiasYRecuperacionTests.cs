/**
 * @file CopiasYRecuperacionTests.cs
 * @brief Pruebas de copias de seguridad, restauración, código de recuperación y migración.
 * @author Santiago Caicedo
 */
using System.IO.Compression;
using Microsoft.Data.Sqlite;
using TallerMoreno.Core.Models;
using TallerMoreno.Infrastructure.Database;

namespace TallerMoreno.Tests
{
    /**
     * @brief Lo que protege los datos del taller: copias, restaurar y no quedar por fuera del programa.
     */
    public class CopiasYRecuperacionTests
    {
        [Fact]
        public void Restaurar_DevuelveLosDatosComoEstabanEnLaCopia()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 20);
            string zip = Path.Combine(e.Carpeta, "copia.zip");
            e.Exportacion.CrearCopiaSeguridad(zip);

            e.Ventas.Vender(null, "Efectivo", new[] { Entorno.Linea(bateria, 5) });
            e.NuevoRepuesto("Filtro de aceite");
            Assert.Equal(15, e.Repuestos.ObtenerPorId(bateria).Stock);

            e.Exportacion.RestaurarCopiaSeguridad(zip);

            Assert.Equal(20, e.Repuestos.ObtenerPorId(bateria).Stock);
            Assert.Single(e.Repuestos.Buscar(null, null));
            Assert.Empty(e.Ventas.Buscar(Periodo.Todo));
        }

        [Fact]
        public void Restaurar_ConUnArchivoQueNoEsCopia_NoTocaLosDatos()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 20);
            string zip = Path.Combine(e.Carpeta, "otro.zip");
            using (var archivo = ZipFile.Open(zip, ZipArchiveMode.Create))
            using (var escritor = new StreamWriter(archivo.CreateEntry("taller.db").Open()))
                escritor.Write("esto no es una base de datos");

            var error = Assert.Throws<InvalidOperationException>(() => e.Exportacion.RestaurarCopiaSeguridad(zip));

            Assert.Contains("no es una copia", error.Message);
            Assert.Equal(20, e.Repuestos.ObtenerPorId(bateria).Stock);
        }

        [Fact]
        public void Restaurar_ComoMecanico_NoSePermite()
        {
            using var e = new Entorno();
            string zip = Path.Combine(e.Carpeta, "copia.zip");
            e.Exportacion.CrearCopiaSeguridad(zip);
            e.Usuarios.CrearUsuario("Juan", "juan", "1234", Usuario.RolMecanico);
            e.Usuarios.CerrarSesion();
            e.Usuarios.IniciarSesion("juan", "1234");

            Assert.Throws<UnauthorizedAccessException>(() => e.Exportacion.RestaurarCopiaSeguridad(zip));
        }

        [Fact]
        public void CopiaDiaria_SeHaceUnaVezPorDiaYGuardaSoloLasRecientes()
        {
            using var e = new Entorno();
            var dia = new DateTime(2026, 1, 1);

            Assert.NotNull(e.Copias.HacerSiFalta(dia));
            Assert.Null(e.Copias.HacerSiFalta(dia));
            for (int i = 1; i <= 20; i++)
                e.Copias.HacerSiFalta(dia.AddDays(i));

            var copias = Directory.GetFiles(e.Copias.Carpeta, "diaria-*.zip").Select(Path.GetFileName).OrderBy(n => n).ToList();
            Assert.Equal(14, copias.Count);
            Assert.Equal("diaria-2026-01-21.zip", copias[^1]);
            Assert.Empty(Directory.GetFiles(e.Copias.Carpeta, "*.tmp"));
        }

        [Fact]
        public void CodigoRecuperacion_PermiteEntrarYSoloSirveUnaVez()
        {
            using var e = new Entorno();
            string codigo = e.Usuarios.GenerarCodigoRecuperacion();
            e.Usuarios.CerrarSesion();

            string nuevo = e.Usuarios.RecuperarClave("Moreno", codigo.ToLowerInvariant().Replace("-", " "), "nueva123");

            Assert.NotEqual(codigo, nuevo);
            Assert.NotNull(e.Usuarios.IniciarSesion("moreno", "nueva123"));
            Assert.Throws<InvalidOperationException>(() => e.Usuarios.RecuperarClave("moreno", codigo, "otra1234"));
        }

        [Fact]
        public void CodigoRecuperacion_NoSirveParaUnMecanicoNiConCodigoErrado()
        {
            using var e = new Entorno();
            e.Usuarios.CrearUsuario("Juan", "juan", "1234", Usuario.RolMecanico);
            string codigo = e.Usuarios.GenerarCodigoRecuperacion();
            e.Usuarios.CerrarSesion();

            Assert.Throws<InvalidOperationException>(() => e.Usuarios.RecuperarClave("juan", codigo, "nueva123"));
            Assert.Throws<InvalidOperationException>(() => e.Usuarios.RecuperarClave("moreno", "AAAA-BBBB-CCCC", "nueva123"));
            Assert.NotNull(e.Usuarios.IniciarSesion("moreno", "clave123"));
        }

        [Fact]
        public void Migracion_DeVersion1_AgregaLosAjustesSinPerderDatos()
        {
            using var e = new Entorno();
            int bateria = e.NuevoRepuesto(stock: 7);
            using (var db = new SqliteConnection($"Data Source={e.RutaBaseDatos};Pooling=False"))
            {
                db.Open();
                using var cmd = db.CreateCommand();
                cmd.CommandText = "DROP TABLE ajuste; PRAGMA user_version = 1;";
                cmd.ExecuteNonQuery();
            }
            Assert.Equal(1, e.Inicializador.VersionGuardada());

            e.Inicializador.Inicializar();

            Assert.Equal(InicializadorBaseDatos.VersionEsquema, e.Inicializador.VersionGuardada());
            Assert.False(e.Usuarios.TieneCodigoRecuperacion());
            e.Usuarios.GenerarCodigoRecuperacion();
            Assert.True(e.Usuarios.TieneCodigoRecuperacion());
            Assert.Equal(7, e.Repuestos.ObtenerPorId(bateria).Stock);
        }

        [Fact]
        public void BaseDeDatosDeUnaVersionMasNueva_NoSeAbre()
        {
            using var e = new Entorno();
            using (var db = new SqliteConnection($"Data Source={e.RutaBaseDatos};Pooling=False"))
            {
                db.Open();
                using var cmd = db.CreateCommand();
                cmd.CommandText = "PRAGMA user_version = 99;";
                cmd.ExecuteNonQuery();
            }

            var error = Assert.Throws<InvalidOperationException>(() => e.Inicializador.Inicializar());
            Assert.Contains("versión más nueva", error.Message);
        }
    }
}
