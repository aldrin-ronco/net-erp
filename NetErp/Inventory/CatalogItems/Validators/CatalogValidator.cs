using System.Collections.Generic;

namespace NetErp.Inventory.CatalogItems.Validators
{
    /// <summary>
    /// Lógica de validación pura para Catalog.
    /// </summary>
    public class CatalogValidator
    {
        public IReadOnlyList<string> Validate(string propertyName, string? value, CatalogValidationContext context)
        {
            return propertyName switch
            {
                "Name" when string.IsNullOrWhiteSpace(value)
                    => ["El nombre del catálogo no puede estar vacío"],
                _ => []
            };
        }

        public Dictionary<string, IReadOnlyList<string>> ValidateAll(CatalogValidationContext context)
        {
            Dictionary<string, IReadOnlyList<string>> result = [];
            AddIfErrors(result, "Name", Validate("Name", context.Name, context));
            return result;
        }

        public bool CanSave(CatalogCanSaveContext context)
        {
            if (context.IsBusy) return false;
            if (string.IsNullOrWhiteSpace(context.Name)) return false;
            if (!context.HasChanges) return false;
            if (context.HasErrors) return false;
            return true;
        }

        private static void AddIfErrors(Dictionary<string, IReadOnlyList<string>> dict,
                                         string key, IReadOnlyList<string> errors)
        {
            if (errors.Count > 0) dict[key] = errors;
        }
    }

    public class CatalogValidationContext
    {
        public string? Name { get; init; }
    }

    public class CatalogCanSaveContext
    {
        public bool IsBusy { get; init; }
        public string? Name { get; init; }
        public bool HasChanges { get; init; }
        public bool HasErrors { get; init; }
    }
}
