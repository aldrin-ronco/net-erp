using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.Inventory;
using NetErp.Billing.PriceList.DTO;
using NetErp.Helpers.Messages;
using NetErp.Helpers.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices.ActiveDirectory;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace NetErp.Billing.PriceList.ViewModels
{
    public class PriceListMasterViewModel : Screen, IHandle<OperationCompletedMessage>  
    {
        private bool _isUpdating = false;
        private readonly Dictionary<Guid, int> _operationItemMapping = new Dictionary<Guid, int>();
        private readonly Helpers.Services.INotificationService _notificationService;
        public IGenericDataAccess<PriceListDetailGraphQLModel> PriceListDetailService { get; set; } = IoC.Get<IGenericDataAccess<PriceListDetailGraphQLModel>>();
        public IBackgroundQueueService BackgroundQueueService { get; set; } = IoC.Get<IBackgroundQueueService>();
        public PriceListViewModel Context { get; set; }

        public string MaskN2 { get; set; } = "n2";

        public string MaskN5 { get; set; } = "n5";


        private ObservableCollection<PriceListDetailDTO> _priceListDetail = new();
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

        private ObservableCollection<CatalogGraphQLModel> _catalogs = new();

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
                    if (!_isUpdating)
                    {
                        LoadItemTypes();
                        if (IsInitialized) _ = Execute.OnUIThreadAsync(async () => await LoadPriceList());
                    }
                }
            }
        }

        private ObservableCollection<ItemTypeGraphQLModel> _itemsTypes = new();

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
                    if (!_isUpdating)
                    {
                        LoadItemCategories();
                        if (IsInitialized) _ = Execute.OnUIThreadAsync(async () => await LoadPriceList());
                    }
                }
            }
        }

        public bool CanShowItemsCategories => SelectedItemType != null && SelectedItemType.Id != 0;

        private ObservableCollection<ItemCategoryGraphQLModel> _itemsCategories = new();

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
                    if (!_isUpdating)
                    {
                        LoadItemSubCategories();
                        if (IsInitialized) _ = Execute.OnUIThreadAsync(async () => await LoadPriceList());
                    }
                }
            }
        }

        private ObservableCollection<ItemSubCategoryGraphQLModel> _itemsSubCategories = new();

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
                    if (!_isUpdating)
                    {
                        if (IsInitialized) _ = Execute.OnUIThreadAsync(async () => await LoadPriceList());
                    }
                }
            }
        }

        public bool CanShowItemsSubCategories => SelectedItemType != null && SelectedItemType.Id != 0 && SelectedItemCategory != null && SelectedItemCategory.Id != 0;

        private ObservableCollection<PriceListGraphQLModel> _priceLists = new();

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

        private PriceListGraphQLModel _selectedPriceList;

        public PriceListGraphQLModel SelectedPriceList
        {
            get { return _selectedPriceList; }
            set
            {
                if (_selectedPriceList != value)
                {
                    _selectedPriceList = value;
                    NotifyOfPropertyChange(nameof(SelectedPriceList));
                    if (IsInitialized) _ = Execute.OnUIThreadAsync(async () => await LoadPriceList());
                }
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                _isUpdating = true;
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
                          priceLists{
                            id
                            name
                          }
                        }";
                var result = await PriceListDetailService.GetDataContext<PriceListDataContext>(query, new { });
                Catalogs = new ObservableCollection<CatalogGraphQLModel>(result.Catalogs);
                SelectedCatalog = Catalogs.FirstOrDefault() ?? throw new Exception("SelectedCatalog can't be null");
                PriceLists = new ObservableCollection<PriceListGraphQLModel>(result.PriceLists);
                SelectedPriceList = PriceLists.FirstOrDefault() ?? throw new Exception("SelectedPriceList can't be null");
                LoadItemTypes();
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
                IsInitialized = true;
                _isUpdating = false;
            }
        }

        public new bool IsInitialized { get; set; } = false;

        public async Task LoadPriceList()
        {
            try
            {
                string query = @"
                        query($filter: PriceListDetailFilterInput){
                          PageResponse: priceListDetailPage(filter: $filter){
                            count
                            rows{
                              catalogItem{
                                id
                                name
                                reference
                              }
                              measurement{
                                id
                                abbreviation
                              }
                              cost
                              profitMargin
                              price
                              minimumPrice
                              discountMargin
                            }
                          }
                        }";
                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.catalogId = new ExpandoObject();
                variables.filter.catalogId.@operator = "=";
                variables.filter.catalogId.value = SelectedCatalog != null ? SelectedCatalog.Id : throw new Exception("SelectedCatalog can't be null");

                variables.filter.priceListId = new ExpandoObject();
                variables.filter.priceListId.@operator = "=";
                variables.filter.priceListId.value = SelectedPriceList.Id;
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
                IGenericDataAccess<PriceListDetailGraphQLModel>.PageResponseType result = await PriceListDetailService.GetPage(query, variables);
                PriceListDetail = [.. Context.AutoMapper.Map<ObservableCollection<PriceListDetailDTO>>(result.PageResponse.Rows)];
                foreach(var item in PriceListDetail)
                {
                    item.Context = this;
                }
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
        }

        private void LoadItemTypes()
        {
            _isUpdating = true;
            ItemsTypes = [.. _selectedCatalog.ItemsTypes];
            ItemsTypes.Insert(0, new ItemTypeGraphQLModel { Id = 0, Name = "<< MOSTRAR TODOS LOS TIPOS DE PRODUCTOS >>" });
            SelectedItemType = ItemsTypes.First(x => x.Id == 0);
            LoadItemCategories();
            LoadItemSubCategories();
            _isUpdating = false;
        }

        private void LoadItemCategories()
        {
            _isUpdating = true;
            if (SelectedItemType.Id != 0)
            {
                ItemsCategories = new ObservableCollection<ItemCategoryGraphQLModel>(_selectedItemType.ItemsCategories);
                ItemsCategories.Insert(0, new ItemCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS CATEGORÍAS DE PRODUCTOS >>" });
                SelectedItemCategory = ItemsCategories.First(x => x.Id == 0);
            }
            else
            {
                ItemsCategories.Clear();
                SelectedItemCategory = new ItemCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS CATEGORÍAS DE PRODUCTOS >>" };
            }
            NotifyOfPropertyChange(nameof(CanShowItemsCategories));
            NotifyOfPropertyChange(nameof(CanShowItemsSubCategories));
            _isUpdating = false;
        }

        private void LoadItemSubCategories()
        {
            _isUpdating = true;
            if (SelectedItemCategory != null && SelectedItemCategory.Id != 0)
            {
                if (SelectedItemCategory.ItemsSubCategories != null)
                    ItemsSubCategories = [.. SelectedItemCategory.ItemsSubCategories];
                else
                    ItemsSubCategories = [];
                ItemsSubCategories.Insert(0, new ItemSubCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS SUBCATEGORÍAS DE PRODUCTOS >>" });
                SelectedItemSubCategory = ItemsSubCategories.First(x => x.Id == 0);
            }
            else
            {
                ItemsSubCategories.Clear();
                SelectedItemSubCategory = new ItemSubCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS SUBCATEGORÍAS DE PRODUCTOS >>" };
            }
            NotifyOfPropertyChange(nameof(CanShowItemsSubCategories));
            _isUpdating = false;
        }


        private ObservableCollection<PriceListDetailDTO> ModifiedProduct { get; set; } = [];
        public void AddModifiedProduct(PriceListDetailDTO priceListDetail)
        {
            priceListDetail.Status = OperationStatus.Pending;

            var operation = new PriceListUpdateOperation
            {
                CatalogItemId = priceListDetail.CatalogItem.Id,
                NewPrice = priceListDetail.Price,
                NewDiscountMargin = priceListDetail.DiscountMargin,
                NewMinimumPrice = priceListDetail.MinimumPrice,
                NewProfitMargin = priceListDetail.ProfitMargin,
                PriceListId = SelectedPriceList.Id,
                ItemName = priceListDetail.CatalogItem.Name
            };

            // Guardar el mapeo de operación a ítem
            _operationItemMapping[operation.OperationId] = priceListDetail.CatalogItem.Id;

            // Encolar la operación
            _ = BackgroundQueueService.EnqueueOperationAsync(operation);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                Context.EventAggregator.Unsubscribe(this);
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        private ICommand _testCommand;

        public ICommand TestCommand
        {
            get 
            {
                if (_testCommand is null) _testCommand = new DelegateCommand(Test);
                return _testCommand; 
            }
        }


        public void Test()
        {
            foreach(var item in PriceListDetail)
            {
                item.Price = 25000;
                var operation = new PriceListUpdateOperation
                {
                    CatalogItemId = item.CatalogItem.Id,
                    NewPrice = item.Price,
                    NewDiscountMargin = item.DiscountMargin,
                    NewMinimumPrice = item.MinimumPrice,
                    NewProfitMargin = item.ProfitMargin,
                    PriceListId = SelectedPriceList.Id
                };

                _operationItemMapping[operation.OperationId] = item.CatalogItem.Id;
                _ = BackgroundQueueService.EnqueueOperationAsync(operation);
            }
        }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);

            _ = Execute.OnUIThreadAsync(async () =>
            {
                try
                {
                    await InitializeAsync();
                    await LoadPriceList();
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
        }

        public async void SaveChangesAsync(object state)
        {
            if (ModifiedProduct.Count == 0) return;

        }

        public Task HandleAsync(OperationCompletedMessage message, CancellationToken cancellationToken)
        {
            // Verificar si tenemos el mapeo para esta operación
            if (_operationItemMapping.TryGetValue(message.OperationId, out int itemId))
            {
                // Buscar el ítem correspondiente
                var item = PriceListDetail.FirstOrDefault(i => i.CatalogItem.Id == itemId);
                if (item != null)
                {
                    // Actualizar estado visual
                    item.Status = message.Success ? OperationStatus.Saved : OperationStatus.Failed;

                    // Mostrar notificación
                    //if (message.Success)
                    //{
                    //    _notificationService.ShowSuccess($"Precio actualizado: {message.DisplayName}");
                    //}
                    //else
                    //{
                    //    _notificationService.ShowError(
                    //        $"Error al actualizar: {message.DisplayName}",
                    //        $"Error: {message.Exception?.Message ?? "Desconocido"}");
                    //}

                    // Limpiamos el mapeo
                    _operationItemMapping.Remove(message.OperationId);
                }
            }

            return Task.CompletedTask;
        }

        public PriceListMasterViewModel(PriceListViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnPublishedThread(this);
            _notificationService = IoC.Get<Helpers.Services.INotificationService>();
        }
    }

    public class PriceListUpdateOperation : IDataOperation
    {
        // Propiedades de la operación
        public int CatalogItemId { get; set; }
        public decimal NewPrice { get; set; }
        public decimal NewDiscountMargin { get; set; }
        public decimal NewProfitMargin { get; set; }
        public decimal NewMinimumPrice { get; set; }
        public int PriceListId { get; set; }
        public string ItemName { get; set; } = "";

        // Esta query sería solo para referencia, siempre se usará la BatchQuery para operaciones
        public string Query => @"
        mutation($data:UpdatePriceListInput!){
          UpdateResponse: updatePriceListDetail(data: $data){
            catalogItem{
              id
              name
              code
            }
            measurement{
              id
              abbreviation
            }
            cost
            profitMargin
            price
            minimumPrice
            discountMargin
          }
        }";

        // Variables para una operación individual (no se usará directamente)
        public object Variables => new
        {
            data = new
            {
                catalogItemId = CatalogItemId,
                price = NewPrice,
                discountMargin = NewDiscountMargin,
                profitMargin = NewProfitMargin,
                minimumPrice = NewMinimumPrice,
                priceListId = PriceListId
            }
        };

        public Type ResponseType => typeof(PriceListDetailGraphQLModel);
        public string GenericDataAccessMethod => "SendMutationList"; // Siempre usamos el método de lotes
        public Guid OperationId { get; set; } = Guid.NewGuid();
        public string DisplayName => !string.IsNullOrEmpty(ItemName) ? ItemName : $"Producto #{CatalogItemId}";
        public int Id => CatalogItemId;

        public BatchOperationInfo GetBatchInfo()
        {
            return new BatchOperationInfo
            {
                // Query específica para operación en lote
                BatchQuery = @"
                mutation ($data: [UpdatePriceListDetailInput!]!) {
                  ListResponse: updatePriceListDetailList(data: $data) {
                    catalogItem {
                      id
                      name
                      code
                    }
                    measurement {
                      id
                      abbreviation
                    }
                    cost
                    profitMargin
                    price
                    minimumPrice
                    discountMargin
                  }
                }",

                // Extraer cada elemento para el lote - ahora es más simple
                ExtractBatchItem = (variables) =>
                {
                    // Ahora extraemos directamente el objeto data que contiene catalogItemId
                    var variablesType = variables.GetType();
                    var dataProp = variablesType.GetProperty("data");
                    return dataProp.GetValue(variables);
                },

                // Construir las variables para el lote completo
                BuildBatchVariables = (items) =>
                {
                    return new
                    {
                        data = items
                    };
                }
            };
        }
    }
}
