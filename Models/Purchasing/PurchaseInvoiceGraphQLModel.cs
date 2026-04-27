using Models.Books;
using Models.Global;
using Models.Inventory;
using Models.Login;
using System;
using System.Collections.Generic;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Purchasing
{
    /// <summary>
    /// Factura de compra. Soporta dos rutas vía <see cref="CreationMode"/>:
    /// DIRECT (la factura crea el TUI) y RADICATION (la factura consolida SMs ya posteados).
    /// Mapea al tipo <c>PurchaseInvoice</c> del schema GraphQL.
    /// </summary>
    public class PurchaseInvoiceGraphQLModel
    {
        public int Id { get; set; }

        /// <summary>DRAFT | POSTED.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>DIRECT | RADICATION. Inmutable post-draft.</summary>
        public string CreationMode { get; set; } = string.Empty;

        public string Number { get; set; } = string.Empty;
        public string SupplierDocumentNumber { get; set; } = string.Empty;
        public DateTime? SupplierDocumentDate { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? DueDate { get; set; }

        public string CurrencyCode { get; set; } = string.Empty;
        public string BaseCurrencyCode { get; set; } = string.Empty;
        public decimal? ExchangeRate { get; set; }
        public DateTime? ExchangeRateDate { get; set; }

        public int LineCount { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal TotalTaxes { get; set; }
        public decimal TotalWithholdings { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalCost { get; set; }
        public decimal BalanceDue { get; set; }

        public string Note { get; set; } = string.Empty;
        public DateTime? PostedAt { get; set; }

        public int? DocumentSequenceId { get; set; }

        public CompanyGraphQLModel Company { get; set; } = new();
        public CostCenterGraphQLModel CostCenter { get; set; } = new();
        public AccountingSourceGraphQLModel AccountingSource { get; set; } = new();
        public AccountingEntityGraphQLModel AccountingEntity { get; set; } = new();
        public StorageGraphQLModel? Storage { get; set; }
        public AccountingAccountGraphQLModel PayableAccount { get; set; } = new();

        public SystemAccountGraphQLModel? CreatedBy { get; set; }
        public SystemAccountGraphQLModel? PostedBy { get; set; }

        public IEnumerable<PurchaseInvoiceLineGraphQLModel> Lines { get; set; } = [];
        public IEnumerable<PurchaseInvoiceWithholdingGraphQLModel> Withholdings { get; set; } = [];
        public PurchaseInvoiceTuiLinkGraphQLModel? TuiLink { get; set; }

        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>Línea de factura de compra (1 línea → 1 ITL con dimensión múltiple en sub-tablas).</summary>
    public class PurchaseInvoiceLineGraphQLModel
    {
        public int Id { get; set; }
        public int PurchaseInvoiceId { get; set; }
        public ItemGraphQLModel Item { get; set; } = new();
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal NetAmount { get; set; }
        public decimal TotalTaxes { get; set; }
        public decimal LineTotal { get; set; }
        public int? PayableAccountId { get; set; }
        public IEnumerable<PurchaseInvoiceLineTaxGraphQLModel> Taxes { get; set; } = [];
        public IEnumerable<PurchaseInvoiceLineDiscountGraphQLModel> Discounts { get; set; } = [];
        public IEnumerable<PurchaseInvoiceLineLotGraphQLModel> LotPreselections { get; set; } = [];
        public IEnumerable<PurchaseInvoiceLineSerialGraphQLModel> SerialPreselections { get; set; } = [];
        public IEnumerable<PurchaseInvoiceLineSizeGraphQLModel> SizePreselections { get; set; } = [];
        public PurchaseInvoiceLineTuiLineLinkGraphQLModel? TuiLineLink { get; set; }
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class PurchaseInvoiceLineTaxGraphQLModel
    {
        public int Id { get; set; }
        public int PurchaseInvoiceLineId { get; set; }
        public TaxGraphQLModel Tax { get; set; } = new();
        public decimal Rate { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public AccountingAccountGraphQLModel AccountingAccount { get; set; } = new();
    }

    public class PurchaseInvoiceLineDiscountGraphQLModel
    {
        public int Id { get; set; }
        public int PurchaseInvoiceLineId { get; set; }
        public AdditionalDiscountTypeGraphQLModel? DiscountType { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
    }

    public class PurchaseInvoiceLineLotGraphQLModel
    {
        public int Id { get; set; }
        public int PurchaseInvoiceLineId { get; set; }
        public ItemGraphQLModel Item { get; set; } = new();
        public LotGraphQLModel? Lot { get; set; }
        public int? LotId { get; set; }

        /// <summary>Populado en modo lote nuevo: el lote se crea al postear.</summary>
        public string? LotNumber { get; set; }

        public DateTime? ExpirationDate { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class PurchaseInvoiceLineSerialGraphQLModel
    {
        public int Id { get; set; }
        public int PurchaseInvoiceLineId { get; set; }
        public ItemGraphQLModel Item { get; set; } = new();
        public SerialGraphQLModel? Serial { get; set; }
        public int? SerialId { get; set; }
        public string? SerialNumber { get; set; }
        public decimal UnitCost { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class PurchaseInvoiceLineSizeGraphQLModel
    {
        public int Id { get; set; }
        public int PurchaseInvoiceLineId { get; set; }
        public ItemGraphQLModel Item { get; set; } = new();
        public ItemSizeValueGraphQLModel Size { get; set; } = new();
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class PurchaseInvoiceWithholdingGraphQLModel
    {
        public int Id { get; set; }
        public int PurchaseInvoiceId { get; set; }
        public WithholdingTypeGraphQLModel WithholdingType { get; set; } = new();
        public decimal Rate { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal Amount { get; set; }
        public AccountingAccountGraphQLModel AccountingAccount { get; set; } = new();
    }

    public class PurchaseInvoiceTuiLinkGraphQLModel
    {
        public int Id { get; set; }
        public int PurchaseInvoiceId { get; set; }
        public int InventoryTransactionId { get; set; }
        public InventoryTransactionGraphQLModel? InventoryTransaction { get; set; }
    }

    public class PurchaseInvoiceLineTuiLineLinkGraphQLModel
    {
        public int Id { get; set; }
        public int PurchaseInvoiceLineId { get; set; }
        public int InventoryTransactionLineId { get; set; }
        public InventoryTransactionLineGraphQLModel? InventoryTransactionLine { get; set; }
    }

    /// <summary>Tipo de descuento adicional (placeholder mínimo; ampliar cuando se modele <c>AdditionalDiscountType</c> completo).</summary>
    public class AdditionalDiscountTypeGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class PurchaseInvoiceMutationPayload
    {
        public PurchaseInvoiceGraphQLModel? PurchaseInvoice { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<GlobalErrorGraphQLModel> Errors { get; set; } = [];
    }

    public class PurchaseInvoiceLineMutationPayload
    {
        public PurchaseInvoiceLineGraphQLModel? PurchaseInvoiceLine { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<GlobalErrorGraphQLModel> Errors { get; set; } = [];
    }

    public class PurchaseInvoiceLineLotsMutationPayload
    {
        public List<PurchaseInvoiceLineLotGraphQLModel> PurchaseInvoiceLineLots { get; set; } = [];
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<GlobalErrorGraphQLModel> Errors { get; set; } = [];
    }

    public class PurchaseInvoiceLineSerialsMutationPayload
    {
        public List<PurchaseInvoiceLineSerialGraphQLModel> PurchaseInvoiceLineSerials { get; set; } = [];
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<GlobalErrorGraphQLModel> Errors { get; set; } = [];
    }

    public class PurchaseInvoiceLineSizesMutationPayload
    {
        public List<PurchaseInvoiceLineSizeGraphQLModel> PurchaseInvoiceLineSizes { get; set; } = [];
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<GlobalErrorGraphQLModel> Errors { get; set; } = [];
    }

    public class PurchaseInvoiceCreateMessage
    {
        public PurchaseInvoiceMutationPayload CreatedPurchaseInvoice { get; set; } = new();
    }

    public class PurchaseInvoiceUpdateMessage
    {
        public PurchaseInvoiceMutationPayload UpdatedPurchaseInvoice { get; set; } = new();
    }

    public class PurchaseInvoiceDeleteMessage
    {
        public PurchaseInvoiceMutationPayload DeletedPurchaseInvoice { get; set; } = new();
    }

    public class PurchaseInvoicePostMessage
    {
        public PurchaseInvoiceMutationPayload PostedPurchaseInvoice { get; set; } = new();
    }
}
