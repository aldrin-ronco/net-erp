using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using static Amazon.S3.Util.S3EventNotification;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.AccountingGroups.ViewModels
{
    public class AccountingGroupDetailViewModel : Screen, INotifyDataErrorInfo
    {
        private readonly IRepository<AccountingGroupGraphQLModel> _accountingGroupService;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;
        private readonly TaxCache _taxCache;

        public AccountingGroupViewModel Context { get; set; }
        public AccountingGroupDetailViewModel(AccountingGroupViewModel context, IRepository<AccountingGroupGraphQLModel> accountingGroupService, AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache,
            TaxCache taxCache)
        {
            Context = context;
            _errors = new Dictionary<string, List<string>>();
            _accountingGroupService = accountingGroupService;
            _auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;
            _taxCache = taxCache;
            _= InitializeAsync();

        }
        public async Task InitializeAsync()
        {
            await Task.WhenAll(
                _auxiliaryAccountingAccountCache.EnsureLoadedAsync(),
                _taxCache.EnsureLoadedAsync()

                );
            AiuAdministrationAccountingAccounts = [.. _auxiliaryAccountingAccountCache.Items];
            AiuUnforeseenAccountingAccounts =     [.. _auxiliaryAccountingAccountCache.Items];
            AiuUtilityAccountingAccounts =        [.. _auxiliaryAccountingAccountCache.Items];

            AccountCostAccountingAccounts =       [.. _auxiliaryAccountingAccountCache.Items];
            IncomeAccountingAccounts =            [.. _auxiliaryAccountingAccountCache.Items];
            IncomeReverseAccountingAccounts =     [.. _auxiliaryAccountingAccountCache.Items];
            InventoryAccountingAccounts =         [.. _auxiliaryAccountingAccountCache.Items];

            PurchasePrimaryTaxes = [.. _taxCache.Items];
            SalesPrimaryTaxes = [.. _taxCache.Items];
            

        }
        protected override void OnViewReady(object view)
        {
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
            this.SetFocus(() => Name);
        }
        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            ValidateProperties();
            this.AcceptChanges();

        }
        #region Collections
        private ObservableCollection<TaxGraphQLModel> _purchasePrimaryTaxes;
        public ObservableCollection<TaxGraphQLModel> PurchasePrimaryTaxes
        {
            get { return _purchasePrimaryTaxes; }
            set
            {
                if (_purchasePrimaryTaxes != value)
                {
                    _purchasePrimaryTaxes = value;
                    NotifyOfPropertyChange(nameof(PurchasePrimaryTaxes));

                }
            }
        }


        private ObservableCollection<TaxGraphQLModel> _purchaseSecondaryTaxes;
        public ObservableCollection<TaxGraphQLModel> PurchaseSecondaryTaxes
        {
            get { return _purchaseSecondaryTaxes; }
            set
            {
                if (_purchaseSecondaryTaxes != value)
                {
                    _purchaseSecondaryTaxes = value;
                    NotifyOfPropertyChange(nameof(PurchaseSecondaryTaxes));
                }
            }
        }

        private ObservableCollection<TaxGraphQLModel> _salesPrimaryTaxes;
        public ObservableCollection<TaxGraphQLModel> SalesPrimaryTaxes
        {
            get { return _salesPrimaryTaxes; }
            set
            {
                if (_salesPrimaryTaxes != value)
                {
                    _salesPrimaryTaxes = value;
                    NotifyOfPropertyChange(nameof(SalesPrimaryTaxes));
                }
            }
        }


        private ObservableCollection<TaxGraphQLModel> _salesSecondaryTaxes;
        public ObservableCollection<TaxGraphQLModel> SalesSecondaryTaxes
        {
            get { return _salesSecondaryTaxes; }
            set
            {
                if (_salesSecondaryTaxes != value)
                {
                    _salesSecondaryTaxes = value;
                    NotifyOfPropertyChange(nameof(SalesSecondaryTaxes));
                }
            }
        }


        private ObservableCollection<AccountingAccountGraphQLModel> _aiuAdministrationAccountingAccounts;
        public ObservableCollection<AccountingAccountGraphQLModel> AiuAdministrationAccountingAccounts
        {
            get { return _aiuAdministrationAccountingAccounts; }
            set
            {
                if (_aiuAdministrationAccountingAccounts != value)
                {
                    _aiuAdministrationAccountingAccounts = value;
                    NotifyOfPropertyChange(nameof(AiuAdministrationAccountingAccounts));
                }
            }
        }


        private ObservableCollection<AccountingAccountGraphQLModel> _aiuUnforeseenAccountingAccounts;
        public ObservableCollection<AccountingAccountGraphQLModel> AiuUnforeseenAccountingAccounts
        {
            get { return _aiuUnforeseenAccountingAccounts; }
            set
            {
                if (_aiuUnforeseenAccountingAccounts != value)
                {
                    _aiuUnforeseenAccountingAccounts = value;
                    NotifyOfPropertyChange(nameof(AiuUnforeseenAccountingAccounts));
                }
            }
        }

        private ObservableCollection<AccountingAccountGraphQLModel> _aiuUtilityAccountingAccounts;
        public ObservableCollection<AccountingAccountGraphQLModel> AiuUtilityAccountingAccounts
        {
            get { return _aiuUtilityAccountingAccounts; }
            set
            {
                if (_aiuUtilityAccountingAccounts != value)
                {
                    _aiuUtilityAccountingAccounts = value;
                    NotifyOfPropertyChange(nameof(AiuUtilityAccountingAccounts));
                }
            }
        }

        private ObservableCollection<AccountingAccountGraphQLModel> _accountCostAccountingAccounts;
        public ObservableCollection<AccountingAccountGraphQLModel> AccountCostAccountingAccounts
        {
            get { return _accountCostAccountingAccounts; }
            set
            {
                if (_accountCostAccountingAccounts != value)
                {
                    _accountCostAccountingAccounts = value;
                    NotifyOfPropertyChange(nameof(AccountCostAccountingAccounts));
                }
            }
        }

        private ObservableCollection<AccountingAccountGraphQLModel> _incomeAccountingAccounts;
        public ObservableCollection<AccountingAccountGraphQLModel> IncomeAccountingAccounts
        {
            get { return _incomeAccountingAccounts; }
            set
            {
                if (_incomeAccountingAccounts != value)
                {
                    _incomeAccountingAccounts = value;
                    NotifyOfPropertyChange(nameof(IncomeAccountingAccounts));
                }
            }
        }


        private ObservableCollection<AccountingAccountGraphQLModel> _incomeReverseAccountingAccounts;
        public ObservableCollection<AccountingAccountGraphQLModel> IncomeReverseAccountingAccounts
        {
            get { return _incomeReverseAccountingAccounts; }
            set
            {
                if (_incomeReverseAccountingAccounts != value)
                {
                    _incomeReverseAccountingAccounts = value;
                    NotifyOfPropertyChange(nameof(IncomeReverseAccountingAccounts));
                }
            }
        }

        private ObservableCollection<AccountingAccountGraphQLModel> _inventoryAccountingAccounts;
        public ObservableCollection<AccountingAccountGraphQLModel> InventoryAccountingAccounts
        {
            get { return _inventoryAccountingAccounts; }
            set
            {
                if (_inventoryAccountingAccounts != value)
                {
                    _inventoryAccountingAccounts = value;
                    NotifyOfPropertyChange(nameof(InventoryAccountingAccounts));
                }
            }
        }

        #endregion
        #region ModelProperties

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

       private AccountingAccountGraphQLModel? _selectedAccountAiuAdministration;
        [ExpandoPath("accountAiuAdministrationId", SerializeAsId = true)]
        public AccountingAccountGraphQLModel? SelectedAccountAiuAdministration
        {
            get => _selectedAccountAiuAdministration;
            set
            {
                _selectedAccountAiuAdministration = value;
                NotifyOfPropertyChange(nameof(SelectedAccountAiuAdministration));
                this.TrackChange(nameof(SelectedAccountAiuAdministration));
                NotifyOfPropertyChange(nameof(CanSave));


            }
        }


       private AccountingAccountGraphQLModel? _selectedAccountAiuUnforeseen;
        [ExpandoPath("accountAiuUnforeseenId", SerializeAsId = true)]
        public AccountingAccountGraphQLModel? SelectedAccountAiuUnforeseen
        {
            get => _selectedAccountAiuUnforeseen;
            set
            {
                _selectedAccountAiuUnforeseen = value;
                NotifyOfPropertyChange(nameof(SelectedAccountAiuUnforeseen));
                this.TrackChange(nameof(SelectedAccountAiuUnforeseen));
                NotifyOfPropertyChange(nameof(CanSave));


            }
        }

       private AccountingAccountGraphQLModel? _selectedAccountAiuUtility;
        [ExpandoPath("accountAiuUtilityId", SerializeAsId = true)]

        public AccountingAccountGraphQLModel? SelectedAccountAiuUtility
        {
            get => _selectedAccountAiuUtility;
            set
            {
                _selectedAccountAiuUtility = value;
                NotifyOfPropertyChange(nameof(SelectedAccountAiuUtility));
                this.TrackChange(nameof(SelectedAccountAiuUtility));
                NotifyOfPropertyChange(nameof(CanSave));


            }
        }

       private AccountingAccountGraphQLModel? _selectedAccountCost;
        [ExpandoPath("accountCostId", SerializeAsId = true)]

        public AccountingAccountGraphQLModel? SelectedAccountCost
        {
            get => _selectedAccountCost;
            set
            {
                _selectedAccountCost = value;
                NotifyOfPropertyChange(nameof(SelectedAccountCost));
                this.TrackChange(nameof(SelectedAccountCost));
                ValidateProperty(nameof(SelectedAccountCost), value?.Id);

                NotifyOfPropertyChange(nameof(CanSave));


            }
        }

       private AccountingAccountGraphQLModel? _selectedAccountIncome;
        [ExpandoPath("accountIncomeId", SerializeAsId = true)]

        public AccountingAccountGraphQLModel? SelectedAccountIncome
        {
            get => _selectedAccountIncome;
            set
            {
                _selectedAccountIncome = value;
                NotifyOfPropertyChange(nameof(SelectedAccountIncome));
                this.TrackChange(nameof(SelectedAccountIncome));
                ValidateProperty(nameof(SelectedAccountIncome), value?.Id);

                NotifyOfPropertyChange(nameof(CanSave));


            }
        }

       private AccountingAccountGraphQLModel? _selectedAccountIncomeReverse;
        [ExpandoPath("accountIncomeReverseId", SerializeAsId = true)]

        public AccountingAccountGraphQLModel? SelectedAccountIncomeReverse
        {
            get => _selectedAccountIncomeReverse;
            set
            {
                _selectedAccountIncomeReverse = value;
                NotifyOfPropertyChange(nameof(SelectedAccountIncomeReverse));
                this.TrackChange(nameof(SelectedAccountIncomeReverse));
                ValidateProperty(nameof(SelectedAccountIncomeReverse), value?.Id);

                NotifyOfPropertyChange(nameof(CanSave));


            }
        }

       private AccountingAccountGraphQLModel? _selectedAccountInventory;
        [ExpandoPath("accountInventoryId", SerializeAsId = true)]

        public AccountingAccountGraphQLModel? SelectedAccountInventory
        {
            get => _selectedAccountInventory;
            set
            {
                _selectedAccountInventory = value;
                this.TrackChange(nameof(SelectedAccountInventory));
                ValidateProperty(nameof(SelectedAccountInventory), value?.Id);
                NotifyOfPropertyChange(nameof(SelectedAccountInventory));

                NotifyOfPropertyChange(nameof(CanSave));


            }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                NotifyOfPropertyChange(nameof(Name));
                this.TrackChange(nameof(Name));
                ValidateProperty(nameof(Name), value);

                NotifyOfPropertyChange(nameof(CanSave));


            }
        }

        private TaxGraphQLModel? _selectedPurchasePrimaryTax;
        [ExpandoPath("purchasePrimaryTaxId", SerializeAsId = true)]

        public TaxGraphQLModel? SelectedPurchasePrimaryTax
        {
            get => _selectedPurchasePrimaryTax;
            set
            {
                _selectedPurchasePrimaryTax = value;
                NotifyOfPropertyChange(nameof(SelectedPurchasePrimaryTax));
                this.TrackChange(nameof(SelectedPurchasePrimaryTax));
                ValidateProperty(nameof(SelectedPurchasePrimaryTax), value?.Id);
                NotifyOfPropertyChange(nameof(PurchaseSecondaryTaxVisibility));
                PurchaseSecondaryTaxes = [.. _taxCache.Items.Where(f => f.Id != value?.Id)];
                this.SeedValue(nameof(SelectedPurchaseSecondaryTax), SelectedPurchaseSecondaryTax);
                NotifyOfPropertyChange(nameof(CanSave));


            }
        }


        private TaxGraphQLModel? _selectedPurchaseSecondaryTax;
        [ExpandoPath("purchaseSecondaryTaxId", SerializeAsId = true)]
        public TaxGraphQLModel? SelectedPurchaseSecondaryTax
        {
            get => _selectedPurchaseSecondaryTax;
            set
            {
                _selectedPurchaseSecondaryTax = value;
                NotifyOfPropertyChange(nameof(SelectedPurchaseSecondaryTax));
                this.TrackChange(nameof(SelectedPurchaseSecondaryTax), SelectedPurchaseSecondaryTax?.Id > 0 ? SelectedPurchaseSecondaryTax : null);
                NotifyOfPropertyChange(nameof(CanSave));


            }
        }

        private TaxGraphQLModel? _selectedSalesPrimaryTax;
        [ExpandoPath("salesPrimaryTaxId", SerializeAsId = true)]
        public TaxGraphQLModel? SelectedSalesPrimaryTax
        {
            get => _selectedSalesPrimaryTax;
            set
            {
                _selectedSalesPrimaryTax = value;
                NotifyOfPropertyChange(nameof(SelectedSalesPrimaryTax));
                this.TrackChange(nameof(SelectedSalesPrimaryTax));
                ValidateProperty(nameof(SelectedSalesPrimaryTax), value?.Id);
                
               NotifyOfPropertyChange(nameof(SalesSecondaryTaxVisibility));
                if (value == null)
                {
                    this.SelectedSalesSecondaryTax = null;
                }
                else
                {
                    SalesSecondaryTaxes = [.. _taxCache.Items.Where(f => f.Id != value.Id)];

                }
                this.SeedValue(nameof(SelectedSalesSecondaryTax), SelectedSalesSecondaryTax);
                NotifyOfPropertyChange(nameof(CanSave));


            }
        }

        private TaxGraphQLModel? _selectedSalesSecondaryTax;
        [ExpandoPath("salesSecondaryTaxId", SerializeAsId = true)]
        public TaxGraphQLModel? SelectedSalesSecondaryTax
        {
            get => _selectedSalesSecondaryTax;
            set
            {
                _selectedSalesSecondaryTax = value;
                NotifyOfPropertyChange(nameof(SelectedSalesSecondaryTax));
                this.TrackChange(nameof(SelectedSalesSecondaryTax), SelectedSalesSecondaryTax?.Id > 0 ? SelectedSalesSecondaryTax : null);

                NotifyOfPropertyChange(nameof(CanSave));


            }
        }
        private bool _allowAiu;
        public bool AllowAiu
        {
            get => _allowAiu;
            set
            {
                _allowAiu = value;
                NotifyOfPropertyChange(nameof(AllowAiu));
                NotifyOfPropertyChange(nameof(AllowAiuVisibility));
                this.TrackChange(nameof(AllowAiu));
                NotifyOfPropertyChange(nameof(CanSave));


            }
        }

        

        public bool IsNewRecord => Id == 0;
        #endregion

        #region PropertiesAndCommands
        
        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync, CanSave);
                return _saveCommand;
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
        public bool CanGoBack(object p)
        {
            return !IsBusy;
        }

        public bool CanSave
        {
            get
            {
                if (_errors.Count > 0 ||  !this.HasChanges()) { return false; }
                return true;
            }
        }
        private Visibility _purchaseSecondaryTaxVisibility;
        public Visibility PurchaseSecondaryTaxVisibility
        {
            get { return SelectedPurchasePrimaryTax != null  ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                _purchaseSecondaryTaxVisibility = value;
            }
        }
        private Visibility _salesSecondaryTaxVisibility;
        public Visibility SalesSecondaryTaxVisibility
        {
            get { return (SelectedSalesPrimaryTax != null) ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                _salesSecondaryTaxVisibility = value;
            }
        }
        private Visibility _allowAiuVisibility;
        public Visibility AllowAiuVisibility
        {
            get { return (AllowAiu == true) ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                _allowAiuVisibility = value;
            }
        }
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

        public void GoBack(object p)
        {
            CleanUpControls();
            _ = Task.Run(() => Context.ActivateMasterViewAsync());

        }
        public void CleanUpControls()
        {
            Name = "";
           
        }
        #endregion
        Dictionary<string, List<string>> _errors;


        
        public bool HasErrors => _errors.Count > 0;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        private void ValidateProperty(string propertyName, string value)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty.Trim();
            try
            {
                ClearErrors(propertyName);
                switch (propertyName)
                {
                    case nameof(Name):
                        if (string.IsNullOrEmpty(value)) AddError(propertyName, "El nombre no puede estar vacío");
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
          
            
                ValidateProperty(nameof(Name), Name);
                ValidateProperty(nameof(SelectedAccountCost), SelectedAccountCost?.Id);
            ValidateProperty(nameof(SelectedAccountIncome), SelectedAccountIncome?.Id);
            
            ValidateProperty(nameof(SelectedAccountIncomeReverse), SelectedAccountIncomeReverse?.Id);
                ValidateProperty(nameof(SelectedAccountInventory), SelectedAccountInventory?.Id);
                ValidateProperty(nameof(SelectedSalesPrimaryTax), SelectedSalesPrimaryTax?.Id);
                ValidateProperty(nameof(SelectedPurchasePrimaryTax), SelectedPurchasePrimaryTax?.Id);

        }
        private void ValidateProperty(string propertyName, int? value)
        {
            
            try
            {
                ClearErrors(propertyName);
                switch (propertyName)
                {
                    case nameof(SelectedAccountCost):
                        if (value == 0 || value == null) AddError(propertyName, "Debe Seleccionar una cuenta de Costo");
                        break;
                    case nameof(SelectedAccountIncome):
                        if (value == 0 || value == null) AddError(propertyName, "Debe Seleccionar una cuenta de Ingreso");
                        break;
                    case nameof(SelectedAccountInventory):
                        if (value == 0 || value == null) AddError(propertyName, "Debe Seleccionar una cuenta de Inventario");
                        break;
                    case nameof(SelectedAccountIncomeReverse):
                        if (value == 0 || value == null) AddError(propertyName, "Debe Seleccionar una cuenta de Devolucion");
                        break;


                   
                    case nameof(SelectedPurchasePrimaryTax):
                        if (value == 0 || value == null) AddError(propertyName, "Debe Seleccionar un impusto de compras");
                        break;
                   
                    case nameof(SelectedSalesPrimaryTax):
                        if (value == 0 || value == null) AddError(propertyName, "Debe Seleccionar un impuesto de venta");
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
        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return new List<object>();
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
        #region ApiMethods
        public async Task<AccountingGroupGraphQLModel> LoadDataForEditAsync(int id)
        {
            try
            {
                string query = GetLoadAccountingGroupByIdQuery();

                dynamic variables = new ExpandoObject();


                variables.singleItemResponseId = id;

                var entity = await _accountingGroupService.FindByIdAsync(query, variables);

                // Poblar el ViewModel con los datos del entity (sin bloquear UI thread)
                PopulateFromAccountingGroup(entity);

                return entity;
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
        public void PopulateFromAccountingGroup(AccountingGroupGraphQLModel entity)
        {
           
            PurchasePrimaryTaxes = [.. _taxCache.Items];
            SalesPrimaryTaxes = [.. _taxCache.Items];

            Id = entity.Id;
            Name = entity.Name;
            AllowAiu = entity.AllowAiu;
            SelectedAccountAiuAdministration = AiuAdministrationAccountingAccounts.FirstOrDefault(f => f.Id ==  entity.AccountAiuAdministration?.Id);
            SelectedAccountAiuUnforeseen = AiuUnforeseenAccountingAccounts.FirstOrDefault(f => f.Id == entity.AccountAiuUnforeseen?.Id);
            SelectedAccountAiuUtility = AiuUtilityAccountingAccounts.FirstOrDefault(f => f.Id == entity.AccountAiuUtility?.Id);
            SelectedAccountCost = AccountCostAccountingAccounts.FirstOrDefault(f => f.Id == entity.AccountCost?.Id);
            SelectedAccountIncome = IncomeAccountingAccounts.FirstOrDefault(f => f.Id == entity.AccountIncome?.Id);
            SelectedAccountIncomeReverse = IncomeReverseAccountingAccounts.FirstOrDefault(f => f.Id == entity.AccountIncomeReverse?.Id);
            SelectedAccountInventory = InventoryAccountingAccounts.FirstOrDefault(f => f.Id == entity.AccountInventory?.Id);

            SelectedPurchasePrimaryTax = PurchasePrimaryTaxes.FirstOrDefault(f => f.Id == entity.PurchasePrimaryTax?.Id);
            SelectedSalesPrimaryTax = SalesPrimaryTaxes.FirstOrDefault(f => f.Id == entity.SalesPrimaryTax?.Id);

            PurchaseSecondaryTaxes = [.. _taxCache.Items.Where(f => f.Id != SelectedPurchasePrimaryTax?.Id)];

            SelectedPurchaseSecondaryTax = PurchaseSecondaryTaxes.FirstOrDefault(f => f.Id == entity.PurchaseSecondaryTax?.Id);


            this.SeedValue(nameof(SelectedPurchaseSecondaryTax), SelectedPurchaseSecondaryTax);

            SalesSecondaryTaxes = [.. _taxCache.Items.Where(f => f.Id != SelectedSalesPrimaryTax?.Id)];

            SelectedSalesSecondaryTax = SalesSecondaryTaxes.FirstOrDefault(f => f.Id == entity.SalesSecondaryTax?.Id);

            this.SeedValue(nameof(Name), Name);

            this.SeedValue(nameof(AllowAiu), AllowAiu);
            this.SeedValue(nameof(SelectedAccountAiuAdministration), SelectedAccountAiuAdministration);
            this.SeedValue(nameof(SelectedAccountAiuUnforeseen), SelectedAccountAiuUnforeseen);
            this.SeedValue(nameof(SelectedAccountAiuUtility), SelectedAccountAiuUtility);
            this.SeedValue(nameof(SelectedAccountCost), SelectedAccountCost);
            this.SeedValue(nameof(SelectedAccountIncome), SelectedAccountIncome);
            this.SeedValue(nameof(SelectedAccountIncomeReverse), SelectedAccountIncomeReverse);
            this.SeedValue(nameof(SelectedAccountInventory), SelectedAccountInventory);
            this.SeedValue(nameof(SelectedPurchasePrimaryTax), SelectedPurchasePrimaryTax);
            this.SeedValue(nameof(SelectedPurchaseSecondaryTax), SelectedPurchaseSecondaryTax);
            this.SeedValue(nameof(SelectedSalesPrimaryTax), SelectedSalesPrimaryTax);
            this.SeedValue(nameof(SelectedSalesSecondaryTax), SelectedSalesSecondaryTax);

            this.AcceptChanges();



        }
        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                UpsertResponseType<AccountingGroupGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }
                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new AccountingGroupCreateMessage() { CreatedAccountingGroup = result }
                        : new AccountingGroupUpdateMessage() { UpdatedAccountingGroup = result }
                );

                // Context.EnableOnViewReady = false;
                await Context.ActivateMasterViewAsync();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
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

        public async Task<UpsertResponseType<AccountingGroupGraphQLModel>> ExecuteSaveAsync()
        {

            dynamic variables = new ExpandoObject();


            if (IsNewRecord)
            {

                variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                string query = GetCreateQuery();
                UpsertResponseType<AccountingGroupGraphQLModel>groupCreated = await _accountingGroupService.CreateAsync<UpsertResponseType<AccountingGroupGraphQLModel>>(query, variables);
                return groupCreated;
            }
            else
            {

                string query = GetUpdateQuery();
                variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = Id;
                UpsertResponseType<AccountingGroupGraphQLModel> updatedGroup = await _accountingGroupService.UpdateAsync<UpsertResponseType<AccountingGroupGraphQLModel>>(query, variables);
                return updatedGroup;
            }

        }
        public string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<AccountingGroupGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingGroup", nested: sq => sq
                   .Field(e => e.Id)
                  .Field(e => e.Name)

                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateAccountingGroupInput!");

            var fragment = new GraphQLQueryFragment("createAccountingGroup", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<AccountingGroupGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingGroup", nested: sq => sq
                    .Field(e => e.Id)
                   .Field(e => e.Name)
                   
                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();


            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateAccountingGroupInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateAccountingGroup", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public string GetLoadAccountingGroupByIdQuery()
        {
            var accountingGroupFields = FieldSpec<AccountingGroupGraphQLModel>
             .Create()

                 .Field(e => e.Id)

                 .Field(e => e.Name)
                 .Select(e => e.AccountAiuAdministration, acc => acc
                         .Field(c => c.Id)
                         .Field(c => c.Name)

                 )
                 .Select(e => e.AccountAiuUnforeseen, acc => acc
                         .Field(c => c.Id)
                         .Field(c => c.Name)

                 )
                 .Select(e => e.AccountAiuUtility, acc => acc
                         .Field(c => c.Id)
                         .Field(c => c.Name)

                 )
                 .Select(e => e.AccountCost, acc => acc
                         .Field(c => c.Id)
                         .Field(c => c.Name)

                 )
                 .Select(e => e.AccountIncome, acc => acc
                         .Field(c => c.Id)
                         .Field(c => c.Name)

                 )
                 .Select(e => e.AccountIncomeReverse, acc => acc
                         .Field(c => c.Id)
                         .Field(c => c.Name)

                 )
                 .Select(e => e.AccountInventory, acc => acc
                         .Field(c => c.Id)
                         .Field(c => c.Name)

                 )
                 .Field(c => c.AllowAiu)

                 .Select(e => e.PurchasePrimaryTax, acc => acc
                         .Field(c => c.Id)
                         .Field(c => c.Name)

                 )
                 .Select(e => e.PurchaseSecondaryTax, acc => acc
                         .Field(c => c.Id)
                         .Field(c => c.Name)

                 )
                 .Select(e => e.SalesPrimaryTax, acc => acc
                         .Field(c => c.Id)
                         .Field(c => c.Name)

                 )
                 .Select(e => e.SalesSecondaryTax, acc => acc
                         .Field(c => c.Id)
                         .Field(c => c.Name)

                 )


                 .Build();
            var accountingGroupIdParameter = new GraphQLQueryParameter("id", "ID!");

            var accountingGroupFragment = new GraphQLQueryFragment("accountingGroup", [accountingGroupIdParameter], accountingGroupFields, "SingleItemResponse");

            var builder = new GraphQLQueryBuilder([accountingGroupFragment]);

            return builder.GetQuery();

        }
        #endregion
    }


}
