using System.Collections.Generic;

namespace NetErp.Inventory.CatalogItems.Validators
{
    /// <summary>
    /// Lógica de validación pura para ItemType.
    /// </summary>
    public class ItemTypeValidator
    {
        public IReadOnlyList<string> Validate(string propertyName, object? value, ItemTypeValidationContext context)
        {
            return propertyName switch
            {
                "Name" when string.IsNullOrWhiteSpace(value as string)
                    => ["El nombre del tipo de item no puede estar vacío"],
                "PrefixChar" when string.IsNullOrWhiteSpace(value as string)
                    => ["El nombre corto del tipo de item no puede estar vacío"],
                "PrefixChar" when (value as string)?.Length != 1
                    => ["El nombre corto debe ser exactamente un caracter"],
                "PrefixChar" when !IsUppercaseLetter((value as string)!)
                    => ["El nombre corto debe ser una letra mayúscula (A-Z)"],
                "DefaultMeasurementUnitId" when (value is int id1 ? id1 : 0) <= 0
                    => ["Debe seleccionar una unidad de medida"],
                "DefaultAccountingGroupId" when (value is int id2 ? id2 : 0) <= 0
                    => ["Debe seleccionar un grupo contable"],
                _ => []
            };
        }

        private static bool IsUppercaseLetter(string value) =>
            value.Length == 1 && value[0] >= 'A' && value[0] <= 'Z';

        public Dictionary<string, IReadOnlyList<string>> ValidateAll(ItemTypeValidationContext context)
        {
            Dictionary<string, IReadOnlyList<string>> result = [];
            AddIfErrors(result, "Name", Validate("Name", context.Name, context));
            AddIfErrors(result, "PrefixChar", Validate("PrefixChar", context.PrefixChar, context));
            AddIfErrors(result, "DefaultMeasurementUnitId", Validate("DefaultMeasurementUnitId", context.DefaultMeasurementUnitId, context));
            AddIfErrors(result, "DefaultAccountingGroupId", Validate("DefaultAccountingGroupId", context.DefaultAccountingGroupId, context));
            return result;
        }

        public bool CanSave(ItemTypeCanSaveContext context)
        {
            if (context.IsBusy) return false;
            if (string.IsNullOrWhiteSpace(context.Name)) return false;
            if (string.IsNullOrWhiteSpace(context.PrefixChar) || context.PrefixChar.Length != 1) return false;
            char prefix = context.PrefixChar[0];
            if (prefix < 'A' || prefix > 'Z') return false;
            if (context.DefaultMeasurementUnitId <= 0) return false;
            if (context.DefaultAccountingGroupId <= 0) return false;
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

    public class ItemTypeValidationContext
    {
        public string? Name { get; init; }
        public string? PrefixChar { get; init; }
        public int DefaultMeasurementUnitId { get; init; }
        public int DefaultAccountingGroupId { get; init; }
    }

    public class ItemTypeCanSaveContext
    {
        public bool IsBusy { get; init; }
        public string? Name { get; init; }
        public string? PrefixChar { get; init; }
        public int DefaultMeasurementUnitId { get; init; }
        public int DefaultAccountingGroupId { get; init; }
        public bool HasChanges { get; init; }
        public bool HasErrors { get; init; }
    }
}
