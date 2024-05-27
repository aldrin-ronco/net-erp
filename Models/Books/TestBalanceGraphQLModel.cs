using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Books
{
    public class TestBalanceGraphQLModel
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public decimal PreviousBalance { get; set; } = 0;
        public string PreviousBalanceStringValue => PreviousBalance.ToString(format: "#,##0.00;(#,##0.00)");

        public decimal Debit { get; set; } = 0;
        public string DebitStringValue => Debit.ToString(format: "#,##0.00;(#,##0.00)");

        public decimal Credit { get; set; } = 0;
        public string CreditStringValue => Credit.ToString(format: "#,##0.00;(#,##0.00)");

        public decimal NewBalance { get; set; } = 0;
        public string NewBalanceStringValue => NewBalance.ToString(format: "#,##0.00;(#,##0.00)");

        public int Level { get; set; } = 0;

        public bool IsNegativePreviousBalance => PreviousBalance < 0;

        public bool IsNegativeDebit => Debit < 0;

        public bool IsNegativeCredit => Credit < 0;

        public bool IsNegativeNewBalance => NewBalance < 0;
    }

    public class TestBalanceDataContext
    {
        public List<AccountingPresentationGraphQLModel> AccountingPresentations { get; set; }
        public List<CostCenterGraphQLModel> CostCenters { get; set; }
    }
}
