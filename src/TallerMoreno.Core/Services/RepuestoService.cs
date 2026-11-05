/**
 * @file RepuestoService.cs
 * @brief Reglas de negocio del inventario de repuestos.
 * @author Santiago Caicedo
 */
using TallerMoreno.Core.Abstractions;
using TallerMoreno.Core.Models;
using TallerMoreno.Core.Sesion;

namespace TallerMoreno.Core.Services
{
    /**
     * @brief Consulta del inventario, altas, cambios, fotos y ajustes de stock.
     *
     * El stock de un repuesto solo cambia por ventas, entradas de mercancía, anulaciones
     * y ajustes; cada cambio queda registrado como movimiento del inventario.
     */
    public class RepuestoService
    {
        /** Acceso a datos del inventario. */
        private readonly IRepuestoRepository _repo;
        /** Historial de movimientos. */
        private readonly IMovimientoRepository _movimientos;
        /** Archivos de las fotos. */
        private readonly IAlmacenFotos _fotos;
        /** Usuario de la sesión. */
        private readonly ISesionUsuario _sesion;

        /**
         * @brief Crea el servicio con sus dependencias.
         * @param repo Acceso a datos del inventario
         * @param movimientos Historial de movimientos
         * @param fotos Archivos de las fotos
         * @param sesion Usuario de la sesión (permisos)
         */
        public RepuestoService(IRepuestoRepository repo, IMovimientoRepository movimientos, IAlmacenFotos fotos, ISesionUsuario sesion)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _movimientos = movimientos ?? throw new ArgumentNullException(nameof(movimientos));
            _fotos = fotos ?? throw new ArgumentNullException(nameof(fotos));
            _sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
        }

        /**
         * @brief Busca en el inventario.
         * @param texto Nombre, código o marca; vacío no filtra
         * @param categoria Categoría; vacía o null no filtra
         * @param soloPocoStock true para ver solo lo que hay que pedir
         * @return Repuestos que coinciden
         */
        public IEnumerable<Repuesto> Buscar(string texto = null, string categoria = null, bool soloPocoStock = false)
        {
            var repuestos = _repo.Buscar(texto?.Trim(), categoria);
            return soloPocoStock ? repuestos.Where(r => r.StockBajo) : repuestos;
        }

        /**
         * @brief Busca un repuesto.
         * @param id Identificador
         * @return Repuesto, o null
         */
        public Repuesto ObtenerPorId(int id) => _repo.GetById(id);

        /**
         * @brief Historial de cambios de stock de un repuesto.
         * @param id Repuesto
         * @return Movimientos, del más reciente al más antiguo
         */
        public IEnumerable<MovimientoInventario> Movimientos(int id) => _movimientos.Buscar(null, null, null, id);

        /**
         * @brief Valida y registra un repuesto con su foto. Solo administrador.
         * @param r Datos del repuesto; su Stock es el conteo inicial
         * @param foto Imagen ya reducida, o null
         * @param extensionFoto Extensión de la imagen, como ".png"
         * @return Identificador asignado
         */
        public int Crear(Repuesto r, byte[] foto = null, string extensionFoto = ".png")
        {
            _sesion.ExigirAdmin("agregar repuestos");
            Validar(r);
            if (r.Stock < 0)
                throw new ArgumentException("Las unidades no pueden ser negativas.");

            if (foto != null)
                r.Foto = _fotos.Guardar(foto, extensionFoto);
            try
            {
                r.Id = _repo.Insert(r, _sesion.UsuarioId);
                return r.Id;
            }
            catch
            {
                if (foto != null) _fotos.Eliminar(r.Foto);
                throw;
            }
        }

        /**
         * @brief Valida y actualiza los datos de un repuesto (no su stock). Solo administrador.
         * @param r Repuesto con los datos nuevos
         * @param fotoNueva Imagen nueva ya reducida, o null para no cambiarla
         * @param quitarFoto true para borrar la foto actual
         * @param extensionFoto Extensión de la imagen nueva
         * @return true si se actualizó
         */
        public bool Actualizar(Repuesto r, byte[] fotoNueva = null, bool quitarFoto = false, string extensionFoto = ".png")
        {
            _sesion.ExigirAdmin("modificar repuestos");
            if (r == null || r.Id <= 0)
                throw new ArgumentException("Repuesto inválido.");
            Validar(r);

            var anterior = _repo.GetById(r.Id) ?? throw new InvalidOperationException("El repuesto ya no existe.");
            string fotoAnterior = anterior.Foto;
            if (fotoNueva != null)
                r.Foto = _fotos.Guardar(fotoNueva, extensionFoto);
            else if (quitarFoto)
                r.Foto = null;
            else
                r.Foto = fotoAnterior;

            bool ok;
            try
            {
                ok = _repo.Update(r);
            }
            catch
            {
                if (fotoNueva != null) _fotos.Eliminar(r.Foto);
                throw;
            }
            if (ok && fotoAnterior != r.Foto)
                _fotos.Eliminar(fotoAnterior);
            return ok;
        }

        /**
         * @brief Corrige el stock con un conteo físico. Solo administrador.
         * @param id Repuesto
         * @param nuevoStock Unidades contadas
         * @param motivo Motivo del ajuste (por ejemplo, "conteo de fin de mes")
         * @return Diferencia con el stock anterior
         */
        public int AjustarStock(int id, int nuevoStock, string motivo)
        {
            _sesion.ExigirAdmin("ajustar el inventario");
            if (nuevoStock < 0)
                throw new ArgumentException("Las unidades no pueden ser negativas.");
            if (string.IsNullOrWhiteSpace(motivo))
                throw new ArgumentException("Escribe el motivo del ajuste.");
            return _repo.AjustarStock(id, nuevoStock, motivo.Trim(), _sesion.UsuarioId);
        }

        /**
         * @brief Elimina un repuesto que nunca se ha vendido ni comprado. Solo administrador.
         * @param id Repuesto
         * @return true si se eliminó
         * @exception InvalidOperationException Si aparece en ventas o entradas
         */
        public bool Eliminar(int id)
        {
            _sesion.ExigirAdmin("eliminar repuestos");
            int usos = _repo.ContarUsos(id);
            if (usos > 0)
                throw new InvalidOperationException($"No se puede eliminar porque aparece en {usos} venta(s), entrada(s) o devolución(es). Puedes dejarlo con 0 unidades.");

            var repuesto = _repo.GetById(id);
            bool ok = _repo.Delete(id);
            if (ok)
                _fotos.Eliminar(repuesto?.Foto);
            return ok;
        }

        /**
         * @brief Reglas comunes al crear y al modificar un repuesto.
         * @param r Repuesto a validar
         */
        private void Validar(Repuesto r)
        {
            ArgumentNullException.ThrowIfNull(r);
            if (string.IsNullOrWhiteSpace(r.Nombre))
                throw new ArgumentException("Escribe el nombre del repuesto.");
            if (string.IsNullOrWhiteSpace(r.Categoria))
                throw new ArgumentException("Selecciona la categoría.");
            if (r.PrecioVenta <= 0)
                throw new ArgumentException("El precio de venta debe ser mayor que cero.");
            if (r.PrecioCompra < 0)
                throw new ArgumentException("El precio de compra no puede ser negativo.");
            if (r.StockMinimo < 0)
                throw new ArgumentException("El stock mínimo no puede ser negativo.");

            r.Nombre = r.Nombre.Trim();
            r.Codigo = string.IsNullOrWhiteSpace(r.Codigo) ? null : r.Codigo.Trim().ToUpperInvariant();
            if (r.Codigo != null && _repo.ExisteCodigo(r.Codigo, r.Id))
                throw new InvalidOperationException($"Ya hay otro repuesto con el código {r.Codigo}.");
        }
    }
}
