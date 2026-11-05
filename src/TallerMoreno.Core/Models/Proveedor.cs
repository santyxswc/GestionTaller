/**
 * @file Proveedor.cs
 * @brief Modelo de proveedor.
 * @author Santiago Caicedo
 */
using CommunityToolkit.Mvvm.ComponentModel;

namespace TallerMoreno.Core.Models
{
    /**
     * @brief Almacén o distribuidor que le vende repuestos al taller (proveedor).
     */
    public partial class Proveedor : ObservableObject
    {
        /** Identificador. */
        public int Id { get; set; }

        /** Nombre o razón social. */
        [ObservableProperty]
        private string nombre;

        /** NIT. */
        [ObservableProperty]
        private string nit;

        /** Persona de contacto. */
        [ObservableProperty]
        private string contacto;

        /** Teléfono o celular. */
        [ObservableProperty]
        private string telefono;

        /** Correo electrónico. */
        [ObservableProperty]
        private string correo;

        /** Dirección. */
        [ObservableProperty]
        private string direccion;

        /** Notas. */
        [ObservableProperty]
        private string notas;

        /** Total comprado al proveedor (solo lectura). */
        public decimal TotalComprado { get; set; }
        /** Número de entradas de mercancía (solo lectura). */
        public int NumeroEntradas { get; set; }
        /** Fecha de la última entrada (solo lectura). */
        public DateTime? UltimaEntrada { get; set; }
        /** Número de devoluciones o garantías (solo lectura). */
        public int Devoluciones { get; set; }
        /** Garantías que el proveedor todavía no ha respondido (solo lectura). */
        public int Pendientes { get; set; }
        /** Devoluciones como texto, como "3 · 1 pendiente", o vacío. */
        public string DevolucionesTexto => Devoluciones == 0 ? "" : Pendientes > 0 ? $"{Devoluciones} · {Pendientes} pendiente{(Pendientes > 1 ? "s" : "")}" : Devoluciones.ToString();
        /** Indica si hay garantías sin respuesta. */
        public bool TienePendientes => Pendientes > 0;
        /** Total que se le ha pagado (solo lectura). */
        public decimal TotalPagado { get; set; }
        /** Lo que se le debe: lo comprado menos lo pagado. */
        public decimal Deuda => TotalComprado - TotalPagado;
        /** Indica si se le debe algo. */
        public bool TieneDeuda => Deuda > 0;
        /** Lo que se le debe como texto, o "Al día". */
        public string DeudaTexto => Deuda > 0 ? Formato.Moneda(Deuda) : "Al día";

        /**
         * @brief Copia el proveedor, para editarlo sin tocar el de la lista.
         * @return Copia con los datos editables y sin los suscriptores de cambios
         */
        public Proveedor Copiar() => new()
        {
            Id = Id,
            Nombre = Nombre,
            Nit = Nit,
            Contacto = Contacto,
            Telefono = Telefono,
            Correo = Correo,
            Direccion = Direccion,
            Notas = Notas
        };
    }
}
