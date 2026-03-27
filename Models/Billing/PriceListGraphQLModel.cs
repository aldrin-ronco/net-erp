using Models.Global;
using Models.Inventory;
using System;
using static Models.Global.GraphQLResponseTypes;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Billing
{
    public class PriceListGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool EditablePrice { get; set; }
        public bool IsActive { get; set; }
        public bool AutoApplyDiscount { get; set; }
        public bool IsPublic { get; set; }
        public bool AllowNewUsersAccess { get; set; }
        public string ListUpdateBehaviorOnCostChange { get; set; } = string.Empty;
        public PriceListGraphQLModel? Parent { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsTaxable { get; set; }
        public bool PriceListIncludeTax { get; set; }
        public bool UseAlternativeFormula { get; set; }
        public string CostMode { get; set; } = "USE_AVERAGE_COST";
        public StorageGraphQLModel? Storage { get; set; }
        public IEnumerable<PaymentMethodGraphQLModel> ExcludedPaymentMethods { get; set; } = [];
        public bool Archived { get; set; }
        public CompanyGraphQLModel? Company { get; set; }
        public SystemAccountGraphQLModel? CreatedBy { get; set; }
        public IEnumerable<PriceListDetailGraphQLModel> Details { get; set; } = [];
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string FullName
        {
            get
            {
                if(Parent != null)
                {
                    return $"{Name} ({Parent.Name})";
                }
                return Name;
            }
        }
    }

    public class PriceListDataContext
    {
        public PageType<CatalogGraphQLModel> CatalogsPage { get; set; } = new();
        public PageType<PriceListGraphQLModel> PriceListsPage { get; set; } = new();
    }

    public class PriceListCreateMessage
    {
        public UpsertResponseType<PriceListGraphQLModel> CreatedPriceList { get; set; } = new();
    }

    public class PriceListUpdateMessage
    {
        public UpsertResponseType<PriceListGraphQLModel> UpdatedPriceList { get; set; } = new();
    }

    public class PriceListDeleteMessage
    {
        public DeleteResponseType DeletedPriceList { get; set; } = new();
    }

    public class PriceListArchiveMessage
    {
        public UpsertResponseType<PriceListGraphQLModel> ArchivedPriceList { get; set; } = new();
    }
}
