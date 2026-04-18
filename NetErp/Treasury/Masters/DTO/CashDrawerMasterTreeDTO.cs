using Caliburn.Micro;
using Models.Books;
using Models.Global;
using Models.Treasury;
using NetErp.Treasury.Masters.ViewModels;

namespace NetErp.Treasury.Masters.DTO
{
    public class CashDrawerMasterTreeDTO : PropertyChangedBase
    {
        public int Id { get; set; }

        public TreasuryRootMasterViewModel? Context { get; set; }

        public string Name
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Name));
                }
            }
        } = string.Empty;

        public bool CashReviewRequired
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CashReviewRequired));
                }
            }
        }

        public bool AutoAdjustBalance
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AutoAdjustBalance));
                }
            }
        }

        public bool AutoTransfer
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AutoTransfer));
                }
            }
        }

        public bool IsPettyCash
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsPettyCash));
                }
            }
        }

        public CashDrawerGraphQLModel? Parent
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Parent));
                }
            }
        }

        public CashDrawerGraphQLModel? AutoTransferCashDrawer
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AutoTransferCashDrawer));
                }
            }
        }

        public CostCenterGraphQLModel? CostCenter
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CostCenter));
                }
            }
        }

        public AccountingAccountGraphQLModel? CashAccountingAccount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CashAccountingAccount));
                }
            }
        }

        public AccountingAccountGraphQLModel? CheckAccountingAccount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CheckAccountingAccount));
                }
            }
        }

        public AccountingAccountGraphQLModel? CardAccountingAccount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CardAccountingAccount));
                }
            }
        }

        public string ComputerName
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ComputerName));
                }
            }
        } = string.Empty;
    }
}
