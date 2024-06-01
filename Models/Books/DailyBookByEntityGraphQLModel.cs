using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Models.Books
{
    public class DailyBookByEntityGraphQLModel
    {
        public string ShortName { get; set; } = string.Empty;
        public DateTime? DocumentDate { get; set; }
        public string DocumentNumber { get; set; } = string.Empty;
        public string FullCode { get; set; } = string.Empty;
        public string AccountingSourceName { get; set; } = string.Empty;
        public string IdentificationNumber { get; set; } = string.Empty;
        public string VerificationDigit { get; set; } = string.Empty;
        public string IdentificationNumberWithVerificationDigit => $"{this.IdentificationNumber}{(string.IsNullOrEmpty(this.VerificationDigit) ? "" : "-" + this.VerificationDigit)}";
        public string SearchName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string AccountingAccountName { get; set; } = string.Empty;
        public string AccountingAccountFullNme
        {
            get
            {
                return RecordType switch
                {
                    "B" => "",
                    "T" => "",
                    "H" => "",
                    _ => $"{this.Code} - {this.AccountingAccountName}"
                };
            }
        }

        public decimal Debit { get; set; } = 0;
        public decimal Credit { get; set; } = 0;
        public string Description { get; set; } = string.Empty;
        public string RecordDetail { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public BigInteger MasterId { get; set; } = 0;
        public string RecordType { get; set; } = string.Empty;

        public string DebitStringValue
        {
            get
            {
                return RecordType switch
                {
                    "B" => "",
                    "H" => "",
                    "N" => Debit.ToString(format: "#,##0.00;(#,##0.00)"),
                    _ => Debit.ToString(format: "#,##0.00;(#,##0.00)")
                };
            }
        }

        public string CreditStringValue
        {
            get
            {
                return RecordType switch
                {
                    "B" => "",
                    "H" => "",
                    "N" => Credit.ToString(format: "#,##0.00;(#,##0.00)"),
                    _ => Credit.ToString(format: "#,##0.00;(#,##0.00)")
                };
            }
        }
    }
    public class DailyBookDataContext
    {
        public List<AccountingPresentationGraphQLModel> AccountingPresentations { get; set; }
        public List<CostCenterGraphQLModel> CostCenters { get; set; }
        public List<AccountingSourceGraphQLModel> AccountingSources { get; set; }
    }
}
