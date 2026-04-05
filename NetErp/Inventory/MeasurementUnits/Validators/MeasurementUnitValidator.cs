using System.Collections.Generic;

namespace NetErp.Inventory.MeasurementUnits.Validators
{
    /// <summary>
    /// Lógica de validación pura para MeasurementUnit.
    /// Sin dependencias de WPF, Caliburn.Micro ni INotifyDataErrorInfo.
    /// El ViewModel delega aquí las decisiones de validación.
    /// </summary>
    public class MeasurementUnitValidator
    {
        /// <summary>
        /// Valida una propiedad individual. Retorna lista vacía si no hay errores.
        /// </summary>
        public IReadOnlyList<string> Validate(string propertyName, string? value)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty;

            return propertyName switch
            {
                "Name" when string.IsNullOrEmpty(value) => ["El nombre no puede estar vacío"],
                "Abbreviation" when string.IsNullOrEmpty(value) => ["La abreviación no puede estar vacía"],
                "Type" when string.IsNullOrEmpty(value) => ["El tipo no puede estar vacío"],
                "DianCode" when string.IsNullOrEmpty(value) => ["El código DIAN no puede estar vacío"],
                _ => []
            };
        }

        /// <summary>
        /// Valida todas las propiedades. Retorna diccionario propiedad → errores (solo las que tienen errores).
        /// </summary>
        public Dictionary<string, IReadOnlyList<string>> ValidateAll(
            string name, string abbreviation, string type, string dianCode)
        {
            var result = new Dictionary<string, IReadOnlyList<string>>();
            AddIfErrors(result, "Name", Validate("Name", name));
            AddIfErrors(result, "Abbreviation", Validate("Abbreviation", abbreviation));
            AddIfErrors(result, "Type", Validate("Type", type));
            AddIfErrors(result, "DianCode", Validate("DianCode", dianCode));
            return result;
        }

        /// <summary>
        /// Evalúa si el formulario es guardable.
        /// </summary>
        public bool CanSave(string? name, string? abbreviation, string? type, string? dianCode,
                            bool hasChanges, bool hasErrors)
        {
            return !hasErrors && hasChanges
                   && !string.IsNullOrEmpty(name?.Trim())
                   && !string.IsNullOrEmpty(abbreviation?.Trim())
                   && !string.IsNullOrEmpty(type?.Trim())
                   && !string.IsNullOrEmpty(dianCode?.Trim());
        }

        private static void AddIfErrors(Dictionary<string, IReadOnlyList<string>> dict,
                                         string key, IReadOnlyList<string> errors)
        {
            if (errors.Count > 0) dict[key] = errors;
        }
    }
}
