/**
 * @file TarjetaRepuesto.axaml.cs
 * @brief Tarjeta con la foto y los datos de un repuesto.
 * @author Santiago Caicedo
 */
using Avalonia.Controls;

namespace TallerMoreno.Desktop.Views
{
    /**
     * @brief Tarjeta de repuesto que se usa en Vender y en Inventario.
     */
    public partial class TarjetaRepuesto : UserControl
    {
        /**
         * @brief Crea la tarjeta.
         */
        public TarjetaRepuesto()
        {
            InitializeComponent();
        }
    }
}
