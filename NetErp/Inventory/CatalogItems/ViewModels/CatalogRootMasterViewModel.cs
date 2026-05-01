using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using Models.Inventory;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Inventory.CatalogItems.DTO;
using NetErp.Inventory.CatalogItems.Validators;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Inventory.CatalogItems.ViewModels
{
    public class CatalogRootMasterViewModel : Screen,
        IHandle<CatalogCreateMessage>, IHandle<CatalogUpdateMessage>, IHandle<CatalogDeleteMessage>,
        IHandle<ItemTypeCreateMessage>, IHandle<ItemTypeUpdateMessage>, IHandle<ItemTypeDeleteMessage>,
        IHandle<ItemCategoryCreateMessage>, IHandle<ItemCategoryUpdateMessage>, IHandle<ItemCategoryDeleteMessage>,
        IHandle<ItemSubCategoryCreateMessage>, IHandle<ItemSubCategoryUpdateMessage>, IHandle<ItemSubCategoryDeleteMessage>,
        IHandle<ItemCreateMessage>, IHandle<ItemUpdateMessage>, IHandle<ItemDeleteMessage>,
        IHandle<PermissionsCacheRefreshedMessage>,
        IHandle<S3StorageLocationCreateMessage>, IHandle<S3StorageLocationUpdateMessage>,
        IHandle<AwsS3ConfigCreateMessage>, IHandle<AwsS3ConfigUpdateMessage>
    {
        #region Services

        private readonly IRepository<CatalogGraphQLModel> _catalogService;
        private readonly IRepository<ItemTypeGraphQLModel> _itemTypeService;
        private readonly IRepository<ItemCategoryGraphQLModel> _itemCategoryService;
        private readonly IRepository<ItemSubCategoryGraphQLModel> _itemSubCategoryService;
        private readonly IRepository<ItemGraphQLModel> _itemService;
        private readonly IRepository<S3StorageLocationGraphQLModel> _s3LocationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IMapper _mapper;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        // Caches
        private readonly IGraphQLClient _graphQLClient;
        private readonly CatalogCache _catalogCache;
        private readonly MeasurementUnitCache _measurementUnitCache;
        private readonly ItemBrandCache _itemBrandCache;
        private readonly AccountingGroupCache _accountingGroupCache;
        private readonly ItemSizeCategoryCache _itemSizeCategoryCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly PermissionCache _permissionCache;

        // Validators
        private readonly CatalogValidator _catalogValidator;
        private readonly ItemTypeValidator _itemTypeValidator;
        private readonly ItemCategoryValidator _itemCategoryValidator;
        private readonly ItemSubCategoryValidator _itemSubCategoryValidator;
        private readonly ItemValidator _itemValidator;

        #endregion

        #region Properties

        public CatalogViewModel Context { get; private set; }

        public S3Helper? S3Helper { get; private set; }
        public string LocalImageCachePath { get; private set; } = string.Empty;
        public bool IsS3Available => S3Helper != null;

        public ObservableCollection<CatalogDTO> Catalogs
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Catalogs));
                    // Intentionally NOT notifying HasRecords/ShowEmptyState here — those
                    // are stored and only evaluated at init / Catalog create / Catalog delete.
                    // Notifying on every reassignment (e.g. when applying/clearing the filter)
                    // causes the Content Grid to re-render and the search field to lose focus.
                }
            }
        } = [];

        // Anti-flicker flag: while false, both HasRecords and ShowEmptyState are false,
        // so neither the EmptyState nor the Content Grid are visible during the initial
        // load — only the blank Border background shows.
        private bool _isInitialized;

        /// <summary>Drives the Content Grid visibility. False until the initial load completes.</summary>
        public bool HasRecords => _isInitialized && !ShowEmptyState;

        /// <summary>
        /// Stored — NEVER computed from <see cref="Catalogs"/> count. Only updated at
        /// initialization, on catalog create (→ false), and on catalog delete (re-evaluated).
        /// Keeping it stored prevents the filter-with-zero-results case from wrongly
        /// showing the empty state, and prevents focus loss during search re-renders.
        /// </summary>
        public bool ShowEmptyState
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ShowEmptyState));
                    NotifyOfPropertyChange(nameof(HasRecords));
                }
            }
        }

        #region Search (filter-only tree replacement)

        private const int MinSearchLength = 4;

        private readonly DebouncedAction _searchDebounce;
        private ObservableCollection<CatalogDTO>? _preSearchCatalogs;

        // Monotonic version used to discard results from stale in-flight searches.
        // DebouncedAction only cancels the delay, not an already-running action body.
        private int _searchVersion;

        public string FilterSearch
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));

                    if (string.IsNullOrEmpty(value))
                    {
                        ClearSearch();
                    }
                    else if (value.Length >= MinSearchLength)
                    {
                        _ = _searchDebounce.RunAsync(SearchItemsAsync);
                    }
                    else if (IsSearching)
                    {
                        // User shrunk the query below the min length while a filter
                        // is active — revert to the original tree.
                        ClearSearch();
                    }
                }
            }
        } = string.Empty;

        public bool IsSearching
        {
            get;
            private set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsSearching));
                }
            }
        }

        public bool IsSearchLoading
        {
            get;
            private set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsSearchLoading));
                }
            }
        }

        public int MatchedItemsCount
        {
            get;
            private set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(MatchedItemsCount));
                }
            }
        }

        public bool MatchedItemsTruncated
        {
            get;
            private set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(MatchedItemsTruncated));
                }
            }
        }

        public bool FilterSearchIsFocused
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FilterSearchIsFocused));
                }
            }
        }

        private void SetFocusOnFilterSearch()
        {
            FilterSearchIsFocused = false;
            FilterSearchIsFocused = true;
        }

        private System.Windows.Input.ICommand? _clearSearchCommand;
        public System.Windows.Input.ICommand ClearSearchCommand => _clearSearchCommand ??= new DevExpress.Mvvm.DelegateCommand(() =>
        {
            FilterSearch = string.Empty;
            SetFocusOnFilterSearch();
        });

        private const int SearchPageSize = 100;

        private async Task SearchItemsAsync()
        {
            if (string.IsNullOrEmpty(FilterSearch) || FilterSearch.Length < MinSearchLength) return;

            // Stamp this invocation; any later invocation bumps the version and
            // this one's result will be discarded on return.
            int myVersion = Interlocked.Increment(ref _searchVersion);

            IsSearchLoading = true;
            try
            {
                (GraphQLQueryFragment fragment, string query) = _searchItemsQuery.Value;
                object variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { PageSize = SearchPageSize })
                    .For(fragment, "filters", new { matching = FilterSearch.Trim().RemoveExtraSpaces() })
                    .Build();

                PageType<ItemGraphQLModel> result = await _itemService.GetPageAsync(query, variables);

                // Discard stale result (a newer search started after we launched ours).
                if (myVersion != Volatile.Read(ref _searchVersion)) return;

                // Snapshot original tree on first search only. We store a reference
                // because the next line reassigns Catalogs to a brand-new instance.
                _preSearchCatalogs ??= Catalogs;

                Catalogs = BuildTreeFromSearchResults(result.Entries);
                MatchedItemsCount = result.Entries.Count;
                MatchedItemsTruncated = result.Entries.Count >= SearchPageSize;
                IsSearching = true;
                SelectedItem = null;
            }
            catch (Exception ex)
            {
                if (myVersion != Volatile.Read(ref _searchVersion)) return;
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(SearchItemsAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (myVersion == Volatile.Read(ref _searchVersion))
                    IsSearchLoading = false;
            }
        }

        private void ClearSearch()
        {
            // Invalidate any in-flight search so its late result won't overwrite us.
            Interlocked.Increment(ref _searchVersion);

            if (_preSearchCatalogs is null)
            {
                // No active filter — still reset transient flags just in case.
                IsSearching = false;
                IsSearchLoading = false;
                MatchedItemsCount = 0;
                MatchedItemsTruncated = false;
                return;
            }

            // Inline UI update — ClearSearch is already called from the setter
            // which runs on the UI thread via WPF binding, so no dispatcher
            // switch is needed here.
            Catalogs = _preSearchCatalogs;
            _preSearchCatalogs = null;
            MatchedItemsCount = 0;
            MatchedItemsTruncated = false;
            IsSearching = false;
            IsSearchLoading = false;
            SelectedItem = null;
        }

        /// <summary>
        /// Reconstruye el árbol usando solo las rutas (Catalog → ItemType → ItemCategory → ItemSubCategory → Item)
        /// de los items que matchearon. Mergea items del mismo padre para evitar duplicados.
        /// </summary>
        private ObservableCollection<CatalogDTO> BuildTreeFromSearchResults(IEnumerable<ItemGraphQLModel> items)
        {
            Dictionary<int, CatalogDTO> catalogs = [];

            foreach (ItemGraphQLModel item in items)
            {
                ItemSubCategoryGraphQLModel? subCat = item.SubCategory;
                ItemCategoryGraphQLModel? cat = subCat?.ItemCategory;
                ItemTypeGraphQLModel? type = cat?.ItemType;
                CatalogGraphQLModel? catalog = type?.Catalog;
                if (subCat == null || cat == null || type == null || catalog == null) continue;

                // Catalog
                if (!catalogs.TryGetValue(catalog.Id, out CatalogDTO? catalogDTO))
                {
                    catalogDTO = _mapper.Map<CatalogDTO>(catalog);
                    catalogDTO.ItemTypes = [];
                    catalogDTO.IsExpanded = true;
                    catalogs[catalog.Id] = catalogDTO;
                }

                // ItemType
                ItemTypeDTO? itemTypeDTO = catalogDTO.ItemTypes.FirstOrDefault(t => t.Id == type.Id);
                if (itemTypeDTO is null)
                {
                    itemTypeDTO = _mapper.Map<ItemTypeDTO>(type);
                    itemTypeDTO.Context = this;
                    itemTypeDTO.ItemCategories = [];
                    itemTypeDTO.IsExpanded = true;
                    catalogDTO.ItemTypes.Add(itemTypeDTO);
                }

                // ItemCategory
                ItemCategoryDTO? itemCategoryDTO = itemTypeDTO.ItemCategories.FirstOrDefault(c => c.Id == cat.Id);
                if (itemCategoryDTO is null)
                {
                    itemCategoryDTO = _mapper.Map<ItemCategoryDTO>(cat);
                    itemCategoryDTO.Context = this;
                    itemCategoryDTO.SubCategories = [];
                    itemCategoryDTO.IsExpanded = true;
                    itemTypeDTO.ItemCategories.Add(itemCategoryDTO);
                }

                // ItemSubCategory
                ItemSubCategoryDTO? subCategoryDTO = itemCategoryDTO.SubCategories.FirstOrDefault(s => s.Id == subCat.Id);
                if (subCategoryDTO is null)
                {
                    subCategoryDTO = _mapper.Map<ItemSubCategoryDTO>(subCat);
                    subCategoryDTO.Context = this;
                    subCategoryDTO.Items = [];
                    subCategoryDTO.IsExpanded = true;
                    itemCategoryDTO.SubCategories.Add(subCategoryDTO);
                }

                // Item
                if (!subCategoryDTO.Items.Any(i => i.Id == item.Id))
                {
                    ItemDTO itemDTO = _mapper.Map<ItemDTO>(item);
                    itemDTO.Context = this;
                    subCategoryDTO.Items.Add(itemDTO);
                }
            }

            return [.. catalogs.Values];
        }

        #endregion

        public object? SelectedItem
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                NotifyOfPropertyChange(nameof(SelectedItem));
                NotifyAllActionStates();

                // Sync side panel with the new selection
                SyncPanelWithSelection();
            }
        }

        private ItemDetailViewModel? _panelItemDetail;
        public ItemDetailViewModel? PanelItemDetail
        {
            get => _panelItemDetail;
            private set
            {
                if (_panelItemDetail != value)
                {
                    _panelItemDetail = value;
                    NotifyOfPropertyChange(nameof(PanelItemDetail));
                }
            }
        }

        private void EnsurePanelItemDetail()
        {
            if (_panelItemDetail is not null) return;
            PanelItemDetail = new ItemDetailViewModel(
                _itemService, _eventAggregator, _dialogService, _stringLengthCache,
                _measurementUnitCache, _itemBrandCache, _accountingGroupCache, _itemSizeCategoryCache,
                _joinableTaskFactory, _itemValidator, _mapper,
                S3Helper, LocalImageCachePath);
            _panelItemDetail!.HasEditPermission = HasItemEditPermission;
            _panelItemDetail.HasAddImagePermission = HasItemAddImagePermission;
        }

        private void SyncPanelWithSelection()
        {
            if (_panelItemDetail is null) return; // lazy-created, not yet used
            if (SelectedItem is ItemDTO itemDto)
            {
                ItemTypeDTO? itemType = FindItemTypeForItem(itemDto);
                bool hasComponents = itemType != null && !itemType.StockControl;
                bool stockControl = itemType?.StockControl ?? false;
                _panelItemDetail.LoadForPanel(_mapper.Map<ItemGraphQLModel>(itemDto), hasComponents, stockControl);
            }
            else
            {
                _panelItemDetail.ClearPanel();
            }
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
                    NotifyAllActionStates();
                }
            }
        }

        #endregion

        #region Permissions

        public bool HasCatalogCreatePermission => _permissionCache.IsAllowed(PermissionCodes.Catalog.Create);
        public bool HasCatalogEditPermission => _permissionCache.IsAllowed(PermissionCodes.Catalog.Edit);
        public bool HasCatalogDeletePermission => _permissionCache.IsAllowed(PermissionCodes.Catalog.Delete);

        public bool HasItemTypeCreatePermission => _permissionCache.IsAllowed(PermissionCodes.ItemType.Create);
        public bool HasItemTypeEditPermission => _permissionCache.IsAllowed(PermissionCodes.ItemType.Edit);
        public bool HasItemTypeDeletePermission => _permissionCache.IsAllowed(PermissionCodes.ItemType.Delete);

        public bool HasItemCategoryCreatePermission => _permissionCache.IsAllowed(PermissionCodes.ItemCategory.Create);
        public bool HasItemCategoryEditPermission => _permissionCache.IsAllowed(PermissionCodes.ItemCategory.Edit);
        public bool HasItemCategoryDeletePermission => _permissionCache.IsAllowed(PermissionCodes.ItemCategory.Delete);

        public bool HasItemSubCategoryCreatePermission => _permissionCache.IsAllowed(PermissionCodes.ItemSubCategory.Create);
        public bool HasItemSubCategoryEditPermission => _permissionCache.IsAllowed(PermissionCodes.ItemSubCategory.Edit);
        public bool HasItemSubCategoryDeletePermission => _permissionCache.IsAllowed(PermissionCodes.ItemSubCategory.Delete);

        public bool HasItemCreatePermission => _permissionCache.IsAllowed(PermissionCodes.Item.Create);
        public bool HasItemEditPermission => _permissionCache.IsAllowed(PermissionCodes.Item.Edit);
        public bool HasItemDeletePermission => _permissionCache.IsAllowed(PermissionCodes.Item.Delete);
        public bool HasItemDiscontinuePermission => _permissionCache.IsAllowed(PermissionCodes.Item.Discontinue);
        public bool HasItemAddImagePermission => _permissionCache.IsAllowed(PermissionCodes.Item.AddImage);

        #endregion

        #region Can* action states

        public bool CanNewCatalog => HasCatalogCreatePermission && !IsBusy;
        public bool CanEditCatalog => HasCatalogEditPermission && SelectedItem is CatalogDTO && !IsBusy;
        public bool CanDeleteCatalog => HasCatalogDeletePermission && SelectedItem is CatalogDTO && !IsBusy;
        public bool CanNewItemType => HasItemTypeCreatePermission && SelectedItem is CatalogDTO && !IsBusy;

        public bool CanEditItemType => HasItemTypeEditPermission && SelectedItem is ItemTypeDTO && !IsBusy;
        public bool CanDeleteItemType => HasItemTypeDeletePermission && SelectedItem is ItemTypeDTO && !IsBusy;
        public bool CanNewItemCategory => HasItemCategoryCreatePermission && SelectedItem is ItemTypeDTO && !IsBusy;

        public bool CanEditItemCategory => HasItemCategoryEditPermission && SelectedItem is ItemCategoryDTO && !IsBusy;
        public bool CanDeleteItemCategory => HasItemCategoryDeletePermission && SelectedItem is ItemCategoryDTO && !IsBusy;
        public bool CanNewItemSubCategory => HasItemSubCategoryCreatePermission && SelectedItem is ItemCategoryDTO && !IsBusy;

        public bool CanEditItemSubCategory => HasItemSubCategoryEditPermission && SelectedItem is ItemSubCategoryDTO && !IsBusy;
        public bool CanDeleteItemSubCategory => HasItemSubCategoryDeletePermission && SelectedItem is ItemSubCategoryDTO && !IsBusy;
        public bool CanNewItem => HasItemCreatePermission && (SelectedItem is ItemSubCategoryDTO || SelectedItem is ItemDTO) && !IsBusy;

        public bool CanDeleteItem => HasItemDeletePermission && SelectedItem is ItemDTO && !IsBusy;
        public bool CanDiscontinueItem => HasItemDiscontinuePermission && SelectedItem is ItemDTO && !IsBusy;

        private void NotifyAllActionStates()
        {
            NotifyOfPropertyChange(nameof(CanNewCatalog));
            NotifyOfPropertyChange(nameof(CanEditCatalog));
            NotifyOfPropertyChange(nameof(CanDeleteCatalog));
            NotifyOfPropertyChange(nameof(CanNewItemType));
            NotifyOfPropertyChange(nameof(CanEditItemType));
            NotifyOfPropertyChange(nameof(CanDeleteItemType));
            NotifyOfPropertyChange(nameof(CanNewItemCategory));
            NotifyOfPropertyChange(nameof(CanEditItemCategory));
            NotifyOfPropertyChange(nameof(CanDeleteItemCategory));
            NotifyOfPropertyChange(nameof(CanNewItemSubCategory));
            NotifyOfPropertyChange(nameof(CanEditItemSubCategory));
            NotifyOfPropertyChange(nameof(CanDeleteItemSubCategory));
            NotifyOfPropertyChange(nameof(CanNewItem));
            NotifyOfPropertyChange(nameof(CanDeleteItem));
            NotifyOfPropertyChange(nameof(CanDiscontinueItem));
        }

        private void NotifyAllPermissionStates()
        {
            NotifyOfPropertyChange(nameof(HasCatalogCreatePermission));
            NotifyOfPropertyChange(nameof(HasCatalogEditPermission));
            NotifyOfPropertyChange(nameof(HasCatalogDeletePermission));
            NotifyOfPropertyChange(nameof(HasItemTypeCreatePermission));
            NotifyOfPropertyChange(nameof(HasItemTypeEditPermission));
            NotifyOfPropertyChange(nameof(HasItemTypeDeletePermission));
            NotifyOfPropertyChange(nameof(HasItemCategoryCreatePermission));
            NotifyOfPropertyChange(nameof(HasItemCategoryEditPermission));
            NotifyOfPropertyChange(nameof(HasItemCategoryDeletePermission));
            NotifyOfPropertyChange(nameof(HasItemSubCategoryCreatePermission));
            NotifyOfPropertyChange(nameof(HasItemSubCategoryEditPermission));
            NotifyOfPropertyChange(nameof(HasItemSubCategoryDeletePermission));
            NotifyOfPropertyChange(nameof(HasItemCreatePermission));
            NotifyOfPropertyChange(nameof(HasItemEditPermission));
            NotifyOfPropertyChange(nameof(HasItemDeletePermission));
            NotifyOfPropertyChange(nameof(HasItemDiscontinuePermission));
            NotifyOfPropertyChange(nameof(HasItemAddImagePermission));

            // The panel-mode ItemDetailViewModel owns the Edit button on ItemDetailPanelView.
            // Push the permission to the panel instance so its CanEnterEditMode re-evaluates
            // whenever permissions change.
            if (_panelItemDetail is not null)
            {
                _panelItemDetail.HasEditPermission = HasItemEditPermission;
                _panelItemDetail.HasAddImagePermission = HasItemAddImagePermission;
            }

            NotifyAllActionStates();
        }

        #endregion

        #region Constructor

        public CatalogRootMasterViewModel(
            CatalogViewModel context,
            IRepository<CatalogGraphQLModel> catalogService,
            IRepository<ItemTypeGraphQLModel> itemTypeService,
            IRepository<ItemCategoryGraphQLModel> itemCategoryService,
            IRepository<ItemSubCategoryGraphQLModel> itemSubCategoryService,
            IRepository<ItemGraphQLModel> itemService,
            IRepository<S3StorageLocationGraphQLModel> s3LocationService,
            Helpers.IDialogService dialogService,
            Helpers.Services.INotificationService notificationService,
            IEventAggregator eventAggregator,
            IMapper mapper,
            JoinableTaskFactory joinableTaskFactory,
            CatalogCache catalogCache,
            MeasurementUnitCache measurementUnitCache,
            ItemBrandCache itemBrandCache,
            AccountingGroupCache accountingGroupCache,
            ItemSizeCategoryCache itemSizeCategoryCache,
            StringLengthCache stringLengthCache,
            CatalogValidator catalogValidator,
            ItemTypeValidator itemTypeValidator,
            ItemCategoryValidator itemCategoryValidator,
            ItemSubCategoryValidator itemSubCategoryValidator,
            ItemValidator itemValidator,
            IGraphQLClient graphQLClient,
            PermissionCache permissionCache,
            DebouncedAction searchDebounce)
        {
            Context = context;
            _catalogService = catalogService;
            _itemTypeService = itemTypeService;
            _itemCategoryService = itemCategoryService;
            _itemSubCategoryService = itemSubCategoryService;
            _itemService = itemService;
            _s3LocationService = s3LocationService;
            _dialogService = dialogService;
            _notificationService = notificationService;
            _eventAggregator = eventAggregator;
            _mapper = mapper;
            _joinableTaskFactory = joinableTaskFactory;
            _catalogCache = catalogCache;
            _measurementUnitCache = measurementUnitCache;
            _itemBrandCache = itemBrandCache;
            _accountingGroupCache = accountingGroupCache;
            _itemSizeCategoryCache = itemSizeCategoryCache;
            _stringLengthCache = stringLengthCache;
            _catalogValidator = catalogValidator;
            _itemTypeValidator = itemTypeValidator;
            _itemCategoryValidator = itemCategoryValidator;
            _itemSubCategoryValidator = itemSubCategoryValidator;
            _itemValidator = itemValidator;
            _graphQLClient = graphQLClient;
            _permissionCache = permissionCache;
            _searchDebounce = searchDebounce ?? throw new ArgumentNullException(nameof(searchDebounce));

            _eventAggregator.SubscribeOnUIThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async Task OnInitializedAsync(CancellationToken cancellationToken)
        {
            try
            {
                IsBusy = true;
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.CatalogItem);
                await CacheBatchLoader.LoadAsync(
                    _graphQLClient, cancellationToken,
                    _catalogCache, _measurementUnitCache, _itemBrandCache, _accountingGroupCache, _itemSizeCategoryCache);
                await LoadS3ConfigAsync();
                EnsurePanelItemDetail();
                BuildTree();

                // Anti-flicker: mark initialized and evaluate empty state from the cache
                // (the authoritative source — not Catalogs which may be a filtered subset).
                _isInitialized = true;
                ShowEmptyState = _catalogCache.Items.Count == 0;
                NotifyOfPropertyChange(nameof(HasRecords));

                // Permissions are already loaded by ShellViewModel when the company is selected.
                // Just force initial evaluation of all HasXPermission / CanX bindings.
                NotifyAllPermissionStates();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    ThemedMessageBox.Show("Atención!",
                        $"{GetType().Name}.{nameof(OnInitializedAsync)}: {ex.GetErrorMessage()}",
                        MessageBoxButton.OK, MessageBoxImage.Error));
                await TryCloseAsync(false);
            }
            finally
            {
                IsBusy = false;
            }
            await base.OnInitializedAsync(cancellationToken);

            // Asegurar foco después de que el BusyMask se haya retirado completamente.
            // OnViewReady solo dispara una vez y a veces compite con la animación del BusyMask
            // en el primer arranque, dejando el foco sin asignar hasta la primera interacción.
            Application.Current.Dispatcher.BeginInvoke(
                new System.Action(SetFocusOnFilterSearch),
                System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            Application.Current.Dispatcher.BeginInvoke(
                new System.Action(SetFocusOnFilterSearch),
                System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                // Invalidate any in-flight search so its late callback becomes a no-op
                // (prevents dispatching into a VM whose event subscription is already gone).
                Interlocked.Increment(ref _searchVersion);

                _eventAggregator.Unsubscribe(this);
                Catalogs?.Clear();

                // Release the pre-search snapshot together with all its DTO references.
                _preSearchCatalogs?.Clear();
                _preSearchCatalogs = null;

                IsSearching = false;
                IsSearchLoading = false;
                MatchedItemsCount = 0;
                MatchedItemsTruncated = false;
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        /// <summary>
        /// Construye el árbol desde CatalogCache. Catalogs + ItemTypes ya vienen del cache;
        /// ItemCategories / ItemSubCategories / Items se cargan lazy al expandir cada nodo.
        /// </summary>
        private void BuildTree()
        {
            // Defensive reset: any active filter state is stale the moment we rebuild the
            // tree from the cache. Drop the snapshot and invalidate in-flight searches.
            Interlocked.Increment(ref _searchVersion);
            _preSearchCatalogs?.Clear();
            _preSearchCatalogs = null;
            IsSearching = false;
            IsSearchLoading = false;
            MatchedItemsCount = 0;
            MatchedItemsTruncated = false;

            ObservableCollection<CatalogDTO> newCatalogs = [];
            foreach (CatalogGraphQLModel catalogModel in _catalogCache.Items)
            {
                CatalogDTO catalogDTO = _mapper.Map<CatalogDTO>(catalogModel);
                foreach (ItemTypeDTO itemType in catalogDTO.ItemTypes)
                {
                    itemType.Context = this;
                    itemType.ItemCategories.Add(new ItemCategoryDTO { IsDummyChild = true, SubCategories = [], Name = "Dummy" });
                }
                newCatalogs.Add(catalogDTO);
            }
            Catalogs = newCatalogs;
        }

        #endregion

        #region S3 Initialization

        private async Task LoadS3ConfigAsync()
        {
            try
            {
                Dictionary<string, object> fields = FieldSpec<S3StorageLocationGraphQLModel>
                    .Create()
                    .Field(f => f.Id)
                    .Field(f => f.Key)
                    .Field(f => f.Bucket)
                    .Field(f => f.Directory)
                    .Field(f => f.Description)
                    .Select(f => f.AwsS3Config, nested: aws => aws
                        .Field(a => a.Id)
                        .Field(a => a.AccessKey)
                        .Field(a => a.SecretKey)
                        .Field(a => a.Region)
                        .Field(a => a.Description))
                    .Build();

                GraphQLQueryFragment fragment = new("s3StorageLocationByKey",
                    [new("key", "String!")], fields, "SingleItemResponse");
                string query = new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.QUERY);
                S3StorageLocationGraphQLModel? location = await _s3LocationService.GetSingleItemAsync(
                    query, new { singleItemResponseKey = "product_images" });

                if (location is null)
                {
                    S3Helper = null;
                    return;
                }

                string appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                LocalImageCachePath = Path.Combine(appDir, "cache", location.Bucket, location.Directory);
                Directory.CreateDirectory(LocalImageCachePath);

                // Verify write permissions on cache directory
                string testFile = Path.Combine(LocalImageCachePath, ".write_test");
                try
                {
                    await File.WriteAllTextAsync(testFile, "test");
                    File.Delete(testFile);
                }
                catch (UnauthorizedAccessException)
                {
                    throw new InvalidOperationException(
                        $"No se tienen permisos de escritura en el directorio de cache de imágenes:\r\n\r\n{LocalImageCachePath}\r\n\r\nAsigne permisos de escritura a este directorio para habilitar la gestión de imágenes de productos.");
                }

                if (location.AwsS3Config is null && SessionInfo.DefaultAwsS3Config is null)
                {
                    S3Helper = null;
                    return;
                }

                S3Helper = Common.Helpers.S3Helper.FromStorageLocation(location);
            }
            catch (Exception ex)
            {
                throw new AsyncException(GetType(), ex);
            }
        }

        #endregion

        #region Lazy Load (ItemCategories / ItemSubCategories / Items)

        public async Task LoadItemCategoriesAsync(ItemTypeDTO itemType)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => itemType.ItemCategories.Clear());

                (GraphQLQueryFragment fragment, string query) = _loadItemCategoriesQuery.Value;
                object variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { PageSize = -1 })
                    .For(fragment, "filters", new { ItemTypeId = itemType.Id })
                    .Build();

                PageType<ItemCategoryGraphQLModel> result = await _itemCategoryService.GetPageAsync(query, variables);

                if (result.Entries.Count == 0)
                {
                    Application.Current.Dispatcher.Invoke(() => itemType.IsExpanded = false);
                    _notificationService.ShowInfo("Este tipo de item no tiene categorías registradas");
                    return;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (ItemCategoryGraphQLModel model in result.Entries)
                    {
                        ItemCategoryDTO dto = _mapper.Map<ItemCategoryDTO>(model);
                        dto.Context = this;
                        dto.SubCategories.Add(new ItemSubCategoryDTO { IsDummyChild = true, Items = [], Name = "Dummy" });
                        itemType.ItemCategories.Add(dto);
                    }
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(LoadItemCategoriesAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        public async Task LoadItemSubCategoriesAsync(ItemCategoryDTO itemCategory)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => itemCategory.SubCategories.Clear());

                (GraphQLQueryFragment fragment, string query) = _loadItemSubCategoriesQuery.Value;
                object variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { PageSize = -1 })
                    .For(fragment, "filters", new { ItemCategoryId = itemCategory.Id })
                    .Build();

                PageType<ItemSubCategoryGraphQLModel> result = await _itemSubCategoryService.GetPageAsync(query, variables);

                if (result.Entries.Count == 0)
                {
                    Application.Current.Dispatcher.Invoke(() => itemCategory.IsExpanded = false);
                    _notificationService.ShowInfo("Esta categoría no tiene subcategorías registradas");
                    return;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (ItemSubCategoryGraphQLModel model in result.Entries)
                    {
                        ItemSubCategoryDTO dto = _mapper.Map<ItemSubCategoryDTO>(model);
                        dto.Context = this;
                        dto.Items.Add(new ItemDTO { IsDummyChild = true, EanCodes = [], Name = "Dummy" });
                        itemCategory.SubCategories.Add(dto);
                    }
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(LoadItemSubCategoriesAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        public async Task LoadItemsAsync(ItemSubCategoryDTO itemSubCategory)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => itemSubCategory.Items.Clear());

                (GraphQLQueryFragment fragment, string query) = _loadItemsQuery.Value;
                object variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { PageSize = -1 })
                    .For(fragment, "filters", new { SubCategoryId = itemSubCategory.Id, IsActive = true })
                    .Build();

                PageType<ItemGraphQLModel> result = await _itemService.GetPageAsync(query, variables);

                if (result.Entries.Count == 0)
                {
                    Application.Current.Dispatcher.Invoke(() => itemSubCategory.IsExpanded = false);
                    _notificationService.ShowInfo("Esta subcategoría no tiene productos registrados");
                    return;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (ItemGraphQLModel model in result.Entries)
                    {
                        ItemDTO dto = _mapper.Map<ItemDTO>(model);
                        dto.Context = this;
                        itemSubCategory.Items.Add(dto);
                    }
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(LoadItemsAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        #endregion

        #region New / Edit Actions

        public Task NewCatalogAsync() => OpenDialogAsync(() =>
        {
            CatalogDetailViewModel detail = new(_catalogService, _eventAggregator, _stringLengthCache, _joinableTaskFactory, _catalogValidator);
            detail.SetForNew();
            ApplyDialogDimensions(detail, 460, 240);
            return detail;
        }, "Nuevo catálogo");

        public Task EditCatalogAsync() => OpenDialogAsync<CatalogDetailViewModel>(() =>
        {
            if (SelectedItem is not CatalogDTO dto) return null;
            CatalogDetailViewModel detail = new(_catalogService, _eventAggregator, _stringLengthCache, _joinableTaskFactory, _catalogValidator);
            detail.SetForEdit(_mapper.Map<CatalogGraphQLModel>(dto));
            ApplyDialogDimensions(detail, 460, 240);
            return detail;
        }, "Editar catálogo");

        public Task NewItemTypeAsync() => OpenDialogAsync<ItemTypeDetailViewModel>(() =>
        {
            if (SelectedItem is not CatalogDTO catalog) return null;

            ItemTypeDetailViewModel detail = new(_itemTypeService, _eventAggregator, _stringLengthCache,
                _measurementUnitCache, _accountingGroupCache, _catalogCache, _joinableTaskFactory, _itemTypeValidator);
            detail.SetForNew(catalog.Id);

            // Prefix letters are unique per company across all catalogs. The VM builds the
            // pool from CatalogCache; if it is empty, every A-Z is already taken and there is
            // no valid prefix to assign — abort before showing the dialog. Using the VM's pool
            // as the single source of truth keeps the check and the combo consistent.
            if (detail.AvailablePrefixChars.Count == 0)
            {
                ThemedMessageBox.Show("Atención",
                    "No hay prefijos disponibles. Todos los caracteres A-Z están en uso por tipos de item existentes. Edite o elimine un tipo de item para liberar un prefijo.",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return null;
            }

            ApplyDialogDimensions(detail, 520, 380);
            return detail;
        }, "Nuevo tipo de item");

        public Task EditItemTypeAsync() => OpenDialogAsync<ItemTypeDetailViewModel>(() =>
        {
            if (SelectedItem is not ItemTypeDTO dto) return null;
            ItemTypeDetailViewModel detail = new(_itemTypeService, _eventAggregator, _stringLengthCache,
                _measurementUnitCache, _accountingGroupCache, _catalogCache, _joinableTaskFactory, _itemTypeValidator);
            detail.SetForEdit(_mapper.Map<ItemTypeGraphQLModel>(dto));
            ApplyDialogDimensions(detail, 520, 380);
            return detail;
        }, "Editar tipo de item");

        public Task NewItemCategoryAsync() => OpenDialogAsync<ItemCategoryDetailViewModel>(() =>
        {
            if (SelectedItem is not ItemTypeDTO itemType) return null;
            ItemCategoryDetailViewModel detail = new(_itemCategoryService, _eventAggregator, _stringLengthCache, _joinableTaskFactory, _itemCategoryValidator);
            detail.SetForNew(itemType.Id);
            ApplyDialogDimensions(detail, 460, 240);
            return detail;
        }, "Nueva categoría");

        public Task EditItemCategoryAsync() => OpenDialogAsync<ItemCategoryDetailViewModel>(() =>
        {
            if (SelectedItem is not ItemCategoryDTO dto) return null;
            ItemCategoryDetailViewModel detail = new(_itemCategoryService, _eventAggregator, _stringLengthCache, _joinableTaskFactory, _itemCategoryValidator);
            detail.SetForEdit(_mapper.Map<ItemCategoryGraphQLModel>(dto));
            ApplyDialogDimensions(detail, 460, 240);
            return detail;
        }, "Editar categoría");

        public Task NewItemSubCategoryAsync() => OpenDialogAsync<ItemSubCategoryDetailViewModel>(() =>
        {
            if (SelectedItem is not ItemCategoryDTO itemCategory) return null;
            ItemSubCategoryDetailViewModel detail = new(_itemSubCategoryService, _eventAggregator, _stringLengthCache, _joinableTaskFactory, _itemSubCategoryValidator);
            detail.SetForNew(itemCategory.Id);
            ApplyDialogDimensions(detail, 460, 240);
            return detail;
        }, "Nueva subcategoría");

        public Task EditItemSubCategoryAsync() => OpenDialogAsync<ItemSubCategoryDetailViewModel>(() =>
        {
            if (SelectedItem is not ItemSubCategoryDTO dto) return null;
            ItemSubCategoryDetailViewModel detail = new(_itemSubCategoryService, _eventAggregator, _stringLengthCache, _joinableTaskFactory, _itemSubCategoryValidator);
            detail.SetForEdit(_mapper.Map<ItemSubCategoryGraphQLModel>(dto));
            ApplyDialogDimensions(detail, 460, 240);
            return detail;
        }, "Editar subcategoría");

        public Task NewItemAsync() => OpenDialogAsync<ItemDetailViewModel>(() =>
        {
            ItemSubCategoryDTO? subCategory = SelectedItem switch
            {
                ItemSubCategoryDTO sc => sc,
                ItemDTO item => FindSubCategoryForItem(item),
                _ => null
            };
            if (subCategory is null) return null;

            // Extract defaults from the parent ItemType (via tree navigation)
            ItemTypeDTO? itemType = FindItemTypeForSubCategory(subCategory);
            int? defaultMuId = itemType?.DefaultMeasurementUnit?.Id;
            int? defaultAgId = itemType?.DefaultAccountingGroup?.Id;
            bool hasComponents = itemType != null && !itemType.StockControl;
            bool stockControl = itemType?.StockControl ?? false;

            ItemDetailViewModel detail = BuildItemDetailViewModel();
            detail.SetForNew(subCategory.Id, hasComponents, stockControl, defaultMuId, defaultAgId);
            ApplyItemDialogDimensions(detail);
            return detail;
        }, "Nuevo item");

        private ItemDetailViewModel BuildItemDetailViewModel()
        {
            return new ItemDetailViewModel(
                _itemService, _eventAggregator, _dialogService, _stringLengthCache,
                _measurementUnitCache, _itemBrandCache, _accountingGroupCache, _itemSizeCategoryCache,
                _joinableTaskFactory, _itemValidator, _mapper,
                S3Helper, LocalImageCachePath);
        }

        private ItemTypeDTO? FindItemTypeForSubCategory(ItemSubCategoryDTO subCategory)
        {
            foreach (CatalogDTO catalog in Catalogs)
                foreach (ItemTypeDTO itemType in catalog.ItemTypes)
                    foreach (ItemCategoryDTO category in itemType.ItemCategories)
                        if (category.SubCategories.Any(s => s.Id == subCategory.Id))
                            return ResolveRichItemType(itemType);
            return null;
        }

        private ItemSubCategoryDTO? FindSubCategoryForItem(ItemDTO item)
        {
            foreach (CatalogDTO catalog in Catalogs)
                foreach (ItemTypeDTO itemType in catalog.ItemTypes)
                    foreach (ItemCategoryDTO category in itemType.ItemCategories)
                        foreach (ItemSubCategoryDTO subCat in category.SubCategories)
                            if (subCat.Items.Any(i => i.Id == item.Id))
                                return subCat;
            return null;
        }

        private ItemTypeDTO? FindItemTypeForItem(ItemDTO item)
        {
            foreach (CatalogDTO catalog in Catalogs)
                foreach (ItemTypeDTO itemType in catalog.ItemTypes)
                    foreach (ItemCategoryDTO category in itemType.ItemCategories)
                        foreach (ItemSubCategoryDTO subCat in category.SubCategories)
                            if (subCat.Items.Any(i => i.Id == item.Id))
                                return ResolveRichItemType(itemType);
            return null;
        }

        /// <summary>
        /// When the search filter is active, <see cref="Catalogs"/> holds a lean tree built
        /// from the search query (<c>_searchItemsQuery</c>), which only projects Id, Name,
        /// PrefixChar and StockControl on ItemType — not DefaultMeasurementUnit or
        /// DefaultAccountingGroup. The pre-search snapshot was built from CatalogCache and
        /// has the full data, so look up the same Id there to obtain the rich copy.
        /// Falls back to the lean instance when no snapshot exists (no active filter).
        /// </summary>
        private ItemTypeDTO ResolveRichItemType(ItemTypeDTO leanType)
        {
            if (_preSearchCatalogs is null || ReferenceEquals(_preSearchCatalogs, Catalogs))
                return leanType;
            foreach (CatalogDTO catalog in _preSearchCatalogs)
            {
                ItemTypeDTO? rich = catalog.ItemTypes.FirstOrDefault(t => t.Id == leanType.Id);
                if (rich is not null) return rich;
            }
            return leanType;
        }

        private void ApplyDialogDimensions(CatalogItemsDetailViewModelBase detail, double defaultWidth, double defaultHeight)
        {
            detail.DialogWidth = defaultWidth;
            detail.DialogHeight = defaultHeight;
            if (this.GetView() is FrameworkElement parent)
            {
                detail.DialogWidth = Math.Min(parent.ActualWidth * 0.7, defaultWidth);
                detail.DialogHeight = Math.Min(parent.ActualHeight * 0.9, defaultHeight);
            }
        }

        private void ApplyItemDialogDimensions(CatalogItemsDetailViewModelBase detail)
        {
            if (this.GetView() is FrameworkElement parent)
            {
                detail.DialogWidth = parent.ActualWidth * 0.7;
                detail.DialogHeight = parent.ActualHeight * 0.9;
            }
        }

        private async Task WithBusy(Func<Task> work)
        {
            try
            {
                IsBusy = true;
                await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
                await work();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Shows a dialog after a brief busy phase that covers ONLY the synchronous
        /// setup (VM construction, default values, dimensions). <see cref="IsBusy"/>
        /// is released BEFORE awaiting <c>ShowDialogAsync</c> so the busy overlay does
        /// not linger visibly behind the open dialog. The setup may return <c>null</c>
        /// to abort (e.g. when the current selection doesn't match the expected type).
        /// </summary>
        private async Task OpenDialogAsync<T>(Func<T?> setup, string title) where T : Screen
        {
            T? detail = null;
            try
            {
                IsBusy = true;
                await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
                detail = setup();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{title}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
                return;
            }
            finally
            {
                IsBusy = false;
            }

            if (detail is null) return;

            try
            {
                await _dialogService.ShowDialogAsync(detail, title);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{title}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        #endregion

        #region ICommand wrappers (for XAML bindings from tree context menus)

        private System.Windows.Input.ICommand? _newCatalogCommand;
        public System.Windows.Input.ICommand NewCatalogCommand => _newCatalogCommand ??= new DevExpress.Mvvm.AsyncCommand(NewCatalogAsync, () => CanNewCatalog);

        private System.Windows.Input.ICommand? _editCatalogCommand;
        public System.Windows.Input.ICommand EditCatalogCommand => _editCatalogCommand ??= new DevExpress.Mvvm.AsyncCommand(EditCatalogAsync, () => CanEditCatalog);

        private System.Windows.Input.ICommand? _deleteCatalogCommand;
        public System.Windows.Input.ICommand DeleteCatalogCommand => _deleteCatalogCommand ??= new DevExpress.Mvvm.AsyncCommand(DeleteCatalogAsync, () => CanDeleteCatalog);

        private System.Windows.Input.ICommand? _newItemTypeCommand;
        public System.Windows.Input.ICommand NewItemTypeCommand => _newItemTypeCommand ??= new DevExpress.Mvvm.AsyncCommand(NewItemTypeAsync, () => CanNewItemType);

        private System.Windows.Input.ICommand? _editItemTypeCommand;
        public System.Windows.Input.ICommand EditItemTypeCommand => _editItemTypeCommand ??= new DevExpress.Mvvm.AsyncCommand(EditItemTypeAsync, () => CanEditItemType);

        private System.Windows.Input.ICommand? _deleteItemTypeCommand;
        public System.Windows.Input.ICommand DeleteItemTypeCommand => _deleteItemTypeCommand ??= new DevExpress.Mvvm.AsyncCommand(DeleteItemTypeAsync, () => CanDeleteItemType);

        private System.Windows.Input.ICommand? _newItemCategoryCommand;
        public System.Windows.Input.ICommand NewItemCategoryCommand => _newItemCategoryCommand ??= new DevExpress.Mvvm.AsyncCommand(NewItemCategoryAsync, () => CanNewItemCategory);

        private System.Windows.Input.ICommand? _editItemCategoryCommand;
        public System.Windows.Input.ICommand EditItemCategoryCommand => _editItemCategoryCommand ??= new DevExpress.Mvvm.AsyncCommand(EditItemCategoryAsync, () => CanEditItemCategory);

        private System.Windows.Input.ICommand? _deleteItemCategoryCommand;
        public System.Windows.Input.ICommand DeleteItemCategoryCommand => _deleteItemCategoryCommand ??= new DevExpress.Mvvm.AsyncCommand(DeleteItemCategoryAsync, () => CanDeleteItemCategory);

        private System.Windows.Input.ICommand? _newItemSubCategoryCommand;
        public System.Windows.Input.ICommand NewItemSubCategoryCommand => _newItemSubCategoryCommand ??= new DevExpress.Mvvm.AsyncCommand(NewItemSubCategoryAsync, () => CanNewItemSubCategory);

        private System.Windows.Input.ICommand? _editItemSubCategoryCommand;
        public System.Windows.Input.ICommand EditItemSubCategoryCommand => _editItemSubCategoryCommand ??= new DevExpress.Mvvm.AsyncCommand(EditItemSubCategoryAsync, () => CanEditItemSubCategory);

        private System.Windows.Input.ICommand? _deleteItemSubCategoryCommand;
        public System.Windows.Input.ICommand DeleteItemSubCategoryCommand => _deleteItemSubCategoryCommand ??= new DevExpress.Mvvm.AsyncCommand(DeleteItemSubCategoryAsync, () => CanDeleteItemSubCategory);

        private System.Windows.Input.ICommand? _newItemCommand;
        public System.Windows.Input.ICommand NewItemCommand => _newItemCommand ??= new DevExpress.Mvvm.AsyncCommand(NewItemAsync, () => CanNewItem);

        private System.Windows.Input.ICommand? _deleteItemCommand;
        public System.Windows.Input.ICommand DeleteItemCommand => _deleteItemCommand ??= new DevExpress.Mvvm.AsyncCommand(DeleteItemAsync, () => CanDeleteItem);

        private System.Windows.Input.ICommand? _discontinueItemCommand;
        public System.Windows.Input.ICommand DiscontinueItemCommand => _discontinueItemCommand ??= new DevExpress.Mvvm.AsyncCommand(DiscontinueItemAsync, () => CanDiscontinueItem);

        #endregion

        #region Delete Actions

        public async Task DeleteCatalogAsync()
        {
            if (SelectedItem is not CatalogDTO dto) return;
            await DeleteEntityAsync(_catalogService, dto.Id,
                _canDeleteCatalogQuery.Value, _deleteCatalogQuery.Value,
                $"¿Confirma que desea eliminar el catálogo {dto.Name}?",
                result => new CatalogDeleteMessage { DeletedCatalog = result });
        }

        public async Task DeleteItemTypeAsync()
        {
            if (SelectedItem is not ItemTypeDTO dto) return;
            await DeleteEntityAsync(_itemTypeService, dto.Id,
                _canDeleteItemTypeQuery.Value, _deleteItemTypeQuery.Value,
                $"¿Confirma que desea eliminar el tipo de item {dto.Name}?",
                result => new ItemTypeDeleteMessage { DeletedItemType = result });
        }

        public async Task DeleteItemCategoryAsync()
        {
            if (SelectedItem is not ItemCategoryDTO dto) return;
            await DeleteEntityAsync(_itemCategoryService, dto.Id,
                _canDeleteItemCategoryQuery.Value, _deleteItemCategoryQuery.Value,
                $"¿Confirma que desea eliminar la categoría {dto.Name}?",
                result => new ItemCategoryDeleteMessage { DeletedItemCategory = result });
        }

        public async Task DeleteItemSubCategoryAsync()
        {
            if (SelectedItem is not ItemSubCategoryDTO dto) return;
            await DeleteEntityAsync(_itemSubCategoryService, dto.Id,
                _canDeleteItemSubCategoryQuery.Value, _deleteItemSubCategoryQuery.Value,
                $"¿Confirma que desea eliminar la subcategoría {dto.Name}?",
                result => new ItemSubCategoryDeleteMessage { DeletedItemSubCategory = result });
        }

        public async Task DeleteItemAsync()
        {
            if (SelectedItem is not ItemDTO dto) return;
            await DeleteEntityAsync(_itemService, dto.Id,
                _canDeleteItemQuery.Value, _deleteItemQuery.Value,
                $"¿Confirma que desea eliminar el item {dto.Name}?",
                result => new ItemDeleteMessage { DeletedItem = result });
        }

        private async Task DeleteEntityAsync<TModel>(
            IRepository<TModel> service,
            int id,
            (GraphQLQueryFragment Fragment, string Query) canDelete,
            (GraphQLQueryFragment Fragment, string Query) delete,
            string confirmMessage,
            Func<DeleteResponseType, object> messageBuilder)
        {
            try
            {
                IsBusy = true;

                object canDeleteVars = new GraphQLVariables()
                    .For(canDelete.Fragment, "id", id)
                    .Build();
                CanDeleteType validation = await service.CanDeleteAsync(canDelete.Query, canDeleteVars);

                if (!validation.CanDelete)
                {
                    IsBusy = false;
                    ThemedMessageBox.Show("Atención!",
                        $"El registro no puede ser eliminado\r\n\r\n{validation.Message}",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = false;
                if (ThemedMessageBox.Show("Confirme...", confirmMessage,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

                IsBusy = true;
                object deleteVars = new GraphQLVariables()
                    .For(delete.Fragment, "id", id)
                    .Build();
                DeleteResponseType result = await service.DeleteAsync<DeleteResponseType>(delete.Query, deleteVars);

                if (!result.Success)
                {
                    ThemedMessageBox.Show("Atención!",
                        $"No pudo ser eliminado el registro\r\n\r\n{result.Message}",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                SelectedItem = null;
                await _eventAggregator.PublishOnCurrentThreadAsync(messageBuilder(result), CancellationToken.None);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(DeleteEntityAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DiscontinueItemAsync()
        {
            if (SelectedItem is not ItemDTO itemDTO) return;
            try
            {
                if (ThemedMessageBox.Show("Confirme...",
                    $"¿Confirma que desea descontinuar el registro {itemDTO.Name}?",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

                IsBusy = true;
                (GraphQLQueryFragment fragment, string query) = _discontinueItemQuery.Value;
                object variables = new GraphQLVariables()
                    .For(fragment, "id", itemDTO.Id)
                    .For(fragment, "data", new { isActive = false })
                    .Build();
                UpsertResponseType<ItemGraphQLModel> result = await _itemService.UpdateAsync<UpsertResponseType<ItemGraphQLModel>>(query, variables);

                if (!result.Success)
                {
                    ThemedMessageBox.Show("Atención!",
                        $"No se pudo descontinuar el registro.\r\n\r\n{result.Message}",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                SelectedItem = null;
                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new ItemDeleteMessage
                    {
                        DeletedItem = new DeleteResponseType { DeletedId = itemDTO.Id, Success = true, Message = result.Message }
                    }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(DiscontinueItemAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region IHandle — Catalog

        /// <summary>
        /// Yields every CatalogDTO tree that must be kept in sync: the currently displayed
        /// <see cref="Catalogs"/> and, when the search filter is active, the snapshot of the
        /// original tree (<c>_preSearchCatalogs</c>) that will be restored on ClearSearch.
        /// </summary>
        private IEnumerable<ObservableCollection<CatalogDTO>> CatalogScopes()
        {
            yield return Catalogs;
            if (_preSearchCatalogs is not null && !ReferenceEquals(_preSearchCatalogs, Catalogs))
                yield return _preSearchCatalogs;
        }

        /// <summary>
        /// Runs <paramref name="mirrorAction"/> against the pre-search snapshot so a Create
        /// that happens while the filter is active also lands in the tree that will be
        /// restored on <see cref="ClearSearch"/>. No-op when the filter is not active.
        /// The action is dispatched to the UI thread and the snapshot reference is re-read
        /// inside the dispatcher callback to guard against a concurrent ClearSearch that may
        /// have run during an <c>await</c> in the caller.
        /// </summary>
        private void MirrorCreateToSnapshot(Action<ObservableCollection<CatalogDTO>> mirrorAction)
        {
            // Fast path: no filter active.
            if (_preSearchCatalogs is null || ReferenceEquals(_preSearchCatalogs, Catalogs)) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                // Re-read under UI thread — ClearSearch may have run between the outer check
                // and this dispatch (possible when the caller awaited something beforehand).
                ObservableCollection<CatalogDTO>? snapshot = _preSearchCatalogs;
                if (snapshot is null || ReferenceEquals(snapshot, Catalogs)) return;
                mirrorAction(snapshot);
            });
        }

        public Task HandleAsync(CatalogCreateMessage message, CancellationToken cancellationToken)
        {
            // A catalog was just created → the "no records" state is no longer possible.
            ShowEmptyState = false;

            CatalogGraphQLModel entity = message.CreatedCatalog.Entity;
            Application.Current.Dispatcher.Invoke(() =>
            {
                CatalogDTO dto = _mapper.Map<CatalogDTO>(entity);
                Catalogs.Add(dto);
                SelectedItem = dto;
            });

            MirrorCreateToSnapshot(snapshot =>
            {
                if (snapshot.Any(c => c.Id == entity.Id)) return;
                snapshot.Add(_mapper.Map<CatalogDTO>(entity));
            });

            _notificationService.ShowSuccess(message.CreatedCatalog.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(CatalogUpdateMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (ObservableCollection<CatalogDTO> roots in CatalogScopes())
                {
                    CatalogDTO? existing = roots.FirstOrDefault(c => c.Id == message.UpdatedCatalog.Entity.Id);
                    if (existing != null) existing.Name = message.UpdatedCatalog.Entity.Name;
                }
            });
            _notificationService.ShowSuccess(message.UpdatedCatalog.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(CatalogDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                int id = message.DeletedCatalog.DeletedId ?? 0;
                foreach (ObservableCollection<CatalogDTO> roots in CatalogScopes())
                {
                    CatalogDTO? toRemove = roots.FirstOrDefault(c => c.Id == id);
                    if (toRemove != null) roots.Remove(toRemove);
                }

                // Re-evaluate empty state against the authoritative collection: the pre-search
                // snapshot when the filter is active, the live tree otherwise. We cannot use
                // Catalogs directly because it may be a filtered subset.
                ObservableCollection<CatalogDTO> authoritative = _preSearchCatalogs ?? Catalogs;
                ShowEmptyState = authoritative.Count == 0;
            });
            _notificationService.ShowSuccess(message.DeletedCatalog.Message);
            return Task.CompletedTask;
        }

        #endregion

        #region IHandle — ItemType

        public Task HandleAsync(ItemTypeCreateMessage message, CancellationToken cancellationToken)
        {
            ItemTypeGraphQLModel entity = message.CreatedItemType.Entity;
            Application.Current.Dispatcher.Invoke(() =>
            {
                ItemTypeDTO dto = _mapper.Map<ItemTypeDTO>(entity);
                dto.Context = this;
                dto.ItemCategories.Add(new ItemCategoryDTO { IsDummyChild = true, SubCategories = [], Name = "Dummy" });
                CatalogDTO? catalog = Catalogs.FirstOrDefault(c => c.Id == entity.Catalog?.Id);
                if (catalog is null) return;
                catalog.ItemTypes.Add(dto);
                if (!catalog.IsExpanded) catalog.IsExpanded = true;
                SelectedItem = dto;
            });

            MirrorCreateToSnapshot(snapshot =>
            {
                CatalogDTO? snapCatalog = snapshot.FirstOrDefault(c => c.Id == entity.Catalog?.Id);
                if (snapCatalog is null || snapCatalog.ItemTypes.Any(t => t.Id == entity.Id)) return;
                ItemTypeDTO snapDto = _mapper.Map<ItemTypeDTO>(entity);
                snapDto.Context = this;
                snapDto.ItemCategories.Add(new ItemCategoryDTO { IsDummyChild = true, SubCategories = [], Name = "Dummy" });
                snapCatalog.ItemTypes.Add(snapDto);
            });

            _notificationService.ShowSuccess(message.CreatedItemType.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemTypeUpdateMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ItemTypeGraphQLModel entity = message.UpdatedItemType.Entity;
                foreach (ObservableCollection<CatalogDTO> roots in CatalogScopes())
                {
                    CatalogDTO? catalog = roots.FirstOrDefault(c => c.Id == entity.Catalog?.Id);
                    if (catalog is null) continue;
                    ItemTypeDTO? existing = catalog.ItemTypes.FirstOrDefault(t => t.Id == entity.Id);
                    if (existing is null) continue;
                    existing.Name = entity.Name;
                    existing.PrefixChar = entity.PrefixChar;
                    existing.StockControl = entity.StockControl;
                    existing.DefaultMeasurementUnit = _mapper.Map<Models.Inventory.MeasurementUnitDTO>(entity.DefaultMeasurementUnit);
                    existing.DefaultAccountingGroup = _mapper.Map<Models.Books.AccountingGroupDTO>(entity.DefaultAccountingGroup);
                }
            });
            _notificationService.ShowSuccess(message.UpdatedItemType.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemTypeDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                int id = message.DeletedItemType.DeletedId ?? 0;
                foreach (ObservableCollection<CatalogDTO> roots in CatalogScopes())
                    foreach (CatalogDTO catalog in roots)
                    {
                        ItemTypeDTO? toRemove = catalog.ItemTypes.FirstOrDefault(t => t.Id == id);
                        if (toRemove != null) { catalog.ItemTypes.Remove(toRemove); break; }
                    }
                SelectedItem = null;
            });
            _notificationService.ShowSuccess(message.DeletedItemType.Message);
            return Task.CompletedTask;
        }

        #endregion

        #region IHandle — ItemCategory

        public async Task HandleAsync(ItemCategoryCreateMessage message, CancellationToken cancellationToken)
        {
            ItemCategoryGraphQLModel entity = message.CreatedItemCategory.Entity;
            ItemTypeDTO? itemType = null;
            foreach (CatalogDTO c in Catalogs)
            {
                itemType = c.ItemTypes.FirstOrDefault(t => t.Id == entity.ItemType?.Id);
                if (itemType != null) break;
            }
            if (itemType is null) return;

            if (!itemType.IsExpanded && itemType.ItemCategories.Count > 0 && itemType.ItemCategories[0].IsDummyChild)
            {
                await LoadItemCategoriesAsync(itemType);
                itemType.IsExpanded = true;
                ItemCategoryDTO? found = itemType.ItemCategories.FirstOrDefault(x => x.Id == entity.Id);
                if (found != null) SelectedItem = found;
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ItemCategoryDTO dto = _mapper.Map<ItemCategoryDTO>(entity);
                    dto.Context = this;
                    dto.SubCategories.Add(new ItemSubCategoryDTO { IsDummyChild = true, Items = [], Name = "Dummy" });
                    if (!itemType.IsExpanded) itemType.IsExpanded = true;
                    itemType.ItemCategories.Add(dto);
                    SelectedItem = dto;
                });
            }

            MirrorCreateToSnapshot(snapshot =>
            {
                ItemTypeDTO? snapType = null;
                foreach (CatalogDTO c in snapshot)
                {
                    snapType = c.ItemTypes.FirstOrDefault(t => t.Id == entity.ItemType?.Id);
                    if (snapType != null) break;
                }
                if (snapType is null) return;
                bool isDummy = snapType.ItemCategories.Count > 0 && snapType.ItemCategories[0].IsDummyChild;
                if (isDummy) return; // will reload from API on next expand
                if (snapType.ItemCategories.Any(x => x.Id == entity.Id)) return;
                ItemCategoryDTO snapDto = _mapper.Map<ItemCategoryDTO>(entity);
                snapDto.Context = this;
                snapDto.SubCategories.Add(new ItemSubCategoryDTO { IsDummyChild = true, Items = [], Name = "Dummy" });
                snapType.ItemCategories.Add(snapDto);
            });

            _notificationService.ShowSuccess(message.CreatedItemCategory.Message);
        }

        public Task HandleAsync(ItemCategoryUpdateMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ItemCategoryGraphQLModel entity = message.UpdatedItemCategory.Entity;
                foreach (ObservableCollection<CatalogDTO> roots in CatalogScopes())
                    foreach (CatalogDTO c in roots)
                        foreach (ItemTypeDTO t in c.ItemTypes)
                        {
                            ItemCategoryDTO? existing = t.ItemCategories.FirstOrDefault(x => x.Id == entity.Id);
                            if (existing != null) { existing.Name = entity.Name; break; }
                        }
            });
            _notificationService.ShowSuccess(message.UpdatedItemCategory.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemCategoryDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                int id = message.DeletedItemCategory.DeletedId ?? 0;
                foreach (ObservableCollection<CatalogDTO> roots in CatalogScopes())
                    foreach (CatalogDTO c in roots)
                        foreach (ItemTypeDTO t in c.ItemTypes)
                        {
                            ItemCategoryDTO? toRemove = t.ItemCategories.FirstOrDefault(x => x.Id == id);
                            if (toRemove != null) { t.ItemCategories.Remove(toRemove); break; }
                        }
                SelectedItem = null;
            });
            _notificationService.ShowSuccess(message.DeletedItemCategory.Message);
            return Task.CompletedTask;
        }

        #endregion

        #region IHandle — ItemSubCategory

        public async Task HandleAsync(ItemSubCategoryCreateMessage message, CancellationToken cancellationToken)
        {
            ItemSubCategoryGraphQLModel entity = message.CreatedItemSubCategory.Entity;
            ItemCategoryDTO? parent = null;
            foreach (CatalogDTO c in Catalogs)
                foreach (ItemTypeDTO t in c.ItemTypes)
                {
                    parent = t.ItemCategories.FirstOrDefault(x => x.Id == entity.ItemCategory?.Id);
                    if (parent != null) break;
                }
            if (parent is null) return;

            if (!parent.IsExpanded && parent.SubCategories.Count > 0 && parent.SubCategories[0].IsDummyChild)
            {
                await LoadItemSubCategoriesAsync(parent);
                parent.IsExpanded = true;
                ItemSubCategoryDTO? found = parent.SubCategories.FirstOrDefault(x => x.Id == entity.Id);
                if (found != null) SelectedItem = found;
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ItemSubCategoryDTO dto = _mapper.Map<ItemSubCategoryDTO>(entity);
                    dto.Context = this;
                    dto.Items.Add(new ItemDTO { IsDummyChild = true, EanCodes = [], Name = "Dummy" });
                    if (!parent.IsExpanded) parent.IsExpanded = true;
                    parent.SubCategories.Add(dto);
                    SelectedItem = dto;
                });
            }

            MirrorCreateToSnapshot(snapshot =>
            {
                ItemCategoryDTO? snapParent = null;
                foreach (CatalogDTO c in snapshot)
                    foreach (ItemTypeDTO t in c.ItemTypes)
                    {
                        snapParent = t.ItemCategories.FirstOrDefault(x => x.Id == entity.ItemCategory?.Id);
                        if (snapParent != null) break;
                    }
                if (snapParent is null) return;
                bool isDummy = snapParent.SubCategories.Count > 0 && snapParent.SubCategories[0].IsDummyChild;
                if (isDummy) return;
                if (snapParent.SubCategories.Any(x => x.Id == entity.Id)) return;
                ItemSubCategoryDTO snapDto = _mapper.Map<ItemSubCategoryDTO>(entity);
                snapDto.Context = this;
                snapDto.Items.Add(new ItemDTO { IsDummyChild = true, EanCodes = [], Name = "Dummy" });
                snapParent.SubCategories.Add(snapDto);
            });

            _notificationService.ShowSuccess(message.CreatedItemSubCategory.Message);
        }

        public Task HandleAsync(ItemSubCategoryUpdateMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ItemSubCategoryGraphQLModel entity = message.UpdatedItemSubCategory.Entity;
                foreach (ObservableCollection<CatalogDTO> roots in CatalogScopes())
                    foreach (CatalogDTO c in roots)
                        foreach (ItemTypeDTO t in c.ItemTypes)
                            foreach (ItemCategoryDTO cat in t.ItemCategories)
                            {
                                ItemSubCategoryDTO? existing = cat.SubCategories.FirstOrDefault(x => x.Id == entity.Id);
                                if (existing != null) { existing.Name = entity.Name; break; }
                            }
            });
            _notificationService.ShowSuccess(message.UpdatedItemSubCategory.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemSubCategoryDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                int id = message.DeletedItemSubCategory.DeletedId ?? 0;
                foreach (ObservableCollection<CatalogDTO> roots in CatalogScopes())
                    foreach (CatalogDTO c in roots)
                        foreach (ItemTypeDTO t in c.ItemTypes)
                            foreach (ItemCategoryDTO cat in t.ItemCategories)
                            {
                                ItemSubCategoryDTO? toRemove = cat.SubCategories.FirstOrDefault(x => x.Id == id);
                                if (toRemove != null) { cat.SubCategories.Remove(toRemove); break; }
                            }
                SelectedItem = null;
            });
            _notificationService.ShowSuccess(message.DeletedItemSubCategory.Message);
            return Task.CompletedTask;
        }

        #endregion

        #region IHandle — Item

        public async Task HandleAsync(ItemCreateMessage message, CancellationToken cancellationToken)
        {
            ItemGraphQLModel entity = message.CreatedItem.Entity;
            ItemSubCategoryDTO? parent = null;
            foreach (CatalogDTO c in Catalogs)
                foreach (ItemTypeDTO t in c.ItemTypes)
                    foreach (ItemCategoryDTO cat in t.ItemCategories)
                    {
                        parent = cat.SubCategories.FirstOrDefault(x => x.Id == entity.SubCategory?.Id);
                        if (parent != null) break;
                    }
            if (parent is null) return;

            if (!parent.IsExpanded && parent.Items.Count > 0 && parent.Items[0].IsDummyChild)
            {
                await LoadItemsAsync(parent);
                parent.IsExpanded = true;
                ItemDTO? found = parent.Items.FirstOrDefault(x => x.Id == entity.Id);
                if (found != null) SelectedItem = found;
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ItemDTO dto = _mapper.Map<ItemDTO>(entity);
                    dto.Context = this;
                    if (!parent.IsExpanded) parent.IsExpanded = true;
                    parent.Items.Add(dto);
                    SelectedItem = dto;
                });
            }

            MirrorCreateToSnapshot(snapshot =>
            {
                ItemSubCategoryDTO? snapParent = null;
                foreach (CatalogDTO c in snapshot)
                    foreach (ItemTypeDTO t in c.ItemTypes)
                        foreach (ItemCategoryDTO cat in t.ItemCategories)
                        {
                            snapParent = cat.SubCategories.FirstOrDefault(x => x.Id == entity.SubCategory?.Id);
                            if (snapParent != null) break;
                        }
                if (snapParent is null) return;
                bool isDummy = snapParent.Items.Count > 0 && snapParent.Items[0].IsDummyChild;
                if (isDummy) return;
                if (snapParent.Items.Any(x => x.Id == entity.Id)) return;
                ItemDTO snapDto = _mapper.Map<ItemDTO>(entity);
                snapDto.Context = this;
                snapParent.Items.Add(snapDto);
            });

            _notificationService.ShowSuccess(message.CreatedItem.Message);
        }

        public Task HandleAsync(ItemUpdateMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ItemGraphQLModel entity = message.UpdatedItem.Entity;
                ItemDTO mapped = _mapper.Map<ItemDTO>(entity);
                foreach (ObservableCollection<CatalogDTO> roots in CatalogScopes())
                    foreach (CatalogDTO c in roots)
                        foreach (ItemTypeDTO t in c.ItemTypes)
                            foreach (ItemCategoryDTO cat in t.ItemCategories)
                                foreach (ItemSubCategoryDTO sub in cat.SubCategories)
                                {
                                    ItemDTO? existing = sub.Items.FirstOrDefault(x => x.Id == entity.Id);
                                    if (existing is null) continue;
                                    existing.Name = mapped.Name;
                                    existing.Reference = mapped.Reference;
                                    existing.IsActive = mapped.IsActive;
                                    existing.AllowFraction = mapped.AllowFraction;
                                    existing.HasExtendedInformation = mapped.HasExtendedInformation;
                                    existing.MeasurementUnit = mapped.MeasurementUnit;
                                    existing.Brand = mapped.Brand;
                                    existing.AccountingGroup = mapped.AccountingGroup;
                                    existing.SizeCategory = mapped.SizeCategory;
                                    existing.EanCodes = new ObservableCollection<EanCodeByItemDTO>(mapped.EanCodes ?? []);
                                    existing.Components = new ObservableCollection<ComponentsByItemDTO>(mapped.Components ?? []);
                                    existing.Images = new ObservableCollection<ImageByItemDTO>((mapped.Images ?? []).OrderBy(x => x.DisplayOrder));
                                    break;
                                }
            });
            _notificationService.ShowSuccess(message.UpdatedItem.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                int id = message.DeletedItem.DeletedId ?? 0;
                foreach (ObservableCollection<CatalogDTO> roots in CatalogScopes())
                    foreach (CatalogDTO c in roots)
                        foreach (ItemTypeDTO t in c.ItemTypes)
                            foreach (ItemCategoryDTO cat in t.ItemCategories)
                                foreach (ItemSubCategoryDTO sub in cat.SubCategories)
                                {
                                    ItemDTO? toRemove = sub.Items.FirstOrDefault(x => x.Id == id);
                                    if (toRemove != null) { sub.Items.Remove(toRemove); break; }
                                }
                SelectedItem = null;
            });
            _notificationService.ShowSuccess(message.DeletedItem.Message);
            return Task.CompletedTask;
        }

        #endregion

        #region IHandle — PermissionsCacheRefreshedMessage

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyAllPermissionStates();
            return Task.CompletedTask;
        }

        #endregion

        #region IHandle — S3 Config Changes

        public async Task HandleAsync(S3StorageLocationCreateMessage message, CancellationToken cancellationToken)
        {
            await ReloadS3ConfigAsync();
        }

        public async Task HandleAsync(S3StorageLocationUpdateMessage message, CancellationToken cancellationToken)
        {
            await ReloadS3ConfigAsync();
        }

        public async Task HandleAsync(AwsS3ConfigCreateMessage message, CancellationToken cancellationToken)
        {
            await ReloadS3ConfigAsync();
        }

        public async Task HandleAsync(AwsS3ConfigUpdateMessage message, CancellationToken cancellationToken)
        {
            await ReloadS3ConfigAsync();
        }

        private async Task ReloadS3ConfigAsync()
        {
            try
            {
                await LoadS3ConfigAsync();
                // Recrear el panel con el nuevo S3Helper
                PanelItemDetail = null;
                EnsurePanelItemDetail();
                SyncPanelWithSelection();
            }
            catch
            {
                // Non-critical — S3 config reload failure doesn't affect core functionality
            }
        }

        #endregion

        #region Queries (Lazy)

        private static Lazy<(GraphQLQueryFragment Fragment, string Query)> BuildCanDeleteLazy(string fragmentName) => new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();
            GraphQLQueryFragment fragment = new(fragmentName, [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static Lazy<(GraphQLQueryFragment Fragment, string Query)> BuildDeleteLazy(string fragmentName) => new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();
            GraphQLQueryFragment fragment = new(fragmentName, [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteCatalogQuery = BuildCanDeleteLazy("canDeleteCatalog");
        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteItemTypeQuery = BuildCanDeleteLazy("canDeleteItemType");
        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteItemCategoryQuery = BuildCanDeleteLazy("canDeleteItemCategory");
        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteItemSubCategoryQuery = BuildCanDeleteLazy("canDeleteItemSubCategory");
        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteItemQuery = BuildCanDeleteLazy("canDeleteItem");

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteCatalogQuery = BuildDeleteLazy("deleteCatalog");
        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteItemTypeQuery = BuildDeleteLazy("deleteItemType");
        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteItemCategoryQuery = BuildDeleteLazy("deleteItemCategory");
        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteItemSubCategoryQuery = BuildDeleteLazy("deleteItemSubCategory");
        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteItemQuery = BuildDeleteLazy("deleteItem");

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadItemCategoriesQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<PageType<ItemCategoryGraphQLModel>>
                .Create()
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Select(e => e.ItemType, it => it.Field(t => t.Id)))
                .Build();
            GraphQLQueryFragment fragment = new("itemCategoriesPage",
                [new("pagination", "Pagination"), new("filters", "ItemCategoryFilters")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadItemSubCategoriesQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<PageType<ItemSubCategoryGraphQLModel>>
                .Create()
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Select(e => e.ItemCategory, ic => ic
                        .Field(c => c.Id)
                        .Select(c => c.ItemType, it => it.Field(t => t.Id))))
                .Build();
            GraphQLQueryFragment fragment = new("itemSubCategoriesPage",
                [new("pagination", "Pagination"), new("filters", "ItemSubCategoryFilters")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadItemsQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<PageType<ItemGraphQLModel>>
                .Create()
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Code)
                    .Field(e => e.Reference)
                    .Field(e => e.IsActive)
                    .Field(e => e.AllowFraction)
                    .Field(e => e.HasExtendedInformation)
                    .Field(e => e.Billable)
                    .Field(e => e.AmountBasedOnWeight)
                    .Field(e => e.AiuBasedService)
                    .Field(e => e.IsLotTracked)
                    .Field(e => e.IsSerialTracked)
                    .SelectList(e => e.EanCodes, ean => ean
                        .Field(ec => ec.EanCode)
                        .Field(ec => ec.IsInternal))
                    .Select(e => e.MeasurementUnit, mu => mu.Field(m => m.Id))
                    .Select(e => e.Brand, b => b.Field(br => br.Id))
                    .Select(e => e.AccountingGroup, ag => ag.Field(a => a.Id))
                    .Select(e => e.SizeCategory, sc => sc.Field(s => s.Id))
                    .SelectList(e => e.Components, comp => comp
                        .Field(c => c.Quantity)
                        .Select(c => c.Component, ci => ci
                            .Field(i => i.Id)
                            .Field(i => i.Name)
                            .Field(i => i.Reference)
                            .Field(i => i.Code)
                            .Select(i => i.MeasurementUnit, mu => mu.Field(m => m.Id).Field(m => m.Name))))
                    .SelectList(e => e.Images, img => img
                        .Field(i => i.DisplayOrder)
                        .Field(i => i.S3Bucket)
                        .Field(i => i.S3BucketDirectory)
                        .Field(i => i.S3FileName)
                        .Select(i => i.Item, item => item.Field(it => it.Id)))
                    .Select(e => e.SubCategory, sub => sub
                        .Field(s => s.Id)
                        .Select(s => s.ItemCategory, ic => ic
                            .Field(c => c.Id)
                            .Select(c => c.ItemType, it => it.Field(t => t.Id).Field(t => t.StockControl)))))
                .Build();
            GraphQLQueryFragment fragment = new("itemsPage",
                [new("pagination", "Pagination"), new("filters", "ItemFilters")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _searchItemsQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<PageType<ItemGraphQLModel>>
                .Create()
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Code)
                    .Field(e => e.Reference)
                    .Field(e => e.IsActive)
                    .Field(e => e.AllowFraction)
                    .Field(e => e.HasExtendedInformation)
                    .Field(e => e.Billable)
                    .Field(e => e.AmountBasedOnWeight)
                    .Field(e => e.AiuBasedService)
                    .SelectList(e => e.EanCodes, ean => ean
                        .Field(ec => ec.EanCode)
                        .Field(ec => ec.IsInternal))
                    .Select(e => e.MeasurementUnit, mu => mu.Field(m => m.Id))
                    .Select(e => e.Brand, b => b.Field(br => br.Id))
                    .Select(e => e.AccountingGroup, ag => ag.Field(a => a.Id))
                    .Select(e => e.SizeCategory, sc => sc.Field(s => s.Id))
                    .Select(e => e.SubCategory, sub => sub
                        .Field(s => s.Id)
                        .Field(s => s.Name)
                        .Select(s => s.ItemCategory, ic => ic
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                            .Select(c => c.ItemType, it => it
                                .Field(t => t.Id)
                                .Field(t => t.Name)
                                .Field(t => t.PrefixChar)
                                .Field(t => t.StockControl)
                                .Select(t => t.Catalog, cat => cat
                                    .Field(cc => cc.Id)
                                    .Field(cc => cc.Name))))))
                .Build();
            GraphQLQueryFragment fragment = new("itemsPage",
                [new("pagination", "Pagination"), new("filters", "ItemFilters")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _discontinueItemQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<UpsertResponseType<ItemGraphQLModel>>
                .Create()
                .Field(f => f.Success)
                .Field(f => f.Message)
                .Build();
            GraphQLQueryFragment fragment = new("updateItem",
                [new("data", "UpdateItemInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
