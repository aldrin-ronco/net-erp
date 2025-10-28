using System;
using System.Dynamic;
using System.Reflection;

namespace Common.Helpers
{
    /// <summary>
    /// Construye un ExpandoObject a partir de las propiedades modificadas en un ViewModel.
    /// Soporta mapeo por atributo [ExpandoPath] y un prefijo opcional.
    /// </summary>
    public static class ChangeCollector
    {
        /// <summary>
        /// Genera un ExpandoObject con las propiedades modificadas.
        /// </summary>
        /// <param name="viewModel">El objeto del cual recolectar los cambios.</param>
        /// <param name="prefix">
        /// Prefijo opcional que se antepone a todas las rutas (por ejemplo: "variables" o "variables.customer").
        /// Si la propiedad tiene [ExpandoPath], el prefijo se ignora para esa propiedad.
        /// </param>
        public static ExpandoObject CollectChanges(object viewModel, string? prefix = null)
        {
            dynamic root = new ExpandoObject();

            foreach (var propName in viewModel.GetChangedProperties())
            {
                var propInfo = viewModel.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (propInfo == null) continue;

                var value = propInfo.GetValue(viewModel);

                // Determinar ruta destino
                var pathAttr = propInfo.GetCustomAttribute<ExpandoPathAttribute>();
                string path;

                if (pathAttr != null)
                {
                    // Si tiene atributo, se respeta su ruta exacta
                    path = pathAttr.Path;
                }
                else if (!string.IsNullOrEmpty(prefix))
                {
                    // Si hay prefijo, se antepone
                    path = $"{prefix}.{propName}";
                }
                else
                {
                    // Caso base: sin prefijo ni atributo
                    path = propName;
                }

                ExpandoHelper.SetNestedProperty(root, path, value);
            }

            return root;
        }
    }
}
