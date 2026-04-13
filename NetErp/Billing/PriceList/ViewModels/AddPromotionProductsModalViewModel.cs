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
using NetErp.Helpers.GraphQLQueryBuilder;
using Models.Global;
using NetErp.Helpers.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.VisualStudio.Threading;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Billing.PriceList.ViewModels
{
    public class AddPromotionProductsModalViewModel : Screen
    {
        private readonly Helpers.IDialogService _dialogService;
        private readonly IRepository<PriceListItemGraphQLModel> _priceListItemService;
        private readonly IRepository<ItemGraphQLModel> _itemService;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private const int BatchSize = 50;
        public PriceListViewModel Context { get; set; }

        public AddPromotionProductsModalViewModel(
            PriceListViewModel context,
            Helpers.IDialogService dialogService,
            IRepository<PriceListItemGraphQLModel> priceListItemService,
            IRepository<ItemGraphQLModel> itemService,
            JoinableTaskFactory joinableTaskFactory)
        {
            Context = context;
            _dialogService = dialogService;
            _priceListItemService = priceListItemService;
            _itemService = itemService;
            _joinableTaskFactory = joinableTaskFactory;

            Items.CollectionChanged += (s, e) =>
            {
                NotifyOfPropertyChange(nameof(ItemsHeaderIsChecked));
            };
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                _cascadeCancellation?.Cancel();
                _cascadeCancellation?.Dispose();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        public new bool IsInitialized { get; set; } = false;

        public int PromotionId { get; set; } = 0;

        #region SelectedItemIds — local in-memory set of checked item IDs

        public HashSet<int> SelectedItemIds { get; } = [];

        public int SelectedCount => SelectedItemIds.Count;

        /// <summary>
        /// Called by PromotionCatalogItemDTO when IsChecked changes.
        /// Adds or removes the item from the local selection set.
        /// </summary>
        public void ToggleItemSelection(int itemId, bool isChecked)
        {
            if (isChecked)
                SelectedItemIds.Add(itemId);
            else
                SelectedItemIds.Remove(itemId);

            NotifyOfPropertyChange(nameof(SelectedCount));
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion

        #region Items Grid

        public ObservableCollection<PromotionCatalogItemDTO> Items
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Items));
                }
            }
        } = [];

        public PromotionCatalogItemDTO? SelectedItem
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                }
            }
        }

        #endregion

        #region Cascade Dropdowns

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

        #endregion

        #region Cascade Build Methods

        // Flag to prevent cascading reload operations during internal updates
        private bool _isUpdating = false;
        private CancellationTokenSource _cascadeCancellation = new();

        private async Task ReloadDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                if (cancellationToken.IsCancellationRequested) return;

                IsBusy = true;
                await LoadItemsAsync();
                IsBusy = false;
            }
            catch (OperationCanceledException)
            {
                // Operation cancelled, do nothing
            }
        }

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
                ItemSubCategories.Clear();
                _selectedItemSubCategory = new ItemSubCategoryGraphQLModel { Id = 0, Name = "<< MOSTRAR TODAS LAS SUBCATEGORÍAS DE PRODUCTOS >>" };
                NotifyOfPropertyChange(nameof(SelectedItemSubCategory));
            }

            NotifyOfPropertyChange(nameof(CanShowItemSubCategories));
            _isUpdating = false;
        }

        #endregion

        #region Busy / Filter / Header Check

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
                        if (IsInitialized) _ = LoadItemsAsync();
                    }
                }
            }
        } = "";

        public bool ItemsHeaderIsChecked
        {
            get
            {
                if (Items is null || Items.Count == 0) return false;
                return field;
            }
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ItemsHeaderIsChecked));
                    foreach (var item in Items)
                    {
                        item.IsChecked = value;
                    }
                }
            }
        }

        #endregion

        #region CanSave

        public bool CanSave => SelectedItemIds.Count > 0;

        #endregion

        #region Load Items

        public async Task LoadItemsAsync()
        {
            try
            {
                IsBusy = true;
                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadItemsQuery.Value;

                dynamic filters = new ExpandoObject();
                filters.isActive = true;
                if (SelectedItemSubCategory != null && SelectedItemSubCategory.Id != 0)
                    filters.subCategoryId = SelectedItemSubCategory.Id;
                if (!string.IsNullOrEmpty(FilterSearch))
                    filters.matching = FilterSearch.Trim().RemoveExtraSpaces();

                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                var result = await _itemService.GetPageAsync(query, variables);
                Items = new ObservableCollection<PromotionCatalogItemDTO>(Context.AutoMapper.Map<ObservableCollection<PromotionCatalogItemDTO>>(result.Entries));
                TotalCount = result.TotalEntries;

                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

                foreach (var item in Items)
                {
                    item.Context = this;
                    item.IsChecked = SelectedItemIds.Contains(item.Id);
                }
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{nameof(LoadItemsAsync)} \r\n{ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Initialize

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

        #endregion

        #region Save / Cancel Commands

        private ICommand? _saveCommand;

        public ICommand SaveCommand
        {
            get
            {
                _saveCommand ??= new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        public async Task SaveAsync()
        {
            if (SelectedItemIds.Count == 0) return;

            var confirmation = ThemedMessageBox.Show("Confirme...",
                $"¿Confirma agregar {SelectedItemIds.Count} productos a la promoción?",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmation != MessageBoxResult.Yes) return;

            try
            {
                MainIsBusy = true;

                var allItems = SelectedItemIds.Select(id => new
                {
                    itemId = id,
                    price = 0m,
                    discountMargin = 0m,
                    profitMargin = 0m,
                    minimumPrice = 0m
                }).ToList();

                string query = _batchUpdatePricesQuery.Value;
                int totalAdded = 0;
                int totalFailed = 0;
                var failedMessages = new List<string>();

                // Batching secuencial — bloques de BatchSize para no saturar el pool de conexiones
                for (int i = 0; i < allItems.Count; i += BatchSize)
                {
                    var batch = allItems.Skip(i).Take(BatchSize).ToList();
                    var variables = new { input = new { priceListId = PromotionId, items = batch } };

                    var batchResult = await _priceListItemService.BatchAsync<BatchResultGraphQLModel>(query, variables);

                    if (batchResult.Success)
                    {
                        totalAdded += batch.Count;
                    }
                    else
                    {
                        totalFailed += batch.Count;
                        failedMessages.Add(batchResult.Message ?? "Error desconocido");
                    }
                }

                SelectedItemIds.Clear();
                NotifyOfPropertyChange(nameof(SelectedCount));
                NotifyOfPropertyChange(nameof(CanSave));

                if (totalFailed > 0 && totalAdded > 0)
                {
                    ThemedMessageBox.Show(
                        title: "Procesamiento parcial",
                        text: $"Productos agregados: {totalAdded}\n" +
                              $"Productos con error: {totalFailed}\n\n" +
                              $"Detalle: {string.Join("\n", failedMessages)}",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Warning);
                }
                else if (totalFailed > 0 && totalAdded == 0)
                {
                    ThemedMessageBox.Show(
                        title: "Error en el procesamiento",
                        text: $"No se pudo agregar ningún producto.\n\n" +
                              $"Detalle: {string.Join("\n", failedMessages)}",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return;
                }

                await Context.EventAggregator.PublishOnUIThreadAsync(
                    new PromotionTempRecordResponseMessage
                    {
                        Response = new SuccessResponseModel { Success = true, Message = $"{totalAdded} productos agregados correctamente" }
                    });
                await _dialogService.CloseDialogAsync(this, true);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{nameof(SaveAsync)} \r\n{ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
            finally
            {
                MainIsBusy = false;
            }
        }

        private ICommand? _cancelCommand;

        public ICommand CancelCommand
        {
            get
            {
                _cancelCommand ??= new AsyncCommand(CancelAsync);
                return _cancelCommand;
            }
        }

        public async Task CancelAsync()
        {
            await _dialogService.CloseDialogAsync(this, true);
        }

        #endregion

        #region CanCloseAsync

        public override async Task<bool> CanCloseAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (SelectedItemIds.Count == 0)
                    return await base.CanCloseAsync(cancellationToken);

                var result = ThemedMessageBox.Show("Confirmar cierre",
                    $"Tiene {SelectedItemIds.Count} productos seleccionados sin guardar.\n¿Desea guardar los cambios antes de cerrar?",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                    return false;

                if (result == MessageBoxResult.Yes)
                    await SaveAsync();

                // If No, just discard selections and close
                if (result == MessageBoxResult.No)
                {
                    SelectedItemIds.Clear();
                    NotifyOfPropertyChange(nameof(SelectedCount));
                    NotifyOfPropertyChange(nameof(CanSave));
                }

                return await base.CanCloseAsync(cancellationToken);
            }
            catch (AsyncException ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{nameof(CanCloseAsync)} \r\n{ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                return false;
            }
        }

        #endregion

        #region Pagination

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
        } = "";

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
        } = 1;

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
        } = 50;

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
        } = 0;

        private ICommand? _paginationCommand;

        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(ExecuteChangeIndexPaginationAsync, CanExecuteChangeIndexPagination);
                return _paginationCommand;
            }
        }

        private async Task ExecuteChangeIndexPaginationAsync()
        {
            IsBusy = true;
            await LoadItemsAsync();
            IsBusy = false;
        }

        private bool CanExecuteChangeIndexPagination()
        {
            return true;
        }

        #endregion

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

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadItemsQuery = new(() =>
        {
            var fields = FieldSpec<PageType<ItemGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Reference)
                    .Field(e => e.Code))
                .Build();

            var filterParam = new GraphQLQueryParameter("filters", "ItemFilters");
            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("itemsPage", [filterParam, paginationParam], fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<string> _batchUpdatePricesQuery = new(() => @"
            mutation ($input: BatchUpdatePriceListPricesInput!) {
              SingleItemResponse: batchUpdatePriceListPrices(input: $input) {
                success
                message
                totalAffected
                affectedIds
                errors { fields message }
              }
            }");

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
