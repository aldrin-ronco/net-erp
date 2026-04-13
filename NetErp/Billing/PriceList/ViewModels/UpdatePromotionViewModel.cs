using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using Models.Global;
using Models.Inventory;
using NetErp.Billing.PriceList.DTO;
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Billing.PriceList.ViewModels
{
    public class UpdatePromotionViewModel: Screen,
                IHandle<PriceListUpdateMessage>,
                IHandle<PromotionTempRecordResponseMessage>,
                IHandle<CriticalSystemErrorMessage>
    {

        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<PriceListItemGraphQLModel> _priceListItemService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly IRepository<ItemGraphQLModel> _itemService;
        private readonly IRepository<TempRecordGraphQLModel> _tempRecordService;
        private readonly IRepository<PriceListGraphQLModel> _priceListServiceForModal;
        private readonly StringLengthCache _stringLengthCache;
        private readonly DebouncedAction _searchDebounce;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        public PriceListViewModel Context { get; set; }

        // Referencia al modal activo para poder cerrarlo en caso de error crítico
        private AddPromotionProductsModalViewModel _activeModal;

        public UpdatePromotionViewModel(
            PriceListViewModel context,
            Helpers.Services.INotificationService notificationService,
            IRepository<PriceListItemGraphQLModel> priceListItemService,
            Helpers.IDialogService dialogService,
            IRepository<ItemGraphQLModel> itemService,
            IRepository<TempRecordGraphQLModel> tempRecordService,
            IRepository<PriceListGraphQLModel> priceListServiceForModal,
            StringLengthCache stringLengthCache,
            DebouncedAction searchDebounce,
            JoinableTaskFactory joinableTaskFactory)
        {
            Context = context;
            _notificationService = notificationService;
            _priceListItemService = priceListItemService;
            _dialogService = dialogService;
            _itemService = itemService;
            _tempRecordService = tempRecordService;
            _priceListServiceForModal = priceListServiceForModal;
            _stringLengthCache = stringLengthCache;
            _searchDebounce = searchDebounce ?? throw new ArgumentNullException(nameof(searchDebounce));
            _joinableTaskFactory = joinableTaskFactory;
            Context.EventAggregator.SubscribeOnUIThread(this);
        }

        public Task HandleAsync(PriceListUpdateMessage message, CancellationToken cancellationToken)
        {
            Name = message.UpdatedPriceList.Entity.Name;
            IsPromotionActive = message.UpdatedPriceList.Entity.IsActive;
            StartDate = message.UpdatedPriceList.Entity.StartDate;
            EndDate = message.UpdatedPriceList.Entity.EndDate;
            _notificationService.ShowSuccess(message.UpdatedPriceList.Message);
            return Task.CompletedTask;
        }

        public ICommand GoBackCommand
        {
            get
            {
                field ??= new AsyncCommand(GoBackAsync);
                return field;
            }
        }

        public string Name
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Name));
                }
            }
        }

        public DateTime? StartDate
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(StartDate));
                }
            }
        }

        public DateTime? EndDate
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(EndDate));
                }
            }
        }

        public int Id { get; set; }

        public bool IsPromotionActive
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsPromotionActive));
                    NotifyOfPropertyChange(nameof(IsPromotionNotActive));
                }
            }
        }

        public bool IsPromotionNotActive => !IsPromotionActive;

        public async Task GoBackAsync()
        {
            await Context.ActivateMasterViewAsync();
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

        private CatalogGraphQLModel _selectedCatalog = new();

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
                        
                        BuildItemTypes();
                        if (IsInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
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

        private ItemTypeGraphQLModel _selectedItemType = new();

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
                        
                        BuildItemCategories();
                        if (IsInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
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

        private ItemCategoryGraphQLModel _selectedItemCategory = new();

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
                        
                        BuildItemSubCategories();
                        if (IsInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
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

        private ItemSubCategoryGraphQLModel _selectedItemSubCategory = new();

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



        // Flag to prevent cascading reload operations during internal updates
        private bool _isUpdating = false;
        private CancellationTokenSource _cascadeCancellation = new();

        // Obsolete method - replaced by individual dropdown logic
        // Keeping for backward compatibility during transition

        // Reload data method with cancellation support
        private async Task ReloadDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                if (cancellationToken.IsCancellationRequested) return;

                IsBusy = true;
                await LoadProducts();
                IsBusy = false;
            }
            catch (OperationCanceledException)
            {
                // Operation cancelled, do nothing
            }
        }

        // Métodos de construcción de listas
        private void BuildItemTypes()
        {
            if (SelectedCatalog?.ItemTypes == null) return;

            _isUpdating = true;
            ItemTypes = [.. SelectedCatalog.ItemTypes];
            ItemTypes.Insert(0, new ItemTypeGraphQLModel { Id = 0, Name = "<< MOSTRAR TODOS LOS TIPOS DE PRODUCTOS >>" });
            _selectedItemType = ItemTypes.First(x => x.Id == 0);
            NotifyOfPropertyChange(nameof(SelectedItemType));
            BuildItemCategories();
            _isUpdating = false;
        }

        private void BuildItemCategories()
        {
            _isUpdating = true;
            
            if (SelectedItemType != null && SelectedItemType.Id != 0 && SelectedItemType.ItemCategories != null)
            {
                ItemCategories = [.. SelectedItemType.ItemCategories];
                ItemCategories.Insert(0, new ItemCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS CATEGORÍAS DE PRODUCTOS >>" });
                _selectedItemCategory = ItemCategories.First(x => x.Id == 0);
                NotifyOfPropertyChange(nameof(SelectedItemCategory));
            }
            else
            {
                // Reset categories when ItemType is "Show All" (Id = 0)
                ItemCategories.Clear();
                _selectedItemCategory = new ItemCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS CATEGORÍAS DE PRODUCTOS >>" };
                NotifyOfPropertyChange(nameof(SelectedItemCategory));
            }

            BuildItemSubCategories();
            NotifyOfPropertyChange(nameof(CanShowItemCategories));
            NotifyOfPropertyChange(nameof(CanShowItemSubCategories));
            _isUpdating = false;
        }

        private void BuildItemSubCategories()
        {
            _isUpdating = true;
            
            if (SelectedItemCategory != null && SelectedItemCategory.Id != 0 && SelectedItemCategory.ItemSubCategories != null)
            {
                ItemSubCategories = [.. SelectedItemCategory.ItemSubCategories];
                ItemSubCategories.Insert(0, new ItemSubCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS SUBCATEGORÍAS DE PRODUCTOS >>" });
                _selectedItemSubCategory = ItemSubCategories.First(x => x.Id == 0);
                NotifyOfPropertyChange(nameof(SelectedItemSubCategory));
            }
            else
            {
                // Reset subcategories when Category is "Show All" (Id = 0) or ItemType is "Show All"
                ItemSubCategories.Clear();
                _selectedItemSubCategory = new ItemSubCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS SUBCATEGORÍAS DE PRODUCTOS >>" };
                NotifyOfPropertyChange(nameof(SelectedItemSubCategory));
            }

            NotifyOfPropertyChange(nameof(CanShowItemSubCategories));
            _isUpdating = false;
        }

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
                        if (IsInitialized) _ = _searchDebounce.RunAsync(LoadProducts);
                    }
                }
            }
        } = string.Empty;

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
        }

        public bool HasCriticalError
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(HasCriticalError));
                    NotifyOfPropertyChange(nameof(CanPerformDataOperations));
                    NotifyOfPropertyChange(nameof(CanDelete));
                    NotifyOfPropertyChange(nameof(CanClearPromotion));
                }
            }
        }

        // Propiedad computed que determina si se pueden realizar operaciones de datos
        public bool CanPerformDataOperations => !HasCriticalError;


        public ICommand EditCommand
        {
            get
            {
                field ??= new AsyncCommand(EditAsync);
                return field;
            }
        }

        public async Task EditAsync()
        {
            try
            {
                UpdatePromotionModalViewModel instance = new(_dialogService, Context.EventAggregator, _priceListServiceForModal, _stringLengthCache, _joinableTaskFactory);
                instance.SetForEdit(new PriceListGraphQLModel
                {
                    Id = Id,
                    Name = Name,
                    IsActive = IsPromotionActive,
                    StartDate = StartDate,
                    EndDate = EndDate
                });
                await _dialogService.ShowDialogAsync(instance, "Editar promoción");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{nameof(EditAsync)} \r\n{ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
        }

        public ICommand AddCommand
        {
            get
            {
                field ??= new AsyncCommand(AddProductsAsync);
                return field;
            }
        }

        public async Task AddProductsAsync()
        {
            try
            {
                _activeModal = new(Context, _dialogService, _priceListItemService, _itemService, _joinableTaskFactory);
                MainIsBusy = true;
                _activeModal.PromotionId = Id;
                await _activeModal.InitializeAsync();
                await _dialogService.ShowDialogAsync(_activeModal, "Agregar productos a la promoción");
                MainIsBusy = false;
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(AddProductsAsync)} \r\n{ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
        }

        public async Task LoadProducts()
        {
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadPromotionItemsQuery.Value;

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
                    .For(fragment, "priceListId", Id)
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
                    item.UpdatePromotionContext = this;
                }
                VerifyItemsInShadowList();
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{nameof(LoadProducts)} \r\n{ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
        }

        public async Task InitializeAsync()
        {
            var (query, catalogsFragment) = _catalogsQuery.Value;

            dynamic variables = new GraphQLVariables()
                .For(catalogsFragment, "pagination", new { pageSize = -1 })
                .Build();

            PriceListDataContext result = await _priceListItemService.GetDataContextAsync<PriceListDataContext>(query, variables);
            Catalogs = [.. result.CatalogsPage.Entries];
            CatalogGraphQLModel selectedCatalog = Catalogs.FirstOrDefault() ?? throw new Exception("SelectedCatalog can't be null");
            IsInitialized = true;
            _isUpdating = true;
            _selectedCatalog = selectedCatalog;
            NotifyOfPropertyChange(nameof(SelectedCatalog));
            BuildItemTypes();
            _isUpdating = false;
            await ReloadDataAsync(_cascadeCancellation.Token);
        }

        public new bool IsInitialized { get; set; } = false;


        public List<int> ShadowItems
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ShadowItems));
                }
            }
        } = [];


        public bool CanDelete => ShadowItems != null && ShadowItems.Count > 0 && CanPerformDataOperations;

        public bool CanClearPromotion => PriceListItems != null && PriceListItems.Count > 0 && CanPerformDataOperations;

        private bool _itemsHeaderIsChecked;

        public bool ItemsHeaderIsChecked
        {
            get
            {
                if (PriceListItems is null || PriceListItems.Count == 0) return false;
                return _itemsHeaderIsChecked;
            }
            set
            {
                if (_itemsHeaderIsChecked != value)
                {
                    _itemsHeaderIsChecked = value;
                    NotifyOfPropertyChange(nameof(ItemsHeaderIsChecked));
                    foreach (PriceListItemDTO item in PriceListItems)
                    {
                        item.IsChecked = value;
                    }
                }
            }
        }


        public void AddItemsToShadowList(int itemId)
        {
            if (!ShadowItems.Contains(itemId))
            {
                ShadowItems.Add(itemId);
            }
            NotifyOfPropertyChange(nameof(CanDelete));
            NotifyOfPropertyChange(nameof(CanClearPromotion));
        }

        public void RemoveItemsFromShadowList(int itemId)
        {
            ShadowItems.Remove(itemId);
            NotifyOfPropertyChange(nameof(CanDelete));
            NotifyOfPropertyChange(nameof(CanClearPromotion));
        }

        public void VerifyItemsInShadowList()
        {
            foreach (PriceListItemDTO item in PriceListItems)
            {
                if (ShadowItems.Contains(item.Item.Id))
                {
                    item.IsChecked = true;
                }
            }
        }

        public ICommand DeleteListCommand
        {
            get
            {
                field ??= new AsyncCommand(DeleteListAsync);
                return field;
            }
        }

        private const int DeleteBatchSize = 50;

        private static readonly Lazy<string> _batchDeleteQuery = new(() => @"
            mutation ($input: BatchDeletePriceListPricesInput!) {
              SingleItemResponse: batchDeletePriceListPrices(input: $input) {
                success
                message
                totalAffected
                affectedIds
                errors { fields message }
              }
            }");

        public async Task DeleteListAsync()
        {
            try
            {
                MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar los registros seleccionados?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;

                if (ShadowItems.Count == 0) return;

                IsBusy = true;
                List<int> itemIds = ShadowItems.ToList();
                string query = _batchDeleteQuery.Value;
                int totalDeleted = 0;
                List<string> failedMessages = [];

                for (int i = 0; i < itemIds.Count; i += DeleteBatchSize)
                {
                    List<int> batch = itemIds.Skip(i).Take(DeleteBatchSize).ToList();
                    var variables = new { input = new { priceListId = Id, itemIds = batch } };

                    BatchResultGraphQLModel batchResult = await _priceListItemService.BatchAsync<BatchResultGraphQLModel>(query, variables);

                    if (batchResult.Success)
                    {
                        foreach (int itemId in batch)
                        {
                            PriceListItemDTO? item = PriceListItems.FirstOrDefault(x => x.Item.Id == itemId);
                            if (item != null) PriceListItems.Remove(item);
                            ShadowItems.Remove(itemId);
                        }
                        totalDeleted += batch.Count;
                    }
                    else
                    {
                        failedMessages.Add(batchResult.Message ?? "Error desconocido");
                    }
                }

                _itemsHeaderIsChecked = false;
                NotifyOfPropertyChange(nameof(ItemsHeaderIsChecked));
                NotifyOfPropertyChange(nameof(CanDelete));
                NotifyOfPropertyChange(nameof(CanClearPromotion));

                if (failedMessages.Count > 0)
                {
                    _notificationService.ShowWarning(
                        $"Eliminados: {totalDeleted}. Con error: {itemIds.Count - totalDeleted}.\n{string.Join("\n", failedMessages)}");
                }
                else
                {
                    _notificationService.ShowSuccess($"{totalDeleted} productos eliminados correctamente");
                }
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{nameof(DeleteListAsync)} \r\n{ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public ICommand ClearPromotionCommand
        {
            get
            {
                field ??= new AsyncCommand(ClearPromotionAsync);
                return field;
            }
        }

        private static readonly Lazy<string> _purgeQuery = new(() => @"
            mutation ($priceListId: ID!) {
              SingleItemResponse: purgePriceListPrices(priceListId: $priceListId) {
                success
                message
                totalAffected
                affectedIds
                errors { fields message }
              }
            }");

        public async Task ClearPromotionAsync()
        {
            try
            {
                MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar TODOS los registros?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;

                IsBusy = true;
                var variables = new { priceListId = Id };
                BatchResultGraphQLModel batchResult = await _priceListItemService.BatchAsync<BatchResultGraphQLModel>(_purgeQuery.Value, variables);

                if (!batchResult.Success)
                {
                    _notificationService.ShowError(batchResult.Message ?? "Error al limpiar la promoción");
                    return;
                }

                PriceListItems.Clear();
                ShadowItems.Clear();
                _itemsHeaderIsChecked = false;
                NotifyOfPropertyChange(nameof(ItemsHeaderIsChecked));
                NotifyOfPropertyChange(nameof(CanDelete));
                NotifyOfPropertyChange(nameof(CanClearPromotion));
                _notificationService.ShowSuccess("Promoción limpiada correctamente");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{nameof(ClearPromotionAsync)} \r\n{ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }

        }
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.SetFocus(nameof(FilterSearch));
        }

        public async Task HandleAsync(PromotionTempRecordResponseMessage message, CancellationToken cancellationToken)
        {
            IsBusy = true;
            ShadowItems.Clear();
            await LoadProducts();
            _itemsHeaderIsChecked = false;
            NotifyOfPropertyChange(nameof(ItemsHeaderIsChecked));
            NotifyOfPropertyChange(nameof(CanDelete));
            NotifyOfPropertyChange(nameof(CanClearPromotion));
            IsBusy = false;
            _notificationService.ShowSuccess(message.Response.Message);
        }

        public Task HandleAsync(CriticalSystemErrorMessage message, CancellationToken cancellationToken)
        {
            if (message.ResponseType == typeof(PriceListItemGraphQLModel) || message.ResponseType == typeof(TempRecordGraphQLModel))
            {
                IsBusy = false;
                HasCriticalError = true;
                _notificationService.ShowError(message.UserMessage, durationMs: 10000);
            }
            return Task.CompletedTask;
        }

        public string ResponseTime
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(() => ResponseTime);
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
                    NotifyOfPropertyChange(() => PageIndex);
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
                    NotifyOfPropertyChange(() => PageSize);
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
                    NotifyOfPropertyChange(() => TotalCount);
                }
            }
        } = 0;

        public ICommand PaginationCommand
        {
            get
            {
                field ??= new AsyncCommand(ExecuteChangeIndexAsync);
                return field;
            }
        }

        private async Task ExecuteChangeIndexAsync()
        {
            await LoadProducts();
        }

        #region Query Builders

        private static readonly Lazy<(string Query, GraphQLQueryFragment CatalogsFragment)> _catalogsQuery = new(() =>
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

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var catalogsFragment = new GraphQLQueryFragment("catalogsPage", [paginationParam], catalogsFields);

            var query = new GraphQLQueryBuilder([catalogsFragment]).GetQuery();
            return (query, catalogsFragment);
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadPromotionItemsQuery = new(() =>
        {
            var fields = FieldSpec<PageType<PriceListItemGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Select(e => e.Item, item => item
                        .Field(i => i.Id)
                        .Field(i => i.Name)
                        .Field(i => i.Code)
                        .Field(i => i.Reference)))
                .Build();

            var filterParam = new GraphQLQueryParameter("filters", "PriceListItemCatalogFilters");
            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var priceListIdParam = new GraphQLQueryParameter("priceListId", "ID!");
            var fragment = new GraphQLQueryFragment("priceListItemCatalogPage", [filterParam, paginationParam, priceListIdParam], fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion
    }
}
