using Caliburn.Micro;
using Common.Constants;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Dictionaries;
using Extensions.Global;
using AutoMapper;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using Models.Books;
using Models.DTO.Global;
using Models.Global;
using NetErp.Billing.Zones.DTO;
using NetErp.Global.CostCenters.DTO;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Dictionaries.BooksDictionaries;
using static Models.Global.GraphQLResponseTypes;


namespace NetErp.Billing.Sellers.ViewModels
{
    public class SellerDetailViewModel : Screen, INotifyDataErrorInfo
    {
        private readonly CostCenterCache _costCenterCache;
        private readonly IdentificationTypeCache _identificationTypeCache;
        private readonly CountryCache _countryCache;
        private readonly ZoneCache _zoneCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly IEventAggregator _eventAggregator;
        private readonly IMapper _mapper;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly IGraphQLClient _graphQLClient;

        #region Commands

        private ICommand? _deleteMailCommand;
        public ICommand? DeleteMailCommand
        {
            get
            {
                if (_deleteMailCommand is null) _deleteMailCommand = new RelayCommand(CanRemoveEmail, RemoveEmail);
                return _deleteMailCommand;
            }
        }
        private ICommand? _cancelCommand;
        public ICommand? CancelCommand
        {
            get
            {
                if (_cancelCommand is null) _cancelCommand = new AsyncCommand(CancelAsync);
                return _cancelCommand;
            }
        }

        private ICommand? _saveCommand;
        public ICommand? SaveCommand
        {
            get
            {
                _saveCommand ??= new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        #endregion

        #region Properties

        private readonly IRepository<SellerGraphQLModel> _sellerService;

        Dictionary<string, List<string>> _errors;


        #region Dialog Size

        private double _dialogWidth = 600;
        public double DialogWidth
        {
            get => _dialogWidth;
            set
            {
                if (_dialogWidth != value)
                {
                    _dialogWidth = value;
                    NotifyOfPropertyChange(nameof(DialogWidth));
                }
            }
        }

        private double _dialogHeight = 500;
        public double DialogHeight
        {
            get => _dialogHeight;
            set
            {
                if (_dialogHeight != value)
                {
                    _dialogHeight = value;
                    NotifyOfPropertyChange(nameof(DialogHeight));
                }
            }
        }

        #endregion

        // MaxLength properties from StringLengthCache
        public int FirstNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.FirstName));
        public int MiddleNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.MiddleName));
        public int FirstLastNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.FirstLastName));
        public int MiddleLastNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.MiddleLastName));
        public int PrimaryPhoneMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.PrimaryPhone));
        public int SecondaryPhoneMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.SecondaryPhone));
        public int PrimaryCellPhoneMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.PrimaryCellPhone));
        public int SecondaryCellPhoneMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.SecondaryCellPhone));
        public int AddressMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.Address));
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

        public bool IsNewRecord => Id == 0;

        public bool CanRemoveEmail(object p) => true;
        public bool CanAddEmail => !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(EmailDescription) && Email.IsValidEmail();

        private string? _emailDescription;
        public string? EmailDescription
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

        private string? _email;

        public string? Email
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

        private ObservableCollection<CostCenterDTO> _costCenters = [];
        public ObservableCollection<CostCenterDTO> CostCenters
        {
            get => _costCenters;
            set
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                    NotifyOfPropertyChange(nameof(SelectedCostCenterIds));
                    this.TrackChange(nameof(SelectedCostCenterIds));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ListenCostCenterCheck();
                }
            }
        }
        public ReadOnlyObservableCollection<ZoneGraphQLModel>? Zones
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Zones));
                }
            }
        }
        [ExpandoPath("CostCenterIds")]
        public List<int> SelectedCostCenterIds => CostCenters.Where(f => f.IsSelected).Select(s => s.Id).ToList();

        public bool HasCostCenterErrors => CostCenters.All(f => !f.IsSelected);

        // Campos que pertenecen a cada tab
        private static readonly string[] _basicDataFields = [nameof(FirstName), nameof(FirstLastName), nameof(PrimaryPhone), nameof(SecondaryPhone), nameof(PrimaryCellPhone), nameof(SecondaryCellPhone)];

        public bool HasBasicDataErrors => _basicDataFields.Any(f => _errors.ContainsKey(f));
        public string? BasicDataTabTooltip => GetTabTooltip(_basicDataFields);

        public string? CostCenterTabTooltip => HasCostCenterErrors ? "Debe seleccionar al menos un centro de costo" : null;

        private string? GetTabTooltip(string[] fields)
        {
            var errors = fields
                .Where(f => _errors.ContainsKey(f))
                .SelectMany(f => _errors[f])
                .ToList();
            return errors.Count > 0 ? string.Join("\n", errors) : null;
        }

        [ExpandoPath("accountingEntity.identificationTypeId", SerializeAsId = true)]
        public IdentificationTypeGraphQLModel? SelectedIdentificationType
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

                    ValidateProperty(nameof(IdentificationNumber), _identificationNumber);
                    if (IsNewRecord)
                    {
                        _ = this.SetFocus(nameof(IdentificationNumber));
                    }
                }
            }
        }

        public ReadOnlyObservableCollection<CountryGraphQLModel>? Countries
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Countries));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("accountingEntity.countryId", SerializeAsId = true)]
        public CountryGraphQLModel? SelectedCountry
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCountry));
                    this.TrackChange(nameof(SelectedCountry));
                    if (field != null && field.Departments.Count > 0)
                    {
                        SelectedDepartment = field.Departments.FirstOrDefault(x => x.CountryId == field.Id);
                        NotifyOfPropertyChange(nameof(SelectedDepartment));
                    }
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }


        [ExpandoPath("accountingEntity.departmentId", SerializeAsId = true)]
        public DepartmentGraphQLModel? SelectedDepartment
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedDepartment));
                    this.TrackChange(nameof(SelectedDepartment));
                    if (field != null && field.Cities.Count > 0)
                    {
                        SelectedCityId = field.Cities.First().Id;
                        NotifyOfPropertyChange(nameof(SelectedCityId));
                    }
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private ObservableCollection<EmailDTO> _emails = [];

        [ExpandoPath("accountingEntity.emails")]
        public ObservableCollection<EmailDTO> Emails
        {
            get => _emails;
            set
            {
                if (_emails != value)
                {
                    // Desuscribirse del anterior si existe
                    if (_emails != null)
                    {
                        _emails.CollectionChanged -= Emails_CollectionChanged!;
                    }

                    _emails = value;

                    // Suscribirse al nuevo
                    if (_emails != null)
                    {
                        _emails.CollectionChanged += Emails_CollectionChanged!;
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

        public EmailDTO? SelectedEmail
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

        [ExpandoPath("accountingEntity.captureType")]
        public BooksDictionaries.CaptureTypeEnum SelectedCaptureType
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
                        ValidateProperty(nameof(FirstName), FirstName);
                        ValidateProperty(nameof(FirstLastName), FirstLastName);
                    }
                    if (CaptureInfoAsPJ)
                    {
                        ClearErrors(nameof(FirstName));
                        ClearErrors(nameof(FirstLastName));
                    }

                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperties();
                    if (string.IsNullOrEmpty(IdentificationNumber))
                    {
                        this.SetFocus(nameof(IdentificationNumber));
                    }
                    else
                    {
                        this.SetFocus(nameof(FirstName));
                    }
                }
            }
        }

        public bool CaptureInfoAsPN => SelectedCaptureType.Equals(BooksDictionaries.CaptureTypeEnum.PN);
        public bool CaptureInfoAsPJ => SelectedCaptureType.Equals(BooksDictionaries.CaptureTypeEnum.PJ);

        [ExpandoPath("accountingEntity.cityId")]
        public int? SelectedCityId
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
        [ExpandoPath("zoneId", SerializeAsId = true)]
        public ZoneGraphQLModel? SelectedZone
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedZone));
                    this.TrackChange(nameof(SelectedZone), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        public new bool IsActive
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsActive));
                    this.TrackChange(nameof(IsActive));
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

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private char _regime = 'N';
        [ExpandoPath("accountingEntity.regime")]
        public char Regime
        {
            get => _regime;
            set
            {
                if (_regime != value)
                {
                    _regime = value;
                    NotifyOfPropertyChange(nameof(Regime));
                    this.TrackChange(nameof(Regime));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        public bool CanSave
        {
            get
            {
                // Debe haber definido el respectivo tipo de identificacion
                //if (SelectedIdentificationType == null) return false;
                // Si el documento de identidad esta vacion o su longitud es inferior a la longitud minima definida para ese tipo de documento
                if (string.IsNullOrEmpty(IdentificationNumber.Trim()) || IdentificationNumber.Length < SelectedIdentificationType?.MinimumDocumentLength) return false;
                if (CostCenters.Where(f => f.IsSelected == true).Count() == 0) return false;
                // Si la captura de informacion es del tipo persona natural, los datos obligados son primer nombre y primer apellido
                if (CaptureInfoAsPN && (string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(FirstLastName))) return false;
                // Si no hay cambios
                if (!this.HasChanges()) return false;
                // Si el control de errores por propiedades tiene algun error
                return _errors.Count <= 0;
            }
        }

        #endregion

        #region Methods

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        public void EndRowEditing()
        {
            try
            {
                NotifyOfPropertyChange(nameof(Emails));
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(EndRowEditing)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
        }

        public void SetForNew()
        {
            try
            {
                List<CostCenterDTO> costCenters = [];

                Id = 0;
                IsActive = true;
                IdentificationNumber = string.Empty;
                SelectedCaptureType = BooksDictionaries.CaptureTypeEnum.PN;
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

                SelectedCountry = _countryCache.Items.FirstOrDefault(x => x.Code == Constant.DefaultCountryCode);
                if (SelectedCountry is not null) SelectedDepartment = SelectedCountry.Departments.Find(x => x.Code == Constant.DefaultDepartmentCode);
                if (SelectedDepartment is not null && SelectedDepartment.Cities is not null) SelectedCityId = SelectedDepartment.Cities.Find(x => x.Code == Constant.DefaultCityCode)?.Id;

                foreach (CostCenterDTO costCenter in _mapper.Map<ObservableCollection<CostCenterDTO>>(_costCenterCache.Items))
                {
                    costCenters.Add(new CostCenterDTO()
                    {
                        Id = costCenter.Id,
                        Name = costCenter.Name,
                        IsSelected = false,
                        CompanyLocation = costCenter.CompanyLocation
                    });
                }

                Zones = _zoneCache.Items;
                CostCenters = new ObservableCollection<CostCenterDTO>(costCenters);

                SeedDefaultValues();
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(IsActive), IsActive);
            this.SeedValue(nameof(SelectedCaptureType), SelectedCaptureType);
            this.SeedValue(nameof(SelectedIdentificationType), SelectedIdentificationType);
            this.SeedValue(nameof(SelectedCountry), SelectedCountry);
            this.SeedValue(nameof(SelectedDepartment), SelectedDepartment);
            this.SeedValue(nameof(SelectedCityId), SelectedCityId);
            this.SeedValue(nameof(Regime), Regime);
            this.AcceptChanges();
        }

        private void ListenCostCenterCheck()
        {
            foreach (var costCenter in CostCenters)
            {
                costCenter.PropertyChanged += CostCenter_PropertyChanged!;
            }

            // Escuchar cuando se agregan nuevos elementos
            CostCenters.CollectionChanged += CostCenter_CollectionChanged!;
        }
        private void CostCenter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CostCenterDTO.IsSelected))
            {
                // Aquí puedes actualizar otra propiedad del ViewModel si necesitas
                this.TrackChange(nameof(SelectedCostCenterIds));
                NotifyOfPropertyChange(() => CanSave);
                NotifyOfPropertyChange(nameof(HasCostCenterErrors));
                NotifyOfPropertyChange(nameof(CostCenterTabTooltip));
            }
        }
        private void CostCenter_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Escuchar cambios de los nuevos elementos
            if (e.NewItems != null)
            {
                foreach (CostCenterDTO p in e.NewItems)
                    p.PropertyChanged += CostCenter_PropertyChanged!;
            }

            // Opcional: dejar de escuchar eliminados
            if (e.OldItems != null)
            {
                foreach (CostCenterDTO p in e.OldItems)
                    p.PropertyChanged -= CostCenter_PropertyChanged!;
            }
            this.TrackChange(nameof(SelectedCostCenterIds));

            NotifyOfPropertyChange(() => CanSave);
        }

        public async Task<UpsertResponseType<SellerGraphQLModel>> ExecuteSaveAsync()
        {

            try
            {
                var transformers = new Dictionary<string, Func<object?, object?>>
                {
                    [nameof(Emails)] = item =>
                    {
                        var email = (EmailGraphQLModel)item!;
                        return new { description = email.Description, email = email.Email };
                    }
                };

                if (IsNewRecord)
                {
                    var (_, query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput", transformers);
                    return await _sellerService.CreateAsync<UpsertResponseType<SellerGraphQLModel>>(query, variables);
                }
                else
                {
                    var (_, query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData", transformers);
                    variables.updateResponseId = Id;
                    return await _sellerService.UpdateAsync<UpsertResponseType<SellerGraphQLModel>>(query, variables);
                }
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<SellerGraphQLModel> result = await ExecuteSaveAsync();

                if (!result.Success)
                {
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    ThemedMessageBox.Show(
                        text: $"El guardado no ha sido exitoso\r\n\r\n{result.Errors.ToUserMessage()}\r\n\r\nVerifique los datos y vuelva a intentarlo",
                        title: $"{result.Message}!",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new SellerCreateMessage { CreatedSeller = result }
                        : new SellerUpdateMessage { UpdatedSeller = result },
                    CancellationToken.None);

                await TryCloseAsync(true);
            }
            catch (AsyncException ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"Error al realizar operación.\r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(SaveAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<SellerGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "seller", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.IsActive))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createSeller",
                [new("input", "CreateSellerInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<SellerGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "seller", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.IsActive))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("updateSeller",
                [new("data", "UpdateSellerInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        public void AddEmail()
        {
            try
            {
                EmailDTO email = new EmailDTO() { Description = EmailDescription ?? "", Email = Email ?? "" };
                Email = string.Empty;
                EmailDescription = string.Empty;
                Emails.Add(email);

                _ = this.SetFocus(nameof(EmailDescription));
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(AddEmail)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
        }

        public void RemoveEmail(object p)
        {
            try
            {
                if (ThemedMessageBox.Show("Confirme ...", $"¿ Confirma que desea eliminar el email : {SelectedEmail?.Email ?? ""} ?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                if (SelectedEmail != null)
                {
                    EmailDTO? emailToDelete = Emails.FirstOrDefault(email => email.Id == SelectedEmail.Id);
                    if (emailToDelete is null) return;
                    Emails.Remove(emailToDelete);
                }
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(RemoveEmail)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
        }

        public SellerDetailViewModel(
            IRepository<SellerGraphQLModel> sellerService,
            IEventAggregator eventAggregator,
            IdentificationTypeCache identificationTypeCache,
            CountryCache countryCache,
            ZoneCache zoneCache,
            CostCenterCache costCenterCache,
            StringLengthCache stringLengthCache,
            IMapper mapper,
            JoinableTaskFactory joinableTaskFactory,
            IGraphQLClient graphQLClient)
        {
            _sellerService = sellerService;
            _eventAggregator = eventAggregator;
            _identificationTypeCache = identificationTypeCache;
            _countryCache = countryCache;
            _zoneCache = zoneCache;
            _costCenterCache = costCenterCache;
            _stringLengthCache = stringLengthCache;
            _mapper = mapper;
            _joinableTaskFactory = joinableTaskFactory;
            _graphQLClient = graphQLClient;
            _errors = [];
            Emails = [];
        }

        public async Task InitializeAsync()
        {
            try
            {
                await CacheBatchLoader.LoadAsync(
                    _graphQLClient, default,
                    _countryCache, _zoneCache, _identificationTypeCache, _costCenterCache);
                Countries = _countryCache.Items;
                Zones = _zoneCache.Items;

                SelectedIdentificationType = _identificationTypeCache.Items.FirstOrDefault(x => x.Code == Constant.IdentificationTypeCodeCC);
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
                var (fragment, query) = _loadByIdQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "id", id)
                    .Build();

                var seller = await _sellerService.FindByIdAsync(query, variables);
                SetForEdit(seller);
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
        public void SetForEdit(SellerGraphQLModel seller)
        {
            Id = seller.Id;
            IsActive = seller.IsActive;

            SelectedIdentificationType = _identificationTypeCache.Items.FirstOrDefault(x => x.Code == seller?.AccountingEntity?.IdentificationType.Code);
            IdentificationNumber = seller?.AccountingEntity?.IdentificationNumber ?? "";
            FirstName = seller?.AccountingEntity?.FirstName ?? "";
            MiddleName = seller?.AccountingEntity?.MiddleName ?? "";
            FirstLastName = seller?.AccountingEntity?.FirstLastName ?? "";
            MiddleLastName = seller?.AccountingEntity?.MiddleLastName ?? "";
            PrimaryPhone = seller?.AccountingEntity?.PrimaryPhone ?? "";
            SecondaryPhone = seller?.AccountingEntity?.SecondaryPhone ?? "";
            PrimaryCellPhone = seller?.AccountingEntity?.PrimaryCellPhone ?? "";
            SecondaryCellPhone = seller?.AccountingEntity?.SecondaryCellPhone ?? "";
            Emails = seller?.AccountingEntity?.Emails is null ? [] : _mapper.Map<ObservableCollection<EmailDTO>>(seller.AccountingEntity.Emails);
            SelectedCountry = Countries?.FirstOrDefault(c => c.Id == seller?.AccountingEntity?.Country.Id);
            SelectedDepartment = SelectedCountry?.Departments.Find(d => d.Id == seller?.AccountingEntity?.Department.Id);
            SelectedCityId = seller?.AccountingEntity?.City.Id;
            Address = seller?.AccountingEntity?.Address ?? "";
            SelectedZone = seller?.Zone is null ? null : Zones?.FirstOrDefault(z => z.Id == seller.Zone.Id);

            ObservableCollection<CostCenterDTO> costCentersSelection = [];
            foreach (CostCenterDTO costCenter in _mapper.Map<ObservableCollection<CostCenterDTO>>(_costCenterCache.Items))
            {
                bool exist = seller?.CostCenters is not null && seller.CostCenters.Any(c => c.Id == costCenter.Id);
                costCentersSelection.Add(new CostCenterDTO()
                {
                    Id = costCenter.Id,
                    Name = costCenter.Name,
                    IsSelected = exist,
                    CompanyLocation = costCenter.CompanyLocation
                });
            }
            CostCenters = costCentersSelection;

            SeedCurrentValues();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(IsActive), IsActive);
            this.SeedValue(nameof(SelectedIdentificationType), SelectedIdentificationType);
            this.SeedValue(nameof(IdentificationNumber), IdentificationNumber);
            this.SeedValue(nameof(SelectedCaptureType), SelectedCaptureType);
            this.SeedValue(nameof(FirstName), FirstName);
            this.SeedValue(nameof(MiddleName), MiddleName);
            this.SeedValue(nameof(FirstLastName), FirstLastName);
            this.SeedValue(nameof(MiddleLastName), MiddleLastName);
            this.SeedValue(nameof(PrimaryPhone), PrimaryPhone);
            this.SeedValue(nameof(SecondaryPhone), SecondaryPhone);
            this.SeedValue(nameof(PrimaryCellPhone), PrimaryCellPhone);
            this.SeedValue(nameof(SecondaryCellPhone), SecondaryCellPhone);
            this.SeedValue(nameof(Address), Address);
            this.SeedValue(nameof(SelectedCountry), SelectedCountry);
            this.SeedValue(nameof(SelectedDepartment), SelectedDepartment);
            this.SeedValue(nameof(SelectedCityId), SelectedCityId);
            this.SeedValue(nameof(SelectedZone), SelectedZone);
            this.SeedValue(nameof(Regime), Regime);
            this.SeedValue(nameof(SelectedCostCenterIds), SelectedCostCenterIds);
            this.AcceptChanges();
        }
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperties();
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }
        #endregion

        #region Validaciones

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            if (_basicDataFields.Contains(propertyName))
            {
                NotifyOfPropertyChange(nameof(BasicDataTabTooltip));
                NotifyOfPropertyChange(nameof(HasBasicDataErrors));
            }
        }

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.TryGetValue(propertyName, out List<string>? value)) return Enumerable.Empty<string>();
            return value;
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = [];

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
                        if (string.IsNullOrEmpty(IdentificationNumber) || IdentificationNumber.Trim().Length < SelectedIdentificationType?.MinimumDocumentLength) AddError(propertyName, "El número de identificación no puede estar vacío");
                        break;
                    case nameof(FirstName):
                        if (string.IsNullOrEmpty(FirstName.Trim()) && CaptureInfoAsPN) AddError(propertyName, "El primer nombre no puede estar vacío");
                        break;
                    case nameof(FirstLastName):
                        if (string.IsNullOrEmpty(FirstLastName.Trim()) && CaptureInfoAsPN) AddError(propertyName, "El primer apellido no puede estar vacío");
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
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(ValidateProperty)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
        }
        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadByIdQuery = new(() =>
        {
            var fields = FieldSpec<SellerGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.IsActive)
                .Select(e => e.AccountingEntity, acc => acc
                    .Field(c => c!.Id)
                    .Field(c => c!.VerificationDigit)
                    .Field(c => c!.IdentificationNumber)
                    .Field(c => c!.FirstName)
                    .Field(c => c!.MiddleName)
                    .Field(c => c!.FirstLastName)
                    .Field(c => c!.MiddleLastName)
                    .Field(c => c!.SearchName)
                    .Field(c => c!.PrimaryPhone)
                    .Field(c => c!.SecondaryPhone)
                    .Field(c => c!.PrimaryCellPhone)
                    .Field(c => c!.SecondaryCellPhone)
                    .Field(c => c!.Address)
                    .Field(c => c!.TelephonicInformation)
                    .Select(e => e!.IdentificationType, co => co
                        .Field(x => x.Id)
                        .Field(x => x.Code))
                    .Select(e => e!.Country, co => co
                        .Field(x => x.Id))
                    .Select(e => e!.City, co => co
                        .Field(x => x.Id))
                    .Select(e => e!.Department, co => co
                        .Field(x => x.Id))
                    .SelectList(e => e!.Emails, co => co
                        .Field(x => x.Id)
                        .Field(x => x.Description)
                        .Field(x => x.Email)
                        .Field(x => x.IsElectronicInvoiceRecipient)))
                .SelectList(e => e.CostCenters!, acc => acc
                    .Field(c => c.Id)
                    .Field(c => c.Name))
                .Select(e => e.Zone, acc => acc
                    .Field(c => c!.Id)
                    .Field(c => c!.Name))
                .Build();

            var fragment = new GraphQLQueryFragment("seller",
                [new("id", "ID!")],
                fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion
        private void ValidateProperties()
        {
            if (CaptureInfoAsPN)
            {
                ValidateProperty(nameof(FirstName), FirstName);
                ValidateProperty(nameof(FirstLastName), FirstLastName);
                ValidateProperty(nameof(IdentificationNumber), IdentificationNumber);
            }
        }
        #endregion

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                if (_emails != null)
                {
                    _emails.CollectionChanged -= Emails_CollectionChanged!;
                }

                foreach (var costCenter in CostCenters)
                {
                    costCenter.PropertyChanged -= CostCenter_PropertyChanged!;
                }
                CostCenters.CollectionChanged -= CostCenter_CollectionChanged!;

                Zones = null!;
                Countries = null!;
                SelectedIdentificationType = null!;
                SelectedCountry = null!;
                this.AcceptChanges();
                Emails?.Clear();
                CostCenters?.Clear();
            }

            return base.OnDeactivateAsync(close, cancellationToken);
        }
    }
}