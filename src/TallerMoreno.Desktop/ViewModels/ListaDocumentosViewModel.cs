/**
 * @file ListaDocumentosViewModel.cs
 * @brief Compras de un cliente o entradas de un proveedor.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace TallerMoreno.Desktop.ViewModels
{
    /**
     * @brief Fila de la lista: una venta o una entrada.
     */
    public class FilaDocumento
    {
        /** Número. */
        public int Id { get; init; }
        /** Fecha y hora. */
        public DateTime Fecha { get; init; }
        /** Pago, factura o quién atendió. */
        public string Descripcion { get; init; }
        /** Unidades. */
        public int Unidades { get; init; }
        /** Total. */
        public decimal Total { get; init; }
        /** "Anulada" o vacío. */
        public string Estado { get; init; }
    }

    /**
     * @brief Resumen y lista de documentos de un cliente o proveedor; al tocar uno se abre su detalle.
     */
    public class ListaDocumentosViewModel
    {
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

        /** Abre el detalle de un documento. */
        public IAsyncRelayCommand<FilaDocumento> AbrirCommand { get; }
        /** Cierra la ventana. */
        public IRelayCommand CerrarCommand { get; }

        /**
         * @brief Crea la lista.
         * @param ventana Ventana que la muestra
         * @param abrir Abre el detalle de un documento por su número
         */
        public ListaDocumentosViewModel(Window ventana, Func<int, Task> abrir)
        {
            AbrirCommand = new AsyncRelayCommand<FilaDocumento>(f => f == null ? Task.CompletedTask : abrir(f.Id));
            CerrarCommand = new RelayCommand(() => ventana.Close());
        }
    }
}
