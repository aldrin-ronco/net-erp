using Models.Inventory;
using System;
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
        public bool ShowDiscountedPrice { get; set; }
        public int PresentationOrder { get; set; }
        public bool AllowViewDiscount { get; set; }
        public bool IsPublic { get; set; }
        public bool AllowNewUsersAccess { get; set; }
        public bool AutoUpdateOnCostChange { get; set; }
        public string ListUpdateBehaviorOnCostChange { get; set; } = string.Empty;
        public bool IsPromotion { get; set; }
        public int ParentId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class PriceListDetailGraphQLModel
    {
        public ItemGraphQLModel CatalogItem { get; set; } = new();
        public MeasurementUnitGraphQLModel Measurement { get; set; } = new();
        public decimal? Cost { get; set; }
        public decimal? ProfitMargin { get; set; }
        public decimal? Price { get; set; }
        public decimal? MinimumPrice { get; set; }
        public decimal? DiscountMargin { get; set; }
    }

    public class PriceListDataContext
    {
        public ObservableCollection<CatalogGraphQLModel> Catalogs { get; set; } = [];
        public ObservableCollection<PriceListGraphQLModel> PriceLists { get; set; } = [];
    }
}
