using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Dictionaries;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
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
        private readonly JoinableTaskFactory _joinableTaskFactory;

        #endregion

        #region State

        public AuthorizationSequenceGraphQLModel? Entity
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Entity));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                    NotifyOfPropertyChange(nameof(OriginVisibility));
                }
            }
        }

        public bool IsNewRecord => Entity == null || Entity.Id < 1;

        private DianSoftwareConfigGraphQLModel? _dianConfig;
        private DianCertificateGraphQLModel? _dianCertificate;

        public bool IsDianAvailable
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsDianAvailable));
                }
            }
        }

        public string DianUnavailableReason
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DianUnavailableReason));
                }
            }
        } = "Verificando disponibilidad DIAN...";

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
        } = 600;

        #endregion

        #region Sequence Origin

        public SequenceOriginEnum SelectedSequenceOrigin
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
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

        public bool LoadOrphan => Entity != null
            && (Entity.CostCenter?.FeCashDefaultAuthorizationSequence?.Id == Entity.Id || Entity.CostCenter?.FeCreditDefaultAuthorizationSequence?.Id == Entity.Id)
            && (Entity.EndRange - Entity.CurrentInvoiceNumber) <= 50;

        #endregion

        #region ComboBox Sources

        public ObservableCollection<CostCenterGraphQLModel> CostCenters
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        } = [];

        public ObservableCollection<AuthorizationSequenceTypeGraphQLModel> AuthorizationSequenceTypes
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AuthorizationSequenceTypes));
                }
            }
        } = [];

        public ObservableCollection<AuthorizationSequenceTypeGraphQLModel> AvailableAuthorizationSequenceTypes
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AvailableAuthorizationSequenceTypes));
                }
            }
        } = [];

        public ObservableCollection<AuthorizationSequenceGraphQLModel> AuthorizationSequences
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AuthorizationSequences));
                    NotifyOfPropertyChange(nameof(Lv1Visibility));
                    NotifyOfPropertyChange(nameof(AuthorizationsVisibility));
                    NotifyOfPropertyChange(nameof(FieldsVisibility));
                }
            }
        } = [];

        public AuthorizationSequenceGraphQLModel? SelectedDianAuthorization
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    if (value != null) SetSelectedAuthorizationSequence(value);
                    if (value == null) ClearValues();
                    NotifyOfPropertyChange(nameof(SelectedDianAuthorization));
                    NotifyOfPropertyChange(nameof(FieldsVisibility));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public ObservableCollection<AuthorizationSequenceGraphQLModel> OrphanAuthorizationSequences
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(OrphanAuthorizationSequences));
                }
            }
        } = [];

        public AuthorizationSequenceGraphQLModel? NextAuthorizationSequenceId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(NextAuthorizationSequenceId));
                    this.TrackChange(nameof(NextAuthorizationSequenceId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public Dictionary<char, string> ModeDictionary => BooksDictionaries.ModeDictionary;

        #endregion

        #region Form Properties

        public string Number
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Number));
                    ValidateProperty(nameof(Number), value);
                    this.TrackChange(nameof(Number));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public string Prefix
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Prefix));
                    ValidateProperty(nameof(Prefix), value);
                    this.TrackChange(nameof(Prefix));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public string TechnicalKey
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(TechnicalKey));
                    ValidateProperty(nameof(TechnicalKey), value);
                    this.TrackChange(nameof(TechnicalKey));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public string Reference
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Reference));
                    ValidateProperty(nameof(Reference), value);
                    this.TrackChange(nameof(Reference));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public int? StartRange
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(StartRange));
                    ValidateProperty(nameof(StartRange), value);
                    this.TrackChange(nameof(StartRange));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public int? EndRange
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(EndRange));
                    ValidateProperty(nameof(EndRange), value);
                    this.TrackChange(nameof(EndRange));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public int? CurrentInvoiceNumber
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CurrentInvoiceNumber));
                    ValidateProperty(nameof(CurrentInvoiceNumber), value);
                    this.TrackChange(nameof(CurrentInvoiceNumber));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public DateOnly? StartDate
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(StartDate));
                    this.TrackChange(nameof(StartDate));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = DateOnly.FromDateTime(DateTime.Now);

        public DateOnly? EndDate
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(EndDate));
                    this.TrackChange(nameof(EndDate));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = DateOnly.FromDateTime(DateTime.Now);

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
        } = true;

        public int? CostCenterId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CostCenterId));
                    ValidateProperty(nameof(CostCenterId), value);
                    this.TrackChange(nameof(CostCenterId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public int? AuthorizationSequenceTypeId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AuthorizationSequenceTypeId));
                    NotifyOfPropertyChange(nameof(TechnicalKey));
                    this.TrackChange(nameof(AuthorizationSequenceTypeId));
                    ValidateProperty(nameof(TechnicalKey), TechnicalKey);
                    ValidateProperty(nameof(AuthorizationSequenceTypeId), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public char Mode
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Mode));
                    this.TrackChange(nameof(Mode));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = 'A';

        public string Origin
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Origin));
                    this.TrackChange(nameof(Origin));
                }
            }
        } = "MANUAL";

        public bool EnabledToCreated => Entity == null;

        #endregion

        #region Validation (INotifyDataErrorInfo)

        private readonly Dictionary<string, List<string>> _errors = [];

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.TryGetValue(propertyName, out List<string>? value))
                return Enumerable.Empty<string>();
            return value;
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
                    AuthorizationSequenceTypeGraphQLModel? selectedType = Entity != null
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
            AuthorizationSequenceTypeCache authorizationSequenceTypeCache,
            JoinableTaskFactory joinableTaskFactory)
        {
            _authorizationSequenceService = authorizationSequenceService;
            _dianConfigService = dianConfigService;
            _dianCertService = dianCertService;
            _eventAggregator = eventAggregator;
            _costCenterCache = costCenterCache;
            _authorizationSequenceTypeCache = authorizationSequenceTypeCache;
            _joinableTaskFactory = joinableTaskFactory;
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

                List<Task> tasks =
                [
                    _costCenterCache.EnsureLoadedAsync(),
                    _authorizationSequenceTypeCache.EnsureLoadedAsync()
                ];

                if (IsNewRecord)
                {
                    tasks.Add(LoadAndCacheDianPrerequisitesAsync());
                }

                await Task.WhenAll(tasks);

                CostCenters = [.. _costCenterCache.Items];
                AuthorizationSequenceTypes = [.. _authorizationSequenceTypeCache.Items];
                AvailableAuthorizationSequenceTypes = [.. _authorizationSequenceTypeCache.Items];

                CostCenterId = Entity?.CostCenter?.Id;
                AuthorizationSequenceTypeId = Entity?.AuthorizationSequenceType?.Id;

                if (LoadOrphan) await LoadOrphanSequencesAsync();
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al inicializar el módulo.\r\n{GetType().Name}.{nameof(OnViewReady)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;

                ValidateProperties();

                if (IsNewRecord)
                    SeedDefaultValues();
                else
                    SeedCurrentValues();

                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        #endregion

        #region DIAN Search

        private async Task LoadAndCacheDianPrerequisitesAsync()
        {
            Task<DianSoftwareConfigGraphQLModel?> configTask = LoadActiveDianSoftwareConfigAsync();
            Task<DianCertificateGraphQLModel?> certTask = LoadActiveDianCertificateAsync();
            await Task.WhenAll(configTask, certTask);

            _dianConfig = await configTask;
            _dianCertificate = await certTask;

            List<string> reasons = [];
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
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    ThemedMessageBox.Show("Atención !", numberingRangeResponse.Message,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(SearchAuthorizationSequencesAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<DianSoftwareConfigGraphQLModel?> LoadActiveDianSoftwareConfigAsync()
        {
            var (fragment, query) = _dianConfigQuery.Value;

            ExpandoObject variables = new GraphQLVariables()
                .For(fragment, "environment", "PRODUCTION")
                .For(fragment, "documentCategory", "INVOICE")
                .Build();

            ActiveDianSoftwareConfigDataContext context = await _dianConfigService.GetDataContextAsync<ActiveDianSoftwareConfigDataContext>(query, variables);
            DianSoftwareConfigGraphQLModel? config = context?.ActiveDianSoftwareConfig;

            if (config == null || config.Id < 1) return null;
            if (!config.IsActive) return null;

            return config;
        }

        private async Task<DianCertificateGraphQLModel?> LoadActiveDianCertificateAsync()
        {
            var (_, query) = _dianCertQuery.Value;

            ActiveDianCertificateDataContext context = await _dianCertService.GetDataContextAsync<ActiveDianCertificateDataContext>(query, new { });
            DianCertificateGraphQLModel? cert = context?.ActiveDianCertificate;

            if (cert == null || cert.Id < 1) return null;

            return cert;
        }

        #endregion

        #region Seed

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(Mode), Mode);
            this.SeedValue(nameof(Origin), Origin);
            this.SeedValue(nameof(StartDate), StartDate);
            this.SeedValue(nameof(EndDate), EndDate);
            this.SeedValue(nameof(IsActive), IsActive);
            this.AcceptChanges();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Number), Number);
            this.SeedValue(nameof(Prefix), Prefix);
            this.SeedValue(nameof(TechnicalKey), TechnicalKey);
            this.SeedValue(nameof(Reference), Reference);
            this.SeedValue(nameof(StartRange), StartRange);
            this.SeedValue(nameof(EndRange), EndRange);
            this.SeedValue(nameof(CurrentInvoiceNumber), CurrentInvoiceNumber);
            this.SeedValue(nameof(StartDate), StartDate);
            this.SeedValue(nameof(EndDate), EndDate);
            this.SeedValue(nameof(Mode), Mode);
            this.SeedValue(nameof(Origin), Origin);
            this.SeedValue(nameof(IsActive), IsActive);
            this.SeedValue(nameof(CostCenterId), CostCenterId);
            this.SeedValue(nameof(AuthorizationSequenceTypeId), AuthorizationSequenceTypeId);
            this.SeedValue(nameof(NextAuthorizationSequenceId), NextAuthorizationSequenceId);
            this.AcceptChanges();
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
                AuthorizationSequenceTypeGraphQLModel? feType = AvailableAuthorizationSequenceTypes.FirstOrDefault(f => f.Prefix == "FE");
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
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(SaveAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
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
            var (fragment, query) = _loadByIdQuery.Value;
            ExpandoObject variables = new GraphQLVariables()
                .For(fragment, "id", id)
                .Build();

            AuthorizationSequenceGraphQLModel entity = await _authorizationSequenceService.FindByIdAsync(query, variables);
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
                var (fragment, query) = _orphanQuery.Value;

                dynamic filters = new ExpandoObject();
                filters.isActive = true;
                filters.costCenterId = Entity!.CostCenter!.Id;
                filters.endDateFrom = DateTime.Today.ToString("yyyy-MM-dd");

                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "filters", filters)
                    .Build();

                AuthorizationSequenceDetailDataContext source = await _authorizationSequenceService.GetDataContextAsync<AuthorizationSequenceDetailDataContext>(query, variables);

                ObservableCollection<AuthorizationSequenceGraphQLModel>? entries = source?.AuthorizationSequences?.Entries;
                if (entries != null)
                {
                    List<AuthorizationSequenceGraphQLModel> orphans = [.. entries
                        .Where(f => f.NextAuthorizationSequence == null)
                        .Where(f => f.Id != Entity!.Id)];

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
                        .Field(c => c!.Id)
                        .Field(c => c!.Name))
                    .Select(e => e.AuthorizationSequenceType, type => type
                        .Field(c => c!.Id)
                        .Field(c => c!.Name)))
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
                        .Field(c => c!.Id)
                        .Field(c => c!.Name))
                    .Select(e => e.AuthorizationSequenceType, type => type
                        .Field(c => c!.Id)
                        .Field(c => c!.Name)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            List<GraphQLQueryParameter> parameters =
            [
                new("data", "UpdateAuthorizationSequenceInput!"),
                new("id", "ID!")
            ];
            var fragment = new GraphQLQueryFragment("updateAuthorizationSequence", parameters, fields, "UpdateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadByIdQuery = new(() =>
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
                    .Field(c => c!.Id)
                    .Field(c => c!.Description))
                .Select(e => e.CostCenter, cat => cat
                    .Field(c => c!.Id)
                    .Field(c => c!.Name)
                    .Select(e => e!.FeCreditDefaultAuthorizationSequence, dep => dep
                        .Field(d => d.Id)
                        .Field(d => d.Description))
                    .Select(e => e!.FeCashDefaultAuthorizationSequence, dep => dep
                        .Field(d => d.Id)
                        .Field(d => d.Description)))
                .Select(e => e.AuthorizationSequenceType, cat => cat
                    .Field(c => c!.Id)
                    .Field(c => c!.Name))
                .Build();

            var fragment = new GraphQLQueryFragment("authorizationSequence",
                [new("id", "ID!")], fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _dianConfigQuery = new(() =>
        {
            var fields = FieldSpec<DianSoftwareConfigGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Field(f => f.ProviderNit)
                .Field(f => f.ProviderDv)
                .Field(f => f.SoftwareId)
                .Field(f => f.ServiceUrl)
                .Field(f => f.WsdlUrl)
                .Field(f => f.IsActive)
                .Build();

            var fragment = new GraphQLQueryFragment("activeDianSoftwareConfig",
                [new("environment", "DianEnvironment!"), new("documentCategory", "DianDocumentCategory!")],
                fields);
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _dianCertQuery = new(() =>
        {
            var fields = FieldSpec<DianCertificateGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Field(f => f.CertificatePem)
                .Field(f => f.PrivateKeyPem)
                .Build();

            var fragment = new GraphQLQueryFragment("activeDianCertificate", [], fields);
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _orphanQuery = new(() =>
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
                        .Field(c => c!.Id)
                        .Field(c => c!.Description))
                    .Select(e => e.CostCenter, cat => cat
                        .Field(c => c!.Id)
                        .Field(c => c!.Name)
                        .Select(e => e!.FeCreditDefaultAuthorizationSequence, dep => dep
                            .Field(d => d.Id)
                            .Field(d => d.Description))
                        .Select(e => e!.FeCashDefaultAuthorizationSequence, dep => dep
                            .Field(d => d.Id)
                            .Field(d => d.Description)))
                    .Select(e => e.AuthorizationSequenceType, cat => cat
                        .Field(c => c!.Id)
                        .Field(c => c!.Name)))
                .Field(o => o.PageNumber)
                .Field(o => o.PageSize)
                .Field(o => o.TotalPages)
                .Field(o => o.TotalEntries)
                .Build();

            var fragment = new GraphQLQueryFragment("authorizationSequencesPage",
                [new("filters", "AuthorizationSequenceFilters")],
                fields, "AuthorizationSequences");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion
    }
}
