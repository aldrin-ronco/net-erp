using Caliburn.Micro;
using Models.Books;
using Models.Global;
using NetErp.Global.CostCenters.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Books.AccountingEntries.DTO
{
    public class AccountingEntryDraftDetailDTO: PropertyChangedBase
    {

        private BigInteger _id;
        public BigInteger Id
        {
            get { return _id; }
            set 
            {
                if(_id != value)
                {
                    _id = value; 
                    NotifyOfPropertyChange(nameof(Id));
                }
            }
        }

        private string _recordDetail = string.Empty;
        public string RecordDetail
        {
            get { return _recordDetail; }
            set
            {
                if (_recordDetail != value)
                {
                    _recordDetail = value;
                    NotifyOfPropertyChange(nameof(RecordDetail));
                }
            }
        }

        private decimal _debit = 0;
        public decimal Debit
        {
            get { return _debit; }
            set
            {
                if (_debit != value)
                {
                    _debit = value;
                    NotifyOfPropertyChange(nameof(Debit));
                }
            }
        }

        private decimal _credit = 0;
        public decimal Credit
        {
            get { return _credit; }
            set
            {
                if (_credit != value)
                {
                    _credit = value;
                    NotifyOfPropertyChange(nameof(Credit));
                }
            }
        }

        private decimal _base = 0;
        public decimal Base
        {
            get { return _base; }
            set
            {
                if (_base != value)
                {
                    _base = value;
                    NotifyOfPropertyChange(nameof(Base));
                }
            }
        }

        private AccountingEntryTotals _totals = new();
        public AccountingEntryTotals Totals
        {
            get { return _totals; }
            set
            {
                if (_totals != value)
                {
                    _totals = value;
                    NotifyOfPropertyChange(nameof(Totals));
                }
            }
        }

        private AccountingAccountGraphQLModel _accountingAccount = new();
        public AccountingAccountGraphQLModel AccountingAccount
        {
            get { return _accountingAccount; }
            set
            {
                if (_accountingAccount != value)
                {
                    _accountingAccount = value;
                    NotifyOfPropertyChange(nameof(AccountingAccount));
                }
            }
        }

        private AccountingEntityGraphQLModel _accountingEntity = new();
        public AccountingEntityGraphQLModel AccountingEntity
        {
            get { return _accountingEntity; }
            set
            {
                if (_accountingEntity != value)
                {
                    _accountingEntity = value;
                    NotifyOfPropertyChange(nameof(AccountingEntity));
                }
            }
        }

        private CostCenterDTO _costCenter = new();
        public CostCenterDTO CostCenter
        {
            get { return _costCenter; }
            set
            {
                if (_costCenter != value)
                {
                    _costCenter = value;
                    NotifyOfPropertyChange(nameof(CostCenter));
                }
            }
        }

        private bool _isChecked = false;
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    NotifyOfPropertyChange(nameof(IsChecked));
                }
            }
        }
    }
}
