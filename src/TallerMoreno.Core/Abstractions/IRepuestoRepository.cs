/**
 * @file IRepuestoRepository.cs
 * @brief Contrato del repositorio de repuestos.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Models;

namespace TallerMoreno.Core.Abstractions
{
    /**
     * @brief Acceso a datos del inventario.
     */
    public interface IRepuestoRepository
    {
        /**
         * @brief Busca repuestos por nombre, código o marca y por categoría.
         * @param texto Texto a buscar; vacío no filtra
         * @param categoria Categoría; vacía no filtra
         * @return Repuestos ordenados por nombre
         */
        IEnumerable<Repuesto> Buscar(string texto, string categoria);

        /**
         * @brief Busca un repuesto.
         * @param id Identificador
         * @return Repuesto, o null si no existe
         */
        Repuesto GetById(int id);

        /**
         * @brief Indica si otro repuesto ya tiene ese código.
         * @param codigo Código a comprobar
         * @param exceptoId Repuesto que se está editando (0 al crear)
         * @return true si el código está en uso
         */
        bool ExisteCodigo(string codigo, int exceptoId);

        /**
         * @brief Registra un repuesto y, si tiene unidades, su movimiento de alta.
         * @param r Datos del repuesto
         * @param usuarioId Usuario que lo registra
         * @return Identificador asignado
         */
        int Insert(Repuesto r, int usuarioId);

        /**
         * @brief Actualiza los datos de un repuesto sin tocar su stock.
         * @param r Repuesto con los datos nuevos
         * @return true si se actualizó
         */
        bool Update(Repuesto r);

        /**
         * @brief Cambia el stock a un conteo nuevo y registra el ajuste.
         * @param id Repuesto
         * @param nuevoStock Unidades contadas
         * @param motivo Motivo del ajuste
         * @param usuarioId Usuario que ajusta
         * @return Diferencia entre el conteo nuevo y el anterior
         */
        int AjustarStock(int id, int nuevoStock, string motivo, int usuarioId);

        /**
         * @brief Cuenta las ventas y entradas en las que aparece el repuesto.
         * @param id Repuesto
         * @return Número de líneas
         */
        int ContarUsos(int id);

        /**
         * @brief Elimina el repuesto y sus movimientos.
         * @param id Repuesto
         * @return true si se eliminó
         */
        bool Delete(int id);
    }
}
