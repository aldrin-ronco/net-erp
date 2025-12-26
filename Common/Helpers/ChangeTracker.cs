using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Common.Helpers
{
    /// <summary>
    /// Gestiona el seguimiento de propiedades modificadas y valores semilla (por defecto).
    /// También puede observar colecciones que implementan INotifyCollectionChanged
    /// para marcar la propiedad como cambiada cuando se modifique la lista.
    /// </summary>
    public class ChangeTracker
    {
        private readonly HashSet<string> _changed = new();
        private readonly Dictionary<string, object?> _seedValues = new();

        // Suscripciones a colecciones observables por propiedad
        // clave: nombre de la propiedad
        // valor: (colección observada, handler suscrito)
        private readonly Dictionary<string, (INotifyCollectionChanged Collection, NotifyCollectionChangedEventHandler Handler)> _collectionSubscriptions
            = new(StringComparer.Ordinal);

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
        /// (No afecta las suscripciones a colecciones; se siguen observando).
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

        /// <summary>
        /// Observa una colección asociada a una propiedad.
        /// - Si collection es null, desuscribe cualquier colección previa para esa propiedad.
        /// - Si collection no es null, se suscribe a CollectionChanged y marca la propiedad como cambiada
        ///   cada vez que se modifique la colección (Add/Remove/Clear/etc.).
        /// </summary>
        internal void ObserveCollection(string propertyName, INotifyCollectionChanged? collection)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return;

            // Si ya había una colección observada para esta propiedad, desuscribirla
            if (_collectionSubscriptions.TryGetValue(propertyName, out var existing))
            {
                existing.Collection.CollectionChanged -= existing.Handler;
                _collectionSubscriptions.Remove(propertyName);
            }

            if (collection is null)
                return;

            // Nuevo handler que marca la propiedad como cambiada cuando la colección cambie
            NotifyCollectionChangedEventHandler handler = (_, __) =>
            {
                RegisterCollectionChange(propertyName);
            };

            collection.CollectionChanged += handler;
            _collectionSubscriptions[propertyName] = (collection, handler);
        }

        /// <summary>
        /// Marca una propiedad como cambiada debido a un cambio en su colección asociada.
        /// Aquí no comparamos contra seed: para listas sólo nos interesa saber que hubo cambio.
        /// </summary>
        private void RegisterCollectionChange(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return;

            _changed.Add(propertyName);
        }
    }
}
