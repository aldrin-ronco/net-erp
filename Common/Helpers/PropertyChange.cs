using System;

namespace Common.Helpers
{
    /// <summary>
    /// Represents a change in a property value
    /// </summary>
    public class PropertyChange
    {
        /// <summary>
        /// Name of the property that changed
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Original value before the change
        /// </summary>
        public object OldValue { get; set; }

        /// <summary>
        /// New value after the change
        /// </summary>
        public object NewValue { get; set; }

        /// <summary>
        /// Type of the property
        /// </summary>
        public Type PropertyType { get; set; }

        /// <summary>
        /// Returns a string representation of the change
        /// </summary>
        public override string ToString()
        {
            return $"{PropertyName}: {OldValue ?? "null"} → {NewValue ?? "null"}";
        }
    }
}
