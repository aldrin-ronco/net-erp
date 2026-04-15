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
using System.Windows.Threading;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Billing.PriceList.ViewModels
{
    public class UpdatePromotionViewModel: Screen,
                IHandle<PriceListUpdateMessage>,
                IHandle<PromotionTempRecordResponseMessage>,
                IHandle<CriticalSystemErrorMessage>,
                IHandle<PermissionsCacheRefreshedMessage>
    {

        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<PriceListItemGraphQLModel> _priceListItemService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly IRepository<ItemGraphQLModel> _itemService;
        private readonly IRepository<TempRecordGraphQLModel> _tempRecordService;
        private readonly IRepository<PriceListGraphQLModel> _priceListServiceForModal;
        private readonly StringLengthCache _stringLengthCache;
        private readonly PermissionCache _permissionCache;
        private readonly CatalogCache _catalogCache;
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
            PermissionCache permissionCache,
            CatalogCache catalogCache,
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
            _permissionCache = permissionCache;
            _catalogCache = catalogCache;
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

                PriceListItems.Clear();
                Catalogs.Clear();
                ItemTypes.Clear();
                ItemCategories.Clear();
                ItemSubCategories.Clear();
                ShadowItems.Clear();
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
                        if (IsInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
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
                        if (IsInitialized) _ = ReloadDataAsync(_cascadeCancellation.Token);
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

        public ItemSubCategoryGraphQLModel? SelectedItemSubCategory
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedItemSubCategory));
                    if (!_isUpdating && IsInitialized)
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

        public bool HasEditPromotionPermission => _permissionCache.IsAllowed(PermissionCodes.Promotion.Edit);

        // Propiedad computed que determina si se pueden realizar operaciones de datos
        public bool CanPerformDataOperations => !HasCriticalError && HasEditPromotionPermission;


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

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    instance.DialogWidth = parentView.ActualWidth * 0.35;
                    instance.DialogHeight = parentView.ActualHeight * 0.55;
                }

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
                _activeModal = new(Context, _dialogService, _priceListItemService, _itemService, _catalogCache, new NetErp.Helpers.DebouncedAction(), _joinableTaskFactory);
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
                if (SelectedCatalog != null)
                    filters.catalogId = SelectedCatalog.Id;
                if (SelectedItemType != null)
                    filters.itemTypeId = SelectedItemType.Id;
                if (SelectedItemCategory != null)
                    filters.itemCategoryId = SelectedItemCategory.Id;
                if (SelectedItemSubCategory != null)
                    filters.itemSubCategoryId = SelectedItemSubCategory.Id;
                if (!string.IsNullOrEmpty(FilterSearch))
                    filters.filterSearch = FilterSearch.Trim().RemoveExtraSpaces();

                ExpandoObject variables = new GraphQLVariables()
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
            await _catalogCache.EnsureLoadedAsync();

            Catalogs = [.. _catalogCache.Items];
            IsInitialized = true;
            _isUpdating = true;
            SelectedCatalog = null;
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

        public async Task DeleteListAsync()
        {
            try
            {
                MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar los registros seleccionados?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;

                if (ShadowItems.Count == 0) return;

                IsBusy = true;
                List<int> itemIds = ShadowItems.ToList();
                var (fragment, query) = _batchDeleteQuery.Value;
                int totalDeleted = 0;
                List<string> failedMessages = [];

                for (int i = 0; i < itemIds.Count; i += DeleteBatchSize)
                {
                    List<int> batch = itemIds.Skip(i).Take(DeleteBatchSize).ToList();
                    ExpandoObject variables = new GraphQLVariables()
                        .For(fragment, "input", new { priceListId = Id, itemIds = batch })
                        .Build();

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

        public async Task ClearPromotionAsync()
        {
            try
            {
                MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar TODOS los registros?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;

                IsBusy = true;
                var (fragment, query) = _purgeQuery.Value;
                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "priceListId", Id)
                    .Build();
                BatchResultGraphQLModel batchResult = await _priceListItemService.BatchAsync<BatchResultGraphQLModel>(query, variables);

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
            NotifyPermissionProperties();
            _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(
                new System.Action(() => this.SetFocus(nameof(FilterSearch))),
                DispatcherPriority.Render);
        }

        private void NotifyPermissionProperties()
        {
            NotifyOfPropertyChange(nameof(HasEditPromotionPermission));
            NotifyOfPropertyChange(nameof(CanPerformDataOperations));
            NotifyOfPropertyChange(nameof(CanDelete));
            NotifyOfPropertyChange(nameof(CanClearPromotion));
        }

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyPermissionProperties();
            return Task.CompletedTask;
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
                    NotifyOfPropertyChange(nameof(ResponseTime));
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
                    NotifyOfPropertyChange(nameof(PageIndex));
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
                    NotifyOfPropertyChange(nameof(PageSize));
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
                    NotifyOfPropertyChange(nameof(TotalCount));
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

        private static Lazy<(GraphQLQueryFragment Fragment, string Query)> BuildBatchResultMutationQuery(string operationName, GraphQLQueryParameter parameter) => new(() =>
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

            var fragment = new GraphQLQueryFragment(operationName, [parameter], fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _batchDeleteQuery =
            BuildBatchResultMutationQuery("batchDeletePriceListPrices", new GraphQLQueryParameter("input", "BatchDeletePriceListPricesInput!"));

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _purgeQuery =
            BuildBatchResultMutationQuery("purgePriceListPrices", new GraphQLQueryParameter("priceListId", "ID!"));

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
                        .Field(i => i.Reference)
                        .Select(i => i.MeasurementUnit, mu => mu
                            .Field(u => u.Id)
                            .Field(u => u.Abbreviation))))
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
