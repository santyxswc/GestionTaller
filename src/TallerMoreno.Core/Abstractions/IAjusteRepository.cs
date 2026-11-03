/**
 * @file IAjusteRepository.cs
 * @brief Contrato de los ajustes generales del programa.
 * @author Santiago Caicedo
 */
namespace TallerMoreno.Core.Abstractions
{
    /**
     * @brief Guarda valores sueltos del programa por su nombre, como el código de recuperación.
     */
    public interface IAjusteRepository
    {
        /**
         * @brief Lee un ajuste.
         * @param clave Nombre del ajuste
         * @return Valor, o null si no existe
         */
        string Obtener(string clave);

        /**
         * @brief Crea o reemplaza un ajuste.
         * @param clave Nombre del ajuste
         * @param valor Valor
         */
        void Guardar(string clave, string valor);
    }
}
