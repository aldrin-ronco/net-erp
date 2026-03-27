using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.Global;
using Models.Inventory;
using NetErp.Billing.PriceList.DTO;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Helpers.Messages;
using NetErp.Helpers.Services;
using Services.Billing.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
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
            IRepository<PriceListGraphQLModel> priceListServiceForModal)
        {
            Context = context;
            _notificationService = notificationService;
            _priceListItemService = priceListItemService;
            _dialogService = dialogService;
            _itemService = itemService;
            _tempRecordService = tempRecordService;
            _priceListServiceForModal = priceListServiceForModal;
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

        private ICommand _goBackCommand;
        public ICommand GoBackCommand
        {
            get
            {
                if (_goBackCommand is null) _goBackCommand = new AsyncCommand(GoBackAsync);
                return _goBackCommand;
            }
        }

        private string _name;

        public string Name
        {
            get { return _name; }
            set
            {
                if(_name != value)
                {
                    _name = value;
                    NotifyOfPropertyChange(nameof(Name));
                }
            }
        }

        private DateTime? _startDate;

        public DateTime? StartDate
        {
            get { return _startDate; }
            set 
            {
                if (_startDate != value)
                {
                    _startDate = value; 
                    NotifyOfPropertyChange(nameof(StartDate));
                }
            }
        }

        private DateTime? _endDate;

        public DateTime? EndDate
        {
            get { return _endDate; }
            set 
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    NotifyOfPropertyChange(nameof(EndDate));
                }
            }
        }

        public int Id { get; set; }

        private bool _isPromotionActive;
        public bool IsPromotionActive
        {
            get => _isPromotionActive;
            set
            {
                if (_isPromotionActive != value)
                {
                    _isPromotionActive = value;
                    NotifyOfPropertyChange(nameof(IsPromotionActive));
                    NotifyOfPropertyChange(nameof(IsPromotionNotActive));
                }
            }
        }

        public bool IsPromotionNotActive => !IsPromotionActive;

        public async Task GoBackAsync()
        {
            try
            {
                await Context.ActivateMasterViewAsync();
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
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

        private ObservableCollection<ItemTypeGraphQLModel> _itemTypes = [];

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

        private ObservableCollection<ItemCategoryGraphQLModel> _itemsCategories = [];

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

        private ObservableCollection<ItemSubCategoryGraphQLModel> _itemsSubCategories = [];

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
                await Execute.OnUIThreadAsync(async () =>
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    IsBusy = true;
                    await LoadProducts();
                    IsBusy = false;
                });
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
                ItemCategories = new ObservableCollection<ItemCategoryGraphQLModel>(SelectedItemType.ItemCategories);
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
                    // Solo ejecutamos la busqueda si esta vacio el filtro o si hay por lo menos 2 caracteres digitados
                    if (string.IsNullOrEmpty(value) || value.Length >= 2)
                    {
                        PageIndex = 1;
                        if (IsInitialized) _ = LoadProducts();
                    }
                }
            }
        }

        private bool _mainIsBusy;
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

        private bool _hasCriticalError = false;
        public bool HasCriticalError
        {
            get { return _hasCriticalError; }
            set
            {
                if (_hasCriticalError != value)
                {
                    _hasCriticalError = value;
                    NotifyOfPropertyChange(nameof(HasCriticalError));
                    NotifyOfPropertyChange(nameof(CanPerformDataOperations));
                    NotifyOfPropertyChange(nameof(CanDelete));
                    NotifyOfPropertyChange(nameof(CanClearPromotion));
                }
            }
        }

        // Propiedad computed que determina si se pueden realizar operaciones de datos
        public bool CanPerformDataOperations => !HasCriticalError;


        private ICommand _editCommand;
        public ICommand EditCommand
        {
            get
            {
                if (_editCommand is null) _editCommand = new AsyncCommand(EditAsync);
                return _editCommand;
            }
        }

        public async Task EditAsync()
        {
            try
            {
                UpdatePromotionModalViewModel instance = new(_dialogService, Context.EventAggregator, _priceListServiceForModal);
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
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }

        private ICommand _addCommand;
        public ICommand AddCommand
        {
            get
            {
                if (_addCommand is null) _addCommand = new AsyncCommand(AddProductsAsync);
                return _addCommand;
            }
        }

        public async Task AddProductsAsync()
        {
            try
            {
                _activeModal = new(Context, _dialogService, _priceListItemService, _itemService);
                MainIsBusy = true;
                _activeModal.PromotionId = Id;
                await _activeModal.InitializeAsync();
                await _dialogService.ShowDialogAsync(_activeModal, "Agregar productos a la promoción");
                MainIsBusy = false;
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

                foreach (var item in PriceListItems)
                {
                    item.UpdatePromotionContext = this;
                }
                VerifyItemsInShadowList();
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{nameof(LoadProducts)} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                var (query, catalogsFragment) = _catalogsQuery.Value;

                dynamic variables = new GraphQLVariables()
                    .For(catalogsFragment, "pagination", new { pageSize = -1 })
                    .Build();

                var result = await _priceListItemService.GetDataContextAsync<PriceListDataContext>(query, variables);
                Catalogs = new ObservableCollection<CatalogGraphQLModel>(result.CatalogsPage.Entries);
                var selectedCatalog = Catalogs.FirstOrDefault() ?? throw new Exception("SelectedCatalog can't be null");
                IsInitialized = true;
                _isUpdating = true;
                _selectedCatalog = selectedCatalog;
                NotifyOfPropertyChange(nameof(SelectedCatalog));
                BuildItemTypes();
                _isUpdating = false;
                await ReloadDataAsync(_cascadeCancellation.Token);
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public new bool IsInitialized { get; set; } = false;


        private List<int> _shadowItems = [];

        public List<int> ShadowItems
        {
            get { return _shadowItems; }
            set 
            {
                if (_shadowItems != value) 
                {
                    _shadowItems = value;
                    NotifyOfPropertyChange(nameof(ShadowItems));
                }
            }
        }


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
                    foreach (var item in PriceListItems)
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
            foreach (var item in PriceListItems)
            {
                if (ShadowItems.Contains(item.Item.Id))
                {
                    item.IsChecked = true;
                }
            }
        }

        private ICommand _deleteListCommand;
        public ICommand DeleteListCommand
        {
            get
            {
                if (_deleteListCommand is null) _deleteListCommand = new AsyncCommand(DeleteListAsync);
                return _deleteListCommand;
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
                var itemIds = ShadowItems.ToList();
                string query = _batchDeleteQuery.Value;
                int totalDeleted = 0;
                var failedMessages = new List<string>();

                for (int i = 0; i < itemIds.Count; i += DeleteBatchSize)
                {
                    var batch = itemIds.Skip(i).Take(DeleteBatchSize).ToList();
                    var variables = new { input = new { priceListId = Id, itemIds = batch } };

                    var batchResult = await _priceListItemService.BatchAsync<BatchResultGraphQLModel>(query, variables);

                    if (batchResult.Success)
                    {
                        foreach (var itemId in batch)
                        {
                            var item = PriceListItems.FirstOrDefault(x => x.Item.Id == itemId);
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

        private ICommand _clearPromotionCommand;
        public ICommand ClearPromotionCommand
        {
            get
            {
                if (_clearPromotionCommand is null) _clearPromotionCommand = new AsyncCommand(ClearPromotionAsync);
                return _clearPromotionCommand;
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
                var batchResult = await _priceListItemService.BatchAsync<BatchResultGraphQLModel>(_purgeQuery.Value, variables);

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
