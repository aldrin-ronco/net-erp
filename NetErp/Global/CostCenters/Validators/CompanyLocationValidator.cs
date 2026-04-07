using System.Collections.Generic;

namespace NetErp.Global.CostCenters.Validators
{
    /// <summary>
    /// Lógica de validación pura para CompanyLocation.
    /// </summary>
    public class CompanyLocationValidator
    {
        public IReadOnlyList<string> Validate(string propertyName, string? value, CompanyLocationValidationContext context)
        {
            return propertyName switch
            {
                "Name" when string.IsNullOrWhiteSpace(value)
                    => ["El nombre no puede estar vacío"],
                _ => []
            };
        }

        public Dictionary<string, IReadOnlyList<string>> ValidateAll(CompanyLocationValidationContext context)
        {
            Dictionary<string, IReadOnlyList<string>> result = [];
            IReadOnlyList<string> nameErrors = Validate("Name", context.Name, context);
            if (nameErrors.Count > 0) result["Name"] = nameErrors;
            return result;
        }

        public bool CanSave(CompanyLocationCanSaveContext context)
        {
            if (context.IsBusy) return false;
            if (string.IsNullOrWhiteSpace(context.Name)) return false;
            if (!context.HasChanges) return false;
            if (context.HasErrors) return false;
            return true;
        }
    }

    public class CompanyLocationValidationContext
    {
        public string? Name { get; init; }
    }

    public class CompanyLocationCanSaveContext
    {
        public bool IsBusy { get; init; }
        public string? Name { get; init; }
        public bool HasChanges { get; init; }
        public bool HasErrors { get; init; }
    }
}
