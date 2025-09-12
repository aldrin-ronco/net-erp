using Models.DTO.Global;
using Models.Global;
using System.Collections.ObjectModel;

namespace Models.Books
{
    public class AccountingEntityGraphQLModel
    {
        Dictionary<char, string> RegimeDictionary => Dictionaries.BooksDictionaries.RegimeDictionary;
        public int Id { get; set; } = 0;
        public string IdentificationNumber { get; set; } = string.Empty;
        public string VerificationDigit { get; set; } = string.Empty;
        public string IdentificationNumberWithVerificationDigit
        {
            get
            {
                return $"{this.IdentificationNumber}{(string.IsNullOrEmpty(this.VerificationDigit) ? "" : "-" + this.VerificationDigit)}";
            }
        }
        public string CaptureType { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string FirstLastName { get; set; } = string.Empty;
        public string MiddleLastName { get; set; } = string.Empty;
        public string Phone1 { get; set; } = string.Empty;
        public string Phone2 { get; set; } = string.Empty;
        public string CellPhone1 { get; set; } = string.Empty;
        public string CellPhone2 { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public char Regime 
        {
            get;
            set; 
        } = 'R';

        public string RegimeResolve 
        {
            get 
            {
                return RegimeDictionary[Regime];
            }
        }
        public string FullName { get; set; }
        public string TradeName { get; set; } = string.Empty;
        public string SearchName { get; set; }
        public string TelephonicInformation { get; set; } = string.Empty;
        public string CommercialCode { get; set; } = string.Empty;
        public IdentificationTypeGraphQLModel IdentificationType { get; set; }
        public CountryGraphQLModel Country { get; set; }
        public DepartmentGraphQLModel Department { get; set; }
        public CityGraphQLModel City { get; set; }
        public ObservableCollection<EmailDTO> Emails { get; set; }

        public override string ToString()
        {
            return this.SearchName;
        }
    }
    public class AccountingEntityDTO : AccountingEntityGraphQLModel
    {
        private bool _isChecked;
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                }
            }
        }
    }
    public class AccountingEntityDataContext
    {
        public IEnumerable<IdentificationTypeGraphQLModel> IdentificationTypes { get; set; } = [];
        public IEnumerable<CountryGraphQLModel> Countries { get; set; } = [];
    }
    
    public class AccountingEntityCreateMessage
    {
        public AccountingEntityGraphQLModel CreatedAccountingEntity { get; set; } = new();
    }
    public class AccountingEntityDeleteMessage
    {
        public AccountingEntityGraphQLModel DeletedAccountingEntity { get; set; } = new();
    }

    public class AccountingEntityUpdateMessage
    {
        public AccountingEntityGraphQLModel UpdatedAccountingEntity { get; set; } = new();
    }

}
