using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using Models.Inventory;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Inventory.CatalogItems.DTO;
using NetErp.Inventory.CatalogItems.PanelEditors;
using NetErp.Inventory.ItemSizes.DTO;
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
using System.Windows.Input;
using Dictionaries;
using NetErp.Helpers.GraphQLQueryBuilder;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Inventory.CatalogItems.ViewModels
{
    public class CatalogRootMasterViewModel : Screen,
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
        IHandle<ItemUpdateMessage>
    {
        #region Services

        private readonly IRepository<CatalogGraphQLModel> _catalogService;
        private readonly IRepository<ItemTypeGraphQLModel> _itemTypeService;
        private readonly IRepository<ItemCategoryGraphQLModel> _itemCategoryService;
        private readonly IRepository<ItemSubCategoryGraphQLModel> _itemSubCategoryService;
        private readonly IRepository<ItemGraphQLModel> _itemService;
        private readonly IRepository<AwsS3ConfigGraphQLModel> _awsS3Service;
        private readonly Helpers.IDialogService _dialogService;
        private readonly Helpers.Services.INotificationService _notificationService;

        // Caches
        private readonly MeasurementUnitCache _measurementUnitCache;
        private readonly ItemBrandCache _itemBrandCache;
        private readonly AccountingGroupCache _accountingGroupCache;
        private readonly ItemSizeCategoryCache _itemSizeCategoryCache;

        #endregion

        #region PanelEditors

        public CatalogPanelEditor CatalogEditor { get; private set; }
        public ItemTypePanelEditor ItemTypeEditor { get; private set; }
        public ItemCategoryPanelEditor ItemCategoryEditor { get; private set; }
        public ItemSubCategoryPanelEditor ItemSubCategoryEditor { get; private set; }
        public ItemPanelEditor ItemEditor { get; private set; }

        private ICatalogItemsPanelEditor? _currentPanelEditor;
        public ICatalogItemsPanelEditor? CurrentPanelEditor
        {
            get => _currentPanelEditor;
            private set
            {
                if (_currentPanelEditor != value)
                {
                    _currentPanelEditor = value;
                    NotifyOfPropertyChange(nameof(CurrentPanelEditor));
                    NotifyOfPropertyChange(nameof(ContentControlVisibility));
                }
            }
        }

        public bool ContentControlVisibility => CurrentPanelEditor != null;

        #endregion

        #region Properties

        private CatalogViewModel _context;
        public CatalogViewModel Context
        {
            get => _context;
            set
            {
                if (_context != value)
                {
                    _context = value;
                    NotifyOfPropertyChange(nameof(Context));
                }
            }
        }

        public AwsS3ConfigGraphQLModel AwsS3Config { get; set; }

        private bool _isNewRecord;
        public bool IsNewRecord
        {
            get => _isNewRecord;
            set
            {
                if (_isNewRecord != value)
                {
                    _isNewRecord = value;
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        private int _selectedIndex;
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    NotifyOfPropertyChange(nameof(SelectedIndex));
                }
            }
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    NotifyOfPropertyChange(nameof(IsEditing));
                    NotifyOfPropertyChange(nameof(TreeViewIsEnable));
                    NotifyOfPropertyChange(nameof(CanSave));
                    NotifyOfPropertyChange(nameof(SelectedCatalogIsEnable));
                    NotifyOfPropertyChange(nameof(MainRibbonPageIsEnable));
                }
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        private bool _canEdit = true;
        public bool CanEdit
        {
            get => _canEdit;
            set
            {
                if (_canEdit != value)
                {
                    _canEdit = value;
                    NotifyOfPropertyChange(nameof(CanEdit));
                }
            }
        }

        private bool _canUndo;
        public bool CanUndo
        {
            get => _canUndo;
            set
            {
                if (_canUndo != value)
                {
                    _canUndo = value;
                    NotifyOfPropertyChange(nameof(CanUndo));
                }
            }
        }

        public bool CanSave => CurrentPanelEditor?.CanSave ?? false;

        public void RefreshCanSave() => NotifyOfPropertyChange(nameof(CanSave));

        public bool TreeViewIsEnable => !IsEditing;
        public bool SelectedCatalogIsEnable => !IsEditing;
        public bool MainRibbonPageIsEnable => !IsEditing;

        public int SelectedSubCategoryIdBeforeNewItem { get; set; }

        #endregion

        #region ComboBox Collections

        private ObservableCollection<MeasurementUnitDTO> _measurementUnits;
        public ObservableCollection<MeasurementUnitDTO> MeasurementUnits
        {
            get => _measurementUnits;
            set
            {
                if (_measurementUnits != value)
                {
                    _measurementUnits = value;
                    NotifyOfPropertyChange(nameof(MeasurementUnits));
                }
            }
        }

        private ObservableCollection<ItemBrandDTO> _itemBrands;
        public ObservableCollection<ItemBrandDTO> ItemBrands
        {
            get => _itemBrands;
            set
            {
                if (_itemBrands != value)
                {
                    _itemBrands = value;
                    NotifyOfPropertyChange(nameof(ItemBrands));
                }
            }
        }

        private ObservableCollection<AccountingGroupDTO> _accountingGroups;
        public ObservableCollection<AccountingGroupDTO> AccountingGroups
        {
            get => _accountingGroups;
            set
            {
                if (_accountingGroups != value)
                {
                    _accountingGroups = value;
                    NotifyOfPropertyChange(nameof(AccountingGroups));
                }
            }
        }

        private ObservableCollection<ItemSizeCategoryDTO> _itemSizeCategories;
        public ObservableCollection<ItemSizeCategoryDTO> ItemSizeCategories
        {
            get => _itemSizeCategories;
            set
            {
                if (_itemSizeCategories != value)
                {
                    _itemSizeCategories = value;
                    NotifyOfPropertyChange(nameof(ItemSizeCategories));
                }
            }
        }

        #endregion

        #region Tree Properties

        private ICatalogItem? _selectedItem;
        public ICatalogItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(ItemDTOIsSelected));
                    HandleSelectedItemChanged();
                }
            }
        }

        public bool ItemDTOIsSelected => SelectedItem is ItemDTO && SelectedCatalog != null;

        private ObservableCollection<CatalogDTO> _catalogs = [];
        public ObservableCollection<CatalogDTO> Catalogs
        {
            get => _catalogs;
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
            get => _selectedCatalog;
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
            get => _itemsCategories;
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
            get => _itemsSubCategories;
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
            get => _items;
            set
            {
                if (_items != value)
                {
                    _items = value;
                    NotifyOfPropertyChange(nameof(Items));
                }
            }
        }

        public bool CatalogIsSelected => SelectedCatalog != null;
        public bool DeleteCatalogButtonEnable => SelectedCatalog != null && SelectedCatalog.ItemTypes.Count == 0;

        #endregion

        #region Constructor

        public CatalogRootMasterViewModel(
            CatalogViewModel context,
            IRepository<CatalogGraphQLModel> catalogService,
            IRepository<ItemTypeGraphQLModel> itemTypeService,
            IRepository<ItemCategoryGraphQLModel> itemCategoryService,
            IRepository<ItemSubCategoryGraphQLModel> itemSubCategoryService,
            IRepository<ItemGraphQLModel> itemService,
            IRepository<AwsS3ConfigGraphQLModel> awsS3Service,
            Helpers.IDialogService dialogService,
            Helpers.Services.INotificationService notificationService,
            MeasurementUnitCache measurementUnitCache,
            ItemBrandCache itemBrandCache,
            AccountingGroupCache accountingGroupCache,
            ItemSizeCategoryCache itemSizeCategoryCache)
        {
            Context = context;
            _catalogService = catalogService;
            _itemTypeService = itemTypeService;
            _itemCategoryService = itemCategoryService;
            _itemSubCategoryService = itemSubCategoryService;
            _itemService = itemService;
            _awsS3Service = awsS3Service;
            _dialogService = dialogService;
            _notificationService = notificationService;
            _measurementUnitCache = measurementUnitCache;
            _itemBrandCache = itemBrandCache;
            _accountingGroupCache = accountingGroupCache;
            _itemSizeCategoryCache = itemSizeCategoryCache;

            // Initialize PanelEditors
            CatalogEditor = new CatalogPanelEditor(this, _catalogService);
            ItemTypeEditor = new ItemTypePanelEditor(this, _itemTypeService);
            ItemCategoryEditor = new ItemCategoryPanelEditor(this, _itemCategoryService);
            ItemSubCategoryEditor = new ItemSubCategoryPanelEditor(this, _itemSubCategoryService);
            ItemEditor = new ItemPanelEditor(this, _itemService, _dialogService);

            // Register for search product messages
            Messenger.Default.Register<ReturnedItemFromModalViewMessage>(this, MessageToken.SearchProduct, false, OnFindProductMessage);

            Context.EventAggregator.SubscribeOnUIThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async Task OnActivatedAsync(CancellationToken cancellationToken)
        {
            if (Context.EnableOnActivateAsync is false) return;
            await base.OnActivatedAsync(cancellationToken);
            await OnStartUpAsync();
        }

        public async Task OnStartUpAsync()
        {
            try
            {
                IsBusy = true;
                await LoadComboBoxesAsync();
                //await LoadAwsS3Credentials();
                await LoadCatalogsAsync();
            }
            catch (AsyncException ex)
            {
                await App.Current.Dispatcher.InvokeAsync(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al inicializar el módulo.\r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
                await Context.TryCloseAsync();
            }
            catch (Exception ex)
            {
                await App.Current.Dispatcher.InvokeAsync(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al inicializar el módulo.\r\n{GetType().Name}.{nameof(OnStartUpAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
                await Context.TryCloseAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                Context.EventAggregator.Unsubscribe(this);
                Messenger.Default.Unregister(this);
            }
            await base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region HandleSelectedItemChanged

        public void HandleSelectedItemChanged()
        {
            if (_selectedItem != null && !(_selectedItem is ItemDTO { IsDummyChild: true })
                                     && !(_selectedItem is ItemTypeDTO { IsDummyChild: true })
                                     && !(_selectedItem is ItemCategoryDTO { IsDummyChild: true })
                                     && !(_selectedItem is ItemSubCategoryDTO { IsDummyChild: true }))
            {
                CurrentPanelEditor = _selectedItem switch
                {
                    ItemTypeDTO => ItemTypeEditor,
                    ItemCategoryDTO => ItemCategoryEditor,
                    ItemSubCategoryDTO => ItemSubCategoryEditor,
                    ItemDTO => ItemEditor,
                    _ => null
                };

                if (!IsNewRecord && CurrentPanelEditor != null)
                {
                    IsEditing = false;
                    CanEdit = true;
                    CanUndo = false;

                    if (_selectedItem is ItemDTO itemDTO)
                    {
                        _ = SetItemForEditAsync(itemDTO);
                    }
                    else
                    {
                        CurrentPanelEditor.SetForEdit(_selectedItem);
                    }
                }
            }
            else if (_selectedItem == null)
            {
                CurrentPanelEditor = null;
            }
        }

        private async Task SetItemForEditAsync(ItemDTO itemDTO)
        {
            ItemEditor.SetForEdit(itemDTO);
            // Download S3 images if needed
            if (ItemEditor.ItemImages.Count > 0)
            {
                foreach (ImageByItemDTO image in ItemEditor.ItemImages)
                {
                    string directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string imagesLocalPath = Path.Combine(directoryPath, "custom", "catalog_item_images", "bd_berdic", image.S3FileName);
                    if (!Path.Exists(imagesLocalPath))
                    {
                        S3Helper.S3FileName = image.S3FileName;
                        S3Helper.LocalFilePath = imagesLocalPath;
                        await S3Helper.DownloadFileFromS3();
                    }
                    System.Windows.Media.Imaging.BitmapImage bitmap = new();
                    bitmap.BeginInit();
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(imagesLocalPath, UriKind.Absolute);
                    bitmap.EndInit();
                    image.SourceImage = bitmap;
                    image.ImagePath = imagesLocalPath;
                }
            }
        }

        #endregion

        #region Edit/Save/Undo Commands

        public void Edit()
        {
            IsEditing = true;
            CanUndo = true;
            CanEdit = false;
            IsNewRecord = false;

            if (CurrentPanelEditor != null)
            {
                CurrentPanelEditor.IsEditing = true;
            }
        }

        public async Task Save()
        {
            if (CurrentPanelEditor == null) return;

            try
            {
                IsBusy = true;
                Refresh();

                bool saveSuccessful = await CurrentPanelEditor.SaveAsync();

                if (saveSuccessful)
                {
                    if (CurrentPanelEditor == CatalogEditor)
                        CurrentPanelEditor = null;

                    IsEditing = false;
                    CanUndo = false;
                    CanEdit = true;
                    IsNewRecord = false;
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void Undo()
        {
            CurrentPanelEditor?.Undo();

            if (IsNewRecord)
            {
                SelectedItem = null;
                CurrentPanelEditor = null;
            }

            IsEditing = false;
            CanUndo = false;
            CanEdit = true;
            IsNewRecord = false;
        }

        private ICommand _editCommand;
        public ICommand EditCommand
        {
            get
            {
                _editCommand ??= new DevExpress.Mvvm.DelegateCommand(Edit, () => CanEdit);
                return _editCommand;
            }
        }

        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                _saveCommand ??= new AsyncCommand(Save, CanSave);
                return _saveCommand;
            }
        }

        private ICommand _undoCommand;
        public ICommand UndoCommand
        {
            get
            {
                _undoCommand ??= new DevExpress.Mvvm.DelegateCommand(Undo, () => CanUndo);
                return _undoCommand;
            }
        }

        #endregion

        #region Create Commands (context menus)

        public async Task CreateItemType()
        {
            if (SelectedCatalog == null) return;
            IsNewRecord = true;
            CurrentPanelEditor = ItemTypeEditor;
            ItemTypeEditor.SetForNew(SelectedCatalog.Id);
            IsEditing = true;
            CanUndo = true;
            CanEdit = false;
        }

        public async Task CreateItemCategory()
        {
            if (SelectedItem is not ItemTypeDTO itemType) return;
            IsNewRecord = true;
            CurrentPanelEditor = ItemCategoryEditor;
            ItemCategoryEditor.SetForNew(itemType.Id);
            IsEditing = true;
            CanUndo = true;
            CanEdit = false;
        }

        public async Task CreateItemSubCategory()
        {
            if (SelectedItem is not ItemCategoryDTO itemCategory) return;
            IsNewRecord = true;
            CurrentPanelEditor = ItemSubCategoryEditor;
            ItemSubCategoryEditor.SetForNew(itemCategory.Id);
            IsEditing = true;
            CanUndo = true;
            CanEdit = false;
        }

        public async Task CreateItem()
        {
            if (SelectedItem is not ItemSubCategoryDTO subCategory) return;
            SelectedSubCategoryIdBeforeNewItem = subCategory.Id;
            IsNewRecord = true;
            CurrentPanelEditor = ItemEditor;
            ItemEditor.SetForNew(subCategory.Id);
            IsEditing = true;
            CanUndo = true;
            CanEdit = false;
        }

        public async Task CreateCatalog()
        {
            IsNewRecord = true;
            CurrentPanelEditor = CatalogEditor;
            CatalogEditor.SetForNew(null);
            IsEditing = true;
            CanUndo = true;
            CanEdit = false;
        }

        public async Task UpdateCatalog()
        {
            if (SelectedCatalog == null) return;
            CurrentPanelEditor = CatalogEditor;
            CatalogEditor.SetForEdit(new CatalogDTO { Id = SelectedCatalog.Id, Name = SelectedCatalog.Name });
            CatalogEditor.IsEditing = true;
            IsEditing = true;
            CanEdit = false;
            CanUndo = true;
        }

        public bool CanCreateCatalog => true;
        public bool CanUpdateCatalog => true;
        public bool CanDeleteCatalog => true;
        public bool CanCreateItemType => true;
        public bool CanUpdateItemType => true;
        public bool CanDeleteItemType => true;
        public bool CanCreateItemCategory => true;
        public bool CanUpdateItemCategory => true;
        public bool CanDeleteItemCategory => true;
        public bool CanCreateItemSubCategory => true;
        public bool CanUpdateItemSubCategory => true;
        public bool CanDeleteItemSubCategory => true;
        public bool CanCreateItem => true;
        public bool CanDeleteItem => true;
        public bool CanDiscontinueItem => true;

        #endregion

        #region Create/Update/Delete Commands (ICommand)

        private ICommand _createCatalogCommand;
        public ICommand CreateCatalogCommand
        {
            get
            {
                _createCatalogCommand ??= new AsyncCommand(CreateCatalog, CanCreateCatalog);
                return _createCatalogCommand;
            }
        }

        private ICommand _updateCatalogCommand;
        public ICommand UpdateCatalogCommand
        {
            get
            {
                _updateCatalogCommand ??= new AsyncCommand(UpdateCatalog, CanUpdateCatalog);
                return _updateCatalogCommand;
            }
        }

        private ICommand _deleteCatalogCommand;
        public ICommand DeleteCatalogCommand
        {
            get
            {
                _deleteCatalogCommand ??= new AsyncCommand(DeleteCatalog, CanDeleteCatalog);
                return _deleteCatalogCommand;
            }
        }

        private ICommand _createItemTypeCommand;
        public ICommand CreateItemTypeCommand
        {
            get
            {
                _createItemTypeCommand ??= new AsyncCommand(CreateItemType, CanCreateItemType);
                return _createItemTypeCommand;
            }
        }

        private ICommand _updateItemTypeCommand;
        public ICommand UpdateItemTypeCommand
        {
            get
            {
                _updateItemTypeCommand ??= new AsyncCommand(UpdateItemType, CanUpdateItemType);
                return _updateItemTypeCommand;
            }
        }

        private ICommand _deleteItemTypeCommand;
        public ICommand DeleteItemTypeCommand
        {
            get
            {
                _deleteItemTypeCommand ??= new AsyncCommand(DeleteItemType, CanDeleteItemType);
                return _deleteItemTypeCommand;
            }
        }

        private ICommand _createItemCategoryCommand;
        public ICommand CreateItemCategoryCommand
        {
            get
            {
                _createItemCategoryCommand ??= new AsyncCommand(CreateItemCategory, CanCreateItemCategory);
                return _createItemCategoryCommand;
            }
        }

        private ICommand _updateItemCategoryCommand;
        public ICommand UpdateItemCategoryCommand
        {
            get
            {
                _updateItemCategoryCommand ??= new AsyncCommand(UpdateItemCategory, CanUpdateItemCategory);
                return _updateItemCategoryCommand;
            }
        }

        private ICommand _deleteItemCategoryCommand;
        public ICommand DeleteItemCategoryCommand
        {
            get
            {
                _deleteItemCategoryCommand ??= new AsyncCommand(DeleteItemCategory, CanDeleteItemCategory);
                return _deleteItemCategoryCommand;
            }
        }

        private ICommand _createItemSubCategoryCommand;
        public ICommand CreateItemSubCategoryCommand
        {
            get
            {
                _createItemSubCategoryCommand ??= new AsyncCommand(CreateItemSubCategory, CanCreateItemSubCategory);
                return _createItemSubCategoryCommand;
            }
        }

        private ICommand _updateItemSubCategoryCommand;
        public ICommand UpdateItemSubCategoryCommand
        {
            get
            {
                _updateItemSubCategoryCommand ??= new AsyncCommand(UpdateItemSubCategory, CanUpdateItemSubCategory);
                return _updateItemSubCategoryCommand;
            }
        }

        private ICommand _deleteItemSubCategoryCommand;
        public ICommand DeleteItemSubCategoryCommand
        {
            get
            {
                _deleteItemSubCategoryCommand ??= new AsyncCommand(DeleteItemSubCategory, CanDeleteItemSubCategory);
                return _deleteItemSubCategoryCommand;
            }
        }

        private ICommand _createItemCommand;
        public ICommand CreateItemCommand
        {
            get
            {
                _createItemCommand ??= new AsyncCommand(CreateItem, CanCreateItem);
                return _createItemCommand;
            }
        }

        private ICommand _deleteItemCommand;
        public ICommand DeleteItemCommand
        {
            get
            {
                _deleteItemCommand ??= new AsyncCommand(DeleteItem, CanDeleteItem);
                return _deleteItemCommand;
            }
        }

        private ICommand _discontinueItemCommand;
        public ICommand DiscontinueItemCommand
        {
            get
            {
                _discontinueItemCommand ??= new AsyncCommand(DiscontinueItem, CanDiscontinueItem);
                return _discontinueItemCommand;
            }
        }

        private ICommand _openSearchProducts;
        public ICommand OpenSearchProducts
        {
            get
            {
                _openSearchProducts ??= new RelayCommand(CanOpenSearchProducts, SearchProducts);
                return _openSearchProducts;
            }
        }

        public bool CanOpenSearchProducts(object p) => true;

        #endregion

        #region Update methods (set for edit via context menu)

        public async Task UpdateItemType()
        {
            if (SelectedItem is not ItemTypeDTO itemType) return;
            CurrentPanelEditor = ItemTypeEditor;
            ItemTypeEditor.SetForEdit(itemType);
            IsEditing = false;
            CanEdit = true;
            CanUndo = false;
        }

        public async Task UpdateItemCategory()
        {
            if (SelectedItem is not ItemCategoryDTO itemCategory) return;
            CurrentPanelEditor = ItemCategoryEditor;
            ItemCategoryEditor.SetForEdit(itemCategory);
            IsEditing = false;
            CanEdit = true;
            CanUndo = false;
        }

        public async Task UpdateItemSubCategory()
        {
            if (SelectedItem is not ItemSubCategoryDTO itemSubCategory) return;
            CurrentPanelEditor = ItemSubCategoryEditor;
            ItemSubCategoryEditor.SetForEdit(itemSubCategory);
            IsEditing = false;
            CanEdit = true;
            CanUndo = false;
        }

        #endregion

        #region Delete Methods

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
                var validation = await _catalogService.CanDeleteAsync(query, variables);

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

                string deleteQuery = @"
                mutation ($id: Int!) {
                  DeleteResponse: deleteCatalog(id: $id) {
                    id
                    name
                  }
                }";

                object deleteVariables = new { Id = id };
                CatalogGraphQLModel deletedCatalog = await _catalogService.DeleteAsync(deleteQuery, deleteVariables);
                SelectedItem = null;

                await Context.EventAggregator.PublishOnUIThreadAsync(new CatalogDeleteMessage() { DeletedCatalog = deletedCatalog });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                if (graphQLError != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(DeleteCatalog)} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else { throw; }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(DeleteCatalog)} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
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
                var validation = await _itemTypeService.CanDeleteAsync(query, variables);

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

                string deleteQuery = @"
                mutation ($id: Int!) {
                  DeleteResponse: deleteItemType(id: $id) {
                    id
                    name
                    prefixChar
                    stockControl
                  }
                }";

                object deleteVariables = new { Id = id };
                ItemTypeGraphQLModel deletedItemType = await _itemTypeService.DeleteAsync(deleteQuery, deleteVariables);
                SelectedItem = null;

                await Context.EventAggregator.PublishOnUIThreadAsync(new ItemTypeDeleteMessage() { DeletedItemType = deletedItemType });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                if (graphQLError != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(DeleteItemType)} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else { throw; }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(DeleteItemType)} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
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
                var validation = await _itemCategoryService.CanDeleteAsync(query, variables);

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

                string deleteQuery = @"
                mutation ($id: Int!) {
                  DeleteResponse: deleteItemCategory(id: $id) {
                    id
                    name
                    itemType{
                        id
                    }
                  }
                }";

                object deleteVariables = new { Id = id };
                ItemCategoryGraphQLModel deletedItemCategory = await _itemCategoryService.DeleteAsync(deleteQuery, deleteVariables);
                SelectedItem = null;

                await Context.EventAggregator.PublishOnUIThreadAsync(new ItemCategoryDeleteMessage() { DeletedItemCategory = deletedItemCategory });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                if (graphQLError != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(DeleteItemCategory)} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else { throw; }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(DeleteItemCategory)} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
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
                var validation = await _itemSubCategoryService.CanDeleteAsync(query, variables);

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

                string deleteQuery = @"
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

                object deleteVariables = new { Id = id };
                ItemSubCategoryGraphQLModel deletedItemSubCategory = await _itemSubCategoryService.DeleteAsync(deleteQuery, deleteVariables);
                SelectedItem = null;

                await Context.EventAggregator.PublishOnUIThreadAsync(new ItemSubCategoryDeleteMessage() { DeletedItemSubCategory = deletedItemSubCategory });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                if (graphQLError != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(DeleteItemSubCategory)} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else { throw; }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(DeleteItemSubCategory)} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
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
                var validation = await _itemService.CanDeleteAsync(query, variables);

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

                IsBusy = true;
                Refresh();

                // Delete images from S3 and local repository
                if (ItemEditor.ItemImages != null && ItemEditor.ItemImages.Count > 0)
                {
                    foreach (ImageByItemDTO image in ItemEditor.ItemImages)
                    {
                        string directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        string imagesLocalPath = Path.Combine(directoryPath, "custom", "catalog_item_images", "bd_berdic", image.S3FileName);
                        if (Path.Exists(imagesLocalPath)) File.Delete(imagesLocalPath);
                        S3Helper.S3FileName = image.S3FileName;
                        await S3Helper.DeleteFileFromS3Async();
                    }
                }

                string deleteQuery = @"
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
                    eanCodes{ id }
                  }
                }";

                object deleteVariables = new { Id = id };
                ItemGraphQLModel deletedItem = await _itemService.DeleteAsync(deleteQuery, deleteVariables);
                SelectedItem = null;

                await Context.EventAggregator.PublishOnUIThreadAsync(new ItemDeleteMessage() { DeletedItem = deletedItem });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                if (graphQLError != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(DeleteItem)} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else { throw; }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(DeleteItem)} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DiscontinueItem()
        {
            try
            {
                IsBusy = true;
                int id = ((ItemDTO)SelectedItem).Id;

                string query = @"query($id:Int!){
                  CanDeleteModel: canDiscontinueItem(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };
                var validation = await _itemService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea descontinuar el registro {((ItemDTO)SelectedItem).Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser descontinuado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }

                Refresh();

                string updateQuery = @"
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
                    eanCodes{ id }
                  }
                }";

                object updateVariables = new { Id = id, Data = new { IsActive = false } };
                ItemGraphQLModel discontinuedItem = await _itemService.UpdateAsync(updateQuery, updateVariables);
                SelectedItem = null;

                await Context.EventAggregator.PublishOnUIThreadAsync(new ItemDeleteMessage() { DeletedItem = discontinuedItem });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                if (graphQLError != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(DiscontinueItem)} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else { throw; }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(DiscontinueItem)} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Search Products

        public async void SearchProducts(object p)
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
                                subCategory{
                                    id
                                    itemCategory{
                                        id
                                        itemType{
                                            id
                                        }
                                    }
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
            dynamic variables = new ExpandoObject();
            variables.filter = new ExpandoObject();
            variables.filter.and = new ExpandoObject[]
            {
                new(),
                new()
            };
            variables.filter.and[0].catalogId = new ExpandoObject();
            variables.filter.and[0].catalogId.@operator = "=";
            variables.filter.and[0].catalogId.value = SelectedCatalog.Id;
            var viewModel = new SearchItemModalViewModel<ItemDTO, ItemGraphQLModel>(query, fieldHeader1, fieldHeader2, fieldHeader3, fieldData1, fieldData2, fieldData3, variables, MessageToken.SearchProduct, Context, _dialogService);

            await _dialogService.ShowDialogAsync(viewModel, "Búsqueda de productos");
        }

        public async void OnFindProductMessage(ReturnedItemFromModalViewMessage message)
        {
            IsBusy = true;
            await OnFindProductMessageAsync(message);
            IsBusy = false;
        }

        public async Task OnFindProductMessageAsync(ReturnedItemFromModalViewMessage message)
        {
            ItemDTO itemDTO = Context.AutoMapper.Map<ItemDTO>(message.ReturnedItem);
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemTypes.FirstOrDefault(x => x.Id == itemDTO.SubCategory.ItemCategory.ItemType.Id);
            if (itemTypeDTO is null) return;
            if (!itemTypeDTO.IsExpanded && itemTypeDTO.ItemsCategories[0].IsDummyChild)
            {
                await LoadItemsCategories(itemTypeDTO);
                itemTypeDTO.IsExpanded = true;
            }
            if (!itemTypeDTO.IsExpanded) itemTypeDTO.IsExpanded = true;
            ItemCategoryDTO? itemCategoryDTO = itemTypeDTO.ItemsCategories.FirstOrDefault(x => x.Id == itemDTO.SubCategory.ItemCategory.Id);
            if (itemCategoryDTO is null) return;
            if (!itemCategoryDTO.IsExpanded && itemCategoryDTO.SubCategories[0].IsDummyChild)
            {
                await LoadItemsSubCategories(itemCategoryDTO);
                itemCategoryDTO.IsExpanded = true;
            }
            if (!itemCategoryDTO.IsExpanded) itemCategoryDTO.IsExpanded = true;
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
                ItemDTO? item = itemSubCategoryDTO.Items.FirstOrDefault(x => x.Id == itemDTO.Id);
                SelectedItem = item;
                return;
            }
            ItemDTO? selectedItem = itemSubCategoryDTO.Items.FirstOrDefault(x => x.Id == itemDTO.Id);
            SelectedItem = selectedItem;
        }

        #endregion

        #region Data Loading

        public async Task LoadAwsS3Credentials()
        {
            try
            {
                string query = @"
                query{
                  SingleItemResponse: awsS3Configs{
                    id
                    secretKey
                    accessKey
                    description
                    region
                  }
                }";
                AwsS3Config = await _awsS3Service.FindByIdAsync(query, new { });
                S3Helper.Initialize("qtsattachments".ToLower(), "berdic/products_images".ToLower(), AwsS3Config.AccessKey, AwsS3Config.SecretKey, GlobalDictionaries.AwsSesRegionDictionary[AwsS3Config.Region]);
            }
            catch (Exception ex)
            {
                throw new AsyncException(GetType(), ex);
            }
        }

        public async Task LoadComboBoxesAsync()
        {
            try
            {
                await Task.WhenAll(
                    _measurementUnitCache.EnsureLoadedAsync(),
                    _itemBrandCache.EnsureLoadedAsync(),
                    _accountingGroupCache.EnsureLoadedAsync(),
                    _itemSizeCategoryCache.EnsureLoadedAsync()
                );

                MeasurementUnits = Context.AutoMapper.Map<ObservableCollection<MeasurementUnitDTO>>(_measurementUnitCache.Items);
                ItemBrands = Context.AutoMapper.Map<ObservableCollection<ItemBrandDTO>>(_itemBrandCache.Items);
                AccountingGroups = Context.AutoMapper.Map<ObservableCollection<AccountingGroupDTO>>(_accountingGroupCache.Items);
                ItemSizeCategories = Context.AutoMapper.Map<ObservableCollection<ItemSizeCategoryDTO>>(_itemSizeCategoryCache.Items);
            }
            catch (Exception ex) when (ex is not AsyncException)
            {
                throw new AsyncException(GetType(), ex);
            }
        }

        private static string GetLoadCatalogsQuery()
        {
            var fields = FieldSpec<PageType<CatalogGraphQLModel>>
                .Create()
                .Field(f => f.PageNumber)
                .Field(f => f.PageSize)
                .Field(f => f.TotalPages)
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .SelectList(e => e.ItemTypes, it => it
                        .Field(t => t.Id)
                        .Field(t => t.Name)
                        .Field(t => t.PrefixChar)
                        .Field(t => t.StockControl)
                        .Select(t => t.DefaultMeasurementUnit, mu => mu
                            .Field(m => m.Id))
                        .Select(t => t.DefaultAccountingGroup, ag => ag
                            .Field(a => a.Id))
                        .Select(t => t.Catalog, c => c
                            .Field(cc => cc.Id))))
                .Build();

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("catalogsPage", [paginationParam], fields, "PageResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        public async Task LoadCatalogsAsync()
        {
            try
            {
                Refresh();
                string query = GetLoadCatalogsQuery();

                dynamic variables = new ExpandoObject();
                variables.PageResponsePagination = new ExpandoObject();
                variables.PageResponsePagination.PageSize = -1;

                var result = await _catalogService.GetPageAsync(query, variables);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Catalogs = Context.AutoMapper.Map<ObservableCollection<CatalogDTO>>(result.Entries);

                    if (Catalogs.Count > 0)
                    {
                        foreach (CatalogDTO catalog in Catalogs)
                        {
                            foreach (ItemTypeDTO itemType in catalog.ItemTypes)
                            {
                                itemType.Context = this;
                                itemType.ItemsCategories.Add(new ItemCategoryDTO() { IsDummyChild = true, SubCategories = [], Name = "Dummy" });
                            }
                        }
                        SelectedCatalog = Catalogs.First();
                    }
                    else
                    {
                        SelectedCatalog = null;
                    }
                });
            }
            catch (Exception ex)
            {
                throw new AsyncException(GetType(), ex);
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
                      ListResponse: itemsCategoriesByItemTypesIds(ids: $ids){
                        id
                        name
                        itemType{
                            id
                        }
                      }
                    }";
                dynamic variables = new ExpandoObject();
                variables.ids = ids;

                var source = await _itemCategoryService.GetListAsync(query, variables);
                ItemsCategories = Context.AutoMapper.Map<ObservableCollection<ItemCategoryDTO>>(source);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (ItemCategoryDTO itemCategory in ItemsCategories)
                    {
                        itemCategory.Context = this;
                        itemCategory.SubCategories.Add(new ItemSubCategoryDTO() { IsDummyChild = true, Items = [], Name = "Dummy" });
                        itemType.ItemsCategories.Add(itemCategory);
                    }
                });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                if (graphQLError != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(LoadItemsCategories)} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else { throw; }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(LoadItemsCategories)} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
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

                var source = await _itemSubCategoryService.GetListAsync(query, variables);
                ItemsSubCategories = Context.AutoMapper.Map<ObservableCollection<ItemSubCategoryDTO>>(source);

                foreach (ItemSubCategoryDTO itemSubCategory in ItemsSubCategories)
                {
                    itemSubCategory.Context = this;
                    itemSubCategory.Items.Add(new ItemDTO() { IsDummyChild = true, EanCodes = [], Name = "Dummy" });
                    itemCategory.SubCategories.Add(itemSubCategory);
                }
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                if (graphQLError != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(LoadItemsSubCategories)} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else { throw; }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(LoadItemsSubCategories)} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task LoadItems(ItemSubCategoryDTO itemSubCategory)
        {
            try
            {
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
                        measurementUnit{ id }
                        brand{ id }
                        accountingGroup{ id }
                        size{ id }
                        eanCodes{
                            id
                            eanCode
                        }
                        images{
                            id
                            s3Bucket
                            s3BucketDirectory
                            s3FileName
                            order
                            item{ id }
                        }
                        relatedProducts{
                            id
                            quantity
                            item{
                                id
                                name
                                reference
                                code
                                measurementUnit{
                                    id
                                    name
                                }
                            }
                        }
                        subCategory{
                            id
                            itemCategory{
                                id
                                itemType{
                                    id
                                    stockControl
                                }
                            }
                        }
                      }
                    }";
                dynamic variables = new ExpandoObject();
                variables.ids = ids;

                var source = await _itemService.GetListAsync(query, variables);
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
                if (graphQLError != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(LoadItems)} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else { throw; }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(LoadItems)} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        #endregion

        #region Message Handlers

        public Task HandleAsync(ItemTypeCreateMessage message, CancellationToken cancellationToken)
        {
            ItemTypeDTO itemTypeDTO = Context.AutoMapper.Map<ItemTypeDTO>(message.CreatedItemType);
            itemTypeDTO.Context = this;
            itemTypeDTO.ItemsCategories.Add(new ItemCategoryDTO() { IsDummyChild = true, Name = "Dummy", SubCategories = [] });
            if (SelectedCatalog.Id != itemTypeDTO.Catalog.Id) return Task.CompletedTask;
            SelectedCatalog.ItemTypes.Add(itemTypeDTO);
            SelectedItem = itemTypeDTO;
            _notificationService.ShowSuccess("Tipo de item creado correctamente");
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemTypeDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemTypes.Where(x => x.Id == message.DeletedItemType.Id).FirstOrDefault();
                if (itemTypeDTO is null) return;
                SelectedCatalog.ItemTypes.Remove(itemTypeDTO);
                SelectedItem = null;
            });
            _notificationService.ShowSuccess("Tipo de item eliminado correctamente");
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemTypeUpdateMessage message, CancellationToken cancellationToken)
        {
            ItemTypeDTO itemTypeDTO = Context.AutoMapper.Map<ItemTypeDTO>(message.UpdatedItemType);
            ItemTypeDTO? itemToUpdate = SelectedCatalog.ItemTypes.FirstOrDefault(x => x.Id == message.UpdatedItemType.Id);
            if (itemToUpdate == null) return Task.CompletedTask;
            itemToUpdate.Id = itemTypeDTO.Id;
            itemToUpdate.Name = itemTypeDTO.Name;
            itemToUpdate.PrefixChar = itemTypeDTO.PrefixChar;
            itemToUpdate.StockControl = itemTypeDTO.StockControl;
            itemToUpdate.DefaultMeasurementUnit = itemTypeDTO.DefaultMeasurementUnit;
            itemToUpdate.DefaultAccountingGroup = itemTypeDTO.DefaultAccountingGroup;
            _notificationService.ShowSuccess("Tipo de item actualizado correctamente");
            return Task.CompletedTask;
        }

        public async Task HandleAsync(ItemCategoryCreateMessage message, CancellationToken cancellationToken)
        {
            ItemCategoryDTO itemCategoryDTO = Context.AutoMapper.Map<ItemCategoryDTO>(message.CreatedItemCategory);
            itemCategoryDTO.Context = this;
            itemCategoryDTO.SubCategories.Add(new ItemSubCategoryDTO() { IsDummyChild = true, Name = "Dummy", Items = [] });
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemTypes.FirstOrDefault(x => x.Id == itemCategoryDTO.ItemType.Id);
            if (itemTypeDTO is null) return;
            if (itemTypeDTO.IsExpanded == false && itemTypeDTO.ItemsCategories[0].IsDummyChild)
            {
                await LoadItemsCategories(itemTypeDTO);
                itemTypeDTO.IsExpanded = true;
                ItemCategoryDTO? itemCategory = itemTypeDTO.ItemsCategories.FirstOrDefault(x => x.Id == itemCategoryDTO.Id);
                if (itemCategory is null) return;
                SelectedItem = itemCategory;
                _notificationService.ShowSuccess("Categoría de item creada correctamente");
                return;
            }
            if (itemTypeDTO.IsExpanded == false)
            {
                itemTypeDTO.IsExpanded = true;
                itemTypeDTO.ItemsCategories.Add(itemCategoryDTO);
                SelectedItem = itemCategoryDTO;
                _notificationService.ShowSuccess("Categoría de item creada correctamente");
                return;
            }
            itemTypeDTO.ItemsCategories.Add(itemCategoryDTO);
            SelectedItem = itemCategoryDTO;
            _notificationService.ShowSuccess("Categoría de item creada correctamente");
        }

        public Task HandleAsync(ItemCategoryDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemTypes.Where(x => x.Id == message.DeletedItemCategory.ItemType.Id).FirstOrDefault();
                if (itemTypeDTO is null) return;
                itemTypeDTO.ItemsCategories.Remove(itemTypeDTO.ItemsCategories.Where(x => x.Id == message.DeletedItemCategory.Id).First());
                SelectedItem = null;
            });
            _notificationService.ShowSuccess("Categoría de item eliminada correctamente");
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemCategoryUpdateMessage message, CancellationToken cancellationToken)
        {
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemTypes.Where(x => x.Id == message.UpdatedItemCategory.ItemType.Id).FirstOrDefault();
            if (itemTypeDTO is null) return Task.CompletedTask;
            ItemCategoryDTO? itemCategoryDTOToUpdate = itemTypeDTO.ItemsCategories.Where(x => x.Id == message.UpdatedItemCategory.Id).FirstOrDefault();
            if (itemCategoryDTOToUpdate is null) return Task.CompletedTask;
            itemCategoryDTOToUpdate.Id = message.UpdatedItemCategory.Id;
            itemCategoryDTOToUpdate.Name = message.UpdatedItemCategory.Name;
            _notificationService.ShowSuccess("Categoría de item actualizada correctamente");
            return Task.CompletedTask;
        }

        public async Task HandleAsync(ItemSubCategoryCreateMessage message, CancellationToken cancellationToken)
        {
            ItemSubCategoryDTO itemSubCategoryDTO = Context.AutoMapper.Map<ItemSubCategoryDTO>(message.CreatedItemSubCategory);
            itemSubCategoryDTO.Context = this;
            itemSubCategoryDTO.Items.Add(new ItemDTO() { IsDummyChild = true, Name = "Dummy" });
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemTypes.FirstOrDefault(x => x.Id == itemSubCategoryDTO.ItemCategory.ItemType.Id);
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
                _notificationService.ShowSuccess("Subcategoría de item creada correctamente");
                return;
            }
            if (!itemCategoryDTO.IsExpanded)
            {
                itemCategoryDTO.IsExpanded = true;
                itemCategoryDTO.SubCategories.Add(itemSubCategoryDTO);
                SelectedItem = itemSubCategoryDTO;
                _notificationService.ShowSuccess("Subcategoría de item creada correctamente");
                return;
            }
            itemCategoryDTO.SubCategories.Add(itemSubCategoryDTO);
            SelectedItem = itemSubCategoryDTO;
            _notificationService.ShowSuccess("Subcategoría de item creada correctamente");
        }

        public Task HandleAsync(ItemSubCategoryDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemTypes.Where(x => x.Id == message.DeletedItemSubCategory.ItemCategory.ItemType.Id).FirstOrDefault();
                if (itemTypeDTO is null) return;
                ItemCategoryDTO? itemCategoryDTO = itemTypeDTO.ItemsCategories.FirstOrDefault(x => x.Id == message.DeletedItemSubCategory.ItemCategory.Id);
                if (itemCategoryDTO is null) return;
                itemCategoryDTO.SubCategories.Remove(itemCategoryDTO.SubCategories.Where(x => x.Id == message.DeletedItemSubCategory.Id).First());
                SelectedItem = null;
            });
            _notificationService.ShowSuccess("Subcategoría de item eliminada correctamente");
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemSubCategoryUpdateMessage message, CancellationToken cancellationToken)
        {
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemTypes.Where(x => x.Id == message.UpdatedItemSubCategory.ItemCategory.ItemType.Id).FirstOrDefault();
            if (itemTypeDTO is null) return Task.CompletedTask;
            ItemCategoryDTO? itemCategoryDTO = itemTypeDTO.ItemsCategories.FirstOrDefault(x => x.Id == message.UpdatedItemSubCategory.ItemCategory.Id);
            if (itemCategoryDTO is null) return Task.CompletedTask;
            ItemSubCategoryDTO? itemSubCategoryDTOToUpdate = itemCategoryDTO.SubCategories.FirstOrDefault(x => x.Id == message.UpdatedItemSubCategory.Id);
            if (itemSubCategoryDTOToUpdate is null) return Task.CompletedTask;
            itemSubCategoryDTOToUpdate.Id = message.UpdatedItemSubCategory.Id;
            itemSubCategoryDTOToUpdate.Name = message.UpdatedItemSubCategory.Name;
            _notificationService.ShowSuccess("Subcategoría de item actualizada correctamente");
            return Task.CompletedTask;
        }

        public Task HandleAsync(CatalogCreateMessage message, CancellationToken cancellationToken)
        {
            CatalogDTO catalogDTO = Context.AutoMapper.Map<CatalogDTO>(message.CreatedCatalog.Entity);
            Catalogs.Add(catalogDTO);
            SelectedCatalog = catalogDTO;
            _notificationService.ShowSuccess(message.CreatedCatalog.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(CatalogUpdateMessage message, CancellationToken cancellationToken)
        {
            CatalogDTO catalogDTO = Context.AutoMapper.Map<CatalogDTO>(message.UpdatedCatalog.Entity);
            CatalogDTO? catalogToUpdate = Catalogs.FirstOrDefault(x => x.Id == catalogDTO.Id);
            if (catalogToUpdate is null) return Task.CompletedTask;
            catalogToUpdate.Id = catalogDTO.Id;
            catalogToUpdate.Name = catalogDTO.Name;
            _notificationService.ShowSuccess(message.UpdatedCatalog.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(CatalogDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Catalogs.Remove(Catalogs.Where(x => x.Id == message.DeletedCatalog.Id).First());
                SelectedCatalog = Catalogs.First();
            });
            _notificationService.ShowSuccess("Catálogo eliminado correctamente");
            return Task.CompletedTask;
        }

        public async Task HandleAsync(ItemDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemTypes.Where(x => x.Id == message.DeletedItem.SubCategory.ItemCategory.ItemType.Id).FirstOrDefault();
                if (itemTypeDTO is null) return;
                ItemCategoryDTO? itemCategoryDTO = itemTypeDTO.ItemsCategories.FirstOrDefault(x => x.Id == message.DeletedItem.SubCategory.ItemCategory.Id);
                if (itemCategoryDTO is null) return;
                ItemSubCategoryDTO? itemSubCategoryDTO = itemCategoryDTO.SubCategories.FirstOrDefault(x => x.Id == message.DeletedItem.SubCategory.Id);
                if (itemSubCategoryDTO is null) return;
                itemSubCategoryDTO.Items.Remove(itemSubCategoryDTO.Items.Where(x => x.Id == message.DeletedItem.Id).First());
                SelectedItem = null;
            });
            _notificationService.ShowSuccess("Item eliminado correctamente");
        }

        public async Task HandleAsync(ItemCreateMessage message, CancellationToken cancellationToken)
        {
            ItemDTO itemDTO = Context.AutoMapper.Map<ItemDTO>(message.CreatedItem);
            itemDTO.Context = this;
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemTypes.FirstOrDefault(x => x.Id == itemDTO.SubCategory.ItemCategory.ItemType.Id);
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
                _notificationService.ShowSuccess("Item creado correctamente");
                return;
            }
            if (!itemSubCategoryDTO.IsExpanded)
            {
                itemSubCategoryDTO.IsExpanded = true;
                itemSubCategoryDTO.Items.Add(itemDTO);
                SelectedItem = itemDTO;
                _notificationService.ShowSuccess("Item creado correctamente");
                return;
            }
            itemSubCategoryDTO.Items.Add(itemDTO);
            SelectedItem = itemDTO;
            _notificationService.ShowSuccess("Item creado correctamente");
        }

        public Task HandleAsync(ItemUpdateMessage message, CancellationToken cancellationToken)
        {
            ItemDTO item = Context.AutoMapper.Map<ItemDTO>(message.UpdatedItem);
            item.Context = this;
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemTypes.Where(x => x.Id == message.UpdatedItem.SubCategory.ItemCategory.ItemType.Id).FirstOrDefault();
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
            itemDTOToUpdate.SizeCategory = item.SizeCategory;
            itemDTOToUpdate.EanCodes = new ObservableCollection<string>(item.EanCodes);
            itemDTOToUpdate.Components = new ObservableCollection<ComponentsByItemDTO>(item.Components);
            itemDTOToUpdate.Images = new ObservableCollection<ImageByItemDTO>(item.Images);
            _notificationService.ShowSuccess("Item actualizado correctamente");
            return Task.CompletedTask;
        }

        #endregion
    }
}
