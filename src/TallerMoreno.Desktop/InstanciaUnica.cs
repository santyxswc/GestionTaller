/**
 * @file InstanciaUnica.cs
 * @brief Trae al frente el programa cuando ya está abierto.
 * @author Santiago Caicedo
 */
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TallerMoreno.Desktop
{
    /**
     * @brief Muestra la ventana del programa que ya está abierto, o avisa si no la encuentra.
     *
     * Se usa antes de iniciar Avalonia, por eso habla directamente con Windows.
     */
    internal static class InstanciaUnica
    {
        /** Restaurar la ventana si está minimizada. */
        private const int SwRestore = 9;
        /** Ícono de información en el mensaje. */
        private const uint MbIconInformation = 0x40;

        /**
         * @brief Busca la ventana del otro proceso del programa y la pone al frente.
         */
        public static void MostrarLaQueYaEstaAbierta()
        {
            if (!OperatingSystem.IsWindows())
                return;

            using var actual = Process.GetCurrentProcess();
            foreach (var otro in Process.GetProcessesByName(actual.ProcessName))
            {
                using (otro)
                {
                    if (otro.Id == actual.Id || otro.MainWindowHandle == IntPtr.Zero)
                        continue;
                    if (IsIconic(otro.MainWindowHandle))
                        ShowWindow(otro.MainWindowHandle, SwRestore);
                    SetForegroundWindow(otro.MainWindowHandle);
                    return;
                }
            }

            MessageBoxW(IntPtr.Zero, "Taller Moreno ya está abierto. Búscalo en la barra de tareas, abajo en la pantalla.",
                        "Taller Moreno", MbIconInformation);
        }

        /** Indica si la ventana está minimizada. */
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr ventana);

        /** Cambia cómo se muestra la ventana. */
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr ventana, int modo);

        /** Pone la ventana al frente. */
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr ventana);

        /** Mensaje de Windows. */
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(IntPtr dueno, string texto, string titulo, uint tipo);
    }
}
