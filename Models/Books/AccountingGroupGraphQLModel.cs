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
        public AccountingAccountGraphQLModel AccountDeductibleVat { get; set; }
        public AccountingAccountGraphQLModel AccountIncome { get; set; }
        public AccountingAccountGraphQLModel AccountCost { get; set; }
        public AccountingAccountGraphQLModel AccountInventory { get; set; }
        public AccountingAccountGraphQLModel AccountGeneratedVat { get; set; }
        public AccountingAccountGraphQLModel AccountGeneratedVatReverse { get; set; }
        public AccountingAccountGraphQLModel AccountDeductibleVatReverse { get; set; }
        public AccountingAccountGraphQLModel AccountIncomeReverse { get; set; }
        public AccountingAccountGraphQLModel AccountAiuAdministration { get; set; }
        public AccountingAccountGraphQLModel AccountAiuUnforenseen { get; set; }
        public AccountingAccountGraphQLModel AccountAiuUtility { get; set; }
        public AccountingAccountGraphQLModel AccountRawMaterial { get; set; }
        public AccountingAccountGraphQLModel AccountProductInProcess { get; set; }
        public AccountingAccountGraphQLModel AccountInc { get; set; }
        public AccountingAccountGraphQLModel AccountIdc { get; set; }
        public bool ProductionUi { get; set; }
        public bool AuiUi { get; set; }
        public bool IcUi { get; set; }
        public TaxGraphQLModel Tax { get; set; }
        public ItemTypeGraphQLModel ItemType { get; set; }
    }

    public class AccountingGroupDTO : AccountingGroupGraphQLModel 
    {
        
    }
}
