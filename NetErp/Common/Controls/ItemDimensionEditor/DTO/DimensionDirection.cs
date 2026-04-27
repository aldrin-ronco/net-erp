namespace NetErp.UserControls.ItemDimensionEditor.DTO
{
    /// <summary>
    /// Sentido de un movimiento de inventario respecto al UC <c>ItemDimensionEditor</c>.
    /// I = entrada (kardex_flow I); O = salida (kardex_flow O).
    /// </summary>
    public enum DimensionDirection
    {
        /// <summary>Entrada de inventario. Lotes y seriales se crean al postear (lot_number / serial_number).</summary>
        In,

        /// <summary>Salida de inventario. Lotes y seriales referencian filas existentes (lot_id / serial_id) con disponibilidad consultada por bodega.</summary>
        Out
    }
}
