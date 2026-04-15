using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
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
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        #region Properties

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public double DialogWidth { get; set; }
        public double DialogHeight { get; set; }

        public int Id { get; set; }

        public string Name
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    ValidateProperty(nameof(Name), value);
                    NotifyOfPropertyChange(nameof(Name));
                    this.TrackChange(nameof(Name));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public bool IsTaxable
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsTaxable));
                    this.TrackChange(nameof(IsTaxable));
                    NotifyOfPropertyChange(nameof(CanSave));
                    if (value is false) PriceListIncludeTax = false;
                    RefreshCostCenters();
                }
            }
        }

        public bool PriceListIncludeTax
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PriceListIncludeTax));
                    this.TrackChange(nameof(PriceListIncludeTax));
                    NotifyOfPropertyChange(nameof(CanSave));
                    RefreshCostCenters();
                }
            }
        }

        public bool UseAlternativeFormula
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(UseAlternativeFormula));
                    this.TrackChange(nameof(UseAlternativeFormula));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public string CostMode
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CostMode));
                    this.TrackChange(nameof(CostMode));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = "USE_AVERAGE_COST";

        public PriceListCostModeEnum SelectedCostMode
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
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
        } = PriceListCostModeEnum.USE_AVERAGE_COST;

        public bool ShowStorageSelector => SelectedCostMode == PriceListCostModeEnum.COST_BY_STORAGE;

        public bool EditablePrice
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(EditablePrice));
                    this.TrackChange(nameof(EditablePrice));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool AutoApplyDiscount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AutoApplyDiscount));
                    this.TrackChange(nameof(AutoApplyDiscount));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool IsPublic
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsPublic));
                    this.TrackChange(nameof(IsPublic));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("isActive")]
        public bool IsActiveFlag
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsActiveFlag));
                    this.TrackChange(nameof(IsActiveFlag));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public ObservableCollection<StorageGraphQLModel> Storages
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Storages));
                }
            }
        } = [];

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

        public ObservableCollection<CostCenterGraphQLModel> ShadowCostCenters
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ShadowCostCenters));
                }
            }
        } = [];

        public ObservableCollection<PaymentMethodPriceListDTO> PaymentMethods
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PaymentMethods));
                    NotifyOfPropertyChange(nameof(ExcludedPaymentMethodIds));
                    this.TrackChange(nameof(ExcludedPaymentMethodIds));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ListenPaymentMethodCheck();
                }
            }
        } = [];

        [ExpandoPath("excludedPaymentMethodIds")]
        public List<int> ExcludedPaymentMethodIds => PaymentMethods.Where(p => !p.IsChecked).Select(p => p.Id).ToList();

        [ExpandoPath("storageId", SerializeAsId = true)]
        public StorageGraphQLModel? SelectedStorage
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedStorage));
                    this.TrackChange(nameof(SelectedStorage));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateStorage();
                }
            }
        }

        public string SelectedListUpdateBehaviorOnCostChange
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedListUpdateBehaviorOnCostChange));
                    this.TrackChange(nameof(SelectedListUpdateBehaviorOnCostChange));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = "UPDATE_PROFIT_MARGIN";

        public Dictionary<string, string> ListUpdateBehaviorOnCostChange { get; } = new()
        {
            { "UPDATE_PROFIT_MARGIN", "Actualizar margen de utilidad" },
            { "UPDATE_PRICE", "Actualizar precio de venta" },
        };

        public string SelectedFormula
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    UseAlternativeFormula = value != "D";
                    NotifyOfPropertyChange(nameof(SelectedFormula));
                    NotifyOfPropertyChange(nameof(Formula));
                }
            }
        } = "D";

        public string Formula
        {
            get
            {
                if (SelectedFormula == "D") return "COSTO / (1-(MARGEN_UTILIDAD/100))";
                return "COSTO * (1+(MARGEN_UTILIDAD/100))";
            }
        }

        #endregion

        #region CanSave

        public bool CanSave
        {
            get
            {
                if (IsBusy) return false;
                if (_errors.Count > 0) return false;
                if (SelectedCostMode == PriceListCostModeEnum.COST_BY_STORAGE && SelectedStorage is null) return false;
                if (!this.HasChanges()) return false;
                return true;
            }
        }

        #endregion

        #region Commands

        public ICommand SaveCommand
        {
            get
            {
                field ??= new AsyncCommand(SaveAsync);
                return field;
            }
        }

        public ICommand CancelCommand
        {
            get
            {
                field ??= new AsyncCommand(CancelAsync);
                return field;
            }
        }

        #endregion

        #region Focus

        public bool NameFocus
        {
            get;
            set
            {
                field = value;
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
            StringLengthCache stringLengthCache,
            IGraphQLClient graphQLClient,
            JoinableTaskFactory joinableTaskFactory)
        {
            _errors = [];
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _autoMapper = autoMapper ?? throw new ArgumentNullException(nameof(autoMapper));
            _priceListService = priceListService ?? throw new ArgumentNullException(nameof(priceListService));
            _storageCache = storageCache ?? throw new ArgumentNullException(nameof(storageCache));
            _costCenterCache = costCenterCache ?? throw new ArgumentNullException(nameof(costCenterCache));
            _paymentMethodCache = paymentMethodCache ?? throw new ArgumentNullException(nameof(paymentMethodCache));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _graphQLClient = graphQLClient ?? throw new ArgumentNullException(nameof(graphQLClient));
            _joinableTaskFactory = joinableTaskFactory ?? throw new ArgumentNullException(nameof(joinableTaskFactory));
        }

        #endregion

        #region Initialize / SetForEdit / Seeding

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await CacheBatchLoader.LoadAsync(
                    _graphQLClient, cancellationToken,
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
            foreach (PaymentMethodGraphQLModel excluded in priceList.ExcludedPaymentMethods)
            {
                PaymentMethodPriceListDTO? pm = PaymentMethods.FirstOrDefault(x => x.Id == excluded.Id);
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
                IsBusy = true;
                var (fragment, query) = _updateQuery.Value;
                string dataPrefix = GraphQLQueryFragment.GetVariableName(fragment.Alias, "data");
                string idVarName = GraphQLQueryFragment.GetVariableName(fragment.Alias, "id");
                ExpandoObject variables = ChangeCollector.CollectChanges(this, prefix: dataPrefix);
                ((IDictionary<string, object>)variables)[idVarName] = Id;

                UpsertResponseType<PriceListGraphQLModel> result = await _priceListService.UpdateAsync<UpsertResponseType<PriceListGraphQLModel>>(query, variables);

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
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(SaveAsync)} \r\n{ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
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
            foreach (PaymentMethodPriceListDTO pm in PaymentMethods)
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
            _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(
                new System.Action(() => SetFocus(() => Name)),
                DispatcherPriority.Render);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                foreach (PaymentMethodPriceListDTO pm in PaymentMethods)
                    pm.PropertyChanged -= PaymentMethod_PropertyChanged!;
                PaymentMethods.CollectionChanged -= PaymentMethod_CollectionChanged!;

                Storages.Clear();
                CostCenters.Clear();
                ShadowCostCenters.Clear();
                PaymentMethods.Clear();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region Validation

        private static readonly string[] _basicDataFields = [nameof(Name), nameof(SelectedStorage)];

        public int NameMaxLength => _stringLengthCache.GetMaxLength<PriceListGraphQLModel>(nameof(PriceListGraphQLModel.Name));

        public bool HasErrors => _errors.Count > 0;

        public bool HasBasicDataErrors => _basicDataFields.Any(f => _errors.ContainsKey(f));

        public string? BasicDataTabTooltip => GetTabTooltip(_basicDataFields);

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

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

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return Enumerable.Empty<string>();
            return _errors[propertyName];
        }

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
            List<string> errors = [];
            if (SelectedCostMode == PriceListCostModeEnum.COST_BY_STORAGE && SelectedStorage is null)
                errors.Add("Debe seleccionar una bodega");
            SetPropertyErrors(nameof(SelectedStorage), errors);
        }

        private void ValidateProperty(string propertyName, string value)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty;
            List<string> errors = [];
            switch (propertyName)
            {
                case nameof(Name):
                    if (string.IsNullOrEmpty(value.Trim())) errors.Add("El nombre no puede estar vacío");
                    break;
            }
            SetPropertyErrors(propertyName, errors);
        }

        #endregion

        #region GraphQL Query

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
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

            var fragment = new GraphQLQueryFragment("updatePriceList",
                [new("data", "UpdatePriceListInput!"), new("id", "ID!")], fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
