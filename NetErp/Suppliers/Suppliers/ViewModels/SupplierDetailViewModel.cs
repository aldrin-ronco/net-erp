using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Dictionaries;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using Models.Books;
using Models.DTO.Global;
using Models.Global;
using Models.Suppliers;
using NetErp.Helpers;
using Services.Billing.DAL.PostgreSQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using static Models.Global.GraphQLResponseTypes;
using Extensions.Global;
using NetErp.Billing.Sellers.ViewModels;
using NetErp.Helpers.GraphQLQueryBuilder;
using Common.Constants;
using System.Security.Policy;
using static Dictionaries.BooksDictionaries;
using DevExpress.Mvvm.Native;
using NetErp.Helpers.Cache;



namespace NetErp.Suppliers.Suppliers.ViewModels
{
    public class SupplierDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Commands

        private ICommand _deleteMailCommand;
        public ICommand DeleteMailCommand
        {
            get
            {
                if (_deleteMailCommand == null) _deleteMailCommand = new RelayCommand(CanRemoveEmail, RemoveEmail);
                return _deleteMailCommand;
            }
        }

        private ICommand _goBackCommand;
        public ICommand GoBackCommand
        {
            get
            {
                if (_goBackCommand is null) _goBackCommand = new RelayCommand(CanGoBack, GoBack);
                return _goBackCommand;
            }
        }

        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync, CanSave);
                return _saveCommand;
            }
        }

        #endregion

        #region Properties
        private readonly IdentificationTypeCache _identificationTypeCache;
        private readonly WithholdingTypeCache _withholdingTypeCache;
        
        private readonly CountryCache _countryCache;
        private readonly IRepository<SupplierGraphQLModel> _supplierService;
        public SupplierViewModel Context { get; private set; }

        Dictionary<string, List<string>> _errors;

        private bool _isBusy = false;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        public Dictionary<char, string> RegimeDictionary => BooksDictionaries.RegimeDictionary;

        private char _selectedRegime = 'R';
        
        [ExpandoPath("accountingEntity.regime")]
        public char SelectedRegime
        {
            get => _selectedRegime;
            set
            {
                if (_selectedRegime != value)
                {
                    _selectedRegime = value;
                    NotifyOfPropertyChange(nameof(SelectedRegime));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int selectedIndexPage = 0;
        public int SelectedIndexPage
        {
            get => selectedIndexPage;
            set
            {
                if (selectedIndexPage != value)
                {
                    selectedIndexPage = value;
                    NotifyOfPropertyChange(nameof(SelectedIndexPage));
                }
            }
        }

        private string _firstName = string.Empty;
        [ExpandoPath("accountingEntity.firstName")]

        public string FirstName
        {
            get
            {
                if (_firstName is null) return string.Empty;
                return _firstName;
            }
            set
            {
                if (_firstName != value)
                {
                    _firstName = value;
                    ValidateProperty(nameof(FirstName), value);
                    NotifyOfPropertyChange(nameof(FirstName));
                    this.TrackChange(nameof(FirstName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _middleName = string.Empty;
        [ExpandoPath("accountingEntity.middleName")]
        public string MiddleName
        {
            get
            {
                if (_middleName is null) return string.Empty;
                return _middleName;
            }
            set
            {
                if (_middleName != value)
                {
                    _middleName = value;
                    NotifyOfPropertyChange(nameof(MiddleName));
                    this.TrackChange(nameof(MiddleName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        
            private string _tradeName = string.Empty;
        [ExpandoPath("accountingEntity.tradeName")]
        public string TradeName
        {
            get
            {
                if (_tradeName is null) return string.Empty;
                return _tradeName;
            }
            set
            {
                if (_tradeName != value)
                {
                    _tradeName = value;
                    NotifyOfPropertyChange(nameof(TradeName));
                    this.TrackChange(nameof(TradeName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        private string _commercialCode = string.Empty;
        [ExpandoPath("accountingEntity.CommercialCode")]
        public string CommercialCode
        {
            get
            {
                if (_commercialCode is null) return string.Empty;
                return _commercialCode;
            }
            set
            {
                if (_commercialCode != value)
                {
                    _commercialCode = value;
                    NotifyOfPropertyChange(nameof(CommercialCode));
                    this.TrackChange(nameof(CommercialCode));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        private string _firstLastName = string.Empty;
        [ExpandoPath("accountingEntity.firstLastName")]
        public string FirstLastName
        {
            get
            {
                if (_firstLastName is null) return string.Empty;
                return _firstLastName;
            }
            set
            {
                if (_firstLastName != value)
                {
                    _firstLastName = value;
                    ValidateProperty(nameof(FirstLastName), value);
                    NotifyOfPropertyChange(nameof(FirstLastName));
                    this.TrackChange(nameof(FirstLastName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _middleLastName = string.Empty;
        [ExpandoPath("accountingEntity.middleLastName")]
        public string MiddleLastName
        {
            get
            {
                if (_middleLastName is null) return string.Empty;
                return _middleLastName;
            }
            set
            {
                if (_middleLastName != value)
                {
                    _middleLastName = value;
                    NotifyOfPropertyChange(nameof(MiddleLastName));
                    this.TrackChange(nameof(MiddleLastName));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _primaryPhone = string.Empty;
        [ExpandoPath("accountingEntity.primaryPhone")]
        public string PrimaryPhone
        {
            get
            {
                if (_primaryPhone is null) return string.Empty;
                return _primaryPhone;
            }
            set
            {
                if (_primaryPhone != value)
                {
                    _primaryPhone = value;
                    ValidateProperty(nameof(PrimaryPhone), value);
                    NotifyOfPropertyChange(nameof(PrimaryPhone));
                    this.TrackChange(nameof(PrimaryPhone));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _secondaryPhone = string.Empty;
        [ExpandoPath("accountingEntity.secondaryPhone")]

        public string SecondaryPhone
        {
            get
            {
                if (_secondaryPhone is null) return string.Empty;
                return _secondaryPhone;
            }
            set
            {
                if (_secondaryPhone != value)
                {
                    _secondaryPhone = value;
                    ValidateProperty(nameof(SecondaryPhone), value);
                    NotifyOfPropertyChange(nameof(SecondaryPhone));
                    this.TrackChange(nameof(SecondaryPhone));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _primaryCellPhone = string.Empty;
        [ExpandoPath("accountingEntity.primaryCellPhone")]

        public string PrimaryCellPhone
        {
            get
            {
                if (_primaryCellPhone is null) return string.Empty;
                return _primaryCellPhone;
            }
            set
            {
                _primaryCellPhone = value;
                ValidateProperty(nameof(PrimaryCellPhone), value);
                NotifyOfPropertyChange(nameof(PrimaryCellPhone));
                this.TrackChange(nameof(PrimaryCellPhone));

                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private string _secondaryCellPhone = string.Empty;
        [ExpandoPath("accountingEntity.secondaryCellPhone")]
        public string SecondaryCellPhone
        {
            get
            {
                if (_secondaryCellPhone is null) return string.Empty;
                return _secondaryCellPhone;
            }
            set
            {
                if (_secondaryCellPhone != value)
                {
                    _secondaryCellPhone = value;
                    ValidateProperty(nameof(SecondaryCellPhone), value);
                    NotifyOfPropertyChange(nameof(SecondaryCellPhone));
                    this.TrackChange(nameof(SecondaryCellPhone));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _address = string.Empty;
        [ExpandoPath("accountingEntity.address")]

        public string Address
        {
            get
            {
                if (_address is null) return string.Empty;
                return _address;
            }
            set
            {
                if (_address != value)
                {
                    _address = value;
                    NotifyOfPropertyChange(nameof(Address));
                    this.TrackChange(nameof(Address));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _businessName = string.Empty;
        [ExpandoPath("accountingEntity.businessName")]
        public string BusinessName
        {
            get
            {
                if (_businessName is null) return string.Empty;
                return _businessName;
            }
            set
            {
                if (_businessName != value)
                {
                    _businessName = value;
                    ValidateProperty(nameof(BusinessName), value);
                    NotifyOfPropertyChange(nameof(BusinessName));
                    this.TrackChange(nameof(BusinessName));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        private decimal _icaWithholdingRate;

        public decimal IcaWithholdingRate
        {
            get
            {
              
                return _icaWithholdingRate;
            }
            set
            {
                if (_icaWithholdingRate != value)
                {
                    _icaWithholdingRate = value;
                    ValidateProperty(nameof(IcaWithholdingRate), value);
                    NotifyOfPropertyChange(nameof(IcaWithholdingRate));
                    this.TrackChange(nameof(IcaWithholdingRate));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        private ObservableCollection<WithholdingTypeDTO> _withholdingTypes;

       // [ExpandoPath("withholdingTypeIds", SerializeAsId = true)]
        public ObservableCollection<WithholdingTypeDTO> WithholdingTypes
        {
            get => _withholdingTypes;
            set
            {
                if (_withholdingTypes != value)
                {
                    _withholdingTypes = value;
                    NotifyOfPropertyChange(nameof(WithholdingTypes));
                }
            }
        }
        private List<int> _withholdingTypeIds;
        public List<int> WithholdingTypeIds 
        {
            get => _withholdingTypeIds;
            set
            {
                if (_withholdingTypeIds != value)
                {
                    _withholdingTypeIds = value;
                    NotifyOfPropertyChange(nameof(WithholdingTypeIds));
                    this.TrackChange(nameof(WithholdingTypeIds));
                }
            }
        }
        private ObservableCollection<IdentificationTypeGraphQLModel> _identificationTypes;
        public ObservableCollection<IdentificationTypeGraphQLModel> IdentificationTypes
        {
            get => _identificationTypes;
            set
            {
                if (_identificationTypes != value)
                {
                    _identificationTypes = value;
                    NotifyOfPropertyChange(nameof(IdentificationTypes));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        private ObservableCollection<CountryGraphQLModel> _countries;
        public ObservableCollection<CountryGraphQLModel> Countries
        {
            get => _countries;
            set
            {
                if (_countries != value)
                {
                    _countries = value;
                    NotifyOfPropertyChange(nameof(Countries));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private IdentificationTypeGraphQLModel _selectedIdentificationType;
        [ExpandoPath("accountingEntity.identificationTypeId", SerializeAsId = true)]
        public IdentificationTypeGraphQLModel SelectedIdentificationType
        {
            get => _selectedIdentificationType;
            set
            {
                if (_selectedIdentificationType != value)
                {
                    _selectedIdentificationType = value;
                    NotifyOfPropertyChange(nameof(SelectedIdentificationType));
                    this.TrackChange(nameof(SelectedIdentificationType));

                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperty(nameof(IdentificationNumber), _identificationNumber);
                    if (IsNewRecord)
                    {
                        _ = this.SetFocus(nameof(IdentificationNumber));
                    }
                }
            }
        }

        

        private CountryGraphQLModel _selectedCountry;
        [ExpandoPath("accountingEntity.countryId", SerializeAsId = true)]

        public CountryGraphQLModel SelectedCountry
        {
            get => _selectedCountry;
            set
            {
                if (_selectedCountry != value)
                {
                    _selectedCountry = value;
                    NotifyOfPropertyChange(nameof(SelectedCountry));
                    this.TrackChange(nameof(SelectedCountry));

                    if (_selectedCountry != null)
                    {
                        SelectedDepartment = SelectedCountry.Departments.FirstOrDefault(x => x.CountryId == _selectedCountry.Id);
                        NotifyOfPropertyChange(nameof(SelectedDepartment));
                    }
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private DepartmentGraphQLModel _selectedDepartment;
        [ExpandoPath("accountingEntity.departmentId", SerializeAsId = true)]

        public DepartmentGraphQLModel SelectedDepartment
        {
            get => _selectedDepartment;
            set
            {
                if (_selectedDepartment != value)
                {
                    _selectedDepartment = value;
                    NotifyOfPropertyChange(nameof(SelectedDepartment));
                    this.TrackChange(nameof(SelectedDepartment));

                    if (_selectedDepartment != null)
                    {
                        SelectedCityId = SelectedDepartment.Cities.FirstOrDefault().Id;
                        NotifyOfPropertyChange(nameof(SelectedCityId));
                    }
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int _selectedCityId;
        [ExpandoPath("accountingEntity.cityId")]

        public int SelectedCityId
        {
            get => _selectedCityId;
            set
            {
                if (_selectedCityId != value)
                {
                    _selectedCityId = value;
                    NotifyOfPropertyChange(nameof(SelectedCityId));
                    this.TrackChange(nameof(SelectedCityId));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        private int? _icaAccountingAccountId;
        public int? IcaAccountingAccountId
        {
            get => _icaAccountingAccountId;
            set
            {
                if (_icaAccountingAccountId != value)
                {
                    _icaAccountingAccountId = value;
                    NotifyOfPropertyChange(nameof(IcaAccountingAccountId));
                    this.TrackChange(nameof(IcaAccountingAccountId));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        
        private string _identificationNumber = string.Empty;
        [ExpandoPath("accountingEntity.identificationNumber")]
        public string IdentificationNumber
        {
            get
            {
                if (_identificationNumber is null) return string.Empty;
                return _identificationNumber;
            }
            set
            {
                if (_identificationNumber != value)
                {
                    _identificationNumber = value;
                    ValidateProperty(nameof(IdentificationNumber), value);
                    NotifyOfPropertyChange(nameof(IdentificationNumber));
                    NotifyOfPropertyChange(nameof(VerificationDigit));
                    this.TrackChange(nameof(IdentificationNumber));
                    this.TrackChange(nameof(VerificationDigit));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _verificationDigit;
        [ExpandoPath("accountingEntity.VerificationDigit")]
        public string VerificationDigit
        {
            set
            {
                if (_verificationDigit != value)
                {
                    _verificationDigit = value;
                  
                    NotifyOfPropertyChange(nameof(VerificationDigit));
                    this.TrackChange(nameof(VerificationDigit));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
            get => !IsNewRecord
                    ? _verificationDigit
                    : SelectedIdentificationType == null
                    ? string.Empty
                    : IdentificationNumber.Trim().Length >= SelectedIdentificationType.MinimumDocumentLength
                    ? IdentificationNumber.GetVerificationDigit()
                    : string.Empty;
        }
        private bool _withholdingAppliesOnAnyAmount = false;
        public bool WithholdingAppliesOnAnyAmount
        {
            get => _withholdingAppliesOnAnyAmount;
            set
            {
                if (_withholdingAppliesOnAnyAmount != value)
                {
                    _withholdingAppliesOnAnyAmount = value;
                    NotifyOfPropertyChange(nameof(WithholdingAppliesOnAnyAmount));
                    this.TrackChange(nameof(WithholdingAppliesOnAnyAmount));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }


        private bool _isTaxFree = false;
        public bool IsTaxFree
        {
            get => _isTaxFree;
            set
            {
                if (_isTaxFree != value)
                {
                    _isTaxFree = value;
                    NotifyOfPropertyChange(nameof(IsTaxFree));
                    this.TrackChange(nameof(IsTaxFree));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        
        public bool CanRemoveEmail(object p) => true;
        public bool CanAddEmail => !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(EmailDescription) && Email.IsValidEmail();

        private string _emailDescription;
        public string EmailDescription
        {
            get
            {
                if (_emailDescription is null) return string.Empty;
                return _emailDescription;
            }
            set
            {
                if (_emailDescription != value)
                {
                    _emailDescription = value;
                    NotifyOfPropertyChange(nameof(EmailDescription));
                    NotifyOfPropertyChange(nameof(CanAddEmail));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _email;
        public string Email
        {
            get
            {
                if (_email is null) return string.Empty;
                return _email;
            }
            set
            {
                if (_email != value)
                {
                    _email = value;
                    NotifyOfPropertyChange(nameof(Email));
                    NotifyOfPropertyChange(nameof(CanAddEmail));
                }
            }
        }

        private ObservableCollection<EmailDTO> _emails = new ObservableCollection<EmailDTO>();
        [ExpandoPath("accountingEntity.emails")]
        public ObservableCollection<EmailDTO> Emails
        {
            get => _emails;
            set
            {
                if (_emails != value)
                {
                    if (_emails != value)
                    {
                        // Desuscribirse del anterior si existe
                        if (_emails != null)
                        {
                            _emails.CollectionChanged -= Emails_CollectionChanged;
                        }

                        _emails = value;

                        // Suscribirse al nuevo
                        if (_emails != null)
                        {
                            _emails.CollectionChanged += Emails_CollectionChanged;
                        }

                        NotifyOfPropertyChange(nameof(Emails));
                        this.TrackChange(nameof(Emails));
                        NotifyOfPropertyChange(nameof(CanSave));
                    }
                }
            }
        }
        private void Emails_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Se dispara cuando se añade, elimina o modifica un elemento
            this.TrackChange(nameof(Emails));
            NotifyOfPropertyChange(nameof(CanSave));
        }

        public void CheckBoxClicked(System.Windows.RoutedEventArgs eventArgs)
        {
            WithholdingTypeIds = [.. WithholdingTypes.Where(f => f.IsSelected == true).Select(s => s.Id).ToList()];
            this.TrackChange(nameof(WithholdingTypeIds));
            NotifyOfPropertyChange(nameof(WithholdingTypeIds));
            NotifyOfPropertyChange(nameof(CanSave));

        }

        private EmailDTO _selectedEmail;
        public EmailDTO SelectedEmail
        {
            get => _selectedEmail;
            set
            {
                if (_selectedEmail != value)
                {
                    _selectedEmail = value;
                    NotifyOfPropertyChange(nameof(SelectedEmail));
                }
            }
        }

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
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        private BooksDictionaries.CaptureTypeEnum _selectedCaptureType;
        [ExpandoPath("accountingEntity.captureType")]
        public BooksDictionaries.CaptureTypeEnum SelectedCaptureType
        {
            get => _selectedCaptureType;
            set
            {
                if (_selectedCaptureType != value)
                {
                    _selectedCaptureType = value;
                    NotifyOfPropertyChange(() => SelectedCaptureType);
                    NotifyOfPropertyChange(() => CaptureInfoAsPN);
                    NotifyOfPropertyChange(() => CaptureInfoAsPJ);
                    this.TrackChange(nameof(SelectedCaptureType));

                    if (CaptureInfoAsPN)
                    {
                        BusinessName = string.Empty;
                        ClearErrors(nameof(BusinessName));
                        this.TrackChange(nameof(BusinessName));
                        ValidateProperty(nameof(FirstName), FirstName);
                        ValidateProperty(nameof(FirstLastName), FirstLastName);

                    }
                    if (CaptureInfoAsPJ)
                    {
                        FirstName = string.Empty;
                        FirstLastName = string.Empty;
                        TradeName = string.Empty;
                        MiddleLastName = string.Empty;
                        MiddleName = string.Empty;
                        this.TrackChange(nameof(FirstName));
                        this.TrackChange(nameof(FirstLastName));
                        this.TrackChange(nameof(TradeName));
                        this.TrackChange(nameof(MiddleLastName));
                        this.TrackChange(nameof(MiddleName));

                        ClearErrors(nameof(FirstName));
                        ClearErrors(nameof(FirstLastName));
                        ValidateProperty(nameof(BusinessName), BusinessName);
                    }
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperties();
                    if (string.IsNullOrEmpty(IdentificationNumber))
                    {
                        _ = this.SetFocus(nameof(IdentificationNumber));
                    }
                    else
                    {
                        if (CaptureInfoAsPN)
                        {
                            _ = this.SetFocus(nameof(FirstName));
                        }
                        else
                        {
                            _ = this.SetFocus(nameof(BusinessName));
                        }

                    }
                }
            }
        }
        private ObservableCollection<AccountingAccountGraphQLModel> _accountingAccounts;

        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccounts
        {
            get { return _accountingAccounts; }
            set
            {
                if (_accountingAccounts != value)
                {
                    _accountingAccounts = value;
                    NotifyOfPropertyChange(nameof(AccountingAccounts));
                }
            }
        }
        private ObservableCollection<AccountingAccountGraphQLModel> _accountingAccountDevolutions;

        public bool CaptureInfoAsPN => SelectedCaptureType.Equals(BooksDictionaries.CaptureTypeEnum.PN);
        public bool CaptureInfoAsPJ => SelectedCaptureType.Equals(BooksDictionaries.CaptureTypeEnum.PJ);

        public bool IsNewRecord => Id == 0;

        public bool CanSave
        {
            get
            {
                // Si esta ocupado
                if (IsBusy) return false;
                // Debe haber definido el respectivo tipo de identificacion
                if (SelectedIdentificationType == null) return false;
                // Si el documento de identidad esta vacion o su longitud es inferior a la longitud minima definida para ese tipo de documento
                if (string.IsNullOrEmpty(IdentificationNumber.Trim()) || IdentificationNumber.Length < SelectedIdentificationType.MinimumDocumentLength) return false;
                // El digito de verificacion debe estar presente en caso de ser requerido
                if (SelectedIdentificationType.HasVerificationDigit && string.IsNullOrEmpty(VerificationDigit)) return false;
                // Si la captura de datos es del tipo razon social
                if (CaptureInfoAsPJ && string.IsNullOrEmpty(BusinessName)) return false;
                // Si la captura de informacion es del tipo persona natural, los datos obligados son primer nombre y primer apellido
                if (CaptureInfoAsPN && (string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(FirstLastName))) return false;
                // Si el control de errores por propiedades tiene algun error
                if (!this.HasChanges()) return false;
                // Si el control de errores por propiedades tiene algun error
                return _errors.Count <= 0;
            }
        }

        #endregion

        #region Methods

        public void EndRowEditing()
        {
            try
            {
                NotifyOfPropertyChange(nameof(Emails));
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
        }

        public void AddEmail()
        {
            try
            {
                EmailDTO email = new EmailDTO() { Description = EmailDescription, Email = Email};
                Email = string.Empty;
                EmailDescription = string.Empty;
                Emails.Add(email);
                _ = this.SetFocus(nameof(EmailDescription));
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
        }

        public void PhoneInputLostFocus(FrameworkElement element)
        {
            switch (element.Name.ToLower())
            {
                case "primaryphone":
                    PrimaryPhone = PrimaryPhone.ToPhoneFormat("### ## ##");
                    break;
                case "secondaryphone":
                    SecondaryPhone = SecondaryPhone.ToPhoneFormat("### ## ##");
                    break;
                case "primarycellphone":
                    PrimaryCellPhone = PrimaryCellPhone.ToPhoneFormat("### ### ## ##");
                    break;
                case "secondarycellphone":
                    SecondaryCellPhone = SecondaryCellPhone.ToPhoneFormat("### ### ## ##");
                    break;
                default:
                    break;
            }
        }

        public void GoBack(object p)
        {
            _ = Task.Run(() => Context.ActivateMasterView());
            CleanUpControls();
        }

        public bool CanGoBack(object p)
        {
            return !IsBusy;
        }

        public void CleanUpControls()
        {
            List<WithholdingTypeDTO> retentionList = [];
            Id = 0; // Por medio del Id se establece si es un nuevo registro o una actualizacion
            SelectedRegime = 'R';
            IdentificationNumber = string.Empty;
            VerificationDigit = string.Empty;
            SelectedIdentificationType = IdentificationTypes.FirstOrDefault(x => x.Code == "31"); // 31 es NIT
            SelectedCaptureType = BooksDictionaries.CaptureTypeEnum.Undefined;
            BusinessName = string.Empty;
            FirstName = string.Empty;
            MiddleName = string.Empty;
            FirstLastName = string.Empty;
            MiddleLastName = string.Empty;
            PrimaryPhone = string.Empty;
            SecondaryPhone = string.Empty;
            PrimaryCellPhone = string.Empty;
            SecondaryCellPhone = string.Empty;
            Address = string.Empty;
            Emails = new ObservableCollection<EmailDTO>();
            SelectedCountry = Countries.FirstOrDefault(x => x.Code == "169"); // 169 es el cóodigo de colombia
            SelectedDepartment = SelectedCountry.Departments.FirstOrDefault(x => x.Code == "01"); // 08 es el código del atlántico
            SelectedCityId = SelectedDepartment.Cities.FirstOrDefault(x => x.Code == "001").Id; // 001 es el Codigo de Barranquilla
            foreach (WithholdingTypeDTO retention in WithholdingTypes)
            {
                retentionList.Add(new WithholdingTypeDTO()
                {
                    Id = retention.Id,
                    Name = retention.Name,
                    IsSelected = false
                });
            }
            WithholdingTypes = new ObservableCollection<WithholdingTypeDTO>(retentionList);
        }

        public void CleanUpControlsForNew()
        {
            List<WithholdingTypeDTO> retentionList = [];
            Id = 0; // Por medio del Id se establece si es un nuevo registro o una actualizacion
            SelectedRegime = 'R';
            IdentificationNumber = string.Empty;
            VerificationDigit = string.Empty;
            SelectedIdentificationType = IdentificationTypes.FirstOrDefault(x => x.Code == "31"); // 31 es NIT
            SelectedCaptureType = BooksDictionaries.CaptureTypeEnum.PN;
            BusinessName = string.Empty;
            FirstName = string.Empty;
            MiddleName = string.Empty;
            FirstLastName = string.Empty;
            MiddleLastName = string.Empty;
            PrimaryPhone = string.Empty;
            SecondaryPhone = string.Empty;
            PrimaryCellPhone = string.Empty;
            SecondaryCellPhone = string.Empty;
            Address = string.Empty;
            Emails = new ObservableCollection<EmailDTO>();
            SelectedCountry = Countries.FirstOrDefault(x => x.Code == Constant.DefaultCountryCode); // 169 es el cóodigo de colombia
            SelectedDepartment = SelectedCountry.Departments.FirstOrDefault(x => x.Code == Constant.DefaultDepartmentCode); // 08 es el código del atlántico
            SelectedCityId = SelectedDepartment.Cities.FirstOrDefault(x => x.Code == Constant.DefaultCityCode).Id; // 001 es el Codigo de Barranquilla
            foreach (WithholdingTypeDTO retention in WithholdingTypes)
            {
                retentionList.Add(new WithholdingTypeDTO()
                {
                    Id = retention.Id,
                    Name = retention.Name,
                    IsSelected = false
                });
            }
            WithholdingTypes = new ObservableCollection<WithholdingTypeDTO>(retentionList);
            IdentificationTypes = _identificationTypeCache.Items;
            this.AcceptChanges();
            this.SeedValue(nameof(SelectedCaptureType), SelectedCaptureType);
            this.SeedValue(nameof(SelectedCountry), SelectedCountry);
            this.SeedValue(nameof(SelectedDepartment), SelectedDepartment);
            this.SeedValue(nameof(SelectedIdentificationType), SelectedIdentificationType);

            this.SeedValue(nameof(SelectedCityId), SelectedCityId);
            this.SeedValue(nameof(SelectedRegime), SelectedRegime);
            this.SeedValue(nameof(WithholdingAppliesOnAnyAmount), false);
            this.SeedValue(nameof(IsTaxFree), false);


        }


        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                UpsertResponseType<SupplierGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }
                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new SupplierCreateMessage() { CreatedSupplier = result }
                        : new SupplierUpdateMessage() { UpdatedSupplier = result }
                );
                await Context.ActivateMasterView();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"\r\n{graphQLError.Errors[0].Message}\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
           
        }

        public async Task<UpsertResponseType<SupplierGraphQLModel>> ExecuteSaveAsync()
        {

            try
            {

                var transformers = new Dictionary<string, Func<object?, object?>>
                {
                    [nameof(SupplierDetailViewModel.Emails)] = item =>
                    {
                        var email = (EmailGraphQLModel)item!;
                        return new
                        {
                            description = email.Description,
                            email = email.Email
                        };
                    }
                };
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: IsNewRecord ? "createResponseInput" : "updateResponseData", transformers);
                string query = IsNewRecord ? GetCreateQuery() : GetUpdateQuery();
                if (IsNewRecord)
                {


                    UpsertResponseType<SupplierGraphQLModel> supplierCreated = await _supplierService.CreateAsync<UpsertResponseType<SupplierGraphQLModel>>(query, variables);
                    return supplierCreated; //este
                }
                else
                {
                    variables.updateResponseId = Id;

                    UpsertResponseType<SupplierGraphQLModel> updatedSupplier = await _supplierService.UpdateAsync<UpsertResponseType<SupplierGraphQLModel>>(query, variables);
                    return updatedSupplier;
                }
            }
            catch (Exception)
            {
                throw;
            }
          
        }
        public string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<SupplierGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "supplier", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.IsTaxFree)
                   )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateSupplierInput!");

            var fragment = new GraphQLQueryFragment("createSupplier", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<SupplierGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "supplier", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.IsTaxFree)
                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateSupplierInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateSupplier", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public void RemoveEmail(object p)
        {
            try
            {
                if (ThemedMessageBox.Show("Confirme ...", $"¿ Confirma que desea eliminar el email : {SelectedEmail.Email} ?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                if (SelectedEmail != null)
                {
                    EmailDTO? emailToDelete = Emails.FirstOrDefault(email => email.Id == SelectedEmail.Id);
                    if (emailToDelete is null) return;
                    Emails.Remove(emailToDelete);
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
        }

        public SupplierDetailViewModel(
            SupplierViewModel context,
            IRepository<SupplierGraphQLModel> supplierService,
            ObservableCollection<AccountingAccountGraphQLModel> accountingAccounts,
            IdentificationTypeCache identificationTypeCache,
            CountryCache countryCache,
            WithholdingTypeCache withholdingTypeCache)
        {
            _errors = new Dictionary<string, List<string>>();
            Context = context;
            AccountingAccounts = accountingAccounts;
            _supplierService = supplierService;
            _identificationTypeCache = identificationTypeCache;
            _countryCache = countryCache;
            _withholdingTypeCache = withholdingTypeCache;

            var joinable = new JoinableTaskFactory(new JoinableTaskContext());
            joinable.Run(async () => await InitializeAsync());
        }

        public async Task InitializeAsync()
        {
            await Task.WhenAll(
                   _identificationTypeCache.EnsureLoadedAsync(),
                   _countryCache.EnsureLoadedAsync(),
                   _withholdingTypeCache.EnsureLoadedAsync()
               );
            IdentificationTypes = _identificationTypeCache.Items;
            Countries = _countryCache.Items;
            WithholdingTypes = Context.AutoMapper.Map<ObservableCollection<WithholdingTypeDTO>>(_withholdingTypeCache.Items);
        }

        

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            ValidateProperties();
            _ = Application.Current.Dispatcher.BeginInvoke(() =>
            {
                SelectedIndexPage = 0; // Selecciona el primer TAB page
                _ = IsNewRecord
                      ? Application.Current.Dispatcher.BeginInvoke(new System.Action(() => this.SetFocus(nameof(IdentificationNumber))), DispatcherPriority.Render)
                      : CaptureInfoAsPN
                          ? Application.Current.Dispatcher.BeginInvoke(new System.Action(() => this.SetFocus(nameof(FirstName))), DispatcherPriority.Render)
                          : Application.Current.Dispatcher.BeginInvoke(new System.Action(() => this.SetFocus(nameof(BusinessName))), DispatcherPriority.Render);
            });
        }
        public async Task<SupplierGraphQLModel> LoadDataForEditAsync(int id)
        {
            try
            {
                string query = GetLoadSupplierByIdQuery();

                dynamic variables = new ExpandoObject();


                variables.singleItemResponseId = id;

                var Supplier = await _supplierService.FindByIdAsync(query, variables);

                // Poblar el ViewModel con los datos del supplier (sin bloquear UI thread)
                PopulateFromSupplier(Supplier);

                return Supplier;
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
        public void PopulateFromSupplier(SupplierGraphQLModel supplier)
        {
            // Propiedades básicas del supplier
      
            Id = supplier.Id;
            VerificationDigit = supplier.AccountingEntity.VerificationDigit;
            SelectedCaptureType = (CaptureTypeEnum)Enum.Parse(typeof(CaptureTypeEnum), supplier.AccountingEntity.CaptureType);
            BusinessName = supplier.AccountingEntity.BusinessName;
            SelectedIdentificationType = IdentificationTypes.FirstOrDefault(x => x.Code == supplier.AccountingEntity.IdentificationType.Code);
            IdentificationNumber = supplier.AccountingEntity.IdentificationNumber;
            FirstName = supplier.AccountingEntity.FirstName;
            MiddleName = supplier.AccountingEntity.MiddleName;
            FirstLastName = supplier.AccountingEntity.FirstLastName;
            MiddleLastName = supplier.AccountingEntity.MiddleLastName;
            PrimaryPhone = supplier.AccountingEntity.PrimaryPhone;
            SecondaryPhone = supplier.AccountingEntity.SecondaryPhone;
            PrimaryCellPhone = supplier.AccountingEntity.PrimaryCellPhone;
            SecondaryCellPhone = supplier.AccountingEntity.SecondaryCellPhone;
            Emails = supplier.AccountingEntity.Emails is null ? new ObservableCollection<EmailDTO>() : Context.AutoMapper.Map<ObservableCollection<EmailDTO>>(supplier.AccountingEntity.Emails);
            SelectedCountry = Countries.FirstOrDefault(c => c.Id == supplier.AccountingEntity.Country.Id);
            SelectedDepartment = SelectedCountry.Departments.FirstOrDefault(d => d.Id == supplier.AccountingEntity.Department.Id);
            SelectedCityId = supplier.AccountingEntity.City.Id;
            Address = supplier.AccountingEntity.Address;
            IsTaxFree = supplier.IsTaxFree;
            IcaWithholdingRate = supplier.IcaWithholdingRate;
            IcaAccountingAccountId = supplier.IcaAccountingAccount?.Id;
            TradeName = supplier.AccountingEntity.TradeName;
            WithholdingAppliesOnAnyAmount = supplier.WithholdingAppliesOnAnyAmount;
            List<WithholdingTypeDTO> withholdingTypes = [];

            foreach (WithholdingTypeDTO withholdingType in WithholdingTypes)
            {
                bool exist = !(supplier.WithholdingTypes is null) && supplier.WithholdingTypes.Any(x => x.Id == withholdingType.Id);
                withholdingTypes.Add(new WithholdingTypeDTO()
                {
                    Id = withholdingType.Id,
                    Name = withholdingType.Name,
                    IsSelected = exist
                });
            }
            WithholdingTypes = new System.Collections.ObjectModel.ObservableCollection<WithholdingTypeDTO>(withholdingTypes);
        }
        public string GetLoadSupplierByIdQuery()
        {
            var supplierFields = FieldSpec<SupplierGraphQLModel>
             .Create()

                 .Field(e => e.Id)
                 .Field(e => e.IsTaxFree)
                 .Field(e => e.IcaWithholdingRate)
                 .Field(e => e.WithholdingAppliesOnAnyAmount)
                 
                 .Select(e => e.AccountingEntity, acc => acc
                           .Field(c => c.Id)
                           .Field(c => c.VerificationDigit)
                           .Field(c => c.IdentificationNumber)
                           .Field(c => c.FirstName)
                           .Field(c => c.MiddleName)
                           .Field(c => c.FirstLastName)
                           .Field(c => c.MiddleLastName)
                           .Field(c => c.SearchName)
                           .Field(c => c.TradeName)
                           .Field(c => c.BusinessName)
                           .Field(c => c.PrimaryPhone)
                           .Field(c => c.SecondaryPhone)
                           .Field(c => c.PrimaryCellPhone)
                           .Field(c => c.SecondaryCellPhone)
                           .Field(c => c.Address)
                           .Field(c => c.CaptureType)
                           .Field(c => c.TelephonicInformation)
                           .Select(e => e.IdentificationType, co => co
                                   .Field(x => x.Id)
                                   .Field(x => x.Code)
                               )
                           .Select(e => e.Country, co => co
                                   .Field(x => x.Id)
                                   .Field(x => x.Code)
                               )
                           .Select(e => e.City, co => co
                                   .Field(x => x.Id)
                                   .Field(x => x.Code)
                               )
                           .Select(e => e.Department, co => co
                                   .Field(x => x.Id)
                                   .Field(x => x.Code)
                                   )
                           .SelectList(e => e.Emails, co => co
                                   .Field(x => x.Id)
                                   .Field(x => x.Description)
                                   .Field(x => x.Email)
                                   .Field(x => x.isElectronicInvoiceRecipient)
                                   )
                           )
                 .Select(e => e.IcaAccountingAccount, acc => acc
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                        .Field(c => c.Code)
                 )
                 .SelectList(e => e.WithholdingTypes, acc => acc
                 .Field (e => e.Id)
                 .Field(e => e.Name)
                 )
                  .Build();
            var supplierIdParameter = new GraphQLQueryParameter("id", "ID!");

            var supplierFragment = new GraphQLQueryFragment("supplier", [supplierIdParameter], supplierFields, "SingleItemResponse");

            var builder = new GraphQLQueryBuilder([supplierFragment]);

            return builder.GetQuery();

        }
        #endregion

        #region Validaciones

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string propertyName)
        {
            return string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName) ? null : (IEnumerable)_errors[propertyName];
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                RaiseErrorsChanged(propertyName);
            }
        }
        private void ValidateProperty(string propertyName, decimal value)
        {
            try
            {
                ClearErrors(propertyName);
                switch (propertyName)
                {
                    case nameof(IcaWithholdingRate):
                        if (value < 0 || value > 100) AddError(propertyName, "El valor debe estar entre 0 y 10");
                        break;
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }
        private void ValidateProperty(string propertyName, string value)
        {
            try
            {
                ClearErrors(propertyName);
                if (propertyName.Contains("Phone"))
                {
                    // Remover espacios a la cadena
                    value = value.Replace(" ", "").Replace(Convert.ToChar(9).ToString(), "");
                    // Remover , = 44 y ; = 59
                    value = value.Replace(Convert.ToChar(44).ToString(), "").Replace(Convert.ToChar(59).ToString(), "");
                    // Remover - = 45 y _ = 95
                    value = value.Replace(Convert.ToChar(45).ToString(), "").Replace(Convert.ToChar(95).ToString(), "").Trim();
                }
                switch (propertyName)
                {
                    case nameof(IdentificationNumber):
                        if (string.IsNullOrEmpty(value) || value.Length < SelectedIdentificationType?.MinimumDocumentLength) AddError(propertyName, "El número de identificación no puede estar vacío");
                        break;
                    case nameof(FirstName):
                        if (string.IsNullOrEmpty(value) && CaptureInfoAsPN) AddError(propertyName, "El primer nombre no puede estar vacío");
                        break;
                    case nameof(FirstLastName):
                        if (string.IsNullOrEmpty(value) && CaptureInfoAsPN) AddError(propertyName, "El primer apellido no puede estar vacío");
                        break;
                    case nameof(BusinessName):
                        if (string.IsNullOrEmpty(value) && CaptureInfoAsPJ) AddError(propertyName, "La razón social no puede estar vacía");
                        break;
                    case nameof(PrimaryPhone):
                        if (value.Length != 7 && !string.IsNullOrEmpty(PrimaryPhone)) AddError(propertyName, "El número de teléfono debe contener 7 digitos");
                        break;
                    case nameof(SecondaryPhone):
                        if (value.Length != 7 && !string.IsNullOrEmpty(SecondaryPhone)) AddError(propertyName, "El número de teléfono debe contener 7 digitos");
                        break;
                    case nameof(PrimaryCellPhone):
                        if (value.Length != 10 && !string.IsNullOrEmpty(PrimaryCellPhone)) AddError(propertyName, "El número de teléfono celular debe contener 10 digitos");
                        break;
                    case nameof(SecondaryCellPhone):
                        if (value.Length != 10 && !string.IsNullOrEmpty(SecondaryCellPhone)) AddError(propertyName, "El número de teléfono celular debe contener 10 digitos");
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private void ValidateProperties()
        {
            if (CaptureInfoAsPJ) ValidateProperty(nameof(BusinessName), BusinessName);
            if (CaptureInfoAsPN)
            {
                ValidateProperty(nameof(FirstName), FirstName);
                ValidateProperty(nameof(FirstLastName), FirstLastName);
                ValidateProperty(nameof(IdentificationNumber), IdentificationNumber);
            }
        }

        #endregion
    }
}
