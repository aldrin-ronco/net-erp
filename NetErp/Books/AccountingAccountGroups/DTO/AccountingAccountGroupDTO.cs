using Caliburn.Micro;
using NetErp.Books.AccountingAccountGroups.ViewModels;
using NetErp.Books.WithholdingCertificateConfig.ViewModels;

namespace NetErp.Books.AccountingAccountGroups.DTO
{
    public class AccountingAccountGroupDTO : PropertyChangedBase
    {
        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    NotifyOfPropertyChange(nameof(Id));
                }
            }
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    NotifyOfPropertyChange(nameof(Name));
                }
            }
        }

        private string _code = string.Empty;
        public string Code
        {
            get => _code;
            set
            {
                if (_code != value)
                {
                    _code = value;
                    NotifyOfPropertyChange(nameof(Code));
                }
            }
        }

        private string _nature = string.Empty;
        public string Nature
        {
            get => _nature;
            set
            {
                if (_nature != value)
                {
                    _nature = value;
                    NotifyOfPropertyChange(nameof(Nature));
                }
            }
        }

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    NotifyOfPropertyChange(nameof(IsChecked));
                    Context?.NotifyOfPropertyChange(nameof(Context.CanDeleteAccountingAccount));
                }
            }
        }

        public string FullName => $"{Code.Trim()} - {Name.Trim()}";

        public AccountingAccountGroupViewModel? Context { get; set; }
    }

    public class AccountingAccountGroupDetailDTO : PropertyChangedBase
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int GroupId { get; set; }

        private bool? _isChecked = false;
        public bool? IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    NotifyOfPropertyChange(nameof(IsChecked));
                    Context?.NotifyOfPropertyChange(nameof(Context.CanSave));
                }
            }
        }

        public WithholdingCertificateConfigDetailViewModel? Context { get; set; }
    }

    public class AccountingAccountGroupFilterDTO : PropertyChangedBase
    {
        public int Id { get; set; }
        public int AccountingAccountId { get; set; }
        public string AccountingAccountCode { get; set; } = string.Empty;
        public string AccountingAccountName { get; set; } = string.Empty;
        public string FullName => $"{AccountingAccountCode.Trim()} - {AccountingAccountName.Trim()}";
    }

}
