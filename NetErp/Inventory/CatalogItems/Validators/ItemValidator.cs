using System.Collections.Generic;

namespace NetErp.Inventory.CatalogItems.Validators
{
    /// <summary>
    /// Lógica de validación pura para Item.
    /// </summary>
    public class ItemValidator
    {
        public IReadOnlyList<string> Validate(string propertyName, object? value, ItemValidationContext context)
        {
            return propertyName switch
            {
                "Name" when string.IsNullOrWhiteSpace(value as string)
                    => ["El nombre del item no puede estar vacío"],
                "SelectedMeasurementUnit" when value is null
                    => ["Debe seleccionar una unidad de medida"],
                "SelectedAccountingGroup" when value is null
                    => ["Debe seleccionar un grupo contable"],
                "SelectedSize" when context.RequiresSizeCategory && value is null
                    => ["Debe seleccionar una categoría de tallas para el modo Talla"],
                _ => []
            };
        }

        public Dictionary<string, IReadOnlyList<string>> ValidateAll(ItemValidationContext context)
        {
            Dictionary<string, IReadOnlyList<string>> result = [];
            AddIfErrors(result, "Name", Validate("Name", context.Name, context));
            AddIfErrors(result, "SelectedMeasurementUnit", Validate("SelectedMeasurementUnit", context.HasMeasurementUnit ? new object() : null, context));
            AddIfErrors(result, "SelectedAccountingGroup", Validate("SelectedAccountingGroup", context.HasAccountingGroup ? new object() : null, context));
            AddIfErrors(result, "SelectedSize", Validate("SelectedSize", context.HasSizeCategory ? new object() : null, context));
            return result;
        }

        public bool CanSave(ItemCanSaveContext context)
        {
            if (context.IsBusy) return false;
            if (string.IsNullOrWhiteSpace(context.Name)) return false;
            if (!context.HasMeasurementUnit) return false;
            if (!context.HasAccountingGroup) return false;
            if (context.RequiresSizeCategory && !context.HasSizeCategory) return false;
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

    public class ItemValidationContext
    {
        public string? Name { get; init; }
        public string? Reference { get; init; }
        public bool HasMeasurementUnit { get; init; }
        public bool HasAccountingGroup { get; init; }
        public bool RequiresSizeCategory { get; init; }
        public bool HasSizeCategory { get; init; }
    }

    public class ItemCanSaveContext
    {
        public bool IsBusy { get; init; }
        public string? Name { get; init; }
        public string? Reference { get; init; }
        public bool HasMeasurementUnit { get; init; }
        public bool HasAccountingGroup { get; init; }
        public bool RequiresSizeCategory { get; init; }
        public bool HasSizeCategory { get; init; }
        public bool HasChanges { get; init; }
        public bool HasErrors { get; init; }
    }
}
