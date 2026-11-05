/**
 * @file ListaDocumentosViewModel.cs
 * @brief Compras y abonos de un cliente, o entradas y pagos de un proveedor.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using TallerMoreno.Core.Models;

namespace TallerMoreno.Desktop.ViewModels
{
    /**
     * @brief Fila de la lista: una venta, una entrada o un pago.
     */
    public class FilaDocumento
    {
        /** Número de la venta o entrada, o identificador del pago. */
        public int Id { get; init; }
        /** Indica si la fila es un pago o abono y no una venta o entrada. */
        public bool EsPago { get; init; }
        /** Número que se muestra; los pagos no tienen. */
        public string Numero => EsPago ? "" : Id.ToString();
        /** Fecha y hora. */
        public DateTime Fecha { get; init; }
        /** Pago, factura o quién atendió. */
        public string Descripcion { get; init; }
        /** Unidades; vacío en los pagos. */
        public int? Unidades { get; init; }
        /** Total. */
        public decimal Total { get; init; }
        /** "Anulada", "Fiada", "A crédito", "Pago" o "Abono" */
        public string Estado { get; init; }

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
