using Models.Global;
using System;

namespace Models.Inventory
{
    /// <summary>
    /// Stock disponible por talla para un item en una bodega.
    /// Mapea al tipo <c>StockBySize</c> del schema GraphQL.
    /// </summary>
    public class StockBySizeGraphQLModel
    {
        public int Id { get; set; }
        public CompanyGraphQLModel Company { get; set; } = new();
        public ItemGraphQLModel Item { get; set; } = new();
        public StorageGraphQLModel Storage { get; set; } = new();
        public ItemSizeValueGraphQLModel Size { get; set; } = new();
        public decimal Quantity { get; set; }
        public decimal Cost { get; set; }
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
