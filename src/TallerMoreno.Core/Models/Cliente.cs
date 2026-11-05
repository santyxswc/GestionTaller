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
            Notas = Notas
        };
    }
}
