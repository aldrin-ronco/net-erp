using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Interfaces;
using DevExpress.Data.Utils;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Billing;
using Models.Books;
using Models.Global;
using Models.Inventory;
using NetErp.Books.AccountingAccounts.DTO;
using NetErp.Helpers;
using NetErp.Inventory.CatalogItems.DTO;
using NetErp.Inventory.ItemSizes.DTO;
using NetErp.IoContainer;
using Services.Inventory.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using Force.DeepCloner;

namespace NetErp.Inventory.CatalogItems.ViewModels
{
    public class CatalogMasterViewModel : Screen
    {
        public readonly IGenericDataAccess<CatalogGraphQLModel> CatalogService = IoC.Get<IGenericDataAccess<CatalogGraphQLModel>>();

        public readonly IGenericDataAccess<ItemTypeGraphQLModel> ItemTypeService = IoC.Get<IGenericDataAccess<ItemTypeGraphQLModel>>();

        public readonly IGenericDataAccess<ItemCategoryGraphQLModel> ItemCategoryService = IoC.Get<IGenericDataAccess<ItemCategoryGraphQLModel>>();

        public readonly IGenericDataAccess<ItemSubCategoryGraphQLModel> ItemSubCategoryService = IoC.Get<IGenericDataAccess<ItemSubCategoryGraphQLModel>>();

        public readonly IGenericDataAccess<ItemGraphQLModel> ItemService = IoC.Get<IGenericDataAccess<ItemGraphQLModel>>();

        public readonly IGenericDataAccess<MeasurementUnitGraphQLModel> MeasurementUnitService = IoC.Get<IGenericDataAccess<MeasurementUnitGraphQLModel>>();


        private ItemDTO _itemForEditing;

        public ItemDTO ItemForEditing
        {
            get { return _itemForEditing; }
            set 
            {
                if(_itemForEditing != value)
                {
                    _itemForEditing = value; 
                    NotifyOfPropertyChange(nameof(ItemForEditing));
                }
            }
        }


        private CatalogViewModel _context;

        public CatalogViewModel Context
        {
            get { return _context; }
            set
            {
                if (_context != value)
                {
                    _context = value;
                    NotifyOfPropertyChange(nameof(Context));
                }
            }
        }

        private ObservableCollection<MeasurementUnitDTO> _measurementUnits;

        public ObservableCollection<MeasurementUnitDTO> MeasurementUnits
        {
            get { return _measurementUnits; }
            set
            {
                if (_measurementUnits != value)
                {
                    _measurementUnits = value;
                    NotifyOfPropertyChange(nameof(MeasurementUnits));
                }
            }
        }

        private bool _isEditing = false;

        public bool IsEditing
        {
            get { return _isEditing; }
            set 
            {
                if(_isEditing != value)
                {
                    _isEditing = value;
                    NotifyOfPropertyChange(nameof(IsEditing));
                }
            }
        }


        private ObservableCollection<BrandDTO> _brands;

        public ObservableCollection<BrandDTO> Brands
        {
            get { return _brands; }
            set
            {
                if (_brands != value)
                {
                    _brands = value;
                    NotifyOfPropertyChange(nameof(Brands));
                }
            }
        }

        private ObservableCollection<AccountingGroupDTO> _accountingGroups;

        public ObservableCollection<AccountingGroupDTO> AccountingGroups
        {
            get { return _accountingGroups; }
            set
            {
                if (_accountingGroups != value)
                {
                    _accountingGroups = value;
                    NotifyOfPropertyChange(nameof(AccountingGroups));
                }
            }
        }

        private ObservableCollection<ItemSizeMasterDTO> _sizes;

        public ObservableCollection<ItemSizeMasterDTO> Sizes
        {
            get { return _sizes; }
            set
            {
                if (_sizes != value)
                {
                    _sizes = value;
                    NotifyOfPropertyChange(nameof(Sizes));
                }
            }
        }

        private ItemDTO _selectedItem = new();
        public ItemDTO SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    ItemForEditing = (ItemDTO)SelectedItem.Clone();
                    if (_selectedItem != null)
                        UpdateComboBoxes();
                }
            }
        }

        private MeasurementUnitDTO _selectedMeasurementUnit;

        public MeasurementUnitDTO SelectedMeasurementUnit
        {
            get { return _selectedMeasurementUnit; }
            set
            {
                if (_selectedMeasurementUnit != value)
                {
                    _selectedMeasurementUnit = value;
                    NotifyOfPropertyChange(nameof(SelectedMeasurementUnit));
                }
            }
        }

        private BrandDTO _selectedBrand;

        public BrandDTO SelectedBrand
        {
            get { return _selectedBrand; }
            set
            {
                if (_selectedBrand != value)
                {
                    _selectedBrand = value;
                    NotifyOfPropertyChange(nameof(SelectedBrand));
                }
            }
        }

        private AccountingGroupDTO _selectedAccountingGroup;

        public AccountingGroupDTO SelectedAccountingGroup
        {
            get { return _selectedAccountingGroup; }
            set
            {
                if (_selectedAccountingGroup != value)
                {
                    _selectedAccountingGroup = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingGroup));
                }
            }
        }

        private ItemSizeMasterDTO _selectedSize;

        public ItemSizeMasterDTO SelectedSize
        {
            get { return _selectedSize; }
            set
            {
                if (_selectedSize != value)
                {
                    _selectedSize = value;
                    NotifyOfPropertyChange(nameof(SelectedSize));
                }
            }
        }

        private ObservableCollection<CatalogDTO> _catalogs = [];

        public ObservableCollection<CatalogDTO> Catalogs
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

        private CatalogDTO _selectedCatalog;

        public CatalogDTO SelectedCatalog
        {
            get { return _selectedCatalog; }
            set
            {
                if (_selectedCatalog != value)
                {
                    _selectedCatalog = value;
                    NotifyOfPropertyChange(nameof(SelectedCatalog));
                }
            }
        }

        private ObservableCollection<ItemCategoryDTO> _itemsCategories = [];
        public ObservableCollection<ItemCategoryDTO> ItemsCategories
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

        private ObservableCollection<ItemSubCategoryDTO> _itemsSubCategories = [];
        public ObservableCollection<ItemSubCategoryDTO> ItemsSubCategories
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

        private ObservableCollection<ItemDTO> _items = [];
        public ObservableCollection<ItemDTO> Items
        {
            get { return _items; }
            set
            {
                if (_items != value)
                {
                    _items = value;
                    NotifyOfPropertyChange(nameof(Items));
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

        private ICommand _editItemCommand;

        public ICommand EditItemCommand
        {
            get
            {
                if (_editItemCommand is null) _editItemCommand = new AsyncCommand(EditItem, CanEditItem);
                return _editItemCommand;
            }
        }

        private bool _canEditItem = true;

        public bool CanEditItem
        {
            get { return _canEditItem; }
            set 
            {
                if (_canEditItem != value)
                {
                    _canEditItem = value; 
                    NotifyOfPropertyChange(nameof(CanEditItem));
                }
            }
        }



        public async Task EditItem()
        {
            IsEditing = true;
            CanUndo = true;
            CanEditItem = false;
        }

        private ICommand _undoCommand;

        public ICommand UndoCommand
        {
            get
            {
                if (_undoCommand is null) _undoCommand = new AsyncCommand(Undo, CanUndo);
                return _undoCommand;
            }
        }

        public async Task Undo()
        {
            ItemForEditing = SelectedItem;
            IsEditing = false;
            CanUndo = false;
            CanEditItem = true;
        }

        private bool _canUndo = false;

        public bool CanUndo
        {
            get { return _canUndo; }
            set 
            {
                if (_canUndo != value)
                {
                    _canUndo = value; 
                    NotifyOfPropertyChange(nameof(CanUndo));
                }
            }
        }
        public CatalogMasterViewModel(CatalogViewModel context)
        {
            Context = context;
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = Task.Run(() => LoadCatalogs());
            _ = Task.Run(() => Initialize());
        }

        public async Task LoadCatalogs()
        {

            try
            {
                IsBusy = true;
                Refresh();
                string query = @"
                query{
                    ListResponse: catalogs{
                    id
                    name
                    itemsTypes{
                        id
                        name
                        prefixChar
                        stockControl
                        }
                    }
                }";

                dynamic variables = new ExpandoObject();
                //variables.filter = new ExpandoObject();
                // Iniciar cronometro
                Stopwatch stopwatch = new();
                stopwatch.Start();

                var source = await CatalogService.GetList(query, variables);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Catalogs = Context.AutoMapper.Map<ObservableCollection<CatalogDTO>>(source);
                    Catalogs.Insert(0, new CatalogDTO() { Id = 0, Name = "<< SELECCIONE UN CATALOGO DE PRODUCTOS >> " });
                });
                foreach (CatalogDTO catalog in Catalogs)
                {
                    foreach (ItemTypeDTO itemType in catalog.ItemsTypes)
                    {
                        itemType.Context = this;
                        itemType.ItemsCategories.Add(new ItemCategoryDTO() { IsDummyChild = true, SubCategories = [], Name = "Fucking Dummy" });
                    }
                }
                SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == 0);
                stopwatch.Stop();

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCatalogs" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task LoadItemsCategories(ItemTypeDTO itemType)
        {
            try
            {
                Refresh();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    itemType.ItemsCategories.Remove(itemType.ItemsCategories[0]);
                });
                List<int> ids = [itemType.Id];
                string query = @"
                    query($ids: [Int!]!){
                      ListResponse: itemsCategoriesByItemsTypesIds(ids: $ids){
                        id
                        name
                      }
                    }";
                dynamic variables = new ExpandoObject();
                variables.ids = ids;

                var source = await ItemCategoryService.GetList(query, variables);
                ItemsCategories = Context.AutoMapper.Map<ObservableCollection<ItemCategoryDTO>>(source);

                foreach (ItemCategoryDTO itemCategory in ItemsCategories)
                {
                    itemCategory.Context = this;
                    itemCategory.SubCategories.Add(new ItemSubCategoryDTO() { IsDummyChild = true, Items = [], Name = "Fucking Dummy" });
                }
                itemType.ItemsCategories = ItemsCategories;


            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadItemsCategories" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task LoadItemsSubCategories(ItemCategoryDTO itemCategory)
        {
            try
            {
                Refresh();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    itemCategory.SubCategories.Remove(itemCategory.SubCategories[0]);
                });
                List<int> ids = [itemCategory.Id];
                string query = @"
                    query($ids: [Int!]!){
                      ListResponse: itemsSubCategoriesByCategoriesIds(ids: $ids){
                        id
                        name
                      }
                    }";
                dynamic variables = new ExpandoObject();
                variables.ids = ids;

                var source = await ItemSubCategoryService.GetList(query, variables);
                ItemsSubCategories = Context.AutoMapper.Map<ObservableCollection<ItemSubCategoryDTO>>(source);

                foreach (ItemSubCategoryDTO itemSubCategory in ItemsSubCategories)
                {
                    itemSubCategory.Context = this;
                    itemSubCategory.Items.Add(new ItemDTO() { IsDummyChild = true, EanCodes = [], Name = "Fucking Dummy" });
                }
                itemCategory.SubCategories = ItemsSubCategories;


            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadItemsSubCategories" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task LoadItems(ItemSubCategoryDTO itemSubCategory)
        {
            try
            {
                Refresh();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    itemSubCategory.Items.Remove(itemSubCategory.Items[0]);
                });
                List<int> ids = [itemSubCategory.Id];
                string query = @"
                    query($ids: [Int!]!){
                      ListResponse: itemsBySubCategoriesIds(ids: $ids){
                        id
                        name
                        code
                        reference
                        isActive
                        allowFraction
                        hasExtendedInformation
                        billable
                        amountBasedOnWeight
                        aiuBasedService
                        measurementUnit{
                            id
                        }
                        brand{
                            id
                        }
                        accountingGroup{
                            id
                        }
                        size{
                            id
                        }
                        eanCodes{
                            id
                            eanCode
                        }
                      }
                    }";
                dynamic variables = new ExpandoObject();
                variables.ids = ids;

                var source = await ItemService.GetList(query, variables);
                Items = Context.AutoMapper.Map<ObservableCollection<ItemDTO>>(source);

                foreach (ItemDTO item in Items)
                {
                    item.Context = this;
                }
                itemSubCategory.Items = Items;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadItems" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task Initialize()
        {
            try
            {
                Refresh();
                string query = @"
                query{
                    measurementUnits{
                        id
                        name
                        }
                    brands{
                        id
                        name
                        }
                    accountingGroups{
                        id
                        name
                        }
                    sizes: itemsSizesMaster{
                        id
                        name
                        }
                }";

                var dataContext = await MeasurementUnitService.GetDataContext<CatalogMasterDataContext>(query, new { });
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MeasurementUnits = Context.AutoMapper.Map<ObservableCollection<MeasurementUnitDTO>>(dataContext.MeasurementUnits);
                    Brands = Context.AutoMapper.Map<ObservableCollection<BrandDTO>>(dataContext.Brands);
                    AccountingGroups = Context.AutoMapper.Map<ObservableCollection<AccountingGroupDTO>>(dataContext.AccountingGroups);
                    Sizes = Context.AutoMapper.Map<ObservableCollection<ItemSizeMasterDTO>>(dataContext.Sizes);
                    Brands.Insert(0, new BrandDTO() { Id = 0, Name = "<< SELECCIONE UNA MARCA >> " });
                    Sizes.Insert(0, new ItemSizeMasterDTO() { Id = 0, Name = "<< SELECCIONE UN GRUPO DE TALLAJE >>" });
                });

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "Initialize" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }
        public void UpdateComboBoxes()
        {
            if (SelectedItem.Brand is null)
            {
                SelectedBrand = Brands.FirstOrDefault(x => x.Id == 0);
            }
            else
            {
                SelectedBrand = Brands.FirstOrDefault(x => x.Id == SelectedItem.Brand.Id);
            };
            if (SelectedItem.Size is null)
            {
                SelectedSize = Sizes.FirstOrDefault(x => x.Id == 0);
            }
            else
            {
                SelectedSize = Sizes.FirstOrDefault(x => x.Id == SelectedItem.Size.Id);
            };
            if (SelectedItem.MeasurementUnit != null) SelectedMeasurementUnit = MeasurementUnits.FirstOrDefault(x => x.Id == SelectedItem.MeasurementUnit.Id);
            if (SelectedItem.AccountingGroup != null) SelectedAccountingGroup = AccountingGroups.FirstOrDefault(x => x.Id == SelectedItem.AccountingGroup.Id);

        }
    }

    public class CatalogMasterDataContext
    {
        public List<MeasurementUnitGraphQLModel> MeasurementUnits { get; set; }
        public List<BrandGraphQLModel> Brands { get; set; }
        public List<AccountingGroupGraphQLModel> AccountingGroups { get; set; }
        public List<ItemSizeMasterGraphQLModel> Sizes { get; set; }
    }
}
