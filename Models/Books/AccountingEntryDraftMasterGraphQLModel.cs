using Common.Interfaces;
using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Models.Books
{
    public class AccountingEntryDraftMasterGraphQLModel
    {
        public BigInteger Id { get; set; } = 0;
        public BigInteger? MasterId { get; set; } = null;
        public DateTime DocumentDate { get; set; } = DateTime.Now.Date;

        private DateTime _createdAt = DateTime.Now;
        public DateTime CreatedAt
        {
            get { return TimeZoneInfo.ConvertTimeFromUtc(this._createdAt.ToUniversalTime(), TimeZoneInfo.Local); }
            set { _createdAt = value; }
        }
        private string _documentNumber = string.Empty;
        public string DocumentNumber
        {
            get
            {
                return string.IsNullOrEmpty(_documentNumber) ? "N/A" : _documentNumber;
            }
            set { _documentNumber = value; }
        }
        public string Description { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public AccountingBookGraphQLModel AccountingBook { get; set; }
        public CostCenterGraphQLModel CostCenter { get; set; }
        public AccountingSourceGraphQLModel AccountingSource { get; set; }
        public IEnumerable<AccountingEntryDraftDetailGraphQLModel> AccountingEntriesDraftDetail { get; set; }
        public AccountingEntryTotals Totals { get; set; }
    }

    public class AccountingEntryDraftMasterDTO : AccountingEntryDraftMasterGraphQLModel
    {
        public bool IsChecked { get; set; } = false;
    }

    public class AccountingEntryDraftMasterDeleteMessage
    {
        public BigInteger Id { get; set; }
    }

    public class AccountingEntryDraftMasterUpdateMessage
    {
        public AccountingEntryDraftMasterDTO UpdatedAccountingEntryDraftMaster { get; set; }
    }

}
