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

namespace NetErp.Billing.PriceList.ViewModels
{
    public class UpdatePromotionViewModel: Screen,
                IHandle<PromotionTempRecordResponseMessage>,
                IHandle<ParallelBatchCompletedMessage>
    {

        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<PriceListDetailGraphQLModel> _priceListDetailService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly IParallelBatchProcessor _parallelBatchProcessor;
        private readonly IRepository<ItemGraphQLModel> _itemService;
        private readonly IRepository<TempRecordGraphQLModel> _tempRecordService;
        private readonly IRepository<PriceListGraphQLModel> _priceListServiceForModal;
        public PriceListViewModel Context { get; set; }
        
        public UpdatePromotionViewModel(
            PriceListViewModel context,
            Helpers.Services.INotificationService notificationService,
            IRepository<PriceListDetailGraphQLModel> priceListDetailService,
            Helpers.IDialogService dialogService,
            IParallelBatchProcessor parallelBatchProcessor,
            IRepository<ItemGraphQLModel> itemService,
            IRepository<TempRecordGraphQLModel> tempRecordService,
            IRepository<PriceListGraphQLModel> priceListServiceForModal)
        {
            Context = context;
            _notificationService = notificationService;
            _priceListDetailService = priceListDetailService;
            _dialogService = dialogService;
            _parallelBatchProcessor = parallelBatchProcessor;
            _itemService = itemService;
            _tempRecordService = tempRecordService;
            _priceListServiceForModal = priceListServiceForModal;
            Context.EventAggregator.SubscribeOnUIThread(this);
            Messenger.Default.Register<ReturnedDataFromUpdatePromotionModalViewMessage<PriceListGraphQLModel>>(this, "UpdatePromotion", false, OnUpdatePromotion);
        }

        public void OnUpdatePromotion(ReturnedDataFromUpdatePromotionModalViewMessage<PriceListGraphQLModel> message)
        {
            if (message.ReturnedData is null) return;
            if (message.ReturnedData is PriceListGraphQLModel priceList)
            {
                Name = priceList.Name;
                StartDate = priceList.StartDate;
                EndDate = priceList.EndDate;
                _notificationService.ShowSuccess("Promoción actualizada correctamente", "Éxito");
            }
            else
            {
                _notificationService.ShowError("No se pudo actualizar la promoción", "Error");
            }
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

        private ObservableCollection<PriceListDetailDTO> _priceListDetail = [];
        public ObservableCollection<PriceListDetailDTO> PriceListDetail
        {
            get { return _priceListDetail; }
            set
            {
                if (_priceListDetail != value)
                {
                    _priceListDetail = value;
                    NotifyOfPropertyChange(nameof(PriceListDetail));
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
                        _cascadeCancellation = new CancellationTokenSource();
                        
                        BuildItemTypes();
                        if (IsInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
                    }
                }
            }
        }

        private ObservableCollection<ItemTypeGraphQLModel> _itemsTypes = [];

        public ObservableCollection<ItemTypeGraphQLModel> ItemsTypes
        {
            get { return _itemsTypes; }
            set
            {
                if (_itemsTypes != value)
                {
                    _itemsTypes = value;
                    NotifyOfPropertyChange(nameof(ItemsTypes));
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
                        _cascadeCancellation = new CancellationTokenSource();
                        
                        BuildItemCategories();
                        if (IsInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
                    }
                }
            }
        }

        public bool CanShowItemsCategories => SelectedItemType != null && SelectedItemType.Id != 0;

        private ObservableCollection<ItemCategoryGraphQLModel> _itemsCategories = [];

        public ObservableCollection<ItemCategoryGraphQLModel> ItemsCategories
        {
            get { return _itemsCategories; }
            set
            {
                if (_itemsCategories != value)
                {
                    _itemsCategories = value;
                    NotifyOfPropertyChange(nameof(ItemsCategories));
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
                        _cascadeCancellation = new CancellationTokenSource();
                        
                        BuildItemSubCategories();
                        if (IsInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
                    }
                }
            }
        }

        private ObservableCollection<ItemSubCategoryGraphQLModel> _itemsSubCategories = [];

        public ObservableCollection<ItemSubCategoryGraphQLModel> ItemsSubCategories
        {
            get { return _itemsSubCategories; }
            set
            {
                if (_itemsSubCategories != value)
                {
                    _itemsSubCategories = value;
                    NotifyOfPropertyChange(nameof(ItemsSubCategories));
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
                        _cascadeCancellation = new CancellationTokenSource();
                        
                        _ = ReloadDataAsync(_cascadeCancellation.Token);
                    }
                }
            }
        }

        public bool CanShowItemsSubCategories => SelectedItemType != null && SelectedItemType.Id != 0 && SelectedItemCategory != null && SelectedItemCategory.Id != 0;



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
            if (SelectedCatalog?.ItemsTypes == null) return;

            _isUpdating = true;
            ItemsTypes = [.. SelectedCatalog.ItemsTypes];
            ItemsTypes.Insert(0, new ItemTypeGraphQLModel { Id = 0, Name = "<< MOSTRAR TODOS LOS TIPOS DE PRODUCTOS >>" });
            _selectedItemType = ItemsTypes.First(x => x.Id == 0);
            NotifyOfPropertyChange(nameof(SelectedItemType));
            BuildItemCategories();
            _isUpdating = false;
        }

        private void BuildItemCategories()
        {
            _isUpdating = true;
            
            if (SelectedItemType != null && SelectedItemType.Id != 0 && SelectedItemType.ItemsCategories != null)
            {
                ItemsCategories = new ObservableCollection<ItemCategoryGraphQLModel>(SelectedItemType.ItemsCategories);
                ItemsCategories.Insert(0, new ItemCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS CATEGORÍAS DE PRODUCTOS >>" });
                _selectedItemCategory = ItemsCategories.First(x => x.Id == 0);
                NotifyOfPropertyChange(nameof(SelectedItemCategory));
            }
            else
            {
                // Reset categories when ItemType is "Show All" (Id = 0)
                ItemsCategories.Clear();
                _selectedItemCategory = new ItemCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS CATEGORÍAS DE PRODUCTOS >>" };
                NotifyOfPropertyChange(nameof(SelectedItemCategory));
            }

            BuildItemSubCategories();
            NotifyOfPropertyChange(nameof(CanShowItemsCategories));
            NotifyOfPropertyChange(nameof(CanShowItemsSubCategories));
            _isUpdating = false;
        }

        private void BuildItemSubCategories()
        {
            _isUpdating = true;
            
            if (SelectedItemCategory != null && SelectedItemCategory.Id != 0 && SelectedItemCategory.ItemsSubCategories != null)
            {
                ItemsSubCategories = [.. SelectedItemCategory.ItemsSubCategories];
                ItemsSubCategories.Insert(0, new ItemSubCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS SUBCATEGORÍAS DE PRODUCTOS >>" });
                _selectedItemSubCategory = ItemsSubCategories.First(x => x.Id == 0);
                NotifyOfPropertyChange(nameof(SelectedItemSubCategory));
            }
            else
            {
                // Reset subcategories when Category is "Show All" (Id = 0) or ItemType is "Show All"
                ItemsSubCategories.Clear();
                _selectedItemSubCategory = new ItemSubCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS SUBCATEGORÍAS DE PRODUCTOS >>" };
                NotifyOfPropertyChange(nameof(SelectedItemSubCategory));
            }

            NotifyOfPropertyChange(nameof(CanShowItemsSubCategories));
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
                    // Solo ejecutamos la busqueda si esta vacio el filtro o si hay por lo menos 3 caracteres digitados
                    if (string.IsNullOrEmpty(value) || value.Length >= 2)
                    {
                        PageIndex = 1;
                        if (IsInitialized) _ = Task.Run(async () =>
                        {
                            IsBusy = true;
                            await LoadProducts();
                            IsBusy = false;
                            _ = this.SetFocus(nameof(FilterSearch));
                        });
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
                UpdatePromotionModalViewModel<PriceListGraphQLModel> instance = new(_dialogService, _priceListServiceForModal);
                instance.Id = Id;
                instance.Name = Name;
                instance.MinimumDate = StartDate;
                instance.StartDate = StartDate;
                instance.EndDate = EndDate;
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
                AddPromotionProductsModalViewModel instance = new(Context, _dialogService, _priceListDetailService, _itemService, _tempRecordService, _parallelBatchProcessor);
                MainIsBusy = true;
                instance.PromotionId = Id;
                await instance.InitializeAsync();
                await _dialogService.ShowDialogAsync(instance, "Agregar productos a la promoción");
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
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string query = @"
                    query ($filter: PriceListDetailFilterInput) {
                      PageResponse: priceListDetailPage(filter: $filter) {
                        count
                        rows {
                          catalogItem {
                            id
                            name
                            code
                            reference
                          }
                        }
                      }
                    }
                    ";
                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.catalogId = new ExpandoObject();
                variables.filter.catalogId.@operator = "=";
                variables.filter.catalogId.value = SelectedCatalog != null ? SelectedCatalog.Id : throw new Exception("SelectedCatalog can't be null");

                variables.filter.priceListId = new ExpandoObject();
                variables.filter.priceListId.@operator = "=";
                variables.filter.priceListId.value = Id;
                variables.filter.priceListId.exclude = true;

                if (SelectedItemType != null && SelectedItemType.Id != 0)
                {
                    variables.filter.itemTypeId = new ExpandoObject();
                    variables.filter.itemTypeId.@operator = "=";
                    variables.filter.itemTypeId.value = SelectedItemType.Id;
                }
                if (SelectedItemCategory != null && SelectedItemCategory.Id != 0)
                {
                    variables.filter.itemCategoryId = new ExpandoObject();
                    variables.filter.itemCategoryId.@operator = "=";
                    variables.filter.itemCategoryId.value = SelectedItemCategory.Id;
                }
                if (SelectedItemSubCategory != null && SelectedItemSubCategory.Id != 0)
                {
                    variables.filter.itemSubCategoryId = new ExpandoObject();
                    variables.filter.itemSubCategoryId.@operator = "=";
                    variables.filter.itemSubCategoryId.value = SelectedItemSubCategory.Id;
                }

                variables.filter.filterSearch = new ExpandoObject();
                variables.filter.filterSearch.@operator = "like";
                variables.filter.filterSearch.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();
                variables.filter.filterSearch.exclude = true;

                PageResult<PriceListDetailGraphQLModel> result = await _priceListDetailService.GetPageAsync(query, variables);
                TotalCount = result.Count;
                PriceListDetail = [.. Context.AutoMapper.Map<ObservableCollection<PriceListDetailDTO>>(result.Rows)];

                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

                foreach (var item in PriceListDetail)
                {
                    item.UpdatePromotionContext = this;
                }
                VerifyItemsInShadowList();
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

        public async Task InitializeAsync()
        {
            try
            {
                string query = @"
                query {
                  catalogs {
                    id
                    name
                    itemsTypes {
                      id
                      name
                      itemsCategories {
                        id
                        name
                        itemsSubCategories: subCategories {
                          id
                          name
                        }
                      }
                    }
                  }
                }";

                var result = await _priceListDetailService.GetDataContextAsync<PriceListDataContext>(query, new { });
                Catalogs = new ObservableCollection<CatalogGraphQLModel>(result.Catalogs);
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


        public bool CanDelete
        {
            get
            {
                return ShadowItems != null && ShadowItems.Count > 0;
            }
        }

        private bool _itemsHeaderIsChecked;

        public bool ItemsHeaderIsChecked
        {
            get
            {
                if (PriceListDetail is null || PriceListDetail.Count == 0) return false;
                return _itemsHeaderIsChecked;
            }
            set
            {
                if (_itemsHeaderIsChecked != value)
                {
                    _itemsHeaderIsChecked = value;
                    NotifyOfPropertyChange(nameof(ItemsHeaderIsChecked));
                    foreach (var item in PriceListDetail)
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
        }

        public void RemoveItemsFromShadowList(int itemId)
        {
            ShadowItems.Remove(itemId);
            NotifyOfPropertyChange(nameof(CanDelete));
        }

        public void VerifyItemsInShadowList()
        {
            foreach (var item in PriceListDetail)
            {
                if (ShadowItems.Contains(item.CatalogItem.Id))
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

        public async Task DeleteListAsync()
        {
            try
            {
                MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar los registros seleccionados?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
                _notificationService.ShowInfo("Eliminando productos de la promoción...");

                if (ShadowItems.Count == 0) return;

                List<object> promotionItems = [];
                IsBusy = true;

                string query = @"
                mutation($data: [DeletePromotionItemInput!]!){
                  ListResponse: deletePromotionItemList(data: $data){
                    catalogItem {
                    id
                    name
                    code
                    reference
                    }
                  }
                }";

                foreach (var itemId in ShadowItems.ToList())
                {
                    object promotionItem = new
                    {
                        PriceListId = Id,
                        ItemId = itemId
                    };
                    promotionItems.Add(promotionItem);
                    PriceListDetail.Remove(PriceListDetail.Where(x => x.CatalogItem.Id == itemId).FirstOrDefault() ?? throw new Exception("No se encontró el elemento en la lista"));
                    ShadowItems.Remove(itemId);
                }

                _ = _parallelBatchProcessor.ProcessBatchAsync(query, promotionItems, typeof(PriceListDetailGraphQLModel), 10);
                NotifyOfPropertyChange(nameof(CanDelete));
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

        public async Task ClearPromotionAsync()
        {
            try
            {
                MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar TODOS los registros?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;

                string query = @"
                mutation($id: Int!){
                  Data: clearPriceListDetail(id: $id){
                    success
                    message
                  }
                }  ";

                IsBusy = true;
                dynamic variables = new ExpandoObject();
                variables.Id = Id;
                var response = await _priceListDetailService.MutationContextAsync<SuccessResponseDataWrapper>(query, variables);

                if (!response.Data.Success)
                {
                    _notificationService.ShowError("Error al limpiar la promoción");
                    throw new Exception(response.Data.Message);
                }
                PriceListDetail.Clear();
                ShadowItems.Clear();
                NotifyOfPropertyChange(nameof(CanDelete));
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
            await LoadProducts();
            IsBusy = false;
            _notificationService.ShowSuccess(message.Response.Message);
        }

        public Task HandleAsync(ParallelBatchCompletedMessage message, CancellationToken cancellationToken)
        {
            if(message.IsSuccess)
            {
                _notificationService.ShowSuccess("Productos eliminados correctamente");
            }
            else
            {
                _notificationService.ShowError($"Error al eliminar productos: {(message.Exception is null ? "" : message.Exception.Message)}");
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
    }
}
