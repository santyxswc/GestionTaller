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
         * @brief Muestra el código de recuperación en grande para que el dueño lo anote.
         * @param codigo Código como "K7QM-3XPA-9HTW"
         * @return Tarea que termina al cerrar la ventana
         */
        public static async Task CodigoRecuperacion(string codigo)
        {
            var window = new Window
            {
                Title = "Código de recuperación",
                Width = 480,
                Background = Brushes.White,
                SizeToContent = SizeToContent.Height,
                CanResize = false,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            var listo = new Button { Content = "Ya lo anoté", HorizontalAlignment = HorizontalAlignment.Stretch, IsDefault = true, Margin = new Thickness(0, 24, 0, 0) };
            listo.Click += (_, _) => window.Close(true);

            window.Content = new StackPanel
            {
                Margin = new Thickness(32, 28),
                Children =
                {
                    new TextBlock
                    {
                        Text = "Anota este código",
                        FontSize = 22,
                        FontWeight = FontWeight.Bold,
                        TextAlignment = TextAlignment.Center,
                        Foreground = Brush.Parse("#1C1C1E"),
                        Margin = new Thickness(0, 0, 0, 10)
                    },
                    new TextBlock
                    {
                        Text = "Si algún día olvidas tu contraseña, con este código puedes poner una nueva. "
                             + "Escríbelo en un papel y guárdalo fuera del computador.",
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        FontSize = 16,
                        LineHeight = 23,
                        Foreground = Brush.Parse("#3A3A3C")
                    },
                    new Border
                    {
                        Background = Brush.Parse("#F2F2F7"),
                        CornerRadius = new CornerRadius(14),
                        Padding = new Thickness(16, 18),
                        Margin = new Thickness(0, 20, 0, 0),
                        Child = new SelectableTextBlock
                        {
                            Text = codigo,
                            FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                            FontSize = 34,
                            FontWeight = FontWeight.Bold,
                            LetterSpacing = 2,
                            TextAlignment = TextAlignment.Center,
                            Foreground = Brush.Parse("#1C1C1E")
                        }
                    },
                    new TextBlock
                    {
                        Text = "Cada vez que se usa, el programa da uno nuevo.",
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        FontSize = 13,
                        Foreground = Brush.Parse("#6E6E73"),
                        Margin = new Thickness(0, 10, 0, 0)
                    },
                    listo
                }
            };

            var owner = Owner;
            if (owner == null)
                window.Show();
            else
                await window.ShowDialog<bool>(owner);
        }

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
         * @brief Ventana de error sin dueño, para cuando el programa no puede arrancar.
         *
         * Se usa como ventana principal: al cerrarla se cierra el programa.
         * @param message Mensaje
         * @param title Título
         * @return Ventana lista para mostrar
         */
        public static Window ErrorAlArrancar(string message, string title = "No se pudo abrir el programa")
        {
            var window = Construir(message, title, null, null);
            window.ShowInTaskbar = true;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return window;
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
            var window = Construir(message, title, confirmar, cancelar);
            var owner = Owner;
            if (owner == null)
            {
                window.Show();
                return false;
            }
            return await window.ShowDialog<bool>(owner);
        }

        /**
         * @brief Construye la ventana de mensaje.
         * @param message Mensaje
         * @param title Título
         * @param confirmar Texto del botón destructivo, o null para un solo botón "Entendido"
         * @param cancelar Texto del botón de cancelar
         * @return Ventana sin mostrar; se cierra con true si se eligió el botón de confirmar
         */
        private static Window Construir(string message, string title, string confirmar, string cancelar)
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
            return window;
        }
    }
}
