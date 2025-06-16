using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Billing;
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

        private readonly Helpers.Services.INotificationService _notificationService = IoC.Get<Helpers.Services.INotificationService>();
        public IGenericDataAccess<PriceListDetailGraphQLModel> PriceListDetailService { get; set; } = IoC.Get<IGenericDataAccess<PriceListDetailGraphQLModel>>();
        public PriceListViewModel Context { get; set; }
        Helpers.IDialogService _dialogService = IoC.Get<Helpers.IDialogService>();
        public IParallelBatchProcessor ParallelBatchProcessor { get; } = IoC.Get<IParallelBatchProcessor>();
        public UpdatePromotionViewModel(PriceListViewModel context)
        {
            Context = context;
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
                    if (!_isSilentUpdate && value != null)
                    {
                        _ = SetFiltersAndLoadAsync(catalog: value);
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
                    if (!_isSilentUpdate && value != null)
                    {
                        _ = SetFiltersAndLoadAsync(itemType: value);
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
                    if (!_isSilentUpdate && value != null)
                    {
                        _ = SetFiltersAndLoadAsync(itemCategory: value);
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
                    if (!_isSilentUpdate && value != null)
                    {
                        _ = SetFiltersAndLoadAsync(itemSubCategory: value);
                    }
                }
            }
        }

        public bool CanShowItemsSubCategories => SelectedItemType != null && SelectedItemType.Id != 0 && SelectedItemCategory != null && SelectedItemCategory.Id != 0;



        // Bandera para actualizaciones silentes
        private bool _isSilentUpdate = false;

        // Método centralizado para manejar cambios de filtros
        private async Task SetFiltersAndLoadAsync(
            CatalogGraphQLModel? catalog = null,
            ItemTypeGraphQLModel? itemType = null,
            ItemCategoryGraphQLModel? itemCategory = null,
            ItemSubCategoryGraphQLModel? itemSubCategory = null,
            bool shouldLoadProducts = true)
        {
            _isSilentUpdate = true;

            try
            {
                // 1. Actualizar catalog si se proporciona
                if (catalog != null)
                {
                    SetCatalogSilent(catalog);
                    BuildItemTypes();
                }

                // 2. Actualizar itemType si se proporciona
                if (itemType != null)
                {
                    SetItemTypeSilent(itemType);
                    BuildItemCategories();
                }

                // 3. Actualizar itemCategory si se proporciona  
                if (itemCategory != null)
                {
                    SetItemCategorySilent(itemCategory);
                    BuildItemSubCategories();
                }

                // 4. Actualizar itemSubCategory si se proporciona
                if (itemSubCategory != null)
                {
                    SetItemSubCategorySilent(itemSubCategory);
                }

                // 5. Cargar productos UNA SOLA VEZ al final
                if (shouldLoadProducts && IsInitialized)
                {
                    await Execute.OnUIThreadAsync(async () =>
                    {
                        IsBusy = true;
                        await LoadProducts();
                        IsBusy = false;
                    });
                }
            }
            finally
            {
                _isSilentUpdate = false;
            }
        }

        // Métodos silentes para establecer valores sin disparar eventos
        private void SetCatalogSilent(CatalogGraphQLModel value)
        {
            _selectedCatalog = value;
            NotifyOfPropertyChange(nameof(SelectedCatalog));
        }

        private void SetItemTypeSilent(ItemTypeGraphQLModel value)
        {
            _selectedItemType = value;
            NotifyOfPropertyChange(nameof(SelectedItemType));
        }

        private void SetItemCategorySilent(ItemCategoryGraphQLModel value)
        {
            _selectedItemCategory = value;
            NotifyOfPropertyChange(nameof(SelectedItemCategory));
        }

        private void SetItemSubCategorySilent(ItemSubCategoryGraphQLModel value)
        {
            _selectedItemSubCategory = value;
            NotifyOfPropertyChange(nameof(SelectedItemSubCategory));
        }

        // Métodos de construcción de listas
        private void BuildItemTypes()
        {
            if (SelectedCatalog == null) return;

            ItemsTypes = [.. SelectedCatalog.ItemsTypes];
            ItemsTypes.Insert(0, new ItemTypeGraphQLModel { Id = 0, Name = "<< MOSTRAR TODOS LOS TIPOS DE PRODUCTOS >>" });
            SetItemTypeSilent(ItemsTypes.First(x => x.Id == 0));
            BuildItemCategories();
        }

        private void BuildItemCategories()
        {
            if (SelectedItemType != null && SelectedItemType.Id != 0)
            {
                ItemsCategories = new ObservableCollection<ItemCategoryGraphQLModel>(SelectedItemType.ItemsCategories);
                ItemsCategories.Insert(0, new ItemCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS CATEGORÍAS DE PRODUCTOS >>" });
                SetItemCategorySilent(ItemsCategories.First(x => x.Id == 0));
            }
            else
            {
                ItemsCategories.Clear();
                SetItemCategorySilent(new ItemCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS CATEGORÍAS DE PRODUCTOS >>" });
            }

            BuildItemSubCategories();
            NotifyOfPropertyChange(nameof(CanShowItemsCategories));
            NotifyOfPropertyChange(nameof(CanShowItemsSubCategories));
        }

        private void BuildItemSubCategories()
        {
            if (SelectedItemCategory != null && SelectedItemCategory.Id != 0)
            {
                if (SelectedItemCategory.ItemsSubCategories != null)
                    ItemsSubCategories = [.. SelectedItemCategory.ItemsSubCategories];
                else
                    ItemsSubCategories = [];

                ItemsSubCategories.Insert(0, new ItemSubCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS SUBCATEGORÍAS DE PRODUCTOS >>" });
                SetItemSubCategorySilent(ItemsSubCategories.First(x => x.Id == 0));
            }
            else
            {
                ItemsSubCategories.Clear();
                SetItemSubCategorySilent(new ItemSubCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS SUBCATEGORÍAS DE PRODUCTOS >>" });
            }

            NotifyOfPropertyChange(nameof(CanShowItemsSubCategories));
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
                UpdatePromotionModalViewModel<PriceListGraphQLModel> instance = new(_dialogService);
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
                AddPromotionProductsModalViewModel instance = new(Context, _dialogService);
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

                IGenericDataAccess<PriceListDetailGraphQLModel>.PageResponseType result = await PriceListDetailService.GetPage(query, variables);
                TotalCount = result.PageResponse.Count;
                PriceListDetail = [.. Context.AutoMapper.Map<ObservableCollection<PriceListDetailDTO>>(result.PageResponse.Rows)];

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

                var result = await PriceListDetailService.GetDataContext<PriceListDataContext>(query, new { });
                Catalogs = new ObservableCollection<CatalogGraphQLModel>(result.Catalogs);
                var selectedCatalog = Catalogs.FirstOrDefault() ?? throw new Exception("SelectedCatalog can't be null");
                IsInitialized = true;
                await SetFiltersAndLoadAsync(catalog: selectedCatalog);
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

                _ = ParallelBatchProcessor.ProcessBatchAsync(query, promotionItems, typeof(PriceListDetailGraphQLModel), 10);
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
                var response = await PriceListDetailService.MutationContext<SuccessResponseDataWrapper>(query, variables);

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
