using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Books;
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
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.AccountingGroups.ViewModels
{
    public class AccountingGroupDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<AccountingGroupGraphQLModel> _accountingGroupService;
        private readonly IEventAggregator _eventAggregator;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;
        private readonly TaxCache _taxCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        #endregion

        #region State

        public bool IsNewRecord => Id == 0;

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

        #endregion

        #region Form Properties

        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    NotifyOfPropertyChange(nameof(Id));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    NotifyOfPropertyChange(nameof(Name));
                    ValidateStringProperty(nameof(Name), value);
                    this.TrackChange(nameof(Name));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _allowAiu;
        public bool AllowAiu
        {
            get => _allowAiu;
            set
            {
                if (_allowAiu != value)
                {
                    _allowAiu = value;
                    NotifyOfPropertyChange(nameof(AllowAiu));
                    NotifyOfPropertyChange(nameof(IsVisibleAiu));
                    this.TrackChange(nameof(AllowAiu));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        #endregion

        #region Account Selections

        private ObservableCollection<AccountingAccountGraphQLModel> _auxiliaryAccounts = [];
        public ObservableCollection<AccountingAccountGraphQLModel> AuxiliaryAccounts
        {
            get => _auxiliaryAccounts;
            set
            {
                if (_auxiliaryAccounts != value)
                {
                    _auxiliaryAccounts = value;
                    NotifyOfPropertyChange(nameof(AuxiliaryAccounts));
                }
            }
        }

        private AccountingAccountGraphQLModel? _selectedAccountCost;
        [ExpandoPath("accountCostId", SerializeAsId = true)]
        public AccountingAccountGraphQLModel? SelectedAccountCost
        {
            get => _selectedAccountCost;
            set
            {
                if (_selectedAccountCost != value)
                {
                    _selectedAccountCost = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountCost));
                    ValidateIntProperty(nameof(SelectedAccountCost), value?.Id);
                    this.TrackChange(nameof(SelectedAccountCost));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private AccountingAccountGraphQLModel? _selectedAccountIncome;
        [ExpandoPath("accountIncomeId", SerializeAsId = true)]
        public AccountingAccountGraphQLModel? SelectedAccountIncome
        {
            get => _selectedAccountIncome;
            set
            {
                if (_selectedAccountIncome != value)
                {
                    _selectedAccountIncome = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountIncome));
                    ValidateIntProperty(nameof(SelectedAccountIncome), value?.Id);
                    this.TrackChange(nameof(SelectedAccountIncome));
                    if (value != null && SelectedAccountIncomeReverse == null)
                        SelectedAccountIncomeReverse = value;
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private AccountingAccountGraphQLModel? _selectedAccountIncomeReverse;
        [ExpandoPath("accountIncomeReverseId", SerializeAsId = true)]
        public AccountingAccountGraphQLModel? SelectedAccountIncomeReverse
        {
            get => _selectedAccountIncomeReverse;
            set
            {
                if (_selectedAccountIncomeReverse != value)
                {
                    _selectedAccountIncomeReverse = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountIncomeReverse));
                    ValidateIntProperty(nameof(SelectedAccountIncomeReverse), value?.Id);
                    this.TrackChange(nameof(SelectedAccountIncomeReverse));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private AccountingAccountGraphQLModel? _selectedAccountInventory;
        [ExpandoPath("accountInventoryId", SerializeAsId = true)]
        public AccountingAccountGraphQLModel? SelectedAccountInventory
        {
            get => _selectedAccountInventory;
            set
            {
                if (_selectedAccountInventory != value)
                {
                    _selectedAccountInventory = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountInventory));
                    ValidateIntProperty(nameof(SelectedAccountInventory), value?.Id);
                    this.TrackChange(nameof(SelectedAccountInventory));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private AccountingAccountGraphQLModel? _selectedAccountAiuAdministration;
        [ExpandoPath("accountAiuAdministrationId", SerializeAsId = true)]
        public AccountingAccountGraphQLModel? SelectedAccountAiuAdministration
        {
            get => _selectedAccountAiuAdministration;
            set
            {
                if (_selectedAccountAiuAdministration != value)
                {
                    _selectedAccountAiuAdministration = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountAiuAdministration));
                    this.TrackChange(nameof(SelectedAccountAiuAdministration));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private AccountingAccountGraphQLModel? _selectedAccountAiuUnforeseen;
        [ExpandoPath("accountAiuUnforeseenId", SerializeAsId = true)]
        public AccountingAccountGraphQLModel? SelectedAccountAiuUnforeseen
        {
            get => _selectedAccountAiuUnforeseen;
            set
            {
                if (_selectedAccountAiuUnforeseen != value)
                {
                    _selectedAccountAiuUnforeseen = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountAiuUnforeseen));
                    this.TrackChange(nameof(SelectedAccountAiuUnforeseen));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private AccountingAccountGraphQLModel? _selectedAccountAiuUtility;
        [ExpandoPath("accountAiuUtilityId", SerializeAsId = true)]
        public AccountingAccountGraphQLModel? SelectedAccountAiuUtility
        {
            get => _selectedAccountAiuUtility;
            set
            {
                if (_selectedAccountAiuUtility != value)
                {
                    _selectedAccountAiuUtility = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountAiuUtility));
                    this.TrackChange(nameof(SelectedAccountAiuUtility));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        #endregion

        #region Tax Selections

        private ObservableCollection<TaxGraphQLModel> _purchasePrimaryTaxes = [];
        public ObservableCollection<TaxGraphQLModel> PurchasePrimaryTaxes
        {
            get => _purchasePrimaryTaxes;
            set
            {
                if (_purchasePrimaryTaxes != value)
                {
                    _purchasePrimaryTaxes = value;
                    NotifyOfPropertyChange(nameof(PurchasePrimaryTaxes));
                }
            }
        }

        private ObservableCollection<TaxGraphQLModel> _purchaseSecondaryTaxes = [];
        public ObservableCollection<TaxGraphQLModel> PurchaseSecondaryTaxes
        {
            get => _purchaseSecondaryTaxes;
            set
            {
                if (_purchaseSecondaryTaxes != value)
                {
                    _purchaseSecondaryTaxes = value;
                    NotifyOfPropertyChange(nameof(PurchaseSecondaryTaxes));
                }
            }
        }

        private ObservableCollection<TaxGraphQLModel> _salesPrimaryTaxes = [];
        public ObservableCollection<TaxGraphQLModel> SalesPrimaryTaxes
        {
            get => _salesPrimaryTaxes;
            set
            {
                if (_salesPrimaryTaxes != value)
                {
                    _salesPrimaryTaxes = value;
                    NotifyOfPropertyChange(nameof(SalesPrimaryTaxes));
                }
            }
        }

        private ObservableCollection<TaxGraphQLModel> _salesSecondaryTaxes = [];
        public ObservableCollection<TaxGraphQLModel> SalesSecondaryTaxes
        {
            get => _salesSecondaryTaxes;
            set
            {
                if (_salesSecondaryTaxes != value)
                {
                    _salesSecondaryTaxes = value;
                    NotifyOfPropertyChange(nameof(SalesSecondaryTaxes));
                }
            }
        }

        private TaxGraphQLModel? _selectedPurchasePrimaryTax;
        [ExpandoPath("purchasePrimaryTaxId", SerializeAsId = true)]
        public TaxGraphQLModel? SelectedPurchasePrimaryTax
        {
            get => _selectedPurchasePrimaryTax;
            set
            {
                if (_selectedPurchasePrimaryTax != value)
                {
                    _selectedPurchasePrimaryTax = value;
                    NotifyOfPropertyChange(nameof(SelectedPurchasePrimaryTax));
                    this.TrackChange(nameof(SelectedPurchasePrimaryTax));
                    ValidateIntProperty(nameof(SelectedPurchasePrimaryTax), value?.Id);
                    NotifyOfPropertyChange(nameof(IsVisiblePurchaseSecondaryTax));
                    PurchaseSecondaryTaxes = [.. _taxCache.Items.Where(f => f.Id != value?.Id)];
                    this.SeedValue(nameof(SelectedPurchaseSecondaryTax), SelectedPurchaseSecondaryTax);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private TaxGraphQLModel? _selectedPurchaseSecondaryTax;
        [ExpandoPath("purchaseSecondaryTaxId", SerializeAsId = true)]
        public TaxGraphQLModel? SelectedPurchaseSecondaryTax
        {
            get => _selectedPurchaseSecondaryTax;
            set
            {
                if (_selectedPurchaseSecondaryTax != value)
                {
                    _selectedPurchaseSecondaryTax = value;
                    NotifyOfPropertyChange(nameof(SelectedPurchaseSecondaryTax));
                    this.TrackChange(nameof(SelectedPurchaseSecondaryTax), value?.Id > 0 ? value : null);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private TaxGraphQLModel? _selectedSalesPrimaryTax;
        [ExpandoPath("salesPrimaryTaxId", SerializeAsId = true)]
        public TaxGraphQLModel? SelectedSalesPrimaryTax
        {
            get => _selectedSalesPrimaryTax;
            set
            {
                if (_selectedSalesPrimaryTax != value)
                {
                    _selectedSalesPrimaryTax = value;
                    NotifyOfPropertyChange(nameof(SelectedSalesPrimaryTax));
                    this.TrackChange(nameof(SelectedSalesPrimaryTax));
                    ValidateIntProperty(nameof(SelectedSalesPrimaryTax), value?.Id);
                    NotifyOfPropertyChange(nameof(IsVisibleSalesSecondaryTax));
                    if (value == null)
                        SelectedSalesSecondaryTax = null;
                    else
                        SalesSecondaryTaxes = [.. _taxCache.Items.Where(f => f.Id != value.Id)];
                    this.SeedValue(nameof(SelectedSalesSecondaryTax), SelectedSalesSecondaryTax);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private TaxGraphQLModel? _selectedSalesSecondaryTax;
        [ExpandoPath("salesSecondaryTaxId", SerializeAsId = true)]
        public TaxGraphQLModel? SelectedSalesSecondaryTax
        {
            get => _selectedSalesSecondaryTax;
            set
            {
                if (_selectedSalesSecondaryTax != value)
                {
                    _selectedSalesSecondaryTax = value;
                    NotifyOfPropertyChange(nameof(SelectedSalesSecondaryTax));
                    this.TrackChange(nameof(SelectedSalesSecondaryTax), value?.Id > 0 ? value : null);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool IsVisiblePurchaseSecondaryTax => SelectedPurchasePrimaryTax != null;
        public bool IsVisibleSalesSecondaryTax => SelectedSalesPrimaryTax != null;
        public bool IsVisibleAiu => AllowAiu;

        #endregion

        #region StringLength Properties

        public int NameMaxLength => _stringLengthCache.GetMaxLength<AccountingGroupGraphQLModel>(nameof(AccountingGroupGraphQLModel.Name));

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

        private void ValidateStringProperty(string propertyName, string value)
        {
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(Name):
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "El nombre no puede estar vacío");
                    break;
            }
        }

        private void ValidateIntProperty(string propertyName, int? value)
        {
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(SelectedAccountCost):
                    if (!value.HasValue || value == 0) AddError(propertyName, "Debe seleccionar una cuenta de costo");
                    break;
                case nameof(SelectedAccountIncome):
                    if (!value.HasValue || value == 0) AddError(propertyName, "Debe seleccionar una cuenta de ingreso");
                    break;
                case nameof(SelectedAccountInventory):
                    if (!value.HasValue || value == 0) AddError(propertyName, "Debe seleccionar una cuenta de inventario");
                    break;
                case nameof(SelectedAccountIncomeReverse):
                    if (!value.HasValue || value == 0) AddError(propertyName, "Debe seleccionar una cuenta de devolución");
                    break;
                case nameof(SelectedPurchasePrimaryTax):
                    if (!value.HasValue || value == 0) AddError(propertyName, "Debe seleccionar un impuesto de compras");
                    break;
                case nameof(SelectedSalesPrimaryTax):
                    if (!value.HasValue || value == 0) AddError(propertyName, "Debe seleccionar un impuesto de venta");
                    break;
            }
        }

        private void ValidateProperties()
        {
            ValidateStringProperty(nameof(Name), Name);
            ValidateIntProperty(nameof(SelectedAccountCost), SelectedAccountCost?.Id);
            ValidateIntProperty(nameof(SelectedAccountIncome), SelectedAccountIncome?.Id);
            ValidateIntProperty(nameof(SelectedAccountIncomeReverse), SelectedAccountIncomeReverse?.Id);
            ValidateIntProperty(nameof(SelectedAccountInventory), SelectedAccountInventory?.Id);
            ValidateIntProperty(nameof(SelectedPurchasePrimaryTax), SelectedPurchasePrimaryTax?.Id);
            ValidateIntProperty(nameof(SelectedSalesPrimaryTax), SelectedSalesPrimaryTax?.Id);
        }

        #endregion

        #region Button States

        public bool CanSave => !HasErrors && this.HasChanges();

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

        #region Constructor

        public AccountingGroupDetailViewModel(
            IRepository<AccountingGroupGraphQLModel> accountingGroupService,
            IEventAggregator eventAggregator,
            AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache,
            TaxCache taxCache,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory)
        {
            _accountingGroupService = accountingGroupService;
            _eventAggregator = eventAggregator;
            _auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;
            _taxCache = taxCache;
            _stringLengthCache = stringLengthCache;
            _joinableTaskFactory = joinableTaskFactory;
        }

        #endregion

        #region Initialization

        public async Task InitializeAsync()
        {
            await Task.WhenAll(
                _auxiliaryAccountingAccountCache.EnsureLoadedAsync(),
                _taxCache.EnsureLoadedAsync());

            AuxiliaryAccounts = [.. _auxiliaryAccountingAccountCache.Items];
            PurchasePrimaryTaxes = [.. _taxCache.Items];
            SalesPrimaryTaxes = [.. _taxCache.Items];
        }

        #endregion

        #region SetForNew / SetForEdit

        public void SetForNew()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(AllowAiu), AllowAiu);
            this.AcceptChanges();
            ValidateProperties();
        }

        public void SetForEdit()
        {
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(AllowAiu), AllowAiu);
            this.SeedValue(nameof(SelectedAccountCost), SelectedAccountCost);
            this.SeedValue(nameof(SelectedAccountIncome), SelectedAccountIncome);
            this.SeedValue(nameof(SelectedAccountIncomeReverse), SelectedAccountIncomeReverse);
            this.SeedValue(nameof(SelectedAccountInventory), SelectedAccountInventory);
            this.SeedValue(nameof(SelectedAccountAiuAdministration), SelectedAccountAiuAdministration);
            this.SeedValue(nameof(SelectedAccountAiuUnforeseen), SelectedAccountAiuUnforeseen);
            this.SeedValue(nameof(SelectedAccountAiuUtility), SelectedAccountAiuUtility);
            this.SeedValue(nameof(SelectedPurchasePrimaryTax), SelectedPurchasePrimaryTax);
            this.SeedValue(nameof(SelectedPurchaseSecondaryTax), SelectedPurchaseSecondaryTax);
            this.SeedValue(nameof(SelectedSalesPrimaryTax), SelectedSalesPrimaryTax);
            this.SeedValue(nameof(SelectedSalesSecondaryTax), SelectedSalesSecondaryTax);
            this.AcceptChanges();
            ValidateProperties();
        }

        #endregion

        #region Load for Edit

        public async Task LoadDataForEditAsync(int id)
        {
            var (fragment, query) = _loadByIdQuery.Value;
            var variables = new GraphQLVariables()
                .For(fragment, "id", id)
                .Build();

            var entity = await _accountingGroupService.FindByIdAsync(query, variables);
            PopulateFromEntity(entity);
        }

        private void PopulateFromEntity(AccountingGroupGraphQLModel entity)
        {
            Id = entity.Id;
            Name = entity.Name;
            AllowAiu = entity.AllowAiu;
            SelectedAccountCost = AuxiliaryAccounts.FirstOrDefault(f => f.Id == entity.AccountCost?.Id);
            SelectedAccountIncome = AuxiliaryAccounts.FirstOrDefault(f => f.Id == entity.AccountIncome?.Id);
            SelectedAccountIncomeReverse = AuxiliaryAccounts.FirstOrDefault(f => f.Id == entity.AccountIncomeReverse?.Id);
            SelectedAccountInventory = AuxiliaryAccounts.FirstOrDefault(f => f.Id == entity.AccountInventory?.Id);
            SelectedAccountAiuAdministration = AuxiliaryAccounts.FirstOrDefault(f => f.Id == entity.AccountAiuAdministration?.Id);
            SelectedAccountAiuUnforeseen = AuxiliaryAccounts.FirstOrDefault(f => f.Id == entity.AccountAiuUnforeseen?.Id);
            SelectedAccountAiuUtility = AuxiliaryAccounts.FirstOrDefault(f => f.Id == entity.AccountAiuUtility?.Id);

            SelectedPurchasePrimaryTax = PurchasePrimaryTaxes.FirstOrDefault(f => f.Id == entity.PurchasePrimaryTax?.Id);
            SelectedSalesPrimaryTax = SalesPrimaryTaxes.FirstOrDefault(f => f.Id == entity.SalesPrimaryTax?.Id);

            PurchaseSecondaryTaxes = [.. _taxCache.Items.Where(f => f.Id != SelectedPurchasePrimaryTax?.Id)];
            SelectedPurchaseSecondaryTax = PurchaseSecondaryTaxes.FirstOrDefault(f => f.Id == entity.PurchaseSecondaryTax?.Id);

            SalesSecondaryTaxes = [.. _taxCache.Items.Where(f => f.Id != SelectedSalesPrimaryTax?.Id)];
            SelectedSalesSecondaryTax = SalesSecondaryTaxes.FirstOrDefault(f => f.Id == entity.SalesSecondaryTax?.Id);
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<AccountingGroupGraphQLModel> result = await ExecuteSaveAsync();

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
                        ? new AccountingGroupCreateMessage { CreatedAccountingGroup = result }
                        : new AccountingGroupUpdateMessage { UpdatedAccountingGroup = result },
                    CancellationToken.None);

                await TryCloseAsync(true);
            }
            catch (AsyncException ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al realizar operación.\r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al realizar operación.\r\n{GetType().Name}.{nameof(SaveAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<UpsertResponseType<AccountingGroupGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                if (IsNewRecord)
                {
                    var (_, query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    return await _accountingGroupService.CreateAsync<UpsertResponseType<AccountingGroupGraphQLModel>>(query, variables);
                }
                else
                {
                    var (_, query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;
                    return await _accountingGroupService.UpdateAsync<UpsertResponseType<AccountingGroupGraphQLModel>>(query, variables);
                }
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<AccountingGroupGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingGroup", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Name))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createAccountingGroup",
                [new("input", "CreateAccountingGroupInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<AccountingGroupGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingGroup", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Name))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("updateAccountingGroup",
                [new("data", "UpdateAccountingGroupInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadByIdQuery = new(() =>
        {
            var fields = FieldSpec<AccountingGroupGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.Name)
                .Field(e => e.AllowAiu)
                .Select(e => e.AccountCost, acc => acc.Field(c => c.Id).Field(c => c.Name))
                .Select(e => e.AccountIncome, acc => acc.Field(c => c.Id).Field(c => c.Name))
                .Select(e => e.AccountIncomeReverse, acc => acc.Field(c => c.Id).Field(c => c.Name))
                .Select(e => e.AccountInventory, acc => acc.Field(c => c.Id).Field(c => c.Name))
                .Select(e => e.AccountAiuAdministration, acc => acc.Field(c => c.Id).Field(c => c.Name))
                .Select(e => e.AccountAiuUnforeseen, acc => acc.Field(c => c.Id).Field(c => c.Name))
                .Select(e => e.AccountAiuUtility, acc => acc.Field(c => c.Id).Field(c => c.Name))
                .Select(e => e.PurchasePrimaryTax, acc => acc.Field(c => c.Id).Field(c => c.Name))
                .Select(e => e.PurchaseSecondaryTax, acc => acc.Field(c => c.Id).Field(c => c.Name))
                .Select(e => e.SalesPrimaryTax, acc => acc.Field(c => c.Id).Field(c => c.Name))
                .Select(e => e.SalesSecondaryTax, acc => acc.Field(c => c.Id).Field(c => c.Name))
                .Build();

            var fragment = new GraphQLQueryFragment("accountingGroup",
                [new("id", "ID!")],
                fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion
    }
}
