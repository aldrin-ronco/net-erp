using System.Collections.Generic;

namespace NetErp.Global.CostCenters.Validators
{
    /// <summary>
    /// Lógica de validación pura para Company.
    /// Company es Update-only: solo se cambia la AccountingEntity asociada.
    /// </summary>
    public class CompanyValidator
    {
        public IReadOnlyList<string> Validate(string propertyName, int value, CompanyValidationContext context)
        {
            return propertyName switch
            {
                "AccountingEntityCompanyId" when value <= 0
                    => ["Debe seleccionar una entidad contable"],
                _ => []
            };
        }

        public Dictionary<string, IReadOnlyList<string>> ValidateAll(CompanyValidationContext context)
        {
            Dictionary<string, IReadOnlyList<string>> result = [];
            IReadOnlyList<string> idErrors = Validate("AccountingEntityCompanyId", context.AccountingEntityCompanyId, context);
            if (idErrors.Count > 0) result["AccountingEntityCompanyId"] = idErrors;
            return result;
        }

        public bool CanSave(CompanyCanSaveContext context)
        {
            if (context.IsBusy) return false;
            if (context.AccountingEntityCompanyId <= 0) return false;
            if (!context.HasChanges) return false;
            if (context.HasErrors) return false;
            return true;
        }
    }

    public class CompanyValidationContext
    {
        public int AccountingEntityCompanyId { get; init; }
    }

    public class CompanyCanSaveContext
    {
        public bool IsBusy { get; init; }
        public int AccountingEntityCompanyId { get; init; }
        public bool HasChanges { get; init; }
        public bool HasErrors { get; init; }
    }
}
