using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using Dictionaries;
using Force.DeepCloner;
using Microsoft.Win32;
using Models.Books;
using Models.Inventory;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Inventory.CatalogItems.DTO;
using NetErp.Inventory.CatalogItems.ViewModels;
using NetErp.Inventory.ItemSizes.DTO;
using NetErp.Inventory.MeasurementUnits.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Inventory.CatalogItems.PanelEditors
{
    public class ItemPanelEditor : CatalogItemsBasePanelEditor<ItemDTO, ItemGraphQLModel>
    {
        #region Fields

        private readonly IRepository<ItemGraphQLModel> _itemService;
        private readonly Helpers.IDialogService _dialogService;

        #endregion

        #region Constructor

        public ItemPanelEditor(
            CatalogRootMasterViewModel masterContext,
            IRepository<ItemGraphQLModel> itemService,
            Helpers.IDialogService dialogService)
            : base(masterContext)
        {
            _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            Messenger.Default.Register<ReturnedItemFromModalViewMessage>(this, MessageToken.Component, false, OnFindComponentMessage);
        }

        #endregion

        #region Core Properties

        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    NotifyOfPropertyChange(nameof(Id));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    NotifyOfPropertyChange(nameof(Name));
                    this.TrackChange(nameof(Name));
                    ValidateName();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _code = string.Empty;
        public string Code
        {
            get => _code;
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
            get => _reference;
            set
            {
                if (_reference != value)
                {
                    _reference = value;
                    NotifyOfPropertyChange(nameof(Reference));
                    this.TrackChange(nameof(Reference));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    NotifyOfPropertyChange(nameof(IsActive));
                    this.TrackChange(nameof(IsActive));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _allowFraction;
        public bool AllowFraction
        {
            get => _allowFraction;
            set
            {
                if (_allowFraction != value)
                {
                    _allowFraction = value;
                    NotifyOfPropertyChange(nameof(AllowFraction));
                    this.TrackChange(nameof(AllowFraction));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _billable;
        public bool Billable
        {
            get => _billable;
            set
            {
                if (_billable != value)
                {
                    _billable = value;
                    NotifyOfPropertyChange(nameof(Billable));
                    this.TrackChange(nameof(Billable));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _amountBasedOnWeight;
        public bool AmountBasedOnWeight
        {
            get => _amountBasedOnWeight;
            set
            {
                if (_amountBasedOnWeight != value)
                {
                    _amountBasedOnWeight = value;
                    NotifyOfPropertyChange(nameof(AmountBasedOnWeight));
                    this.TrackChange(nameof(AmountBasedOnWeight));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _hasExtendedInformation;
        public bool HasExtendedInformation
        {
            get => _hasExtendedInformation;
            set
            {
                if (_hasExtendedInformation != value)
                {
                    _hasExtendedInformation = value;
                    NotifyOfPropertyChange(nameof(HasExtendedInformation));
                    this.TrackChange(nameof(HasExtendedInformation));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _aiuBasedService;
        public bool AiuBasedService
        {
            get => _aiuBasedService;
            set
            {
                if (_aiuBasedService != value)
                {
                    _aiuBasedService = value;
                    NotifyOfPropertyChange(nameof(AiuBasedService));
                    this.TrackChange(nameof(AiuBasedService));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        #endregion

        #region ComboBox Selection Properties

        private int _measurementUnitId;
        public int MeasurementUnitId
        {
            get => _measurementUnitId;
            set
            {
                if (_measurementUnitId != value)
                {
                    _measurementUnitId = value;
                    NotifyOfPropertyChange(nameof(MeasurementUnitId));
                    this.TrackChange(nameof(MeasurementUnitId));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private int _brandId;
        public int BrandId
        {
            get => _brandId;
            set
            {
                if (_brandId != value)
                {
                    _brandId = value;
                    NotifyOfPropertyChange(nameof(BrandId));
                    this.TrackChange(nameof(BrandId));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private int _accountingGroupId;
        public int AccountingGroupId
        {
            get => _accountingGroupId;
            set
            {
                if (_accountingGroupId != value)
                {
                    _accountingGroupId = value;
                    NotifyOfPropertyChange(nameof(AccountingGroupId));
                    this.TrackChange(nameof(AccountingGroupId));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private int _sizeId;
        public int SizeId
        {
            get => _sizeId;
            set
            {
                if (_sizeId != value)
                {
                    _sizeId = value;
                    NotifyOfPropertyChange(nameof(SizeId));
                    this.TrackChange(nameof(SizeId));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private MeasurementUnitDTO _selectedMeasurementUnit;
        public MeasurementUnitDTO SelectedMeasurementUnit
        {
            get => _selectedMeasurementUnit;
            set
            {
                if (_selectedMeasurementUnit != value)
                {
                    _selectedMeasurementUnit = value;
                    NotifyOfPropertyChange(nameof(SelectedMeasurementUnit));
                    if (value != null) MeasurementUnitId = value.Id;
                }
            }
        }

        private ItemBrandDTO _selectedBrand;
        public ItemBrandDTO SelectedBrand
        {
            get => _selectedBrand;
            set
            {
                if (_selectedBrand != value)
                {
                    _selectedBrand = value;
                    NotifyOfPropertyChange(nameof(SelectedBrand));
                    if (value != null) BrandId = value.Id;
                }
            }
        }

        private AccountingGroupDTO _selectedAccountingGroup;
        public AccountingGroupDTO SelectedAccountingGroup
        {
            get => _selectedAccountingGroup;
            set
            {
                if (_selectedAccountingGroup != value)
                {
                    _selectedAccountingGroup = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingGroup));
                    if (value != null) AccountingGroupId = value.Id;
                }
            }
        }

        private ItemSizeCategoryDTO _selectedSize;
        public ItemSizeCategoryDTO SelectedSize
        {
            get => _selectedSize;
            set
            {
                if (_selectedSize != value)
                {
                    _selectedSize = value;
                    NotifyOfPropertyChange(nameof(SelectedSize));
                    if (value != null) SizeId = value.Id;
                }
            }
        }

        public ObservableCollection<MeasurementUnitDTO> MeasurementUnits => MasterContext.MeasurementUnits;
        public ObservableCollection<ItemBrandDTO> ItemBrands => MasterContext.ItemBrands;
        public ObservableCollection<AccountingGroupDTO> AccountingGroups => MasterContext.AccountingGroups;
        public ObservableCollection<ItemSizeCategoryDTO> Sizes => MasterContext.ItemSizeCategories;

        #endregion

        #region Collections

        private ObservableCollection<string> _eanCodes = [];
        public ObservableCollection<string> EanCodes
        {
            get => _eanCodes;
            set
            {
                if (_eanCodes != value)
                {
                    _eanCodes = value;
                    NotifyOfPropertyChange(nameof(EanCodes));
                }
            }
        }

        private ObservableCollection<ComponentsByItemDTO> _components = [];
        public ObservableCollection<ComponentsByItemDTO> Components
        {
            get => _components;
            set
            {
                if (_components != value)
                {
                    _components = value;
                    NotifyOfPropertyChange(nameof(Components));
                }
            }
        }

        private ObservableCollection<ImageByItemDTO> _itemImages = [];
        public ObservableCollection<ImageByItemDTO> ItemImages
        {
            get => _itemImages;
            set
            {
                if (_itemImages != value)
                {
                    _itemImages = value;
                    NotifyOfPropertyChange(nameof(ItemImages));
                    NotifyOfPropertyChange(nameof(CanAddImage));
                }
            }
        }

        #endregion

        #region EanCode Properties

        private string _eanCode = string.Empty;
        public string EanCode
        {
            get => _eanCode;
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

        private string _selectedEanCode;
        public string SelectedEanCode
        {
            get => _selectedEanCode;
            set
            {
                if (_selectedEanCode != value)
                {
                    _selectedEanCode = value;
                    NotifyOfPropertyChange(nameof(SelectedEanCode));
                }
            }
        }

        #endregion

        #region Related Products Properties

        private string _componentName = string.Empty;
        public string ComponentName
        {
            get => _componentName;
            set
            {
                if (_componentName != value)
                {
                    _componentName = value;
                    NotifyOfPropertyChange(nameof(ComponentName));
                }
            }
        }

        private string _componentReference = string.Empty;
        public string ComponentReference
        {
            get => _componentReference;
            set
            {
                if (_componentReference != value)
                {
                    _componentReference = value;
                    NotifyOfPropertyChange(nameof(ComponentReference));
                }
            }
        }

        private string _componentCode = string.Empty;
        public string ComponentCode
        {
            get => _componentCode;
            set
            {
                if (_componentCode != value)
                {
                    _componentCode = value;
                    NotifyOfPropertyChange(nameof(ComponentCode));
                }
            }
        }

        private decimal _componentQuantity;
        public decimal ComponentQuantity
        {
            get => _componentQuantity;
            set
            {
                if (_componentQuantity != value)
                {
                    _componentQuantity = value;
                    NotifyOfPropertyChange(nameof(ComponentQuantity));
                }
            }
        }

        private bool _componentQuantityIsEnable;
        public bool ComponentQuantityIsEnable
        {
            get => _componentQuantityIsEnable;
            set
            {
                if (_componentQuantityIsEnable != value)
                {
                    _componentQuantityIsEnable = value;
                    NotifyOfPropertyChange(nameof(ComponentQuantityIsEnable));
                }
            }
        }

        private bool _componentAllowFraction;
        public bool ComponentAllowFraction
        {
            get => _componentAllowFraction;
            set
            {
                if (_componentAllowFraction != value)
                {
                    _componentAllowFraction = value;
                    NotifyOfPropertyChange(nameof(ComponentAllowFraction));
                }
            }
        }

        private ComponentsByItemDTO _selectedComponent;
        public ComponentsByItemDTO SelectedComponent
        {
            get => _selectedComponent;
            set
            {
                if (_selectedComponent != value)
                {
                    _selectedComponent = value;
                    NotifyOfPropertyChange(nameof(SelectedComponent));
                }
            }
        }

        private ItemDTO _returnedItemFromModal;
        public ItemDTO ReturnedItemFromModal
        {
            get => _returnedItemFromModal;
            set
            {
                if (_returnedItemFromModal != value)
                {
                    _returnedItemFromModal = value;
                    NotifyOfPropertyChange(nameof(ReturnedItemFromModal));
                }
            }
        }

        private bool _hasComponents;
        public bool HasComponents
        {
            get => _hasComponents;
            set
            {
                if (_hasComponents != value)
                {
                    _hasComponents = value;
                    NotifyOfPropertyChange(nameof(HasComponents));
                }
            }
        }

        #endregion

        #region Other Properties

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    NotifyOfPropertyChange(nameof(SelectedTabIndex));
                }
            }
        }

        private int _subCategoryId;
        public int SubCategoryId
        {
            get => _subCategoryId;
            set
            {
                if (_subCategoryId != value)
                {
                    _subCategoryId = value;
                    NotifyOfPropertyChange(nameof(SubCategoryId));
                }
            }
        }

        #endregion

        #region CanSave

        public override bool CanSave
        {
            get
            {
                if (!IsEditing) return false;
                if (HasErrors) return false;
                if (!this.HasChanges()) return false;
                return true;
            }
        }

        #endregion

        #region Validation

        private void ValidateName()
        {
            ClearErrors(nameof(Name));
            if (string.IsNullOrWhiteSpace(Name))
                AddError(nameof(Name), "El nombre del item no puede estar vacío");
        }

        public override void ValidateAll()
        {
            ValidateName();
        }

        #endregion

        #region Commands - EanCodes

        private ICommand _addEanCodeCommand;
        public ICommand AddEanCodeCommand
        {
            get
            {
                _addEanCodeCommand ??= new RelayCommand(CanAddEanCode, AddEanCode);
                return _addEanCodeCommand;
            }
        }

        public void AddEanCode(object p)
        {
            string eanCode = EanCode;
            EanCode = string.Empty;
            EanCodes.Add(eanCode);
        }

        public bool CanAddEanCode(object p) => !string.IsNullOrEmpty(EanCode);

        private ICommand _deleteEanCodeCommand;
        public ICommand DeleteEanCodeCommand
        {
            get
            {
                _deleteEanCodeCommand ??= new RelayCommand(CanDeleteEanCode, DeleteEanCode);
                return _deleteEanCodeCommand;
            }
        }

        public void DeleteEanCode(object p)
        {
            if (SelectedEanCode != null)
            {
                EanCodes.Remove(SelectedEanCode);
            }
        }

        public bool CanDeleteEanCode(object p) => true;

        #endregion

        #region Commands - Images

        private ICommand _addImageCommand;
        public ICommand AddImageCommand
        {
            get
            {
                _addImageCommand ??= new RelayCommand(CanAddImageCmd, AddImage);
                return _addImageCommand;
            }
        }

        public void AddImage(object p)
        {
            OpenFileDialog fileDialog = new()
            {
                Filter = "Image Files (*.jpg; *.jpeg; *.png; *.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
            };
            if (fileDialog.ShowDialog() == true)
            {
                FileInfo fileInfo = new(fileDialog.FileName);
                long fileSizeLimit = 400 * 1024;
                if (fileInfo.Length > fileSizeLimit)
                {
                    DevExpress.Xpf.Core.ThemedMessageBox.Show(
                        title: "Archivo demasiado grande",
                        text: "El archivo seleccionado es demasiado grande. Por favor, selecciona un archivo de menos de 400KB",
                        messageBoxButtons: System.Windows.MessageBoxButton.OK,
                        image: System.Windows.MessageBoxImage.Warning);
                }
                else
                {
                    string selectedFilePath = fileDialog.FileName;
                    string fileName = Path.GetFileName(selectedFilePath);
                    BitmapImage bitmap = ConvertBitMapImage(selectedFilePath);
                    ImageByItemDTO itemImage = new()
                    {
                        ImagePath = selectedFilePath,
                        SourceImage = bitmap,
                        S3FileName = fileName.Replace(" ", "_").ToLower(),
                        S3Bucket = S3Helper.S3Bucket,
                        S3BucketDirectory = S3Helper.S3Directory
                    };
                    ItemImages.Add(itemImage);
                }
            }
        }

        public bool CanAddImageCmd(object p) => ItemImages != null && ItemImages.Count < 4;
        public bool CanAddImage => ItemImages != null && ItemImages.Count < 4;

        private ICommand _deleteImageCommand;
        public ICommand DeleteImageCommand
        {
            get
            {
                _deleteImageCommand ??= new RelayCommand(CanDeleteImage, DeleteImage);
                return _deleteImageCommand;
            }
        }

        public void DeleteImage(object p)
        {
            if (p is ImageByItemDTO itemImageDTO)
            {
                ItemImages.Remove(itemImageDTO);
            }
        }

        public bool CanDeleteImage(object p) => true;

        private BitmapImage ConvertBitMapImage(string imageFilePath)
        {
            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(imageFilePath, UriKind.Absolute);
            bitmap.EndInit();
            return bitmap;
        }

        #endregion

        #region Commands - Related Products

        private ICommand _openSearchComponents;
        public ICommand OpenSearchComponents
        {
            get
            {
                _openSearchComponents ??= new RelayCommand(CanOpenSearchComponents, SearchComponents);
                return _openSearchComponents;
            }
        }

        public async void SearchComponents(object p)
        {
            string query = @"query($filter: ItemFilterInput){
                        PageResponse: itemPage(filter: $filter){
                        count
                        rows{
                            id
                            name
                            code
                            reference
                            allowFraction
                            measurementUnit{
                            id
                            name
                            }
                        }
                        }
                    }";

            string fieldHeader1 = "Código";
            string fieldHeader2 = "Nombre";
            string fieldHeader3 = "Referencia";
            string fieldData1 = "Code";
            string fieldData2 = "Name";
            string fieldData3 = "Reference";
            dynamic variables = new System.Dynamic.ExpandoObject();
            variables.filter = new System.Dynamic.ExpandoObject();
            variables.filter.and = new System.Dynamic.ExpandoObject[]
            {
                new(),
                new()
            };
            variables.filter.and[0].catalogId = new System.Dynamic.ExpandoObject();
            variables.filter.and[0].catalogId.@operator = "=";
            variables.filter.and[0].catalogId.value = MasterContext.SelectedCatalog.Id;
            var viewModel = new SearchItemModalViewModel<ItemDTO, ItemGraphQLModel>(query, fieldHeader1, fieldHeader2, fieldHeader3, fieldData1, fieldData2, fieldData3, variables, MessageToken.Component, MasterContext.Context, _dialogService);

            await _dialogService.ShowDialogAsync(viewModel, "Búsqueda de productos");
        }

        public bool CanOpenSearchComponents(object p) => true;

        private ICommand _deleteComponentCommand;
        public ICommand DeleteComponentCommand
        {
            get
            {
                _deleteComponentCommand ??= new RelayCommand(CanDeleteComponent, DeleteComponent);
                return _deleteComponentCommand;
            }
        }

        public void DeleteComponent(object p)
        {
            if (SelectedComponent != null)
            {
                Components.Remove(SelectedComponent);
            }
        }

        public bool CanDeleteComponent(object p) => true;

        private ICommand _addComponentCommand;
        public ICommand AddComponentCommand
        {
            get
            {
                _addComponentCommand ??= new RelayCommand(CanAddComponent, AddComponent);
                return _addComponentCommand;
            }
        }

        public void AddComponent(object p)
        {
            ComponentsByItemDTO component = new() { Component = ReturnedItemFromModal, Parent = (ItemDTO)MasterContext.SelectedItem, Quantity = ComponentQuantity };
            ComponentName = string.Empty;
            ComponentReference = string.Empty;
            ComponentCode = string.Empty;
            ComponentQuantity = 0;
            ComponentQuantityIsEnable = false;
            Components.Add(component);
        }

        public bool CanAddComponent(object p) => ComponentQuantity != 0;

        private void OnFindComponentMessage(ReturnedItemFromModalViewMessage message)
        {
            ReturnedItemFromModal = MasterContext.Context.AutoMapper.Map<ItemDTO>(message.ReturnedItem);
            ComponentName = ReturnedItemFromModal.Name;
            ComponentReference = ReturnedItemFromModal.Reference;
            ComponentCode = ReturnedItemFromModal.Code;
            ComponentAllowFraction = ReturnedItemFromModal.AllowFraction;
        }

        #endregion

        #region SetForNew / SetForEdit

        public override void SetForNew(object context)
        {
            if (context is not int subCategoryId) return;

            OriginalDto = null;
            Id = 0;
            SubCategoryId = subCategoryId;
            Name = string.Empty;
            Code = string.Empty;
            Reference = string.Empty;
            IsActive = true;
            AllowFraction = false;
            Billable = false;
            AmountBasedOnWeight = false;
            HasExtendedInformation = false;
            AiuBasedService = false;
            EanCodes = [];
            Components = [];
            ItemImages = [];
            SelectedTabIndex = 0;

            // Set defaults from parent ItemType
            var selectedItem = MasterContext.SelectedItem;
            if (selectedItem is ItemSubCategoryDTO subCatDTO)
            {
                ItemTypeDTO itemTypeDTO = MasterContext.SelectedCatalog.ItemTypes.FirstOrDefault(x => x.Id == subCatDTO.ItemCategory.ItemType.Id);
                if (itemTypeDTO != null)
                {
                    SelectedMeasurementUnit = MeasurementUnits?.FirstOrDefault(x => x.Id == itemTypeDTO.DefaultMeasurementUnit?.Id);
                    SelectedAccountingGroup = AccountingGroups?.FirstOrDefault(x => x.Id == itemTypeDTO.DefaultAccountingGroup?.Id);
                    HasComponents = !itemTypeDTO.StockControl;
                }
            }
            SelectedBrand = ItemBrands?.FirstOrDefault(x => x.Id == 0);
            SelectedSize = Sizes?.FirstOrDefault(x => x.Id == 0);

            ComponentName = string.Empty;
            ComponentCode = string.Empty;
            ComponentReference = string.Empty;
            ComponentQuantity = 0;
            ComponentQuantityIsEnable = false;

            SeedDefaultValues();
            ClearAllErrors();
            ValidateAll();
            IsEditing = true;
        }

        public override void SetForEdit(object dto)
        {
            if (dto is not ItemDTO itemDTO) return;

            OriginalDto = itemDTO;
            Id = itemDTO.Id;
            Name = itemDTO.Name;
            Code = itemDTO.Code;
            Reference = itemDTO.Reference;
            IsActive = itemDTO.IsActive;
            AllowFraction = itemDTO.AllowFraction;
            Billable = itemDTO.Billable;
            AmountBasedOnWeight = itemDTO.AmountBasedOnWeight;
            HasExtendedInformation = itemDTO.HasExtendedInformation;
            AiuBasedService = itemDTO.AiuBasedService;
            SubCategoryId = itemDTO.SubCategory?.Id ?? 0;

            // ComboBox selections
            if (itemDTO.Brand != null)
            {
                SelectedBrand = ItemBrands?.FirstOrDefault(x => x.Id == itemDTO.Brand.Id);
                BrandId = itemDTO.Brand.Id;
            }
            else
            {
                SelectedBrand = ItemBrands?.FirstOrDefault(x => x.Id == 0);
                BrandId = 0;
            }

            if (itemDTO.SizeCategory != null)
            {
                SelectedSize = Sizes?.FirstOrDefault(x => x.Id == itemDTO.SizeCategory.Id);
                SizeId = itemDTO.SizeCategory.Id;
            }
            else
            {
                SelectedSize = Sizes?.FirstOrDefault(x => x.Id == 0);
                SizeId = 0;
            }

            if (itemDTO.MeasurementUnit != null)
            {
                SelectedMeasurementUnit = MeasurementUnits?.FirstOrDefault(x => x.Id == itemDTO.MeasurementUnit.Id);
                MeasurementUnitId = itemDTO.MeasurementUnit.Id;
            }

            if (itemDTO.AccountingGroup != null)
            {
                SelectedAccountingGroup = AccountingGroups?.FirstOrDefault(x => x.Id == itemDTO.AccountingGroup.Id);
                AccountingGroupId = itemDTO.AccountingGroup.Id;
            }

            // Collections
            EanCodes = itemDTO.EanCodes != null
                ? new ObservableCollection<string>(itemDTO.EanCodes)
                : [];
            Components = itemDTO.Components != null
                ? new ObservableCollection<ComponentsByItemDTO>(itemDTO.Components.Select(x => (ComponentsByItemDTO)x.Clone()).ToList())
                : [];
            ItemImages = itemDTO.Images != null
                ? new ObservableCollection<ImageByItemDTO>(itemDTO.Images.Select(x => (ImageByItemDTO)x.Clone()).ToList())
                : [];

            // Load images from local/S3
            _ = LoadImagesAsync();

            SelectedTabIndex = 0;
            EanCode = string.Empty;
            HasComponents = itemDTO.SubCategory?.ItemCategory?.ItemType?.StockControl == false;
            ComponentName = string.Empty;
            ComponentCode = string.Empty;
            ComponentReference = string.Empty;
            ComponentQuantity = 0;
            ComponentQuantityIsEnable = false;

            SeedCurrentValues();
            ClearAllErrors();
            ValidateAll();
            IsEditing = false;
        }

        private async Task LoadImagesAsync()
        {
            if (ItemImages.Count > 0)
            {
                foreach (ImageByItemDTO image in ItemImages)
                {
                    string directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string imagesLocalPath = Path.Combine(directoryPath, "custom", "catalog_item_images", "bd_berdic", image.S3FileName);
                    if (!Path.Exists(imagesLocalPath))
                    {
                        S3Helper.S3FileName = image.S3FileName;
                        S3Helper.LocalFilePath = imagesLocalPath;
                        await S3Helper.DownloadFileFromS3();
                    }
                    BitmapImage bitmap = ConvertBitMapImage(imagesLocalPath);
                    image.SourceImage = bitmap;
                    image.ImagePath = imagesLocalPath;
                }
            }
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(Reference), Reference);
            this.SeedValue(nameof(IsActive), IsActive);
            this.SeedValue(nameof(AllowFraction), AllowFraction);
            this.SeedValue(nameof(Billable), Billable);
            this.SeedValue(nameof(AmountBasedOnWeight), AmountBasedOnWeight);
            this.SeedValue(nameof(HasExtendedInformation), HasExtendedInformation);
            this.SeedValue(nameof(AiuBasedService), AiuBasedService);
            this.SeedValue(nameof(MeasurementUnitId), MeasurementUnitId);
            this.SeedValue(nameof(BrandId), BrandId);
            this.SeedValue(nameof(AccountingGroupId), AccountingGroupId);
            this.SeedValue(nameof(SizeId), SizeId);
            this.AcceptChanges();
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.AcceptChanges();
        }

        #endregion

        #region Abstract Methods Implementation

        protected override int GetId() => Id;

        protected override string GetCreateQuery()
        {
            return @"
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
                    accountingGroup{ id }
                    brand{ id }
                    measurementUnit{ id }
                    size{ id }
                    subCategory{
                      id
                      itemCategory{
                        id
                        itemType{ id }
                      }
                    }
                    eanCodes{ id eanCode }
                    images{
                        id s3Bucket s3BucketDirectory s3FileName order
                        item{ id }
                    }
                    components{
                        id quantity
                        item{ id name code reference allowFraction measurementUnit{ id name } }
                        parent{ id }
                    }
                  }
                }";
        }

        protected override string GetUpdateQuery()
        {
            return @"
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
                    accountingGroup{ id }
                    brand{ id }
                    measurementUnit{ id }
                    size{ id }
                    subCategory{
                      id
                      itemCategory{
                        id
                        itemType{ id }
                      }
                    }
                    eanCodes{ id eanCode }
                    images{
                        id s3Bucket s3BucketDirectory s3FileName order
                        item{ id }
                    }
                    components{
                        id quantity
                        item{ id name code reference allowFraction measurementUnit{ id name } }
                        parent{ id }
                    }
                  }
                }";
        }

        protected override async Task<UpsertResponseType<ItemGraphQLModel>> ExecuteSaveAsync()
        {
            // Handle S3 images
            await HandleS3ImagesAsync();

            // Build variables
            List<object> eanCodes = [];
            List<object> components = [];
            List<object> images = [];

            if (ItemImages != null)
            {
                foreach (ImageByItemDTO image in ItemImages)
                {
                    images.Add(new { image.S3Bucket, image.S3BucketDirectory, image.S3FileName, DisplayOrder = ItemImages.IndexOf(image) });
                }
            }

            if (EanCodes != null)
            {
                foreach (string eanCode in EanCodes)
                {
                    eanCodes.Add(eanCode);
                }
            }

            if (Components != null)
            {
                foreach (ComponentsByItemDTO component in Components)
                {
                    components.Add(new { itemId = component.Component.Id, component.Quantity });
                }
            }

            dynamic variables = new System.Dynamic.ExpandoObject();
            variables.Data = new System.Dynamic.ExpandoObject();
            if (!IsNewRecord) variables.Id = Id;
            variables.Data.Name = Name;
            variables.Data.Reference = Reference;
            variables.Data.IsActive = IsActive;
            variables.Data.AllowFraction = AllowFraction;
            variables.Data.HasExtendedInformation = HasExtendedInformation;
            variables.Data.AiuBasedService = AiuBasedService;
            variables.Data.AmountBasedOnWeight = AmountBasedOnWeight;
            variables.Data.Billable = Billable;
            variables.Data.MeasurementUnitId = SelectedMeasurementUnit?.Id ?? 0;
            variables.Data.ItemBrandId = SelectedBrand?.Id ?? 0;
            variables.Data.AccountingGroupId = SelectedAccountingGroup?.Id ?? 0;
            variables.Data.ItemSizeCategoryId = SelectedSize?.Id ?? 0;
            if (IsNewRecord) variables.Data.ItemSubCategoryId = SubCategoryId;
            variables.Data.EanCodes = eanCodes;
            variables.Data.Components = components;
            variables.Data.Images = images;

            string query = IsNewRecord ? GetCreateQuery() : GetUpdateQuery();

            // Wrap in UpsertResponseType manually since existing API returns ItemGraphQLModel directly
            var result = IsNewRecord
                ? await _itemService.CreateAsync(query, variables)
                : await _itemService.UpdateAsync(query, variables);

            return new UpsertResponseType<ItemGraphQLModel>
            {
                Entity = result,
                Success = true,
                Message = "OK"
            };
        }

        private async Task HandleS3ImagesAsync()
        {
            if (ItemImages == null) return;

            if (IsNewRecord)
            {
                foreach (ImageByItemDTO image in ItemImages)
                {
                    S3Helper.LocalFilePath = image.ImagePath;
                    S3Helper.S3FileName = image.S3FileName;
                    await S3Helper.UploadFileToS3Async();
                    string directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string destinationPath = Path.Combine(directoryPath, "custom", "catalog_item_images", "bd_berdic", image.S3FileName);
                    File.Copy(image.ImagePath, destinationPath, true);
                }
            }
            else
            {
                // Compare with original images to find additions/deletions
                var originalImages = OriginalDto?.Images != null
                    ? new List<ImageByItemDTO>(OriginalDto.Images.Select(x => (ImageByItemDTO)x.Clone()).ToList())
                    : new List<ImageByItemDTO>();

                var itemsToDelete = originalImages.Where(item => !ItemImages.Select(i => i.S3FileName).Contains(item.S3FileName)).ToList();
                var itemsToAdd = ItemImages.Where(item => !originalImages.Select(i => i.S3FileName).Contains(item.S3FileName)).ToList();

                foreach (ImageByItemDTO image in itemsToDelete)
                {
                    S3Helper.S3FileName = image.S3FileName;
                    await S3Helper.DeleteFileFromS3Async();
                    string directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string destinationPath = Path.Combine(directoryPath, "custom", "catalog_item_images", "bd_berdic", image.S3FileName);
                    if (Path.Exists(destinationPath)) File.Delete(destinationPath);
                }

                foreach (ImageByItemDTO image in itemsToAdd)
                {
                    S3Helper.LocalFilePath = image.ImagePath;
                    S3Helper.S3FileName = image.S3FileName;
                    await S3Helper.UploadFileToS3Async();
                    string directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string destinationPath = Path.Combine(directoryPath, "custom", "catalog_item_images", "bd_berdic", image.S3FileName);
                    File.Copy(image.ImagePath, destinationPath, true);
                }
            }
        }

        protected override async Task PublishMessageAsync(UpsertResponseType<ItemGraphQLModel> result)
        {
            if (IsNewRecord)
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new ItemCreateMessage { CreatedItem = result.Entity });
            }
            else
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new ItemUpdateMessage { UpdatedItem = result.Entity });
            }
        }

        #endregion
    }
}
