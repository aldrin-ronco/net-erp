using AutoMapper;
using Caliburn.Micro;
using Models.Books;
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

        private CatalogMasterViewModel _catalogMasterViewModel;

        public CatalogMasterViewModel CatalogMasterViewModel
        {
            get
            {
                if (_catalogMasterViewModel is null) _catalogMasterViewModel = new CatalogMasterViewModel(this);
                return _catalogMasterViewModel;
            }
        }

        public CatalogViewModel(IMapper mapper, IEventAggregator eventAggregator)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
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
                ItemTypeDetailViewModel instance = new(this, measurementUnits, accountingGroups);
                instance.CatalogId = selectedCatalogId;
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
                ItemTypeDetailViewModel instance = new(this, measurementUnits, accountingGroups);
                instance.Id = itemType.Id;
                instance.Name = itemType.Name;
                instance.PrefixChar = itemType.PrefixChar;
                instance.StockControl = itemType.StockControl;
                instance.CatalogId = itemType.Catalog.Id;
                instance.MeasurementUnitIdByDefault = itemType.MeasurementUnitByDefault.Id;
                instance.AccountingGroupIdByDefault = itemType.AccountingGroupByDefault.Id;
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
                CategoryDetailViewModel instance = new(this)
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
                CategoryDetailViewModel instance = new(this)
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
                SubCategoryDetailViewModel instance = new(this)
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
                SubCategoryDetailViewModel instance = new(this)
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
                CatalogDetailViewModel instance = new(this);
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
                CatalogDetailViewModel instance = new(this)
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
