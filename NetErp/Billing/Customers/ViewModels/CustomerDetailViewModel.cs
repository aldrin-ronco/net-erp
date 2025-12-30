using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using Dictionaries;
using GraphQL.Client.Http;
using Models.Billing;
using Models.Books;
using Models.DTO.Global;
using Models.Global;
using NetErp.Helpers;
using System;
using Extensions.Billing;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.VisualStudio.Threading;
using DevExpress.Xpf.Core;
using DevExpress.Mvvm;
using System.Windows.Threading;
using static Models.Global.GraphQLResponseTypes;
using NetErp.Helpers.GraphQLQueryBuilder;
using Newtonsoft.Json;
using Extensions.Global;
using static Dictionaries.BooksDictionaries;

namespace NetErp.Billing.Customers.ViewModels
{
    public class CustomerDetailViewModel : Screen,
        INotifyDataErrorInfo
    {
        private readonly IRepository<CustomerGraphQLModel> _customerService;

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

        Dictionary<string, List<string>> _errors;
        private List<string> _seedEmails = new List<string>();

        public CustomerViewModel Context { get; set; }

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
      

        private ObservableCollection<ZoneGraphQLModel> _zones;
        public ObservableCollection<ZoneGraphQLModel> Zones
        {
            get => _zones;
            set
            {
                if (_zones != value)
                {
                    _zones = value;
                    NotifyOfPropertyChange(nameof(Zones));
                }
            }
        }

        private ZoneGraphQLModel? _selectedZone;
        [ExpandoPath("zoneId", SerializeAsId = true)]
        public ZoneGraphQLModel? SelectedZone
        {
            get => _selectedZone;
            set
            {
                if (_selectedZone != value)
                {
                    _selectedZone = value;
                    NotifyOfPropertyChange(nameof(SelectedZone));
                    this.TrackChange(nameof(SelectedZone));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        private int _selectedIndexPage = 0;
        public int SelectedIndexPage
        {
            get => _selectedIndexPage;
            set
            {
                if (_selectedIndexPage != value)
                {
                    _selectedIndexPage = value;
                    NotifyOfPropertyChange(nameof(SelectedIndexPage));
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
                    this.TrackChange(nameof(SelectedRegime));
                    NotifyOfPropertyChange(nameof(CanSave));
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
                if (_primaryCellPhone != value)
                {
                    _primaryCellPhone = value;
                    ValidateProperty(nameof(PrimaryCellPhone), value);
                    NotifyOfPropertyChange(nameof(PrimaryCellPhone));
                    this.TrackChange(nameof(PrimaryCellPhone));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
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
                    ValidateProperty(nameof(SelectedCountry), _selectedCountry);
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
                    ValidateProperty(nameof(SelectedDepartment), _selectedDepartment);
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
                    ValidateProperty(nameof(SelectedCityId), _selectedCityId);
                    NotifyOfPropertyChange(nameof(SelectedCityId));
                    this.TrackChange(nameof(SelectedCityId));
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
                    this.TrackChange(nameof(IdentificationNumber));
                    NotifyOfPropertyChange(nameof(VerificationDigit));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _verificationDigit;
        [ExpandoPath("accountingEntity.verificationDigit")]
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

        private ObservableCollection<EmailGraphQLModel> _emails;
        [ExpandoPath("accountingEntity.emails")]
        public ObservableCollection<EmailGraphQLModel> Emails
        {
            get => _emails;
            set
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

        private void Emails_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Se dispara cuando se añade, elimina o modifica un elemento
            this.TrackChange(nameof(Emails));
            NotifyOfPropertyChange(nameof(CanSave));
        }

        private EmailGraphQLModel _selectedEmail;
        public EmailGraphQLModel SelectedEmail
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
                _id = value;
                NotifyOfPropertyChange(nameof(Id));
                NotifyOfPropertyChange(nameof(IsNewRecord));
            }
        }

        private BooksDictionaries.CaptureTypeEnum _selectedCaptureType;
        [ExpandoPath(path: "accountingEntity.captureType")]
        public BooksDictionaries.CaptureTypeEnum SelectedCaptureType
        {
            get => _selectedCaptureType;
            set
            {
                if (_selectedCaptureType != value)
                {
                    _selectedCaptureType = value;
                    NotifyOfPropertyChange(() => SelectedCaptureType);
                    this.TrackChange(nameof(SelectedCaptureType));
                    NotifyOfPropertyChange(() => CaptureInfoAsPN);
                    NotifyOfPropertyChange(() => CaptureInfoAsPJ);
                    if (CaptureInfoAsPN)
                    {
                        ClearErrors(nameof(BusinessName));
                        ValidateProperty(nameof(FirstName), FirstName);
                        ValidateProperty(nameof(FirstLastName), FirstLastName);
                    }
                    if (CaptureInfoAsPJ)
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
        public bool CaptureInfoAsPJ => SelectedCaptureType.Equals(BooksDictionaries.CaptureTypeEnum.PJ);

        public bool IsNewRecord => Id == 0;

        // Customer Data Properties
        private int _creditTerm = 0;
        public int CreditTerm
        {
            get => _creditTerm;
            set
            {
                if (_creditTerm != value)
                {
                    _creditTerm = value;
                    NotifyOfPropertyChange(nameof(CreditTerm));
                    this.TrackChange(nameof(CreditTerm));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _isActive = false;
        public new bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    NotifyOfPropertyChange(nameof(IsActive));
                    this.TrackChange(nameof(IsActive));

                    // Si el cliente no está bloqueado (IsActive = true), limpiar el motivo de bloqueo
                    if (_isActive)
                    {
                        BlockingReason = string.Empty;
                        ClearErrors(nameof(BlockingReason));
                    }
                    else
                    {
                        // Si está bloqueado, validar que tenga motivo de bloqueo
                        ValidateProperty(nameof(BlockingReason), BlockingReason);
                    }

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

        private string _blockingReason = string.Empty;
        public string BlockingReason
        {
            get => _blockingReason;
            set
            {
                if (_blockingReason != value)
                {
                    _blockingReason = value;
                    NotifyOfPropertyChange(nameof(BlockingReason));
                    this.TrackChange(nameof(BlockingReason));

                    // Validar solo si el cliente está bloqueado (IsActive = false)
                    if (!IsActive)
                    {
                        ValidateProperty(nameof(BlockingReason), value);
                    }

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _retainsAnyBasis = false;
        public bool RetainsAnyBasis
        {
            get => _retainsAnyBasis;
            set
            {
                if (_retainsAnyBasis != value)
                {
                    _retainsAnyBasis = value;
                    NotifyOfPropertyChange(nameof(RetainsAnyBasis));
                    this.TrackChange(nameof(RetainsAnyBasis));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool HasEmailChanges()
        {
            // Para nuevos registros, los emails no afectan el guardado
            if (IsNewRecord)
            {
                return false;
            }

            // Para actualizaciones, comparar con la lista seed
            var currentEmails = Emails.Select(e => e.Email).ToList();

            // Diferente cantidad = hubo cambios
            if (currentEmails.Count != _seedEmails.Count)
            {
                return true;
            }

            // Misma cantidad, verificar si todos los emails son los mismos
            // Usar HashSet para comparación eficiente
            var seedSet = new HashSet<string>(_seedEmails);
            return !currentEmails.All(email => seedSet.Contains(email));
        }

        public bool CanSave
        {
            get
            {
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
                // Validar ubicación geográfica requerida
                if (SelectedCountry == null) return false;
                if (SelectedDepartment == null) return false;
                if (SelectedCityId == 0) return false;
                // Si el control de errores por propiedades tiene algun error
                if (_errors.Count > 0) return false;
                // Si es actualización y no hay cambios en propiedades ni en emails, no permitir guardar
                if (!IsNewRecord && !this.HasChanges() && !HasEmailChanges()) return false;
                return true;
            }
        }

        #endregion

        
        
        #region Methods

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                UpsertResponseType<CustomerGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new CustomerCreateMessage() { CreatedCustomer = result});
                }
                else
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new CustomerUpdateMessage() { UpdatedCustomer = result});
                }
                await Context.ActivateMasterViewAsync();
            }
            catch (AsyncException ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            finally
            {
                IsBusy = false;
            }
        }


        public async Task<UpsertResponseType<CustomerGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                string query;
                List<int> withholdingTypes = [];

                // Build retentions list
                if (WithholdingTypes != null)
                {
                    foreach (WithholdingTypeDTO withholdingType in WithholdingTypes)
                    {
                        if (withholdingType.IsSelected)
                            withholdingTypes.Add(withholdingType.Id);
                    }
                }

                var transformers = new Dictionary<string, Func<object?, object?>>
                {
                    [nameof(Emails)] = item =>
                    {
                        var email = (EmailGraphQLModel)item!;
                        return new
                        {
                            description = email.Description,
                            email = email.Email
                        };
                    }
                };

                // Use ChangeCollector to build variables from tracked changes
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: IsNewRecord ? "createResponseInput" : "updateResponseData", transformers);

                // Add Id for UPDATE operations
                if (!IsNewRecord) variables.updateResponseId = Id;

                // Query usando QueryBuilder
                query = IsNewRecord ? GetCreateQuery() : GetUpdateQuery();

                UpsertResponseType<CustomerGraphQLModel> result = IsNewRecord
                    ? await _customerService.CreateAsync<UpsertResponseType<CustomerGraphQLModel>>(query, variables)
                    : await _customerService.UpdateAsync<UpsertResponseType<CustomerGraphQLModel>>(query, variables);
                return result;
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
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

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            if (IsNewRecord)
            {
                // Seed default values for new records
                this.SeedValue(nameof(SelectedRegime), SelectedRegime);
                this.SeedValue(nameof(SelectedCaptureType), SelectedCaptureType);
                this.SeedValue(nameof(SelectedIdentificationType), SelectedIdentificationType);
                this.SeedValue(nameof(CreditTerm), CreditTerm);
                this.SeedValue(nameof(IsTaxFree), IsTaxFree);
                this.SeedValue(nameof(RetainsAnyBasis), RetainsAnyBasis);
                this.SeedValue(nameof(IsActive), IsActive);
                this.SeedValue(nameof(SelectedCountry), SelectedCountry);
                this.SeedValue(nameof(SelectedDepartment), SelectedDepartment);
                this.SeedValue(nameof(SelectedCityId), SelectedCityId);
            }
            else
            {
                // Guardar los emails iniciales para comparación en actualizaciones
                _seedEmails = Emails.Select(e => e.Email).ToList();
            }
            // Accept all changes to reset the tracker
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        public CustomerDetailViewModel(CustomerViewModel context, IRepository<CustomerGraphQLModel> customerService)
        {
            _errors = new Dictionary<string, List<string>>();
            Context = context ?? throw new ArgumentNullException(nameof(context));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));

            // Inicializar la colección para activar la suscripción al CollectionChanged
            Emails = [];

            Context.EventAggregator.SubscribeOnUIThread(this);
        }

        public void AddEmail()
        {
            try
            {
                EmailGraphQLModel email = new() { Description = EmailDescription, Email = Email};
                Email = string.Empty;
                EmailDescription = string.Empty;
                Emails.Add(email); // CollectionChanged se dispara automáticamente
                _ = this.SetFocus(nameof(EmailDescription));
            }
            catch (Exception ex)
            {
                Execute.OnUIThread(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                });
            }
        }

        public void RemoveEmail(object p)
        {
            try
            {
                if (ThemedMessageBox.Show("Confirme ...", $"¿ Confirma que desea eliminar el email : {SelectedEmail.Email} ?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                if(SelectedEmail != null)
                {
                    EmailGraphQLModel? emailToDelete = Emails.FirstOrDefault(email => email.Id == SelectedEmail.Id);
                    if (emailToDelete is null) return;
                    Emails.Remove(emailToDelete); // CollectionChanged se dispara automáticamente
                }
            }
            catch (Exception ex)
            {
                Execute.OnUIThread(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                });
            }
        }

        public string GetLoadDataForNewQuery()
        {
            var identificationTypeFields = FieldSpec<PageType<IdentificationTypeGraphQLModel>>
                .Create()
                .SelectList(selector: it => it.Entries, nested: entries => entries
                    .Field(it => it.Id)
                    .Field(it => it.Name)
                    .Field(it => it.Code)
                    .Field(it => it.HasVerificationDigit)
                    .Field(it => it.MinimumDocumentLength)
                )
                .Build();

            var countryFields = FieldSpec<PageType<CountryGraphQLModel>>
                .Create()
                .SelectList(selector: c => c.Entries, nested: cEntry => cEntry
                    .Field(c => c.Id)
                    .Field(c => c.Name)
                    .Field(c => c.Code)
                    .SelectList(c => c.Departments, deptSpec => deptSpec
                        .Field(d => d.Id)
                        .Field(d => d.Name)
                        .Field(d => d.Code)
                        .SelectList(d => d.Cities, citySpec => citySpec
                            .Field(ci => ci.Id)
                            .Field(ci => ci.Name)
                            .Field(ci => ci.Code)
                        )
                    )
                )
                .Build();

            var withholdingTypeFields = FieldSpec<PageType<WithholdingCertificateConfigGraphQLModel>>
                .Create()
                .SelectList(selector: wtc => wtc.Entries, nested: wtSpec => wtSpec
                    .Field(wt => wt.Id)
                    .Field(wt => wt.Name)
                )
                .Build();

            var zoneFields = FieldSpec<PageType<ZoneGraphQLModel>>
                .Create()
                .SelectList(selector: z => z.Entries, nested: zSpec => zSpec
                    .Field(z => z.Id)
                    .Field(z => z.Name)
                    .Field(z => z.IsActive)
                )
                .Build();

            var parameter = new GraphQLQueryParameter("pagination", "Pagination");

            var identificationTypeFragment = new GraphQLQueryFragment("identificationTypesPage", [parameter], identificationTypeFields, "identificationTypes");
            var countryFragment = new GraphQLQueryFragment("countriesPage", [parameter], countryFields, "countries");
            var withholdingTypeFragment = new GraphQLQueryFragment("withholdingTypesPage", [parameter], withholdingTypeFields, "withholdingTypes");
            var zoneFragment = new GraphQLQueryFragment("zonesPage", [parameter], zoneFields, "zones");

            var builder = new GraphQLQueryBuilder([identificationTypeFragment, countryFragment, withholdingTypeFragment, zoneFragment]);

            return builder.GetQuery();
        }

        public string GetLoadDataForEditQuery()
        {
            var identificationTypeFields = FieldSpec<PageType<IdentificationTypeGraphQLModel>>
                .Create()
                .SelectList(selector: it => it.Entries, nested: entries => entries
                    .Field(it => it.Id)
                    .Field(it => it.Name)
                    .Field(it => it.Code)
                    .Field(it => it.HasVerificationDigit)
                    .Field(it => it.MinimumDocumentLength)
                )
                .Build();

            var countryFields = FieldSpec<PageType<CountryGraphQLModel>>
                .Create()
                .SelectList(selector: c => c.Entries, nested: cEntry => cEntry
                    .Field(c => c.Id)
                    .Field(c => c.Name)
                    .Field(c => c.Code)
                    .SelectList(c => c.Departments, deptSpec => deptSpec
                        .Field(d => d.Id)
                        .Field(d => d.Name)
                        .Field(d => d.Code)
                        .SelectList(d => d.Cities, citySpec => citySpec
                            .Field(ci => ci.Id)
                            .Field(ci => ci.Name)
                            .Field(ci => ci.Code)
                        )
                    )
                )
                .Build();

            var withholdingTypeFields = FieldSpec<PageType<WithholdingCertificateConfigGraphQLModel>>
                .Create()
                .SelectList(selector: wtc => wtc.Entries, nested: wtSpec => wtSpec
                    .Field(wt => wt.Id)
                    .Field(wt => wt.Name)
                )
                .Build();

            var zoneFields = FieldSpec<PageType<ZoneGraphQLModel>>
                .Create()
                .SelectList(selector: z => z.Entries, nested: zSpec => zSpec
                    .Field(z => z.Id)
                    .Field(z => z.Name)
                    .Field(z => z.IsActive)
                )
                .Build();

            var customerFields = FieldSpec<CustomerGraphQLModel>
                .Create()
                .Field(c => c.Id)
                .Field(c => c.CreditTerm)
                .Field(c => c.IsTaxFree)
                .Field(c => c.IsActive)
                .Field(c => c.BlockingReason)
                .Field(c => c.RetainsAnyBasis)
                .Select(c => c.AccountingEntity, entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.IdentificationNumber)
                    .Field(e => e.VerificationDigit)
                    .Field(e => e.CaptureType)
                    .Field(e => e.BusinessName)
                    .Field(e => e.FirstName)
                    .Field(e => e.MiddleName)
                    .Field(e => e.FirstLastName)
                    .Field(e => e.MiddleLastName)
                    .Field(e => e.PrimaryPhone)
                    .Field(e => e.SecondaryPhone)
                    .Field(e => e.PrimaryCellPhone)
                    .Field(e => e.SecondaryCellPhone)
                    .Field(e => e.Address)
                    .Field(e => e.Regime)
                    .Field(e => e.FullName)
                    .Field(e => e.TradeName)
                    .Field(e => e.SearchName)
                    .Field(e => e.TelephonicInformation)
                    .Field(e => e.CommercialCode)
                    .Select(e => e.IdentificationType, it => it
                        .Field(i => i.Id)
                        .Field(i => i.Name)
                        .Field(i => i.Code)
                        .Field(i => i.HasVerificationDigit)
                        .Field(i => i.MinimumDocumentLength))
                    .Select(e => e.Country, co => co
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                        .Field(c => c.Code))
                    .Select(e => e.Department, dept => dept
                        .Field(d => d.Id)
                        .Field(d => d.Name)
                        .Field(d => d.Code))
                    .Select(e => e.City, city => city
                        .Field(ci => ci.Id)
                        .Field(ci => ci.Name)
                        .Field(ci => ci.Code))
                    .SelectList(e => e.Emails, email => email
                        .Field(em => em.Id)
                        .Field(em => em.Description)
                        .Field(em => em.Email)))
                .SelectList(c => c.WithholdingTypes, wt => wt
                    .Field(w => w.Id)
                    .Field(w => w.Name))
                .Select(c => c.Zone, zone => zone
                    .Field(z => z.Id)
                    .Field(z => z.Name)
                    .Field(z => z.IsActive))
                .Build();

            var paginationParameter = new GraphQLQueryParameter("pagination", "Pagination");
            var customerIdParameter = new GraphQLQueryParameter("id", "ID!");

            var identificationTypeFragment = new GraphQLQueryFragment("identificationTypesPage", [paginationParameter], identificationTypeFields, "identificationTypes");
            var countryFragment = new GraphQLQueryFragment("countriesPage", [paginationParameter], countryFields, "countries");
            var withholdingTypeFragment = new GraphQLQueryFragment("withholdingTypesPage", [paginationParameter], withholdingTypeFields, "withholdingTypes");
            var zoneFragment = new GraphQLQueryFragment("zonesPage", [paginationParameter], zoneFields, "zones");
            var customerFragment = new GraphQLQueryFragment("customer", [customerIdParameter], customerFields, "customer");

            var builder = new GraphQLQueryBuilder([identificationTypeFragment, countryFragment, withholdingTypeFragment, zoneFragment, customerFragment]);

            return builder.GetQuery();
        }

        public async Task LoadDataForNewAsync()
        {
            try
            {
                string query = GetLoadDataForNewQuery();

                dynamic variables = new ExpandoObject();
                variables.identificationTypesPagePagination = new ExpandoObject();
                variables.countriesPagePagination = new ExpandoObject();
                variables.withholdingTypesPagePagination = new ExpandoObject();
                variables.zonesPagePagination = new ExpandoObject();

                variables.identificationTypesPagePagination.pageSize = -1;
                variables.countriesPagePagination.pageSize = -1;
                variables.withholdingTypesPagePagination.pageSize = -1;
                variables.zonesPagePagination.pageSize = -1;

                CustomersDataContext result = await _customerService.GetDataContextAsync<CustomersDataContext>(query, variables);
                IdentificationTypes = new ObservableCollection<IdentificationTypeGraphQLModel>(result.IdentificationTypes.Entries);
                WithholdingTypes = new ObservableCollection<WithholdingTypeDTO>(Context.AutoMapper.Map<ObservableCollection<WithholdingTypeDTO>>(result.WithholdingTypes.Entries));
                Countries = new ObservableCollection<CountryGraphQLModel>(result.Countries.Entries);
                Zones = new ObservableCollection<ZoneGraphQLModel>(result.Zones.Entries);             
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public async Task<CustomerGraphQLModel> LoadDataForEditAsync(int id)
        {
            try
            {
                string query = GetLoadDataForEditQuery();

                dynamic variables = new ExpandoObject();
                variables.identificationTypesPagePagination = new ExpandoObject();
                variables.countriesPagePagination = new ExpandoObject();
                variables.withholdingTypesPagePagination = new ExpandoObject();
                variables.zonesPagePagination = new ExpandoObject();

                variables.customerId = id;
                variables.identificationTypesPagePagination.pageSize = -1;
                variables.countriesPagePagination.pageSize = -1;
                variables.withholdingTypesPagePagination.pageSize = -1;
                variables.zonesPagePagination.pageSize = -1;

                CustomersDataContext result = await _customerService.GetDataContextAsync<CustomersDataContext>(query, variables);
                IdentificationTypes = new ObservableCollection<IdentificationTypeGraphQLModel>(result.IdentificationTypes.Entries);
                WithholdingTypes = new ObservableCollection<WithholdingTypeDTO>(Context.AutoMapper.Map<ObservableCollection<WithholdingTypeDTO>>(result.WithholdingTypes.Entries));
                Countries = new ObservableCollection<CountryGraphQLModel>(result.Countries.Entries);
                Zones = new ObservableCollection<ZoneGraphQLModel>(result.Zones.Entries);

                // Poblar el ViewModel con los datos del customer (sin bloquear UI thread)
                PopulateFromCustomer(result.Customer);

                return result.Customer;
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public void PopulateFromCustomer(CustomerGraphQLModel customer)
        {
            // Propiedades básicas del customer
            Id = customer.Id;
            CreditTerm = customer.CreditTerm;
            IsTaxFree = customer.IsTaxFree;
            IsActive = customer.IsActive;
            BlockingReason = customer.BlockingReason;
            RetainsAnyBasis = customer.RetainsAnyBasis;

            // Propiedades del AccountingEntity
            SelectedCaptureType = (CaptureTypeEnum)Enum.Parse(typeof(CaptureTypeEnum), customer.AccountingEntity.CaptureType);
            SelectedIdentificationType = IdentificationTypes.FirstOrDefault(x => x.Id == customer.AccountingEntity.IdentificationType.Id);
            FirstName = customer.AccountingEntity.FirstName;
            MiddleName = customer.AccountingEntity.MiddleName;
            FirstLastName = customer.AccountingEntity.FirstLastName;
            MiddleLastName = customer.AccountingEntity.MiddleLastName;
            PrimaryPhone = customer.AccountingEntity.PrimaryPhone;
            SecondaryPhone = customer.AccountingEntity.SecondaryPhone;
            PrimaryCellPhone = customer.AccountingEntity.PrimaryCellPhone;
            SecondaryCellPhone = customer.AccountingEntity.SecondaryCellPhone;
            BusinessName = customer.AccountingEntity.BusinessName;
            Address = customer.AccountingEntity.Address;
            IdentificationNumber = customer.AccountingEntity.IdentificationNumber;
            VerificationDigit = customer.AccountingEntity.VerificationDigit;

            // Emails
            Emails = customer.AccountingEntity.Emails is null ? [] : new ObservableCollection<EmailGraphQLModel>(customer.AccountingEntity.Emails);

            // Location
            SelectedCountry = Countries.FirstOrDefault(c => c.Id == customer.AccountingEntity.Country.Id);
            SelectedDepartment = SelectedCountry?.Departments.FirstOrDefault(d => d.Id == customer.AccountingEntity.Department.Id);
            SelectedCityId = customer.AccountingEntity.City.Id;

            // WithholdingTypes
            List<WithholdingTypeDTO> withholdingTypes = [];
            foreach (WithholdingTypeDTO retention in WithholdingTypes)
            {
                bool exist = customer.WithholdingTypes is null ? false : customer.WithholdingTypes.Any(x => x.Id == retention.Id);
                withholdingTypes.Add(new WithholdingTypeDTO()
                {
                    Id = retention.Id,
                    Name = retention.Name,
                    IsSelected = exist
                });
            }
            WithholdingTypes = new ObservableCollection<WithholdingTypeDTO>(withholdingTypes);

            // Zone
            SelectedZone = customer.Zone is null ? null : Zones.FirstOrDefault(z => z.Id == customer.Zone.Id);
        }

        public void GoBack(object p)
        {
            try
            {
                _ = Context.ActivateMasterViewAsync();
            }
            catch (AsyncException ex)
            {
                Execute.OnUIThread(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                });
            }
        }

        public void CleanUpControls()
        {
            try
            {
                List<WithholdingTypeDTO> withholdingTypes = [];
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
                Emails = [];
                SelectedCountry = Countries.FirstOrDefault(x => x.Code == "169"); // 169 es el cóodigo de colombia
                SelectedDepartment = SelectedCountry.Departments.FirstOrDefault(x => x.Code == "01"); // 01 es el código del atlántico
                SelectedCityId = SelectedDepartment.Cities.FirstOrDefault(x => x.Code == "001").Id; // 001 es el Codigo de Barranquilla

                // Inicializar propiedades de Customer
                CreditTerm = 0;
                IsActive = true;
                IsTaxFree = false;
                RetainsAnyBasis = false;
                BlockingReason = string.Empty;
                SelectedZone = null;

                foreach (WithholdingTypeDTO retention in WithholdingTypes)
                {
                    withholdingTypes.Add(new WithholdingTypeDTO()
                    {
                        Id = retention.Id,
                        Name = retention.Name,
                        IsSelected = false
                    });
                }
                WithholdingTypes = new ObservableCollection<WithholdingTypeDTO>(withholdingTypes);
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public void PhoneInputLostFocus(FrameworkElement element)
        {
            switch (element.Name.ToLower())
            {
                case "phone1":
                    PrimaryPhone = PrimaryPhone.ToPhoneFormat("### ## ##");
                    break;
                case "phone2":
                    SecondaryPhone = SecondaryPhone.ToPhoneFormat("### ## ##");
                    break;
                case "cellphone1":
                    PrimaryCellPhone = PrimaryCellPhone.ToPhoneFormat("### ### ## ##");
                    break;
                case "cellphone2":
                    SecondaryCellPhone = SecondaryCellPhone.ToPhoneFormat("### ### ## ##");
                    break;
                default:
                    break;
            }
        }

        public void EndRowEditing()
        {
            try
            {
                NotifyOfPropertyChange(nameof(Emails));
                NotifyOfPropertyChange(nameof(CanSave));
            }
            catch (Exception ex)
            {
                Execute.OnUIThread(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                });
            }
        }

        #endregion

        #region QueryBuilder Methods

        public string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<CustomerGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "customer", nested: sq => sq
                    .Field(c => c.Id)
                    .Field(c => c.CreditTerm)
                    .Field(c => c.IsTaxFree)
                    .Field(c => c.IsActive)
                    .Field(c => c.BlockingReason)
                    .Field(c => c.RetainsAnyBasis)
                    .SelectList(c => c.WithholdingTypes, ret => ret
                        .Field(r => r.Id)
                        .Field(r => r.Name)
                        .Field(r => r.WithholdingRate))
                    .Select(c => c.AccountingEntity, nested: entity => entity
                        .Field(e => e.Id)
                        .Field(e => e.IdentificationNumber)
                        .Field(e => e.VerificationDigit)
                        .Field(e => e.CaptureType)
                        .Field(e => e.BusinessName)
                        .Field(e => e.FirstName)
                        .Field(e => e.MiddleName)
                        .Field(e => e.FirstLastName)
                        .Field(e => e.MiddleLastName)
                        .Field(e => e.PrimaryPhone)
                        .Field(e => e.SecondaryPhone)
                        .Field(e => e.PrimaryCellPhone)
                        .Field(e => e.SecondaryCellPhone)
                        .Field(e => e.Address)
                        .Field(e => e.Regime)
                        .Field(e => e.FullName)
                        .Field(e => e.TradeName)
                        .Field(e => e.SearchName)
                        .Field(e => e.TelephonicInformation)
                        .Field(e => e.CommercialCode)
                        .Select(e => e.IdentificationType, it => it
                            .Field(i => i.Id)
                            .Field(i => i.Code)
                            .Field(i => i.Name))
                        .Select(e => e.Country, c => c
                            .Field(co => co.Id)
                            .Field(co => co.Code)
                            .Field(co => co.Name))
                        .Select(e => e.Department, d => d
                            .Field(de => de.Id)
                            .Field(de => de.Code)
                            .Field(de => de.Name))
                        .Select(e => e.City, ci => ci
                            .Field(cit => cit.Id)
                            .Field(cit => cit.Code)
                            .Field(cit => cit.Name))
                        .SelectList(e => e.Emails, em => em
                            .Field(email => email.Id)
                            .Field(email => email.Description)
                            .Field(email => email.Email))))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateCustomerInput!");

            var fragment = new GraphQLQueryFragment("createCustomer", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<CustomerGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "customer", nested: sq => sq
                    .Field(c => c.Id)
                    .Field(c => c.CreditTerm)
                    .Field(c => c.IsTaxFree)
                    .Field(c => c.IsActive)
                    .Field(c => c.BlockingReason)
                    .Field(c => c.RetainsAnyBasis)
                    .SelectList(c => c.WithholdingTypes, ret => ret
                        .Field(r => r.Id)
                        .Field(r => r.Name)
                        .Field(r => r.WithholdingRate))
                    .Select(c => c.AccountingEntity, nested: entity => entity
                        .Field(e => e.Id)
                        .Field(e => e.IdentificationNumber)
                        .Field(e => e.VerificationDigit)
                        .Field(e => e.CaptureType)
                        .Field(e => e.BusinessName)
                        .Field(e => e.FirstName)
                        .Field(e => e.MiddleName)
                        .Field(e => e.FirstLastName)
                        .Field(e => e.MiddleLastName)
                        .Field(e => e.PrimaryPhone)
                        .Field(e => e.SecondaryPhone)
                        .Field(e => e.PrimaryCellPhone)
                        .Field(e => e.SecondaryCellPhone)
                        .Field(e => e.Address)
                        .Field(e => e.Regime)
                        .Field(e => e.FullName)
                        .Field(e => e.TradeName)
                        .Field(e => e.SearchName)
                        .Field(e => e.TelephonicInformation)
                        .Field(e => e.CommercialCode)
                        .Select(e => e.IdentificationType, it => it
                            .Field(i => i.Id)
                            .Field(i => i.Code)
                            .Field(i => i.Name))
                        .Select(e => e.Country, c => c
                            .Field(co => co.Id)
                            .Field(co => co.Code)
                            .Field(co => co.Name))
                        .Select(e => e.Department, d => d
                            .Field(de => de.Id)
                            .Field(de => de.Code)
                            .Field(de => de.Name))
                        .Select(e => e.City, ci => ci
                            .Field(cit => cit.Id)
                            .Field(cit => cit.Code)
                            .Field(cit => cit.Name))
                        .SelectList(e => e.Emails, em => em
                            .Field(email => email.Id)
                            .Field(email => email.Description)
                            .Field(email => email.Email)
                            .Field(email => email.IsCorporate))))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateCustomerInput!"),
                new("id", "ID!")
            };

            var fragment = new GraphQLQueryFragment("updateCustomer", parameters, fields, "UpdateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
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
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null;
            return _errors[propertyName];
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
            if(string.IsNullOrEmpty(value)) value = string.Empty.Trim();
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
                    value = value.Replace(Convert.ToChar(45).ToString(), "").Replace(Convert.ToChar(95).ToString(), "");
                }
                switch (propertyName)
                {
                    case nameof(IdentificationNumber):
                        // Protección: no validar si SelectedIdentificationType es null (durante limpieza/desactivación)
                        if (SelectedIdentificationType == null) break;
                        if (string.IsNullOrEmpty(value) || value.Trim().Length < SelectedIdentificationType.MinimumDocumentLength) AddError(propertyName, "El número de identificación no puede estar vacío");
                        break;
                    case nameof(FirstName):
                        if (string.IsNullOrEmpty(value.Trim()) && CaptureInfoAsPN) AddError(propertyName, "El primer nombre no puede estar vacío");
                        break;
                    case nameof(FirstLastName):
                        if (string.IsNullOrEmpty(value.Trim()) && CaptureInfoAsPN) AddError(propertyName, "El primer apellido no puede estar vacío");
                        break;
                    case nameof(BusinessName):
                        if (string.IsNullOrEmpty(value.Trim()) && CaptureInfoAsPJ) AddError(propertyName, "La razón social no puede estar vacía");
                        break;
                    case nameof(BlockingReason):
                        if (string.IsNullOrEmpty(value.Trim()) && !IsActive) AddError(propertyName, "Debe especificar un motivo de bloqueo");
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
                Execute.OnUIThread(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                });
            }
        }

        private void ValidateProperty(string propertyName, object value)
        {
            try
            {
                ClearErrors(propertyName);
                switch (propertyName)
                {
                    case nameof(SelectedCountry):
                        if (value == null) AddError(propertyName, "Debe seleccionar un país");
                        break;
                    case nameof(SelectedDepartment):
                        if (value == null) AddError(propertyName, "Debe seleccionar un departamento");
                        break;
                    case nameof(SelectedCityId):
                        if (value is int cityId && cityId == 0) AddError(propertyName, "Debe seleccionar un municipio");
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Execute.OnUIThread(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                });
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
        public bool CanGoBack(object p)
        {
            return !IsBusy;
        }

        #endregion

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            // 1. Desuscribirse del EventAggregator PRIMERO para evitar memory leaks
            Context.EventAggregator.Unsubscribe(this);

            // 2. Desuscribirse del CollectionChanged ANTES de limpiar (evita disparar eventos durante limpieza)
            if (_emails != null)
            {
                _emails.CollectionChanged -= Emails_CollectionChanged;
            }

            // 3. Limpiar el ChangeTracker para liberar referencias
            this.AcceptChanges();

            // 4. AHORA sí limpiar las colecciones (ya no dispararán eventos porque nos desuscribimos)
            Emails?.Clear();
            Countries?.Clear();
            Zones?.Clear();
            WithholdingTypes?.Clear();
            IdentificationTypes?.Clear();

            return base.OnDeactivateAsync(close, cancellationToken);
        }

    }
}
