/**
 * @file IMovimientoRepository.cs
 * @brief Contrato del repositorio de movimientos del inventario.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Models;

namespace TallerMoreno.Core.Abstractions
{
    /**
     * @brief Consulta del historial de cambios de stock.
     */
    public interface IMovimientoRepository
    {
        /**
         * @brief Busca movimientos, del más reciente al más antiguo.
         * @param desde Fecha inicial (incluida), o null
         * @param hasta Fecha final (excluida), o null
         * @param texto Repuesto, tipo o referencia; vacío no filtra
         * @param repuestoId Solo los de este repuesto, o null
         * @return Movimientos que coinciden (máximo 1000)
         */
        IEnumerable<MovimientoInventario> Buscar(DateTime? desde, DateTime? hasta, string texto, int? repuestoId);
    }
}
