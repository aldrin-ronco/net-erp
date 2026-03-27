using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using Models.Books;
using Models.Global;
using Models.Inventory;
using NetErp.Helpers.Cache;
using NetErp.Inventory.CatalogItems.DTO;
using System;
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

        // Caches
        private readonly IGraphQLClient _graphQLClient;
        private readonly MeasurementUnitCache _measurementUnitCache;
        private readonly ItemBrandCache _itemBrandCache;
        private readonly AccountingGroupCache _accountingGroupCache;
        private readonly ItemSizeCategoryCache _itemSizeCategoryCache;
        private readonly StringLengthCache _stringLengthCache;

        private CatalogRootMasterViewModel _catalogRootMasterViewModel;

        public CatalogRootMasterViewModel CatalogRootMasterViewModel
        {
            get
            {
                if (_catalogRootMasterViewModel is null) _catalogRootMasterViewModel = new CatalogRootMasterViewModel(
                    this,
                    _catalogService,
                    _itemTypeService,
                    _itemCategoryService,
                    _itemSubCategoryService,
                    _itemService,
                    _s3LocationService,
                    _dialogService,
                    _notificationService,
                    _measurementUnitCache,
                    _itemBrandCache,
                    _accountingGroupCache,
                    _itemSizeCategoryCache,
                    _stringLengthCache,
                    _graphQLClient);
                return _catalogRootMasterViewModel;
            }
        }

        private bool _enableOnActivateAsync = true;

        public bool EnableOnActivateAsync
        {
            get { return _enableOnActivateAsync; }
            set
            {
                if(_enableOnActivateAsync != value)
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
            MeasurementUnitCache measurementUnitCache,
            ItemBrandCache itemBrandCache,
            AccountingGroupCache accountingGroupCache,
            ItemSizeCategoryCache itemSizeCategoryCache,
            StringLengthCache stringLengthCache,
            IGraphQLClient graphQLClient)
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
            _measurementUnitCache = measurementUnitCache;
            _itemBrandCache = itemBrandCache;
            _accountingGroupCache = accountingGroupCache;
            _itemSizeCategoryCache = itemSizeCategoryCache;
            _stringLengthCache = stringLengthCache;
            _graphQLClient = graphQLClient;
            Task.Run(ActivateMasterView);
        }

        public async Task ActivateMasterView()
        {
            try
            {
                await ActivateItemAsync(CatalogRootMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
