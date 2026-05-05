using Models.Global;

namespace Models.Inventory
{
    /// <summary>
    /// Stock total por (item, storage, tracking) sobre <c>inventory.v_stock_total</c>.
    /// Sólo expone filas con quantity > 0.
    /// Mapea al tipo <c>StockTotal</c> del schema GraphQL.
    /// </summary>
    public class StockTotalGraphQLModel
    {
        /// <summary>BASE | SIZE | LOT | SERIAL.</summary>
        public string Tracking { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Cost { get; set; }
        public ItemGraphQLModel Item { get; set; } = new();
        public StorageGraphQLModel Storage { get; set; } = new();

        // Pass-through helpers para binding del modal de búsqueda (3 columnas).
        public string Code => Item?.Code ?? string.Empty;
        public string Name => Item?.Name ?? string.Empty;
        public string Reference => Item?.Reference ?? string.Empty;
    }
}
