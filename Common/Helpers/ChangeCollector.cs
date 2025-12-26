using Common.Helpers;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace Common.Helpers
{
    public static class ChangeCollector
    {
        /// <summary>
        /// Versión simple (compatibilidad hacia atrás).
        /// No aplica transformaciones por colección.
        /// </summary>
        public static ExpandoObject CollectChanges(object viewModel, string? prefix = null)
            => CollectChanges(viewModel, prefix, null);

        /// <summary>
        /// Construye un ExpandoObject con los cambios del ViewModel.
        ///
        /// - Usa el ChangeTracker interno del ViewModel.
        /// - Incluye:
        ///     - Propiedades modificadas.
        ///     - Seeds no modificados.
        /// - Aplica sanitización (SanitizerRegistry).
        /// - Aplica normalización para payload (p.ej. Country → Country.Id si SerializeAsId = true).
        /// - Combina:
        ///     prefix + "." + ExpandoPathAttribute.Path (si existe)
        ///     o, en su defecto, prefix + "." + nombrePropiedad.
        ///
        /// Además, permite aplicar una transformación por item en propiedades que son colecciones
        /// mediante el parámetro collectionItemTransformers.
        /// </summary>
        /// <param name="viewModel">ViewModel origen de los cambios.</param>
        /// <param name="prefix">
        /// Prefijo raíz del payload, por ejemplo:
        /// - "createResponseInput.Data"
        /// - "updateResponseData.Data"
        /// </param>
        /// <param name="collectionItemTransformers">
        /// Diccionario opcional:
        ///  - key: nombre de la propiedad (ej. "Emails")
        ///  - value: Func<object?, object?> que recibe cada item de la colección
        ///           y devuelve el item transformado para el payload.
        /// Sólo se aplica si el valor de la propiedad es una colección (IEnumerable no string).
        /// </param>
        public static ExpandoObject CollectChanges(
            object viewModel,
            string? prefix,
            IDictionary<string, Func<object?, object?>>? collectionItemTransformers)
        {
            dynamic root = new ExpandoObject();
            var tracker = viewModel.GetInternalTracker();

            if (tracker == null)
                return root;

            var vmType = viewModel.GetType();

            // 1️⃣ Propiedades modificadas
            foreach (var propName in tracker.ChangedProperties)
            {
                var propInfo = vmType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (propInfo == null)
                    continue;

                var rawValue = propInfo.GetValue(viewModel);

                // Sanitizar el valor antes de mandarlo al payload
                var sanitized = SanitizerRegistry.Sanitize(vmType, propName, rawValue);

                var pathAttr = propInfo.GetCustomAttribute<ExpandoPathAttribute>();

                // Normalizar para payload (por ejemplo, si es un tipo complejo con SerializeAsId = true → Id)
                var normalized = NormalizeForPayload(propInfo, sanitized, pathAttr);

                // Aplicar transformación por colección si corresponde
                normalized = ApplyCollectionItemTransformIfNeeded(
                    propName,
                    normalized,
                    collectionItemTransformers
                );

                string path = BuildPath(prefix, propName, pathAttr);

                ExpandoHelper.SetNestedProperty(root, path, normalized);
            }

            // 2️⃣ Seeds no modificados: se envían sólo si la propiedad no aparece en ChangedProperties
            foreach (var kv in tracker.SeedValues)
            {
                string propName = kv.Key;

                // Si la propiedad fue cambiada, el valor de ChangedProperties tiene prioridad sobre el seed
                if (tracker.ChangedProperties.Contains(propName))
                    continue;

                var propInfo = vmType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (propInfo == null)
                    continue;

                var pathAttr = propInfo.GetCustomAttribute<ExpandoPathAttribute>();

                // Sanitizar el seed
                object? sanitizedSeed = SanitizerRegistry.Sanitize(vmType, propName, kv.Value);

                // Normalizar para payload (mismo criterio que para propiedades modificadas)
                var normalizedSeed = NormalizeForPayload(propInfo, sanitizedSeed, pathAttr);

                // Aplicar transformación por colección si corresponde
                normalizedSeed = ApplyCollectionItemTransformIfNeeded(
                    propName,
                    normalizedSeed,
                    collectionItemTransformers
                );

                string path = BuildPath(prefix, propName, pathAttr);

                ExpandoHelper.SetNestedProperty(root, path, normalizedSeed);
            }

            return root;
        }

        /// <summary>
        /// Construye la ruta final a usar en el ExpandoObject.
        ///
        /// Reglas:
        /// - Si hay ExpandoPathAttribute:
        ///     path = prefix + "." + attr.Path (si prefix no es null/empty)
        ///     path = attr.Path (si prefix es null/empty)
        /// - Si NO hay ExpandoPathAttribute:
        ///     path = prefix + "." + propertyName (si prefix no es null/empty)
        ///     path = propertyName (si prefix es null/empty)
        /// </summary>
        private static string BuildPath(string? prefix, string propertyName, ExpandoPathAttribute? pathAttr)
        {
            if (pathAttr != null && !string.IsNullOrWhiteSpace(pathAttr.Path))
            {
                if (!string.IsNullOrEmpty(prefix))
                    return $"{prefix}.{pathAttr.Path}";

                return pathAttr.Path;
            }

            if (!string.IsNullOrEmpty(prefix))
                return $"{prefix}.{propertyName}";

            return propertyName;
        }

        /// <summary>
        /// Normaliza el valor antes de enviarlo al payload.
        ///
        /// - Si el valor es null → null.
        /// - Si el tipo es simple (string, bool, int, DateTime, Guid, enum, etc.) → se deja igual.
        /// - Si el tipo es complejo:
        ///     - Si el atributo tiene SerializeAsId = true:
        ///         - Busca propiedad Id (o la indicada en IdPropertyName, case-insensitive).
        ///         - Si la encuentra, retorna ese Id.
        ///         - Si no, retorna el objeto completo para no perder información.
        ///     - Si NO tiene SerializeAsId = true:
        ///         - Se retorna el objeto completo.
        /// </summary>
        private static object? NormalizeForPayload(
            PropertyInfo propInfo,
            object? value,
            ExpandoPathAttribute? pathAttr)
        {
            if (value == null)
                return null;

            var valueType = value.GetType();

            // Tipos simples se dejan tal cual
            if (IsSimpleType(valueType))
                return value;

            // Si no está marcada para serializar como Id, dejamos el objeto completo
            if (pathAttr?.SerializeAsId != true)
                return value;

            // Aquí sí queremos serializar sólo el Id
            var idPropName = !string.IsNullOrWhiteSpace(pathAttr.IdPropertyName)
                ? pathAttr.IdPropertyName!
                : "Id";

            var idProp = valueType.GetProperty(
                idPropName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (idProp == null)
            {
                // No encontramos propiedad Id, devolvemos el objeto completo
                return value;
            }

            return idProp.GetValue(value);
        }

        /// <summary>
        /// Si la propiedad actual corresponde a una colección y existe un transformer
        /// registrado para esa propiedad en collectionItemTransformers, aplica la función
        /// a cada item de la colección y devuelve una lista con los items transformados.
        ///
        /// Si no hay transformer, o el valor no es colección, devuelve el valor sin cambios.
        /// </summary>
        private static object? ApplyCollectionItemTransformIfNeeded(
            string propertyName,
            object? value,
            IDictionary<string, Func<object?, object?>>? collectionItemTransformers)
        {
            if (value == null || collectionItemTransformers == null)
                return value;

            if (!collectionItemTransformers.TryGetValue(propertyName, out var transformer))
                return value;

            // String implementa IEnumerable, pero no queremos tratarlo como colección de chars
            if (value is string)
                return value;

            if (value is System.Collections.IEnumerable enumerable)
            {
                var list = new List<object?>();
                foreach (var item in enumerable)
                {
                    list.Add(transformer(item));
                }
                return list;
            }

            // No es colección, devolvemos el valor tal cual
            return value;
        }

        /// <summary>
        /// Determina si un tipo puede considerarse "simple" para efectos de serialización:
        /// primitivos, string, decimal, DateTime, Guid, enums, etc.
        /// Todo lo que no sea simple se trata como "complejo".
        /// </summary>
        private static bool IsSimpleType(Type type)
        {
            if (type.IsPrimitive || type.IsEnum)
                return true;

            if (type == typeof(string) ||
                type == typeof(decimal) ||
                type == typeof(DateTime) ||
                type == typeof(DateTimeOffset) ||
                type == typeof(TimeSpan) ||
                type == typeof(Guid))
                return true;

            var underlying = Nullable.GetUnderlyingType(type);
            if (underlying is not null)
                return IsSimpleType(underlying);

            return false;
        }
    }
}