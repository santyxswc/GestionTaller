/**
 * @file ProveedorService.cs
 * @brief Reglas de negocio de los proveedores.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Abstractions;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Sesion;

namespace TallerMoreno.Core.Services
{
    /**
     * @brief Registro de proveedores y consulta de lo que se les ha comprado.
     */
    public class ProveedorService
    {
        /** Acceso a datos de proveedores. */
        private readonly IProveedorRepository _repo;
        /** Acceso a datos de entradas (historial del proveedor). */
        private readonly IEntradaRepository _entradas;
        /** Usuario de la sesión. */
        private readonly ISesionUsuario _sesion;

        /**
         * @brief Crea el servicio con sus dependencias.
         * @param repo Acceso a datos de proveedores
         * @param entradas Acceso a datos de entradas
         * @param sesion Usuario de la sesión (permisos)
         */
        public ProveedorService(IProveedorRepository repo, IEntradaRepository entradas, ISesionUsuario sesion)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _entradas = entradas ?? throw new ArgumentNullException(nameof(entradas));
            _sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
        }

        /**
         * @brief Busca proveedores con lo que se le ha comprado a cada uno.
         * @param texto Nombre, NIT, contacto o teléfono; vacío devuelve todos
         * @return Proveedores
         */
        public IEnumerable<Proveedor> Buscar(string texto = null) => _repo.Buscar(texto?.Trim());

        /**
         * @brief Busca un proveedor.
         * @param id Identificador
         * @return Proveedor, o null
         */
        public Proveedor ObtenerPorId(int id) => _repo.GetById(id);

        /**
         * @brief Proveedores para las listas desplegables.
         * @return Identificador y nombre
         */
        public IEnumerable<Seleccion> ParaSeleccion() => _repo.GetParaSeleccion();

        /**
         * @brief Entradas de mercancía de un proveedor.
         * @param id Proveedor
         * @return Entradas, de la más reciente a la más antigua
         */
        public IEnumerable<Entrada> Entradas(int id) => _entradas.Buscar(null, null, id);

        /**
         * @brief Valida y registra un proveedor. Lo puede hacer cualquier usuario, por ejemplo al recibir mercancía.
         * @param p Datos del proveedor
         * @return Identificador asignado
         */
        public int Crear(Proveedor p)
        {
            _sesion.ExigirSesion();
            Validar(p);
            p.Id = _repo.Insert(p);
            return p.Id;
        }

        /**
         * @brief Valida y actualiza un proveedor.
         * @param p Proveedor con los datos nuevos
         * @return true si se actualizó
         */
        public bool Actualizar(Proveedor p)
        {
            _sesion.ExigirSesion();
            if (p == null || p.Id <= 0)
                throw new ArgumentException("Proveedor inválido.");
            Validar(p);
            return _repo.Update(p);
        }

        /**
         * @brief Elimina un proveedor sin entradas de mercancía. Solo administrador.
         * @param id Proveedor
         * @return true si se eliminó
         * @exception InvalidOperationException Si tiene entradas
         */
        public bool Eliminar(int id)
        {
            _sesion.ExigirAdmin("eliminar proveedores");
            int entradas = _repo.ContarEntradas(id);
            if (entradas > 0)
                throw new InvalidOperationException($"No se puede eliminar porque tiene {entradas} entrada(s) de mercancía.");
            return _repo.Delete(id);
        }

        /**
         * @brief Valida y limpia los datos de un proveedor.
         * @param p Proveedor
         */
        private static void Validar(Proveedor p)
        {
            ArgumentNullException.ThrowIfNull(p);
            if (string.IsNullOrWhiteSpace(p.Nombre))
                throw new ArgumentException("Escribe el nombre del proveedor.");
            p.Nombre = p.Nombre.Trim();
            if (!string.IsNullOrWhiteSpace(p.Correo) && !p.Correo.Contains('@'))
                throw new ArgumentException("El correo no es válido.");
        }
    }
}
