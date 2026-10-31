/**
 * @file IProveedorRepository.cs
 * @brief Contrato del repositorio de proveedores.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Models;

namespace TallerMoreno.Core.Abstractions
{
    /**
     * @brief Acceso a datos de proveedores.
     */
    public interface IProveedorRepository
    {
        /**
         * @brief Busca proveedores por nombre, NIT, contacto o teléfono, con lo que se les ha comprado.
         * @param texto Texto a buscar; vacío devuelve todos
         * @return Proveedores ordenados por nombre
         */
        IEnumerable<Proveedor> Buscar(string texto);

        /**
         * @brief Busca un proveedor con su resumen de compras.
         * @param id Identificador
         * @return Proveedor, o null si no existe
         */
        Proveedor GetById(int id);

        /**
         * @brief Proveedores para las listas desplegables.
         * @return Identificador y nombre
         */
        IEnumerable<Seleccion> GetParaSeleccion();

        /**
         * @brief Registra un proveedor.
         * @param p Datos del proveedor
         * @return Identificador asignado
         */
        int Insert(Proveedor p);

        /**
         * @brief Actualiza un proveedor.
         * @param p Proveedor con los datos nuevos
         * @return true si se actualizó
         */
        bool Update(Proveedor p);

        /**
         * @brief Cuenta las entradas de mercancía del proveedor.
         * @param id Proveedor
         * @return Número de entradas
         */
        int ContarEntradas(int id);

        /**
         * @brief Elimina un proveedor y lo quita como proveedor habitual de los repuestos.
         * @param id Proveedor
         * @return true si se eliminó
         */
        bool Delete(int id);
    }
}
