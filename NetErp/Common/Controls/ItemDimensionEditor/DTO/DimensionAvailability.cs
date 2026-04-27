using System;

namespace NetErp.UserControls.ItemDimensionEditor.DTO
{
    /// <summary>
    /// Disponibilidad de un lote para un (item, storage) en salidas.
    /// Refleja una fila de <c>stock_by_lot</c> con <c>quantity &gt; 0</c>.
    /// </summary>
    public sealed record LotAvailability(
        int LotId,
        string LotNumber,
        DateTime? ExpirationDate,
        decimal AvailableQuantity,
        decimal Cost);

    /// <summary>
    /// Disponibilidad de un serial para un (item, storage) en salidas.
    /// </summary>
    public sealed record SerialAvailability(
        int SerialId,
        string SerialNumber,
        decimal Cost);

    /// <summary>
    /// Disponibilidad por talla para un (item, storage) en salidas.
    /// </summary>
    public sealed record SizeAvailability(
        int SizeId,
        string SizeName,
        decimal AvailableQuantity,
        decimal Cost);
}
