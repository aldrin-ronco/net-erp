using Models.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Books
{
    public class AccountingGroupGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public AccountingAccountGraphQLModel AccountIncome { get; set; }
        public AccountingAccountGraphQLModel AccountCost { get; set; }
        public AccountingAccountGraphQLModel AccountInventory { get; set; }
        public AccountingAccountGraphQLModel AccountIncomeReverse { get; set; }
        public AccountingAccountGraphQLModel AccountAiuAdministration { get; set; }
        public AccountingAccountGraphQLModel AccountAiuUnforenseen { get; set; }
        public AccountingAccountGraphQLModel AccountAiuUtility { get; set; }
        public bool AllowAiu { get; set; }
        public IEnumerable<TaxGraphQLModel> BuyTaxes { get; set; } = [];
        public IEnumerable<TaxGraphQLModel> SellTaxes { get; set; } = [];
    }

    public class AccountingGroupDTO : AccountingGroupGraphQLModel 
    {
        
    }
}
