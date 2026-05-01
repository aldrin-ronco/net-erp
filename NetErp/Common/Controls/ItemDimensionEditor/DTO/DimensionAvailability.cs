using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

    /// <summary>
    /// Conflicto detectado en un serial propuesto para entrada (validateInboundSerials).
    /// </summary>
    public sealed record SerialInboundConflict(
        string SerialNumber,
        SerialValidationStatus Status,
        string? StorageName,
        int? DraftId,
        string? DraftDocumentNumber);

    /// <summary>
    /// Estado de validación de un serial en captura de entrada.
    /// </summary>
    public enum SerialValidationStatus
    {
        Pending,
        Available,
        AlreadyActive,
        PreselectedInDraft,
        DuplicateInList
    }

    /// <summary>
    /// Validador de seriales para entrada — pre-check contra master + drafts ajenos.
    /// Retorna lista de conflictos (vacía = todos disponibles).
    /// </summary>
    public delegate Task<IReadOnlyList<SerialInboundConflict>> InboundSerialValidator(
        int itemId,
        IReadOnlyList<string> serialNumbers,
        int? excludeStockMovementId,
        CancellationToken cancellationToken);
}
