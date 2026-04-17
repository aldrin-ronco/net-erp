using System.Collections.Generic;

namespace NetErp.Treasury.Masters.Validators
{
    /// <summary>
    /// Lógica de validación pura para Auxiliary Cash Drawer (Caja Auxiliar, <c>Parent != null</c>).
    /// </summary>
    public class AuxiliaryCashDrawerValidator
    {
        public IReadOnlyList<string> Validate(string propertyName, object? value, AuxiliaryCashDrawerValidationContext context)
        {
            return propertyName switch
            {
                nameof(AuxiliaryCashDrawerValidationContext.Name) => ValidateName(value as string),
                nameof(AuxiliaryCashDrawerValidationContext.ComputerName) => ValidateComputerName(value as string),
                nameof(AuxiliaryCashDrawerValidationContext.AutoTransferCashDrawerId) => ValidateAutoTransfer(context),
                _ => []
            };
        }

        public Dictionary<string, IReadOnlyList<string>> ValidateAll(AuxiliaryCashDrawerValidationContext context)
        {
            Dictionary<string, IReadOnlyList<string>> result = [];

            IReadOnlyList<string> nameErrors = ValidateName(context.Name);
            if (nameErrors.Count > 0) result[nameof(AuxiliaryCashDrawerValidationContext.Name)] = nameErrors;

            IReadOnlyList<string> computerErrors = ValidateComputerName(context.ComputerName);
            if (computerErrors.Count > 0) result[nameof(AuxiliaryCashDrawerValidationContext.ComputerName)] = computerErrors;

            IReadOnlyList<string> autoTransferErrors = ValidateAutoTransfer(context);
            if (autoTransferErrors.Count > 0) result[nameof(AuxiliaryCashDrawerValidationContext.AutoTransferCashDrawerId)] = autoTransferErrors;

            return result;
        }

        public bool CanSave(AuxiliaryCashDrawerValidationContext context, bool hasChanges, bool hasErrors)
        {
            if (hasErrors) return false;
            if (!hasChanges) return false;
            if (string.IsNullOrWhiteSpace(context.Name)) return false;
            if (string.IsNullOrWhiteSpace(context.ComputerName)) return false;
            if (context.AutoTransfer && context.AutoTransferCashDrawerId == 0) return false;
            return true;
        }

        private static IReadOnlyList<string> ValidateName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ["El nombre de la caja no puede estar vacío"];
            return [];
        }

        private static IReadOnlyList<string> ValidateComputerName(string? computerName)
        {
            if (string.IsNullOrWhiteSpace(computerName))
                return ["El nombre del equipo no puede estar vacío"];
            return [];
        }

        private static IReadOnlyList<string> ValidateAutoTransfer(AuxiliaryCashDrawerValidationContext context)
        {
            if (context.AutoTransfer && context.AutoTransferCashDrawerId == 0)
                return ["Debe seleccionar una caja para la transferencia automática"];
            return [];
        }
    }

    public class AuxiliaryCashDrawerValidationContext
    {
        public string Name { get; set; } = string.Empty;
        public string ComputerName { get; set; } = string.Empty;
        public bool AutoTransfer { get; set; }
        public int AutoTransferCashDrawerId { get; set; }
    }
}
