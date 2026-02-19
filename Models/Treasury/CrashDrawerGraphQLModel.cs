using Models.Books;
using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Treasury
{
    public class CashDrawerGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool CashReviewRequired { get; set; }
        public bool AutoAdjustBalance { get; set; }
        public bool AutoTransfer { get; set; }
        public bool IsPettyCash { get; set; }
        public CashDrawerGraphQLModel? AutoTransferCashDrawer { get; set; }
        public CostCenterGraphQLModel CostCenter { get; set; } = new();
        public AccountingAccountGraphQLModel CashAccountingAccount { get; set; } = new();
        public AccountingAccountGraphQLModel CheckAccountingAccount { get; set; } = new();
        public AccountingAccountGraphQLModel CardAccountingAccount { get; set; } = new();
        public CashDrawerGraphQLModel? Parent { get; set; }
        public string ComputerName { get; set; } = string.Empty;
        public DateTime InsertedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public override string ToString()
        {
            return Name;
        }
    }

    public class TreasuryCashDrawerCreateMessage
    {
        public UpsertResponseType<CashDrawerGraphQLModel> CreatedCashDrawer { get; set; } = new();
    }

    public class TreasuryCashDrawerUpdateMessage
    {
        public UpsertResponseType<CashDrawerGraphQLModel> UpdatedCashDrawer { get; set; } = new();
    }

    public class TreasuryCashDrawerDeleteMessage
    {
        public DeleteResponseType DeletedCashDrawer { get; set; } = new();
    }
}
