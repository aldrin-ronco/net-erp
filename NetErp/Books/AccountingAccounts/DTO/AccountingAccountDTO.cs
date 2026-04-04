using Caliburn.Micro;
using NetErp.Books.AccountingAccounts.ViewModels;
using System.Collections.ObjectModel;

namespace NetErp.Books.AccountingAccounts.DTO
{
    public class AccountingAccountDTO : PropertyChangedBase
    {
        public AccountPlanMasterViewModel? Context { get; set; }

        public bool IsDummyChild
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsDummyChild));
                }
            }
        }

        public bool IsAuxiliary => Code.Trim().Length >= 8;

        public bool IsExpanded
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsExpanded));
                    OnIsExpandedChanged();
                }
            }
        }

        private void OnIsExpandedChanged()
        {
            if (Childrens != null && IsExpanded && Childrens.Count > 0)
            {
                if (Childrens[0].IsDummyChild)
                    Context?.LoadChildren(this, Context.Accounts);
            }
        }

        public bool IsSelected
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsSelected));
                }
            }
        }

        public int Id
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Id));
                }
            }
        }

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

        public ObservableCollection<AccountingAccountDTO> Childrens
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Childrens));
                }
            }
        } = [];

        public override string ToString()
        {
            return $"{Code} - {Name}";
        }
    }
}
