using Models.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Books
{
    public class AccountingGroupGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public AccountingAccountGraphQLModel AccountIncome { get; set; } = new();
        public AccountingAccountGraphQLModel AccountCost { get; set; } = new();
        public AccountingAccountGraphQLModel AccountInventory { get; set; } = new();
        public AccountingAccountGraphQLModel AccountIncomeReverse { get; set; } = new();
        public AccountingAccountGraphQLModel AccountAiuAdministration { get; set; } = new();
        public AccountingAccountGraphQLModel AccountAiuUnforenseen { get; set; } = new();
        public AccountingAccountGraphQLModel AccountAiuUtility { get; set; } = new();
        public bool AllowAiu { get; set; }
        public TaxGraphQLModel BuyTax1 { get; set; } = new();
        public TaxGraphQLModel BuyTax2 { get; set; } = new();
        public TaxGraphQLModel SellTax1 { get; set; } = new();
        public TaxGraphQLModel SellTax2 { get; set; } = new();
    }

    public class AccountingGroupDTO : AccountingGroupGraphQLModel
    {

    }

    public class AccountingGroupCreateMessage
    {
        public UpsertResponseType<AccountingGroupGraphQLModel> CreatedAccountingGroup { get; set; } = new();
    }

    public class AccountingGroupUpdateMessage
    {
        public UpsertResponseType<AccountingGroupGraphQLModel> UpdatedAccountingGroup { get; set; } = new();
    }

    public class AccountingGroupDeleteMessage
    {
        public DeleteResponseType DeletedAccountingGroup { get; set; } = new();
    }
}
