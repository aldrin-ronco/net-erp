using System;
using System.Collections.Generic;

namespace Common.Helpers
{
    /// <summary>
    /// Gestiona el seguimiento de propiedades modificadas y valores semilla (por defecto).
    /// </summary>
    public class ChangeTracker
    {
        private readonly HashSet<string> _changed = new();
        private readonly Dictionary<string, object?> _seedValues = new();

        /// <summary>
        /// Registra una propiedad como modificada.
        /// </summary>
        public void RegisterChange(string propertyName, object? currentValue = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return;

            // Si existe un seed y el valor coincide con la semilla, se remueve de cambios
            if (_seedValues.TryGetValue(propertyName, out var seedValue))
            {
                if (Equals(seedValue, currentValue))
                {
                    _changed.Remove(propertyName);
                    return;
                }
            }

            _changed.Add(propertyName);
        }

        /// <summary>
        /// Registra un valor semilla (por defecto) para una propiedad.
        /// </summary>
        public void Seed(string propertyName, object? value)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return;
            _seedValues[propertyName] = value;
        }

        /// <summary>
        /// Limpia los valores semilla.
        /// </summary>
        public void ClearSeeds() => _seedValues.Clear();

        /// <summary>
        /// Limpia el registro de cambios.
        /// </summary>
        public void AcceptChanges() => _changed.Clear();

        /// <summary>
        /// Propiedades modificadas actualmente.
        /// </summary>
        public IEnumerable<string> ChangedProperties => _changed;

        /// <summary>
        /// Valores semilla registrados.
        /// </summary>
        public IReadOnlyDictionary<string, object?> SeedValues => _seedValues;

        public bool HasChanges => _changed.Count > 0;
    }
}


