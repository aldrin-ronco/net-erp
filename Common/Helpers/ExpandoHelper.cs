using System.Collections.Generic;
using System.Dynamic;

namespace Common.Helpers
{
    /// <summary>
    /// Utilidades para manipular objetos dinámicos (ExpandoObject) en forma anidada.
    /// </summary>
    public static class ExpandoHelper
    {
        /// <summary>
        /// Crea o actualiza una propiedad anidada en un ExpandoObject usando una ruta de tipo "propA.propB.propC".
        /// </summary>
        public static void SetNestedProperty(ExpandoObject expando, string path, object? value)
        {
            var dict = (IDictionary<string, object?>)expando;
            var parts = path.Split('.');

            for (int i = 0; i < parts.Length; i++)
            {
                string key = parts[i];

                if (i == parts.Length - 1)
                {
                    dict[key] = value;
                }
                else
                {
                    if (!dict.TryGetValue(key, out var next))
                    {
                        next = new ExpandoObject();
                        dict[key] = next;
                    }

                    dict = (IDictionary<string, object?>)next!;
                }
            }
        }
    }
}

