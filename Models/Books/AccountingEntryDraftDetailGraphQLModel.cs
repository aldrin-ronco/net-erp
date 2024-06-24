using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Models.Books
{
    public class AccountingEntryDraftDetailGraphQLModel
    {
        public AccountingAccountGraphQLModel AccountingAccount { get; set; }
        public AccountingEntityGraphQLModel AccountingEntity { get; set; }
        public CostCenterGraphQLModel CostCenter { get; set; }
        public BigInteger Id { get; set; } = 0;
        public string RecordDetail { get; set; } = string.Empty;
        public decimal Debit { get; set; } = 0;
        public decimal Credit { get; set; } = 0;
        public decimal Base { get; set; } = 0;
        public AccountingEntryTotals Totals { get; set; }
    }

    public class AccountingEntryDraftDetailDTO : AccountingEntryDraftDetailGraphQLModel
    {
        public bool IsChecked { get; set; } = false;

    }

}
