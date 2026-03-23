using Models.Inventory;

namespace Models.Billing
{
    public class PriceListItemGraphQLModel
    {
        public decimal? DiscountMargin { get; set; }
        public ItemGraphQLModel Item { get; set; } = new();
        public decimal? MinimumPrice { get; set; }
        public decimal? Price { get; set; }
        public PriceListGraphQLModel? PriceList { get; set; }
        public decimal? ProfitMargin { get; set; }
        public decimal? Quantity { get; set; }
    }
}
