using Caliburn.Micro;
using Common.Extensions;
using Extensions.Global;
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
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Billing.PriceList.ViewModels
{
    public class CreatePriceListModalViewModel : Screen, INotifyDataErrorInfo
    {

        private readonly Helpers.IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        Dictionary<string, List<string>> _errors;
        private readonly IRepository<PriceListGraphQLModel> _priceListService;
        private readonly IGraphQLClient _graphQLClient;
        private readonly StorageCache _storageCache;
        private readonly CostCenterCache _costCenterCache;
        private readonly StringLengthCache _stringLengthCache;

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
                    NotifyOfPropertyChange(nameof(CanSave));
                    this.TrackChange(nameof(Name));
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

        private ObservableCollection<StorageGraphQLModel> _storages;

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

        private ObservableCollection<CostCenterGraphQLModel> _costCenters;

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

        private ObservableCollection<CostCenterGraphQLModel> _shadowCostCenters;

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
                    ValidateStorage();
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        // Hidden defaults - seeded for ChangeCollector, not shown in UI
        public bool EditablePrice { get; set; } = true;

        [ExpandoPath("isActive")]
        public bool IsActiveFlag { get; set; } = true;

        public bool AutoApplyDiscount { get; set; }
        public bool IsPublic { get; set; } = true;
        public bool AllowNewUsersAccess { get; set; } = true;
        public string ListUpdateBehaviorOnCostChange { get; set; } = "UPDATE_PROFIT_MARGIN";

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

        public void RefreshCostCenters()
        {
            ShadowCostCenters = [.. CostCenters.Where(x => x.IsTaxable == IsTaxable && x.PriceListIncludeTax == PriceListIncludeTax)];
        }


        private ICommand _saveCommand;

        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand == null) _saveCommand = new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                var (_, query) = _createQuery.Value;
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                UpsertResponseType<PriceListGraphQLModel> result = await _priceListService.CreateAsync<UpsertResponseType<PriceListGraphQLModel>>(query, variables);

                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(new PriceListCreateMessage { CreatedPriceList = result });
                await _dialogService.CloseDialogAsync(this, true);
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }

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

        public async Task InitializeAsync()
        {
            try
            {
                await CacheBatchLoader.LoadAsync(
                    _graphQLClient, default,
                    _storageCache, _costCenterCache);
                Storages = [.. _storageCache.Items];
                CostCenters = [.. _costCenterCache.Items];
                RefreshCostCenters();
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public void SetForNew()
        {
            Name = string.Empty;
            IsTaxable = false;
            PriceListIncludeTax = false;
            UseAlternativeFormula = false;
            SelectedCostMode = PriceListCostModeEnum.USE_AVERAGE_COST;
            SelectedStorage = null;
            SeedDefaultValues();
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            // UI properties
            this.SeedValue(nameof(IsTaxable), IsTaxable);
            this.SeedValue(nameof(PriceListIncludeTax), PriceListIncludeTax);
            this.SeedValue(nameof(UseAlternativeFormula), UseAlternativeFormula);
            this.SeedValue(nameof(CostMode), CostMode);
            // Hidden defaults
            this.SeedValue(nameof(EditablePrice), EditablePrice);
            this.SeedValue(nameof(IsActiveFlag), IsActiveFlag);
            this.SeedValue(nameof(AutoApplyDiscount), AutoApplyDiscount);
            this.SeedValue(nameof(IsPublic), IsPublic);
            this.SeedValue(nameof(AllowNewUsersAccess), AllowNewUsersAccess);
            this.SeedValue(nameof(ListUpdateBehaviorOnCostChange), ListUpdateBehaviorOnCostChange);
            this.AcceptChanges();
        }

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



        private ICommand _cancelCommand;

        public ICommand CancelCommand
        {
            get
            {
                if (_cancelCommand == null) _cancelCommand = new AsyncCommand(CancelAsync);
                return _cancelCommand;
            }
        }

        public async Task CancelAsync()
        {
            await _dialogService.CloseDialogAsync(this, true);
        }


        public CreatePriceListModalViewModel(
            Helpers.IDialogService dialogService,
            IEventAggregator eventAggregator,
            IRepository<PriceListGraphQLModel> priceListService,
            StorageCache storageCache,
            CostCenterCache costCenterCache,
            StringLengthCache stringLengthCache,
            IGraphQLClient graphQLClient)
        {
            _errors = [];
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _priceListService = priceListService;
            _storageCache = storageCache;
            _costCenterCache = costCenterCache;
            _stringLengthCache = stringLengthCache;
            _graphQLClient = graphQLClient;
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = Application.Current.Dispatcher.BeginInvoke(new System.Action(() => this.SetFocus(() => Name)), DispatcherPriority.Render);
            ValidateProperty(nameof(Name), Name);
        }

        public int NameMaxLength => _stringLengthCache.GetMaxLength<PriceListGraphQLModel>(nameof(PriceListGraphQLModel.Name));

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
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                });
            }
        }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
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
                    .Select(f => f.Storage, s => s.Field(x => x.Id).Field(x => x.Name)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createPriceList",
                [new("input", "CreatePriceListInput!")], fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });
    }

    public enum PriceListCostModeEnum
    {
        USE_AVERAGE_COST,
        COST_BY_STORAGE
    }
}
