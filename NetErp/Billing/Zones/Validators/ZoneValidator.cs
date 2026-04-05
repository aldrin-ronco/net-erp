using System.Collections.Generic;

namespace NetErp.Billing.Zones.Validators
{
    /// <summary>
    /// Lógica de validación pura para Zone.
    /// Sin dependencias de WPF, Caliburn.Micro ni INotifyDataErrorInfo.
    /// </summary>
    public class ZoneValidator
    {
        public IReadOnlyList<string> Validate(string propertyName, string? value)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty;

            return propertyName switch
            {
                "Name" when string.IsNullOrEmpty(value) => ["El nombre de la zona no puede estar vacío"],
                _ => []
            };
        }

        public Dictionary<string, IReadOnlyList<string>> ValidateAll(string name)
        {
            var result = new Dictionary<string, IReadOnlyList<string>>();
            var nameErrors = Validate("Name", name);
            if (nameErrors.Count > 0) result["Name"] = nameErrors;
            return result;
        }

        public bool CanSave(string? name, bool hasChanges, bool hasErrors)
        {
            return !hasErrors && hasChanges
                   && !string.IsNullOrEmpty(name?.Trim());
        }
    }
}
