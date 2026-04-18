using Caliburn.Micro;
using Models.Books;
using NetErp.Treasury.Masters.ViewModels;
using System.Collections.ObjectModel;

namespace NetErp.Treasury.Masters.DTO
{
    public class TreasuryBankMasterTreeDTO : PropertyChangedBase, ITreasuryTreeMasterSelectedItem
    {
        public int Id { get; set; }

        public TreasuryRootMasterViewModel? Context { get; set; }

        public string Code
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Code));
                }
            }
        } = string.Empty;

        public AccountingEntityDTO AccountingEntity
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountingEntity));
                }
            }
        } = new();

        public string PaymentMethodPrefix
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PaymentMethodPrefix));
                }
            }
        } = string.Empty;

        public ObservableCollection<TreasuryBankAccountMasterTreeDTO> BankAccounts
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(BankAccounts));
                }
            }
        } = [];

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

        public BankDummyDTO? DummyParent
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DummyParent));
                }
            }
        }

        public bool AllowContentControlVisibility => true;
    }
}
