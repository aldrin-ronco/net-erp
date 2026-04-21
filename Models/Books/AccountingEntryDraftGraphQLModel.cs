using Models.Global;
using System;
using System.Collections.Generic;
using System.Numerics;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Books
{
    /// <summary>
    /// Borrador de comprobante contable.
    /// Mapea al tipo <c>DraftAccountingEntry</c> del schema GraphQL.
    /// </summary>
    public class DraftAccountingEntryGraphQLModel
    {
        public BigInteger Id { get; set; } = 0;
        public DateTime DocumentDate { get; set; } = DateTime.Now.Date;

        private DateTime _insertedAt = DateTime.Now;
        public DateTime InsertedAt
        {
            get { return TimeZoneInfo.ConvertTimeFromUtc(_insertedAt.ToUniversalTime(), TimeZoneInfo.Local); }
            set { _insertedAt = value; }
        }

        public DateTime UpdatedAt { get; set; }

        private string _documentNumber = string.Empty;
        public string DocumentNumber
        {
            get { return string.IsNullOrEmpty(_documentNumber) ? "N/A" : _documentNumber; }
            set { _documentNumber = value; }
        }

        public string Description { get; set; } = string.Empty;
        public SystemAccountGraphQLModel CreatedBy { get; set; }
        public AccountingBookGraphQLModel AccountingBook { get; set; }
        public CostCenterGraphQLModel CostCenter { get; set; }
        public AccountingSourceGraphQLModel AccountingSource { get; set; }

        /// <summary>
        /// Líneas del borrador (subselección <c>lines</c> del schema).
        /// </summary>
        public IEnumerable<DraftAccountingEntryLineGraphQLModel> Lines { get; set; } = [];

        /// <summary>
        /// Si el borrador ya fue finalizado, apunta al <c>AccountingEntry</c> resultante.
        /// </summary>
        public AccountingEntryGraphQLModel AccountingEntry { get; set; }
    }

    public class DraftAccountingEntryDeleteMessage
    {
        public DeleteResponseType DeletedDraftAccountingEntry { get; set; } = new();
    }

    public class DraftAccountingEntryFinalizeMessage
    {
        public BigInteger DraftId { get; set; }
    }

    public class DraftAccountingEntryUpdateMessage
    {
        public DraftAccountingEntryGraphQLModel UpdatedDraftAccountingEntry { get; set; }
    }
}
