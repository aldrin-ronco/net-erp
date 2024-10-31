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


        private CashDrawerGraphQLModel _cashDrawerAutoTransfer;
        public CashDrawerGraphQLModel CashDrawerAutoTransfer
        {
            get { return _cashDrawerAutoTransfer; }
            set
            {
                if (_cashDrawerAutoTransfer != value)
                {
                    _cashDrawerAutoTransfer = value;
                    NotifyOfPropertyChange(nameof(CashDrawerAutoTransfer));
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

        private AccountingAccountGraphQLModel _accountingAccountCash;
        public AccountingAccountGraphQLModel AccountingAccountCash
        {
            get { return _accountingAccountCash; }
            set
            {
                if (_accountingAccountCash != value)
                {
                    _accountingAccountCash = value;
                    NotifyOfPropertyChange(nameof(AccountingAccountCash));
                }
            }
        }

        private AccountingAccountGraphQLModel _accountingAccountCheck;
        public AccountingAccountGraphQLModel AccountingAccountCheck
        {
            get { return _accountingAccountCheck; }
            set
            {
                if (_accountingAccountCheck != value)
                {
                    _accountingAccountCheck = value;
                    NotifyOfPropertyChange(nameof(AccountingAccountCheck));
                }
            }
        }

        private AccountingAccountGraphQLModel _accountingAccountCard;
        public AccountingAccountGraphQLModel AccountingAccountCard
        {
            get { return _accountingAccountCard; }
            set
            {
                if (_accountingAccountCard != value)
                {
                    _accountingAccountCard = value;
                    NotifyOfPropertyChange(nameof(AccountingAccountCard));
                }
            }
        }
    }
}
