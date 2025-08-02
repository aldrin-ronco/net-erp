using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Models.Billing;
using Models.Global;
using Models.Inventory;
using NetErp.Billing.PriceList.DTO;
using NetErp.Billing.PriceList.Views;
using NetErp.Helpers;
using NetErp.Helpers.Services;
using Newtonsoft.Json;
using Ninject.Activation;
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

namespace NetErp.Billing.PriceList.ViewModels
{
    public class AddPromotionProductsModalViewModel : Screen
    {
        private readonly Helpers.IDialogService _dialogService;
        private readonly IRepository<PriceListDetailGraphQLModel> _priceListDetailService;
        private readonly IRepository<ItemGraphQLModel> _itemService;
        private readonly IRepository<TempRecordGraphQLModel> _tempRecordService;
        private readonly IParallelBatchProcessor _parallelBatchProcessor;
        public PriceListViewModel Context { get; set; }
        
        public AddPromotionProductsModalViewModel(
            PriceListViewModel context, 
            Helpers.IDialogService dialogService,
            IRepository<PriceListDetailGraphQLModel> priceListDetailService,
            IRepository<ItemGraphQLModel> itemService,
            IRepository<TempRecordGraphQLModel> tempRecordService,
            IParallelBatchProcessor parallelBatchProcessor)
        {
            Context = context;
            _dialogService = dialogService;
            _priceListDetailService = priceListDetailService;
            _itemService = itemService;
            _tempRecordService = tempRecordService;
            _parallelBatchProcessor = parallelBatchProcessor;
            
            AddedItems.CollectionChanged += (s, e) =>
            {
                NotifyOfPropertyChange(nameof(AddedItemsHeaderIsChecked));
            };
            Items.CollectionChanged += (s, e) =>
            {
                NotifyOfPropertyChange(nameof(ItemsHeaderIsChecked));
            };
            AddedItemsShadowListIds.CollectionChanged += (s, e) =>
            {
                NotifyOfPropertyChange(nameof(CanSave));
            };
            Context.EventAggregator.SubscribeOnUIThread(this);
        }
        public new bool IsInitialized { get; set; } = false;


        public int PromotionId { get; set; } = 0;

        //Lista creada para poder controlar que el usuario no se quede sin guardar cambios

        private ObservableCollection<int> _addedItemsShadowListIds = [];

        public ObservableCollection<int> AddedItemsShadowListIds
        {
            get { return _addedItemsShadowListIds; }
            set
            {
                if (_addedItemsShadowListIds != null)
                {
                    _addedItemsShadowListIds = value;
                    NotifyOfPropertyChange(nameof(AddedItemsShadowListIds));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private ObservableCollection<PromotionCatalogItemDTO> _items = [];

        public ObservableCollection<PromotionCatalogItemDTO> Items
        {
            get { return _items; }
            set
            {
                if (_items != value)
                {
                    _items = value;
                    NotifyOfPropertyChange(nameof(Items));
                }
            }
        }

        private PromotionCatalogItemDTO _selectedItem = new();

        public PromotionCatalogItemDTO SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                }
            }
        }

        private ObservableCollection<PromotionCatalogItemDTO> _addedItems = [];

        public ObservableCollection<PromotionCatalogItemDTO> AddedItems
        {
            get { return _addedItems; }
            set
            {
                if (_addedItems != value)
                {
                    _addedItems = value;
                    NotifyOfPropertyChange(nameof(AddedItems));
                }
            }
        }

        private PromotionCatalogItemDTO _selectedAddedItem = new();

        public PromotionCatalogItemDTO SelectedAddedItem
        {
            get { return _selectedAddedItem; }
            set
            {
                if (_selectedAddedItem != value)
                {
                    _selectedAddedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedAddedItem));
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

        // Bandera para actualizaciones silentes
        // Flag to prevent cascading reload operations during internal updates
        private bool _isUpdating = false;
        private CancellationTokenSource _cascadeCancellation = new();

        // Obsolete methods removed - replaced by individual dropdown logic

        // Reload data method with cancellation support
        private async Task ReloadDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Execute.OnUIThreadAsync(async () =>
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    IsBusy = true;
                    await LoadItemsAsync();
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

        private bool _itemsGridIsBusy;

        public bool ItemsGridIsBusy
        {
            get { return _itemsGridIsBusy; }
            set
            {
                if (_itemsGridIsBusy != value)
                {
                    _itemsGridIsBusy = value;
                    NotifyOfPropertyChange(nameof(ItemsGridIsBusy));
                }
            }
        }

        private bool _addedItemsGridIsBusy;

        public bool AddedItemsGridIsBusy
        {
            get { return _addedItemsGridIsBusy; }
            set
            {
                if (_addedItemsGridIsBusy != value)
                {
                    _addedItemsGridIsBusy = value;
                    NotifyOfPropertyChange(nameof(AddedItemsGridIsBusy));
                }
            }
        }

        private string _itemsFilterSearch = "";
        public string ItemsFilterSearch
        {
            get { return _itemsFilterSearch; }
            set
            {
                if (_itemsFilterSearch != value)
                {
                    _itemsFilterSearch = value;
                    NotifyOfPropertyChange(nameof(ItemsFilterSearch));
                    // Solo ejecutamos la busqueda si esta vacio el filtro o si hay por lo menos 3 caracteres digitados
                    if (string.IsNullOrEmpty(value) || value.Length >= 2)
                    {
                        ItemsPageIndex = 1;
                        if (IsInitialized) _ = Task.Run(async () =>
                        {
                            IsBusy = true;
                            await LoadItemsAsync();
                            IsBusy = false;
                            _ = this.SetFocus(nameof(ItemsFilterSearch));
                        });
                    }
                }
            }
        }

        private string _addedItemsFilterSearch = "";
        public string AddedItemsFilterSearch
        {
            get { return _addedItemsFilterSearch; }
            set
            {
                if (_addedItemsFilterSearch != value)
                {
                    _addedItemsFilterSearch = value;
                    NotifyOfPropertyChange(nameof(AddedItemsFilterSearch));
                    // Solo ejecutamos la busqueda si esta vacio el filtro o si hay por lo menos 3 caracteres digitados
                    if (string.IsNullOrEmpty(value) || value.Length >= 2)
                    {
                        AddedItemsPageIndex = 1;
                        if (IsInitialized) _ = Task.Run(async () =>
                        {
                            IsBusy = true;
                            await LoadTempAddedItemsPromotionAsync();
                            IsBusy = false;
                            _ = this.SetFocus(nameof(AddedItemsFilterSearch));
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

        private bool _itemsHeaderIsChecked;

        public bool ItemsHeaderIsChecked
        {
            get
            {
                if (Items is null || Items.Count == 0) return false;
                return _itemsHeaderIsChecked;
            }
            set
            {
                if (_itemsHeaderIsChecked != value)
                {
                    _itemsHeaderIsChecked = value;
                    NotifyOfPropertyChange(nameof(ItemsHeaderIsChecked));
                    foreach (var item in Items)
                    {
                        item.IsChecked = value;
                    }
                }
            }
        }

        private bool _addedItemsHeaderIsChecked;

        public bool AddedItemsHeaderIsChecked
        {
            get
            {
                if (AddedItems is null || AddedItems.Count == 0) return false;
                return _addedItemsHeaderIsChecked;
            }
            set
            {
                if (_addedItemsHeaderIsChecked != value)
                {
                    _addedItemsHeaderIsChecked = value;
                    NotifyOfPropertyChange(nameof(AddedItemsHeaderIsChecked));
                    foreach (var item in AddedItems)
                    {
                        item.IsChecked = value;
                    }
                }
            }
        }



        public async Task LoadItemsAsync()
        {
            try
            {
                ItemsGridIsBusy = true;
                // Iniciar cronometro
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string query = @"
                    query($filter: ItemPromotionFilterInput){
                      PageResponse: promotionItemPage(filter: $filter){
                        count
                        rows{
                          id
                          name
                          reference
                          code
                          subCategory{
                            id
                            name
                            itemCategory{
                              id
                              name
                              itemType{
                                id
                                name
                                catalog{
                                  id
                                  name
                                }
                              }
                            }
                          }
                        }
                      }
                    }";
                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();

                //Propiedades excluidas
                variables.filter.priceListId = new ExpandoObject();
                variables.filter.priceListId.@operator = "=";
                variables.filter.priceListId.value = PromotionId;
                variables.filter.priceListId.exclude = true;

                variables.filter.userId = new ExpandoObject();
                variables.filter.userId.@operator = "=";
                variables.filter.userId.value = 1;
                variables.filter.userId.exclude = true;

                variables.filter.tableName = new ExpandoObject();
                variables.filter.tableName.@operator = "=";
                variables.filter.tableName.value = TempRecordTableName.InventoryItem;
                variables.filter.tableName.exclude = true;

                variables.filter.type = new ExpandoObject();
                variables.filter.type.@operator = "=";
                variables.filter.type.value = TempRecordType.PromotionItem;
                variables.filter.type.exclude = true;


                //Propiedades incluidas - todas van dentro del AND
                var andFilters = new List<dynamic>();

                // Filtro obligatorio: catalogId
                dynamic catalogFilter = new ExpandoObject();
                catalogFilter.catalogId = new ExpandoObject();
                catalogFilter.catalogId.@operator = "=";
                catalogFilter.catalogId.value = SelectedCatalog != null ? SelectedCatalog.Id : throw new Exception("SelectedCatalog can't be null");
                andFilters.Add(catalogFilter);

                // Filtro obligatorio: isActive
                dynamic activeFilter = new ExpandoObject();
                activeFilter.isActive = new ExpandoObject();
                activeFilter.isActive.@operator = "=";
                activeFilter.isActive.value = true;
                andFilters.Add(activeFilter);

                // Filtros condicionales de dropdowns
                if (SelectedItemType != null && SelectedItemType.Id != 0)
                {
                    dynamic itemTypeFilter = new ExpandoObject();
                    itemTypeFilter.itemTypeId = new ExpandoObject();
                    itemTypeFilter.itemTypeId.@operator = "=";
                    itemTypeFilter.itemTypeId.value = SelectedItemType.Id;
                    andFilters.Add(itemTypeFilter);
                }

                if (SelectedItemCategory != null && SelectedItemCategory.Id != 0)
                {
                    dynamic categoryFilter = new ExpandoObject();
                    categoryFilter.itemCategoryId = new ExpandoObject();
                    categoryFilter.itemCategoryId.@operator = "=";
                    categoryFilter.itemCategoryId.value = SelectedItemCategory.Id;
                    andFilters.Add(categoryFilter);
                }

                if (SelectedItemSubCategory != null && SelectedItemSubCategory.Id != 0)
                {
                    dynamic subCategoryFilter = new ExpandoObject();
                    subCategoryFilter.itemSubCategoryId = new ExpandoObject();
                    subCategoryFilter.itemSubCategoryId.@operator = "=";
                    subCategoryFilter.itemSubCategoryId.value = SelectedItemSubCategory.Id;
                    andFilters.Add(subCategoryFilter);
                }

                // Filtro OR para búsqueda de texto (name OR reference OR code)
                if (!string.IsNullOrEmpty(ItemsFilterSearch))
                {
                    dynamic searchFilter = new ExpandoObject();
                    
                    dynamic nameFilter = new ExpandoObject();
                    nameFilter.name = new ExpandoObject();
                    nameFilter.name.@operator = "like";
                    nameFilter.name.value = ItemsFilterSearch.Trim().RemoveExtraSpaces();

                    dynamic referenceFilter = new ExpandoObject();
                    referenceFilter.reference = new ExpandoObject();
                    referenceFilter.reference.@operator = "like";
                    referenceFilter.reference.value = ItemsFilterSearch.Trim().RemoveExtraSpaces();
                    
                    dynamic codeFilter = new ExpandoObject();
                    codeFilter.code = new ExpandoObject();
                    codeFilter.code.@operator = "like";
                    codeFilter.code.value = ItemsFilterSearch.Trim().RemoveExtraSpaces();
                    
                    searchFilter.or = new[] { nameFilter, referenceFilter, codeFilter };
                    andFilters.Add(searchFilter);
                }

                // Convertir a array dinámico del tamaño exacto
                variables.filter.and = andFilters.ToArray();

                var result = await _itemService.GetPageAsync(query, variables);
                Items = new ObservableCollection<PromotionCatalogItemDTO>(Context.AutoMapper.Map<ObservableCollection<PromotionCatalogItemDTO>>(result.Rows));
                ItemsTotalCount = result.Count;

                stopwatch.Stop();
                ItemsResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

                foreach (var item in Items)
                {
                    item.Context = this;
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
                ItemsGridIsBusy = false;
            }
        }

        public async Task LoadTempAddedItemsPromotionAsync()
        {
            try
            {
                AddedItemsGridIsBusy = true;
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string query = @"
                query ($filter: ItemPromotionFilterInput) {
                  PageResponse: promotionAddedTempItemPage(filter: $filter) {
                    count
                    rows {
                      id
                      name
                      reference
                      code
                      subCategory {
                        id
                        name
                        itemCategory {
                          id
                          name
                          itemType {
                            id
                            name
                            catalog {
                              id
                              name
                            }
                          }
                        }
                      }
                    }
                  }
                }";
                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();

                variables.filter.or = new ExpandoObject[]
                {
                    new(),
                    new(),
                    new()
                };

                variables.filter.or[0].name = new ExpandoObject();
                variables.filter.or[0].name.@operator = "like";
                variables.filter.or[0].name.value = AddedItemsFilterSearch.Trim().RemoveExtraSpaces();

                variables.filter.or[1].reference = new ExpandoObject();
                variables.filter.or[1].reference.@operator = "like";
                variables.filter.or[1].reference.value = AddedItemsFilterSearch.Trim().RemoveExtraSpaces();

                variables.filter.or[2].code = new ExpandoObject();
                variables.filter.or[2].code.@operator = "like";
                variables.filter.or[2].code.value = AddedItemsFilterSearch.Trim().RemoveExtraSpaces();

                variables.filter.userId = new ExpandoObject();
                variables.filter.userId.@operator = "=";
                variables.filter.userId.value = 1;
                variables.filter.userId.exclude = true;

                variables.filter.tableName = new ExpandoObject();
                variables.filter.tableName.@operator = "=";
                variables.filter.tableName.value = TempRecordTableName.InventoryItem;
                variables.filter.tableName.exclude = true;

                variables.filter.type = new ExpandoObject();
                variables.filter.type.@operator = "=";
                variables.filter.type.value = TempRecordType.PromotionItem;
                variables.filter.type.exclude = true;

                var result = await _itemService.GetPageAsync(query, variables);
                AddedItems = new ObservableCollection<PromotionCatalogItemDTO>(Context.AutoMapper.Map<ObservableCollection<PromotionCatalogItemDTO>>(result.Rows));
                AddedItemsTotalCount = result.Count;

                stopwatch.Stop();
                AddedItemsResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
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
                AddedItemsGridIsBusy = false;
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

        public async Task AddItemAsync()
        {
            try
            {
                IsBusy = true;
                string query = @"
                    mutation($data: CreateTempRecordInput!){
                      CreateResponse: createTempRecord(data: $data){
                        id
                        recordId
                        tableName
                        userId
                      }
                    }";
                dynamic variables = new ExpandoObject();
                variables.data = new ExpandoObject();
                variables.data.tableName = TempRecordTableName.InventoryItem;
                variables.data.recordId = SelectedItem.Id;
                variables.data.userId = 1; // Cambiar por el ID del usuario actual
                variables.data.type = TempRecordType.PromotionItem;
                _ = await _tempRecordService.CreateAsync(query, variables);
                SelectedItem.IsChecked = false;
                AddedItems.Add(SelectedItem);
                AddedItemsShadowListIds.Add(SelectedItem.Id);
                Items.Remove(SelectedItem);
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

        public async Task RemoveItemAsync()
        {
            try
            {
                IsBusy = true;
                string query = @"
                    mutation($data: DeleteTempRecordInput!){
                      DeleteResponse: deleteTempRecord(data: $data){
                        id
                        recordId
                        tableName
                        userId
                      }
                    }
                ";
                dynamic variables = new ExpandoObject();
                variables.data = new ExpandoObject();
                variables.data.tableName = TempRecordTableName.InventoryItem;
                variables.data.recordId = SelectedAddedItem.Id;
                variables.data.userId = 1; // Cambiar por el ID del usuario actual
                variables.data.type = TempRecordType.PromotionItem;
                _ = await _tempRecordService.DeleteAsync(query, variables);
                SelectedAddedItem.IsChecked = false;
                Items.Add(SelectedAddedItem);
                AddedItemsShadowListIds.Remove(SelectedAddedItem.Id);
                AddedItems.Remove(SelectedAddedItem);
                NotifyOfPropertyChange(nameof(CanRemoveAddedItemList));
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


        public bool CanAddItemList
        {
            get
            {
                if (Items is null || Items.Count == 0) return false;
                return Items.Any(item => item.IsChecked);
            }
        }

        public bool CanRemoveAddedItemList
        {
            get
            {
                if (AddedItems is null || AddedItems.Count == 0) return false;
                return AddedItems.Any(item => item.IsChecked);
            }
        }

        public bool CanSave
        {
            get
            {
                if (AddedItemsShadowListIds is null || AddedItemsShadowListIds.Count == 0) return false;
                return AddedItemsShadowListIds.Count > 0;
            }
        }

        private ICommand _addItemListCommand;

        public ICommand AddItemListCommand
        {
            get
            {
                if (_addItemListCommand is null) _addItemListCommand = new AsyncCommand(AddItemListAsync);
                return _addItemListCommand;
            }
        }

        public async Task AddItemListAsync()
        {
            var checkedItems = Items.Where(item => item.IsChecked).ToList();
            if (checkedItems.Count == 0) return;

            List<object> tempRecords = [];
            IsBusy = true;

            string query = @"
            mutation($data: [CreateTempRecordInput!]!){
              ListResponse: createTempRecordList(data: $data){
                id
                recordId
                tableName
                userId
              }
            }";

            try
            {
                foreach (var item in checkedItems)
                {
                    object tempRecord = new
                    {
                        RecordId = item.Id,
                        TableName = TempRecordTableName.InventoryItem,
                        UserId = 1,
                        Type = TempRecordType.PromotionItem
                    };
                    tempRecords.Add(tempRecord);
                }
                foreach (var item in checkedItems)
                {
                    item.IsChecked = false;
                    Items.Remove(item);
                    AddedItems.Add(item);
                    AddedItemsShadowListIds.Add(item.Id);
                }
                ItemsHeaderIsChecked = false;
                _ = _parallelBatchProcessor.ProcessBatchAsync(query, tempRecords, typeof(TempRecordGraphQLModel), 10);
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

        private ICommand _removeItemListCommand;

        public ICommand RemoveItemListCommand
        {
            get
            {
                if (_removeItemListCommand is null) _removeItemListCommand = new AsyncCommand(RemoveItemListAsync);
                return _removeItemListCommand;
            }
        }

        public async Task RemoveItemListAsync()
        {
            var checkedItems = AddedItems.Where(item => item.IsChecked).ToList();
            if (checkedItems.Count == 0) return;

            List<object> tempRecords = [];
            IsBusy = true;

            string query = @"
            mutation($data: [DeleteTempRecordInput!]!){
              ListResponse: deleteTempRecordList(data: $data){
                id
                recordId
                tableName
                userId
              }
            }";

            try
            {
                foreach (var item in checkedItems)
                {
                    object tempRecord = new
                    {
                        RecordId = item.Id,
                        TableName = TempRecordTableName.InventoryItem,
                        UserId = 1,
                        Type = TempRecordType.PromotionItem
                    };
                    tempRecords.Add(tempRecord);
                }
                foreach (var item in checkedItems)
                {
                    item.IsChecked = false;
                    AddedItems.Remove(item);
                    AddedItemsShadowListIds.Remove(item.Id);
                    Items.Add(item);
                }
                AddedItemsHeaderIsChecked = false;
                _ = _parallelBatchProcessor.ProcessBatchAsync(query, tempRecords, typeof(TempRecordGraphQLModel), 10);

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

        private ICommand _cancelCommand;

        public ICommand CancelCommand
        {
            get
            {
                if (_cancelCommand == null) _cancelCommand = new AsyncCommand(Cancel);
                return _cancelCommand;
            }
        }

        public async Task Cancel()
        {
            await _dialogService.CloseDialogAsync(this, true);
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
                IsBusy = true;
                SuccessResponseDataWrapper result = await ExecuteSaveAsync();
                await Context.EventAggregator.PublishOnUIThreadAsync(new PromotionTempRecordResponseMessage() { Response = result.Data });
                await _dialogService.CloseDialogAsync(this, true);
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
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<SuccessResponseDataWrapper> ExecuteSaveAsync()
        {
            try
            {
                string query = @"
                  mutation($data: SaveAddedPromotionItemInput!){
                      Data: saveAddedPromotionItems(data: $data){
                        success
                        message
                      }
                    }  
                ";
                dynamic variables = new ExpandoObject();
                variables.data = new ExpandoObject();
                variables.data.promotionId = PromotionId;
                variables.data.userId = 1; // Cambiar por el ID del usuario actual
                variables.data.tableName = TempRecordTableName.InventoryItem;
                variables.data.type = TempRecordType.PromotionItem;

                SuccessResponseDataWrapper result = await _tempRecordService.MutationContextAsync<SuccessResponseDataWrapper>(query, variables);
                if (!result.Data.Success)
                {
                    throw new Exception(result.Data.Message);
                }
                AddedItems.Clear();
                AddedItemsShadowListIds.Clear();
                return result;
            }
            catch (Exception ex)
            {

                throw new AsyncException(innerException: ex);
            }
        }

        public async Task ClearTempRecordsAsync()
        {
            try
            {
                IsBusy = true;
                string query = @"
                    mutation($data: ClearTempRecordInput!){
                      Data: clearTempRecords(data: $data){
                        success
                        message
                      }
                    }";
                dynamic variables = new ExpandoObject();
                variables.data = new ExpandoObject();
                variables.data.tableName = TempRecordTableName.InventoryItem;
                variables.data.type = TempRecordType.PromotionItem;
                variables.data.userId = 1; // Cambiar por el ID del usuario actual
                var result = await _tempRecordService.MutationContextAsync<SuccessResponseDataWrapper>(query, variables);
                if (!result.Data.Success)
                {
                    throw new Exception(result.Data.Message);
                }
                AddedItems.Clear();
                AddedItemsShadowListIds.Clear();
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public override async Task<bool> CanCloseAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (AddedItemsShadowListIds is null || AddedItemsShadowListIds.Count == 0) return await base.CanCloseAsync(cancellationToken);
                var result = ThemedMessageBox.Show("Confirmar cierre",
                    "¿Confirma que desea guardar los cambios?",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                {
                    return false;
                }

                if (result == MessageBoxResult.Yes)
                {
                    await SaveAsync();
                }

                if (result == MessageBoxResult.No)
                {
                    await ClearTempRecordsAsync();
                }

                return await base.CanCloseAsync(cancellationToken);
            }
            catch (AsyncException ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
                return false;
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
                return false;
            }
        }

        #region ItemsPagination


        private string _itemsResponseTime;
        public string ItemsResponseTime
        {
            get { return _itemsResponseTime; }
            set
            {
                if (_itemsResponseTime != value)
                {
                    _itemsResponseTime = value;
                    NotifyOfPropertyChange(() => ItemsResponseTime);
                }
            }
        }


        /// <summary>
        /// PageIndex
        /// </summary>
        private int _itemsPageIndex = 1; // DefaultPageIndex = 1
        public int ItemsPageIndex
        {
            get { return _itemsPageIndex; }
            set
            {
                if (_itemsPageIndex != value)
                {
                    _itemsPageIndex = value;
                    NotifyOfPropertyChange(() => ItemsPageIndex);
                }
            }
        }

        /// <summary>
        /// PageSize
        /// </summary>
        private int _itemsPageSize = 50; // Default PageSize 50
        public int ItemsPageSize
        {
            get { return _itemsPageSize; }
            set
            {
                if (_itemsPageSize != value)
                {
                    _itemsPageSize = value;
                    NotifyOfPropertyChange(() => ItemsPageSize);
                }
            }
        }

        /// <summary>
        /// TotalCount
        /// </summary>
        private int _itemsTotalCount = 0;
        public int ItemsTotalCount
        {
            get { return _itemsTotalCount; }
            set
            {
                if (_itemsTotalCount != value)
                {
                    _itemsTotalCount = value;
                    NotifyOfPropertyChange(() => ItemsTotalCount);
                }
            }
        }

        /// <summary>
        /// PaginationCommand para controlar evento
        /// </summary>
        private ICommand _itemsPaginationCommand;
        public ICommand ItemsPaginationCommand
        {
            get
            {
                if (_itemsPaginationCommand == null) this._itemsPaginationCommand = new AsyncCommand(ExecuteChangeIndexItemsPaginationAsync, CanExecuteChangeIndexItemsPagination);
                return _itemsPaginationCommand;
            }
        }

        private async Task ExecuteChangeIndexItemsPaginationAsync()
        {
            IsBusy = true;
            await LoadItemsAsync();
            IsBusy = false;
        }

        private bool CanExecuteChangeIndexItemsPagination()
        {
            return true;
        }

        #endregion

        #region AddedItemsPagination


        private string _addedItemsResponseTime;
        public string AddedItemsResponseTime
        {
            get { return _addedItemsResponseTime; }
            set
            {
                if (_addedItemsResponseTime != value)
                {
                    _addedItemsResponseTime = value;
                    NotifyOfPropertyChange(() => AddedItemsResponseTime);
                }
            }
        }


        /// <summary>
        /// PageIndex
        /// </summary>
        private int _addedItemsPageIndex = 1; // DefaultPageIndex = 1
        public int AddedItemsPageIndex
        {
            get { return _addedItemsPageIndex; }
            set
            {
                if (_addedItemsPageIndex != value)
                {
                    _addedItemsPageIndex = value;
                    NotifyOfPropertyChange(() => AddedItemsPageIndex);
                }
            }
        }

        /// <summary>
        /// PageSize
        /// </summary>
        private int _addedItemsPageSize = 50; // Default PageSize 50
        public int AddedItemsPageSize
        {
            get { return _addedItemsPageSize; }
            set
            {
                if (_addedItemsPageSize != value)
                {
                    _addedItemsPageSize = value;
                    NotifyOfPropertyChange(() => AddedItemsPageSize);
                }
            }
        }

        /// <summary>
        /// TotalCount
        /// </summary>
        private int _addedItemsTotalCount = 0;
        public int AddedItemsTotalCount
        {
            get { return _addedItemsTotalCount; }
            set
            {
                if (_addedItemsTotalCount != value)
                {
                    _addedItemsTotalCount = value;
                    NotifyOfPropertyChange(() => AddedItemsTotalCount);
                }
            }
        }

        /// <summary>
        /// PaginationCommand para controlar evento
        /// </summary>
        private ICommand _addedItemsPaginationCommand;
        public ICommand AddedItemsPaginationCommand
        {
            get
            {
                if (_addedItemsPaginationCommand == null) this._addedItemsPaginationCommand = new AsyncCommand(ExecuteChangeIndexAddedItemsPaginationAsync, CanExecuteChangeIndexAddedItemsPagination);
                return _addedItemsPaginationCommand;
            }
        }

        private async Task ExecuteChangeIndexAddedItemsPaginationAsync()
        {
            IsBusy = true;
            await LoadItemsAsync();
            IsBusy = false;
        }

        private bool CanExecuteChangeIndexAddedItemsPagination()
        {
            return true;
        }

        #endregion
    }

    public class PromotionTempRecordResponseMessage
    {
        public SuccessResponseModel Response { get; set; } = new();
    }

    public class SuccessResponseModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SuccessResponseDataWrapper
    {
        public SuccessResponseModel Data { get; set; } = new();
    }
}
