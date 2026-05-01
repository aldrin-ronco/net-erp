namespace NetErp.Inventory.CatalogItems.DTO
{
    /// <summary>
    /// Modo de dimensión que maneja el item. Mutuamente exclusivos.
    /// </summary>
    public enum ItemDimensionMode
    {
        /// <summary>Sin dimensiones — stock agregado por bodega.</summary>
        Generic,
        /// <summary>Trazabilidad por lote (lotNumber + expirationDate).</summary>
        Lot,
        /// <summary>Trazabilidad individual por serial.</summary>
        Serial,
        /// <summary>Stock segmentado por valores de talla — requiere ItemSizeCategory.</summary>
        Size
    }
}
