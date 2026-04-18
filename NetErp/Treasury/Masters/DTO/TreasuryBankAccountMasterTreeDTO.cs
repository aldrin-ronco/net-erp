using Caliburn.Micro;
using Models.Billing;
using Models.Books;
using NetErp.Treasury.Masters.ViewModels;
using System.Collections.ObjectModel;

namespace NetErp.Treasury.Masters.DTO
{
    public class TreasuryBankAccountMasterTreeDTO : PropertyChangedBase, ITreasuryTreeMasterSelectedItem
    {
        public int Id { get; set; }
        public TreasuryRootMasterViewModel? Context { get; set; }

        public string Type
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Type));
                }
            }
        } = string.Empty;

        public string Number
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Number));
                }
            }
        } = string.Empty;

        public bool IsActive
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsActive));
                }
            }
        }

        public string Description
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Description));
                }
            }
        } = string.Empty;

        public string Reference
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Reference));
                }
            }
        } = string.Empty;

        public int DisplayOrder
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DisplayOrder));
                }
            }
        }

        public AccountingAccountGraphQLModel AccountingAccount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountingAccount));
                }
            }
        } = new();

        public TreasuryBankMasterTreeDTO Bank
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Bank));
                }
            }
        } = new();

        public bool IsExpanded
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsExpanded));
                }
            }
        }

        public string Provider
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Provider));
                }
            }
        } = string.Empty;

        public PaymentMethodGraphQLModel PaymentMethod
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PaymentMethod));
                }
            }
        } = new();

        public ObservableCollection<TreasuryBankAccountCostCenterDTO> AllowedCostCenters { get; set; } = [];

        public bool AllowContentControlVisibility => true;
    }
}
