using Models.Books;
using Models.Global;
using Models.Login;
using System;
using System.Collections.Generic;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Inventory
{
    /// <summary>
    /// Movimiento de stock por concepto (ajustes, mermas, ingresos, donaciones).
    /// Mapea al tipo <c>StockMovement</c> del schema GraphQL.
    /// </summary>
    public class StockMovementGraphQLModel
    {
        public int Id { get; set; }
        public string? DocumentNumber { get; set; }

        /// <summary>
        /// Etiqueta de presentación: "{AccountingSource.Code}-{DocumentNumber}" si posted,
        /// "B#{Id}" si es draft sin número asignado.
        /// </summary>
        public string DocumentDisplay =>
            string.IsNullOrEmpty(DocumentNumber)
                ? $"B#{Id}"
                : $"{AccountingSource?.Code}-{DocumentNumber}";

        /// <summary>DRAFT | POSTED.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>A | X (null si no anulado).</summary>
        public string? CancelledWith { get; set; }

        public bool IsCancelled => !string.IsNullOrEmpty(CancelledWith);

        public string Note { get; set; } = string.Empty;
        public string? CancelNote { get; set; }
        public DateTime? PostedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        public CompanyGraphQLModel Company { get; set; } = new();
        public AccountingSourceGraphQLModel AccountingSource { get; set; } = new();
        public CostCenterGraphQLModel CostCenter { get; set; } = new();
        public StorageGraphQLModel Storage { get; set; } = new();

        public SystemAccountGraphQLModel? CreatedBy { get; set; }
        public SystemAccountGraphQLModel? PostedBy { get; set; }
        public SystemAccountGraphQLModel? CancelledBy { get; set; }

        public IEnumerable<StockMovementLineGraphQLModel> Lines { get; set; } = [];
        public IEnumerable<StockMovementTuiLinkGraphQLModel> TuiLinks { get; set; } = [];

        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>Línea de stock movement (ítem + cantidad).</summary>
    public class StockMovementLineGraphQLModel
    {
        public int Id { get; set; }
        public int StockMovementId { get; set; }
        public ItemGraphQLModel Item { get; set; } = new();
        public decimal Quantity { get; set; }
        public decimal? UnitCost { get; set; }
        public int DisplayOrder { get; set; }

        /// <summary>Subtotal calculado en cliente (cantidad × costo unitario).</summary>
        public decimal Subtotal => Quantity * (UnitCost ?? 0m);
        public IEnumerable<StockMovementLineLotGraphQLModel> LotPreselections { get; set; } = [];
        public IEnumerable<StockMovementLineSerialGraphQLModel> SerialPreselections { get; set; } = [];
        public IEnumerable<StockMovementLineSizeGraphQLModel> SizePreselections { get; set; } = [];
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>Preselección de lote para una línea de stock movement.</summary>
    public class StockMovementLineLotGraphQLModel
    {
        public int Id { get; set; }
        public int StockMovementLineId { get; set; }
        public int? LotId { get; set; }

        /// <summary>Populado en entradas (modo lote nuevo): el lote se crea al postear.</summary>
        public string? LotNumber { get; set; }

        /// <summary>Sólo aplica al lote nuevo.</summary>
        public DateTime? ExpirationDate { get; set; }

        public decimal Quantity { get; set; }
        public decimal? UnitCost { get; set; }
        public int DisplayOrder { get; set; }
        public ItemGraphQLModel Item { get; set; } = new();
        public LotGraphQLModel? Lot { get; set; }
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>Preselección de serial para una línea de stock movement.</summary>
    public class StockMovementLineSerialGraphQLModel
    {
        public int Id { get; set; }
        public int StockMovementLineId { get; set; }
        public int? SerialId { get; set; }

        /// <summary>Populado en entradas: número del serial a crear al postear.</summary>
        public string? SerialNumber { get; set; }

        public decimal? UnitCost { get; set; }
        public int DisplayOrder { get; set; }
        public ItemGraphQLModel Item { get; set; } = new();
        public SerialGraphQLModel? Serial { get; set; }
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>Preselección de talla para una línea de stock movement.</summary>
    public class StockMovementLineSizeGraphQLModel
    {
        public int Id { get; set; }
        public int StockMovementLineId { get; set; }
        public int SizeId { get; set; }
        public decimal Quantity { get; set; }
        public decimal? UnitCost { get; set; }
        public int DisplayOrder { get; set; }
        public ItemGraphQLModel Item { get; set; } = new();
        public ItemSizeValueGraphQLModel Size { get; set; } = new();
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>Vínculo entre un stock movement y uno de sus TUI (header level).</summary>
    public class StockMovementTuiLinkGraphQLModel
    {
        public int Id { get; set; }
        public int StockMovementId { get; set; }
        public int InventoryTransactionId { get; set; }
        public bool IsReversalLink { get; set; }
        public InventoryTransactionGraphQLModel? InventoryTransaction { get; set; }
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class StockMovementMutationPayload
    {
        public StockMovementGraphQLModel? StockMovement { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<GlobalErrorGraphQLModel> Errors { get; set; } = [];
    }

    public class StockMovementLineMutationPayload
    {
        public StockMovementLineGraphQLModel? StockMovementLine { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<GlobalErrorGraphQLModel> Errors { get; set; } = [];
    }

    public class StockMovementLineLotsMutationPayload
    {
        public List<StockMovementLineLotGraphQLModel> StockMovementLineLots { get; set; } = [];
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<GlobalErrorGraphQLModel> Errors { get; set; } = [];
    }

    public class StockMovementLineSerialsMutationPayload
    {
        public List<StockMovementLineSerialGraphQLModel> StockMovementLineSerials { get; set; } = [];
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<GlobalErrorGraphQLModel> Errors { get; set; } = [];
    }

    public class StockMovementLineSizesMutationPayload
    {
        public List<StockMovementLineSizeGraphQLModel> StockMovementLineSizes { get; set; } = [];
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<GlobalErrorGraphQLModel> Errors { get; set; } = [];
    }

    public class StockMovementCreateMessage
    {
        public StockMovementMutationPayload CreatedStockMovement { get; set; } = new();
    }

    public class StockMovementUpdateMessage
    {
        public StockMovementMutationPayload UpdatedStockMovement { get; set; } = new();
    }

    public class StockMovementDeleteMessage
    {
        public StockMovementMutationPayload DeletedStockMovement { get; set; } = new();
    }

    public class StockMovementPostMessage
    {
        public StockMovementMutationPayload PostedStockMovement { get; set; } = new();
    }

    public class StockMovementCancelMessage
    {
        public StockMovementMutationPayload CancelledStockMovement { get; set; } = new();
    }

    /// <summary>
    /// Payload de la query <c>validateInboundSerials</c>.
    /// </summary>
    public class ValidateInboundSerialsPayload
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<GlobalErrorGraphQLModel> Errors { get; set; } = [];
        public List<SerialConflictGraphQLModel> SerialsInConflict { get; set; } = [];
    }

    /// <summary>
    /// Conflicto detectado en un serial propuesto para entrada.
    /// <c>Reason</c> mapea al enum <c>SerialConflictReason</c> del schema.
    /// </summary>
    public class SerialConflictGraphQLModel
    {
        public string SerialNumber { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public StorageGraphQLModel? Storage { get; set; }
        public StockMovementGraphQLModel? Draft { get; set; }
    }
}
