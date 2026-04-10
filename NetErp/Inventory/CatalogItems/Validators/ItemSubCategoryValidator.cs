using System.Collections.Generic;

namespace NetErp.Inventory.CatalogItems.Validators
{
    /// <summary>
    /// Lógica de validación pura para ItemSubCategory.
    /// </summary>
    public class ItemSubCategoryValidator
    {
        public IReadOnlyList<string> Validate(string propertyName, string? value, ItemSubCategoryValidationContext context)
        {
            return propertyName switch
            {
                "Name" when string.IsNullOrWhiteSpace(value)
                    => ["El nombre de la subcategoría no puede estar vacío"],
                _ => []
            };
        }

        public Dictionary<string, IReadOnlyList<string>> ValidateAll(ItemSubCategoryValidationContext context)
        {
            Dictionary<string, IReadOnlyList<string>> result = [];
            AddIfErrors(result, "Name", Validate("Name", context.Name, context));
            return result;
        }

        public bool CanSave(ItemSubCategoryCanSaveContext context)
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

    public class ItemSubCategoryValidationContext
    {
        public string? Name { get; init; }
    }

    public class ItemSubCategoryCanSaveContext
    {
        public bool IsBusy { get; init; }
        public string? Name { get; init; }
        public bool HasChanges { get; init; }
        public bool HasErrors { get; init; }
    }
}
