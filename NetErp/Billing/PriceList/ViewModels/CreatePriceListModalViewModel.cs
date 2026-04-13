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
using Microsoft.VisualStudio.Threading;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Billing.PriceList.ViewModels
{
    public class CreatePriceListModalViewModel : Screen, INotifyDataErrorInfo
    {

        private readonly Helpers.IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Dictionary<string, List<string>> _errors;
        private readonly IRepository<PriceListGraphQLModel> _priceListService;
        private readonly IGraphQLClient _graphQLClient;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly StorageCache _storageCache;
        private readonly CostCenterCache _costCenterCache;
        private readonly StringLengthCache _stringLengthCache;

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
                    NotifyOfPropertyChange(nameof(CanSave));
                    this.TrackChange(nameof(Name));
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
        }

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
        }

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
        }



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

        public string SelectedFormula
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedFormula));
                    NotifyOfPropertyChange(nameof(Formula));
                }
            }
        } = "D";

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
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{nameof(SaveAsync)} \r\n{ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
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
            IGraphQLClient graphQLClient,
            JoinableTaskFactory joinableTaskFactory)
        {
            _errors = [];
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _priceListService = priceListService;
            _storageCache = storageCache;
            _costCenterCache = costCenterCache;
            _stringLengthCache = stringLengthCache;
            _graphQLClient = graphQLClient;
            _joinableTaskFactory = joinableTaskFactory;
        }

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

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.TryGetValue(propertyName, out List<string>? value)) return Enumerable.Empty<string>();
            return value;
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
