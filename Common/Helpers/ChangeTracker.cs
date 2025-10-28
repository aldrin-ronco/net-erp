using System;
using System.Collections.Generic;

namespace Common.Helpers
{
    /// <summary>
    /// Encapsula el seguimiento de propiedades modificadas en un ViewModel o entidad.
    /// </summary>
    public class ChangeTracker
    {
        private readonly HashSet<string> _changed = new();

        /// <summary>
        /// Registra una propiedad como modificada.
        /// </summary>
        public void RegisterChange(string propertyName)
        {
            if (!string.IsNullOrWhiteSpace(propertyName))
                _changed.Add(propertyName);
        }

        /// <summary>
        /// Verifica si una propiedad específica ha cambiado.
        /// </summary>
        public bool IsChanged(string propertyName) => _changed.Contains(propertyName);

        /// <summary>
        /// Obtiene todas las propiedades modificadas.
        /// </summary>
        public IEnumerable<string> ChangedProperties => _changed;

        /// <summary>
        /// Limpia el registro de propiedades modificadas (acepta los cambios actuales).
        /// </summary>
        public void AcceptChanges() => _changed.Clear();

        /// <summary>
        /// Propiedad que indica si hay cambios registrados.
        /// </summary>
        public bool HasChanges => _changed.Count > 0;
    }
}

