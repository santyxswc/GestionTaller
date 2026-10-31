/**
 * @file IEntradaRepository.cs
 * @brief Contrato del repositorio de entradas de mercancía.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Models;

namespace TallerMoreno.Core.Abstractions
{
    /**
     * @brief Registro y consulta de la mercancía que llega de los proveedores.
     */
    public interface IEntradaRepository
    {
        /**
         * @brief Registra una entrada, suma el stock y actualiza el costo de cada repuesto en una transacción.
         * @param proveedorId Proveedor
         * @param usuarioId Usuario que recibe
         * @param facturaProveedor Número de la factura del proveedor
         * @param lineas Repuestos, cantidades y costos (sin repetidos)
         * @return Número de la entrada
         * @exception InvalidOperationException Si un repuesto no existe; no se guarda nada
         */
        int Insert(int proveedorId, int usuarioId, string facturaProveedor, IEnumerable<LineaDocumento> lineas);

        /**
         * @brief Busca una entrada sin sus líneas.
         * @param id Número de la entrada
         * @return Entrada, o null si no existe
         */
        Entrada GetById(int id);

        /**
         * @brief Obtiene las líneas de una entrada.
         * @param id Número de la entrada
         * @return Lineas con el nombre de cada repuesto
         */
        List<LineaDocumento> GetItems(int id);

        /**
         * @brief Busca entradas, de la más reciente a la más antigua.
         * @param desde Fecha inicial (incluida), o null
         * @param hasta Fecha final (excluida), o null
         * @param proveedorId Solo las de este proveedor, o null
         * @return Entradas que coinciden
         */
        IEnumerable<Entrada> Buscar(DateTime? desde, DateTime? hasta, int? proveedorId);
    }
}
