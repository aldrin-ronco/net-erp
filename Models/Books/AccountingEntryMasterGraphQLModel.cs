using Models.Global;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Books
{
    public class AccountingEntryMasterGraphQLModel
    {
        public BigInteger Id { get; set; } = 0;
        public BigInteger? DraftMasterId { get; set; }
        public string State { get; set; } = string.Empty;
        public DateTime DocumentDate { get; set; } = DateTime.Now.Date;
        private DateTime _createdAt = DateTime.Now;
        public DateTime CreatedAt
        {
            get { return TimeZoneInfo.ConvertTimeFromUtc(this._createdAt.ToUniversalTime(), TimeZoneInfo.Local); }
            set { _createdAt = value; }
        }
        public TimeSpan DocumentTime {  get; set; }
        public string Description { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string CancelledBy { get; set; } = string.Empty;
        public bool Annulment { get; set; } = false;
        public AccountingBookGraphQLModel AccountingBook { get; set; }
        public CostCenterGraphQLModel CostCenter { get; set; }
        public AccountingSourceGraphQLModel AccountingSource { get; set; }
        public IEnumerable<AccountingEntryDetailGraphQLModel> AccountingEntriesDetail { get; set; }
        public AccountingEntryTotals Totals { get; set; }

        private string GetInfo()
        {
            string _info = "";
            if (this.DraftMasterId != null) _info = "Existe un borrador asociado a este documento";
            if (!string.IsNullOrEmpty(this.State)) _info = string.IsNullOrEmpty(_info) ? "Este documento ha sido anulado" : $"{_info}\r\nEste documento ha sido anulado";
            if (this.Annulment) _info = string.IsNullOrEmpty(_info) ? $"Este es un documento de anulación" : $"{_info}\r\nEste es un documento de anulación";
            return _info;
        }

        public string Info { get { return this.GetInfo(); } }
    }

    public class AccountingEntryTotals
    {
        public decimal Debit { get; set; } = 0;
        public decimal Credit { get; set; } = 0;
    }

    public class AccountingEntryDraftTotals
    {
        public decimal Debit { get; set; } = 0;
        public decimal Credit { get; set; } = 0;
    }

    public class AccountingEntryCountDelete
    {
        public int Count { get; set; }
    }

    public class AccountingEntriesDataContext
    {
        public ObservableCollection<AccountingBookGraphQLModel> AccountingBooks { get; set; }
        public ObservableCollection<AccountingSourceGraphQLModel> AccountingSources { get; set; }
        public ObservableCollection<CostCenterGraphQLModel> CostCenters { get; set; }
        public PageType<AccountingEntryDraftMasterGraphQLModel>  AccountingEntryDraftMasterPage { get; set; }
    }

    public class AccountingEntriesDraftDetailDataContext
    {
        public AccountingEntryDraftTotals AccountingEntryDraftTotals { get; set; } = new();
    }

    //TODO
    public class BulkDeleteAccountingEntryMaster
    {
        public int Count { get; set; }
    }
    public class AccountingEntryDocumentPreviewDataContext
    {
        public AccountingEntryMasterGraphQLModel AccountingEntryMaster { get; set; }
        public PageType<AccountingEntryDetailGraphQLModel> AccountingEntryDetailPage { get; set; }
    }

    public class AccountingEntryMasterDTO : AccountingEntryMasterGraphQLModel
    {
        public bool IsChecked { get; set; } = false;
    }

    public class AccountingEntryMasterDeleteMessage
    {
        public BigInteger Id { get; set; }
    }

    public class AccountingEntryMasterCancellationMessage
    {
        public AccountingEntryMasterGraphQLModel CancelledAccountingEntry { get; set; }
    }
}
