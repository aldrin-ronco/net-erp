using Common.Helpers;
using System;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace Common.Helpers
{
    public static class ChangeCollector
    {
        public static ExpandoObject CollectChanges(object viewModel, string? prefix = null)
        {
            dynamic root = new ExpandoObject();
            var tracker = viewModel.GetInternalTracker();

            if (tracker == null)
                return root;

            // 1️⃣ Propiedades modificadas
            foreach (var propName in tracker.ChangedProperties)
            {
                var propInfo = viewModel.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (propInfo == null) continue;

                var value = propInfo.GetValue(viewModel);
                value = SanitizerRegistry.Sanitize(viewModel.GetType(), propName, value);

                var pathAttr = propInfo.GetCustomAttribute<ExpandoPathAttribute>();
                string path = pathAttr?.Path ??
                              (!string.IsNullOrEmpty(prefix) ? $"{prefix}.{propName}" : propName);

                ExpandoHelper.SetNestedProperty(root, path, value);
            }

            // 2️⃣ Propiedades con seed sin cambios
            foreach (var kv in tracker.SeedValues)
            {
                string propName = kv.Key;
                if (tracker.ChangedProperties.Contains(propName))
                    continue;

                var propInfo = viewModel.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (propInfo == null) continue;

                object? seedValue = SanitizerRegistry.Sanitize(viewModel.GetType(), propName, kv.Value);

                var pathAttr = propInfo.GetCustomAttribute<ExpandoPathAttribute>();
                string path = pathAttr?.Path ??
                              (!string.IsNullOrEmpty(prefix) ? $"{prefix}.{propName}" : propName);

                ExpandoHelper.SetNestedProperty(root, path, seedValue);
            }

            return root;
        }
    }
}
