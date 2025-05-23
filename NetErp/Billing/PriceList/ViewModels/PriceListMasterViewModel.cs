﻿using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.Books;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace NetErp.Billing.PriceList.ViewModels
{
    public class PriceListMasterViewModel : Screen, IHandle<OperationCompletedMessage>  
    {
        //Propiedad para controlar la ejecución de algunos métodos de carga
        private bool _isUpdating = false;
        private readonly Dictionary<Guid, int> _operationItemMapping = new Dictionary<Guid, int>();
        private readonly Helpers.Services.INotificationService _notificationService;
        public IGenericDataAccess<PriceListDetailGraphQLModel> PriceListDetailService { get; set; } = IoC.Get<IGenericDataAccess<PriceListDetailGraphQLModel>>();
        public IBackgroundQueueService BackgroundQueueService { get; set; } = IoC.Get<IBackgroundQueueService>();
        public IPriceListCalculatorFactory CalculatorFactory { get; set; } = IoC.Get<IPriceListCalculatorFactory>();
        public PriceListViewModel Context { get; set; }

        Helpers.IDialogService _dialogService = IoC.Get<Helpers.IDialogService>();
        public string MaskN2 { get; set; } = "n2";

        public string MaskN5 { get; set; } = "n5";

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
                        if (IsInitialized) _ = Execute.OnUIThreadAsync(async () =>
                        {
                            IsBusy = true;
                            await LoadPriceList();
                            IsBusy = false;
                        });
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
                        if (IsInitialized) _ = Execute.OnUIThreadAsync(async () =>
                        {
                            IsBusy = true;
                            await LoadPriceList();
                            IsBusy = false;
                        });
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
                        if (IsInitialized) _ = Execute.OnUIThreadAsync(async () =>
                        {
                            IsBusy = true;
                            await LoadPriceList();
                            IsBusy = false;
                        });
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
                        if (IsInitialized) _ = Execute.OnUIThreadAsync(async () =>
                        {
                            IsBusy = true;
                            await LoadPriceList();
                            IsBusy = false;
                        });
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
                    NotifyOfPropertyChange(nameof(CostByStorageInformation));
                    LoadItemTypes();
                    if (IsInitialized) _ = Execute.OnUIThreadAsync(async () =>
                    {
                        IsBusy = true;
                        await LoadPriceList();
                        IsBusy = false;
                    });
                    FilterSearch = "";
                }
            }
        }

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
            var viewModel = new CreatePriceListModalViewModel<PriceListGraphQLModel>(_dialogService);
            await viewModel.InitializeAsync();
            await _dialogService.ShowDialogAsync(viewModel, "Creación de lista de precios");
        }

        private ICommand _updatePriceListCommand;
        public ICommand UpdatePriceListCommand
        {
            get
            {
                if (_updatePriceListCommand is null) _updatePriceListCommand = new AsyncCommand(UpdatePriceListAsync);
                return _updatePriceListCommand;
            }
        }

        public async Task UpdatePriceListAsync()
        {
            var viewModel = new UpdatePriceListModalViewModel<PriceListGraphQLModel>(_dialogService, Context.AutoMapper);
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
            viewModel.SelectedStorage = SelectedPriceList.Storage is null ? viewModel.Storages.FirstOrDefault(x => x.Id == 0) : viewModel.Storages.FirstOrDefault(x => x.Id == SelectedPriceList.Storage.Id);
            foreach(PaymentMethodGraphQLModel item in SelectedPriceList.PaymentMethods)
            {
                PaymentMethodPriceListDTO paymentMethod = viewModel.PaymentMethods.FirstOrDefault(x => x.Id == item.Id);
                if (paymentMethod != null)
                {
                    paymentMethod.IsChecked = false;
                }
            }
            viewModel.SelectedListUpdateBehaviorOnCostChange = SelectedPriceList.ListUpdateBehaviorOnCostChange;
            viewModel.IsActive = SelectedPriceList.IsActive;
            await _dialogService.ShowDialogAsync(viewModel, "Configuración de lista de precios");
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
                            isTaxable
                            priceListIncludeTax
                            useAlternativeFormula
                            editablePrice
                            autoApplyDiscount
                            listUpdateBehaviorOnCostChange
                            isPublic
                            isActive
                            storage{
                                id
                                name
                                }
                            paymentMethods{
                                id
                                name
                                abbreviation
                            }
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
                // Iniciar cronometro
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string query = @"
                        query($filter: PriceListDetailFilterInput){
                          PageResponse: priceListDetailPage(filter: $filter){
                            count
                            rows{
                              catalogItem{
                                id
                                name
                                reference
                                stock{
                                    storage{
                                        id
                                        name
                                    }
                                cost
                                quantity
                                }
                                accountingGroup{
                                    sellTaxes{
                                        margin
                                        formula
                                        alternativeFormula
                                        taxType{
                                            prefix
                                        }
                                    }
                                }
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

                IGenericDataAccess<PriceListDetailGraphQLModel>.PageResponseType result = await PriceListDetailService.GetPage(query, variables);
                TotalCount = result.PageResponse.Count;
                PriceListDetail = [.. Context.AutoMapper.Map<ObservableCollection<PriceListDetailDTO>>(result.PageResponse.Rows)];

                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

                foreach (var item in PriceListDetail)
                {
                    item.Context = this;
                    item.IVA = GetIvaValue(item.CatalogItem.AccountingGroup.SellTaxes);
                    item.Profit = GetProfit(item);
                }

            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
        }

        private decimal GetProfit(PriceListDetailDTO item)
        {
            if (item.Cost == 0) return 0;
            decimal priceWithoutDiscount = (item.Cost / (1 - item.ProfitMargin / 100));
            decimal profit = priceWithoutDiscount - item.Cost;
            return profit;
        }

        private decimal GetIvaValue(IEnumerable<TaxGraphQLModel> sellTaxes) 
        {
            if(sellTaxes == null || !sellTaxes.Any()) return -1;

            var ivaTax = sellTaxes.FirstOrDefault(x => x.TaxType != null && x.TaxType.Prefix == "IVA");

            if(ivaTax is null) return -1;

            return ivaTax.Margin;
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
        public void AddModifiedProduct(PriceListDetailDTO priceListDetail, string modifiedProperty)
        {
            IPriceListCalculator calculator = CalculatorFactory.GetCalculator(SelectedPriceList.UseAlternativeFormula);
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

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);

            _ = Execute.OnUIThreadAsync(async () =>
            {
                try
                {
                    MainIsBusy = true;
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
                finally
                {
                    MainIsBusy = false;
                }
            });
            _ = this.SetFocus(nameof(FilterSearch));
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
            Messenger.Default.Register<ReturnedDataFromCreatePriceListModalViewMessage<PriceListGraphQLModel>>(this, "CreatePriceList", false, OnCreatePriceList);
            Messenger.Default.Register<ReturnedDataFromUpdatePriceListModalViewMessage<PriceListGraphQLModel>>(this, "UpdatePriceList", false, OnUpdatePriceList);
            _notificationService = IoC.Get<Helpers.Services.INotificationService>();
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
                    SelectedPriceList = existingPriceList;
                }
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
