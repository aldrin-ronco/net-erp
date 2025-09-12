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
    public class PriceListMasterViewModel : Screen, IHandle<OperationCompletedMessage>, IHandle<CriticalSystemErrorMessage>  
    {
        // Flag to prevent cascading reload operations during internal updates
        private bool _isUpdating = false;
        private readonly Dictionary<Guid, int> _operationItemMapping = new Dictionary<Guid, int>();
        
        // Dependency injection fields
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<PriceListDetailGraphQLModel> _priceListDetailService;
        private readonly IBackgroundQueueService _backgroundQueueService;
        private readonly IPriceListCalculatorFactory _calculatorFactory;
        private readonly Helpers.IDialogService _dialogService;
        private readonly IRepository<PriceListGraphQLModel> _priceListService;

        //Service necesario en ventanas modales
        private readonly IRepository<StorageGraphQLModel> _storageService;
        
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
                    // Solo ejecutamos la busqueda si esta vacio el filtro o si hay por lo menos 3 caracteres digitados
                    if (string.IsNullOrEmpty(value) || value.Length >= 2)
                    {
                        PageIndex = 1;
                        if (IsInitialized) _ =  Task.Run(async () =>
                        {
                            IsBusy = true;
                            await LoadPriceList();
                            IsBusy = false;
                            _ = this.SetFocus(nameof(FilterSearch));
                        });
                    }
                }
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
                        _cascadeCancellation = new CancellationTokenSource();
                        
                        LoadItemTypes();
                        if (IsInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
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
                    if (!_isUpdating && value != null)
                    {
                        _cascadeCancellation?.Cancel();
                        _cascadeCancellation = new CancellationTokenSource();
                        
                        LoadItemCategories();
                        if (IsInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
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
                    if (!_isUpdating && value != null)
                    {
                        _cascadeCancellation?.Cancel();
                        _cascadeCancellation = new CancellationTokenSource();
                        
                        LoadItemSubCategories();
                        if (IsInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
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
                    NotifyOfPropertyChange(nameof(IsPriceList));
                    NotifyOfPropertyChange(nameof(CanCreatePromotion));
                    
                    _cascadeCancellation?.Cancel();
                    _cascadeCancellation = new CancellationTokenSource();
                    
                    if (value != null)
                    {
                        LoadItemTypes();
                        FilterSearch = "";
                        if (IsInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
                    }
                }
            }
        }

        public bool SelectedPriceListIsNotActive => SelectedPriceList != null && !SelectedPriceList.IsActive;

        private PriceListDetailDTO? _selectedPriceListDetail;

        public PriceListDetailDTO? SelectedPriceListDetail
        {
            get { return _selectedPriceListDetail; }
            set 
            {
                if (_selectedPriceListDetail != value)
                {
                    _selectedPriceListDetail = value;
                    NotifyOfPropertyChange(nameof(SelectedPriceListDetail));
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
            get { return SelectedPriceListDetail != null && SelectedPriceListDetail.CatalogItem.Stock.Any(); }
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
                var viewModel = new CreatePriceListModalViewModel<PriceListGraphQLModel>(_dialogService, _priceListService, _storageService);
                await viewModel.InitializeAsync();
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
                    await Task.Run(() => Context.ActivateUpdatePromotionViewAsync(SelectedPriceList));
                    MainIsBusy = false;
                    return;
                }
                var viewModel = new UpdatePriceListModalViewModel<PriceListGraphQLModel>(_dialogService, Context.AutoMapper, _priceListService, _storageService);
                await viewModel.InitializeAsync();
                viewModel.SelectedPriceListId = SelectedPriceList.Id;
                viewModel.Name = SelectedPriceList.Name;
                viewModel.IsTaxable = SelectedPriceList.IsTaxable;
                viewModel.PriceListIncludeTax = SelectedPriceList.PriceListIncludeTax;
                viewModel.UseAlternativeFormula = SelectedPriceList.UseAlternativeFormula;
                viewModel.SelectedFormula = SelectedPriceList.UseAlternativeFormula ? "A" : "D";
                viewModel.EditablePrice = SelectedPriceList.EditablePrice;
                viewModel.AutoApplyDiscount = SelectedPriceList.AutoApplyDiscount;
                viewModel.IsPublic = SelectedPriceList.IsPublic;
                viewModel.SelectedStorage = SelectedPriceList.Storage is null ? viewModel.Storages.FirstOrDefault(x => x.Id == 0) ?? throw new Exception($"Invalid null reference") : viewModel.Storages.FirstOrDefault(x => x.Id == SelectedPriceList.Storage.Id) ?? throw new Exception("Invalid null reference");
                foreach(PaymentMethodGraphQLModel item in SelectedPriceList.PaymentMethods)
                {
                    PaymentMethodPriceListDTO paymentMethod = viewModel.PaymentMethods.FirstOrDefault(x => x.Id == item.Id) ?? throw new Exception("Invalid nullreference");
                    if (paymentMethod != null)
                    {
                        paymentMethod.IsChecked = false;
                    }
                }
                viewModel.SelectedListUpdateBehaviorOnCostChange = SelectedPriceList.ListUpdateBehaviorOnCostChange;
                viewModel.IsActive = SelectedPriceList.IsActive;
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
                MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {SelectedPriceList.Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
                IsBusy = true;
                int id = SelectedPriceList.Id;
                string query;

                query = @"
                    query($id: Int!){
                      CanDeleteModel: canDeletePriceList(id: $id){
                        canDelete
                        message
                      }
                    }";

                dynamic variables = new ExpandoObject();
                variables.id = id;

                var validation = await _priceListService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    query = @"
                    mutation($id: Int!){
                      DeleteResponse: deletePriceList(id: $id){
                        id
                      }
                    }";
                    variables = new ExpandoObject();
                    variables.id = id;
                    PriceListGraphQLModel deletedPriceList = await _priceListService.DeleteAsync(query, variables);
                    Messenger.Default.Send(message: new PriceListDeleteMessage() { DeletedPriceList = deletedPriceList });
                    return;
                }
                else
                {
                    query = @"
                    mutation($id: Int!, $data: UpdatePriceListInput!){
                      UpdateResponse: updatePriceList(id: $id, data: $data){
                        id
                      }
                    }";

                    variables = new ExpandoObject();
                    variables.id = id;
                    variables.data = new ExpandoObject();
                    variables.data.Archived = true;
                    PriceListGraphQLModel deletedPriceList = await _priceListService.UpdateAsync(query, variables);
                    Messenger.Default.Send(message: new PriceListDeleteMessage() { DeletedPriceList = deletedPriceList });
                    return;
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
            var viewModel = new CreatePromotionModalViewModel<PriceListGraphQLModel>(_dialogService, SelectedPriceList, _priceListService);
            await _dialogService.ShowDialogAsync(viewModel, "Creación de promociones");
        }

        public bool CanCreatePromotion => SelectedPriceList != null && SelectedPriceList.IsActive && SelectedPriceList.Parent == null;

        public async Task InitializeAsync()
        {
            try
            {
                _isUpdating = true;
                string query = @"
                    query ($priceListFilter: PriceListFilterInput) {
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
                      priceLists(filter: $priceListFilter) {
                        id
                        name
                        isTaxable
                        priceListIncludeTax
                        useAlternativeFormula
                        editablePrice
                        autoApplyDiscount
                        listUpdateBehaviorOnCostChange
                        isPublic
                        isActive
                        startDate
                        endDate
                        archived
                        parent{
                            id
                            name
                        }
                        storage {
                          id
                          name
                        }
                        paymentMethods {
                          id
                          name
                          abbreviation
                        }
                      }
                    }";

                dynamic variables = new ExpandoObject();
                variables.priceListFilter = new ExpandoObject();
                variables.priceListFilter.archived = new ExpandoObject();
                variables.priceListFilter.archived.@operator = "=";
                variables.priceListFilter.archived.value = false;

                PriceListDataContext result = await _priceListDetailService.GetDataContextAsync<PriceListDataContext>(query, variables);


                Catalogs = new ObservableCollection<CatalogGraphQLModel>(result.Catalogs);
                SelectedCatalog = Catalogs.FirstOrDefault() ?? throw new Exception("SelectedCatalog can't be null");
                PriceLists = new ObservableCollection<PriceListGraphQLModel>(result.PriceLists);
                NotifyOfPropertyChange(nameof(ShowAllControls));
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

        public bool ShowAllControls
        {
            get
            {
                return PriceLists != null && PriceLists.Count > 0;
            }
        }

        public new bool IsInitialized { get; set; } = false;

        public async Task LoadPriceList()
        {
            try
            {
                if (ShowAllControls is false) return;
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
                        reference
                        stock {
                          storage {
                            id
                            name
                          }
                          cost
                          quantity
                        }
                        accountingGroup {
                          sellTax1 {
                            margin
                            formula
                            alternativeFormula
                            taxType {
                              prefix
                            }
                          }
                          sellTax2 {
                            margin
                            formula
                            alternativeFormula
                            taxType {
                              prefix
                            }
                          }
                        }
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
                      quantity
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

                variables.filter.filterSearch = new ExpandoObject();
                variables.filter.filterSearch.@operator = "like";
                variables.filter.filterSearch.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();
                variables.filter.filterSearch.exclude = true;

                PageType<PriceListDetailGraphQLModel> result = await _priceListDetailService.GetPageAsync(query, variables);
                TotalCount = result.Count;
                PriceListDetail = [.. Context.AutoMapper.Map<ObservableCollection<PriceListDetailDTO>>(result.Rows)];

                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

                foreach (var item in PriceListDetail)
                {
                    item.Context = this;
                    item.IVA = GetIvaValue(item.CatalogItem.AccountingGroup.SellTax1, item.CatalogItem.AccountingGroup.SellTax2);
                    item.Profit = GetProfit(item);
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

        private decimal GetProfit(PriceListDetailDTO item)
        {
            if (item.Cost == 0) return 0;
            decimal priceWithoutDiscount = (item.Cost / (1 - item.ProfitMargin / 100));
            decimal profit = priceWithoutDiscount - item.Cost;
            return profit;
        }

        private decimal GetIvaValue(TaxGraphQLModel? tax1, TaxGraphQLModel? tax2) 
        {
            if(tax1 is null && tax2 is null) return -1;

            if(tax1 != null && tax1.TaxType.Prefix == "IVA") return tax1.Margin;
            if(tax2 != null && tax2.TaxType.Prefix == "IVA") return tax2.Margin;

            return -1; // No IVA found
        }

        private async Task ReloadDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Execute.OnUIThreadAsync(async () =>
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    IsBusy = true;
                    await LoadPriceList();
                    IsBusy = false;
                });
            }
            catch (OperationCanceledException)
            {

            } 
        }

        private void LoadItemTypes()
        {
            if (SelectedCatalog?.ItemsTypes is null) return;

            _isUpdating = true;
            ItemsTypes = new ObservableCollection<ItemTypeGraphQLModel>(SelectedCatalog.ItemsTypes);
            ItemsTypes.Insert(0, new ItemTypeGraphQLModel { Id = 0, Name = "<< MOSTRAR TODOS LOS TIPOS DE PRODUCTOS >>" });
            SelectedItemType = ItemsTypes.FirstOrDefault(x => x.Id == 0) ?? throw new Exception("SelectedItemType can't be null");
            LoadItemCategories();
            _isUpdating = false;
        }

        private void LoadItemCategories()
        {
            _isUpdating = true;
            
            if (SelectedItemType != null && SelectedItemType.Id != 0 && SelectedItemType.ItemsCategories != null)
            {
                ItemsCategories = new ObservableCollection<ItemCategoryGraphQLModel>(SelectedItemType.ItemsCategories);
                ItemsCategories.Insert(0, new ItemCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS CATEGORÍAS DE PRODUCTOS >>" });
                SelectedItemCategory = ItemsCategories.FirstOrDefault(x => x.Id == 0) ?? throw new Exception("SelectedItemCategory can't be null");
            }
            else
            {
                // Reset categories when ItemType is "Show All" (Id = 0)
                ItemsCategories.Clear();
                SelectedItemCategory = new ItemCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS CATEGORÍAS DE PRODUCTOS >>" };
            }
            
            LoadItemSubCategories();
            NotifyOfPropertyChange(nameof(CanShowItemsCategories));
            NotifyOfPropertyChange(nameof(CanShowItemsSubCategories));
            _isUpdating = false;
        }

        private void LoadItemSubCategories()
        {
            _isUpdating = true;
            
            if (SelectedItemCategory != null && SelectedItemCategory.Id != 0 && SelectedItemCategory.ItemsSubCategories != null)
            {
                ItemsSubCategories = new ObservableCollection<ItemSubCategoryGraphQLModel>(SelectedItemCategory.ItemsSubCategories);
                ItemsSubCategories.Insert(0, new ItemSubCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS SUBCATEGORÍAS DE PRODUCTOS >>" });
                SelectedItemSubCategory = ItemsSubCategories.FirstOrDefault(x => x.Id == 0) ?? throw new Exception("SelectedItemSubCategory can't be null");
            }
            else
            {
                // Reset subcategories when Category is "Show All" (Id = 0) or ItemType is "Show All"
                ItemsSubCategories.Clear();
                SelectedItemSubCategory = new ItemSubCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS SUBCATEGORÍAS DE PRODUCTOS >>" };
            }
            
            NotifyOfPropertyChange(nameof(CanShowItemsSubCategories));
            _isUpdating = false;
        }


        private ObservableCollection<PriceListDetailDTO> ModifiedProduct { get; set; } = [];
        public void AddModifiedProduct(PriceListDetailDTO priceListDetail, string modifiedProperty)
        {
            IPriceListCalculator calculator = _calculatorFactory.GetCalculator(SelectedPriceList.UseAlternativeFormula);
            calculator.RecalculateProductValues(priceListDetail, modifiedProperty, SelectedPriceList);
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
            _ = _backgroundQueueService.EnqueueOperationAsync(operation);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                Context.EventAggregator.Unsubscribe(this);
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }


        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.SetFocus(nameof(FilterSearch));
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await Execute.OnUIThreadAsync(async () =>
            {
                try
                {
                    if (IsInitialized && IsActive)
                    {
                        IsBusy = true;
                        await LoadPriceList();
                        IsBusy = false;
                    }
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
            await base.OnActivateAsync(cancellationToken);
        }

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            await Execute.OnUIThreadAsync(async () =>
            {
                try
                {
                    // Verificar si el BackgroundQueueService tiene un error crítico
                    if (_backgroundQueueService.HasCriticalError())
                    {
                        string criticalErrorMessage = _backgroundQueueService.GetCriticalErrorMessage();
                        string userMessage = $"Se ha detectado un error crítico en el sistema que impide continuar.\n\n" +
                                           $"Error: {criticalErrorMessage}\n\n" +
                                           $"Por favor, comuníquese con el área de soporte técnico.";
                        
                        ThemedMessageBox.Show(
                            title: "Error Crítico del Sistema", 
                            text: userMessage,
                            messageBoxButtons: MessageBoxButton.OK, 
                            image: MessageBoxImage.Error
                        );
                        
                        // Bloquear la vista
                        MainIsBusy = true;
                        
                        // Mostrar notificación adicional
                        _notificationService.ShowError("Módulo bloqueado debido a error crítico. Contacte soporte técnico.", "Sistema Bloqueado");
                        
                        return; // No continuar con la inicialización
                    }

                    MainIsBusy = true;
                    await InitializeAsync();
                    await LoadPriceList();
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

            await base.OnInitializeAsync(cancellationToken);
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

                    // Limpiamos el mapeo
                    _operationItemMapping.Remove(message.OperationId);
                }
            }

            return Task.CompletedTask;
        }

        public async Task HandleAsync(CriticalSystemErrorMessage message, CancellationToken cancellationToken)
        {
            // Solo procesar si el error afecta al tipo de datos que maneja este ViewModel
            if (message.ResponseType == typeof(PriceListDetailGraphQLModel))
            {
                // Mostrar mensaje al usuario
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(
                        title: "Error Crítico del Sistema", 
                        text: message.UserMessage,
                        messageBoxButtons: MessageBoxButton.OK, 
                        image: MessageBoxImage.Error
                    );
                    
                    // Bloquear la vista
                    MainIsBusy = true;
                    
                    // Mostrar notificación adicional
                    _notificationService.ShowError("Módulo bloqueado debido a error crítico. Contacte soporte técnico.", "Sistema Bloqueado");
                    
                    return Task.CompletedTask;
                });
            }
        }

        public PriceListMasterViewModel(
            PriceListViewModel context,
            IRepository<PriceListDetailGraphQLModel> priceListDetailService,
            IBackgroundQueueService backgroundQueueService,
            Helpers.Services.INotificationService notificationService,
            IPriceListCalculatorFactory calculatorFactory,
            Helpers.IDialogService dialogService,
            IRepository<PriceListGraphQLModel> priceListService,
            IRepository<StorageGraphQLModel> storageService)
        {
            Context = context;
            _priceListDetailService = priceListDetailService;
            _backgroundQueueService = backgroundQueueService;
            _notificationService = notificationService;
            _calculatorFactory = calculatorFactory;
            _dialogService = dialogService;
            _priceListService = priceListService;
            _storageService = storageService;
            Context.EventAggregator.SubscribeOnPublishedThread(this);
            Messenger.Default.Register<ReturnedDataFromCreatePriceListModalViewMessage<PriceListGraphQLModel>>(this, "CreatePriceList", false, OnCreatePriceList);
            Messenger.Default.Register<ReturnedDataFromUpdatePriceListModalViewMessage<PriceListGraphQLModel>>(this, "UpdatePriceList", false, OnUpdatePriceList);
            Messenger.Default.Register<PriceListDeleteMessage>(this, null, false, OnDeletePriceList);
            Messenger.Default.Register<ReturnedDataFromUpdatePromotionModalViewMessage<PriceListGraphQLModel>>(this, "UpdatePromotion", false, OnUpdatePromotion);
        }

        public void OnUpdatePromotion(ReturnedDataFromUpdatePromotionModalViewMessage<PriceListGraphQLModel> message)
        {
            if (message.ReturnedData is null) return;
            if (message.ReturnedData is PriceListGraphQLModel priceList)
            {
                var existingPriceList = PriceLists.FirstOrDefault(x => x.Id == priceList.Id);
                if (existingPriceList != null)
                {
                    existingPriceList.Name = priceList.Name;
                    existingPriceList.StartDate = priceList.StartDate;
                    existingPriceList.EndDate = priceList.EndDate;
                    var selectedItem = SelectedPriceList;
                    SelectedPriceList = null;
                    SelectedPriceList = selectedItem;
                }
            }
        }

        //TODO : Posible refactorización en en el mensaje de throw exception
        public void OnDeletePriceList(PriceListDeleteMessage message)
        {
            try
            {
                if(message.DeletedPriceList is null) return;
                PriceLists.Remove(PriceLists.FirstOrDefault(x => x.Id == message.DeletedPriceList.Id) ?? throw new Exception("Invalid null reference"));
                SelectedPriceList = PriceLists.FirstOrDefault();
                _notificationService.ShowSuccess("Lista de precios eliminada correctamente", "Éxito");
                NotifyOfPropertyChange(nameof(ShowAllControls));
            }
            catch (Exception ex)
            {
                Execute.OnUIThread(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                });
            }
        }

        public void OnCreatePriceList(ReturnedDataFromCreatePriceListModalViewMessage<PriceListGraphQLModel> message)
        {
            if (message.ReturnedData is null) return;
            if (message.ReturnedData is PriceListGraphQLModel priceList)
            {
                PriceLists.Add(priceList);
                SelectedPriceList = priceList;
                _notificationService.ShowSuccess("Lista de precios creada correctamente", "Éxito");
            }
            else
            {
                _notificationService.ShowError("No se pudo crear la lista de precios", "Error");
            }
            NotifyOfPropertyChange(nameof(ShowAllControls));
        }

        public void OnUpdatePriceList(ReturnedDataFromUpdatePriceListModalViewMessage<PriceListGraphQLModel> message)
        {
            if (message.ReturnedData is null) return;
            if (message.ReturnedData is PriceListGraphQLModel priceList)
            {
                var existingPriceList = PriceLists.FirstOrDefault(x => x.Id == priceList.Id);
                if (existingPriceList != null)
                {
                    existingPriceList.Name = priceList.Name;
                    existingPriceList.IsTaxable = priceList.IsTaxable;
                    existingPriceList.PriceListIncludeTax = priceList.PriceListIncludeTax;
                    existingPriceList.UseAlternativeFormula = priceList.UseAlternativeFormula;
                    existingPriceList.EditablePrice = priceList.EditablePrice;
                    existingPriceList.AutoApplyDiscount = priceList.AutoApplyDiscount;
                    existingPriceList.ListUpdateBehaviorOnCostChange = priceList.ListUpdateBehaviorOnCostChange;
                    existingPriceList.IsPublic = priceList.IsPublic;
                    existingPriceList.IsActive = priceList.IsActive;
                    existingPriceList.PaymentMethods = priceList.PaymentMethods;
                    SelectedPriceList = null;
                    SelectedPriceList = existingPriceList;
                }
                NotifyOfPropertyChange(nameof(SelectedPriceListIsNotActive));
                _notificationService.ShowSuccess("Lista de precios actualizada correctamente", "Éxito");
            }
            else
            {
                _notificationService.ShowError("No se pudo actualizar la lista de precios", "Error");
            }
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
            await LoadPriceList();
            IsBusy = false;
        }

        private bool CanExecuteChangeIndex()
        {
            return true;
        }

        #endregion
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
