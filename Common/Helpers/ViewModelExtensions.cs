using Common.Helpers;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Common.Helpers
{
    public static class ViewModelExtensions
    {
        private static readonly ConditionalWeakTable<object, ChangeTracker> _trackers = new();
        private static ChangeTracker GetTracker(object vm) => _trackers.GetOrCreateValue(vm);

        public static void TrackChange(this object viewModel, string propertyName, object? currentValue = null)
        {
            // 🔹 Sanitizar antes de registrar el cambio
            currentValue = SanitizerRegistry.Sanitize(viewModel.GetType(), propertyName, currentValue);
            GetTracker(viewModel).RegisterChange(propertyName, currentValue);
        }

        public static void SeedValue(this object viewModel, string propertyName, object? value)
        {
            value = SanitizerRegistry.Sanitize(viewModel.GetType(), propertyName, value);
            GetTracker(viewModel).Seed(propertyName, value);
        }

        public static void ClearSeeds(this object viewModel) => GetTracker(viewModel).ClearSeeds();
        public static IEnumerable<string> GetChangedProperties(this object viewModel) => GetTracker(viewModel).ChangedProperties;
        public static void AcceptChanges(this object viewModel) => GetTracker(viewModel).AcceptChanges();
        internal static ChangeTracker? GetInternalTracker(this object viewModel) => _trackers.TryGetValue(viewModel, out var tracker) ? tracker : null;
        public static bool HasChanges(this object viewModel)
        {
            if (_trackers.TryGetValue(viewModel, out var tracker))
                return tracker.HasChanges;
            return false;
        }
    }
}

