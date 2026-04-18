using System.Collections.Generic;

namespace NetErp.Treasury.Masters.Validators
{
    /// <summary>
    /// Lógica de validación pura para Franchise. Sin dependencias de WPF o Caliburn.
    /// </summary>
    public class FranchiseValidator
    {
        public IReadOnlyList<string> Validate(string propertyName, object? value, FranchiseValidationContext context)
        {
            return propertyName switch
            {
                nameof(FranchiseValidationContext.Name) => ValidateName(value as string),
                nameof(FranchiseValidationContext.CommissionAccountingAccountId) => ValidateCommissionAccount(context.CommissionAccountingAccountId),
                nameof(FranchiseValidationContext.BankAccountId) => ValidateBankAccount(context.BankAccountId),
                _ => []
            };
        }

        public Dictionary<string, IReadOnlyList<string>> ValidateAll(FranchiseValidationContext context)
        {
            Dictionary<string, IReadOnlyList<string>> result = [];

            IReadOnlyList<string> nameErrors = ValidateName(context.Name);
            if (nameErrors.Count > 0) result[nameof(FranchiseValidationContext.Name)] = nameErrors;

            IReadOnlyList<string> commissionErrors = ValidateCommissionAccount(context.CommissionAccountingAccountId);
            if (commissionErrors.Count > 0) result[nameof(FranchiseValidationContext.CommissionAccountingAccountId)] = commissionErrors;

            IReadOnlyList<string> bankErrors = ValidateBankAccount(context.BankAccountId);
            if (bankErrors.Count > 0) result[nameof(FranchiseValidationContext.BankAccountId)] = bankErrors;

            return result;
        }

        public bool CanSave(FranchiseValidationContext context, bool hasChanges, bool hasErrors)
        {
            if (hasErrors) return false;
            if (!hasChanges) return false;
            if (string.IsNullOrWhiteSpace(context.Name)) return false;
            if (context.CommissionAccountingAccountId == 0) return false;
            if (context.BankAccountId == 0) return false;
            return true;
        }

        private static IReadOnlyList<string> ValidateName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ["El nombre de la franquicia no puede estar vacío"];
            return [];
        }

        private static IReadOnlyList<string> ValidateCommissionAccount(int id)
        {
            if (id == 0) return ["Debe seleccionar una cuenta contable de comisión"];
            return [];
        }

        private static IReadOnlyList<string> ValidateBankAccount(int id)
        {
            if (id == 0) return ["Debe seleccionar una cuenta bancaria"];
            return [];
        }
    }

    public class FranchiseValidationContext
    {
        public string Name { get; set; } = string.Empty;
        public int CommissionAccountingAccountId { get; set; }
        public int BankAccountId { get; set; }
    }
}
