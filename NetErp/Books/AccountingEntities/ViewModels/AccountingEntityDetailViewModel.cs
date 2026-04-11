using Caliburn.Micro;
using Common.Constants;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Dictionaries;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Books.AccountingEntities.Validators;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Dictionaries.BooksDictionaries;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.AccountingEntities.ViewModels
{
    public class AccountingEntityDetailViewModel : Screen, INotifyDataErrorInfo
    {


        private readonly IRepository<AccountingEntityGraphQLModel> _accountingEntityService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IdentificationTypeCache _identificationTypeCache;
        private readonly CountryCache _countryCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly AccountingEntityValidator _validator;

        private readonly Dictionary<string, List<string>> _errors = [];
        private List<string> _seedEmails = [];

        #region MaxLength Properties

        public int BusinessNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.BusinessName));
        public int FirstNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.FirstName));
        public int MiddleNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.MiddleName));
        public int FirstLastNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.FirstLastName));
        public int MiddleLastNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.MiddleLastName));
        public int TradeNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.TradeName));
        public int AddressMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.Address));
        public int PrimaryPhoneMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.PrimaryPhone));
        public int SecondaryPhoneMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.SecondaryPhone));
        public int PrimaryCellPhoneMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.PrimaryCellPhone));
        public int SecondaryCellPhoneMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.SecondaryCellPhone));
        public int IdentificationNumberMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.IdentificationNumber));
        public string IdentificationNumberMask
        {
            get
            {
                int max = IdentificationNumberMaxLength;
                bool allowsLetters = SelectedIdentificationType?.AllowsLetters ?? false;
                return allowsLetters ? $"[a-zA-Z0-9]{{0,{max}}}" : $"[0-9]{{0,{max}}}";
            }
        }

        public int EmailDescriptionMaxLength => _stringLengthCache.GetMaxLength<EmailGraphQLModel>(nameof(EmailGraphQLModel.Description));
        public int EmailMaxLength => _stringLengthCache.GetMaxLength<EmailGraphQLModel>(nameof(EmailGraphQLModel.Email));

        #endregion

        #region Commands
        private ICommand? _deleteMailCommand;
        public ICommand DeleteMailCommand
        {
            get
            {
                if (_deleteMailCommand == null) this._deleteMailCommand = new RelayCommand(CanRemoveEmail, RemoveEmail);
                return _deleteMailCommand;
            }
        }

        private ICommand? _cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                _cancelCommand ??= new AsyncCommand(CancelAsync);
                return _cancelCommand;
            }
        }

        private ICommand? _saveCommand;
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

        // Identity
        public int Id
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Id));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        public int SelectedIndexPage
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedIndexPage));
                }
            }
        }

        // Control de visibilidad de panels de captura de datos
        public bool CaptureInfoAsPN => SelectedCaptureType.Equals(CaptureTypeEnum.PN);
        public bool CaptureInfoAsPJ => SelectedCaptureType.Equals(CaptureTypeEnum.PJ);

        /// <summary>
        /// Tipo de Captura, Razon Social = RS o Persona Natural = PN
        /// </summary>
        [ExpandoPath("captureType")]
        public CaptureTypeEnum SelectedCaptureType
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
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
        public Dictionary<char, string> RegimeDictionary => BooksDictionaries.RegimeDictionary;

        // Regimen Seleccionado
        [ExpandoPath("regime")]
        public char SelectedRegime
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedRegime));
                    this.TrackChange(nameof(SelectedRegime));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = 'R';

        /// <summary>
        /// Tipos de Documentos
        /// </summary>
        public ReadOnlyObservableCollection<IdentificationTypeGraphQLModel> IdentificationTypes
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IdentificationTypes));
                }
            }
        } = null!;

        /// <summary>
        /// Selected Email
        /// </summary>
        public EmailGraphQLModel? SelectedEmail
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedEmail));
                }
            }
        }

        // Tipo de documento seleccionado
        [ExpandoPath("identificationTypeId", SerializeAsId = true)]
        public IdentificationTypeGraphQLModel SelectedIdentificationType
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedIdentificationType));
                    NotifyOfPropertyChange(nameof(IdentificationNumberMask));
                    this.TrackChange(nameof(SelectedIdentificationType));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperty(nameof(IdentificationNumber), IdentificationNumber);
                    if (IsNewRecord)
                    {
                        _ = this.SetFocus(nameof(IdentificationNumber));
                    }
                }
            }
        } = null!;

        // Emails (Lista de emails del tercero)
        public ObservableCollection<EmailGraphQLModel> Emails
        {
            get;
            set
            {
                if (field != value)
                {
                    // Desuscribirse del anterior si existe
                    if (field != null)
                        field.CollectionChanged -= Emails_CollectionChanged;

                    field = value;

                    // Suscribirse al nuevo
                    if (field != null)
                        field.CollectionChanged += Emails_CollectionChanged;

                    NotifyOfPropertyChange(nameof(Emails));
                    this.TrackChange(nameof(Emails));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = [];

        private void Emails_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Se dispara cuando se añade, elimina o modifica un elemento
            this.TrackChange(nameof(Emails));
            NotifyOfPropertyChange(nameof(CanSave));
        }

        /// Descripcion de Email (Para agregar)
        public string EmailDescription
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(EmailDescription));
                    NotifyOfPropertyChange(nameof(CanAddEmail));
                }
            }
        } = string.Empty;

        /// Email (Para agregar)
        public string Email
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Email));
                    NotifyOfPropertyChange(nameof(CanAddEmail));
                }
            }
        } = string.Empty;

        /// <summary>
        /// Número de identificación
        /// </summary>
        [ExpandoPath("identificationNumber")]
        public string IdentificationNumber
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IdentificationNumber));
                    this.TrackChange(nameof(IdentificationNumber));
                    ValidateProperty(nameof(IdentificationNumber), value);
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
        } = string.Empty;

        /// <summary>
        /// Digito de Verificacion. Tiene lógica de getter computada para modo "nuevo
        /// registro" — cuando IsNewRecord es true, se calcula a partir del IdentificationNumber
        /// y del MinimumDocumentLength del tipo de identificación. Cuando es edición,
        /// retorna el valor persistido. El setter SOLO aplica en modo edición o como
        /// pre-llenado manual; en modo nuevo el getter ignora el backing field.
        /// </summary>
        [ExpandoPath("verificationDigit")]
        public string VerificationDigit
        {
            get => !IsNewRecord
                   ? field
                   : SelectedIdentificationType == null
                   ? string.Empty
                   : IdentificationNumber.Trim().Length >= SelectedIdentificationType.MinimumDocumentLength
                   ? IdentificationNumber.GetVerificationDigit()
                   : string.Empty;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(VerificationDigit));
                    this.TrackChange(nameof(VerificationDigit));
                }
            }
        }

        /// <summary>
        /// Razon Social
        /// </summary>
        [ExpandoPath("businessName")]
        public string BusinessName
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(BusinessName));
                    this.TrackChange(nameof(BusinessName));
                    ValidateProperty(nameof(BusinessName), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        /// <summary>
        /// Nombre Comercial (solo para persona natural)
        /// </summary>
        [ExpandoPath("tradeName")]
        public string TradeName
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(TradeName));
                    this.TrackChange(nameof(TradeName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        /// <summary>
        /// Primer Nombre
        /// </summary>
        [ExpandoPath("firstName")]
        public string FirstName
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FirstName));
                    this.TrackChange(nameof(FirstName));
                    ValidateProperty(nameof(FirstName), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        /// <summary>
        /// Segundo Nombre
        /// </summary>
        [ExpandoPath("middleName")]
        public string MiddleName
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(MiddleName));
                    this.TrackChange(nameof(MiddleName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        /// <summary>
        /// Primer Apellido
        /// </summary>
        [ExpandoPath("firstLastName")]
        public string FirstLastName
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FirstLastName));
                    this.TrackChange(nameof(FirstLastName));
                    ValidateProperty(nameof(FirstLastName), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        /// <summary>
        /// Segundo Apellido
        /// </summary>
        [ExpandoPath("middleLastName")]
        public string MiddleLastName
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(MiddleLastName));
                    this.TrackChange(nameof(MiddleLastName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        /// <summary>
        /// Telefono Fijo 1
        /// </summary>
        [ExpandoPath("primaryPhone")]
        public string PrimaryPhone
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PrimaryPhone));
                    this.TrackChange(nameof(PrimaryPhone));
                    ValidateProperty(nameof(PrimaryPhone), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        /// <summary>
        /// Telefono Fijo 2
        /// </summary>
        [ExpandoPath("secondaryPhone")]
        public string SecondaryPhone
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SecondaryPhone));
                    this.TrackChange(nameof(SecondaryPhone));
                    ValidateProperty(nameof(SecondaryPhone), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        /// <summary>
        /// Telefono Celular 1
        /// </summary>
        [ExpandoPath("primaryCellPhone")]
        public string PrimaryCellPhone
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PrimaryCellPhone));
                    this.TrackChange(nameof(PrimaryCellPhone));
                    ValidateProperty(nameof(PrimaryCellPhone), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        /// <summary>
        /// Telefono Celular 2
        /// </summary>
        [ExpandoPath("secondaryCellPhone")]
        public string SecondaryCellPhone
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SecondaryCellPhone));
                    this.TrackChange(nameof(SecondaryCellPhone));
                    ValidateProperty(nameof(SecondaryCellPhone), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        /// <summary>
        /// Direccion
        /// </summary>
        [ExpandoPath("address")]
        public string Address
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Address));
                    this.TrackChange(nameof(Address));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        /// <summary>
        /// Is Busy
        /// </summary>
        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        /// <summary>
        /// Paises
        /// </summary>
        public ReadOnlyObservableCollection<CountryGraphQLModel> Countries
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Countries));
                }
            }
        } = null!;

        // Pais Seleccionado - SelectedItem
        [ExpandoPath("countryId", SerializeAsId = true)]
        public CountryGraphQLModel SelectedCountry
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCountry));
                    this.TrackChange(nameof(SelectedCountry));

                    // Inicializar cascada País → Departamento → Ciudad
                    if (field != null)
                    {
                        InitializeCountryDependencies();
                    }

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = null!;

        // Departamento Seleccionado
        [ExpandoPath("departmentId", SerializeAsId = true)]
        public DepartmentGraphQLModel SelectedDepartment
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedDepartment));
                    this.TrackChange(nameof(SelectedDepartment));

                    // Inicializar cascada Departamento → Ciudad
                    if (field != null)
                    {
                        InitializeDepartmentDependencies();
                    }

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = null!;

        /// <summary>
        /// Ciudad seleccionada
        /// </summary>
        [ExpandoPath("cityId")]
        public int SelectedCityId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
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
            if (SelectedCountry.Departments == null || !SelectedCountry.Departments.Any())
            {
                _ = ShowCountryDepartmentErrorAsync();
                return;
            }

            // Auto-seleccionar el primer departamento (esto a su vez seleccionará la ciudad)
            SelectedDepartment = SelectedCountry.Departments.First();
        }

        /// <summary>
        /// Inicializa la cascada Departamento → Ciudad
        /// </summary>
        private void InitializeDepartmentDependencies()
        {
            // Validar que el departamento tenga ciudades
            if (SelectedDepartment.Cities == null || !SelectedDepartment.Cities.Any())
            {
                _ = ShowDepartmentCitiesErrorAsync();
                return;
            }

            // Auto-seleccionar la primera ciudad
            SelectedCityId = SelectedDepartment.Cities.First().Id;
        }

        /// <summary>
        /// Muestra error cuando un país no tiene departamentos
        /// </summary>
        private async Task ShowCountryDepartmentErrorAsync()
        {
            await _joinableTaskFactory.SwitchToMainThreadAsync();
            ThemedMessageBox.Show(
                title: "Error de datos",
                text: $"El país '{SelectedCountry.Name}' no tiene departamentos asociados. Comuníquese con el área de soporte técnico.",
                messageBoxButtons: MessageBoxButton.OK,
                icon: MessageBoxImage.Error);
        }

        /// <summary>
        /// Muestra error cuando un departamento no tiene ciudades
        /// </summary>
        private async Task ShowDepartmentCitiesErrorAsync()
        {
            await _joinableTaskFactory.SwitchToMainThreadAsync();
            ThemedMessageBox.Show(
                title: "Error de datos",
                text: $"El departamento '{SelectedDepartment.Name}' no tiene ciudades asociadas. Comuníquese con el área de soporte técnico.",
                messageBoxButtons: MessageBoxButton.OK,
                icon: MessageBoxImage.Error);
        }

        #endregion


        public AccountingEntityDetailViewModel(
            IRepository<AccountingEntityGraphQLModel> accountingEntityService,
            IEventAggregator eventAggregator,
            IdentificationTypeCache identificationTypeCache,
            CountryCache countryCache,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            AccountingEntityValidator validator)
        {
            _accountingEntityService = accountingEntityService ?? throw new ArgumentNullException(nameof(accountingEntityService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _identificationTypeCache = identificationTypeCache ?? throw new ArgumentNullException(nameof(identificationTypeCache));
            _countryCache = countryCache ?? throw new ArgumentNullException(nameof(countryCache));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _joinableTaskFactory = joinableTaskFactory;
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));

            Emails = [];
        }

        public double DialogWidth
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogWidth));
                }
            }
        }

        public double DialogHeight
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogHeight));
                }
            }
        }

        public async Task LoadCachesAsync()
        {
            await Task.WhenAll(
                _identificationTypeCache.EnsureLoadedAsync(),
                _countryCache.EnsureLoadedAsync());

            IdentificationTypes = _identificationTypeCache.Items;
            Countries = _countryCache.Items;
        }


        public void EndRowEditing()
        {
            try
            {
                //TODO: Implementar si es necesario
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(EndRowEditing)}: {ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
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
                ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(RemoveEmail)}: {ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
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
                ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(AddEmail)}: {ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
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
                await _eventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new AccountingEntityCreateMessage { CreatedAccountingEntity = result }
                        : new AccountingEntityUpdateMessage { UpdatedAccountingEntity = result }
                );

                await TryCloseAsync(true);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(SaveAsync)}: {ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<UpsertResponseType<AccountingEntityGraphQLModel>> ExecuteSaveAsync()
        {
            Dictionary<string, Func<object?, object?>> transformers = new()
            {
                [nameof(Emails)] = item =>
                {
                    EmailGraphQLModel email = (EmailGraphQLModel)item!;
                    return new
                    {
                        description = email.Description,
                        email = email.Email
                    };
                }
            };

            dynamic variables = ChangeCollector.CollectChanges(this, prefix: IsNewRecord ? "createResponseInput" : "updateResponseData", transformers);

            if (!IsNewRecord) variables.updateResponseId = Id;

            string query = IsNewRecord ? _createQuery.Value.Query : _updateQuery.Value.Query;

            UpsertResponseType<AccountingEntityGraphQLModel> result = IsNewRecord
                ? await _accountingEntityService.CreateAsync<UpsertResponseType<AccountingEntityGraphQLModel>>(query, variables)
                : await _accountingEntityService.UpdateAsync<UpsertResponseType<AccountingEntityGraphQLModel>>(query, variables);
            return result;
        }

        private bool HasEmailChanges()
        {
            // Para nuevos registros, los emails no afectan el guardado
            if (IsNewRecord)
            {
                return false;
            }

            // Para actualizaciones, comparar con la lista seed
            List<string> currentEmails = [.. Emails.Select(e => e.Email)];

            // Diferente cantidad = hubo cambios
            if (currentEmails.Count != _seedEmails.Count)
            {
                return true;
            }

            // Misma cantidad, verificar si todos los emails son los mismos
            // Usar HashSet para comparación eficiente
            HashSet<string> seedSet = new(_seedEmails);
            return !currentEmails.All(email => seedSet.Contains(email));
        }

        public bool CanSave => _validator.CanSave(BuildCanSaveContext());

        private AccountingEntityValidationContext BuildValidationContext() => new()
        {
            CaptureInfoAsPN = CaptureInfoAsPN,
            CaptureInfoAsPJ = CaptureInfoAsPJ,
            MinimumDocumentLength = SelectedIdentificationType?.MinimumDocumentLength ?? 0,
            IdentificationNumber = IdentificationNumber,
            FirstName = FirstName,
            FirstLastName = FirstLastName,
            BusinessName = BusinessName,
            PrimaryPhone = PrimaryPhone,
            SecondaryPhone = SecondaryPhone,
            PrimaryCellPhone = PrimaryCellPhone,
            SecondaryCellPhone = SecondaryCellPhone
        };

        private AccountingEntityCanSaveContext BuildCanSaveContext() => new()
        {
            IsBusy = IsBusy,
            IsNewRecord = IsNewRecord,
            MinimumDocumentLength = SelectedIdentificationType?.MinimumDocumentLength ?? 0,
            HasVerificationDigit = SelectedIdentificationType?.HasVerificationDigit ?? false,
            IdentificationNumber = IdentificationNumber,
            VerificationDigit = VerificationDigit,
            CaptureInfoAsPN = CaptureInfoAsPN,
            CaptureInfoAsPJ = CaptureInfoAsPJ,
            FirstName = FirstName,
            FirstLastName = FirstLastName,
            BusinessName = BusinessName,
            HasCountry = SelectedCountry != null,
            HasDepartment = SelectedDepartment != null,
            HasCity = SelectedCityId != 0,
            HasChanges = this.HasChanges(),
            HasEmailChanges = HasEmailChanges(),
            HasErrors = _errors.Count > 0
        };

        #region Validaciones
        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        private static readonly string[] _basicDataFields = [nameof(FirstName), nameof(FirstLastName), nameof(BusinessName), nameof(PrimaryPhone), nameof(SecondaryPhone), nameof(PrimaryCellPhone), nameof(SecondaryCellPhone)];

        public bool HasBasicDataErrors => _basicDataFields.Any(f => _errors.ContainsKey(f));
        public string? BasicDataTabTooltip => GetTabTooltip(_basicDataFields);

        private string? GetTabTooltip(string[] fields)
        {
            List<string> errors = [.. fields
                .Where(f => _errors.ContainsKey(f))
                .SelectMany(f => _errors[f])];
            return errors.Count > 0 ? string.Join("\n", errors) : null;
        }

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            if (_basicDataFields.Contains(propertyName))
            {
                NotifyOfPropertyChange(nameof(HasBasicDataErrors));
                NotifyOfPropertyChange(nameof(BasicDataTabTooltip));
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }
        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.TryGetValue(propertyName, out List<string>? errors))
                return Enumerable.Empty<string>();
            return errors;
        }

        /// <summary>
        /// Actualiza los errores de una propiedad de forma atómica: primero muta el
        /// diccionario, luego dispara una única notificación. Evita estados intermedios
        /// que rompen los tab tooltips de <c>DXTabItem</c> vía <c>DataContextProxy</c>.
        /// </summary>
        private void SetPropertyErrors(string propertyName, IReadOnlyList<string> errors)
        {
            bool hadErrors = _errors.ContainsKey(propertyName);

            if (errors.Count > 0)
                _errors[propertyName] = [.. errors];
            else if (hadErrors)
                _errors.Remove(propertyName);

            if (hadErrors || errors.Count > 0)
                RaiseErrorsChanged(propertyName);
        }

        /// <summary>
        /// Fuerza la limpieza de errores para una propiedad (usado por el setter de
        /// <c>SelectedCaptureType</c> cuando cambia el modo de captura y los errores
        /// de nombre/razón social dejan de aplicar).
        /// </summary>
        private void ClearErrors(string propertyName) => SetPropertyErrors(propertyName, []);

        private void ValidateProperty(string propertyName, string value)
        {
            IReadOnlyList<string> errors = _validator.Validate(propertyName, value, BuildValidationContext());
            SetPropertyErrors(propertyName, errors);
        }

        private void ValidateProperties()
        {
            // Re-run validation for the fields that ValidateAll emits (scoped by
            // capture mode). SetPropertyErrors clears stale errors atomically so a
            // transition from PN→PJ (or viceversa) drops the now-irrelevant errors.
            string[] trackedProps =
            [
                nameof(IdentificationNumber),
                nameof(FirstName),
                nameof(FirstLastName),
                nameof(BusinessName)
            ];

            Dictionary<string, IReadOnlyList<string>> allErrors = _validator.ValidateAll(BuildValidationContext());

            foreach (string prop in trackedProps)
            {
                IReadOnlyList<string> errors = allErrors.TryGetValue(prop, out IReadOnlyList<string>? list)
                    ? list
                    : [];
                SetPropertyErrors(prop, errors);
            }
        }
        #endregion

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(SelectedRegime), SelectedRegime);
            this.SeedValue(nameof(SelectedCaptureType), SelectedCaptureType);
            this.SeedValue(nameof(SelectedIdentificationType), SelectedIdentificationType);
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
            this.SeedValue(nameof(VerificationDigit), VerificationDigit);
            this.SeedValue(nameof(SelectedCountry), SelectedCountry);
            this.SeedValue(nameof(SelectedDepartment), SelectedDepartment);
            this.SeedValue(nameof(SelectedCityId), SelectedCityId);
            this.AcceptChanges();
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(SelectedRegime), SelectedRegime);
            this.SeedValue(nameof(SelectedCaptureType), SelectedCaptureType);
            this.SeedValue(nameof(SelectedIdentificationType), SelectedIdentificationType);
            this.SeedValue(nameof(VerificationDigit), VerificationDigit);
            this.SeedValue(nameof(SelectedCountry), SelectedCountry);
            this.SeedValue(nameof(SelectedDepartment), SelectedDepartment);
            this.SeedValue(nameof(SelectedCityId), SelectedCityId);
            this.AcceptChanges();
        }

        #region Data Loading and GraphQL Queries

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            if (!IsNewRecord)
            {
                _seedEmails = [.. Emails.Select(e => e.Email)];
            }
            ValidateProperties();
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _accountingEntityByIdQuery = new(() =>
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

            var fragment = new GraphQLQueryFragment("accountingEntity",
                [new("id", "ID!")], accountingEntityFields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        public void SetForNew()
        {
            try
            {
                Id = 0;
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

                SeedDefaultValues();
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public async Task LoadDataForEditAsync(int id)
        {
            try
            {
                (GraphQLQueryFragment fragment, string query) = _accountingEntityByIdQuery.Value;
                object variables = new GraphQLVariables()
                    .For(fragment, "id", id)
                    .Build();

                AccountingEntityGraphQLModel entity = await _accountingEntityService.FindByIdAsync(query, variables);

                // Poblar el ViewModel con los datos del accounting entity
                PopulateFromAccountingEntity(entity);
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public void PopulateFromAccountingEntity(AccountingEntityGraphQLModel entity)
        {
            Id = entity.Id;
            SelectedIdentificationType = IdentificationTypes.First(x => x.Id == entity.IdentificationType.Id);
            VerificationDigit = entity.VerificationDigit;
            SelectedRegime = entity.Regime;
            IdentificationNumber = entity.IdentificationNumber;
            SelectedCaptureType = Enum.Parse<CaptureTypeEnum>(entity.CaptureType);
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
            SelectedCountry = Countries.First(c => c.Id == entity.Country.Id);
            SelectedDepartment = SelectedCountry?.Departments.First(d => d.Id == entity.Department.Id);
            SelectedCityId = entity.City.Id;
            Emails = entity.Emails is null ? [] : new ObservableCollection<EmailGraphQLModel>(entity.Emails);

            SeedCurrentValues();
        }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
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
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
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
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                if (Emails != null)
                    Emails.CollectionChanged -= Emails_CollectionChanged;

                this.AcceptChanges();
                Emails?.Clear();
            }

            return base.OnDeactivateAsync(close, cancellationToken);
        }

    }
}
