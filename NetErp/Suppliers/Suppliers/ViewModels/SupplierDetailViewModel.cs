using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Dictionaries;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _middleName = string.Empty;
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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _firstLastName = string.Empty;
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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _middleLastName = string.Empty;
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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _primaryPhone = string.Empty;
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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _secondaryPhone = string.Empty;
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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _primaryCellPhone = string.Empty;
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
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private string _secondaryCellPhone = string.Empty;
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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _address = string.Empty;
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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _businessName = string.Empty;
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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private ObservableCollection<WithholdingTypeDTO> _withholdingTypes;
        public ObservableCollection<WithholdingTypeDTO> WithholdingTypes
        {
            get => _withholdingTypes;
            set
            {
                if (_withholdingTypes != value)
                {
                    _withholdingTypes = value;
                    NotifyOfPropertyChange(nameof(WithholdingTypes));
                    NotifyOfPropertyChange(nameof(CanSave));
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
        public IdentificationTypeGraphQLModel SelectedIdentificationType
        {
            get => _selectedIdentificationType;
            set
            {
                if (_selectedIdentificationType != value)
                {
                    _selectedIdentificationType = value;
                    NotifyOfPropertyChange(nameof(SelectedIdentificationType));
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
        public CountryGraphQLModel SelectedCountry
        {
            get => _selectedCountry;
            set
            {
                if (_selectedCountry != value)
                {
                    _selectedCountry = value;
                    NotifyOfPropertyChange(nameof(SelectedCountry));
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
        public DepartmentGraphQLModel SelectedDepartment
        {
            get => _selectedDepartment;
            set
            {
                if (_selectedDepartment != value)
                {
                    _selectedDepartment = value;
                    NotifyOfPropertyChange(nameof(SelectedDepartment));
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
        public int SelectedCityId
        {
            get => _selectedCityId;
            set
            {
                if (_selectedCityId != value)
                {
                    _selectedCityId = value;
                    NotifyOfPropertyChange(nameof(SelectedCityId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _identificationNumber = string.Empty;
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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _verificationDigit;
        public string VerificationDigit
        {
            set
            {
                if (_verificationDigit != value)
                {
                    _verificationDigit = value;
                    NotifyOfPropertyChange(nameof(VerificationDigit));
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
        public ObservableCollection<EmailDTO> Emails
        {
            get => _emails;
            set
            {
                if (_emails != value)
                {
                    _emails = value;
                    NotifyOfPropertyChange(nameof(Emails));
                    NotifyOfPropertyChange(nameof(FilteredEmails));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private ObservableCollection<EmailDTO> _filteredEmails;
        public ObservableCollection<EmailDTO> FilteredEmails
        {
            get
            {
                if (_filteredEmails == null) _filteredEmails = new ObservableCollection<EmailDTO>();
                _filteredEmails.Clear();
                if (Emails == null)
                    return _filteredEmails;
                foreach (EmailDTO email in Emails)
                    _filteredEmails.Add(email);
                return _filteredEmails;
            }
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
                    NotifyOfPropertyChange(() => CaptureInfoAsRS);
                    if (CaptureInfoAsPN)
                    {
                        ClearErrors(nameof(BusinessName));
                        ValidateProperty(nameof(FirstName), FirstName);
                        ValidateProperty(nameof(FirstLastName), FirstLastName);
                    }
                    if (CaptureInfoAsRS)
                    {
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

        public bool CaptureInfoAsPN => SelectedCaptureType.Equals(BooksDictionaries.CaptureTypeEnum.PN);
        public bool CaptureInfoAsRS => SelectedCaptureType.Equals(BooksDictionaries.CaptureTypeEnum.RS);

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
                if (CaptureInfoAsRS && string.IsNullOrEmpty(BusinessName)) return false;
                // Si la captura de informacion es del tipo persona natural, los datos obligados son primer nombre y primer apellido
                if (CaptureInfoAsPN && (string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(FirstLastName))) return false;
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
                NotifyOfPropertyChange(nameof(FilteredEmails));
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
            SelectedDepartment = SelectedCountry.Departments.FirstOrDefault(x => x.Code == "05"); // 08 es el código del atlántico
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
            SelectedCountry = Countries.FirstOrDefault(x => x.Code == "169"); // 169 es el cóodigo de colombia
            SelectedDepartment = SelectedCountry.Departments.FirstOrDefault(x => x.Code == "05"); // 08 es el código del atlántico
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

        
        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                SupplierGraphQLModel result = await ExecuteSaveAsync();
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new SupplierCreateMessage() { CreatedSupplier = Context.AutoMapper.Map<SupplierDTO>(result)});
                }
                else
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new SupplierUpdateMessage() { UpdatedSupplier = Context.AutoMapper.Map<SupplierDTO>(result)});
                }
                Context.EnableOnViewReady = false;
                await Context.ActivateMasterView();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
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

        public async Task<SupplierGraphQLModel> ExecuteSaveAsync()
        {
            string query;

            try
            {
                List<int> retentions = new List<int>();
                List<object> emailList = new List<object>();
                List<string> phones = new List<string>();

                if (!string.IsNullOrEmpty(PrimaryPhone)) phones.Add(PrimaryPhone);
                if (!string.IsNullOrEmpty(SecondaryPhone)) phones.Add(SecondaryPhone);
                if (!string.IsNullOrEmpty(PrimaryCellPhone)) phones.Add(PrimaryCellPhone);
                if (!string.IsNullOrEmpty(SecondaryCellPhone)) phones.Add(SecondaryCellPhone);

                if (Emails != null)
                {
                    foreach (EmailDTO email in Emails)
                    {
                        emailList.Add(new { email.Description, email.Email, email.isElectronicInvoiceRecipient });
                    }
                }
                if (WithholdingTypes != null)
                {
                    foreach (WithholdingTypeDTO retention in WithholdingTypes)
                    {
                        if (retention.IsSelected)
                            retentions.Add(retention.Id);
                    }
                }

                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                variables.Data.Entity = new ExpandoObject();

                // Connection
                if (!IsNewRecord) variables.Id = Id;

                // Customer Data
                variables.Data.IsTaxFree = false;
                variables.Data.IcaRetentionMargin = 9.6;
                variables.Data.IcaRetentionMarginBasis = 100;
                variables.Data.RetainsAnyBasis = false;
                variables.Data.IcaAccountingAccountId = 290;

                // Entity Data
                variables.Data.Entity.IdentificationNumber = IdentificationNumber;
                variables.Data.Entity.VerificationDigit = SelectedIdentificationType.HasVerificationDigit ? VerificationDigit : "";
                variables.Data.Entity.CaptureType = SelectedCaptureType;
                variables.Data.Entity.BusinessName = CaptureInfoAsRS ? BusinessName : "";
                variables.Data.Entity.FirstName = CaptureInfoAsPN ? FirstName : "";
                variables.Data.Entity.MiddleName = CaptureInfoAsPN ? MiddleName : "";
                variables.Data.Entity.FirstLastName = CaptureInfoAsPN ? FirstLastName : "";
                variables.Data.Entity.MiddleLastName = CaptureInfoAsPN ? MiddleLastName : "";
                variables.Data.Entity.Phone1 = PrimaryPhone;
                variables.Data.Entity.Phone2 = SecondaryPhone;
                variables.Data.Entity.CellPhone1 = PrimaryCellPhone;
                variables.Data.Entity.CellPhone2 = SecondaryCellPhone;
                variables.Data.Entity.Address = Address;
                variables.Data.Entity.Regime = SelectedRegime;
                variables.Data.Entity.FullName = $"{variables.Data.Entity.FirstName} {variables.Data.Entity.MiddleName} {variables.Data.Entity.FirstLastName} {variables.Data.Entity.MiddleLastName}".Trim().RemoveExtraSpaces();
                variables.Data.Entity.TradeName = string.Empty;
                variables.Data.Entity.SearchName = $"{variables.Data.Entity.FirstName} {variables.Data.Entity.MiddleName} {variables.Data.Entity.FirstLastName} {variables.Data.Entity.MiddleLastName}".Trim().RemoveExtraSpaces() +
                (CaptureInfoAsRS ? BusinessName.ToString() : "").Trim().RemoveExtraSpaces();
                variables.Data.Entity.TelephonicInformation = string.Join(" - ", phones);
                variables.Data.Entity.CommercialCode = string.Empty;
                variables.Data.Entity.IdentificationTypeId = SelectedIdentificationType.Id;
                variables.Data.Entity.CountryId = SelectedCountry.Id;
                variables.Data.Entity.DepartmentId = SelectedDepartment.Id;
                variables.Data.Entity.CityId = SelectedCityId;

                // Emails
                if(emailList.Count == 0) variables.Data.Entity.Emails = new List<object>();
                if (emailList.Count > 0) 
                {
                    variables.Data.Entity.Emails = new List<object>();
                    variables.Data.Entity.Emails = emailList; 
                }

                // Retentions
                if(retentions.Count == 0) variables.Data.Retentions = new List<object>();
                if (retentions.Count > 0) 
                {
                    variables.Data.Retentions = variables.Data.Retentions = new List<int>();
                    variables.Data.Retentions = retentions; 
                }

                query = IsNewRecord ?
                        @"mutation ($data: CreateSupplierInput!) {
                          createResponse: createSupplier(data: $data) {
                            id
                            isTaxFree
                            icaRetentionMargin
                            icaRetentionMarginBasis
                            retainsAnyBasis
                            icaAccountingAccount {
                              id
                              code
                              name
                            }
                            retentions {
                              id
                              name
                              initialBase
                              margin
                              marginBase
                              retentionGroup
                            }
                            entity {
                              id
                              identificationType {
                                id
                                code
                              }
                              country {
                                id
                                code
                              }
                              department {
                                id
                                code
                              }
                              city {
                                id
                                code
                              }
                              identificationNumber
                              verificationDigit
                              captureType
                              searchName
                              firstName
                              middleName
                              firstLastName
                              middleLastName
                              businessName
                              phone1
                              phone2
                              cellPhone1
                              cellPhone2
                              address
                              telephonicInformation
                              emails {
                                id
                                description
                                password
                                sendElectronicInvoice
                              }
                            }
                          }
                        }" : @"mutation($data:UpdateSupplierInput!, $id:Int!) {
                          updateResponse: updateSupplier(data:$data, id:$id) {
                            id
                            isTaxFree
                            icaRetentionMargin
                            icaRetentionMarginBasis
                            retainsAnyBasis
                            icaAccountingAccount {
                              id
                              code
                              name
                            }
                            retentions {
                              id
                              name
                              initialBase
                              margin
                              marginBase
                              retentionGroup
                            }
                            entity {
                              id
                              identificationType {
                                id
                                code
                              }
                              country {
                                id
                                code
                              }
                              department {
                                id
                                code
                              }
                              city {
                                id
                                code
                              }
                              identificationNumber
                              verificationDigit
                              captureType
                              searchName
                              firstName
                              middleName
                              firstLastName
                              middleLastName
                              businessName
                              phone1
                              phone2
                              cellPhone1
                              cellPhone2
                              address
                              telephonicInformation
                              emails {
                                id
                                description
                                password
                                sendElectronicInvoice
                              }
                            }
                          }
                        }";

                SupplierGraphQLModel result = IsNewRecord ? await _supplierService.CreateAsync(query, variables) : await _supplierService.UpdateAsync(query, variables);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
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
            IRepository<SupplierGraphQLModel> supplierService)
        {
            _errors = new Dictionary<string, List<string>>();
            Context = context;
            _supplierService = supplierService;
            var joinable = new JoinableTaskFactory(new JoinableTaskContext());
            joinable.Run(async () => await InitializeAsync());
        }

        public async Task InitializeAsync()
        {
            try
            {
                string query = @"
                query{
                  identificationTypes{
                    id
                    code
                    name
                    hasVerificationDigit
                    minimumDocumentLength
                  }
                  countries{
                    id
                    code
                    name
                    departments {
                      id
                      code
                      name
                      cities {
                        id
                        code
                        name
                      }
                    }
                  }
                  retentionTypes{
                    id
                    name
                  }
                }";

                object variables = new();
                var result = await _supplierService.GetDataContextAsync<SupplierDataContext>(query, variables);
                IdentificationTypes = new ObservableCollection<IdentificationTypeGraphQLModel>(result.IdentificationTypes.Entries);
                WithholdingTypes = new ObservableCollection<WithholdingTypeDTO>(Context.AutoMapper.Map<ObservableCollection<WithholdingTypeDTO>>(result.WithholdingTypes.Entries));
                Countries = new ObservableCollection<CountryGraphQLModel>(result.Countries.Entries);

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        //protected override void OnViewReady(object view)
        //{
        //    base.OnViewReady(view);
        //    //ValidateProperties();
        //    _ = Application.Current.Dispatcher.BeginInvoke(() =>
        //      {
        //          SelectedIndexPage = 0; // Selecciona el primer TAB page
        //          _ = IsNewRecord
        //                ? Application.Current.Dispatcher.BeginInvoke(new System.Action(() => this.SetFocus(nameof(IdentificationNumber))), DispatcherPriority.Render)
        //                : CaptureInfoAsPN
        //                    ? Application.Current.Dispatcher.BeginInvoke(new System.Action(() => this.SetFocus(nameof(FirstName))), DispatcherPriority.Render)
        //                    : Application.Current.Dispatcher.BeginInvoke(new System.Action(() => this.SetFocus(nameof(BusinessName))), DispatcherPriority.Render);
        //      });
        //}

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
                        if (string.IsNullOrEmpty(value) || value.Length < SelectedIdentificationType.MinimumDocumentLength) AddError(propertyName, "El número de identificación no puede estar vacío");
                        break;
                    case nameof(FirstName):
                        if (string.IsNullOrEmpty(value) && CaptureInfoAsPN) AddError(propertyName, "El primer nombre no puede estar vacío");
                        break;
                    case nameof(FirstLastName):
                        if (string.IsNullOrEmpty(value) && CaptureInfoAsPN) AddError(propertyName, "El primer apellido no puede estar vacío");
                        break;
                    case nameof(BusinessName):
                        if (string.IsNullOrEmpty(value) && CaptureInfoAsRS) AddError(propertyName, "La razón social no puede estar vacía");
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
            if (CaptureInfoAsRS) ValidateProperty(nameof(BusinessName), BusinessName);
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
