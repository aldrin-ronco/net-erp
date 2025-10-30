using System;
using System.Collections.Generic;

namespace Common.Helpers
{
    /// <summary>
    /// Permite registrar funciones de sanitización para tipos o propiedades específicas.
    /// </summary>
    public static class SanitizerRegistry
    {
        private static readonly Dictionary<Type, Func<object?, object?>> _typeSanitizers = new();
        private static readonly Dictionary<(Type, string), Func<object?, object?>> _propertySanitizers = new();

        /// <summary>
        /// Registra un sanitizador global para un tipo específico.
        /// Ejemplo: SanitizerRegistry.RegisterType<string>(s => s?.Trim());
        /// </summary>
        public static void RegisterType<T>(Func<T?, T?> sanitizer)
        {
            _typeSanitizers[typeof(T)] = v => sanitizer((T?)v);
        }

        /// <summary>
        /// Registra un sanitizador específico para una propiedad.
        /// </summary>
        public static void RegisterProperty<T>(Type viewModelType, string propertyName, Func<T?, T?> sanitizer)
        {
            _propertySanitizers[(viewModelType, propertyName)] = v => sanitizer((T?)v);
        }

        /// <summary>
        /// Aplica la función de sanitización correspondiente, si existe.
        /// </summary>
        public static object? Sanitize(Type ownerType, string propertyName, object? value)
        {
            // 1️) Sanitizador por propiedad
            if (_propertySanitizers.TryGetValue((ownerType, propertyName), out var propSanitizer))
                return propSanitizer(value);

            // 2️) Sanitizador por tipo
            var valueType = value?.GetType();
            if (valueType != null && _typeSanitizers.TryGetValue(valueType, out var typeSanitizer))
                return typeSanitizer(value);

            // 3️) Sin sanitización
            return value;
        }
    }
}
