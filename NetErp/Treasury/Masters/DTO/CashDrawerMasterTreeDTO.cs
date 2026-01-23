using Caliburn.Micro;
using Models.Books;
using Models.Global;
using Models.Treasury;
using NetErp.Treasury.Masters.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Treasury.Masters.DTO
{
    public class CashDrawerMasterTreeDTO: Screen
    {
        public int Id { get; set; }

        public TreasuryRootMasterViewModel Context { get; set; }


        private string _name = string.Empty;

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    NotifyOfPropertyChange(nameof(Name));
                }
            }
        }


        private bool _isDummyChild = false;
        public bool IsDummyChild
        {
            get { return _isDummyChild; }
            set
            {
                if (_isDummyChild != value)
                {
                    _isDummyChild = value;
                    NotifyOfPropertyChange(nameof(IsDummyChild));
                }
            }
        }

        private bool _cashReviewRequired;
        public bool CashReviewRequired
        {
            get { return _cashReviewRequired; }
            set
            {
                if (_cashReviewRequired != value)
                {
                    _cashReviewRequired = value;
                    NotifyOfPropertyChange(nameof(CashReviewRequired));
                }
            }
        }

        private bool _autoAdjustBalance;
        public bool AutoAdjustBalance
        {
            get { return _autoAdjustBalance; }
            set
            {
                if (_autoAdjustBalance != value)
                {
                    _autoAdjustBalance = value;
                    NotifyOfPropertyChange(nameof(AutoAdjustBalance));
                }
            }
        }

        private bool _autoTransfer;
        public bool AutoTransfer
        {
            get { return _autoTransfer; }
            set
            {
                if (_autoTransfer != value)
                {
                    _autoTransfer = value;
                    NotifyOfPropertyChange(nameof(AutoTransfer));
                }
            }
        }

        private bool _isPettyCash;
        public bool IsPettyCash
        {
            get { return _isPettyCash; }
            set
            {
                if (_isPettyCash != value)
                {
                    _isPettyCash = value;
                    NotifyOfPropertyChange(nameof(IsPettyCash));
                }
            }
        }

        private CashDrawerGraphQLModel _parent;

        public CashDrawerGraphQLModel Parent
        {
            get { return _parent; }
            set 
            {
                if (_parent != value)
                {
                    _parent = value;
                    NotifyOfPropertyChange(nameof(Parent));
                }
            }
        }


        private CashDrawerGraphQLModel _autoTransferCashDrawer;
        public CashDrawerGraphQLModel AutoTransferCashDrawer
        {
            get { return _autoTransferCashDrawer; }
            set
            {
                if (_autoTransferCashDrawer != value)
                {
                    _autoTransferCashDrawer = value;
                    NotifyOfPropertyChange(nameof(AutoTransferCashDrawer));
                }
            }
        }

        private CostCenterGraphQLModel _costCenter;
        public CostCenterGraphQLModel CostCenter
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

        private AccountingAccountGraphQLModel _cashAccountingAccount;
        public AccountingAccountGraphQLModel CashAccountingAccount
        {
            get { return _cashAccountingAccount; }
            set
            {
                if (_cashAccountingAccount != value)
                {
                    _cashAccountingAccount = value;
                    NotifyOfPropertyChange(nameof(CashAccountingAccount));
                }
            }
        }

        private AccountingAccountGraphQLModel _checkAccountingAccount;
        public AccountingAccountGraphQLModel CheckAccountingAccount
        {
            get { return _checkAccountingAccount; }
            set
            {
                if (_checkAccountingAccount != value)
                {
                    _checkAccountingAccount = value;
                    NotifyOfPropertyChange(nameof(CheckAccountingAccount));
                }
            }
        }

        private AccountingAccountGraphQLModel _cardAccountingAccount;
        public AccountingAccountGraphQLModel CardAccountingAccount
        {
            get { return _cardAccountingAccount; }
            set
            {
                if (_cardAccountingAccount != value)
                {
                    _cardAccountingAccount = value;
                    NotifyOfPropertyChange(nameof(CardAccountingAccount));
                }
            }
        }

        private string _computerName;

        public string ComputerName
        {
            get { return _computerName; }
            set 
            {
                if (_computerName != value)
                {
                    _computerName = value;
                    NotifyOfPropertyChange(nameof(ComputerName));
                }
            }
        }

    }
}
