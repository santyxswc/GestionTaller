/**
 * @file Avisos.cs
 * @brief Avisos breves que aparecen y se ocultan solos.
 * @author Santiago Caicedo
 */
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TallerMoreno.Desktop.Services
{
    /**
     * @brief Confirma una acción con un aviso en la parte baja de la ventana.
     *
     * Para lo que salió bien no se abre una ventana que haya que cerrar: el aviso se ve
     * unos segundos y desaparece solo, así nadie tiene que tocar el computador de más.
     */
    public partial class Avisos : ObservableObject
    {
        /** Tiempo que se muestra un aviso. */
        private static readonly TimeSpan Duracion = TimeSpan.FromSeconds(4);

        /** Temporizador que oculta el aviso. */
        private readonly DispatcherTimer _temporizador;

        /** Texto del aviso. */
        [ObservableProperty]
        private string mensaje;

        /** Indica si el aviso está a la vista. */
        [ObservableProperty]
        private bool visible;

        /** Indica si el aviso es de error (rojo) en vez de confirmación (verde). */
        [ObservableProperty]
        private bool esError;

        /**
         * @brief Crea el servicio de avisos.
         */
        public Avisos()
        {
            _temporizador = new DispatcherTimer { Interval = Duracion };
            _temporizador.Tick += (_, _) =>
            {
                _temporizador.Stop();
                Visible = false;
            };
        }

        /**
         * @brief Muestra un aviso de confirmación.
         * @param mensaje Texto
         */
        public void Exito(string mensaje) => Mostrar(mensaje, false);

        /**
         * @brief Muestra un aviso de error.
         * @param mensaje Texto
         */
        public void Error(string mensaje) => Mostrar(mensaje, true);

        /**
         * @brief Muestra el aviso y reinicia el tiempo.
         * @param mensaje Texto
         * @param error true si es un error
         */
        private void Mostrar(string mensaje, bool error)
        {
            Mensaje = mensaje;
            EsError = error;
            Visible = true;
            _temporizador.Stop();
            _temporizador.Start();
        }
    }
}
