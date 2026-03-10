using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Dictionaries;
using Extensions.Global;
using GraphQL.Client.Http;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Dictionaries.BooksDictionaries;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.AuthorizationSequence.ViewModels
{
    public class AuthorizationSequenceDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<AuthorizationSequenceGraphQLModel> _authorizationSequenceService;
        private readonly IRepository<DianSoftwareConfigGraphQLModel> _dianConfigService;
        private readonly IRepository<DianCertificateGraphQLModel> _dianCertService;
        private readonly IEventAggregator _eventAggregator;
        private readonly CostCenterCache _costCenterCache;
        private readonly AuthorizationSequenceTypeCache _authorizationSequenceTypeCache;

        #endregion

        #region State

        private AuthorizationSequenceGraphQLModel? _entity;
        public AuthorizationSequenceGraphQLModel? Entity
        {
            get => _entity;
            set
            {
                if (_entity != value)
                {
                    _entity = value;
                    NotifyOfPropertyChange(nameof(Entity));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                    NotifyOfPropertyChange(nameof(OriginVisibility));
                }
            }
        }

        public bool IsNewRecord => Entity == null || Entity.Id < 1;

        private DianSoftwareConfigGraphQLModel? _dianConfig;
        private DianCertificateGraphQLModel? _dianCertificate;

        private bool _isDianAvailable = false;
        public bool IsDianAvailable
        {
            get => _isDianAvailable;
            set
            {
                if (_isDianAvailable != value)
                {
                    _isDianAvailable = value;
                    NotifyOfPropertyChange(nameof(IsDianAvailable));
                }
            }
        }

        private string _dianUnavailableReason = "Verificando disponibilidad DIAN...";
        public string DianUnavailableReason
        {
            get => _dianUnavailableReason;
            set
            {
                if (_dianUnavailableReason != value)
                {
                    _dianUnavailableReason = value;
                    NotifyOfPropertyChange(nameof(DianUnavailableReason));
                }
            }
        }

        private bool _isBusy;
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

        #endregion

        #region Sequence Origin

        private SequenceOriginEnum _selectedSequenceOrigin;
        public SequenceOriginEnum SelectedSequenceOrigin
        {
            get => _selectedSequenceOrigin;
            set
            {
                if (_selectedSequenceOrigin != value)
                {
                    _selectedSequenceOrigin = value;
                    Origin = value == SequenceOriginEnum.D ? "DIAN" : "MANUAL";
                    NotifyOfPropertyChange(nameof(SelectedSequenceOrigin));
                    NotifyOfPropertyChange(nameof(Lv1Visibility));
                    NotifyOfPropertyChange(nameof(SequenceD));
                    NotifyOfPropertyChange(nameof(EnabledAST));
                    NotifyOfPropertyChange(nameof(CanSave));
                    NotifyOfPropertyChange(nameof(FieldsVisibility));
                    NotifyOfPropertyChange(nameof(AuthorizationsVisibility));
                    ClearValues();
                }
            }
        }

        public bool SequenceD => IsNewRecord
            ? SelectedSequenceOrigin.Equals(SequenceOriginEnum.D)
            : string.Equals(Entity?.Origin, "DIAN", StringComparison.OrdinalIgnoreCase);

        public bool EnabledAST => IsNewRecord
            ? SelectedSequenceOrigin.Equals(SequenceOriginEnum.M) || (SelectedSequenceOrigin.Equals(SequenceOriginEnum.D) && string.IsNullOrEmpty(TechnicalKey))
            : !SequenceD;

        #endregion

        #region Visibility

        public Visibility OriginVisibility => (Entity == null || Entity.Id < 1) ? Visibility.Visible : Visibility.Collapsed;

        public Visibility Lv1Visibility => SelectedSequenceOrigin.Equals(SequenceOriginEnum.D) && !(AuthorizationSequences?.Count > 0) ? Visibility.Visible : Visibility.Collapsed;

        public Visibility AuthorizationsVisibility => SelectedSequenceOrigin.Equals(SequenceOriginEnum.D) && AuthorizationSequences?.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility FieldsVisibility => (SelectedSequenceOrigin.Equals(SequenceOriginEnum.M) || (SelectedSequenceOrigin.Equals(SequenceOriginEnum.D) && SelectedDianAuthorization != null)) ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ReliefVisibility => LoadOrphan ? Visibility.Visible : Visibility.Collapsed;

        public bool LoadOrphan => _entity != null
            && (_entity.CostCenter?.FeCashDefaultAuthorizationSequence?.Id == Entity!.Id || _entity.CostCenter?.FeCreditDefaultAuthorizationSequence?.Id == Entity!.Id)
            && (_entity.EndRange - _entity.CurrentInvoiceNumber) <= 50;

        #endregion

        #region ComboBox Sources

        private ObservableCollection<CostCenterGraphQLModel> _costCenters = [];
        public ObservableCollection<CostCenterGraphQLModel> CostCenters
        {
            get => _costCenters;
            set
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        }

        private ObservableCollection<AuthorizationSequenceTypeGraphQLModel> _authorizationSequenceTypes = [];
        public ObservableCollection<AuthorizationSequenceTypeGraphQLModel> AuthorizationSequenceTypes
        {
            get => _authorizationSequenceTypes;
            set
            {
                if (_authorizationSequenceTypes != value)
                {
                    _authorizationSequenceTypes = value;
                    NotifyOfPropertyChange(nameof(AuthorizationSequenceTypes));
                }
            }
        }

        private ObservableCollection<AuthorizationSequenceTypeGraphQLModel> _availableAuthorizationSequenceTypes = [];
        public ObservableCollection<AuthorizationSequenceTypeGraphQLModel> AvailableAuthorizationSequenceTypes
        {
            get => _availableAuthorizationSequenceTypes;
            set
            {
                if (_availableAuthorizationSequenceTypes != value)
                {
                    _availableAuthorizationSequenceTypes = value;
                    NotifyOfPropertyChange(nameof(AvailableAuthorizationSequenceTypes));
                }
            }
        }

        private ObservableCollection<AuthorizationSequenceGraphQLModel> _authorizationSequences = [];
        public ObservableCollection<AuthorizationSequenceGraphQLModel> AuthorizationSequences
        {
            get => _authorizationSequences;
            set
            {
                if (_authorizationSequences != value)
                {
                    _authorizationSequences = value;
                    NotifyOfPropertyChange(nameof(AuthorizationSequences));
                    NotifyOfPropertyChange(nameof(Lv1Visibility));
                    NotifyOfPropertyChange(nameof(AuthorizationsVisibility));
                    NotifyOfPropertyChange(nameof(FieldsVisibility));
                }
            }
        }

        private AuthorizationSequenceGraphQLModel? _selectedDianAuthorization;
        public AuthorizationSequenceGraphQLModel? SelectedDianAuthorization
        {
            get => _selectedDianAuthorization;
            set
            {
                if (_selectedDianAuthorization != value)
                {
                    _selectedDianAuthorization = value;
                    if (value != null) SetSelectedAuthorizationSequence(value);
                    if (value == null) ClearValues();
                    NotifyOfPropertyChange(nameof(SelectedDianAuthorization));
                    NotifyOfPropertyChange(nameof(FieldsVisibility));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private ObservableCollection<AuthorizationSequenceGraphQLModel> _orphanAuthorizationSequences = [];
        public ObservableCollection<AuthorizationSequenceGraphQLModel> OrphanAuthorizationSequences
        {
            get => _orphanAuthorizationSequences;
            set
            {
                if (_orphanAuthorizationSequences != value)
                {
                    _orphanAuthorizationSequences = value;
                    NotifyOfPropertyChange(nameof(OrphanAuthorizationSequences));
                }
            }
        }

        private AuthorizationSequenceGraphQLModel? _nextAuthorizationSequenceId;
        public AuthorizationSequenceGraphQLModel? NextAuthorizationSequenceId
        {
            get => _nextAuthorizationSequenceId;
            set
            {
                if (_nextAuthorizationSequenceId != value)
                {
                    _nextAuthorizationSequenceId = value;
                    NotifyOfPropertyChange(nameof(NextAuthorizationSequenceId));
                    this.TrackChange(nameof(NextAuthorizationSequenceId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public Dictionary<char, string> ModeDictionary => BooksDictionaries.ModeDictionary;

        #endregion

        #region Form Properties

        private string _number = string.Empty;
        public string Number
        {
            get => _number;
            set
            {
                if (_number != value)
                {
                    _number = value;
                    NotifyOfPropertyChange(nameof(Number));
                    ValidateProperty(nameof(Number), value);
                    this.TrackChange(nameof(Number));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _prefix = string.Empty;
        public string Prefix
        {
            get => _prefix;
            set
            {
                if (_prefix != value)
                {
                    _prefix = value;
                    NotifyOfPropertyChange(nameof(Prefix));
                    ValidateProperty(nameof(Prefix), value);
                    this.TrackChange(nameof(Prefix));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _technicalKey = string.Empty;
        public string TechnicalKey
        {
            get => _technicalKey;
            set
            {
                if (_technicalKey != value)
                {
                    _technicalKey = value;
                    NotifyOfPropertyChange(nameof(TechnicalKey));
                    ValidateProperty(nameof(TechnicalKey), value);
                    this.TrackChange(nameof(TechnicalKey));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _reference = string.Empty;
        public string Reference
        {
            get => _reference;
            set
            {
                if (_reference != value)
                {
                    _reference = value;
                    NotifyOfPropertyChange(nameof(Reference));
                    ValidateProperty(nameof(Reference), value);
                    this.TrackChange(nameof(Reference));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int? _startRange;
        public int? StartRange
        {
            get => _startRange;
            set
            {
                if (_startRange != value)
                {
                    _startRange = value;
                    NotifyOfPropertyChange(nameof(StartRange));
                    ValidateProperty(nameof(StartRange), value);
                    this.TrackChange(nameof(StartRange));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int? _endRange;
        public int? EndRange
        {
            get => _endRange;
            set
            {
                if (_endRange != value)
                {
                    _endRange = value;
                    NotifyOfPropertyChange(nameof(EndRange));
                    ValidateProperty(nameof(EndRange), value);
                    this.TrackChange(nameof(EndRange));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int? _currentInvoiceNumber;
        public int? CurrentInvoiceNumber
        {
            get => _currentInvoiceNumber;
            set
            {
                if (_currentInvoiceNumber != value)
                {
                    _currentInvoiceNumber = value;
                    NotifyOfPropertyChange(nameof(CurrentInvoiceNumber));
                    ValidateProperty(nameof(CurrentInvoiceNumber), value);
                    this.TrackChange(nameof(CurrentInvoiceNumber));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private DateOnly? _startDate = DateOnly.FromDateTime(DateTime.Now);
        public DateOnly? StartDate
        {
            get => _startDate;
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    NotifyOfPropertyChange(nameof(StartDate));
                    this.TrackChange(nameof(StartDate));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private DateOnly? _endDate = DateOnly.FromDateTime(DateTime.Now);
        public DateOnly? EndDate
        {
            get => _endDate;
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    NotifyOfPropertyChange(nameof(EndDate));
                    this.TrackChange(nameof(EndDate));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    NotifyOfPropertyChange(nameof(IsActive));
                    this.TrackChange(nameof(IsActive));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int? _costCenterId;
        public int? CostCenterId
        {
            get => _costCenterId;
            set
            {
                if (_costCenterId != value)
                {
                    _costCenterId = value;
                    NotifyOfPropertyChange(nameof(CostCenterId));
                    ValidateProperty(nameof(CostCenterId), value);
                    this.TrackChange(nameof(CostCenterId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int? _authorizationSequenceTypeId;
        public int? AuthorizationSequenceTypeId
        {
            get => _authorizationSequenceTypeId;
            set
            {
                if (_authorizationSequenceTypeId != value)
                {
                    _authorizationSequenceTypeId = value;
                    NotifyOfPropertyChange(nameof(AuthorizationSequenceTypeId));
                    NotifyOfPropertyChange(nameof(TechnicalKey));
                    this.TrackChange(nameof(AuthorizationSequenceTypeId));
                    ValidateProperty(nameof(TechnicalKey), TechnicalKey);
                    ValidateProperty(nameof(AuthorizationSequenceTypeId), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private char _mode = 'A';
        public char Mode
        {
            get => _mode;
            set
            {
                if (_mode != value)
                {
                    _mode = value;
                    NotifyOfPropertyChange(nameof(Mode));
                    this.TrackChange(nameof(Mode));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _origin = "MANUAL";
        public string Origin
        {
            get => _origin;
            set
            {
                if (_origin != value)
                {
                    _origin = value;
                    NotifyOfPropertyChange(nameof(Origin));
                    this.TrackChange(nameof(Origin));
                }
            }
        }

        public bool EnabledToCreated => Entity == null;

        #endregion

        #region Validation (INotifyDataErrorInfo)

        private readonly Dictionary<string, List<string>> _errors = [];

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null!;
            return _errors[propertyName];
        }

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
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

        private void ValidateProperty(string propertyName, int? value)
        {
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(CostCenterId):
                    if (!value.HasValue || value == 0) AddError(propertyName, "Debe seleccionar un centro de costo");
                    break;
                case nameof(AuthorizationSequenceTypeId):
                    if (!value.HasValue || value == 0) AddError(propertyName, "Debe seleccionar un tipo de autorización");
                    break;
                case nameof(StartRange):
                    if (!value.HasValue) AddError(propertyName, "El rango inicial no puede estar vacío");
                    break;
                case nameof(EndRange):
                    if (!value.HasValue) AddError(propertyName, "El rango final no puede estar vacío");
                    break;
                case nameof(CurrentInvoiceNumber):
                    if (!value.HasValue) AddError(propertyName, "El número de factura no puede estar vacío");
                    if (value.HasValue && (value < StartRange || value > EndRange)) AddError(propertyName, "El número de factura debe estar dentro del rango");
                    break;
            }
        }

        private void ValidateProperty(string propertyName, string value)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty;
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(Number):
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "El número de autorización no puede estar vacío");
                    break;
                case nameof(Prefix):
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "El prefijo no puede estar vacío");
                    if (!string.IsNullOrEmpty(value) && int.TryParse(value[^1].ToString(), out _)) AddError(propertyName, "El último carácter no debe ser numérico");
                    break;
                case nameof(Reference):
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "La referencia no puede estar vacía");
                    break;
                case nameof(TechnicalKey):
                    var selectedType = Entity != null
                        ? Entity.AuthorizationSequenceType
                        : AuthorizationSequenceTypeId is > 0
                            ? AuthorizationSequenceTypes.FirstOrDefault(f => f.Id == AuthorizationSequenceTypeId)
                            : null;
                    if (string.IsNullOrEmpty(value) && selectedType?.Prefix == "FE") AddError(propertyName, "La clave técnica no puede estar vacía");
                    break;
            }
        }

        private void ValidateProperties()
        {
            ValidateProperty(nameof(CostCenterId), CostCenterId);
            ValidateProperty(nameof(AuthorizationSequenceTypeId), AuthorizationSequenceTypeId);
            ValidateProperty(nameof(Number), Number);
            ValidateProperty(nameof(Prefix), Prefix);
            ValidateProperty(nameof(Reference), Reference);
            ValidateProperty(nameof(TechnicalKey), TechnicalKey);
            ValidateProperty(nameof(CurrentInvoiceNumber), CurrentInvoiceNumber);
            ValidateProperty(nameof(StartRange), StartRange);
            ValidateProperty(nameof(EndRange), EndRange);
        }

        #endregion

        #region Button States

        public bool CanSave => !HasErrors
            && AuthorizationSequenceTypeId is > 0
            && CostCenterId is > 0
            && Mode != '\0'
            && this.HasChanges();

        #endregion

        #region Commands

        private ICommand? _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                _saveCommand ??= new AsyncCommand(SaveAsync);
                return _saveCommand;
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

        private ICommand? _searchDianCommand;
        public ICommand SearchDianCommand
        {
            get
            {
                _searchDianCommand ??= new AsyncCommand(SearchAuthorizationSequencesAsync);
                return _searchDianCommand;
            }
        }

        #endregion

        #region Constructor

        public AuthorizationSequenceDetailViewModel(
            IRepository<AuthorizationSequenceGraphQLModel> authorizationSequenceService,
            IRepository<DianSoftwareConfigGraphQLModel> dianConfigService,
            IRepository<DianCertificateGraphQLModel> dianCertService,
            IEventAggregator eventAggregator,
            CostCenterCache costCenterCache,
            AuthorizationSequenceTypeCache authorizationSequenceTypeCache)
        {
            _authorizationSequenceService = authorizationSequenceService;
            _dianConfigService = dianConfigService;
            _dianCertService = dianCertService;
            _eventAggregator = eventAggregator;
            _costCenterCache = costCenterCache;
            _authorizationSequenceTypeCache = authorizationSequenceTypeCache;
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                IsBusy = true;
                SelectedSequenceOrigin = SequenceOriginEnum.M;

                var tasks = new List<Task>
                {
                    _costCenterCache.EnsureLoadedAsync(),
                    _authorizationSequenceTypeCache.EnsureLoadedAsync()
                };

                if (IsNewRecord)
                {
                    tasks.Add(LoadAndCacheDianPrerequisitesAsync());
                }

                await Task.WhenAll(tasks);

                CostCenters = [.. _costCenterCache.Items];
                AuthorizationSequenceTypes = [.. _authorizationSequenceTypeCache.Items];
                AvailableAuthorizationSequenceTypes = [.. _authorizationSequenceTypeCache.Items];

                CostCenterId = _entity?.CostCenter?.Id;
                AuthorizationSequenceTypeId = _entity?.AuthorizationSequenceType?.Id;

                if (LoadOrphan) await LoadOrphanSequencesAsync();
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.OnViewReady: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;

                ValidateProperties();
                this.AcceptChanges();

                if (Entity == null)
                {
                    this.TrackChange(nameof(Mode));
                    this.TrackChange(nameof(Origin));
                    this.TrackChange(nameof(StartDate));
                    this.TrackChange(nameof(EndDate));
                }

                NotifyOfPropertyChange(nameof(CanSave));
                System.Windows.Application.Current.Dispatcher.BeginInvoke(
                    new System.Action(() => this.SetFocus(() => Number)),
                    System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }
        }

        #endregion

        #region DIAN Search

        private async Task LoadAndCacheDianPrerequisitesAsync()
        {
            var configTask = LoadActiveDianSoftwareConfigAsync();
            var certTask = LoadActiveDianCertificateAsync();
            await Task.WhenAll(configTask, certTask);

            _dianConfig = configTask.Result;
            _dianCertificate = certTask.Result;

            var reasons = new List<string>();
            if (_dianConfig == null) reasons.Add("No hay configuración DIAN activa para facturación electrónica");
            if (_dianCertificate == null) reasons.Add("No hay certificado DIAN activo");

            IsDianAvailable = reasons.Count == 0;
            DianUnavailableReason = reasons.Count > 0 ? string.Join("\n", reasons) : string.Empty;
        }

        public async Task SearchAuthorizationSequencesAsync()
        {
            try
            {
                IsBusy = true;

                var numberingRangeResponse = await Task.Run(() => GetAuthorizationSequences.GetNumberingRange(_dianConfig!, _dianCertificate!));
                if (numberingRangeResponse.Status)
                {
                    AuthorizationSequences = [.. numberingRangeResponse.AuthorizationSequences];
                    SelectedDianAuthorization = null;
                }
                else
                {
                    ThemedMessageBox.Show("Atención !", numberingRangeResponse.Message,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception e)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"Error al consultar la DIAN: {e.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<DianSoftwareConfigGraphQLModel?> LoadActiveDianSoftwareConfigAsync()
        {
            string query = _dianConfigQuery.Value;

            dynamic variables = new ExpandoObject();
            variables.activeDianSoftwareConfigEnvironment = "PRODUCTION";
            variables.activeDianSoftwareConfigDocumentCategory = "INVOICE";

            var context = await _dianConfigService.GetDataContextAsync<ActiveDianSoftwareConfigDataContext>(query, variables);
            return context?.ActiveDianSoftwareConfig;
        }

        private async Task<DianCertificateGraphQLModel?> LoadActiveDianCertificateAsync()
        {
            string query = _dianCertQuery.Value;

            dynamic variables = new ExpandoObject();

            var context = await _dianCertService.GetDataContextAsync<ActiveDianCertificateDataContext>(query, variables);
            return context?.ActiveDianCertificate;
        }

        #endregion

        #region Helper Methods

        public void SetSelectedAuthorizationSequence(AuthorizationSequenceGraphQLModel authorization)
        {
            Number = authorization.Number;
            Prefix = authorization.Prefix;
            TechnicalKey = authorization.TechnicalKey;
            StartDate = authorization.StartDate;
            EndDate = authorization.EndDate;
            StartRange = authorization.StartRange;
            EndRange = authorization.EndRange;
            CurrentInvoiceNumber = authorization.StartRange;

            if (string.IsNullOrEmpty(authorization.TechnicalKey))
            {
                AvailableAuthorizationSequenceTypes = new ObservableCollection<AuthorizationSequenceTypeGraphQLModel>(
                    AuthorizationSequenceTypes.Where(f => f.Prefix != "FE"));
                AuthorizationSequenceTypeId = null;
            }
            else
            {
                AvailableAuthorizationSequenceTypes = [.. AuthorizationSequenceTypes];
                var feType = AvailableAuthorizationSequenceTypes.FirstOrDefault(f => f.Prefix == "FE");
                AuthorizationSequenceTypeId = feType?.Id;
            }
            NotifyOfPropertyChange(nameof(EnabledAST));
        }

        public void ClearValues()
        {
            Number = string.Empty;
            Prefix = string.Empty;
            TechnicalKey = string.Empty;
            StartDate = null;
            EndDate = null;
            StartRange = null;
            EndRange = null;
            CurrentInvoiceNumber = null;
            AvailableAuthorizationSequenceTypes = [.. AuthorizationSequenceTypes];
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<AuthorizationSequenceGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(
                        text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo",
                        title: $"{result.Message}!",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new AuthorizationSequenceCreateMessage { CreatedAuthorizationSequence = result }
                        : new AuthorizationSequenceUpdateMessage { UpdatedAuthorizationSequence = result }
                );

                await TryCloseAsync(true);
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content!.ToString()!);
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"\r\n{graphQLError.Errors[0].Message}\r\n{graphQLError.Errors[0].Extensions.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{currentMethod!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<UpsertResponseType<AuthorizationSequenceGraphQLModel>> ExecuteSaveAsync()
        {
            if (IsNewRecord)
            {
                string query = _createQuery.Value;
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                return await _authorizationSequenceService.CreateAsync<UpsertResponseType<AuthorizationSequenceGraphQLModel>>(query, variables);
            }
            else
            {
                string query = _updateQuery.Value;
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = Entity!.Id;
                return await _authorizationSequenceService.UpdateAsync<UpsertResponseType<AuthorizationSequenceGraphQLModel>>(query, variables);
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        #endregion

        #region Load Data

        public async Task LoadDataForEditAsync(int id)
        {
            string query = _loadByIdQuery.Value;
            dynamic variables = new ExpandoObject();
            variables.singleItemResponseId = id;

            var entity = await _authorizationSequenceService.FindByIdAsync(query, variables);
            Entity = entity;
            PopulateFromEntity(entity);
        }

        private void PopulateFromEntity(AuthorizationSequenceGraphQLModel entity)
        {
            Number = entity.Number;
            Reference = entity.Reference;
            Prefix = entity.Prefix;
            TechnicalKey = entity.TechnicalKey;
            Mode = entity.Mode;
            Origin = entity.Origin;
            StartDate = entity.StartDate;
            EndDate = entity.EndDate;
            StartRange = entity.StartRange;
            EndRange = entity.EndRange;
            CurrentInvoiceNumber = entity.CurrentInvoiceNumber;
            IsActive = entity.IsActive;
            NotifyOfPropertyChange(nameof(SequenceD));
            NotifyOfPropertyChange(nameof(EnabledAST));
        }

        private async Task LoadOrphanSequencesAsync()
        {
            try
            {
                IsBusy = true;
                string query = _orphanQuery.Value;
                dynamic variables = new ExpandoObject();
                variables.authorizationSequencesFilters = new ExpandoObject();
                variables.authorizationSequencesFilters.isActive = true;
                variables.authorizationSequencesFilters.costCenterId = _entity!.CostCenter!.Id;
                variables.authorizationSequencesFilters.endDateFrom = DateTime.Today.ToString("yyyy-MM-dd");

                AuthorizationSequenceDetailDataContext source = await _authorizationSequenceService.GetDataContextAsync<AuthorizationSequenceDetailDataContext>(query, variables);

                ObservableCollection<AuthorizationSequenceGraphQLModel>? entries = source?.AuthorizationSequences?.Entries;
                if (entries != null)
                {
                    List<AuthorizationSequenceGraphQLModel> orphans = entries
                        .Where(f => f.NextAuthorizationSequence == null)
                        .Where(f => f.Id != Entity!.Id)
                        .ToList();

                    if (Entity!.NextAuthorizationSequence != null && !orphans.Any(f => f.Id == Entity.NextAuthorizationSequence.Id))
                    {
                        orphans.Add(Entity.NextAuthorizationSequence);
                    }
                    OrphanAuthorizationSequences = [.. orphans];
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<string> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<AuthorizationSequenceGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "authorizationSequence", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    .Field(e => e.Prefix)
                    .Field(e => e.CurrentInvoiceNumber)
                    .Field(e => e.StartRange)
                    .Field(e => e.EndRange)
                    .Field(e => e.StartDate)
                    .Field(e => e.EndDate)
                    .Field(e => e.IsActive)
                    .Field(e => e.Origin)
                    .Select(e => e.CostCenter, cos => cos
                        .Field(c => c.Id)
                        .Field(c => c.Name))
                    .Select(e => e.AuthorizationSequenceType, type => type
                        .Field(c => c.Id)
                        .Field(c => c.Name)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateAuthorizationSequenceInput!");
            var fragment = new GraphQLQueryFragment("createAuthorizationSequence", [parameter], fields, "CreateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        private static readonly Lazy<string> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<AuthorizationSequenceGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "authorizationSequence", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    .Field(e => e.Prefix)
                    .Field(e => e.CurrentInvoiceNumber)
                    .Field(e => e.StartRange)
                    .Field(e => e.EndRange)
                    .Field(e => e.StartDate)
                    .Field(e => e.EndDate)
                    .Field(e => e.IsActive)
                    .Field(e => e.Origin)
                    .Select(e => e.CostCenter, cos => cos
                        .Field(c => c.Id)
                        .Field(c => c.Name))
                    .Select(e => e.AuthorizationSequenceType, type => type
                        .Field(c => c.Id)
                        .Field(c => c.Name)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateAuthorizationSequenceInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateAuthorizationSequence", parameters, fields, "UpdateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        private static readonly Lazy<string> _loadByIdQuery = new(() =>
        {
            var fields = FieldSpec<AuthorizationSequenceGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.Description)
                .Field(e => e.Number)
                .Field(e => e.IsActive)
                .Field(e => e.Origin)
                .Field(e => e.Prefix)
                .Field(e => e.CurrentInvoiceNumber)
                .Field(e => e.Mode)
                .Field(e => e.TechnicalKey)
                .Field(e => e.Reference)
                .Field(e => e.StartDate)
                .Field(e => e.EndDate)
                .Field(e => e.StartRange)
                .Field(e => e.EndRange)
                .Select(e => e.NextAuthorizationSequence, cat => cat
                    .Field(c => c.Id)
                    .Field(c => c.Description))
                .Select(e => e.CostCenter, cat => cat
                    .Field(c => c.Id)
                    .Field(c => c.Name)
                    .Select(e => e.FeCreditDefaultAuthorizationSequence, dep => dep
                        .Field(d => d.Id)
                        .Field(d => d.Description))
                    .Select(e => e.FeCashDefaultAuthorizationSequence, dep => dep
                        .Field(d => d.Id)
                        .Field(d => d.Description)))
                .Select(e => e.AuthorizationSequenceType, cat => cat
                    .Field(c => c.Id)
                    .Field(c => c.Name))
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("authorizationSequence", [parameter], fields, "SingleItemResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        });

        private static readonly Lazy<string> _dianConfigQuery = new(() =>
        {
            var fields = FieldSpec<DianSoftwareConfigGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Field(f => f.ProviderNit)
                .Field(f => f.ProviderDv)
                .Field(f => f.SoftwareId)
                .Field(f => f.ServiceUrl)
                .Field(f => f.WsdlUrl)
                .Build();

            var envParam = new GraphQLQueryParameter("environment", "DianEnvironment!");
            var catParam = new GraphQLQueryParameter("documentCategory", "DianDocumentCategory!");
            var fragment = new GraphQLQueryFragment("activeDianSoftwareConfig", [envParam, catParam], fields);
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        });

        private static readonly Lazy<string> _dianCertQuery = new(() =>
        {
            var fields = FieldSpec<DianCertificateGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Field(f => f.CertificatePem)
                .Field(f => f.PrivateKeyPem)
                .Build();

            var fragment = new GraphQLQueryFragment("activeDianCertificate", [], fields);
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        });

        private static readonly Lazy<string> _orphanQuery = new(() =>
        {
            var fields = FieldSpec<PageType<AuthorizationSequenceGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    .Field(e => e.Number)
                    .Field(e => e.IsActive)
                    .Field(e => e.Prefix)
                    .Field(e => e.CurrentInvoiceNumber)
                    .Field(e => e.Mode)
                    .Field(e => e.TechnicalKey)
                    .Field(e => e.Reference)
                    .Field(e => e.StartDate)
                    .Field(e => e.EndDate)
                    .Field(e => e.StartRange)
                    .Field(e => e.EndRange)
                    .Select(e => e.NextAuthorizationSequence, cat => cat
                        .Field(c => c.Id)
                        .Field(c => c.Description))
                    .Select(e => e.CostCenter, cat => cat
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                        .Select(e => e.FeCreditDefaultAuthorizationSequence, dep => dep
                            .Field(d => d.Id)
                            .Field(d => d.Description))
                        .Select(e => e.FeCashDefaultAuthorizationSequence, dep => dep
                            .Field(d => d.Id)
                            .Field(d => d.Description)))
                    .Select(e => e.AuthorizationSequenceType, cat => cat
                        .Field(c => c.Id)
                        .Field(c => c.Name)))
                .Field(o => o.PageNumber)
                .Field(o => o.PageSize)
                .Field(o => o.TotalPages)
                .Field(o => o.TotalEntries)
                .Build();

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var filtersParam = new GraphQLQueryParameter("filters", "AuthorizationSequenceFilters");
            var fragment = new GraphQLQueryFragment("authorizationSequencesPage", [paginationParam, filtersParam], fields, "AuthorizationSequences");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        });

        #endregion

        #region Helper

        public new void AcceptChanges()
        {
            ViewModelExtensions.AcceptChanges(this);
        }

        #endregion
    }
}
