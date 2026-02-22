using Caliburn.Micro;
using Common.Constants;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Dictionaries;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using static Dictionaries.BooksDictionaries;
using NetErp.Helpers.GraphQLQueryBuilder;
using static Models.Global.GraphQLResponseTypes;
using Newtonsoft.Json;
using Extensions.Global;
using System.Threading;

namespace NetErp.Books.AccountingEntities.ViewModels
{
    public class AccountingEntityDetailViewModel : Screen, INotifyDataErrorInfo
    {


        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AccountingEntityGraphQLModel> _accountingEntityService;
        private readonly IdentificationTypeCache _identificationTypeCache;
        private readonly CountryCache _countryCache;


        Dictionary<string, List<string>> _errors;
        private List<string> _seedEmails = new List<string>();

        #region Commands
        private ICommand _deleteMailCommand;
        public ICommand DeleteMailCommand
        {
            get
            {
                if (_deleteMailCommand == null) this._deleteMailCommand = new RelayCommand(CanRemoveEmail, RemoveEmail);
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

        #region Propiedades
        // Context
        private AccountingEntityViewModel _context;
        public AccountingEntityViewModel Context
        {
            get { return _context; }
            set
            {
                if(_context  != value)
                {
                    _context = value;
                    NotifyOfPropertyChange(nameof(Context));
                }
            }
        }

        // Identity
        private int _id;
        public int Id
        {
            get { return _id; }
            set
            {
                if(_id != value)
                {
                    _id = value;
                    NotifyOfPropertyChange(nameof(Id));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        private bool _identificationNumberIsFocused;
        public bool IdentificationNumberIsFocused
        {
            get { return _identificationNumberIsFocused; }
            set 
            {
                if(_identificationNumberIsFocused != value)
                {
                    _identificationNumberIsFocused = value;
                    NotifyOfPropertyChange(nameof(IdentificationNumberIsFocused));
                }
            }
        }

        private bool _firstNameIsFocused;

        public bool FirstNameIsFocused
        {
            get { return _firstNameIsFocused; }
            set 
            {
                if(_firstNameIsFocused != value)
                {
                    _firstNameIsFocused = value;
                    NotifyOfPropertyChange(nameof(FirstNameIsFocused));
                }
            }
        }
        private int _selectedIndexPage = 0;
        public int SelectedIndexPage
        {
            get => _selectedIndexPage;
            set
            {
                if(_selectedIndexPage != value)
                {
                    _selectedIndexPage = value;
                    NotifyOfPropertyChange(nameof(SelectedIndexPage));
                }
            }
        }

        private bool _businessNameIsFocused;

        public bool BusinessNameIsFocused
        {
            get { return _businessNameIsFocused; }
            set
            {
                if(_businessNameIsFocused != value)
                {
                    _businessNameIsFocused = value;
                    NotifyOfPropertyChange(nameof(BusinessNameIsFocused));
                }
            }
        }

        private bool _emailDescriptionIsFocused;

        public bool EmailDescriptionIsFocused
        {
            get { return _emailDescriptionIsFocused; }
            set 
            { 
                if(_emailDescriptionIsFocused != value)
                {
                    _emailDescriptionIsFocused = value;
                    NotifyOfPropertyChange(nameof(EmailDescriptionIsFocused));
                }
            }
        }

        // Control de visibilidad de panels de captura de datos
        public bool CaptureInfoAsPN => SelectedCaptureType.Equals(CaptureTypeEnum.PN);
        public bool CaptureInfoAsPJ => SelectedCaptureType.Equals(CaptureTypeEnum.PJ);

        /// <summary>
        /// Tipo de Captura, Razon Social = RS o Persona Natural = PN
        /// </summary>
        private CaptureTypeEnum _selectedCaptureType;
        [ExpandoPath("captureType")]
        public CaptureTypeEnum SelectedCaptureType
        {
            get { return _selectedCaptureType; }
            set
            {
                if(_selectedCaptureType != value)
                {
                    _selectedCaptureType = value;
                    NotifyOfPropertyChange(nameof(SelectedCaptureType));
                    this.TrackChange(nameof(SelectedCaptureType));
                    NotifyOfPropertyChange(nameof(CaptureInfoAsPN));
                    NotifyOfPropertyChange(nameof(CaptureInfoAsPJ));
                    if (CaptureInfoAsPN)
                    {
                        BusinessName = string.Empty;
                        ClearErrors(nameof(BusinessName));
                        ValidateProperty(nameof(FirstName), FirstName);
                        ValidateProperty(nameof(FirstLastName), FirstLastName);
                    }
                    if (CaptureInfoAsPJ)
                    {
                        FirstName = string.Empty;
                        FirstLastName = string.Empty;
                        MiddleName = string.Empty;
                        MiddleLastName = string.Empty;
                        TradeName = string.Empty;
                        NotifyOfPropertyChange(nameof(FirstName));
                        NotifyOfPropertyChange(nameof(FirstLastName));
                        NotifyOfPropertyChange(nameof(MiddleName));
                        NotifyOfPropertyChange(nameof(MiddleLastName));
                        NotifyOfPropertyChange(nameof(TradeName));
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


        void SetFocus(Expression<Func<object>> expression)
        {
            string controlName = expression.GetMemberInfo().Name;

            IdentificationNumberIsFocused = false;
            FirstNameIsFocused = false;
            BusinessNameIsFocused = false;
            EmailDescriptionIsFocused = false;

            IdentificationNumberIsFocused = controlName == nameof(IdentificationNumber);
            FirstNameIsFocused = controlName == nameof(FirstName);
            BusinessNameIsFocused = controlName == nameof(BusinessName);
            EmailDescriptionIsFocused = controlName == nameof(EmailDescription);
        }

        /// <summary>
        /// Si es un nuevo registro
        /// </summary>
        public bool IsNewRecord
        {
            get { return (this.Id == 0); }
        }

        /// <summary>
        /// Regimen
        /// </summary>
        public Dictionary<char, string> RegimeDictionary { get { return BooksDictionaries.RegimeDictionary; } }
        // Regimen Seleccionado
        private char _selectedRegime = 'R';
        [ExpandoPath("regime")]
        public char SelectedRegime
        {
            get { return _selectedRegime; }
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

        /// <summary>
        /// Tipos de Documentos
        /// </summary>
        private ObservableCollection<IdentificationTypeGraphQLModel> _identificationTypes;
        public ObservableCollection<IdentificationTypeGraphQLModel> IdentificationTypes
        {
            get { return _identificationTypes; }
            set
            {
                if (_identificationTypes != value)
                {
                    _identificationTypes = value;
                    NotifyOfPropertyChange(nameof(IdentificationTypes));
                }
            }
        }

        /// <summary>
        /// Selected Email
        /// </summary>
        private EmailGraphQLModel _selectedEmail;
        public EmailGraphQLModel SelectedEmail
        {
            get { return _selectedEmail; }
            set
            {
                if (_selectedEmail != value)
                {
                    _selectedEmail = value;
                    NotifyOfPropertyChange(nameof(SelectedEmail));
                }
            }
        }

        // Tipo de documento seleccionado
        private IdentificationTypeGraphQLModel _selectedIdentificationType;
        [ExpandoPath("identificationTypeId", SerializeAsId = true)]
        public IdentificationTypeGraphQLModel SelectedIdentificationType
        {
            get { return _selectedIdentificationType; }
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

        // Emails (Lista de emails del tercero)
        private ObservableCollection<EmailGraphQLModel> _emails;
        public ObservableCollection<EmailGraphQLModel> Emails
        {
            get { return _emails; }
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

        private void Emails_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Se dispara cuando se añade, elimina o modifica un elemento
            this.TrackChange(nameof(Emails));
            NotifyOfPropertyChange(nameof(CanSave));
        }

        /// Descripcion de Email (Para agregar)
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
                }
            }
        }

        /// Email (Para agregar)
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

        /// <summary>
        /// Número de identificación
        /// </summary>
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
                    this.TrackChange(nameof(IdentificationNumber));
                    NotifyOfPropertyChange(nameof(VerificationDigit));

                    // Trackear VerificationDigit cuando se auto-calcula (solo en nuevos registros)
                    if (IsNewRecord && SelectedIdentificationType != null && SelectedIdentificationType.HasVerificationDigit)
                    {
                        if (value.Trim().Length >= SelectedIdentificationType.MinimumDocumentLength)
                        {
                            this.TrackChange(nameof(VerificationDigit));
                        }
                    }

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        /// <summary>
        /// Digito de Verificacion
        /// </summary>
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

        /// <summary>
        /// Razon Social
        /// </summary>
        private string _businessName = string.Empty;
        public string BusinessName
        {
            get
            {
                if(_businessName is null) return string.Empty;
                return _businessName;
            }
            set
            {
                if (_businessName != value)
                {
                    _businessName = value;
                    NotifyOfPropertyChange(nameof(BusinessName));
                    this.TrackChange(nameof(BusinessName));
                    ValidateProperty(nameof(BusinessName), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        /// <summary>
        /// Nombre Comercial (solo para persona natural)
        /// </summary>
        private string _tradeName = string.Empty;
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

        /// <summary>
        /// Primer Nombre
        /// </summary>
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
                    NotifyOfPropertyChange(nameof(FirstName));
                    this.TrackChange(nameof(FirstName));
                    ValidateProperty(nameof(FirstName), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }



        /// <summary>
        /// Segundo Nombre
        /// </summary>
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
                    this.TrackChange(nameof(MiddleName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        /// <summary>
        /// Primer Apellido
        /// </summary>
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
                    NotifyOfPropertyChange(nameof(FirstLastName));
                    this.TrackChange(nameof(FirstLastName));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperty(nameof(FirstLastName), value);
                }
            }
        }

        /// <summary>
        /// Segundo Apellido
        /// </summary>
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
                    this.TrackChange(nameof(MiddleLastName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        /// <summary>
        /// Telefono Fijo 1
        /// </summary>
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
                    NotifyOfPropertyChange(nameof(PrimaryPhone));
                    this.TrackChange(nameof(PrimaryPhone));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperty(nameof(PrimaryPhone), value);
                }
            }
        }

        /// <summary>
        /// Telefono Fijo 2
        /// </summary>
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
                    NotifyOfPropertyChange(nameof(SecondaryPhone));
                    this.TrackChange(nameof(SecondaryPhone));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperty(nameof(SecondaryPhone), value);
                }
            }
        }

        /// <summary>
        /// Telefono Celular 1
        /// </summary>
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
                if (_primaryCellPhone != value)
                {
                    _primaryCellPhone = value;
                    NotifyOfPropertyChange(nameof(PrimaryCellPhone));
                    this.TrackChange(nameof(PrimaryCellPhone));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperty(nameof(PrimaryCellPhone), value);
                }
            }
        }

        /// <summary>
        /// Telefono Celular 2
        /// </summary>
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
                    NotifyOfPropertyChange(nameof(SecondaryCellPhone));
                    this.TrackChange(nameof(SecondaryCellPhone));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperty(nameof(SecondaryCellPhone), value);
                }
            }
        }

        /// <summary>
        /// Direccion
        /// </summary>
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
                    this.TrackChange(nameof(Address));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        /// <summary>
		/// Is Busy
		/// </summary>
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

        /// <summary>
        /// Paises
        /// </summary>
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
                }
            }
        }

        // Pais Seleccionado - SelectedItem
        private CountryGraphQLModel _selectedCountry;
        [ExpandoPath("countryId", SerializeAsId = true)]
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

                    // Inicializar cascada País → Departamento → Ciudad
                    if (_selectedCountry != null)
                    {
                        InitializeCountryDependencies();
                    }

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        // Departamento Seleccionado
        private DepartmentGraphQLModel _selectedDepartment;
        [ExpandoPath("departmentId", SerializeAsId = true)]
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
                    
                    // Inicializar cascada Departamento → Ciudad
                    if (_selectedDepartment != null)
                    {
                        InitializeDepartmentDependencies();
                    }

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        /// <summary>
        /// Ciudad seleccionada
        /// </summary>
        private int _selectedCityId;
        [ExpandoPath("cityId")]
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

        #endregion

        #region Métodos de inicialización de dependencias geográficas

        /// <summary>
        /// Inicializa la cascada País → Departamento → Ciudad
        /// </summary>
        private void InitializeCountryDependencies()
        {
            // Validar que el país tenga departamentos
            if (_selectedCountry.Departments == null || !_selectedCountry.Departments.Any())
            {
                _ = ShowCountryDepartmentErrorAsync();
                return;
            }

            // Auto-seleccionar el primer departamento (esto a su vez seleccionará la ciudad)
            SelectedDepartment = _selectedCountry.Departments.First();
        }

        /// <summary>
        /// Inicializa la cascada Departamento → Ciudad
        /// </summary>
        private void InitializeDepartmentDependencies()
        {
            // Validar que el departamento tenga ciudades
            if (_selectedDepartment.Cities == null || !_selectedDepartment.Cities.Any())
            {
                _ = ShowDepartmentCitiesErrorAsync();
                return;
            }

            // Auto-seleccionar la primera ciudad
            SelectedCityId = _selectedDepartment.Cities.First().Id;
        }

        /// <summary>
        /// Muestra error cuando un país no tiene departamentos
        /// </summary>
        private async Task ShowCountryDepartmentErrorAsync()
        {
            await Execute.OnUIThreadAsync(() =>
            {
                ThemedMessageBox.Show(
                    title: "Error de datos",
                    text: $"El país '{_selectedCountry.Name}' no tiene departamentos asociados. Comuníquese con el área de soporte técnico.",
                    messageBoxButtons: MessageBoxButton.OK,
                    icon: MessageBoxImage.Error);
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Muestra error cuando un departamento no tiene ciudades
        /// </summary>
        private async Task ShowDepartmentCitiesErrorAsync()
        {
            await Execute.OnUIThreadAsync(() =>
            {
                ThemedMessageBox.Show(
                    title: "Error de datos",
                    text: $"El departamento '{_selectedDepartment.Name}' no tiene ciudades asociadas. Comuníquese con el área de soporte técnico.",
                    messageBoxButtons: MessageBoxButton.OK,
                    icon: MessageBoxImage.Error);
                return Task.CompletedTask;
            });
        }

        #endregion

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            ValidateProperties();
            
            _ = Application.Current.Dispatcher.BeginInvoke(() =>
            {
                SelectedIndexPage = 0;
                string focusTarget = IsNewRecord ? nameof(IdentificationNumber)
                    : CaptureInfoAsPN ? nameof(FirstName)
                    : nameof(BusinessName);
                this.SetFocus(focusTarget);
            }, DispatcherPriority.Render);
        }

        public AccountingEntityDetailViewModel(
            AccountingEntityViewModel context,
            IRepository<AccountingEntityGraphQLModel> accountingEntityService,
            IdentificationTypeCache identificationTypeCache,
            CountryCache countryCache)
        {
            _errors = [];
            Context = context;
            _accountingEntityService = accountingEntityService;
            _identificationTypeCache = identificationTypeCache;
            _countryCache = countryCache;

            // Inicializar la colección para activar la suscripción al CollectionChanged
            Emails = [];

            Context.EventAggregator.SubscribeOnUIThread(this);
        }


        public void EndRowEditing()
        {
            try
            {
                //TODO: Implementar si es necesario
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "EndRowEditing" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async void RemoveEmail(object p)
        {
            try
            {
                if (ThemedMessageBox.Show(title: "Confirme ...", text: $"¿Confirma que desea eliminar el email: {SelectedEmail.Email}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question) == MessageBoxResult.No) return;
                if (SelectedEmail != null)
                {
                    EmailGraphQLModel? emailToDelete = Emails.FirstOrDefault(email => email.Id == SelectedEmail.Id);
                    if (emailToDelete is null) return;
                    Emails.Remove(emailToDelete); // CollectionChanged se dispara automáticamente
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "RemoveEmail" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public bool CanRemoveEmail(object p) => true;

        public void AddEmail()
        {
            try
            {
                EmailGraphQLModel email = new() { Description = EmailDescription, Email = Email };
                Email = "";
                EmailDescription = "";
                Emails.Add(email); // CollectionChanged se dispara automáticamente
                _ = this.SetFocus(nameof(EmailDescription));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "AddEmail" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public bool CanAddEmail => !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(EmailDescription) && Email.IsValidEmail();


     
        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                UpsertResponseType<AccountingEntityGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnCurrentThreadAsync(new AccountingEntityCreateMessage() { CreatedAccountingEntity = result });
                }
                else
                {
                    await Context.EventAggregator.PublishOnCurrentThreadAsync(new AccountingEntityUpdateMessage() { UpdatedAccountingEntity = result});
                }
                await Context.ActivateMasterViewAsync();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{graphQLError.Errors[0].Extensions.Message} {graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "Save" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<UpsertResponseType<AccountingEntityGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                string query;

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

                UpsertResponseType<AccountingEntityGraphQLModel> result = IsNewRecord
                    ? await _accountingEntityService.CreateAsync<UpsertResponseType<AccountingEntityGraphQLModel>>(query, variables)
                    : await _accountingEntityService.UpdateAsync<UpsertResponseType<AccountingEntityGraphQLModel>>(query, variables);
                return result;
            }
            catch (Exception)
            {
                throw;
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

        #region Validaciones
        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public void GoBack(object p)
        {
            _ = Task.Run(() => Context.ActivateMasterViewAsync());
        }

        public bool CanGoBack(object p)
        {
            return !IsBusy;
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
            if (string.IsNullOrEmpty(value)) value = string.Empty.Trim();
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
                    case nameof(PrimaryPhone):
                        if (value.Length != 7 && !string.IsNullOrEmpty(value)) AddError(propertyName, "El número de teléfono debe contener 7 digitos");
                        break;
                    case nameof(SecondaryPhone):
                        if (value.Length != 7 && !string.IsNullOrEmpty(value)) AddError(propertyName, "El número de teléfono debe contener 7 digitos");
                        break;
                    case nameof(PrimaryCellPhone):
                        if (value.Length != 10 && !string.IsNullOrEmpty(value)) AddError(propertyName, "El número de teléfono celular debe contener 10 digitos");
                        break;
                    case nameof(SecondaryCellPhone):
                        if (value.Length != 10 && !string.IsNullOrEmpty(value)) AddError(propertyName, "El número de teléfono celular debe contener 10 digitos");
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

        #region Data Loading and QueryBuilder Methods

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            if (IsNewRecord)
            {
                // Seed default values for new records
                this.SeedValue(nameof(SelectedRegime), SelectedRegime);
                this.SeedValue(nameof(SelectedCaptureType), SelectedCaptureType);
                this.SeedValue(nameof(SelectedIdentificationType), SelectedIdentificationType);
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

            // Establecer foco inicial
            if (IsNewRecord)
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

        public string GetAccountingEntityByIdQuery()
        {
            var accountingEntityFields = FieldSpec<AccountingEntityGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.IdentificationNumber)
                .Field(e => e.VerificationDigit)
                .Field(e => e.CaptureType)
                .Field(e => e.BusinessName)
                .Field(e => e.TradeName)
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
                    .Field(em => em.Email))
                .Build();

            var accountingEntityIdParameter = new GraphQLQueryParameter("id", "ID!");

            var accountingEntityFragment = new GraphQLQueryFragment("accountingEntity", [accountingEntityIdParameter], accountingEntityFields, "SingleItemResponse");

            var builder = new GraphQLQueryBuilder([accountingEntityFragment]);

            return builder.GetQuery();
        }

        public async Task SetDataForNewAsync()
        {
            try
            {
                // Cargar datos comunes desde los caches individuales
                await Task.WhenAll(
                    _identificationTypeCache.EnsureLoadedAsync(),
                    _countryCache.EnsureLoadedAsync()
                );
                IdentificationTypes = _identificationTypeCache.Items;
                Countries = _countryCache.Items;

                Id = 0; // Por medio del Id se establece si es un nuevo registro o una actualizacion
                SelectedRegime = 'R';
                VerificationDigit = "";
                SelectedIdentificationType = IdentificationTypes.FirstOrDefault(x => x.Code == Constant.DefaultIdentificationTypeCode) ?? throw new InvalidOperationException($"No se encontró el tipo de identificación por defecto con código '{Constant.DefaultIdentificationTypeCode}'. Contacte al área de soporte técnico."); // 31 es NIT
                IdentificationNumber = "";
                SelectedCaptureType = CaptureTypeEnum.PN;
                BusinessName = "";
                TradeName = "";
                FirstName = "";
                MiddleName = "";
                FirstLastName = "";
                MiddleLastName = "";
                PrimaryPhone = "";
                SecondaryPhone = "";
                PrimaryCellPhone = "";
                SecondaryCellPhone = "";
                Address = "";
                Emails = [];

                // Validar y asignar datos maestros por defecto
                SelectedCountry = Countries.FirstOrDefault(x => x.Code == Constant.DefaultCountryCode) ?? throw new InvalidOperationException($"No se encontró el país por defecto con código '{Constant.DefaultCountryCode}'. Contacte al área de soporte técnico.");
                SelectedDepartment = SelectedCountry.Departments.FirstOrDefault(x => x.Code == Constant.DefaultDepartmentCode) ?? throw new InvalidOperationException($"No se encontró el departamento por defecto con código '{Constant.DefaultDepartmentCode}' en el país '{SelectedCountry.Name}'. Contacte al área de soporte técnico.");
                CityGraphQLModel selectedCity = SelectedDepartment.Cities.FirstOrDefault(x => x.Code == Constant.DefaultCityCode) ?? throw new InvalidOperationException($"No se encontró la ciudad por defecto con código '{Constant.DefaultCityCode}' en el departamento '{SelectedDepartment.Name}'. Contacte al área de soporte técnico.");
                SelectedCityId = selectedCity.Id;
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public async Task<AccountingEntityGraphQLModel> LoadDataForEditAsync(int id)
        {
            try
            {
                // Cargar datos comunes desde los caches individuales
                await Task.WhenAll(
                    _identificationTypeCache.EnsureLoadedAsync(),
                    _countryCache.EnsureLoadedAsync()
                );
                IdentificationTypes = _identificationTypeCache.Items;
                Countries = _countryCache.Items;

                // Cargar solo el AccountingEntity específico
                string query = GetAccountingEntityByIdQuery();
                dynamic variables = new ExpandoObject();
                variables.singleItemResponseId = id;

                AccountingEntityGraphQLModel entity = await _accountingEntityService.FindByIdAsync(query, variables);

                // Poblar el ViewModel con los datos del accounting entity
                PopulateFromAccountingEntity(entity);

                return entity;
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public void PopulateFromAccountingEntity(AccountingEntityGraphQLModel entity)
        {
            Id = entity.Id;
            SelectedIdentificationType = IdentificationTypes.FirstOrDefault(x => x.Id == entity.IdentificationType.Id);
            VerificationDigit = entity.VerificationDigit;
            SelectedRegime = entity.Regime;
            IdentificationNumber = entity.IdentificationNumber;
            SelectedCaptureType = (CaptureTypeEnum)Enum.Parse(typeof(CaptureTypeEnum), entity.CaptureType);
            BusinessName = entity.BusinessName;
            TradeName = entity.TradeName;
            FirstName = entity.FirstName;
            MiddleName = entity.MiddleName;
            FirstLastName = entity.FirstLastName;
            MiddleLastName = entity.MiddleLastName;
            PrimaryPhone = entity.PrimaryPhone;
            SecondaryPhone = entity.SecondaryPhone;
            PrimaryCellPhone = entity.PrimaryCellPhone;
            SecondaryCellPhone = entity.SecondaryCellPhone;
            Address = entity.Address;
            SelectedCountry = Countries.FirstOrDefault(c => c.Id == entity.Country.Id);
            SelectedDepartment = SelectedCountry?.Departments.FirstOrDefault(d => d.Id == entity.Department.Id);
            SelectedCityId = entity.City.Id;
            Emails = entity.Emails is null ? [] : new ObservableCollection<EmailGraphQLModel>(entity.Emails);

            // Seed values to track changes when fields are cleared
            this.SeedValue(nameof(BusinessName), BusinessName);
            this.SeedValue(nameof(TradeName), TradeName);
            this.SeedValue(nameof(FirstName), FirstName);
            this.SeedValue(nameof(MiddleName), MiddleName);
            this.SeedValue(nameof(FirstLastName), FirstLastName);
            this.SeedValue(nameof(MiddleLastName), MiddleLastName);
            this.SeedValue(nameof(PrimaryPhone), PrimaryPhone);
            this.SeedValue(nameof(SecondaryPhone), SecondaryPhone);
            this.SeedValue(nameof(PrimaryCellPhone), PrimaryCellPhone);
            this.SeedValue(nameof(SecondaryCellPhone), SecondaryCellPhone);
            this.SeedValue(nameof(Address), Address);
            this.AcceptChanges();
        }

        public string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<AccountingEntityGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingEntity", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.IdentificationNumber)
                    .Field(e => e.VerificationDigit)
                    .Field(e => e.CaptureType)
                    .Field(e => e.BusinessName)
                    .Field(e => e.TradeName)
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
                    .Select(e => e.IdentificationType, it => it
                        .Field(i => i.Id)
                        .Field(i => i.Name)
                        .Field(i => i.Code))
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
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateAccountingEntityInput!");

            var fragment = new GraphQLQueryFragment("createAccountingEntity", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<AccountingEntityGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingEntity", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.IdentificationNumber)
                    .Field(e => e.VerificationDigit)
                    .Field(e => e.CaptureType)
                    .Field(e => e.BusinessName)
                    .Field(e => e.TradeName)
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
                    .Select(e => e.IdentificationType, it => it
                        .Field(i => i.Id)
                        .Field(i => i.Name)
                        .Field(i => i.Code))
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
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateAccountingEntityInput!"),
                new("id", "ID!")
            };

            var fragment = new GraphQLQueryFragment("updateAccountingEntity", parameters, fields, "UpdateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
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

            return base.OnDeactivateAsync(close, cancellationToken);
        }

    }
}
