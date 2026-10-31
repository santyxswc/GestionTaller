/**
 * @file Dialogs.cs
 * @brief Ventanas de mensaje y de confirmación.
 * @author Santiago Caicedo
 */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;

namespace TallerMoreno.Desktop.Services
{
    /**
     * @brief Errores, confirmaciones y formularios modales.
     *
     * Las confirmaciones se usan solo para lo que no se puede deshacer (eliminar, anular);
     * lo que sale bien se avisa con Avisos, sin ventanas que haya que cerrar.
     */
    public static class Dialogs
    {
        /** Ventana activa, que queda como dueña de los diálogos. */
        public static Window Owner
        {
            get
            {
                var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                return desktop?.Windows.FirstOrDefault(w => w.IsActive) ?? desktop?.MainWindow;
            }
        }

        /**
         * @brief Muestra un mensaje informativo.
         * @param message Mensaje
         * @param title Título
         * @return Tarea que termina al cerrar el mensaje
         */
        public static Task Info(string message, string title = "Listo") =>
            Show(message, title, null, null);

        /**
         * @brief Muestra un mensaje de error.
         * @param message Mensaje
         * @param title Título
         * @return Tarea que termina al cerrar el mensaje
         */
        public static Task Error(string message, string title = "No se pudo completar") =>
            Show(message, title, null, null);

        /**
         * @brief Pide confirmación para una acción que no se puede deshacer.
         * @param message Pregunta
         * @param title Título
         * @param accion Texto del botón que confirma, como "Eliminar"
         * @return true si el usuario confirmó
         */
        public static Task<bool> Confirm(string message, string title, string accion) =>
            Show(message, title, accion, "Cancelar");

        /**
         * @brief Abre un formulario como ventana modal.
         * @param form Ventana del formulario
         * @return true si el formulario se cerró con Close(true), es decir, si se guardó
         */
        public static async Task<bool> ShowForm(Window form)
        {
            var owner = Owner;
            if (owner == null)
            {
                form.Show();
                return false;
            }
            return await form.ShowDialog<bool>(owner);
        }

        /**
         * @brief Construye y muestra la ventana de mensaje.
         * @param message Mensaje
         * @param title Título
         * @param confirmar Texto del botón destructivo, o null para un solo botón "Entendido"
         * @param cancelar Texto del botón de cancelar
         * @return true si se eligió el botón de confirmar
         */
        private static async Task<bool> Show(string message, string title, string confirmar, string cancelar)
        {
            var window = new Window
            {
                Title = title,
                Width = 420,
                Background = Brushes.White,
                SizeToContent = SizeToContent.Height,
                CanResize = false,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var buttons = new StackPanel { Spacing = 10, Margin = new Thickness(0, 24, 0, 0) };
            if (confirmar != null)
            {
                var si = new Button { Content = confirmar, HorizontalAlignment = HorizontalAlignment.Stretch };
                si.Classes.Add("peligro");
                var no = new Button { Content = cancelar, HorizontalAlignment = HorizontalAlignment.Stretch, IsCancel = true, IsDefault = true };
                no.Classes.Add("secundario");
                si.Click += (_, _) => window.Close(true);
                no.Click += (_, _) => window.Close(false);
                buttons.Children.Add(si);
                buttons.Children.Add(no);
            }
            else
            {
                var ok = new Button { Content = "Entendido", HorizontalAlignment = HorizontalAlignment.Stretch, IsDefault = true, IsCancel = true };
                ok.Click += (_, _) => window.Close(true);
                buttons.Children.Add(ok);
            }

            window.Content = new StackPanel
            {
                Margin = new Thickness(32, 28),
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        FontSize = 20,
                        FontWeight = FontWeight.Bold,
                        LetterSpacing = -0.3,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        Foreground = Brush.Parse("#1C1C1E"),
                        Margin = new Thickness(0, 0, 0, 10)
                    },
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        FontSize = 16,
                        LineHeight = 23,
                        Foreground = Brush.Parse("#3A3A3C")
                    },
                    buttons
                }
            };

            var owner = Owner;
            if (owner == null)
            {
                window.Show();
                return false;
            }
            return await window.ShowDialog<bool>(owner);
        }
    }
}
