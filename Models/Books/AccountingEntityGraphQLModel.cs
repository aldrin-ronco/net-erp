using Models.DTO.Global;
using Models.Global;
using System.Collections.ObjectModel;
using static Models.Global.GraphQLResponseTypes;

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
        public string PrimaryPhone { get; set; } = string.Empty;
        public string SecondaryPhone { get; set; } = string.Empty;
        public string PrimaryCellPhone { get; set; } = string.Empty;
        public string SecondaryCellPhone { get; set; } = string.Empty;
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
        public string FullName { get; set; } = string.Empty;
        public string TradeName { get; set; } = string.Empty;
        public string SearchName { get; set; } = string.Empty;
        public string TelephonicInformation { get; set; } = string.Empty;
        public string CommercialCode { get; set; } = string.Empty;
        public IdentificationTypeGraphQLModel IdentificationType { get; set; } = new();
        public CountryGraphQLModel Country { get; set; } = new();
        public DepartmentGraphQLModel Department { get; set; } = new();
        public CityGraphQLModel City { get; set; } = new();
        public ObservableCollection<EmailGraphQLModel> Emails { get; set; } = [];
        public DateTime InsertedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
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
        public PageType<IdentificationTypeGraphQLModel> IdentificationTypes { get; set; } = new();
        public PageType<CountryGraphQLModel> Countries { get; set; } = new();
        public AccountingEntityGraphQLModel? AccountingEntity { get; set; }
    }
    
    public class AccountingEntityCreateMessage
    {
        public UpsertResponseType<AccountingEntityGraphQLModel> CreatedAccountingEntity { get; set; } = new();
    }
    public class AccountingEntityDeleteMessage
    {
        public DeleteResponseType DeletedAccountingEntity { get; set; } = new();
    }

    public class AccountingEntityUpdateMessage
    {
        public UpsertResponseType<AccountingEntityGraphQLModel> UpdatedAccountingEntity { get; set; } = new();
    }

}
