using Models.Books;
using Models.Treasury;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Global
{
    public class CostCenterGraphQLModel
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public string TradeName { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public char State { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Phone1 { get; set; } = string.Empty;
        public string Phone2 { get; set; } = string.Empty;
        public string CellPhone1 { get; set; } = string.Empty;
        public string CellPhone2 { get; set; } = string.Empty;
        public string DateControlType { get; set; } = string.Empty;
        public int RelatedAccountingEntityId { get; set; } = 0;
        public bool IsTaxable { get; set; } = false;
        public bool PriceListIncludeTax { get; set; } = false;
        public bool InvoicePriceIncludeTax { get; set; } = false;
        public bool ShowChangeWindowOnCash { get; set; } = false;
        public bool AllowRepeatItemsOnSales { get; set; } = false;
        public bool RequiresConfirmationToPrintCopies { get; set; } = false;
        public bool AllowBuy { get; set; } = false;
        public bool AllowSell { get; set; } = false;
        public bool TaxToCost { get; set; } = false;
        public string DefaultInvoiceObservation { get; set; } = string.Empty;
        public string InvoiceFooter { get; set; } = string.Empty;
        public string RemissionFooter { get; set; } = string.Empty;
        public int InvoiceCopiesToPrint { get; set; } = 0;
        public CountryGraphQLModel Country { get; set; }
        public DepartmentGraphQLModel Department { get; set; }
        public CityGraphQLModel City { get; set; }
        public CompanyLocationGraphQLModel Location { get; set; }
        public AccountingEntityGraphQLModel RelatedAccountingEntity { get; set; }
        public ObservableCollection<CashDrawerGraphQLModel> CashDrawers { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class CostCenterDTO : CostCenterGraphQLModel
    {
        public bool IsExpanded { get; set; } = false;
        public bool IsSelected { get; set; } = false;
    }

    public class CostCenterCreateMessage
    {
        public CostCenterGraphQLModel CreatedCostCenter { get; set; }
    }

    public class CostCenterUpdateMessage
    {
        public CostCenterGraphQLModel UpdatedCostCenter { get; set; }
    }

    public class CostCenterDeleteMessage
    {
        public CostCenterGraphQLModel DeletedCodtCenter { get; set; }
    }
}
