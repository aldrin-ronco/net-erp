using System.Collections.Generic;

namespace NetErp.Treasury.Masters.Validators
{
    /// <summary>
    /// Lógica de validación pura para Major Cash Drawer (Caja Mayor).
    /// </summary>
    public class MajorCashDrawerValidator
    {
        public IReadOnlyList<string> Validate(string propertyName, object? value, MajorCashDrawerValidationContext context)
        {
            return propertyName switch
            {
                nameof(MajorCashDrawerValidationContext.Name) => ValidateName(value as string),
                nameof(MajorCashDrawerValidationContext.AutoTransferCashDrawerId) => ValidateAutoTransfer(context),
                _ => []
            };
        }

        public Dictionary<string, IReadOnlyList<string>> ValidateAll(MajorCashDrawerValidationContext context)
        {
            Dictionary<string, IReadOnlyList<string>> result = [];

            IReadOnlyList<string> nameErrors = ValidateName(context.Name);
            if (nameErrors.Count > 0) result[nameof(MajorCashDrawerValidationContext.Name)] = nameErrors;

            IReadOnlyList<string> autoTransferErrors = ValidateAutoTransfer(context);
            if (autoTransferErrors.Count > 0) result[nameof(MajorCashDrawerValidationContext.AutoTransferCashDrawerId)] = autoTransferErrors;

            return result;
        }

        public bool CanSave(MajorCashDrawerValidationContext context, bool hasChanges, bool hasErrors)
        {
            if (hasErrors) return false;
            if (!hasChanges) return false;
            if (string.IsNullOrWhiteSpace(context.Name)) return false;
            if (context.AutoTransfer && context.AutoTransferCashDrawerId == 0) return false;
            return true;
        }

        private static IReadOnlyList<string> ValidateName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ["El nombre de la caja no puede estar vacío"];
            return [];
        }

        private static IReadOnlyList<string> ValidateAutoTransfer(MajorCashDrawerValidationContext context)
        {
            if (context.AutoTransfer && context.AutoTransferCashDrawerId == 0)
                return ["Debe seleccionar una caja para la transferencia automática"];
            return [];
        }
    }

    public class MajorCashDrawerValidationContext
    {
        public string Name { get; set; } = string.Empty;
        public bool AutoTransfer { get; set; }
        public int AutoTransferCashDrawerId { get; set; }
    }
}
