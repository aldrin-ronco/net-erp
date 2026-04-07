using NetErp.Global.CostCenters.Shared;
using System.Collections.Generic;

namespace NetErp.Global.CostCenters.Validators
{
    /// <summary>
    /// Lógica de validación pura para CostCenter.
    /// </summary>
    public class CostCenterValidator
    {
        public IReadOnlyList<string> Validate(string propertyName, string? value, CostCenterValidationContext context)
        {
            return propertyName switch
            {
                "Name" when string.IsNullOrWhiteSpace(value)
                    => ["El nombre no puede estar vacío"],
                "ShortName" when string.IsNullOrWhiteSpace(value)
                    => ["El nombre corto no puede estar vacío"],
                "PrimaryPhone" => PhoneValidationRules.ValidateLandline(value),
                "SecondaryPhone" => PhoneValidationRules.ValidateLandline(value),
                "PrimaryCellPhone" => PhoneValidationRules.ValidateCellPhone(value),
                "SecondaryCellPhone" => PhoneValidationRules.ValidateCellPhone(value),
                _ => []
            };
        }

        public Dictionary<string, IReadOnlyList<string>> ValidateAll(CostCenterValidationContext context)
        {
            Dictionary<string, IReadOnlyList<string>> result = [];
            AddIfErrors(result, "Name", Validate("Name", context.Name, context));
            AddIfErrors(result, "ShortName", Validate("ShortName", context.ShortName, context));
            AddIfErrors(result, "PrimaryPhone", Validate("PrimaryPhone", context.PrimaryPhone, context));
            AddIfErrors(result, "SecondaryPhone", Validate("SecondaryPhone", context.SecondaryPhone, context));
            AddIfErrors(result, "PrimaryCellPhone", Validate("PrimaryCellPhone", context.PrimaryCellPhone, context));
            AddIfErrors(result, "SecondaryCellPhone", Validate("SecondaryCellPhone", context.SecondaryCellPhone, context));
            return result;
        }

        public bool CanSave(CostCenterCanSaveContext context)
        {
            if (context.IsBusy) return false;
            if (string.IsNullOrWhiteSpace(context.Name)) return false;
            if (string.IsNullOrWhiteSpace(context.ShortName)) return false;
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

    public class CostCenterValidationContext
    {
        public string? Name { get; init; }
        public string? ShortName { get; init; }
        public string? PrimaryPhone { get; init; }
        public string? SecondaryPhone { get; init; }
        public string? PrimaryCellPhone { get; init; }
        public string? SecondaryCellPhone { get; init; }
    }

    public class CostCenterCanSaveContext
    {
        public bool IsBusy { get; init; }
        public string? Name { get; init; }
        public string? ShortName { get; init; }
        public bool HasChanges { get; init; }
        public bool HasErrors { get; init; }
    }
}
