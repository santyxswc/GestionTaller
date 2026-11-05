/**
 * @file Cliente.cs
 * @brief Modelo de cliente.
 * @author Santiago Caicedo
 */
using CommunityToolkit.Mvvm.ComponentModel;

namespace TallerMoreno.Core.Models
{
    /**
     * @brief Cliente del taller (cliente) con el resumen de lo que ha comprado.
     */
    public partial class Cliente : ObservableObject
    {
        /** Identificador. */
        public int Id { get; set; }

        /** Nombre completo o razón social. */
        [ObservableProperty]
        private string nombre;

        /** Cédula o NIT. */
        [ObservableProperty]
        private string documento;

        /** Teléfono o celular. */
        [ObservableProperty]
        private string telefono;

        /** Placa del vehículo. */
        [ObservableProperty]
        private string placa;

        /** Vehículo (marca, línea, modelo). */
        [ObservableProperty]
        private string vehiculo;

        /** Notas. */
        [ObservableProperty]
        private string notas;

        /** Placa y vehículo juntos, como "ABC123 · Chevrolet Spark". */
        public string VehiculoTexto => string.Join(" · ", new[] { Placa, Vehiculo }.Where(s => !string.IsNullOrWhiteSpace(s)));

        /** Último kilometraje conocido del vehículo, o null si nunca se anotó. */
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(KilometrajeTexto))]
        private int? kilometraje;

        /** Cuándo se anotó el kilometraje (solo lectura). */
        public DateTime? KilometrajeFecha { get; set; }
        /** Kilometraje como texto, como "85.000 km", o vacío. */
        public string KilometrajeTexto => Formato.Kilometraje(Kilometraje);
        /** Número de devoluciones o garantías (solo lectura). */
        public int Devoluciones { get; set; }
        /** Devoluciones como texto, o vacío si no tiene. */
        public string DevolucionesTexto => Devoluciones == 0 ? "" : Devoluciones.ToString();

        /** Total comprado en ventas no anuladas (solo lectura). */
        public decimal TotalComprado { get; set; }
        /** Número de compras no anuladas (solo lectura). */
        public int NumeroCompras { get; set; }
        /** Fecha de la última compra (solo lectura). */
        public DateTime? UltimaCompra { get; set; }
        /** Total abonado en compras no anuladas (solo lectura). */
        public decimal TotalPagado { get; set; }
        /** Lo que debe: lo comprado menos lo abonado. */
        public decimal Deuda => TotalComprado - TotalPagado;
        /** Indica si debe algo. */
        public bool TieneDeuda => Deuda > 0;
        /** Lo que debe como texto, "Al día" o el saldo a favor si abonó de más y luego se anuló una venta. */
        public string DeudaTexto => Deuda > 0 ? Formato.Moneda(Deuda) : Deuda < 0 ? $"A favor {Formato.Moneda(-Deuda)}" : "Al día";

        /**
         * @brief Copia el cliente, para editarlo sin tocar el de la lista.
         * @return Copia con los datos editables y sin los suscriptores de cambios
         */
        public Cliente Copiar() => new()
        {
            Id = Id,
            Nombre = Nombre,
            Documento = Documento,
            Telefono = Telefono,
            Placa = Placa,
            Vehiculo = Vehiculo,
            Notas = Notas,
            Kilometraje = Kilometraje,
            KilometrajeFecha = KilometrajeFecha
        };
    }
}
