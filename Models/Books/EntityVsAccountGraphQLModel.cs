using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Books
{
    public class EntityVsAccountGraphQLModel
    {
        public string ShortName { get; set; } = string.Empty;
        public DateTime? DocumentDate { get; set; }
        public string FullCode { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string Info { get; set; } = string.Empty;
        public string AccountingAccountCode { get; set; } = string.Empty;
        public string AccountingAccountName { get; set; } = string.Empty;
        public string IdentificationNumber { get; set; } = string.Empty;
        public string VerificationDigit { get; set; } = string.Empty;
        public string IdentificationNumberWithVerificationDigit => $"{this.IdentificationNumber}{(string.IsNullOrEmpty(this.VerificationDigit) ? "" : "-" + this.VerificationDigit)}";
        public string RecordDetail { get; set; } = string.Empty;
        public string RecordType { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string SearchName { get; set; } = string.Empty;
        public decimal Debit { get; set; } = 0;
        public string DebitStringValue
        {
            get
            {
                return RecordType switch
                {
                    "N" => Debit.ToString(format: "#,##0.00;(#,##0.00)"),
                    "T" => Debit != 0 ? Debit.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    "S" => Debit != 0 ? Debit.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    "B" => "",
                    "E" => "",
                    "A" => "",
                    _ => Debit.ToString(format: "#,##0.00;(#,##0.00)"),
                };
            }
        }

        public decimal Credit { get; set; } = 0;
        public string CreditStringValue
        {
            get
            {
                return RecordType switch
                {
                    "N" => Credit.ToString(format: "#,##0.00;(#,##0.00)"),
                    "T" => Credit != 0 ? Credit.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    "S" => Credit != 0 ? Credit.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    "B" => "",
                    "E" => "",
                    "A" => "",
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
                    "N" => Balance.ToString(format: "#,##0.00;(#,##0.00)"),
                    "T" => Balance != 0 ? Balance.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    "S" => Balance != 0 ? Balance.ToString(format: "#,##0.00;(#,##0.00)") : "",
                    "B" => "",
                    "E" => "",
                    "A" => "",
                    _ => Balance.ToString(format: "#,##0.00;(#,##0.00)")
                };
            }
        }

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
    public class EntityVsAccountDataContext
    {
        public List<AccountingPresentationGraphQLModel> AccountingPresentations { get; set; }
        public List<CostCenterGraphQLModel> CostCenters { get; set; }
        public List<AccountingSourceGraphQLModel> AccountingSources { get; set; }
        public List<AccountingAccountGraphQLModel> AccountingAccounts { get; set; }
    }
}

