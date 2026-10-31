/**
 * @file ReporteService.cs
 * @brief Reportes de ventas y movimientos del inventario.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Abstractions;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Sesion;

namespace TallerMoreno.Core.Services
{
    /**
     * @brief Totales, rankings y movimientos del inventario de un periodo.
     */
    public class ReporteService
    {
        /** Consultas de reportes. */
        private readonly IReporteRepository _repo;
        /** Historial de movimientos. */
        private readonly IMovimientoRepository _movimientos;
        /** Usuario de la sesión. */
        private readonly ISesionUsuario _sesion;

        /**
         * @brief Crea el servicio con sus dependencias.
         * @param repo Consultas de reportes
         * @param movimientos Historial de movimientos
         * @param sesion Usuario de la sesión (permisos)
         */
        public ReporteService(IReporteRepository repo, IMovimientoRepository movimientos, ISesionUsuario sesion)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _movimientos = movimientos ?? throw new ArgumentNullException(nameof(movimientos));
            _sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
        }

        /**
         * @brief Lo vendido hoy, para la pantalla de inicio. Lo ve cualquier usuario.
         * @return Totales de hoy
         */
        public ResumenVentas VentasDeHoy()
        {
            var (desde, hasta) = Periodo.Rango(Periodo.Hoy, DateTime.Now);
            return _repo.Resumen(desde, hasta);
        }

        /**
         * @brief Totales de un periodo. Solo administrador.
         * @param periodo Uno de Periodo.Todos
         * @return Total, ventas, unidades y ganancia
         */
        public ResumenVentas Resumen(string periodo)
        {
            _sesion.ExigirAdmin("ver los reportes");
            var (desde, hasta) = Periodo.Rango(periodo, DateTime.Now);
            return _repo.Resumen(desde, hasta);
        }

        /**
         * @brief Los 10 repuestos más vendidos. Solo administrador.
         * @param periodo Uno de Periodo.Todos
         * @return Repuesto, unidades y total
         */
        public IEnumerable<ItemRanking> TopRepuestos(string periodo)
        {
            _sesion.ExigirAdmin("ver los reportes");
            var (desde, hasta) = Periodo.Rango(periodo, DateTime.Now);
            return _repo.TopRepuestos(desde, hasta, 10);
        }

        /**
         * @brief Los 10 clientes que más compraron. Solo administrador.
         * @param periodo Uno de Periodo.Todos
         * @return Cliente, compras y total
         */
        public IEnumerable<ItemRanking> TopClientes(string periodo)
        {
            _sesion.ExigirAdmin("ver los reportes");
            var (desde, hasta) = Periodo.Rango(periodo, DateTime.Now);
            return _repo.TopClientes(desde, hasta, 10);
        }

        /**
         * @brief Ventas de cada usuario. Solo administrador.
         * @param periodo Uno de Periodo.Todos
         * @return Usuario, ventas y total
         */
        public IEnumerable<ItemRanking> VentasPorUsuario(string periodo)
        {
            _sesion.ExigirAdmin("ver los reportes");
            var (desde, hasta) = Periodo.Rango(periodo, DateTime.Now);
            return _repo.VentasPorUsuario(desde, hasta);
        }

        /**
         * @brief Movimientos del inventario. Solo administrador.
         * @param periodo Uno de Periodo.Todos
         * @param texto Repuesto, tipo o referencia; vacío no filtra
         * @return Movimientos, del más reciente al más antiguo
         */
        public IEnumerable<MovimientoInventario> Movimientos(string periodo, string texto = null)
        {
            _sesion.ExigirAdmin("ver los movimientos del inventario");
            var (desde, hasta) = Periodo.Rango(periodo, DateTime.Now);
            return _movimientos.Buscar(desde, hasta, texto?.Trim(), null);
        }
    }
}
