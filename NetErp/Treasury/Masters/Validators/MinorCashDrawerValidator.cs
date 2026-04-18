using System.Collections.Generic;

namespace NetErp.Treasury.Masters.Validators
{
    /// <summary>
    /// Lógica de validación pura para Minor Cash Drawer (Caja Menor).
    /// </summary>
    public class MinorCashDrawerValidator
    {
        public IReadOnlyList<string> Validate(string propertyName, object? value, MinorCashDrawerValidationContext context)
        {
            return propertyName switch
            {
                nameof(MinorCashDrawerValidationContext.Name) => ValidateName(value as string),
                _ => []
            };
        }

        public Dictionary<string, IReadOnlyList<string>> ValidateAll(MinorCashDrawerValidationContext context)
        {
            Dictionary<string, IReadOnlyList<string>> result = [];

            IReadOnlyList<string> nameErrors = ValidateName(context.Name);
            if (nameErrors.Count > 0) result[nameof(MinorCashDrawerValidationContext.Name)] = nameErrors;

            return result;
        }

        public bool CanSave(MinorCashDrawerValidationContext context, bool hasChanges, bool hasErrors)
        {
            if (hasErrors) return false;
            if (!hasChanges) return false;
            if (string.IsNullOrWhiteSpace(context.Name)) return false;
            return true;
        }

        private static IReadOnlyList<string> ValidateName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ["El nombre de la caja no puede estar vacío"];
            return [];
        }
    }

    public class MinorCashDrawerValidationContext
    {
        public string Name { get; set; } = string.Empty;
    }
}
