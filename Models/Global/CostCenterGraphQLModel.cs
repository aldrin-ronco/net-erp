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
        public string Status { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PrimaryPhone { get; set; } = string.Empty;
        public string SecondaryPhone { get; set; } = string.Empty;
        public string PrimaryCellPhone { get; set; } = string.Empty;
        public string SecondaryCellPhone { get; set; } = string.Empty;
        public string DateControlType { get; set; } = string.Empty;
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
        public CountryGraphQLModel Country { get; set; } = new();
        public DepartmentGraphQLModel Department { get; set; } = new();
        public CityGraphQLModel City { get; set; } = new();
        public CompanyLocationGraphQLModel CompanyLocation { get; set; } = new();
        public ObservableCollection<CashDrawerGraphQLModel> CashDrawers { get; set; } = [];
        public AuthorizationSequenceGraphQLModel FeCreditDefaultAuthorizationSequence { get; set; } = new();
        public AuthorizationSequenceGraphQLModel FeCashDefaultAuthorizationSequence { get; set; } = new();

        public AuthorizationSequenceGraphQLModel PeDefaultAuthorizationSequence { get; set; } = new();
        public AuthorizationSequenceGraphQLModel DsDefaultAuthorizationSequence { get; set; } = new();
        public override string ToString()
        {
            return Name;
        }
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
        public CostCenterGraphQLModel DeletedCostCenter { get; set; }
    }
}
