using System;

namespace NetErp.UserControls.ItemDimensionEditor.DTO
{
    /// <summary>
    /// Una preselección de lote para una línea, lista para enviar al backend.
    /// En entradas (<see cref="DimensionDirection.In"/>) trae <see cref="LotNumber"/> y opcionalmente <see cref="ExpirationDate"/>.
    /// En salidas (<see cref="DimensionDirection.Out"/>) trae <see cref="LotId"/>.
    /// </summary>
    public sealed record LotDraft(
        int? LotId,
        string? LotNumber,
        DateTime? ExpirationDate,
        decimal Quantity);

    /// <summary>
    /// Una preselección de serial. Cantidad implícita = 1 por entrada.
    /// En entradas trae <see cref="SerialNumber"/>; en salidas trae <see cref="SerialId"/>.
    /// </summary>
    public sealed record SerialDraft(
        int? SerialId,
        string? SerialNumber);

    /// <summary>
    /// Una preselección por talla. <see cref="SizeId"/> es obligatorio en ambas direcciones.
    /// </summary>
    public sealed record SizeDraft(
        int SizeId,
        string SizeName,
        decimal Quantity);
}
