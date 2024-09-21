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
using Services.Billing.DAL.PostgreSQL;
using System.Threading;
using DevExpress.Data;
using System.Collections;
using System.ComponentModel;
using Models.DTO.Global;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using Common.Helpers;

namespace NetErp.Inventory.CatalogItems.ViewModels
{
    public class CatalogMasterViewModel : Screen,
        IHandle<ItemTypeCreateMessage>,
        IHandle<ItemTypeDeleteMessage>,
        IHandle<ItemTypeUpdateMessage>,
        IHandle<ItemCategoryCreateMessage>,
        IHandle<ItemCategoryDeleteMessage>,
        IHandle<ItemCategoryUpdateMessage>,
        IHandle<ItemSubCategoryCreateMessage>,
        IHandle<ItemSubCategoryDeleteMessage>,
        IHandle<ItemSubCategoryUpdateMessage>,
        IHandle<CatalogCreateMessage>,
        IHandle<CatalogUpdateMessage>,
        IHandle<CatalogDeleteMessage>, 
        IHandle<ItemDeleteMessage>, 
        IHandle<ItemCreateMessage>, 
        IHandle<ItemUpdateMessage>, INotifyDataErrorInfo
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
                if (_itemForEditing != value)
                {
                    _itemForEditing = value;
                    NotifyOfPropertyChange(nameof(ItemForEditing));
                }
            }
        }

        private int _id;

        public int Id
        {
            get { return _id; }
            set 
            {
                if (_id != value)
                {
                    _id = value;
                    NotifyOfPropertyChange(nameof(Id));
                }
            }
        }


        private string _name = string.Empty;

        public string Name
        {
            get { return _name; }
            set 
            {
                if (_name != value)
                {
                    _name = value;
                    ValidateProperty(nameof(Name), value);
                    NotifyOfPropertyChange(nameof(Name));
                    NotifyOfPropertyChange(nameof(CanSaveItem));
                }
            }
        }

        private string _code = string.Empty;

        public string Code
        {
            get { return _code; }
            set
            {
                if (_code != value)
                {
                    _code = value;
                    NotifyOfPropertyChange(nameof(Code));
                }
            }
        }

        private string _reference = string.Empty;

        public string Reference
        {
            get { return _reference; }
            set
            {
                if (_reference != value)
                {
                    _reference = value;
                    NotifyOfPropertyChange(nameof(Reference));
                }
            }
        }

        private bool _isActive;

        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    NotifyOfPropertyChange(nameof(IsActive));
                }
            }
        }

        private bool _allowFraction;

        public bool AllowFraction
        {
            get { return _allowFraction; }
            set
            {
                if (_allowFraction != value)
                {
                    _allowFraction = value;
                    NotifyOfPropertyChange(nameof(AllowFraction));
                }
            }
        }

        private bool _billable;

        public bool Billable
        {
            get { return _billable; }
            set
            {
                if (_billable != value)
                {
                    _billable = value;
                    NotifyOfPropertyChange(nameof(Billable));
                }
            }
        }

        private bool _amountBasedOnWeight;

        public bool AmountBasedOnWeight
        {
            get { return _amountBasedOnWeight; }
            set
            {
                if (_amountBasedOnWeight != value)
                {
                    _amountBasedOnWeight = value;
                    NotifyOfPropertyChange(nameof(AmountBasedOnWeight));
                }
            }
        }

        private bool _hasExtendedInformation;

        public bool HasExtendedInformation
        {
            get { return _hasExtendedInformation; }
            set
            {
                if (_hasExtendedInformation != value)
                {
                    _hasExtendedInformation = value;
                    NotifyOfPropertyChange(nameof(HasExtendedInformation));
                }
            }
        }

        private bool _aiuBasedService;

        public bool AiuBasedService
        {
            get { return _aiuBasedService; }
            set
            {
                if (_aiuBasedService != value)
                {
                    _aiuBasedService = value;
                    NotifyOfPropertyChange(nameof(AiuBasedService));
                }
            }
        }

        private ObservableCollection<EanCodeDTO> _eanCodes;

        public ObservableCollection<EanCodeDTO> EanCodes
        {
            get { return _eanCodes; }
            set
            {
                if (_eanCodes != value)
                {
                    _eanCodes = value;
                    NotifyOfPropertyChange(nameof(EanCodes));
                }
            }
        }

        private string _eanCode;

        public string EanCode
        {
            get { return _eanCode; }
            set 
            {
                if (_eanCode != value) 
                {
                    _eanCode = value;
                    NotifyOfPropertyChange(nameof(EanCode));
                    NotifyOfPropertyChange(nameof(CanAddEanCode));
                }
            }
        }


        private int _measurementUnitId;

        public int MeasurementUnitId
        {
            get { return _measurementUnitId; }
            set
            {
                if (_measurementUnitId != value)
                {
                    _measurementUnitId = value;
                    NotifyOfPropertyChange(nameof(MeasurementUnitId));
                }
            }
        }

        private int _brandId;

        public int BrandId
        {
            get
            {
                return _brandId;
            }
            set
            {
                if (_brandId != value)
                {
                    _brandId = value;
                    NotifyOfPropertyChange(nameof(BrandId));
                }
            }
        }

        private int _accountingGroupId;

        public int AccountingGroupId
        {
            get
            {
                return _accountingGroupId;
            }
            set
            {
                if (_accountingGroupId != value)
                {
                    _accountingGroupId = value;
                    NotifyOfPropertyChange(nameof(AccountingGroupId));
                }
            }
        }

        private int _sizeId;

        public int SizeId
        {
            get
            {
                return _sizeId;
            }
            set
            {
                if (_sizeId != value)
                {
                    _sizeId = value;
                    NotifyOfPropertyChange(nameof(SizeId));
                }
            }
        }

        private int _selectedIndex;

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set 
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    NotifyOfPropertyChange(nameof(SelectedIndex));
                }
            }
        }

        private bool _isNewRecord;

        public bool IsNewRecord
        {
            get { return _isNewRecord; }
            set 
            {
                if (_isNewRecord != value)
                {
                    _isNewRecord = value;
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        public int SelectedSubCategoryIdBeforeNewItem { get; set; }

        public bool TreeViewEnable => !IsEditing;

        public void SetItemForNew()
        {
            Id = 0;
            Name = string.Empty;
            Code = string.Empty;
            Reference = string.Empty;
            IsActive = true;
            AllowFraction = false;
            Billable = false;
            AmountBasedOnWeight = false;
            HasExtendedInformation = false;
            AiuBasedService = false;
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemsTypes.FirstOrDefault(x => x.Id == ((ItemSubCategoryDTO)SelectedItem).ItemCategory.ItemType.Id);
            if (itemTypeDTO is null) return;
            SelectedMeasurementUnit = MeasurementUnits.FirstOrDefault(x => x.Id == itemTypeDTO.MeasurementUnitByDefault.Id) ?? throw new Exception("");
            SelectedAccountingGroup = AccountingGroups.FirstOrDefault(x => x.Id == itemTypeDTO.AccountingGroupByDefault.Id) ?? throw new Exception("");
            SelectedBrand = Brands.FirstOrDefault(x => x.Id == 0);
            SelectedSize = Sizes.FirstOrDefault(x => x.Id == 0);
            EanCodes = [];
            SelectedIndex = 0;
            if (SelectedItem is null) return;
            SelectedSubCategoryIdBeforeNewItem = ((ItemSubCategoryDTO)SelectedItem).Id;
        }

        public void SetItemForEdit(ItemDTO selectedItem)
        {
            IsNewRecord = false;
            Id = selectedItem.Id;
            Name = selectedItem.Name;
            Code = selectedItem.Code;
            Reference = selectedItem.Reference;
            IsActive = selectedItem.IsActive;
            AllowFraction = selectedItem.AllowFraction;
            Billable = selectedItem.Billable;
            AmountBasedOnWeight = selectedItem.AmountBasedOnWeight;
            HasExtendedInformation = selectedItem.HasExtendedInformation;
            AiuBasedService = selectedItem.AiuBasedService;
            UpdateComboBoxes();
            EanCodes = selectedItem.EanCodes is null ? [] : new ObservableCollection<EanCodeDTO>(selectedItem.EanCodes.Select(x => (EanCodeDTO)x.Clone()).ToList());
            SelectedIndex = 0;
            EanCode = string.Empty;
        }


        private EanCodeDTO _selectedEanCode;

        public EanCodeDTO SelectedEanCode
        {
            get { return _selectedEanCode; }
            set 
            {
                if (_selectedEanCode != value)
                {
                    _selectedEanCode = value;
                    NotifyOfPropertyChange(nameof(SelectedEanCode));
                }
            }
        }


        private ICommand _deleteEanCodeCommand;

        public ICommand DeleteEanCodeCommand
        {
            get
            {
                if (_deleteEanCodeCommand is null) _deleteEanCodeCommand = new RelayCommand(CanDeleteEanCode, DeleteEanCode);
                return _deleteEanCodeCommand;
            }
        }

        public void DeleteEanCode(object p)
        {
            try
            {
                if (ThemedMessageBox.Show("Confirme ...", $"¿Confirma que desea eliminar el código de barras: {SelectedEanCode.EanCode} ?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                if (SelectedEanCode != null)
                {
                    EanCodeDTO? eanCodeToDelete = EanCodes.FirstOrDefault(eanCode => eanCode.Id == SelectedEanCode.Id);
                    if (eanCodeToDelete is null) return;
                    EanCodes.Remove(eanCodeToDelete);
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
        }

        public bool CanDeleteEanCode(object p) => true;

        private ICommand _addEanCodeCommand;

        public ICommand AddEanCodeCommand
        {
            get
            {
                if (_addEanCodeCommand is null) _addEanCodeCommand = new RelayCommand(CanAddEanCode, AddEanCode);
                return _addEanCodeCommand;
            }
        }

        public void AddEanCode(object p)
        {
            try
            {
                EanCodeDTO eanCode = new EanCodeDTO() { EanCode = EanCode};
                EanCode = string.Empty;
                EanCodes.Add(eanCode);
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
        }

        public bool CanAddEanCode(object p) => !string.IsNullOrEmpty(EanCode);

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
                if (_isEditing != value)
                {
                    _isEditing = value;
                    NotifyOfPropertyChange(nameof(IsEditing));
                    NotifyOfPropertyChange(nameof(CanSaveItem));
                    NotifyOfPropertyChange(nameof(TreeViewEnable));
                }
            }
        }

        private bool _itemDTOIsSelected = false;

        public bool ItemDTOIsSelected
        {
            get
            {
                if (SelectedItem is ItemDTO && SelectedCatalog.Id != 0)
                {
                    return true;
                }
                return false;
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

        private ICatalogItem? _selectedItem;
        public ICatalogItem? SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(ItemDTOIsSelected));
                    if (_selectedItem != null)
                    {
                        if(_selectedItem is ItemTypeDTO itemTypeDTO)
                        {
                            SelectedMeasurementUnitByDefault = itemTypeDTO.MeasurementUnitByDefault;
                            SelectedAccountingGroupByDefault = itemTypeDTO.AccountingGroupByDefault;
                        }
                        if (_selectedItem is ItemDTO itemDTO)
                        {
                            IsEditing = false;
                            CanEditItem = true;
                            CanUndo = false;
                            SetItemForEdit(itemDTO);
                        }
                    }
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

        private MeasurementUnitDTO _selectedMeasurementUnitByDefault;

        public MeasurementUnitDTO SelectedMeasurementUnitByDefault
        {
            get { return _selectedMeasurementUnitByDefault; }
            set
            {
                if (_selectedMeasurementUnitByDefault != value)
                {
                    _selectedMeasurementUnitByDefault = value;
                    NotifyOfPropertyChange(nameof(SelectedMeasurementUnitByDefault));
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

        private AccountingGroupDTO _selectedAccountingGroupByDefault;

        public AccountingGroupDTO SelectedAccountingGroupByDefault
        {
            get { return _selectedAccountingGroupByDefault; }
            set
            {
                if (_selectedAccountingGroupByDefault != value)
                {
                    _selectedAccountingGroupByDefault = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingGroupByDefault));
                }
            }
        }

        public bool CanSaveItem => IsEditing == true && _errors.Count <= 0;

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
                    NotifyOfPropertyChange(nameof(CatalogIsSelected));
                    NotifyOfPropertyChange(nameof(DeleteCatalogButtonEnable));
                    NotifyOfPropertyChange(nameof(ItemDTOIsSelected));              
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

        private ICommand _saveItemCommand;

        public ICommand SaveItemCommand
        {
            get
            {
                if (_saveItemCommand is null) _saveItemCommand = new AsyncCommand(SaveItem, CanSaveItem);
                return _saveItemCommand;
            }
        }


        public async Task SaveItem()
        {
            try
            {
                IsBusy = true;
                Refresh();
                ItemGraphQLModel result = await ExecuteSaveItem();
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new ItemCreateMessage() { CreatedItem =  result});
                }
                else
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new ItemUpdateMessage() { UpdatedItem = result });
                    IsEditing = false;
                    CanUndo = false;
                    CanEditItem = true;
                    SelectedIndex = 0;
                }
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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "SaveItem" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<ItemGraphQLModel> ExecuteSaveItem()
        {
            try
            {
                string query;
                List<object> eanCodes = [];

                if (EanCodes != null)
                {
                    foreach (EanCodeDTO eanCode in EanCodes)
                    {
                        eanCodes.Add(new { eanCode.EanCode });
                    }
                }

                dynamic variables = new ExpandoObject();

                variables.Data = new ExpandoObject();
                if(!IsNewRecord) variables.Id = Id;
                variables.Data.Name = Name;
                variables.Data.Reference = Reference;
                variables.Data.IsActive = IsActive;
                variables.Data.AllowFraction = AllowFraction;
                variables.Data.HasExtendedInformation = HasExtendedInformation;
                variables.Data.AiuBasedService = AiuBasedService;
                variables.Data.AmountBasedOnWeight = AmountBasedOnWeight;
                variables.Data.Billable = Billable;
                variables.Data.MeasurementUnitId = SelectedMeasurementUnit.Id;
                variables.Data.ItemBrandId = SelectedBrand.Id;
                variables.Data.AccountingGroupId = SelectedAccountingGroup.Id;
                variables.Data.ItemSizeMasterId = SelectedSize.Id;
                if(IsNewRecord) variables.Data.ItemSubCategoryId = SelectedSubCategoryIdBeforeNewItem;

                if (eanCodes.Count == 0) variables.Data.EanCodes = new List<object>();
                if (eanCodes.Count > 0)
                {
                    variables.Data.EanCodes = new List<object>();
                    variables.Data.EanCodes = eanCodes;
                }

                if (IsNewRecord)
                {
                    query = @"
                    mutation ($data: CreateItemInput!) {
                      CreateResponse: createItem(data: $data) {
                        id
                        name
                        reference
                        code
                        isActive
                        allowFraction
                        hasExtendedInformation
                        aiuBasedService
                        amountBasedOnWeight
                        billable
                        accountingGroup{
                          id
                        }
                        brand{
                          id
                        }
                        measurementUnit{
                          id
                        }
                        size{
                          id
                        }
                        subCategory{
                          id
                          itemCategory{
                            id
                            itemType{
                              id
                            }
                          }
                        }
                        eanCodes{
                          id
                          eanCode
                        }
                      }
                    }";
                }
                else
                {
                    query = @"
                    mutation ($id: Int!, $data: UpdateItemInput!) {
                      UpdateResponse: updateItem(id: $id, data: $data) {
                        id
                        name
                        reference
                        code
                        isActive
                        allowFraction
                        hasExtendedInformation
                        aiuBasedService
                        amountBasedOnWeight
                        billable
                        accountingGroup{
                          id
                        }
                        brand{
                          id
                        }
                        measurementUnit{
                          id
                        }
                        size{
                          id
                        }
                        subCategory{
                          id
                          itemCategory{
                            id
                            itemType{
                              id
                            }
                          }
                        }
                        eanCodes{
                          id
                          eanCode
                        }
                      }
                    }";
                }
                var result = IsNewRecord ? await ItemService.Create(query, variables) : await ItemService.Update(query, variables);
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }

        private ICommand _deleteItemCommand;

        public ICommand DeleteItemCommand
        {
            get
            {
                if (_deleteItemCommand is null) _deleteItemCommand = new AsyncCommand(DeleteItem, CanDeleteCatalog);
                return _deleteItemCommand;
            }
        }
        public async Task DeleteItem()
        {
            try
            {
                IsBusy = true;
                int id = ((ItemDTO)SelectedItem).Id;

                string query = @"query($id:Int!){
                  CanDeleteModel: canDeleteItem(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await this.ItemService.CanDelete(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {((ItemDTO)SelectedItem).Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }

                Refresh();

                ItemGraphQLModel deletedItem = await ExecuteDeleteItem(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new ItemDeleteMessage() { DeletedItem = deletedItem });

                NotifyOfPropertyChange(nameof(CanDeleteItem));
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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteItemSubCategory" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
        public bool CanDeleteItem => true;

        public async Task<ItemGraphQLModel> ExecuteDeleteItem(int id)
        {
            try
            {
                string query = @"
                mutation ($id: Int!) {
                  DeleteResponse: deleteItem(id: $id) {
                    id
                    name
                    reference
                    code
                    isActive
                    allowFraction
                    hasExtendedInformation
                    aiuBasedService
                    amountBasedOnWeight
                    billable
                    accountingGroup{
                      id
                    }
                    brand{
                      id
                    }
                    measurementUnit{
                      id
                    }
                    size{
                      id
                    }
                    subCategory{
                      id
                      itemCategory{
                        id
                        itemType{
                          id
                            }
                        }
                    }
                    eanCodes{
                      id
                    }
                  }
                }";

                object variables = new { Id = id };
                ItemGraphQLModel deletedItem = await ItemService.Delete(query, variables);
                this.SelectedItem = null;
                return deletedItem;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private ICommand _createItemCommand;

        public ICommand CreateItemCommand
        {
            get
            {
                if (_createItemCommand is null) _createItemCommand = new AsyncCommand(CreateItem, CanCreateItem);
                return _createItemCommand;
            }
        }

        public async Task CreateItem()
        {
            SetItemForNew();
            SelectedItem = new ItemDTO();
            IsEditing = true;
            CanUndo = true;
            CanEditItem = false;
            IsNewRecord = true;
        }

        public bool CanCreateItem => true;

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


        public bool CatalogIsSelected => SelectedCatalog != null && SelectedCatalog.Id != 0;

        public bool DeleteCatalogButtonEnable => SelectedCatalog != null && SelectedCatalog.Id != 0 && SelectedCatalog.ItemsTypes.Count == 0;

        public async Task EditItem()
        {
            IsEditing = true;
            CanUndo = true;
            CanEditItem = false;
            IsNewRecord = false;
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
            if (IsNewRecord)
            {
                SelectedItem = null;
                IsEditing = false;
                CanUndo = false;
                CanEditItem = true;
                return;
            }
            SetItemForEdit((ItemDTO)SelectedItem);
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

        private ICommand _deleteCatalogCommand;

        public ICommand DeleteCatalogCommand
        {
            get
            {
                if (_deleteCatalogCommand is null) _deleteCatalogCommand = new AsyncCommand(DeleteCatalog, CanDeleteCatalog);
                return _deleteCatalogCommand;
            }
        }
        public async Task DeleteCatalog()
        {
            try
            {
                IsBusy = true;
                int id = SelectedCatalog.Id;

                string query = @"query($id:Int!){
                  CanDeleteModel: canDeleteCatalog(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await this.CatalogService.CanDelete(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {SelectedCatalog.Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }

                Refresh();

                CatalogGraphQLModel deletedCatalog = await ExecuteDeleteCatalog(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new CatalogDeleteMessage() { DeletedCatalog = deletedCatalog });

                NotifyOfPropertyChange(nameof(CanDeleteCatalog));
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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteItemSubCategory" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
        public bool CanDeleteCatalog => true;

        public async Task<CatalogGraphQLModel> ExecuteDeleteCatalog(int id)
        {
            try
            {
                string query = @"
                mutation ($id: Int!) {
                  DeleteResponse: deleteCatalog(id: $id) {
                    id
                    name
                  }
                }";

                object variables = new { Id = id };
                CatalogGraphQLModel deletedCatalog = await CatalogService.Delete(query, variables);
                this.SelectedItem = null;
                return deletedCatalog;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private ICommand _updateCatalogCommand;

        public ICommand UpdateCatalogCommand
        {
            get
            {
                if (_updateCatalogCommand is null) _updateCatalogCommand = new AsyncCommand(UpdateCatatalog, CanUpdateCatalog);
                return _updateCatalogCommand;
            }
        }

        public async Task UpdateCatatalog()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteUpdateCatalog());

            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "UpdateItemType" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ExecuteUpdateCatalog()
        {
            await Context.ActivateCatalogDetailForEdit(SelectedCatalog);
        }

        public bool CanUpdateCatalog => true;

        private ICommand _createCatalogCommand;

        public ICommand CreateCatalogCommand
        {
            get
            {
                if (_createCatalogCommand is null) _createCatalogCommand = new AsyncCommand(CreateCatalog, CanCreateCatalog);
                return _createCatalogCommand;
            }
        }

        public async Task CreateCatalog()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await ExecuteCreateCatalog();
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "CreateCategory" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ExecuteCreateCatalog()
        {
            try
            {
                await Context.ActivateCatalogDetailForNew();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool CanCreateCatalog => true;


        private ICommand _updateItemSubCategoryCommand;

        public ICommand UpdateItemSubCategoryCommand
        {
            get
            {
                if (_updateItemSubCategoryCommand is null) _updateItemSubCategoryCommand = new AsyncCommand(UpdateItemSubCategory, CanUpdateItemSubCategory);
                return _updateItemSubCategoryCommand;
            }
        }

        public async Task UpdateItemSubCategory()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteUpdateItemSubCategory());
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "UpdateItemType" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ExecuteUpdateItemSubCategory()
        {
            await Context.ActivateItemSubCategoryDetailForEdit((ItemSubCategoryDTO)SelectedItem);
        }

        public bool CanUpdateItemSubCategory => true;

        private ICommand _createItemSubCategoryCommand;

        public ICommand CreateItemSubCategoryCommand
        {
            get
            {
                if (_createItemSubCategoryCommand is null) _createItemSubCategoryCommand = new AsyncCommand(CreateItemSubCategory, CanCreateItemSubCategory);
                return _createItemSubCategoryCommand;
            }
        }

        public async Task CreateItemSubCategory()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await ExecuteCreateItemSubCategory();
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "CreateCategory" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ExecuteCreateItemSubCategory()
        {
            try
            {
                await Context.ActivateItemSubCategoryDetailForNew(((ItemCategoryDTO)SelectedItem).Id);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool CanCreateItemSubCategory => true;


        private ICommand _createItemTypeCommand;

        public ICommand CreateItemTypeCommand
        {
            get
            {
                if (_createItemTypeCommand is null) _createItemTypeCommand = new AsyncCommand(CreateItemType, CanCreateItemType);
                return _createItemTypeCommand;
            }
        }

        private ICommand _createItemCategoryCommand;

        public ICommand CreateItemCategoryCommand
        {
            get
            {
                if (_createItemCategoryCommand is null) _createItemCategoryCommand = new AsyncCommand(CreateItemCategory, CanCreateItemCategory);
                return _createItemCategoryCommand;
            }
        }

        public async Task CreateItemCategory()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await ExecuteCreateItemCategory();
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "CreateCategory" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ExecuteCreateItemCategory()
        {
            try
            {
                await Context.ActivateItemCategoryDetailForNew(((ItemTypeDTO)SelectedItem).Id);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool CanCreateItemCategory => true;

        private ICommand _updateItemCategoryCommand;

        public ICommand UpdateItemCategoryCommand
        {
            get
            {
                if (_updateItemCategoryCommand is null) _updateItemCategoryCommand = new AsyncCommand(UpdateItemCategory, CanUpdateItemCategory);
                return _updateItemCategoryCommand;
            }
        }

        public async Task UpdateItemCategory()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteUpdateItemCategory());
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "UpdateItemType" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ExecuteUpdateItemCategory()
        {
            try
            {
                await Context.ActivateItemCategoryDetailForEdit((ItemCategoryDTO)SelectedItem);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool CanUpdateItemCategory => true;

        public async Task CreateItemType()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await ExecuteCreateItemType();
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "CreateItemType" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }


        public async Task ExecuteCreateItemType()
        {
            try
            {
                await Context.ActivateItemTypeDetailForNew(SelectedCatalog.Id, MeasurementUnits, AccountingGroups);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool CanCreateItemType => true;

        private ICommand _updateItemTypeCommand;

        public ICommand UpdateItemTypeCommand
        {
            get
            {
                if (_updateItemTypeCommand is null) _updateItemTypeCommand = new AsyncCommand(UpdateItemType, CanUpdateItemType);
                return _updateItemTypeCommand;
            }
        }

        public async Task UpdateItemType()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteUpdateItemType());
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "UpdateItemType" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ExecuteUpdateItemType()
        {
            await Context.ActivateItemTypeDetailForEdit((ItemTypeDTO)SelectedItem, MeasurementUnits, AccountingGroups);
        }

        public bool CanUpdateItemType => true;


        private ICommand _deleteItemTypeCommand;

        public ICommand DeleteItemTypeCommand
        {
            get
            {
                if (_deleteItemTypeCommand is null) _deleteItemTypeCommand = new AsyncCommand(DeleteItemType, CanDeleteItemType);
                return _deleteItemTypeCommand;
            }
        }

        private ICommand _deleteItemSubCategoryCommand;

        public ICommand DeleteItemSubCategoryCommand
        {
            get
            {
                if (_deleteItemSubCategoryCommand is null) _deleteItemSubCategoryCommand = new AsyncCommand(DeleteItemSubCategory, CanDeleteItemSubCategory);
                return _deleteItemSubCategoryCommand;
            }
        }
        public async Task DeleteItemSubCategory()
        {
            try
            {
                IsBusy = true;
                int id = ((ItemSubCategoryDTO)SelectedItem).Id;

                string query = @"query($id:Int!){
                  CanDeleteModel: canDeleteItemSubCategory(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await this.ItemSubCategoryService.CanDelete(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {((ItemSubCategoryDTO)SelectedItem).Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }

                Refresh();

                ItemSubCategoryGraphQLModel deletedItemSubCategory = await ExecuteDeleteItemSubCategory(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new ItemSubCategoryDeleteMessage() { DeletedItemSubCategory = deletedItemSubCategory });

                NotifyOfPropertyChange(nameof(CanDeleteItemSubCategory));
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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteItemSubCategory" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
        public bool CanDeleteItemSubCategory => true;

        public async Task<ItemSubCategoryGraphQLModel> ExecuteDeleteItemSubCategory(int id)
        {
            try
            {
                string query = @"
                mutation ($id: Int!) {
                  DeleteResponse: deleteItemSubCategory(id: $id) {
                    id
                    name
                    itemCategory{
                        id
                        itemType{
                            id
                        }
                    }
                  }
                }";

                object variables = new { Id = id };
                ItemSubCategoryGraphQLModel deletedItemSubCategory = await ItemSubCategoryService.Delete(query, variables);
                this.SelectedItem = null;
                return deletedItemSubCategory;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task DeleteItemType()
        {
            try
            {
                IsBusy = true;
                int id = ((ItemTypeDTO)SelectedItem).Id;

                string query = @"query($id:Int!){
                  CanDeleteModel: canDeleteItemType(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await this.ItemTypeService.CanDelete(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {((ItemTypeDTO)SelectedItem).Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }

                Refresh();

                ItemTypeGraphQLModel deletedItemType = await ExecuteDeleteItemType(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new ItemTypeDeleteMessage() { DeletedItemType = deletedItemType });

                NotifyOfPropertyChange(nameof(CanDeleteItemType));
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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteItemType" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
        public bool CanDeleteItemType => true;

        public async Task<ItemTypeGraphQLModel> ExecuteDeleteItemType(int id)
        {
            try
            {
                string query = @"
                mutation ($id: Int!) {
                  DeleteResponse: deleteItemType(id: $id) {
                    id
                    name
                    prefixChar
                    stockControl
                  }
                }";

                object variables = new { Id = id };
                ItemTypeGraphQLModel deletedItemType = await ItemTypeService.Delete(query, variables);
                this.SelectedItem = null;
                return deletedItemType;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private ICommand _deleteItemCategoryCommand;

        public ICommand DeleteItemCategoryCommand
        {
            get
            {
                if (_deleteItemCategoryCommand is null) _deleteItemCategoryCommand = new AsyncCommand(DeleteItemCategory, CanDeleteItemCategory);
                return _deleteItemCategoryCommand;
            }
        }
        public async Task DeleteItemCategory()
        {
            try
            {
                IsBusy = true;
                int id = ((ItemCategoryDTO)SelectedItem).Id;

                string query = @"query($id:Int!){
                  CanDeleteModel: canDeleteItemCategory(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await this.ItemCategoryService.CanDelete(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {((ItemCategoryDTO)SelectedItem).Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }

                Refresh();

                ItemCategoryGraphQLModel deletedItemCategory = await ExecuteDeleteItemCategory(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new ItemCategoryDeleteMessage() { DeletedItemCategory = deletedItemCategory });

                NotifyOfPropertyChange(nameof(CanDeleteItemCategory));
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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteItemType" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
        public bool CanDeleteItemCategory => true;

        public async Task<ItemCategoryGraphQLModel> ExecuteDeleteItemCategory(int id)
        {
            try
            {
                string query = @"
                mutation ($id: Int!) {
                  DeleteResponse: deleteItemCategory(id: $id) {
                    id
                    name
                    itemType{
                        id
                    }
                  }
                }";

                object variables = new { Id = id };
                ItemCategoryGraphQLModel deletedItemCategory = await ItemCategoryService.Delete(query, variables);
                this.SelectedItem = null;
                return deletedItemCategory;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public CatalogMasterViewModel(CatalogViewModel context)
        {
            Context = context;
            _errors = new Dictionary<string, List<string>>();
            Context.EventAggregator.SubscribeOnUIThread(this);
            _ = Task.Run(() => LoadCatalogs());
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
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
                        measurementUnitByDefault{
                            id 
                            }
                        accountingGroupByDefault{
                            id
                            }   
                        catalog{
                            id
                            }
                        }
                    }
                }";

                dynamic variables = new ExpandoObject();

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
                        itemType{
                            id
                        }
                      }
                    }";
                dynamic variables = new ExpandoObject();
                variables.ids = ids;

                var source = await ItemCategoryService.GetList(query, variables);
                ItemsCategories = Context.AutoMapper.Map<ObservableCollection<ItemCategoryDTO>>(source);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (ItemCategoryDTO itemCategory in ItemsCategories)
                    {
                        itemCategory.Context = this;
                        itemCategory.SubCategories.Add(new ItemSubCategoryDTO() { IsDummyChild = true, Items = [], Name = "Fucking Dummy" });
                        itemType.ItemsCategories.Add(itemCategory);
                    }
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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadItemsCategories" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task LoadItemsSubCategories(ItemCategoryDTO itemCategory)
        {
            try
            {
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
                        itemCategory{
                            id
                            itemType{
                                id
                                measurementUnitByDefault{
                                    id
                                    name
                                }
                                accountingGroupByDefault{
                                    id
                                    name
                                }
                            }
                        }
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
                    itemCategory.SubCategories.Add(itemSubCategory);
                }
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
                        subCategory{
                            id
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
                    itemSubCategory.Items.Add(item);
                }
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
            //ItemDTOIsSelected = ((ItemDTO)SelectedItem).IsSelected;
            if (((ItemDTO)SelectedItem).Brand is null)
            {
                SelectedBrand = Brands.FirstOrDefault(x => x.Id == 0);
                BrandId = 0;
            }
            else
            {
                SelectedBrand = Brands.FirstOrDefault(x => x.Id == ((ItemDTO)SelectedItem).Brand.Id);
                BrandId = ((ItemDTO)SelectedItem).Brand.Id;
            };
            if (((ItemDTO)SelectedItem).Size is null)
            {
                SelectedSize = Sizes.FirstOrDefault(x => x.Id == 0);
                SizeId = 0;
            }
            else
            {
                SelectedSize = Sizes.FirstOrDefault(x => x.Id == ((ItemDTO)SelectedItem).Size.Id);
                SizeId = ((ItemDTO)SelectedItem).Size.Id;
            };
            if (((ItemDTO)SelectedItem).MeasurementUnit != null) 
            {
                SelectedMeasurementUnit = MeasurementUnits.FirstOrDefault(x => x.Id == ((ItemDTO)SelectedItem).MeasurementUnit.Id);
                MeasurementUnitId = ((ItemDTO)SelectedItem).MeasurementUnit.Id;
            }
            if (((ItemDTO)SelectedItem).AccountingGroup != null) 
            {
                SelectedAccountingGroup = AccountingGroups.FirstOrDefault(x => x.Id == ((ItemDTO)SelectedItem).AccountingGroup.Id);
                AccountingGroupId = ((ItemDTO)SelectedItem).AccountingGroup.Id;
            }
        }

        Dictionary<string, List<string>> _errors;

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null;
            return _errors[propertyName];
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ValidateProperty(string propertyName, string value)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty.Trim();
            try
            {
                ClearErrors(propertyName);
                switch (propertyName)
                {
                    case nameof(Name):
                        if (string.IsNullOrEmpty(Name)) AddError(propertyName, "El nombre del item no puede estar vacío");
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        public Task HandleAsync(ItemTypeCreateMessage message, CancellationToken cancellationToken)
        {
            ItemTypeDTO itemTypeDTO = Context.AutoMapper.Map<ItemTypeDTO>(message.CreatedItemType);
            itemTypeDTO.ItemsCategories.Add(new ItemCategoryDTO() { IsDummyChild = true, Name = "Fucking Dummy", SubCategories = [] });
            if (SelectedCatalog.Id != itemTypeDTO.Catalog.Id) return Task.CompletedTask;
            SelectedCatalog.ItemsTypes.Add(itemTypeDTO);
            SelectedItem = itemTypeDTO;
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemTypeDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemsTypes.Where(x => x.Id == message.DeletedItemType.Id).FirstOrDefault();
                if (itemTypeDTO is null) return;
                SelectedCatalog.ItemsTypes.Remove(itemTypeDTO);
                SelectedItem = null;
            });
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemTypeUpdateMessage message, CancellationToken cancellationToken)
        {
            ItemTypeDTO itemTypeDTO = Context.AutoMapper.Map<ItemTypeDTO>(message.UpdatedItemType);
            ItemTypeDTO? itemToUpdate = SelectedCatalog.ItemsTypes.FirstOrDefault(x => x.Id == message.UpdatedItemType.Id);
            if (itemToUpdate == null) return Task.CompletedTask;
            itemToUpdate.Id = itemTypeDTO.Id;
            itemToUpdate.Name = itemTypeDTO.Name;
            itemToUpdate.PrefixChar = itemTypeDTO.PrefixChar;
            itemToUpdate.StockControl = itemTypeDTO.StockControl;
            itemToUpdate.MeasurementUnitByDefault = itemTypeDTO.MeasurementUnitByDefault;
            itemToUpdate.AccountingGroupByDefault = itemTypeDTO.AccountingGroupByDefault;
            return Task.CompletedTask;
        }

        public async Task HandleAsync(ItemCategoryCreateMessage message, CancellationToken cancellationToken)
        {
            ItemCategoryDTO itemCategoryDTO = Context.AutoMapper.Map<ItemCategoryDTO>(message.CreatedItemCategory);
            itemCategoryDTO.SubCategories.Add(new ItemSubCategoryDTO() { IsDummyChild = true, Name = "Fucking Dummy", Items = [] });
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemsTypes.FirstOrDefault(x => x.Id == itemCategoryDTO.ItemType.Id);
            if (itemTypeDTO is null) return;
            //Si el nodo no está expandido y tiene un dummy child
            if (itemTypeDTO.IsExpanded == false && itemTypeDTO.ItemsCategories[0].IsDummyChild)
            {
                await LoadItemsCategories(itemTypeDTO);
                itemTypeDTO.IsExpanded = true;
                ItemCategoryDTO? itemCategory = itemTypeDTO.ItemsCategories.FirstOrDefault(x => x.Id == itemCategoryDTO.Id);
                if (itemCategory is null) return;
                SelectedItem = itemCategory;
                return;
            }
            //si el nodo no está expandido, pero ya fueron cargados sus hijos
            if (itemTypeDTO.IsExpanded == false)
            {
                itemTypeDTO.IsExpanded = true;
                itemTypeDTO.ItemsCategories.Add(itemCategoryDTO);
                SelectedItem = itemCategoryDTO;
                return;
            }
            //si el nodo está expandido
            itemTypeDTO.ItemsCategories.Add(itemCategoryDTO);
            SelectedItem = itemCategoryDTO;
            return;
        }

        public Task HandleAsync(ItemCategoryDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                //encontrar el itemType
                ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemsTypes.Where(x => x.Id == message.DeletedItemCategory.ItemType.Id).FirstOrDefault();
                if (itemTypeDTO is null) return;
                //eliminar la categoria dentro de la lista de categorias del itemtype encontrado 
                itemTypeDTO.ItemsCategories.Remove(itemTypeDTO.ItemsCategories.Where(x => x.Id == message.DeletedItemCategory.Id).First());
                SelectedItem = null;
            });
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemCategoryUpdateMessage message, CancellationToken cancellationToken)
        {
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemsTypes.Where(x => x.Id == message.UpdatedItemCategory.ItemType.Id).FirstOrDefault();
            if (itemTypeDTO is null) return Task.CompletedTask;
            ItemCategoryDTO? itemCategoryDTOToUpdate = itemTypeDTO.ItemsCategories.Where(x => x.Id == message.UpdatedItemCategory.Id).FirstOrDefault();
            if (itemCategoryDTOToUpdate is null) return Task.CompletedTask;
            itemCategoryDTOToUpdate.Id = message.UpdatedItemCategory.Id;
            itemCategoryDTOToUpdate.Name = message.UpdatedItemCategory.Name;
            return Task.CompletedTask;
        }

        public async Task HandleAsync(ItemSubCategoryCreateMessage message, CancellationToken cancellationToken)
        {
            ItemSubCategoryDTO itemSubCategoryDTO = Context.AutoMapper.Map<ItemSubCategoryDTO>(message.CreatedItemSubCategory);
            itemSubCategoryDTO.Items.Add(new ItemDTO() { IsDummyChild = true, Name = "Fucking Dummy" });
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemsTypes.FirstOrDefault(x => x.Id == itemSubCategoryDTO.ItemCategory.ItemType.Id);
            if (itemTypeDTO is null) return;
            ItemCategoryDTO? itemCategoryDTO = itemTypeDTO.ItemsCategories.FirstOrDefault(x => x.Id == itemSubCategoryDTO.ItemCategory.Id);
            if (itemCategoryDTO is null) return;
            if (!itemCategoryDTO.IsExpanded && itemCategoryDTO.SubCategories[0].IsDummyChild)
            {
                await LoadItemsSubCategories(itemCategoryDTO);
                itemCategoryDTO.IsExpanded = true;
                ItemSubCategoryDTO? itemSubCategory = itemCategoryDTO.SubCategories.FirstOrDefault(x => x.Id == itemSubCategoryDTO.Id);
                if (itemSubCategory is null) return;
                SelectedItem = itemSubCategory;
                return;
            }
            if (!itemCategoryDTO.IsExpanded)
            {
                itemCategoryDTO.IsExpanded = true;
                itemCategoryDTO.SubCategories.Add(itemSubCategoryDTO);
                SelectedItem = itemSubCategoryDTO;
                return;
            }
            itemCategoryDTO.SubCategories.Add(itemSubCategoryDTO);
            SelectedItem = itemSubCategoryDTO;
            return;
        }

        public Task HandleAsync(ItemSubCategoryDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                //encontrar el itemType
                ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemsTypes.Where(x => x.Id == message.DeletedItemSubCategory.ItemCategory.ItemType.Id).FirstOrDefault();
                if (itemTypeDTO is null) return;
                //eliminar la categoria dentro de la lista de categorias del itemtype encontrado 
                ItemCategoryDTO? itemCategoryDTO =  itemTypeDTO.ItemsCategories.FirstOrDefault(x => x.Id == message.DeletedItemSubCategory.ItemCategory.Id);
                if (itemCategoryDTO is null) return;
                itemCategoryDTO.SubCategories.Remove(itemCategoryDTO.SubCategories.Where(x => x.Id == message.DeletedItemSubCategory.Id).First());
                SelectedItem = null;
            });
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemSubCategoryUpdateMessage message, CancellationToken cancellationToken)
        {
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemsTypes.Where(x => x.Id == message.UpdatedItemSubCategory.ItemCategory.ItemType.Id).FirstOrDefault();
            if (itemTypeDTO is null) return Task.CompletedTask;
            ItemCategoryDTO? itemCategoryDTO = itemTypeDTO.ItemsCategories.FirstOrDefault(x => x.Id == message.UpdatedItemSubCategory.ItemCategory.Id);
            if (itemCategoryDTO is null) return Task.CompletedTask;
            ItemSubCategoryDTO? itemSubCategoryDTOToUpdate = itemCategoryDTO.SubCategories.FirstOrDefault(x => x.Id == message.UpdatedItemSubCategory.Id);
            if (itemSubCategoryDTOToUpdate is null) return Task.CompletedTask;
            itemSubCategoryDTOToUpdate.Id = message.UpdatedItemSubCategory.Id;
            itemSubCategoryDTOToUpdate.Name = message.UpdatedItemSubCategory.Name;
            return Task.CompletedTask;
        }

        public Task HandleAsync(CatalogCreateMessage message, CancellationToken cancellationToken)
        {
            Catalogs.Add(Context.AutoMapper.Map<CatalogDTO>(message.CreatedCatalog));
            SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == message.CreatedCatalog.Id);
            return Task.CompletedTask;
        }

        public Task HandleAsync(CatalogUpdateMessage message, CancellationToken cancellationToken)
        {
            CatalogDTO catalogDTO = Context.AutoMapper.Map<CatalogDTO>(message.UpdatedCatalog);
            CatalogDTO? catalogToUpdate = Catalogs.FirstOrDefault(x => x.Id == catalogDTO.Id);
            if (catalogToUpdate is null) return Task.CompletedTask;
            catalogToUpdate.Id = catalogDTO.Id;
            catalogToUpdate.Name = catalogDTO.Name;
            return Task.CompletedTask;
        }

        public Task HandleAsync(CatalogDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Catalogs.Remove(Catalogs.Where(x => x.Id == message.DeletedCatalog.Id).First());
                SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == 0);
            });
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                //encontrar el itemType
                ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemsTypes.Where(x => x.Id == message.DeletedItem.SubCategory.ItemCategory.ItemType.Id).FirstOrDefault();
                if (itemTypeDTO is null) return;
                ItemCategoryDTO? itemCategoryDTO = itemTypeDTO.ItemsCategories.FirstOrDefault(x => x.Id == message.DeletedItem.SubCategory.ItemCategory.Id);
                if (itemCategoryDTO is null) return;
                ItemSubCategoryDTO? itemSubCategoryDTO = itemCategoryDTO.SubCategories.FirstOrDefault(x => x.Id == message.DeletedItem.SubCategory.Id);
                if (itemSubCategoryDTO is null) return;
                itemSubCategoryDTO.Items.Remove(itemSubCategoryDTO.Items.Where(x => x.Id == message.DeletedItem.Id).First());
                SelectedItem = null;
            });
            return Task.CompletedTask;
        }

        public async Task HandleAsync(ItemCreateMessage message, CancellationToken cancellationToken)
        {
            ItemDTO itemDTO = Context.AutoMapper.Map<ItemDTO>(message.CreatedItem);
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemsTypes.FirstOrDefault(x => x.Id == itemDTO.SubCategory.ItemCategory.ItemType.Id);
            if (itemTypeDTO is null) return;
            ItemCategoryDTO? itemCategoryDTO = itemTypeDTO.ItemsCategories.FirstOrDefault(x => x.Id == itemDTO.SubCategory.ItemCategory.Id);
            if (itemCategoryDTO is null) return;
            ItemSubCategoryDTO? itemSubCategoryDTO = itemCategoryDTO.SubCategories.FirstOrDefault(x => x.Id == itemDTO.SubCategory.Id);
            if (itemSubCategoryDTO is null) return;
            if (!itemSubCategoryDTO.IsExpanded && itemSubCategoryDTO.Items[0].IsDummyChild)
            {
                await LoadItems(itemSubCategoryDTO);
                itemSubCategoryDTO.IsExpanded = true;
                ItemDTO? item = itemSubCategoryDTO.Items.FirstOrDefault(x => x.Id == itemDTO.Id);
                if (item is null) return;
                SelectedItem = item;
                return;
            }
            if (!itemSubCategoryDTO.IsExpanded)
            {
                itemSubCategoryDTO.IsExpanded = true;
                itemSubCategoryDTO.Items.Add(itemDTO);
                SelectedItem = itemDTO;
                return;
            }
            itemSubCategoryDTO.Items.Add(itemDTO);
            SelectedItem = itemDTO;
            return;
        }

        public Task HandleAsync(ItemUpdateMessage message, CancellationToken cancellationToken)
        {
            ItemDTO item = Context.AutoMapper.Map<ItemDTO>(message.UpdatedItem);
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemsTypes.Where(x => x.Id == message.UpdatedItem.SubCategory.ItemCategory.ItemType.Id).FirstOrDefault();
            if (itemTypeDTO is null) return Task.CompletedTask;
            ItemCategoryDTO? itemCategoryDTO = itemTypeDTO.ItemsCategories.FirstOrDefault(x => x.Id == message.UpdatedItem.SubCategory.ItemCategory.Id);
            if (itemCategoryDTO is null) return Task.CompletedTask;
            ItemSubCategoryDTO? itemSubCategoryDTO = itemCategoryDTO.SubCategories.FirstOrDefault(x => x.Id == message.UpdatedItem.SubCategory.Id);
            if (itemSubCategoryDTO is null) return Task.CompletedTask;
            ItemDTO? itemDTOToUpdate = itemSubCategoryDTO.Items.FirstOrDefault(x => x.Id == message.UpdatedItem.Id);
            if (itemDTOToUpdate is null) return Task.CompletedTask;
            itemDTOToUpdate.Id = item.Id;
            itemDTOToUpdate.Name = item.Name;
            itemDTOToUpdate.Reference = item.Reference;
            itemDTOToUpdate.IsActive = item.IsActive;
            itemDTOToUpdate.AllowFraction = item.AllowFraction;
            itemDTOToUpdate.HasExtendedInformation = item.HasExtendedInformation;
            itemDTOToUpdate.MeasurementUnit = item.MeasurementUnit;
            itemDTOToUpdate.Brand = item.Brand;
            itemDTOToUpdate.AccountingGroup = item.AccountingGroup;
            itemDTOToUpdate.Size = item.Size;
            itemDTOToUpdate.EanCodes = new ObservableCollection<EanCodeDTO>(item.EanCodes);
            return Task.CompletedTask;
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
