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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Billing.PriceList.ViewModels
{
    public class AddPromotionProductsModalViewModel : Screen
    {
        private readonly Helpers.IDialogService _dialogService;
        private readonly IRepository<PriceListItemGraphQLModel> _priceListItemService;
        private readonly IRepository<ItemGraphQLModel> _itemService;
        private readonly CatalogCache _catalogCache;
        private readonly DebouncedAction _searchDebounce;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private const int BatchSize = 50;
        public PriceListViewModel Context { get; set; }

        public AddPromotionProductsModalViewModel(
            PriceListViewModel context,
            Helpers.IDialogService dialogService,
            IRepository<PriceListItemGraphQLModel> priceListItemService,
            IRepository<ItemGraphQLModel> itemService,
            CatalogCache catalogCache,
            DebouncedAction searchDebounce,
            JoinableTaskFactory joinableTaskFactory)
        {
            Context = context;
            _dialogService = dialogService;
            _priceListItemService = priceListItemService;
            _itemService = itemService;
            _catalogCache = catalogCache;
            _searchDebounce = searchDebounce ?? throw new ArgumentNullException(nameof(searchDebounce));
            _joinableTaskFactory = joinableTaskFactory;

            Items.CollectionChanged += OnItemsCollectionChanged!;
        }

        private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyOfPropertyChange(nameof(ItemsHeaderIsChecked));
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                _cascadeCancellation?.Cancel();
                _cascadeCancellation?.Dispose();

                Items.CollectionChanged -= OnItemsCollectionChanged!;

                Items.Clear();
                Catalogs.Clear();
                ItemTypes.Clear();
                ItemCategories.Clear();
                ItemSubCategories.Clear();
                SelectedItemIds.Clear();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = System.Windows.Application.Current.Dispatcher?.BeginInvoke(
                new System.Action(() => SetFocus(() => FilterSearch)),
                DispatcherPriority.Render);
        }

        public bool FilterSearchFocus
        {
            get;
            set
            {
                field = value;
                NotifyOfPropertyChange(nameof(FilterSearchFocus));
            }
        }

        void SetFocus(Expression<Func<object>> propertyExpression)
        {
            string controlName = propertyExpression.GetMemberInfo().Name;
            FilterSearchFocus = false;
            FilterSearchFocus = controlName == nameof(FilterSearch);
        }

        private bool _isInitialized;

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

        public CatalogGraphQLModel? SelectedCatalog
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCatalog));
                    NotifyOfPropertyChange(nameof(CanShowItemTypes));
                    if (!_isUpdating)
                    {
                        _cascadeCancellation?.Cancel();
                        _cascadeCancellation?.Dispose();
                        _cascadeCancellation = new CancellationTokenSource();

                        BuildItemTypes();
                        if (_isInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
                    }
                }
            }
        }

        public bool CanShowItemTypes => SelectedCatalog != null;

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

        public ItemTypeGraphQLModel? SelectedItemType
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedItemType));
                    if (!_isUpdating)
                    {
                        _cascadeCancellation?.Cancel();
                        _cascadeCancellation?.Dispose();
                        _cascadeCancellation = new CancellationTokenSource();

                        BuildItemCategories();
                        if (_isInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
                    }
                }
            }
        }

        public bool CanShowItemCategories => SelectedItemType != null;

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

        public ItemCategoryGraphQLModel? SelectedItemCategory
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedItemCategory));
                    if (!_isUpdating)
                    {
                        _cascadeCancellation?.Cancel();
                        _cascadeCancellation?.Dispose();
                        _cascadeCancellation = new CancellationTokenSource();

                        BuildItemSubCategories();
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

        public ItemSubCategoryGraphQLModel? SelectedItemSubCategory
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedItemSubCategory));
                    if (!_isUpdating && _isInitialized)
                    {
                        _cascadeCancellation?.Cancel();
                        _cascadeCancellation?.Dispose();
                        _cascadeCancellation = new CancellationTokenSource();

                        _ = ReloadDataAsync(_cascadeCancellation.Token);
                    }
                }
            }
        }

        public bool CanShowItemSubCategories => SelectedItemType != null && SelectedItemCategory != null;

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
            _isUpdating = true;
            ItemTypes = SelectedCatalog?.ItemTypes is null
                ? []
                : [.. SelectedCatalog.ItemTypes];
            SelectedItemType = null;
            BuildItemCategories();
            _isUpdating = false;
        }

        private void BuildItemCategories()
        {
            _isUpdating = true;
            ItemCategories = SelectedItemType?.ItemCategories is null
                ? []
                : [.. SelectedItemType.ItemCategories];
            SelectedItemCategory = null;

            BuildItemSubCategories();
            NotifyOfPropertyChange(nameof(CanShowItemCategories));
            NotifyOfPropertyChange(nameof(CanShowItemSubCategories));
            _isUpdating = false;
        }

        private void BuildItemSubCategories()
        {
            _isUpdating = true;
            ItemSubCategories = SelectedItemCategory?.ItemSubCategories is null
                ? []
                : [.. SelectedItemCategory.ItemSubCategories];
            SelectedItemSubCategory = null;

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
                        if (_isInitialized) _ = _searchDebounce.RunAsync(LoadItemsAsync);
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
                    foreach (PromotionCatalogItemDTO item in Items)
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
                if (SelectedItemSubCategory != null)
                    filters.subCategoryId = SelectedItemSubCategory.Id;
                if (!string.IsNullOrEmpty(FilterSearch))
                    filters.matching = FilterSearch.Trim().RemoveExtraSpaces();

                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<ItemGraphQLModel> result = await _itemService.GetPageAsync(query, variables);
                Items = [.. Context.AutoMapper.Map<ObservableCollection<PromotionCatalogItemDTO>>(result.Entries)];
                TotalCount = result.TotalEntries;

                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

                foreach (PromotionCatalogItemDTO item in Items)
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
                await _catalogCache.EnsureLoadedAsync();

                Catalogs = [.. _catalogCache.Items];
                _isInitialized = true;
                _isUpdating = true;
                SelectedCatalog = null;
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

        public ICommand SaveCommand
        {
            get
            {
                field ??= new AsyncCommand(SaveAsync);
                return field;
            }
        }

        public async Task SaveAsync()
        {
            if (SelectedItemIds.Count == 0) return;

            MessageBoxResult confirmation = ThemedMessageBox.Show("Confirme...",
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

                var (fragment, query) = _batchUpdatePricesQuery.Value;
                int totalAdded = 0;
                int totalFailed = 0;
                List<string> failedMessages = [];

                // Batching secuencial — bloques de BatchSize para no saturar el pool de conexiones
                for (int i = 0; i < allItems.Count; i += BatchSize)
                {
                    var batch = allItems.Skip(i).Take(BatchSize).ToList();
                    ExpandoObject variables = new GraphQLVariables()
                        .For(fragment, "input", new { priceListId = PromotionId, items = batch })
                        .Build();

                    BatchResultGraphQLModel batchResult = await _priceListItemService.BatchAsync<BatchResultGraphQLModel>(query, variables);

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

        public ICommand CancelCommand
        {
            get
            {
                field ??= new AsyncCommand(CancelAsync);
                return field;
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

                MessageBoxResult result = ThemedMessageBox.Show("Confirmar cierre",
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

        public ICommand PaginationCommand
        {
            get
            {
                field ??= new AsyncCommand(ExecuteChangeIndexPaginationAsync, CanExecuteChangeIndexPagination);
                return field;
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

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadItemsQuery = new(() =>
        {
            var fields = FieldSpec<PageType<ItemGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Reference)
                    .Field(e => e.Code)
                    .Select(e => e.MeasurementUnit, mu => mu
                        .Field(u => u.Id)
                        .Field(u => u.Abbreviation)))
                .Build();

            var filterParam = new GraphQLQueryParameter("filters", "ItemFilters");
            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("itemsPage", [filterParam, paginationParam], fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _batchUpdatePricesQuery = new(() =>
        {
            var fields = FieldSpec<BatchResultGraphQLModel>
                .Create()
                .Field(f => f.Success)
                .Field(f => f.Message)
                .Field(f => f.TotalAffected)
                .Field(f => f.AffectedIds)
                .SelectList(f => f.Errors, sq => sq
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            var inputParam = new GraphQLQueryParameter("input", "BatchUpdatePriceListPricesInput!");
            var fragment = new GraphQLQueryFragment("batchUpdatePriceListPrices", [inputParam], fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

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
