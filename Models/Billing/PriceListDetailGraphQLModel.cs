using Models.Inventory;
using System;

namespace Models.Billing
{
    public class PriceListDetailGraphQLModel
    {
        public int Id { get; set; }
        public decimal? Cost { get; set; }
        public decimal? DiscountMargin { get; set; }
        public ItemGraphQLModel Item { get; set; } = new();
        public decimal? MinimumPrice { get; set; }
        public decimal? Price { get; set; }
        public PriceListGraphQLModel? PriceList { get; set; }
        public decimal? ProfitMargin { get; set; }
        public decimal? Quantity { get; set; }
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
