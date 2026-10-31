/**
 * @file IClienteRepository.cs
 * @brief Contrato del repositorio de clientes.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Models;

namespace TallerMoreno.Core.Abstractions
{
    /**
     * @brief Acceso a datos de clientes.
     */
    public interface IClienteRepository
    {
        /**
         * @brief Busca clientes por nombre, documento, teléfono o placa, con lo que ha comprado cada uno.
         * @param texto Texto a buscar; vacío devuelve todos
         * @return Clientes ordenados por nombre
         */
        IEnumerable<Cliente> Buscar(string texto);

        /**
         * @brief Busca un cliente con su resumen de compras.
         * @param id Identificador
         * @return Cliente, o null si no existe
         */
        Cliente GetById(int id);

        /**
         * @brief Clientes para la lista de una venta.
         * @return Identificador y nombre con la placa
         */
        IEnumerable<Seleccion> GetParaSeleccion();

        /**
         * @brief Registra un cliente.
         * @param c Datos del cliente
         * @return Identificador asignado
         */
        int Insert(Cliente c);

        /**
         * @brief Actualiza un cliente.
         * @param c Cliente con los datos nuevos
         * @return true si se actualizó
         */
        bool Update(Cliente c);

        /**
         * @brief Cuenta las ventas del cliente, incluidas las anuladas.
         * @param id Cliente
         * @return Número de ventas
         */
        int ContarVentas(int id);

        /**
         * @brief Elimina un cliente.
         * @param id Cliente
         * @return true si se eliminó
         */
        bool Delete(int id);
    }
}
