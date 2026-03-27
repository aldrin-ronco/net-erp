using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.Global;
using NetErp.Billing.PriceList.DTO;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Billing.PriceList.ViewModels
{
    public class UpdatePriceListModalViewModel : Screen, INotifyDataErrorInfo
    {
        private readonly Helpers.IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IMapper _autoMapper;
        private readonly Dictionary<string, List<string>> _errors;
        private readonly IRepository<PriceListGraphQLModel> _priceListService;
        private readonly IGraphQLClient _graphQLClient;
        private readonly StorageCache _storageCache;
        private readonly CostCenterCache _costCenterCache;
        private readonly PaymentMethodCache _paymentMethodCache;

        #region Properties

        public int Id { get; set; }

        private string _name = string.Empty;

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    ValidateProperty(nameof(Name), value);
                    NotifyOfPropertyChange(nameof(Name));
                    this.TrackChange(nameof(Name));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _isTaxable;

        public bool IsTaxable
        {
            get { return _isTaxable; }
            set
            {
                if (_isTaxable != value)
                {
                    _isTaxable = value;
                    NotifyOfPropertyChange(nameof(IsTaxable));
                    this.TrackChange(nameof(IsTaxable));
                    NotifyOfPropertyChange(nameof(CanSave));
                    if (value is false) PriceListIncludeTax = false;
                    RefreshCostCenters();
                }
            }
        }

        private bool _priceListIncludeTax;

        public bool PriceListIncludeTax
        {
            get { return _priceListIncludeTax; }
            set
            {
                if (_priceListIncludeTax != value)
                {
                    _priceListIncludeTax = value;
                    NotifyOfPropertyChange(nameof(PriceListIncludeTax));
                    this.TrackChange(nameof(PriceListIncludeTax));
                    NotifyOfPropertyChange(nameof(CanSave));
                    RefreshCostCenters();
                }
            }
        }

        private bool _useAlternativeFormula;

        public bool UseAlternativeFormula
        {
            get { return _useAlternativeFormula; }
            set
            {
                if (_useAlternativeFormula != value)
                {
                    _useAlternativeFormula = value;
                    NotifyOfPropertyChange(nameof(UseAlternativeFormula));
                    this.TrackChange(nameof(UseAlternativeFormula));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _costMode = "USE_AVERAGE_COST";

        public string CostMode
        {
            get => _costMode;
            set
            {
                if (_costMode != value)
                {
                    _costMode = value;
                    NotifyOfPropertyChange(nameof(CostMode));
                    this.TrackChange(nameof(CostMode));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private PriceListCostModeEnum _selectedCostMode = PriceListCostModeEnum.USE_AVERAGE_COST;

        public PriceListCostModeEnum SelectedCostMode
        {
            get => _selectedCostMode;
            set
            {
                if (_selectedCostMode != value)
                {
                    _selectedCostMode = value;
                    CostMode = value.ToString();
                    NotifyOfPropertyChange(nameof(SelectedCostMode));
                    NotifyOfPropertyChange(nameof(ShowStorageSelector));
                    if (value == PriceListCostModeEnum.USE_AVERAGE_COST)
                    {
                        SelectedStorage = null;
                        ClearErrors(nameof(SelectedStorage));
                    }
                    else
                    {
                        ValidateStorage();
                    }
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool ShowStorageSelector => SelectedCostMode == PriceListCostModeEnum.COST_BY_STORAGE;

        private bool _editablePrice;

        public bool EditablePrice
        {
            get { return _editablePrice; }
            set
            {
                if (_editablePrice != value)
                {
                    _editablePrice = value;
                    NotifyOfPropertyChange(nameof(EditablePrice));
                    this.TrackChange(nameof(EditablePrice));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _autoApplyDiscount;

        public bool AutoApplyDiscount
        {
            get { return _autoApplyDiscount; }
            set
            {
                if (_autoApplyDiscount != value)
                {
                    _autoApplyDiscount = value;
                    NotifyOfPropertyChange(nameof(AutoApplyDiscount));
                    this.TrackChange(nameof(AutoApplyDiscount));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _isPublic;

        public bool IsPublic
        {
            get { return _isPublic; }
            set
            {
                if (_isPublic != value)
                {
                    _isPublic = value;
                    NotifyOfPropertyChange(nameof(IsPublic));
                    this.TrackChange(nameof(IsPublic));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _isActiveFlag;

        [ExpandoPath("isActive")]
        public bool IsActiveFlag
        {
            get { return _isActiveFlag; }
            set
            {
                if (_isActiveFlag != value)
                {
                    _isActiveFlag = value;
                    NotifyOfPropertyChange(nameof(IsActiveFlag));
                    this.TrackChange(nameof(IsActiveFlag));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private ObservableCollection<StorageGraphQLModel> _storages = [];

        public ObservableCollection<StorageGraphQLModel> Storages
        {
            get { return _storages; }
            set
            {
                if (_storages != value)
                {
                    _storages = value;
                    NotifyOfPropertyChange(nameof(Storages));
                }
            }
        }

        private ObservableCollection<CostCenterGraphQLModel> _costCenters = [];

        public ObservableCollection<CostCenterGraphQLModel> CostCenters
        {
            get { return _costCenters; }
            set
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        }

        private ObservableCollection<CostCenterGraphQLModel> _shadowCostCenters = [];

        public ObservableCollection<CostCenterGraphQLModel> ShadowCostCenters
        {
            get { return _shadowCostCenters; }
            set
            {
                if (_shadowCostCenters != value)
                {
                    _shadowCostCenters = value;
                    NotifyOfPropertyChange(nameof(ShadowCostCenters));
                }
            }
        }

        private ObservableCollection<PaymentMethodPriceListDTO> _paymentMethods = [];

        public ObservableCollection<PaymentMethodPriceListDTO> PaymentMethods
        {
            get { return _paymentMethods; }
            set
            {
                if (_paymentMethods != value)
                {
                    _paymentMethods = value;
                    NotifyOfPropertyChange(nameof(PaymentMethods));
                    NotifyOfPropertyChange(nameof(ExcludedPaymentMethodIds));
                    this.TrackChange(nameof(ExcludedPaymentMethodIds));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ListenPaymentMethodCheck();
                }
            }
        }

        [ExpandoPath("excludedPaymentMethodIds")]
        public List<int> ExcludedPaymentMethodIds => PaymentMethods.Where(p => !p.IsChecked).Select(p => p.Id).ToList();

        private StorageGraphQLModel? _selectedStorage;

        [ExpandoPath("storageId", SerializeAsId = true)]
        public StorageGraphQLModel? SelectedStorage
        {
            get { return _selectedStorage; }
            set
            {
                if (_selectedStorage != value)
                {
                    _selectedStorage = value;
                    NotifyOfPropertyChange(nameof(SelectedStorage));
                    this.TrackChange(nameof(SelectedStorage));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateStorage();
                }
            }
        }

        private string _selectedListUpdateBehaviorOnCostChange = "UPDATE_PROFIT_MARGIN";

        public string SelectedListUpdateBehaviorOnCostChange
        {
            get { return _selectedListUpdateBehaviorOnCostChange; }
            set
            {
                if (_selectedListUpdateBehaviorOnCostChange != value)
                {
                    _selectedListUpdateBehaviorOnCostChange = value;
                    NotifyOfPropertyChange(nameof(SelectedListUpdateBehaviorOnCostChange));
                    this.TrackChange(nameof(SelectedListUpdateBehaviorOnCostChange));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public Dictionary<string, string> ListUpdateBehaviorOnCostChange { get; set; } = new()
        {
            { "UPDATE_PROFIT_MARGIN", "Actualizar margen de utilidad" },
            { "UPDATE_PRICE", "Actualizar precio de venta" },
        };

        private string _selectedFormula = "D";

        public string SelectedFormula
        {
            get { return _selectedFormula; }
            set
            {
                if (_selectedFormula != value)
                {
                    _selectedFormula = value;
                    NotifyOfPropertyChange(nameof(SelectedFormula));
                    NotifyOfPropertyChange(nameof(Formula));
                }
            }
        }

        public string Formula
        {
            get
            {
                if (SelectedFormula == "D") return "COSTO / (1 - (MARGEN_UTILIDAD/100))";
                return "(COSTO * (1 + (MARGEN_IMPUESTO / 100))) / (1 - (MARGEN_UTILIDAD / 100))";
            }
        }

        #endregion

        #region CanSave

        public bool CanSave
        {
            get
            {
                if (_errors.Count > 0) return false;
                if (SelectedCostMode == PriceListCostModeEnum.COST_BY_STORAGE && SelectedStorage is null) return false;
                if (!this.HasChanges()) return false;
                return true;
            }
        }

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

        #endregion

        #region Focus

        private bool _nameFocus;

        public bool NameFocus
        {
            get { return _nameFocus; }
            set
            {
                _nameFocus = value;
                NotifyOfPropertyChange(nameof(NameFocus));
            }
        }

        void SetFocus(Expression<Func<object>> propertyExpression)
        {
            string controlName = propertyExpression.GetMemberInfo().Name;
            NameFocus = false;
            NameFocus = controlName == nameof(Name);
        }

        #endregion

        #region Constructor

        public UpdatePriceListModalViewModel(
            Helpers.IDialogService dialogService,
            IEventAggregator eventAggregator,
            IMapper autoMapper,
            IRepository<PriceListGraphQLModel> priceListService,
            StorageCache storageCache,
            CostCenterCache costCenterCache,
            PaymentMethodCache paymentMethodCache,
            IGraphQLClient graphQLClient)
        {
            _errors = [];
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _autoMapper = autoMapper;
            _priceListService = priceListService;
            _storageCache = storageCache;
            _costCenterCache = costCenterCache;
            _paymentMethodCache = paymentMethodCache;
            _graphQLClient = graphQLClient;
        }

        #endregion

        #region Initialize / SetForEdit / Seeding

        public async Task InitializeAsync()
        {
            try
            {
                await CacheBatchLoader.LoadAsync(
                    _graphQLClient, default,
                    _storageCache, _costCenterCache, _paymentMethodCache);

                Storages = [.. _storageCache.Items];
                CostCenters = [.. _costCenterCache.Items];
                RefreshCostCenters();
                PaymentMethods = [.. _autoMapper.Map<ObservableCollection<PaymentMethodPriceListDTO>>(_paymentMethodCache.Items)];
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public void SetForEdit(PriceListGraphQLModel priceList)
        {
            Id = priceList.Id;
            Name = priceList.Name;
            IsTaxable = priceList.IsTaxable;
            PriceListIncludeTax = priceList.PriceListIncludeTax;
            UseAlternativeFormula = priceList.UseAlternativeFormula;
            SelectedFormula = priceList.UseAlternativeFormula ? "A" : "D";
            EditablePrice = priceList.EditablePrice;
            AutoApplyDiscount = priceList.AutoApplyDiscount;
            IsPublic = priceList.IsPublic;
            IsActiveFlag = priceList.IsActive;
            SelectedListUpdateBehaviorOnCostChange = priceList.ListUpdateBehaviorOnCostChange;

            // CostMode
            if (priceList.CostMode == "COST_BY_STORAGE" && priceList.Storage != null)
            {
                SelectedCostMode = PriceListCostModeEnum.COST_BY_STORAGE;
                SelectedStorage = Storages.FirstOrDefault(x => x.Id == priceList.Storage.Id);
            }
            else
            {
                SelectedCostMode = PriceListCostModeEnum.USE_AVERAGE_COST;
                SelectedStorage = null;
            }

            // Payment methods - uncheck excluded ones
            foreach (var excluded in priceList.ExcludedPaymentMethods)
            {
                var pm = PaymentMethods.FirstOrDefault(x => x.Id == excluded.Id);
                if (pm != null) pm.IsChecked = false;
            }

            SeedCurrentValues();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(IsTaxable), IsTaxable);
            this.SeedValue(nameof(PriceListIncludeTax), PriceListIncludeTax);
            this.SeedValue(nameof(UseAlternativeFormula), UseAlternativeFormula);
            this.SeedValue(nameof(EditablePrice), EditablePrice);
            this.SeedValue(nameof(AutoApplyDiscount), AutoApplyDiscount);
            this.SeedValue(nameof(IsPublic), IsPublic);
            this.SeedValue(nameof(IsActiveFlag), IsActiveFlag);
            this.SeedValue(nameof(CostMode), CostMode);
            this.SeedValue(nameof(SelectedStorage), SelectedStorage);
            this.SeedValue(nameof(SelectedListUpdateBehaviorOnCostChange), SelectedListUpdateBehaviorOnCostChange);
            this.SeedValue(nameof(ExcludedPaymentMethodIds), ExcludedPaymentMethodIds);
            this.AcceptChanges();
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                string query = _updateQuery.Value;
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = Id;

                var result = await _priceListService.UpdateAsync<UpsertResponseType<PriceListGraphQLModel>>(query, variables);

                if (!result.Success)
                {
                    ThemedMessageBox.Show(
                        text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo",
                        title: $"{result.Message}!",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(new PriceListUpdateMessage { UpdatedPriceList = result });
                await _dialogService.CloseDialogAsync(this, true);
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }

        public async Task CancelAsync()
        {
            await _dialogService.CloseDialogAsync(this, true);
        }

        #endregion

        #region Helpers

        public void RefreshCostCenters()
        {
            ShadowCostCenters = [.. CostCenters.Where(x => x.IsTaxable == IsTaxable && x.PriceListIncludeTax == PriceListIncludeTax)];
        }

        private void ListenPaymentMethodCheck()
        {
            foreach (var pm in PaymentMethods)
                pm.PropertyChanged += PaymentMethod_PropertyChanged!;

            PaymentMethods.CollectionChanged += PaymentMethod_CollectionChanged!;
        }

        private void PaymentMethod_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PaymentMethodPriceListDTO.IsChecked))
            {
                this.TrackChange(nameof(ExcludedPaymentMethodIds));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private void PaymentMethod_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (PaymentMethodPriceListDTO p in e.NewItems)
                    p.PropertyChanged += PaymentMethod_PropertyChanged!;
            }
            if (e.OldItems != null)
            {
                foreach (PaymentMethodPriceListDTO p in e.OldItems)
                    p.PropertyChanged -= PaymentMethod_PropertyChanged!;
            }
            this.TrackChange(nameof(ExcludedPaymentMethodIds));
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion

        #region Lifecycle

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperty(nameof(Name), Name);
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                foreach (var pm in PaymentMethods)
                    pm.PropertyChanged -= PaymentMethod_PropertyChanged!;
                PaymentMethods.CollectionChanged -= PaymentMethod_CollectionChanged!;
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region Validation

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable? GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null;
            return _errors[propertyName];
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

        private void ValidateStorage()
        {
            ClearErrors(nameof(SelectedStorage));
            if (SelectedCostMode == PriceListCostModeEnum.COST_BY_STORAGE && SelectedStorage is null)
                AddError(nameof(SelectedStorage), "Debe seleccionar una bodega");
        }

        private void ValidateProperty(string propertyName, string value)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty.Trim();
            try
            {
                ClearErrors(propertyName);
                switch (propertyName)
                {
                    case nameof(Name):
                        if (string.IsNullOrEmpty(value.Trim())) AddError(propertyName, "El nombre no puede estar vacío");
                        break;
                }
            }
            catch (Exception ex)
            {
                Execute.OnUIThread(() =>
                {
                    ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error);
                });
            }
        }

        #endregion

        #region GraphQL Query

        private static readonly Lazy<string> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<PriceListGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "priceList", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.EditablePrice)
                    .Field(f => f.IsActive)
                    .Field(f => f.AutoApplyDiscount)
                    .Field(f => f.IsPublic)
                    .Field(f => f.AllowNewUsersAccess)
                    .Field(f => f.ListUpdateBehaviorOnCostChange)
                    .Field(f => f.IsTaxable)
                    .Field(f => f.PriceListIncludeTax)
                    .Field(f => f.UseAlternativeFormula)
                    .Field(f => f.CostMode)
                    .Select(f => f.Parent, p => p.Field(x => x.Id).Field(x => x.Name))
                    .Select(f => f.Storage, s => s.Field(x => x.Id).Field(x => x.Name))
                    .SelectList(f => f.ExcludedPaymentMethods, pm => pm
                        .Field(p => p.Id)
                        .Field(p => p.Name)
                        .Field(p => p.Abbreviation)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var dataParam = new GraphQLQueryParameter("data", "UpdatePriceListInput!");
            var idParam = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("updatePriceList", [dataParam, idParam], fields, "UpdateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        #endregion
    }
}
