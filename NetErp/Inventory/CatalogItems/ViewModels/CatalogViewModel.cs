using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using Models.Inventory;
using NetErp.Helpers.Cache;
using NetErp.Inventory.CatalogItems.Validators;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Inventory.CatalogItems.ViewModels
{
    public class CatalogViewModel : Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; set; }

        private readonly IRepository<CatalogGraphQLModel> _catalogService;
        private readonly IRepository<ItemTypeGraphQLModel> _itemTypeService;
        private readonly IRepository<ItemCategoryGraphQLModel> _itemCategoryService;
        private readonly IRepository<ItemSubCategoryGraphQLModel> _itemSubCategoryService;
        private readonly IRepository<ItemGraphQLModel> _itemService;
        private readonly IRepository<S3StorageLocationGraphQLModel> _s3LocationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        // Caches
        private readonly IGraphQLClient _graphQLClient;
        private readonly CatalogCache _catalogCache;
        private readonly MeasurementUnitCache _measurementUnitCache;
        private readonly ItemBrandCache _itemBrandCache;
        private readonly AccountingGroupCache _accountingGroupCache;
        private readonly ItemSizeCategoryCache _itemSizeCategoryCache;
        private readonly StringLengthCache _stringLengthCache;

        // Validators
        private readonly CatalogValidator _catalogValidator;
        private readonly ItemTypeValidator _itemTypeValidator;
        private readonly ItemCategoryValidator _itemCategoryValidator;
        private readonly ItemSubCategoryValidator _itemSubCategoryValidator;
        private readonly ItemValidator _itemValidator;

        private readonly PermissionCache _permissionCache;

        private CatalogRootMasterViewModel? _catalogRootMasterViewModel;
        public CatalogRootMasterViewModel CatalogRootMasterViewModel
        {
            get
            {
                _catalogRootMasterViewModel ??= new CatalogRootMasterViewModel(
                    this,
                    _catalogService,
                    _itemTypeService,
                    _itemCategoryService,
                    _itemSubCategoryService,
                    _itemService,
                    _s3LocationService,
                    _dialogService,
                    _notificationService,
                    EventAggregator,
                    AutoMapper,
                    _joinableTaskFactory,
                    _catalogCache,
                    _measurementUnitCache,
                    _itemBrandCache,
                    _accountingGroupCache,
                    _itemSizeCategoryCache,
                    _stringLengthCache,
                    _catalogValidator,
                    _itemTypeValidator,
                    _itemCategoryValidator,
                    _itemSubCategoryValidator,
                    _itemValidator,
                    _graphQLClient,
                    _permissionCache);
                return _catalogRootMasterViewModel;
            }
        }

        private bool _enableOnActivateAsync = true;
        public bool EnableOnActivateAsync
        {
            get => _enableOnActivateAsync;
            set
            {
                if (_enableOnActivateAsync != value)
                {
                    _enableOnActivateAsync = value;
                    NotifyOfPropertyChange(nameof(EnableOnActivateAsync));
                }
            }
        }

        public CatalogViewModel(
            IMapper mapper,
            IEventAggregator eventAggregator,
            IRepository<CatalogGraphQLModel> catalogService,
            IRepository<ItemTypeGraphQLModel> itemTypeService,
            IRepository<ItemCategoryGraphQLModel> itemCategoryService,
            IRepository<ItemSubCategoryGraphQLModel> itemSubCategoryService,
            IRepository<ItemGraphQLModel> itemService,
            IRepository<S3StorageLocationGraphQLModel> s3LocationService,
            Helpers.IDialogService dialogService,
            Helpers.Services.INotificationService notificationService,
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
            PermissionCache permissionCache)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
            _catalogService = catalogService;
            _itemTypeService = itemTypeService;
            _itemCategoryService = itemCategoryService;
            _itemSubCategoryService = itemSubCategoryService;
            _itemService = itemService;
            _s3LocationService = s3LocationService;
            _dialogService = dialogService;
            _notificationService = notificationService;
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
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await base.OnActivateAsync(cancellationToken);
            try
            {
                // Activate via the Caliburn lifecycle (UI thread) instead of Task.Run.
                // This ensures CatalogRootMasterViewModel is constructed on the UI thread,
                // so SubscribeOnUIThread(this) wires the event aggregator handlers correctly
                // — including IHandle<PermissionsCacheRefreshedMessage> for reactive refresh.
                await ActivateItemAsync(CatalogRootMasterViewModel, cancellationToken);
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    DevExpress.Xpf.Core.ThemedMessageBox.Show(
                        "Error de inicialización",
                        $"{GetType().Name}.{nameof(OnActivateAsync)}: {ex.Message}",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error));
            }
        }
    }
}
