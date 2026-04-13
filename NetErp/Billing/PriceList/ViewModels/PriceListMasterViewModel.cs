using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using Common.Services;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
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
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Billing.PriceList.ViewModels
{
    public class PriceListMasterViewModel : Screen,
        IHandle<PriceListCreateMessage>,
        IHandle<PriceListUpdateMessage>,
        IHandle<PriceListDeleteMessage>,
        IHandle<OperationCompletedMessage>,
        IHandle<CriticalSystemErrorMessage>,
        IHandle<PriceListArchiveMessage>,
        IHandle<CatalogCreateMessage>,
        IHandle<CatalogDeleteMessage>,
        IHandle<CostCenterCreateMessage>,
        IHandle<CostCenterDeleteMessage>,
        IHandle<StorageCreateMessage>,
        IHandle<StorageDeleteMessage>
    {
        // Flag to prevent cascading reload operations during internal updates
        private bool _isUpdating = false;
        private readonly Dictionary<Guid, int> _operationItemMapping = [];
        private readonly DebouncedAction _searchDebounce = new();
        
        // Dependency injection fields
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<PriceListItemGraphQLModel> _priceListItemService;
        private readonly IBackgroundQueueService _backgroundQueueService;
        private readonly IPriceListCalculatorFactory _calculatorFactory;
        private readonly Helpers.IDialogService _dialogService;
        private readonly IRepository<PriceListGraphQLModel> _priceListService;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        //Caches necesarios en ventanas modales
        private readonly IGraphQLClient _graphQLClient;
        private readonly CatalogCache _catalogCache;
        private readonly StorageCache _storageCache;
        private readonly CostCenterCache _costCenterCache;
        private readonly PaymentMethodCache _paymentMethodCache;
        private readonly StringLengthCache _stringLengthCache;

        public PriceListViewModel Context { get; set; }
        public string MaskN2 { get; set; } = "n2";

        public string MaskN5 { get; set; } = "n5";

        public bool MainIsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(MainIsBusy));
                }
            }
        } = true;

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


        public string FilterSearch
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 2)
                    {
                        PageIndex = 1;
                        if (_isInitialized) _ = _searchDebounce.RunAsync(LoadPriceListItemsAsync);
                    }
                }
            }
        } = "";


        public ObservableCollection<PriceListItemDTO> PriceListItems
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PriceListItems));
                }
            }
        } = [];

        public ObservableCollection<CatalogGraphQLModel> Catalogs
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Catalogs));
                }
            }
        } = [];

        private CancellationTokenSource _cascadeCancellation = new();

        public CatalogGraphQLModel SelectedCatalog
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCatalog));
                    if (!_isUpdating && value != null)
                    {
                        _cascadeCancellation?.Cancel();
                        _cascadeCancellation?.Dispose();
                        _cascadeCancellation = new CancellationTokenSource();

                        LoadItemTypes();
                        if (_isInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
                    }
                }
            }
        }

        public ObservableCollection<ItemTypeGraphQLModel> ItemTypes
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ItemTypes));
                }
            }
        } = [];

        public ItemTypeGraphQLModel SelectedItemType
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedItemType));
                    if (!_isUpdating && value != null)
                    {
                        _cascadeCancellation?.Cancel();
                        _cascadeCancellation?.Dispose();
                        _cascadeCancellation = new CancellationTokenSource();

                        LoadItemCategories();
                        if (_isInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
                    }
                }
            }
        }

        public bool CanShowItemCategories => SelectedItemType != null && SelectedItemType.Id != 0;

        public ObservableCollection<ItemCategoryGraphQLModel> ItemCategories
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ItemCategories));
                }
            }
        } = [];

        public ItemCategoryGraphQLModel SelectedItemCategory
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedItemCategory));
                    if (!_isUpdating && value != null)
                    {
                        _cascadeCancellation?.Cancel();
                        _cascadeCancellation?.Dispose();
                        _cascadeCancellation = new CancellationTokenSource();

                        LoadItemSubCategories();
                        if (_isInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
                    }
                }
            }
        }

        public ObservableCollection<ItemSubCategoryGraphQLModel> ItemSubCategories
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ItemSubCategories));
                }
            }
        } = [];

        public ItemSubCategoryGraphQLModel SelectedItemSubCategory
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedItemSubCategory));
                    if (!_isUpdating && value != null && _isInitialized)
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

        public ObservableCollection<PriceListGraphQLModel> PriceLists
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PriceLists));
                }
            }
        } = [];

        public PriceListGraphQLModel? SelectedPriceList
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
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
                        if (_isInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
                    }
                }
            }
        }

        public bool SelectedPriceListIsNotActive => SelectedPriceList != null && !SelectedPriceList.IsActive;
        public bool IsGridReadOnly => SelectedPriceListIsNotActive || _backgroundQueueService.HasCriticalError();

        public PriceListItemDTO? SelectedPriceListItem
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
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

        public ICommand CreatePriceListCommand
        {
            get
            {
                if (field is null) field = new AsyncCommand(CreatePriceListAsync);
                return field;
            }
        }

        public async Task CreatePriceListAsync()
        {
            try
            {
                CreatePriceListModalViewModel viewModel = new(_dialogService, Context.EventAggregator, _priceListService, _storageCache, _costCenterCache, _stringLengthCache, _graphQLClient, _joinableTaskFactory);
                await viewModel.InitializeAsync();
                viewModel.SetForNew();
                await _dialogService.ShowDialogAsync(viewModel, "Creación de lista de precios");
            }
            catch (AsyncException ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{nameof(CreatePriceListAsync)} \r\n{ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
        }

        public ICommand ConfigurationCommand
        {
            get
            {
                if (field is null) field = new AsyncCommand(ConfigurationAsync);
                return field;
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
                UpdatePriceListModalViewModel viewModel = new(_dialogService, Context.EventAggregator, Context.AutoMapper, _priceListService, _storageCache, _costCenterCache, _paymentMethodCache, _stringLengthCache, _graphQLClient, _joinableTaskFactory);
                await viewModel.InitializeAsync();
                viewModel.SetForEdit(SelectedPriceList);
                await _dialogService.ShowDialogAsync(viewModel, "Configuración de lista de precios");
            }
            catch (AsyncException ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{nameof(ConfigurationAsync)} \r\n{ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
        }

        public ICommand DeletePriceListCommand
        {
            get
            {
                if (field is null) field = new AsyncCommand(DeletePriceListAsync);
                return field;
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
                ExpandoObject canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", id)
                    .Build();
                CanDeleteType validation = await _priceListService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

                if (validation.CanDelete)
                {
                    var (deleteFragment, deleteQuery) = _deletePriceListQuery.Value;
                    ExpandoObject deleteVars = new GraphQLVariables()
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
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{nameof(DeletePriceListAsync)} \r\n{ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public ICommand CreatePromotionCommand
        {
            get
            {
                if (field is null) field = new AsyncCommand(CreatePromotionAsync);
                return field;
            }
        }

        public async Task CreatePromotionAsync()
        {
            if (SelectedPriceList is null) return;
            try
            {
                CreatePromotionModalViewModel viewModel = new(_dialogService, Context.EventAggregator, SelectedPriceList, _priceListService, _stringLengthCache, _joinableTaskFactory);
                viewModel.SetForNew();
                await _dialogService.ShowDialogAsync(viewModel, "Creación de promociones");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(CreatePromotionAsync)}: {ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
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


                Catalogs = [.. result.CatalogsPage.Entries];
                SelectedCatalog = Catalogs.FirstOrDefault() ?? throw new Exception("SelectedCatalog can't be null");
                PriceLists = [.. result.PriceListsPage.Entries];
                _isInitialized = true;
                ShowEmptyState = PriceLists == null || PriceLists.Count == 0;
                NotifyOfPropertyChange(nameof(HasRecords));
                if (ShowEmptyState) return;
                SelectedPriceList = PriceLists?.FirstOrDefault() ?? throw new Exception("SelectedPriceList can't be null");
                LoadItemTypes();
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private bool _isInitialized;

        public bool ShowEmptyState
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ShowEmptyState));
                    NotifyOfPropertyChange(nameof(CanShowEmptyState));
                    NotifyOfPropertyChange(nameof(HasRecords));
                }
            }
        }

        public bool CanShowEmptyState => ShowEmptyState && !HasUnmetDependencies;

        public bool HasRecords => _isInitialized && !ShowEmptyState && !HasUnmetDependencies;

        private List<DependencyItem>? _dependencies;
        public List<DependencyItem>? Dependencies
        {
            get => _dependencies;
            private set
            {
                _dependencies = value;
                NotifyOfPropertyChange(nameof(Dependencies));
                NotifyOfPropertyChange(nameof(HasUnmetDependencies));
                NotifyOfPropertyChange(nameof(CanShowEmptyState));
                NotifyOfPropertyChange(nameof(HasRecords));
            }
        }

        public bool HasUnmetDependencies => Dependencies?.Any(d => !d.IsMet) == true;

        public async Task LoadPriceListItemsAsync()
        {
            try
            {
                if (ShowEmptyState) return;
                Stopwatch stopwatch = Stopwatch.StartNew();

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

                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "priceListId", SelectedPriceList!.Id)
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<PriceListItemGraphQLModel> result = await _priceListItemService.GetPageAsync(query, variables);
                TotalCount = result.TotalEntries;
                PriceListItems = [.. Context.AutoMapper.Map<ObservableCollection<PriceListItemDTO>>(result.Entries)];

                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

                foreach (PriceListItemDTO item in PriceListItems)
                {
                    item.Context = this;
                    item.ResolveCost(SelectedPriceList);
                }
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{nameof(LoadPriceListItemsAsync)} \r\n{ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
        }

        private async Task ReloadDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                if (cancellationToken.IsCancellationRequested) return;

                IsBusy = true;
                await LoadPriceListItemsAsync();
                IsBusy = false;
            }
            catch (OperationCanceledException)
            {

            }
        }

        private void LoadItemTypes()
        {
            if (SelectedCatalog?.ItemTypes is null) return;

            _isUpdating = true;
            ItemTypes = [.. SelectedCatalog.ItemTypes];
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
                ItemCategories = [.. SelectedItemType.ItemCategories];
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
                ItemSubCategories = [.. SelectedItemCategory.ItemSubCategories];
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
            foreach (PriceListItemDTO item in PriceListItems)
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
                PriceListItems.Clear();
                PriceLists.Clear();
                Catalogs.Clear();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            if (!HasUnmetDependencies)
                this.SetFocus(nameof(FilterSearch));
        }

        protected override async Task OnInitializedAsync(CancellationToken cancellationToken)
        {
            try
            {
                MainIsBusy = true;
                await CacheBatchLoader.LoadAsync(
                    _graphQLClient, cancellationToken,
                    _catalogCache, _storageCache, _costCenterCache, _paymentMethodCache);

                EvaluateDependencies();
                if (HasUnmetDependencies)
                {
                    _isInitialized = true;
                    NotifyOfPropertyChange(nameof(HasRecords));
                    return;
                }

                await PerformInitialLoadAsync();
                MainIsBusy = false;
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(OnInitializedAsync)} \r\n{ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
            finally
            {
                MainIsBusy = false;
            }

            await base.OnInitializedAsync(cancellationToken);
        }

        public Task HandleAsync(OperationCompletedMessage message, CancellationToken cancellationToken)
        {
            if (_operationItemMapping.TryGetValue(message.OperationId, out int itemId))
            {
                PriceListItemDTO? item = PriceListItems.FirstOrDefault(i => i.Item.Id == itemId);
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
            CatalogCache catalogCache,
            StorageCache storageCache,
            CostCenterCache costCenterCache,
            PaymentMethodCache paymentMethodCache,
            StringLengthCache stringLengthCache,
            IGraphQLClient graphQLClient,
            JoinableTaskFactory joinableTaskFactory)
        {
            Context = context;
            _priceListItemService = priceListItemService;
            _backgroundQueueService = backgroundQueueService;
            _notificationService = notificationService;
            _calculatorFactory = calculatorFactory;
            _dialogService = dialogService;
            _priceListService = priceListService;
            _catalogCache = catalogCache;
            _storageCache = storageCache;
            _costCenterCache = costCenterCache;
            _paymentMethodCache = paymentMethodCache;
            _stringLengthCache = stringLengthCache;
            _graphQLClient = graphQLClient;
            _joinableTaskFactory = joinableTaskFactory;
            Context.EventAggregator.SubscribeOnUIThread(this);
        }

        public Task HandleAsync(PriceListCreateMessage message, CancellationToken cancellationToken)
        {
            PriceLists.Add(message.CreatedPriceList.Entity);
            SelectedPriceList = message.CreatedPriceList.Entity;
            ShowEmptyState = false;
            _notificationService.ShowSuccess(message.CreatedPriceList.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(PriceListUpdateMessage message, CancellationToken cancellationToken)
        {
            PriceListGraphQLModel? existing = PriceLists.FirstOrDefault(x => x.Id == message.UpdatedPriceList.Entity.Id);
            if (existing != null)
            {
                int index = PriceLists.IndexOf(existing);
                PriceLists[index] = message.UpdatedPriceList.Entity;
                SelectedPriceList = message.UpdatedPriceList.Entity;
            }
            NotifyOfPropertyChange(nameof(SelectedPriceListIsNotActive));
            _notificationService.ShowSuccess(message.UpdatedPriceList.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(PriceListDeleteMessage message, CancellationToken cancellationToken)
        {
            PriceListGraphQLModel? toRemove = PriceLists.FirstOrDefault(x => x.Id == message.DeletedPriceList.DeletedId);
            if (toRemove != null) PriceLists.Remove(toRemove);
            SelectedPriceList = PriceLists.FirstOrDefault();
            ShowEmptyState = PriceLists == null || PriceLists.Count == 0;
            _notificationService.ShowSuccess(message.DeletedPriceList.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(PriceListArchiveMessage message, CancellationToken cancellationToken)
        {
            PriceListGraphQLModel? toRemove = PriceLists.FirstOrDefault(x => x.Id == message.ArchivedPriceList.Entity.Id);
            if (toRemove != null) PriceLists.Remove(toRemove);
            SelectedPriceList = PriceLists.FirstOrDefault();
            ShowEmptyState = PriceLists == null || PriceLists.Count == 0;
            _notificationService.ShowSuccess(message.ArchivedPriceList.Message);
            return Task.CompletedTask;
        }

        #region Dependencies

        private void EvaluateDependencies()
        {
            Dependencies =
            [
                DependencyDefinitions.Catalogs(_catalogCache),
                DependencyDefinitions.CostCenters(_costCenterCache),
                DependencyDefinitions.Storages(_storageCache),
            ];
        }

        private async Task PerformInitialLoadAsync()
        {
            await InitializeAsync();
            await LoadPriceListItemsAsync();
        }

        public async Task HandleAsync(CatalogCreateMessage message, CancellationToken cancellationToken)
        {
            if (!HasUnmetDependencies) return;

#pragma warning disable VSTHRD001
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                () => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
#pragma warning restore VSTHRD001

            EvaluateDependencies();
            if (!HasUnmetDependencies)
                await PerformInitialLoadAsync();
        }

        public async Task HandleAsync(CatalogDeleteMessage message, CancellationToken cancellationToken)
        {
            if (HasUnmetDependencies) return;

#pragma warning disable VSTHRD001
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                () => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
#pragma warning restore VSTHRD001

            EvaluateDependencies();
        }

        public async Task HandleAsync(CostCenterCreateMessage message, CancellationToken cancellationToken)
        {
            if (!HasUnmetDependencies) return;

#pragma warning disable VSTHRD001
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                () => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
#pragma warning restore VSTHRD001

            EvaluateDependencies();
            if (!HasUnmetDependencies)
                await PerformInitialLoadAsync();
        }

        public async Task HandleAsync(CostCenterDeleteMessage message, CancellationToken cancellationToken)
        {
            if (HasUnmetDependencies) return;

#pragma warning disable VSTHRD001
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                () => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
#pragma warning restore VSTHRD001

            EvaluateDependencies();
        }

        public async Task HandleAsync(StorageCreateMessage message, CancellationToken cancellationToken)
        {
            if (!HasUnmetDependencies) return;

#pragma warning disable VSTHRD001
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                () => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
#pragma warning restore VSTHRD001

            EvaluateDependencies();
            if (!HasUnmetDependencies)
                await PerformInitialLoadAsync();
        }

        public async Task HandleAsync(StorageDeleteMessage message, CancellationToken cancellationToken)
        {
            if (HasUnmetDependencies) return;

#pragma warning disable VSTHRD001
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                () => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
#pragma warning restore VSTHRD001

            EvaluateDependencies();
        }

        #endregion

        #region Paginacion


        public string ResponseTime
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ResponseTime));
                }
            }
        }


        /// <summary>
        /// PageIndex
        /// </summary>
        public int PageIndex
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PageIndex));
                }
            }
        } = 1; // DefaultPageIndex = 1

        /// <summary>
        /// PageSize
        /// </summary>
        public int PageSize
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PageSize));
                }
            }
        } = 50; // Default PageSize 50

        /// <summary>
        /// TotalCount
        /// </summary>
        public int TotalCount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(TotalCount));
                }
            }
        }

        /// <summary>
        /// PaginationCommand para controlar evento
        /// </summary>
        public ICommand PaginationCommand
        {
            get
            {
                if (field == null) field = new AsyncCommand(ExecuteChangeIndexAsync, CanExecuteChangeIndex);
                return field;
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
                        .Field(pp => pp!.Id)
                        .Field(pp => pp!.Name))
                    .Select(e => e.Storage, s => s
                        .Field(ss => ss!.Id)
                        .Field(ss => ss!.Name))
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
