using System.Collections.Generic;

namespace NetErp.Treasury.Masters.Validators
{
    /// <summary>
    /// Lógica de validación pura para BankAccount.
    /// </summary>
    public class BankAccountValidator
    {
        public IReadOnlyList<string> Validate(string propertyName, object? value, BankAccountValidationContext context)
        {
            return propertyName switch
            {
                nameof(BankAccountValidationContext.Number) => ValidateNumber(value as string),
                nameof(BankAccountValidationContext.AccountingAccountId) => ValidateAccountingAccount(context),
                _ => []
            };
        }

        public Dictionary<string, IReadOnlyList<string>> ValidateAll(BankAccountValidationContext context)
        {
            Dictionary<string, IReadOnlyList<string>> result = [];

            IReadOnlyList<string> numberErrors = ValidateNumber(context.Number);
            if (numberErrors.Count > 0) result[nameof(BankAccountValidationContext.Number)] = numberErrors;

            IReadOnlyList<string> accountErrors = ValidateAccountingAccount(context);
            if (accountErrors.Count > 0) result[nameof(BankAccountValidationContext.AccountingAccountId)] = accountErrors;

            return result;
        }

        public bool CanSave(BankAccountValidationContext context, bool hasChanges, bool hasErrors)
        {
            if (hasErrors) return false;
            if (!hasChanges) return false;
            if (string.IsNullOrWhiteSpace(context.Number)) return false;
            if (context.AccountingAccountSelectExisting && context.AccountingAccountId == 0) return false;
            return true;
        }

        private static IReadOnlyList<string> ValidateNumber(string? number)
        {
            if (string.IsNullOrWhiteSpace(number))
                return ["El número de cuenta no puede estar vacío"];
            return [];
        }

        private static IReadOnlyList<string> ValidateAccountingAccount(BankAccountValidationContext context)
        {
            if (context.AccountingAccountSelectExisting && context.AccountingAccountId == 0)
                return ["Debe seleccionar una cuenta contable"];
            return [];
        }
    }

    public class BankAccountValidationContext
    {
        public string Number { get; set; } = string.Empty;
        public int AccountingAccountId { get; set; }
        public bool AccountingAccountSelectExisting { get; set; }
    }
}
