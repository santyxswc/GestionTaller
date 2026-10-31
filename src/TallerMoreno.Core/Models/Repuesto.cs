/**
 * @file Repuesto.cs
 * @brief Modelo de repuesto del inventario.
 * @author Santiago Caicedo
 */
using CommunityToolkit.Mvvm.ComponentModel;

namespace TallerMoreno.Core.Models
{
    /**
     * @brief Pieza o artículo del inventario (repuesto).
     *
     * Notifica sus cambios para validar el formulario mientras se escribe.
     */
    public partial class Repuesto : ObservableObject
    {
        /** Categorías con las que se agrupa el inventario. */
        public static readonly string[] Categorias =
        {
            "Baterías", "Llantas", "Aceites y lubricantes", "Filtros", "Frenos", "Suspensión",
            "Motor", "Eléctricos", "Luces", "Correas", "Refrigeración", "Accesorios", "Otros"
        };

        /** Identificador. */
        public int Id { get; set; }

        /** Código o referencia del fabricante (opcional, único). */
        [ObservableProperty]
        private string codigo;

        /** Nombre del repuesto. */
        [ObservableProperty]
        private string nombre;

        /** Categoría (una de Categorias). */
        [ObservableProperty]
        private string categoria;

        /** Marca. */
        [ObservableProperty]
        private string marca;

        /** Lo que le cuesta al taller cada unidad (último costo de compra). */
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ValorInventario), nameof(Ganancia))]
        private decimal precioCompra;

        /** Precio al que se vende cada unidad. */
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Ganancia))]
        private decimal precioVenta;

        /** Unidades disponibles. Solo cambia con ventas, entradas de mercancía y ajustes. */
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StockBajo), nameof(Agotado), nameof(EstadoStock), nameof(ValorInventario))]
        private int stock;

        /** Unidades desde las que el repuesto se marca con poco stock. */
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StockBajo), nameof(EstadoStock))]
        private int stockMinimo = 2;

        /** Dónde está guardado en el taller (estante, cajón...). */
        [ObservableProperty]
        private string ubicacion;

        /** Nombre del archivo de la foto en la carpeta de fotos, o null. */
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TieneFoto))]
        private string foto;

        /** Proveedor habitual, o null. */
        [ObservableProperty]
        private int? proveedorId;

        /** Nombre del proveedor habitual (solo lectura). */
        public string ProveedorNombre { get; set; }

        /** Notas o descripción. */
        [ObservableProperty]
        private string descripcion;

        /** Indica si no quedan unidades. */
        public bool Agotado => Stock <= 0;
        /** Indica si el stock está en el mínimo o por debajo. */
        public bool StockBajo => Stock <= StockMinimo;
        /** Agotado, Poco stock o Disponible. */
        public string EstadoStock => Agotado ? "Agotado" : StockBajo ? "Poco stock" : "Disponible";
        /** Indica si tiene foto. */
        public bool TieneFoto => !string.IsNullOrWhiteSpace(Foto);
        /** Costo de las unidades en bodega. */
        public decimal ValorInventario => Stock * PrecioCompra;
        /** Lo que se gana por unidad vendida. */
        public decimal Ganancia => PrecioVenta - PrecioCompra;

        /**
         * @brief Copia el repuesto, para editarlo sin tocar el de la lista.
         * @return Copia con los mismos datos y sin los suscriptores de cambios
         */
        public Repuesto Copiar() => new()
        {
            Id = Id,
            Codigo = Codigo,
            Nombre = Nombre,
            Categoria = Categoria,
            Marca = Marca,
            PrecioCompra = PrecioCompra,
            PrecioVenta = PrecioVenta,
            Stock = Stock,
            StockMinimo = StockMinimo,
            Ubicacion = Ubicacion,
            Foto = Foto,
            ProveedorId = ProveedorId,
            ProveedorNombre = ProveedorNombre,
            Descripcion = Descripcion
        };
    }
}
