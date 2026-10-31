/**
 * @file DocumentoViewModel.cs
 * @brief Detalle de una venta o de una entrada de mercancía.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using TallerMoreno.Core;
using TallerMoreno.Core.Models;

namespace TallerMoreno.Desktop.ViewModels
{
    /**
     * @brief Par etiqueta y valor que se muestra en un detalle.
     * @param Etiqueta Texto pequeño, como "Cliente"
     * @param Valor Texto grande, como "Carlos Pérez"
     */
    public record Dato(string Etiqueta, string Valor);

    /**
     * @brief Muestra los repuestos, cantidades y total de una venta o entrada.
     */
    public class DocumentoViewModel
    {
        /** Título, como "Venta N° 15". */
        public string Titulo { get; init; }
        /** Fecha y hora. */
        public string Subtitulo { get; init; }
        /** Datos del encabezado. */
        public List<Dato> Datos { get; init; }
        /** Líneas. */
        public List<LineaDocumento> Items { get; init; }
        /** Total. */
        public decimal Total { get; init; }
        /** Indica si la venta está anulada. */
        public bool Anulada { get; init; }
        /** Cierra la ventana. */
        public IRelayCommand CerrarCommand { get; private init; }

        /**
         * @brief Crea el detalle de una venta.
         * @param ventana Ventana que lo muestra
         * @param v Venta con sus líneas
         * @return ViewModel del detalle
         */
        public static DocumentoViewModel DeVenta(Window ventana, Venta v) => new()
        {
            Titulo = $"Venta N° {v.Id}",
            Subtitulo = v.Fecha.ToString("dddd d 'de' MMMM 'de' yyyy · h:mm tt", Formato.Cultura),
            Datos =
            [
                new Dato("Cliente", string.IsNullOrEmpty(v.ClientePlaca) ? v.Cliente : $"{v.Cliente} · {v.ClientePlaca}"),
                new Dato("Vendió", v.UsuarioNombre),
                new Dato("Pago", v.MetodoPago)
            ],
            Items = v.Items,
            Total = v.Total,
            Anulada = v.Anulada,
            CerrarCommand = new RelayCommand(() => ventana.Close())
        };

        /**
         * @brief Crea el detalle de una entrada de mercancía.
         * @param ventana Ventana que lo muestra
         * @param e Entrada con sus líneas
         * @return ViewModel del detalle
         */
        public static DocumentoViewModel DeEntrada(Window ventana, Entrada e) => new()
        {
            Titulo = $"Entrada N° {e.Id}",
            Subtitulo = e.Fecha.ToString("dddd d 'de' MMMM 'de' yyyy · h:mm tt", Formato.Cultura),
            Datos =
            [
                new Dato("Proveedor", e.ProveedorNombre),
                new Dato("Factura", string.IsNullOrEmpty(e.FacturaProveedor) ? "—" : e.FacturaProveedor),
                new Dato("Recibió", e.UsuarioNombre)
            ],
            Items = e.Items,
            Total = e.Total,
            CerrarCommand = new RelayCommand(() => ventana.Close())
        };
    }
}
