using Models.Books;
using Models.Global;
using System;
using System.Collections.Generic;

namespace Models.Inventory
{
    /// <summary>
    /// TUI (Transacción Unitaria de Inventario): evento físico atómico de stock.
    /// Mapea al tipo <c>InventoryTransaction</c> del schema GraphQL.
    /// </summary>
    public class InventoryTransactionGraphQLModel
    {
        public int Id { get; set; }

        /// <summary>posted | reversed (etc., según InventoryTransactionStatus).</summary>
        public string Status { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;
        public CompanyGraphQLModel Company { get; set; } = new();
        public AccountingSourceGraphQLModel AccountingSource { get; set; } = new();
        public SystemAccountGraphQLModel? CreatedBy { get; set; }
        public IEnumerable<InventoryTransactionLineGraphQLModel> Lines { get; set; } = [];
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>Línea de un TUI: un movimiento de (ítem, bodega).</summary>
    public class InventoryTransactionLineGraphQLModel
    {
        public int Id { get; set; }
        public int InventoryTransactionId { get; set; }
        public ItemGraphQLModel Item { get; set; } = new();
        public StorageGraphQLModel Storage { get; set; } = new();
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public int DisplayOrder { get; set; }
        public IEnumerable<InventoryTransactionLineLotGraphQLModel> Lots { get; set; } = [];
        public IEnumerable<InventoryTransactionLineSerialGraphQLModel> Serials { get; set; } = [];
        public IEnumerable<InventoryTransactionLineSizeGraphQLModel> Sizes { get; set; } = [];
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class InventoryTransactionLineLotGraphQLModel
    {
        public int Id { get; set; }
        public int InventoryTransactionLineId { get; set; }
        public ItemGraphQLModel Item { get; set; } = new();
        public StorageGraphQLModel Storage { get; set; } = new();
        public LotGraphQLModel Lot { get; set; } = new();
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class InventoryTransactionLineSerialGraphQLModel
    {
        public int Id { get; set; }
        public int InventoryTransactionLineId { get; set; }
        public ItemGraphQLModel Item { get; set; } = new();
        public StorageGraphQLModel Storage { get; set; } = new();
        public SerialGraphQLModel Serial { get; set; } = new();
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class InventoryTransactionLineSizeGraphQLModel
    {
        public int Id { get; set; }
        public int InventoryTransactionLineId { get; set; }
        public ItemGraphQLModel Item { get; set; } = new();
        public StorageGraphQLModel Storage { get; set; } = new();
        public ItemSizeValueGraphQLModel Size { get; set; } = new();
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
