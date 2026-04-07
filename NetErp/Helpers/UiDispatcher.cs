using System;
using System.Windows;

namespace NetErp.Helpers
{
    /// <summary>
    /// Helper para marshalizar operaciones al hilo UI cuando hay una aplicación WPF corriendo.
    /// En entornos sin WPF activo (ej. unit tests), ejecuta la acción directamente en el hilo actual.
    /// Usado por caches que mutan <c>ObservableCollection</c> bound al UI — previene cross-thread
    /// crashes en runtime y evita <c>NullReferenceException</c> en tests donde <c>Application.Current</c> es null.
    /// </summary>
    public static class UiDispatcher
    {
        /// <summary>
        /// Ejecuta la acción en el hilo UI de forma síncrona.
        /// Si no hay <c>Application.Current</c> (test environment), ejecuta inmediatamente en el hilo actual.
        /// </summary>
        public static void Invoke(Action action)
        {
            if (action is null) return;

            if (Application.Current?.Dispatcher is { } dispatcher)
            {
                #pragma warning disable VSTHRD001 // Synchronous Dispatcher.Invoke is intentional here — this helper is designed to be callable from synchronous code paths (cache mutations inside locks). SwitchToMainThreadAsync requires async context.
                dispatcher.Invoke(action);
                #pragma warning restore VSTHRD001
            }
            else
            {
                action();
            }
        }
    }
}
