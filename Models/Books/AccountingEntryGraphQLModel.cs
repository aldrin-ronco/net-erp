using Models.Global;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Books
{
    /// <summary>
    /// Comprobante contable publicado (encabezado).
    /// Mapea al tipo <c>AccountingEntry</c> del schema GraphQL.
    /// </summary>
    public class AccountingEntryGraphQLModel
    {
        public BigInteger Id { get; set; } = 0;
        public string Status { get; set; } = string.Empty;
        public DateTime DocumentDate { get; set; } = DateTime.Now.Date;

        private DateTime _insertedAt = DateTime.Now;
        public DateTime InsertedAt
        {
            get { return TimeZoneInfo.ConvertTimeFromUtc(_insertedAt.ToUniversalTime(), TimeZoneInfo.Local); }
            set { _insertedAt = value; }
        }

        public DateTime UpdatedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public SystemAccountGraphQLModel CreatedBy { get; set; }
        public SystemAccountGraphQLModel CancelledBy { get; set; }
        public bool Annulment { get; set; } = false;
        public AccountingBookGraphQLModel AccountingBook { get; set; }
        public CostCenterGraphQLModel CostCenter { get; set; }
        public AccountingSourceGraphQLModel AccountingSource { get; set; }
        public CompanyGraphQLModel Company { get; set; }

        /// <summary>
        /// Líneas del comprobante publicado (subselección <c>lines</c> del schema).
        /// </summary>
        public IEnumerable<AccountingEntryLineGraphQLModel> Lines { get; set; } = [];

        /// <summary>
        /// Si el comprobante es una anulación de otro, aquí apunta al original.
        /// </summary>
        public AccountingEntryGraphQLModel Reverse { get; set; }

        private string GetInfo()
        {
            string _info = "";
            if (Status is "CANCELLED_WITH_DOCUMENT" or "CANCELLED_NO_DOCUMENT") _info = "Este documento ha sido anulado";
            if (Annulment) _info = string.IsNullOrEmpty(_info) ? "Este es un documento de anulación" : $"{_info}\r\nEste es un documento de anulación";
            return _info;
        }

        public string Info => GetInfo();
    }

    public class AccountingEntriesDataContext
    {
        public ObservableCollection<AccountingBookGraphQLModel> AccountingBooks { get; set; } = [];
        public ObservableCollection<AccountingSourceGraphQLModel> AccountingSources { get; set; } = [];
        public ObservableCollection<CostCenterGraphQLModel> CostCenters { get; set; } = [];
        public PageType<DraftAccountingEntryGraphQLModel> DraftAccountingEntryPage { get; set; } = new();
    }

    public class AccountingEntryDTO : AccountingEntryGraphQLModel
    {
        public bool IsChecked { get; set; } = false;
    }

    public class AccountingEntryDeleteMessage
    {
        public BigInteger Id { get; set; }
    }

    public class AccountingEntryCancellationMessage
    {
        public AccountingEntryGraphQLModel CancelledAccountingEntry { get; set; }
    }
}
