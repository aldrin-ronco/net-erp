using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Books
{
    public class TestBalanceByEntityGraphQLModel
    {
        public string Nature { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int AccountingEntityId { get; set; } = 0;
        public string IdentificationNumber { get; set; } = string.Empty;
        public string VerificationDigit { get; set; } = string.Empty;
        public string IdentificationNumberWithVerificationDigit => $"{this.IdentificationNumber}{(string.IsNullOrEmpty(this.VerificationDigit) ? "" : "-" + this.VerificationDigit)}";
        public string SearchName { get; set; } = string.Empty;
        public decimal PreviousBalance { get; set; } = 0;
        public string PreviousBalanceStringValue => PreviousBalance.ToString(format: "#,##0.00;(#,##0.00)");
        public decimal Debit { get; set; } = 0;
        public string DebitStringValue => Debit.ToString(format: "#,##0.00;(#,##0.00)");
        public decimal Credit { get; set; } = 0;
        public string CreditStringValue => Credit.ToString(format: "#,##0.00;(#,##0.00)");
        public decimal NewBalance { get; set; } = 0;
        public string NewBalanceStringValue => NewBalance.ToString(format: "#,##0.00;(#,##0.00)");
        public bool IsNegativePreviousBalance => PreviousBalance < 0;
        public bool IsNegativeDebit => Debit < 0;
        public bool IsNegativeCredit => Credit < 0;
        public bool IsNegativeNewBalance => NewBalance < 0;
        public int Level { get; set; } = 0;
    }
    public class TestBalanceByEntityDataContext
    {
        public List<AccountingPresentationGraphQLModel> AccountingPresentations { get; set; }
        public List<CostCenterGraphQLModel> CostCenters { get; set; }
        public List<AccountingSourceGraphQLModel> AccountingSources { get; set; }
        public List<AccountingAccountGraphQLModel> AccountingAccounts { get; set; }
    }
}
