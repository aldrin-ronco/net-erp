using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Common.Helpers
{
    /// <summary>
    /// Métodos de extensión para agregar tracking de cambios a cualquier ViewModel
    /// sin modificar su jerarquía de herencia.
    /// </summary>
    public static class ViewModelExtensions
    {
        private static readonly ConditionalWeakTable<object, ChangeTracker> _trackers = new();

        /// <summary>
        /// Registra un cambio de propiedad en el ViewModel.
        /// </summary>
        public static void TrackChange(this object viewModel, string propertyName)
        {
            var tracker = _trackers.GetOrCreateValue(viewModel);
            tracker.RegisterChange(propertyName);
        }

        /// <summary>
        /// Obtiene la lista de propiedades modificadas para el ViewModel.
        /// </summary>
        public static IEnumerable<string> GetChangedProperties(this object viewModel)
        {
            if (_trackers.TryGetValue(viewModel, out var tracker))
                return tracker.ChangedProperties;

            return Array.Empty<string>();
        }

        /// <summary>
        /// Limpia el estado de cambios registrados.
        /// </summary>
        public static void AcceptChanges(this object viewModel)
        {
            if (_trackers.TryGetValue(viewModel, out var tracker))
                tracker.AcceptChanges();
        }

        public static bool HasChanges(this object viewModel)
        {
            if (_trackers.TryGetValue(viewModel, out var tracker))
                return tracker.HasChanges;
            return false;
        }
    }
}
