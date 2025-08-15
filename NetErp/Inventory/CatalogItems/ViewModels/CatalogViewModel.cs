using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using Models.Books;
using Models.Global;
using Models.Inventory;
using NetErp.Inventory.CatalogItems.DTO;
using NetErp.Inventory.MeasurementUnits.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
        private readonly IRepository<MeasurementUnitGraphQLModel> _measurementUnitService;
        private readonly IRepository<AwsS3ConfigGraphQLModel> _awsS3Service;
        private readonly Helpers.IDialogService _dialogService;
        private readonly Helpers.Services.INotificationService _notificationService;

        private CatalogMasterViewModel _catalogMasterViewModel;

        public CatalogMasterViewModel CatalogMasterViewModel
        {
            get
            {
                if (_catalogMasterViewModel is null) _catalogMasterViewModel = new CatalogMasterViewModel(this, _catalogService, _itemTypeService, _itemCategoryService, _itemSubCategoryService, _itemService, _measurementUnitService, _awsS3Service, _dialogService, _notificationService);
                return _catalogMasterViewModel;
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
            IRepository<MeasurementUnitGraphQLModel> measurementUnitService,
            IRepository<AwsS3ConfigGraphQLModel> awsS3Service,
            Helpers.IDialogService dialogService,
            Helpers.Services.INotificationService notificationService)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
            _catalogService = catalogService;
            _itemTypeService = itemTypeService;
            _itemCategoryService = itemCategoryService;
            _itemSubCategoryService = itemSubCategoryService;
            _itemService = itemService;
            _measurementUnitService = measurementUnitService;
            _awsS3Service = awsS3Service;
            _dialogService = dialogService;
            _notificationService = notificationService;
            Task.Run(ActivateMasterView);
        }

        public async Task ActivateMasterView()
        {
            try
            {
                await ActivateItemAsync(CatalogMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task ActivateItemTypeDetailForNew(int selectedCatalogId, ObservableCollection<MeasurementUnitDTO> measurementUnits, ObservableCollection<AccountingGroupDTO> accountingGroups)
        {
            try
            {
                ItemTypeDetailViewModel instance = new(this, measurementUnits, accountingGroups, _itemTypeService);
                instance.CatalogId = selectedCatalogId;
                instance.StockControlEnable = true;
                instance.CleanUpControls();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task ActivateItemTypeDetailForEdit(ItemTypeDTO itemType, ObservableCollection<MeasurementUnitDTO> measurementUnits, ObservableCollection<AccountingGroupDTO> accountingGroups)
        {
            try
            {
                ItemTypeDetailViewModel instance = new(this, measurementUnits, accountingGroups, _itemTypeService);
                instance.Id = itemType.Id;
                instance.Name = itemType.Name;
                instance.PrefixChar = itemType.PrefixChar;
                instance.StockControl = itemType.StockControl;
                instance.CatalogId = itemType.Catalog.Id;
                instance.MeasurementUnitIdByDefault = itemType.MeasurementUnitByDefault.Id;
                instance.AccountingGroupIdByDefault = itemType.AccountingGroupByDefault.Id;
                instance.StockControlEnable = false;
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task ActivateItemCategoryDetailForNew(int selectedItemType)
        {
            try
            {
                CategoryDetailViewModel instance = new(this, _itemCategoryService)
                {
                    ItemTypeId = selectedItemType
                };
                instance.CleanUpControls();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task ActivateItemCategoryDetailForEdit(ItemCategoryDTO itemCategory)
        {
            try
            {
                CategoryDetailViewModel instance = new(this, _itemCategoryService)
                {
                    Id = itemCategory.Id,
                    Name = itemCategory.Name,
                    ItemTypeId = itemCategory.ItemType.Id
                };
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task ActivateItemSubCategoryDetailForNew(int selectedItemCategory)
        {
            try
            {
                SubCategoryDetailViewModel instance = new(this, _itemSubCategoryService)
                {
                    ItemCategoryId = selectedItemCategory
                };
                instance.CleanUpControls();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task ActivateItemSubCategoryDetailForEdit(ItemSubCategoryDTO itemSubCategory)
        {
            try
            {
                SubCategoryDetailViewModel instance = new(this, _itemSubCategoryService)
                {
                    Id = itemSubCategory.Id,
                    Name = itemSubCategory.Name,
                    ItemCategoryId = itemSubCategory.ItemCategory.Id
                };
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task ActivateCatalogDetailForNew()
        {
            try
            {
                CatalogDetailViewModel instance = new(this, _catalogService);
                instance.CleanUpControls();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task ActivateCatalogDetailForEdit(CatalogDTO catalog)
        {
            try
            {
                CatalogDetailViewModel instance = new(this, _catalogService)
                {
                    Id = catalog.Id,
                    Name = catalog.Name
                };
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
