using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using Common.Services;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.Books;
using Models.Global;
using Models.Inventory;
using NetErp.Billing.PriceList.DTO;
using NetErp.Billing.PriceList.PriceListHelpers;
using NetErp.Global.Modals.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Helpers.Messages;
using NetErp.Helpers.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Linq;
using static Models.Global.GraphQLResponseTypes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace NetErp.Billing.PriceList.ViewModels
{
    public class PriceListMasterViewModel : Screen,
        IHandle<PriceListCreateMessage>,
        IHandle<PriceListUpdateMessage>,
        IHandle<PriceListDeleteMessage>,
        IHandle<OperationCompletedMessage>,
        IHandle<CriticalSystemErrorMessage>,
        IHandle<PriceListArchiveMessage>
    {
        // Flag to prevent cascading reload operations during internal updates
        private bool _isUpdating = false;
        private readonly Dictionary<Guid, int> _operationItemMapping = new Dictionary<Guid, int>();
        
        // Dependency injection fields
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<PriceListItemGraphQLModel> _priceListItemService;
        private readonly IBackgroundQueueService _backgroundQueueService;
        private readonly IPriceListCalculatorFactory _calculatorFactory;
        private readonly Helpers.IDialogService _dialogService;
        private readonly IRepository<PriceListGraphQLModel> _priceListService;

        //Caches necesarios en ventanas modales
        private readonly IGraphQLClient _graphQLClient;
        private readonly StorageCache _storageCache;
        private readonly CostCenterCache _costCenterCache;
        private readonly PaymentMethodCache _paymentMethodCache;
        
        public PriceListViewModel Context { get; set; }
        public string MaskN2 { get; set; } = "n2";

        public string MaskN5 { get; set; } = "n5";

        private bool _mainIsBusy = true;
        public bool MainIsBusy
        {
            get { return _mainIsBusy; }
            set
            {
                if (_mainIsBusy != value)
                {
                    _mainIsBusy = value;
                    NotifyOfPropertyChange(nameof(MainIsBusy));
                }
            }
        }

        private bool _isBusy;

        public bool IsBusy
        {
            get { return _isBusy; }
            set 
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }


        private string _filterSearch = "";
        public string FilterSearch
        {
            get { return _filterSearch; }
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 2)
                    {
                        PageIndex = 1;
                        if (IsInitialized) _ = LoadPriceListItemsAsync();
                    }
                }
            }
        }


        private ObservableCollection<PriceListItemDTO> _priceListItems = [];
        public ObservableCollection<PriceListItemDTO> PriceListItems
        {
            get { return _priceListItems; }
            set
            {
                if (_priceListItems != value)
                {
                    _priceListItems = value;
                    NotifyOfPropertyChange(nameof(PriceListItems));
                }
            }
        }

        private ObservableCollection<CatalogGraphQLModel> _catalogs = [];

        public ObservableCollection<CatalogGraphQLModel> Catalogs
        {
            get { return _catalogs; }
            set
            {
                if (_catalogs != value)
                {
                    _catalogs = value;
                    NotifyOfPropertyChange(nameof(Catalogs));
                }
            }
        }

        private CancellationTokenSource _cascadeCancellation = new();

        private CatalogGraphQLModel _selectedCatalog;

        public CatalogGraphQLModel SelectedCatalog
        {
            get { return _selectedCatalog; }
            set
            {
                if (_selectedCatalog != value)
                {
                    _selectedCatalog = value;
                    NotifyOfPropertyChange(nameof(SelectedCatalog));
                    if (!_isUpdating && value != null)
                    {
                        _cascadeCancellation?.Cancel();
                        _cascadeCancellation?.Dispose();
                        _cascadeCancellation = new CancellationTokenSource();
                        
                        LoadItemTypes();
                        if (IsInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
                    }
                }
            }
        }

        private ObservableCollection<ItemTypeGraphQLModel> _itemTypes = new();

        public ObservableCollection<ItemTypeGraphQLModel> ItemTypes
        {
            get { return _itemTypes; }
            set
            {
                if (_itemTypes != value)
                {
                    _itemTypes = value;
                    NotifyOfPropertyChange(nameof(ItemTypes));
                }
            }
        }

        private ItemTypeGraphQLModel _selectedItemType;

        public ItemTypeGraphQLModel SelectedItemType
        {
            get { return _selectedItemType; }
            set
            {
                if (_selectedItemType != value)
                {
                    _selectedItemType = value;
                    NotifyOfPropertyChange(nameof(SelectedItemType));
                    if (!_isUpdating && value != null)
                    {
                        _cascadeCancellation?.Cancel();
                        _cascadeCancellation?.Dispose();
                        _cascadeCancellation = new CancellationTokenSource();
                        
                        LoadItemCategories();
                        if (IsInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
                    }
                }
            }
        }

        public bool CanShowItemCategories => SelectedItemType != null && SelectedItemType.Id != 0;

        private ObservableCollection<ItemCategoryGraphQLModel> _itemsCategories = new();

        public ObservableCollection<ItemCategoryGraphQLModel> ItemCategories
        {
            get { return _itemsCategories; }
            set
            {
                if (_itemsCategories != value)
                {
                    _itemsCategories = value;
                    NotifyOfPropertyChange(nameof(ItemCategories));
                }
            }
        }

        private ItemCategoryGraphQLModel _selectedItemCategory;

        public ItemCategoryGraphQLModel SelectedItemCategory
        {
            get { return _selectedItemCategory; }
            set
            {
                if (_selectedItemCategory != value)
                {
                    _selectedItemCategory = value;
                    NotifyOfPropertyChange(nameof(SelectedItemCategory));
                    if (!_isUpdating && value != null)
                    {
                        _cascadeCancellation?.Cancel();
                        _cascadeCancellation?.Dispose();
                        _cascadeCancellation = new CancellationTokenSource();
                        
                        LoadItemSubCategories();
                        if (IsInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
                    }
                }
            }
        }

        private ObservableCollection<ItemSubCategoryGraphQLModel> _itemsSubCategories = new();

        public ObservableCollection<ItemSubCategoryGraphQLModel> ItemSubCategories
        {
            get { return _itemsSubCategories; }
            set
            {
                if (_itemsSubCategories != value)
                {
                    _itemsSubCategories = value;
                    NotifyOfPropertyChange(nameof(ItemSubCategories));
                }
            }
        }

        private ItemSubCategoryGraphQLModel _selectedItemSubCategory;

        public ItemSubCategoryGraphQLModel SelectedItemSubCategory
        {
            get { return _selectedItemSubCategory; }
            set
            {
                if (_selectedItemSubCategory != value)
                {
                    _selectedItemSubCategory = value;
                    NotifyOfPropertyChange(nameof(SelectedItemSubCategory));
                    if (!_isUpdating && value != null && IsInitialized)
                    {
                        _cascadeCancellation?.Cancel();
                        _cascadeCancellation?.Dispose();
                        _cascadeCancellation = new CancellationTokenSource();
                        
                        _ = ReloadDataAsync(_cascadeCancellation.Token);
                    }
                }
            }
        }

        public bool CanShowItemSubCategories => SelectedItemType != null && SelectedItemType.Id != 0 && SelectedItemCategory != null && SelectedItemCategory.Id != 0;

        private ObservableCollection<PriceListGraphQLModel> _priceLists = [];

        public ObservableCollection<PriceListGraphQLModel> PriceLists
        {
            get { return _priceLists; }
            set
            {
                if (_priceLists != value)
                {
                    _priceLists = value;
                    NotifyOfPropertyChange(nameof(PriceLists));
                }
            }
        }

        private PriceListGraphQLModel? _selectedPriceList;

        public PriceListGraphQLModel? SelectedPriceList
        {
            get { return _selectedPriceList; }
            set
            {
                if (_selectedPriceList != value)
                {
                    _selectedPriceList = value;
                    NotifyOfPropertyChange(nameof(SelectedPriceList));
                    NotifyOfPropertyChange(nameof(CostByStorageInformation));
                    NotifyOfPropertyChange(nameof(SelectedPriceListIsNotActive));
                    NotifyOfPropertyChange(nameof(IsGridReadOnly));
                    NotifyOfPropertyChange(nameof(IsPriceList));
                    NotifyOfPropertyChange(nameof(CanCreatePromotion));

                    if (!_isUpdating && value != null)
                    {
                        _cascadeCancellation?.Cancel();
                        _cascadeCancellation?.Dispose();
                        _cascadeCancellation = new CancellationTokenSource();

                        LoadItemTypes();
                        FilterSearch = "";
                        if (IsInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
                    }
                }
            }
        }

        public bool SelectedPriceListIsNotActive => SelectedPriceList != null && !SelectedPriceList.IsActive;
        public bool IsGridReadOnly => SelectedPriceListIsNotActive || _backgroundQueueService.HasCriticalError();

        private PriceListItemDTO? _selectedPriceListItems;

        public PriceListItemDTO? SelectedPriceListItem
        {
            get { return _selectedPriceListItems; }
            set 
            {
                if (_selectedPriceListItems != value)
                {
                    _selectedPriceListItems = value;
                    NotifyOfPropertyChange(nameof(SelectedPriceListItem));
                    NotifyOfPropertyChange(nameof(ShowInventoryQuantity));
                }
            }
        }

        public bool IsPriceList
        {
            get
            {
                if (SelectedPriceList == null) return false;
                return SelectedPriceList.Parent == null || SelectedPriceList.Parent.Id == 0;
            }
        }

        public bool ShowInventoryQuantity
        {
            get { return SelectedPriceListItem != null && SelectedPriceListItem.Item.Stocks.Any(); }
        }

        public bool CostByStorageInformation
        {
            get
            {
                if(SelectedPriceList != null) return SelectedPriceList.Storage != null && SelectedPriceList.Storage.Id != 0;
                return false;
            }
        }

        private ICommand _createPriceListCommand;
        public ICommand CreatePriceListCommand
        {
            get
            {
                if (_createPriceListCommand is null) _createPriceListCommand = new AsyncCommand(CreatePriceListAsync);
                return _createPriceListCommand;
            }
        }

        public async Task CreatePriceListAsync()
        {
            try
            {
                var viewModel = new CreatePriceListModalViewModel(_dialogService, Context.EventAggregator, _priceListService, _storageCache, _costCenterCache, _graphQLClient);
                await viewModel.InitializeAsync();
                viewModel.SetForNew();
                await _dialogService.ShowDialogAsync(viewModel, "Creación de lista de precios");
            }
            catch (AsyncException ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }

        private ICommand _configurationCommand;
        public ICommand ConfigurationCommand
        {
            get
            {
                if (_configurationCommand is null) _configurationCommand = new AsyncCommand(ConfigurationAsync);
                return _configurationCommand;
            }
        }

        //TODO: Refactorizar throw exception en FirstOrDefault
        public async Task ConfigurationAsync()
        {
            try
            {
                if (!IsPriceList)
                {
                    MainIsBusy = true;
                    await Context.ActivateUpdatePromotionViewAsync(SelectedPriceList);
                    MainIsBusy = false;
                    return;
                }
                var viewModel = new UpdatePriceListModalViewModel(_dialogService, Context.EventAggregator, Context.AutoMapper, _priceListService, _storageCache, _costCenterCache, _paymentMethodCache, _graphQLClient);
                await viewModel.InitializeAsync();
                viewModel.SetForEdit(SelectedPriceList);
                await _dialogService.ShowDialogAsync(viewModel, "Configuración de lista de precios");
            }
            catch (AsyncException ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }

        private ICommand _deletePriceListCommand;
        public ICommand DeletePriceListCommand
        {
            get
            {
                if (_deletePriceListCommand is null) _deletePriceListCommand = new AsyncCommand(DeletePriceListAsync);
                return _deletePriceListCommand;
            }
        }

        public async Task DeletePriceListAsync()
        {
            try
            {
                if (SelectedPriceList is null) return;
                MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {SelectedPriceList.Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
                IsBusy = true;
                int id = SelectedPriceList.Id;

                var (canDeleteFragment, canDeleteQuery) = _canDeletePriceListQuery.Value;
                var canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", id)
                    .Build();
                var validation = await _priceListService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

                if (validation.CanDelete)
                {
                    var (deleteFragment, deleteQuery) = _deletePriceListQuery.Value;
                    var deleteVars = new GraphQLVariables()
                        .For(deleteFragment, "id", id)
                        .Build();
                    DeleteResponseType deletedPriceList = await _priceListService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                    if (!deletedPriceList.Success)
                    {
                        ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedPriceList.Message}");
                        return;
                    }

                    await Context.EventAggregator.PublishOnCurrentThreadAsync(new PriceListDeleteMessage { DeletedPriceList = deletedPriceList });
                }
                else
                {
                    // Si no se puede eliminar, se archiva
                    var (archiveFragment, archiveQuery) = _archivePriceListQuery.Value;
                    dynamic archiveVars = new ExpandoObject();
                    archiveVars.UpdateResponseId = id;
                    archiveVars.UpdateResponseData = new ExpandoObject();
                    archiveVars.UpdateResponseData.archived = true;
                    UpsertResponseType<PriceListGraphQLModel> archivedPriceList = await _priceListService.UpdateAsync<UpsertResponseType<PriceListGraphQLModel>>(archiveQuery, archiveVars);

                    if (!archivedPriceList.Success)
                    {
                        ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {archivedPriceList.Message}");
                        return;
                    }

                    await Context.EventAggregator.PublishOnCurrentThreadAsync(new PriceListArchiveMessage { ArchivedPriceList = archivedPriceList });
                }
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        private ICommand _createPromotionCommand;
        public ICommand CreatePromotionCommand
        {
            get
            {
                if (_createPromotionCommand is null) _createPromotionCommand = new AsyncCommand(CreatePromotionAsync);
                return _createPromotionCommand;
            }
        }

        public async Task CreatePromotionAsync()
        {
            if (SelectedPriceList is null) return;
            try
            {
                var viewModel = new CreatePromotionModalViewModel(_dialogService, Context.EventAggregator, SelectedPriceList, _priceListService);
                viewModel.SetForNew();
                await _dialogService.ShowDialogAsync(viewModel, "Creación de promociones");
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(CreatePromotionAsync)}: {ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
        }

        public bool CanCreatePromotion => SelectedPriceList != null && SelectedPriceList.IsActive && SelectedPriceList.Parent == null;

        public async Task InitializeAsync()
        {
            try
            {
                _isUpdating = true;
                var (query, catalogsFragment, priceListsFragment) = _initializeQuery.Value;

                dynamic variables = new GraphQLVariables()
                    .For(catalogsFragment, "pagination", new { pageSize = -1 })
                    .For(priceListsFragment, "pagination", new { pageSize = -1 })
                    .Build();

                PriceListDataContext result = await _priceListItemService.GetDataContextAsync<PriceListDataContext>(query, variables);


                Catalogs = new ObservableCollection<CatalogGraphQLModel>(result.CatalogsPage.Entries);
                SelectedCatalog = Catalogs.FirstOrDefault() ?? throw new Exception("SelectedCatalog can't be null");
                PriceLists = new ObservableCollection<PriceListGraphQLModel>(result.PriceListsPage.Entries);
                NotifyOfPropertyChange(nameof(ShowEmptyState));
                NotifyOfPropertyChange(nameof(HasRecords));
                if(PriceLists is null || PriceLists.Count == 0) return;
                SelectedPriceList = PriceLists.FirstOrDefault() ?? throw new Exception("SelectedPriceList can't be null");
                LoadItemTypes();
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
            finally
            {
                IsInitialized = true;
                _isUpdating = false;
            }
        }

        public bool ShowEmptyState => PriceLists is null || PriceLists.Count == 0;

        public bool HasRecords => !ShowEmptyState;

        public new bool IsInitialized { get; set; } = false;

        public async Task LoadPriceListItemsAsync()
        {
            try
            {
                if (ShowEmptyState) return;
                Stopwatch stopwatch = new();
                stopwatch.Start();

                var (fragment, query) = _loadPriceListItemsQuery.Value;

                dynamic filters = new ExpandoObject();
                if (SelectedCatalog != null && SelectedCatalog.Id != 0)
                    filters.catalogId = SelectedCatalog.Id;
                if (SelectedItemType != null && SelectedItemType.Id != 0)
                    filters.itemTypeId = SelectedItemType.Id;
                if (SelectedItemCategory != null && SelectedItemCategory.Id != 0)
                    filters.itemCategoryId = SelectedItemCategory.Id;
                if (SelectedItemSubCategory != null && SelectedItemSubCategory.Id != 0)
                    filters.itemSubCategoryId = SelectedItemSubCategory.Id;
                if (!string.IsNullOrEmpty(FilterSearch))
                    filters.filterSearch = FilterSearch.Trim().RemoveExtraSpaces();

                var variables = new GraphQLVariables()
                    .For(fragment, "priceListId", SelectedPriceList!.Id)
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<PriceListItemGraphQLModel> result = await _priceListItemService.GetPageAsync(query, variables);
                TotalCount = result.TotalEntries;
                PriceListItems = [.. Context.AutoMapper.Map<ObservableCollection<PriceListItemDTO>>(result.Entries)];

                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

                foreach (var item in PriceListItems)
                {
                    item.Context = this;
                    item.ResolveCost(SelectedPriceList);
                }
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }

        private async Task ReloadDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Execute.OnUIThreadAsync(async () =>
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    IsBusy = true;
                    await LoadPriceListItemsAsync();
                    IsBusy = false;
                });
            }
            catch (OperationCanceledException)
            {

            } 
        }

        private void LoadItemTypes()
        {
            if (SelectedCatalog?.ItemTypes is null) return;

            _isUpdating = true;
            ItemTypes = new ObservableCollection<ItemTypeGraphQLModel>(SelectedCatalog.ItemTypes);
            ItemTypes.Insert(0, new ItemTypeGraphQLModel { Id = 0, Name = "<< MOSTRAR TODOS LOS TIPOS DE PRODUCTOS >>" });
            SelectedItemType = ItemTypes.FirstOrDefault(x => x.Id == 0) ?? throw new Exception("SelectedItemType can't be null");
            LoadItemCategories();
            _isUpdating = false;
        }

        private void LoadItemCategories()
        {
            _isUpdating = true;
            
            if (SelectedItemType != null && SelectedItemType.Id != 0 && SelectedItemType.ItemCategories != null)
            {
                ItemCategories = new ObservableCollection<ItemCategoryGraphQLModel>(SelectedItemType.ItemCategories);
                ItemCategories.Insert(0, new ItemCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS CATEGORÍAS DE PRODUCTOS >>" });
                SelectedItemCategory = ItemCategories.FirstOrDefault(x => x.Id == 0) ?? throw new Exception("SelectedItemCategory can't be null");
            }
            else
            {
                // Reset categories when ItemType is "Show All" (Id = 0)
                ItemCategories.Clear();
                SelectedItemCategory = new ItemCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS CATEGORÍAS DE PRODUCTOS >>" };
            }
            
            LoadItemSubCategories();
            NotifyOfPropertyChange(nameof(CanShowItemCategories));
            NotifyOfPropertyChange(nameof(CanShowItemSubCategories));
            _isUpdating = false;
        }

        private void LoadItemSubCategories()
        {
            _isUpdating = true;
            
            if (SelectedItemCategory != null && SelectedItemCategory.Id != 0 && SelectedItemCategory.ItemSubCategories != null)
            {
                ItemSubCategories = new ObservableCollection<ItemSubCategoryGraphQLModel>(SelectedItemCategory.ItemSubCategories);
                ItemSubCategories.Insert(0, new ItemSubCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS SUBCATEGORÍAS DE PRODUCTOS >>" });
                SelectedItemSubCategory = ItemSubCategories.FirstOrDefault(x => x.Id == 0) ?? throw new Exception("SelectedItemSubCategory can't be null");
            }
            else
            {
                // Reset subcategories when Category is "Show All" (Id = 0) or ItemType is "Show All"
                ItemSubCategories.Clear();
                SelectedItemSubCategory = new ItemSubCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS SUBCATEGORÍAS DE PRODUCTOS >>" };
            }
            
            NotifyOfPropertyChange(nameof(CanShowItemSubCategories));
            _isUpdating = false;
        }


        // TEST: Batch price update
        public decimal BatchTestPrice { get; set; }
        public void ApplyBatchTestPrice()
        {
            if (PriceListItems == null || PriceListItems.Count == 0 || BatchTestPrice <= 0) return;
            foreach (var item in PriceListItems)
            {
                item.Price = BatchTestPrice;
            }
        }

        private ObservableCollection<PriceListItemDTO> ModifiedProduct { get; set; } = [];
        public async void AddModifiedProduct(PriceListItemDTO priceListDetail, string modifiedProperty)
        {
            try
            {
                if (SelectedPriceList is null) return;
                IPriceListCalculator calculator = _calculatorFactory.GetCalculator(SelectedPriceList.UseAlternativeFormula);
                calculator.RecalculateProductValues(priceListDetail, modifiedProperty, SelectedPriceList);
                priceListDetail.Status = OperationStatus.Pending;

                var operation = new PriceListUpdateOperation(_priceListItemService)
                {
                    ItemId = priceListDetail.Item.Id,
                    NewPrice = priceListDetail.Price,
                    NewDiscountMargin = priceListDetail.DiscountMargin,
                    NewMinimumPrice = priceListDetail.MinimumPrice,
                    NewProfitMargin = priceListDetail.ProfitMargin,
                    PriceListId = SelectedPriceList.Id,
                    ItemName = priceListDetail.Item.Name
                };

                _operationItemMapping[operation.OperationId] = priceListDetail.Item.Id;
                await _backgroundQueueService.EnqueueOperationAsync(operation);
            }
            catch (InvalidOperationException)
            {
                priceListDetail.Status = OperationStatus.Failed;
                _notificationService.ShowError(_backgroundQueueService.GetCriticalErrorMessage());
            }
            catch (Exception ex)
            {
                priceListDetail.Status = OperationStatus.Failed;
                _notificationService.ShowError($"Error inesperado al procesar \"{priceListDetail.Item.Name}\": {ex.Message}", durationMs: 8000);
            }
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                Context.EventAggregator.Unsubscribe(this);
                _cascadeCancellation?.Cancel();
                _cascadeCancellation?.Dispose();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }


        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.SetFocus(nameof(FilterSearch));
        }

        protected override async Task OnInitializedAsync(CancellationToken cancellationToken)
        {
            await Execute.OnUIThreadAsync(async () =>
            {
                try
                {
                    MainIsBusy = true;
                    await InitializeAsync();
                    await LoadPriceListItemsAsync();
                    MainIsBusy = false;
                }
                catch (AsyncException ex)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                }
            });

            await base.OnInitializedAsync(cancellationToken);
        }

        public Task HandleAsync(OperationCompletedMessage message, CancellationToken cancellationToken)
        {
            if (_operationItemMapping.TryGetValue(message.OperationId, out int itemId))
            {
                var item = PriceListItems.FirstOrDefault(i => i.Item.Id == itemId);
                if (item != null)
                {
                    if (message.Success)
                    {
                        item.Status = OperationStatus.Saved;
                        _operationItemMapping.Remove(message.OperationId);
                    }
                    else if (message.IsRetrying)
                    {
                        item.Status = OperationStatus.Retrying;
                        item.StatusTooltip = message.ErrorDetail;
                    }
                    else
                    {
                        item.Status = OperationStatus.Failed;
                        item.StatusTooltip = message.ErrorDetail ?? message.Exception?.Message;
                        _operationItemMapping.Remove(message.OperationId);
                        _notificationService.ShowError(
                            $"Error al guardar \"{item.Item.Name}\": {message.ErrorDetail ?? message.Exception?.Message}\n\nSi el problema persiste, comuníquese con soporte técnico.",
                            durationMs: 6000);
                    }
                }
            }

            return Task.CompletedTask;
        }

        public Task HandleAsync(CriticalSystemErrorMessage message, CancellationToken cancellationToken)
        {
            if (message.ResponseType == PriceListUpdateOperation.OperationResponseType)
            {
                _notificationService.ShowError(message.UserMessage);
                NotifyOfPropertyChange(nameof(IsGridReadOnly));
            }
            return Task.CompletedTask;
        }

        public PriceListMasterViewModel(
            PriceListViewModel context,
            IRepository<PriceListItemGraphQLModel> priceListItemService,
            IBackgroundQueueService backgroundQueueService,
            Helpers.Services.INotificationService notificationService,
            IPriceListCalculatorFactory calculatorFactory,
            Helpers.IDialogService dialogService,
            IRepository<PriceListGraphQLModel> priceListService,
            StorageCache storageCache,
            CostCenterCache costCenterCache,
            PaymentMethodCache paymentMethodCache,
            IGraphQLClient graphQLClient)
        {
            Context = context;
            _priceListItemService = priceListItemService;
            _backgroundQueueService = backgroundQueueService;
            _notificationService = notificationService;
            _calculatorFactory = calculatorFactory;
            _dialogService = dialogService;
            _priceListService = priceListService;
            _storageCache = storageCache;
            _costCenterCache = costCenterCache;
            _paymentMethodCache = paymentMethodCache;
            _graphQLClient = graphQLClient;
            Context.EventAggregator.SubscribeOnUIThread(this);
        }

        public Task HandleAsync(PriceListCreateMessage message, CancellationToken cancellationToken)
        {
            PriceLists.Add(message.CreatedPriceList.Entity);
            SelectedPriceList = message.CreatedPriceList.Entity;
            NotifyOfPropertyChange(nameof(ShowEmptyState));
                NotifyOfPropertyChange(nameof(HasRecords));
            _notificationService.ShowSuccess(message.CreatedPriceList.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(PriceListUpdateMessage message, CancellationToken cancellationToken)
        {
            var existing = PriceLists.FirstOrDefault(x => x.Id == message.UpdatedPriceList.Entity.Id);
            if (existing != null)
            {
                var index = PriceLists.IndexOf(existing);
                PriceLists[index] = message.UpdatedPriceList.Entity;
                SelectedPriceList = message.UpdatedPriceList.Entity;
            }
            NotifyOfPropertyChange(nameof(SelectedPriceListIsNotActive));
            _notificationService.ShowSuccess(message.UpdatedPriceList.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(PriceListDeleteMessage message, CancellationToken cancellationToken)
        {
            var toRemove = PriceLists.FirstOrDefault(x => x.Id == message.DeletedPriceList.DeletedId);
            if (toRemove != null) PriceLists.Remove(toRemove);
            SelectedPriceList = PriceLists.FirstOrDefault();
            NotifyOfPropertyChange(nameof(ShowEmptyState));
                NotifyOfPropertyChange(nameof(HasRecords));
            _notificationService.ShowSuccess(message.DeletedPriceList.Message);
            return Task.CompletedTask;
        }
        public Task HandleAsync(PriceListArchiveMessage message, CancellationToken cancellationToken)
        {
            var toRemove = PriceLists.FirstOrDefault(x => x.Id == message.ArchivedPriceList.Entity.Id);
            if (toRemove != null) PriceLists.Remove(toRemove);
            SelectedPriceList = PriceLists.FirstOrDefault();
            NotifyOfPropertyChange(nameof(ShowEmptyState));
                NotifyOfPropertyChange(nameof(HasRecords));
            _notificationService.ShowSuccess(message.ArchivedPriceList.Message);
            return Task.CompletedTask;
        }

        #region Paginacion


        private string _responseTime;
        public string ResponseTime
        {
            get { return _responseTime; }
            set
            {
                if (_responseTime != value)
                {
                    _responseTime = value;
                    NotifyOfPropertyChange(() => ResponseTime);
                }
            }
        }


        /// <summary>
        /// PageIndex
        /// </summary>
        private int _pageIndex = 1; // DefaultPageIndex = 1
        public int PageIndex
        {
            get { return _pageIndex; }
            set
            {
                if (_pageIndex != value)
                {
                    _pageIndex = value;
                    NotifyOfPropertyChange(() => PageIndex);
                }
            }
        }

        /// <summary>
        /// PageSize
        /// </summary>
        private int _pageSize = 50; // Default PageSize 50
        public int PageSize
        {
            get { return _pageSize; }
            set
            {
                if (_pageSize != value)
                {
                    _pageSize = value;
                    NotifyOfPropertyChange(() => PageSize);
                }
            }
        }

        /// <summary>
        /// TotalCount
        /// </summary>
        private int _totalCount = 0;
        public int TotalCount
        {
            get { return _totalCount; }
            set
            {
                if (_totalCount != value)
                {
                    _totalCount = value;
                    NotifyOfPropertyChange(() => TotalCount);
                }
            }
        }

        /// <summary>
        /// PaginationCommand para controlar evento
        /// </summary>
        private ICommand _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                if (_paginationCommand == null) this._paginationCommand = new AsyncCommand(ExecuteChangeIndexAsync, CanExecuteChangeIndex);
                return _paginationCommand;
            }
        }

        private async Task ExecuteChangeIndexAsync()
        {
            IsBusy = true;
            await LoadPriceListItemsAsync();
            IsBusy = false;
        }

        private bool CanExecuteChangeIndex()
        {
            return true;
        }

        #endregion

        #region Query Builders

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadPriceListItemsQuery = new(() =>
        {
            var fields = FieldSpec<PageType<PriceListItemGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.ProfitMargin)
                    .Field(e => e.Price)
                    .Field(e => e.MinimumPrice)
                    .Field(e => e.DiscountMargin)
                    .Field(e => e.Quantity)
                    .Select(e => e.Item, item => item
                        .Field(i => i.Id)
                        .Field(i => i.Name)
                        .Field(i => i.Reference)
                        .Select(i => i.MeasurementUnit, mu => mu
                            .Field(m => m.Id)
                            .Field(m => m.Abbreviation))
                        .Select(i => i.AccountingGroup, ag => ag
                            .Select(a => a.SalesPrimaryTax, t => t
                                .Field(tx => tx.Rate)
                                .Field(tx => tx.Formula)
                                .Field(tx => tx.AlternativeFormula)
                                .Select(tx => tx.TaxCategory, tc => tc
                                    .Field(c => c.Prefix)))
                            .Select(a => a.SalesSecondaryTax, t => t
                                .Field(tx => tx.Rate)
                                .Field(tx => tx.Formula)
                                .Field(tx => tx.AlternativeFormula)
                                .Select(tx => tx.TaxCategory, tc => tc
                                    .Field(c => c.Prefix))))
                        .SelectList(i => i.Stocks, stocks => stocks
                            .Select(s => s.Storage, st => st.Field(x => x.Id))
                            .Field(s => s.Cost)
                            .Field(s => s.AverageCost)
                            .Field(s => s.Quantity))))
                .Build();

            var filterParam = new GraphQLQueryParameter("filters", "PriceListItemCatalogFilters");
            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var priceListIdParam = new GraphQLQueryParameter("priceListId", "ID!");
            var fragment = new GraphQLQueryFragment("priceListItemCatalogPage", [filterParam, paginationParam, priceListIdParam], fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeletePriceListQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeletePriceList",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deletePriceListQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deletePriceList",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _archivePriceListQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<PriceListGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "priceList", nested: sq => sq
                    .Field(f => f.Id))
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

        private static readonly Lazy<(string Query, GraphQLQueryFragment CatalogsFragment, GraphQLQueryFragment PriceListsFragment)> _initializeQuery = new(() =>
        {
            var catalogsFields = FieldSpec<PageType<CatalogGraphQLModel>>
                .Create()
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .SelectList(e => e.ItemTypes, it => it
                        .Field(t => t.Id)
                        .Field(t => t.Name)
                        .SelectList(t => t.ItemCategories, ic => ic
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                            .SelectList(c => c.ItemSubCategories, isc => isc
                                .Field(s => s.Id)
                                .Field(s => s.Name)))))
                .Build();

            var catalogsPaginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var catalogsFragment = new GraphQLQueryFragment("catalogsPage", [catalogsPaginationParam], catalogsFields);

            var priceListsFields = FieldSpec<PageType<PriceListGraphQLModel>>
                .Create()
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.IsTaxable)
                    .Field(e => e.PriceListIncludeTax)
                    .Field(e => e.UseAlternativeFormula)
                    .Field(e => e.EditablePrice)
                    .Field(e => e.AutoApplyDiscount)
                    .Field(e => e.ListUpdateBehaviorOnCostChange)
                    .Field(e => e.IsPublic)
                    .Field(e => e.IsActive)
                    .Field(e => e.StartDate)
                    .Field(e => e.EndDate)
                    .Field(e => e.Archived)
                    .Select(e => e.Parent, p => p
                        .Field(pp => pp.Id)
                        .Field(pp => pp.Name))
                    .Select(e => e.Storage, s => s
                        .Field(ss => ss.Id)
                        .Field(ss => ss.Name))
                    .SelectList(e => e.ExcludedPaymentMethods, pm => pm
                        .Field(p => p.Id)
                        .Field(p => p.Name)
                        .Field(p => p.Abbreviation)))
                .Build();

            var priceListsFiltersParam = new GraphQLQueryParameter("filters", "PriceListFilters");
            var priceListsPaginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var priceListsFragment = new GraphQLQueryFragment("priceListsPage", [priceListsFiltersParam, priceListsPaginationParam], priceListsFields);

            var query = new GraphQLQueryBuilder([catalogsFragment, priceListsFragment]).GetQuery();
            return (query, catalogsFragment, priceListsFragment);
        });

        #endregion
    }

    public class PriceListUpdateOperation : IDataOperation
    {
        private readonly IRepository<PriceListItemGraphQLModel> _repository;

        public int ItemId { get; set; }
        public decimal NewPrice { get; set; }
        public decimal NewDiscountMargin { get; set; }
        public decimal NewProfitMargin { get; set; }
        public decimal NewMinimumPrice { get; set; }
        public int PriceListId { get; set; }
        public string ItemName { get; set; } = "";

        public PriceListUpdateOperation(IRepository<PriceListItemGraphQLModel> repository)
        {
            _repository = repository;
        }

        public object Variables => new
        {
            item = new
            {
                itemId = ItemId,
                price = NewPrice,
                discountMargin = NewDiscountMargin,
                profitMargin = NewProfitMargin,
                minimumPrice = NewMinimumPrice
            },
            priceListId = PriceListId
        };

        public static Type OperationResponseType => typeof(PriceListItemGraphQLModel);
        public Type ResponseType => OperationResponseType;
        public Guid OperationId { get; set; } = Guid.NewGuid();
        public string DisplayName => !string.IsNullOrEmpty(ItemName) ? ItemName : $"Producto #{ItemId}";
        public int Id => ItemId;

        public BatchOperationInfo GetBatchInfo()
        {
            return new BatchOperationInfo
            {
                BatchQuery = @"
                mutation ($input: BatchUpdatePriceListPricesInput!) {
                  SingleItemResponse: batchUpdatePriceListPrices(input: $input) {
                    success
                    message
                    totalAffected
                    affectedIds
                    errors { fields message }
                  }
                }",

                ExtractBatchItem = (variables) =>
                {
                    return variables.GetType().GetProperty("item")!.GetValue(variables)!;
                },

                BuildBatchVariables = (batchItems) =>
                {
                    return new
                    {
                        input = new
                        {
                            priceListId = PriceListId,
                            items = batchItems
                        }
                    };
                },

                ExecuteBatchAsync = async (query, variables, cancellationToken) =>
                {
                    return await _repository.BatchAsync<BatchResultGraphQLModel>(query, variables, cancellationToken);
                }
            };
        }
    }
}
