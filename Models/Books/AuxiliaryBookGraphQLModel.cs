using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Books
{
    public class AuxiliaryBookGraphQLModel
    {
        public string ShortName { get; set; } = string.Empty;
        public DateTime? DocumentDate { get; set; }
        public string FullCode { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string IdentificationNumber { get; set; } = string.Empty;
        public string VerificationDigit { get; set; } = string.Empty;
        public string RecordDetail { get; set; } = string.Empty;
        public string AccountingAccountCode { get; set; } = string.Empty;
        public string AccountingAccountName { get; set; } = string.Empty;
        public decimal Debit { get; set; } = 0;
        public string DebitStringValue
        {
            get
            {
                return RecordType switch
                {
                    "H" => Debit != 0 ? Debit.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    "B" => "",
                    "T" => Debit != 0 ? Debit.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    _ => Debit.ToString(format: "#,##0.00;(#,##0.00)"),
                };
            }
        }
        public string IdentificationNumberWithVerificationDigit => $"{this.IdentificationNumber}{(string.IsNullOrEmpty(this.VerificationDigit) ? "" : "-" + this.VerificationDigit)}";

        public decimal Credit { get; set; } = 0;
        public string CreditStringValue
        {
            get
            {
                return RecordType switch
                {
                    "H" => Credit != 0 ? Credit.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    "B" => "",
                    "T" => Credit != 0 ? Credit.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    _ => Credit.ToString(format: "#,##0.00;(#,##0.00)")
                };
            }
        }

        public decimal Balance { get; set; } = 0;
        public string BalanceStringValue
        {
            get
            {
                return RecordType switch
                {
                    "T" => "",
                    "B" => "",
                    "H" => "",
                    "S" => "",
                    _ => Balance.ToString(format: "#,##0.00;(#,##0.00)")
                };
            }
        }

        public string RecordType { get; set; } = string.Empty;

        // Control de negativos para colorear en rojo
        public bool IsNegativeDebit
        {
            get { return Debit < 0; }
        }

        public bool IsNegativeCredit
        {
            get { return Credit < 0; }
        }

        public bool IsNegativeBalance
        {
            get { return Balance < 0; }
        }
    }
    public class AuxiliaryBookDataContext
    {
        public List<AccountingPresentationGraphQLModel> AccountingPresentations { get; set; }
        public List<CostCenterGraphQLModel> CostCenters { get; set; }
        public List<AccountingSourceGraphQLModel> AccountingSources { get; set; }
        public List<AccountingAccountGraphQLModel> AccountingAccounts { get; set; }
    }
}
