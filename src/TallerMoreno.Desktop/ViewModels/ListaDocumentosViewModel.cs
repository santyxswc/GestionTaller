/**
 * @file ListaDocumentosViewModel.cs
 * @brief Compras, abonos y devoluciones de un cliente, o entradas, pagos y devoluciones de un proveedor.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using TallerMoreno.Core.Models;

namespace TallerMoreno.Desktop.ViewModels
{
    /**
     * @brief Qué es una fila de la lista.
     */
    public enum TipoFila
    {
        /** Venta o entrada. */
        Documento,
        /** Pago a un proveedor o abono de un cliente. */
        Pago,
        /** Devolución o garantía. */
        Devolucion
    }

    /**
     * @brief Fila de la lista: una venta, una entrada, un pago o una devolución.
     */
    public class FilaDocumento
    {
        /** Número de la venta o entrada, o identificador del pago o la devolución. */
        public int Id { get; init; }
        /** Qué es la fila. */
        public TipoFila Tipo { get; init; }
        /** Indica si la fila es un pago o abono. */
        public bool EsPago => Tipo == TipoFila.Pago;
        /** Número que se muestra; solo las ventas y entradas tienen. */
        public string Numero => Tipo == TipoFila.Documento ? Id.ToString() : "";
        /** Fecha y hora. */
        public DateTime Fecha { get; init; }
        /** Pago, factura o quién atendió. */
        public string Descripcion { get; init; }
        /** Unidades; vacío en los pagos. */
        public int? Unidades { get; init; }
        /** Total; vacío en una devolución sin plata. */
        public decimal? Total { get; init; }
        /** "Anulada", "Fiada", "A crédito", "Pago", "Abono", "Devolución" o "Pendiente" */
        public string Estado { get; init; }

        /**
         * @brief Texto de una devolución en la lista.
         * @param d Devolución
         * @param deProveedor true si es una devolución a un proveedor
         * @param conValor true para decir cuánta plata (donde no hay una columna con el valor)
         * @return Descripción, como "Batería 12V · Garantía · se le cambió por otro · no carga"
         */
        public static string DescripcionDevolucion(Devolucion d, bool deProveedor, bool conValor = true)
        {
            var partes = new[] { d.RepuestoNombre, d.Motivo, d.TextoResultado(deProveedor, conValor), d.Notas };
            return string.Join(" · ", partes.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        /**
         * @brief Fila de una devolución.
         * @param d Devolución
         * @param deProveedor true si es una devolución a un proveedor
         * @return Fila
         */
        public static FilaDocumento DeDevolucion(Devolucion d, bool deProveedor) => new()
        {
            Id = d.Id,
            Tipo = TipoFila.Devolucion,
            Fecha = d.Fecha,
            Descripcion = DescripcionDevolucion(d, deProveedor, conValor: false),
            Unidades = d.Cantidad,
            Total = d.Valor > 0 ? d.Valor : null,
            Estado = d.EsPendiente ? "Pendiente" : "Devolución"
        };

        /**
         * @brief Texto de un pago o abono en la lista.
         * @param tipo "Pago" o "Abono"
         * @param g Pago
         * @return Descripción, como "Transferencia · comprobante 123 · registró Don Moreno"
         */
        public static string DescripcionPago(string tipo, Pago g)
        {
            var partes = new[] { g.MetodoPago ?? tipo, g.Notas, string.IsNullOrEmpty(g.UsuarioNombre) ? null : $"registró {g.UsuarioNombre}" };
            return string.Join(" · ", partes.Where(p => !string.IsNullOrWhiteSpace(p)));
        }
    }

    /**
     * @brief Resumen y lista de documentos de un cliente o proveedor; al tocar uno se abre su detalle.
     */
    public class ListaDocumentosViewModel
    {
        /** Ventana que muestra la lista. */
        private readonly Window _ventana;

        /** Nombre del cliente o proveedor. */
        public string Titulo { get; init; }
        /** Datos de contacto. */
        public string Subtitulo { get; init; }
        /** Cifras del resumen. */
        public List<Dato> Cifras { get; init; }
        /** Documentos, del más reciente al más antiguo. */
        public List<FilaDocumento> Filas { get; init; }
        /** Indica si no hay documentos. */
        public bool Vacia => Filas.Count == 0;
        /** Texto cuando no hay documentos. */
        public string TextoVacio { get; init; }
        /** Explicación bajo la lista. */
        public string Ayuda { get; init; } = "Toca una fila dos veces para ver el detalle.";
        /** Texto del botón de acción, como "Registrar pago"; vacío lo oculta. */
        public string TextoAccion { get; init; }
        /** Indica si se muestra el botón de acción. */
        public bool HayAccion => !string.IsNullOrEmpty(TextoAccion);
        /** Indica si algo cambió y hay que volver a abrir la lista con los datos nuevos. */
        public bool Recargar { get; private set; }

        /** Abre el detalle de un documento. */
        public IAsyncRelayCommand<FilaDocumento> AbrirCommand { get; }
        /** Acción del botón, como registrar un pago. */
        public IAsyncRelayCommand AccionCommand { get; }
        /** Cierra la ventana. */
        public IRelayCommand CerrarCommand { get; }

        /**
         * @brief Crea la lista.
         * @param ventana Ventana que la muestra
         * @param abrir Abre el detalle de una fila; devuelve true si cambió algo y hay que recargar
         * @param accion Acción del botón; devuelve true si cambió algo y hay que recargar (opcional)
         */
        public ListaDocumentosViewModel(Window ventana, Func<FilaDocumento, Task<bool>> abrir, Func<Task<bool>> accion = null)
        {
            _ventana = ventana;
            AbrirCommand = new AsyncRelayCommand<FilaDocumento>(async f =>
            {
                if (f != null && await abrir(f))
                    CerrarParaRecargar();
            });
            AccionCommand = new AsyncRelayCommand(async () =>
            {
                if (accion != null && await accion())
                    CerrarParaRecargar();
            });
            CerrarCommand = new RelayCommand(() => ventana.Close());
        }

        /**
         * @brief Cierra la lista avisando que hay que abrirla otra vez con los datos nuevos.
         */
        private void CerrarParaRecargar()
        {
            Recargar = true;
            _ventana.Close();
        }
    }
}
