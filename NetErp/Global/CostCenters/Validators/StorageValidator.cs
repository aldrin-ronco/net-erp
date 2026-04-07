using System.Collections.Generic;

namespace NetErp.Global.CostCenters.Validators
{
    /// <summary>
    /// Lógica de validación pura para Storage.
    /// </summary>
    public class StorageValidator
    {
        public IReadOnlyList<string> Validate(string propertyName, string? value, StorageValidationContext context)
        {
            return propertyName switch
            {
                "Name" when string.IsNullOrWhiteSpace(value)
                    => ["El nombre de la bodega no puede estar vacío"],
                _ => []
            };
        }

        public Dictionary<string, IReadOnlyList<string>> ValidateAll(StorageValidationContext context)
        {
            Dictionary<string, IReadOnlyList<string>> result = [];
            IReadOnlyList<string> nameErrors = Validate("Name", context.Name, context);
            if (nameErrors.Count > 0) result["Name"] = nameErrors;
            return result;
        }

        public bool CanSave(StorageCanSaveContext context)
        {
            if (context.IsBusy) return false;
            if (string.IsNullOrWhiteSpace(context.Name)) return false;
            if (context.SelectedCityId <= 0) return false;
            if (!context.HasChanges) return false;
            if (context.HasErrors) return false;
            return true;
        }
    }

    public class StorageValidationContext
    {
        public string? Name { get; init; }
    }

    public class StorageCanSaveContext
    {
        public bool IsBusy { get; init; }
        public string? Name { get; init; }
        public int SelectedCityId { get; init; }
        public bool HasChanges { get; init; }
        public bool HasErrors { get; init; }
    }
}
