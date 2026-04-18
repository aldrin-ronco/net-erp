using System.Collections.Generic;

namespace NetErp.Treasury.Masters.Validators
{
    /// <summary>
    /// Lógica de validación pura para Bank.
    /// Sin dependencias de WPF, Caliburn.Micro ni INotifyDataErrorInfo.
    /// </summary>
    public class BankValidator
    {
        /// <summary>
        /// Valida una propiedad individual. Retorna una lista vacía si no hay errores.
        /// </summary>
        public IReadOnlyList<string> Validate(string propertyName, object? value, BankValidationContext context)
        {
            string text = value?.ToString() ?? string.Empty;

            return propertyName switch
            {
                nameof(BankValidationContext.Code) => ValidateCode(text),
                nameof(BankValidationContext.PaymentMethodPrefix) => ValidatePaymentMethodPrefix(text),
                nameof(BankValidationContext.AccountingEntityName) => ValidateAccountingEntityName(text, context.AccountingEntityId),
                _ => []
            };
        }

        /// <summary>
        /// Valida todas las propiedades y retorna diccionario propiedad → errores.
        /// </summary>
        public Dictionary<string, IReadOnlyList<string>> ValidateAll(BankValidationContext context)
        {
            Dictionary<string, IReadOnlyList<string>> result = [];

            IReadOnlyList<string> codeErrors = ValidateCode(context.Code);
            if (codeErrors.Count > 0) result[nameof(BankValidationContext.Code)] = codeErrors;

            IReadOnlyList<string> prefixErrors = ValidatePaymentMethodPrefix(context.PaymentMethodPrefix);
            if (prefixErrors.Count > 0) result[nameof(BankValidationContext.PaymentMethodPrefix)] = prefixErrors;

            IReadOnlyList<string> entityErrors = ValidateAccountingEntityName(context.AccountingEntityName, context.AccountingEntityId);
            if (entityErrors.Count > 0) result[nameof(BankValidationContext.AccountingEntityName)] = entityErrors;

            return result;
        }

        public bool CanSave(BankValidationContext context, bool hasChanges, bool hasErrors)
        {
            if (hasErrors) return false;
            if (!hasChanges) return false;
            if (context.AccountingEntityId == 0) return false;
            if (string.IsNullOrWhiteSpace(context.AccountingEntityName)) return false;
            if (string.IsNullOrWhiteSpace(context.Code) || context.Code.Length != 3) return false;
            if (string.IsNullOrWhiteSpace(context.PaymentMethodPrefix) || context.PaymentMethodPrefix.Length != 1) return false;
            return true;
        }

        private static IReadOnlyList<string> ValidateCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return ["El código es obligatorio"];
            if (code.Length != 3) return ["El código debe tener exactamente 3 dígitos"];
            return [];
        }

        private static IReadOnlyList<string> ValidatePaymentMethodPrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return ["El prefijo de método de pago es obligatorio"];
            if (prefix.Length != 1) return ["El prefijo debe ser exactamente una letra"];
            return [];
        }

        private static IReadOnlyList<string> ValidateAccountingEntityName(string name, int id)
        {
            if (id == 0 || string.IsNullOrWhiteSpace(name))
                return ["Debe seleccionar una entidad contable"];
            return [];
        }
    }

    public class BankValidationContext
    {
        public string Code { get; set; } = string.Empty;
        public string PaymentMethodPrefix { get; set; } = string.Empty;
        public string AccountingEntityName { get; set; } = string.Empty;
        public int AccountingEntityId { get; set; }
    }
}
