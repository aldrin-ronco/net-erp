using Common.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;

namespace Common.Helpers
{
    public static class ViewModelExtensions
    {
        private static readonly ConditionalWeakTable<object, ChangeTracker> _trackers = new();
        private static ChangeTracker GetTracker(object vm) => _trackers.GetOrCreateValue(vm);

        /// <summary>
        /// Registra un cambio de propiedad en el ChangeTracker asociado al ViewModel.
        /// Además:
        /// - Sanitiza el valor antes de registrarlo.
        /// - Si el valor es una colección que implementa INotifyCollectionChanged,
        ///   se suscribe a CollectionChanged para marcar la propiedad como cambiada
        ///   cuando se haga Add/Remove/Clear, etc. (nivel 0-1 para listas).
        /// </summary>
        public static void TrackChange(this object viewModel, string propertyName, object? currentValue = null)
        {
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));
            if (string.IsNullOrWhiteSpace(propertyName)) return;

            var tracker = GetTracker(viewModel);

            // 🔹 Sanitizar antes de registrar el cambio
            currentValue = SanitizerRegistry.Sanitize(viewModel.GetType(), propertyName, currentValue);
            tracker.RegisterChange(propertyName, currentValue);

            // 🔹 Soporte para colecciones (nivel 0-1):
            // Si el valor actual es una colección observable, la observamos.
            // Si no lo es (o es null), dejamos de observar cualquier colección previa para esa propiedad.
            if (currentValue is INotifyCollectionChanged observable)
            {
                tracker.ObserveCollection(propertyName, observable);
            }
            else
            {
                // Si antes había una colección asociada a esta propiedad, la desenganchamos.
                tracker.ObserveCollection(propertyName, null);
            }
        }

        public static void SeedValue(this object viewModel, string propertyName, object? value)
        {
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));
            if (string.IsNullOrWhiteSpace(propertyName)) return;

            value = SanitizerRegistry.Sanitize(viewModel.GetType(), propertyName, value);
            GetTracker(viewModel).Seed(propertyName, value);
        }

        public static void ClearSeeds(this object viewModel)
        {
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));
            GetTracker(viewModel).ClearSeeds();
        }

        public static IEnumerable<string> GetChangedProperties(this object viewModel)
        {
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));
            return GetTracker(viewModel).ChangedProperties;
        }

        public static void AcceptChanges(this object viewModel)
        {
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));
            GetTracker(viewModel).AcceptChanges();
        }

        internal static ChangeTracker? GetInternalTracker(this object viewModel)
        {
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));
            return _trackers.TryGetValue(viewModel, out var tracker) ? tracker : null;
        }

        public static bool HasChanges(this object viewModel)
        {
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));

            if (_trackers.TryGetValue(viewModel, out var tracker))
                return tracker.HasChanges;

            return false;
        }
    }
}