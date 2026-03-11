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
using NetErp.Global.Modals.ViewModels;
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
        private readonly IRepository<S3StorageLocationGraphQLModel> _s3LocationService;
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

        public S3Helper S3Helper { get; private set; }
        public string LocalImageCachePath { get; private set; }

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
            IRepository<S3StorageLocationGraphQLModel> s3LocationService,
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
            _s3LocationService = s3LocationService;
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
            Messenger.Default.Register<ReturnedDataFromModalWithThreeColumnsGridViewMessage<ItemGraphQLModel>>(this, SearchWithThreeColumnsGridMessageToken.SearchProduct, false, OnFindProductMessage);

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
                await LoadS3ConfigAsync();
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
            if (ItemEditor.Images.Count > 0)
            {
                foreach (ImageByItemDTO image in ItemEditor.Images)
                {
                    string imagesLocalPath = Path.Combine(LocalImageCachePath, image.S3FileName);
                    if (!Path.Exists(imagesLocalPath))
                    {
                        await S3Helper.DownloadFileAsync(imagesLocalPath, image.S3FileName);
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

        public async Task SaveAsync()
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
            bool wasCatalogEditor = CurrentPanelEditor == CatalogEditor;

            CurrentPanelEditor?.Undo();

            if (IsNewRecord || wasCatalogEditor)
            {
                SelectedItem = null;
                CurrentPanelEditor = null;
            }

            IsEditing = false;
            CanUndo = false;
            CanEdit = SelectedItem != null;
            IsNewRecord = false;
        }

        private ICommand _editCommand;
        public ICommand EditCommand
        {
            get
            {
                _editCommand ??= new DevExpress.Mvvm.DelegateCommand(Edit);
                return _editCommand;
            }
        }

        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                _saveCommand ??= new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        private ICommand _undoCommand;
        public ICommand UndoCommand
        {
            get
            {
                _undoCommand ??= new DevExpress.Mvvm.DelegateCommand(Undo);
                return _undoCommand;
            }
        }

        #endregion

        #region Create Commands (context menus)

        public async Task CreateItemTypeAsync()
        {
            if (SelectedCatalog == null) return;
            IsNewRecord = true;
            CurrentPanelEditor = ItemTypeEditor;
            ItemTypeEditor.SetForNew(SelectedCatalog.Id);
            IsEditing = true;
            CanUndo = true;
            CanEdit = false;
        }

        public async Task CreateItemCategoryAsync()
        {
            if (SelectedItem is not ItemTypeDTO itemType) return;
            IsNewRecord = true;
            CurrentPanelEditor = ItemCategoryEditor;
            ItemCategoryEditor.SetForNew(itemType.Id);
            IsEditing = true;
            CanUndo = true;
            CanEdit = false;
        }

        public async Task CreateItemSubCategoryAsync()
        {
            if (SelectedItem is not ItemCategoryDTO itemCategory) return;
            IsNewRecord = true;
            CurrentPanelEditor = ItemSubCategoryEditor;
            ItemSubCategoryEditor.SetForNew(itemCategory.Id);
            IsEditing = true;
            CanUndo = true;
            CanEdit = false;
        }

        public async Task CreateItemAsync()
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

        public async Task CreateCatalogAsync()
        {
            SelectedItem = null;
            IsNewRecord = true;
            CurrentPanelEditor = CatalogEditor;
            CatalogEditor.SetForNew(null);
            IsEditing = true;
            CanUndo = true;
            CanEdit = false;
        }

        public async Task UpdateCatalogAsync()
        {
            if (SelectedCatalog == null) return;
            SelectedItem = null;
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
                _createCatalogCommand ??= new AsyncCommand(CreateCatalogAsync, CanCreateCatalog);
                return _createCatalogCommand;
            }
        }

        private ICommand _updateCatalogCommand;
        public ICommand UpdateCatalogCommand
        {
            get
            {
                _updateCatalogCommand ??= new AsyncCommand(UpdateCatalogAsync, CanUpdateCatalog);
                return _updateCatalogCommand;
            }
        }

        private ICommand _deleteCatalogCommand;
        public ICommand DeleteCatalogCommand
        {
            get
            {
                _deleteCatalogCommand ??= new AsyncCommand(DeleteCatalogAsync, CanDeleteCatalog);
                return _deleteCatalogCommand;
            }
        }

        private ICommand _createItemTypeCommand;
        public ICommand CreateItemTypeCommand
        {
            get
            {
                _createItemTypeCommand ??= new AsyncCommand(CreateItemTypeAsync, CanCreateItemType);
                return _createItemTypeCommand;
            }
        }

        private ICommand _updateItemTypeCommand;
        public ICommand UpdateItemTypeCommand
        {
            get
            {
                _updateItemTypeCommand ??= new AsyncCommand(UpdateItemTypeAsync, CanUpdateItemType);
                return _updateItemTypeCommand;
            }
        }

        private ICommand _deleteItemTypeCommand;
        public ICommand DeleteItemTypeCommand
        {
            get
            {
                _deleteItemTypeCommand ??= new AsyncCommand(DeleteItemTypeAsync, CanDeleteItemType);
                return _deleteItemTypeCommand;
            }
        }

        private ICommand _createItemCategoryCommand;
        public ICommand CreateItemCategoryCommand
        {
            get
            {
                _createItemCategoryCommand ??= new AsyncCommand(CreateItemCategoryAsync, CanCreateItemCategory);
                return _createItemCategoryCommand;
            }
        }

        private ICommand _updateItemCategoryCommand;
        public ICommand UpdateItemCategoryCommand
        {
            get
            {
                _updateItemCategoryCommand ??= new AsyncCommand(UpdateItemCategoryAsync, CanUpdateItemCategory);
                return _updateItemCategoryCommand;
            }
        }

        private ICommand _deleteItemCategoryCommand;
        public ICommand DeleteItemCategoryCommand
        {
            get
            {
                _deleteItemCategoryCommand ??= new AsyncCommand(DeleteItemCategoryAsync, CanDeleteItemCategory);
                return _deleteItemCategoryCommand;
            }
        }

        private ICommand _createItemSubCategoryCommand;
        public ICommand CreateItemSubCategoryCommand
        {
            get
            {
                _createItemSubCategoryCommand ??= new AsyncCommand(CreateItemSubCategoryAsync, CanCreateItemSubCategory);
                return _createItemSubCategoryCommand;
            }
        }

        private ICommand _updateItemSubCategoryCommand;
        public ICommand UpdateItemSubCategoryCommand
        {
            get
            {
                _updateItemSubCategoryCommand ??= new AsyncCommand(UpdateItemSubCategoryAsync, CanUpdateItemSubCategory);
                return _updateItemSubCategoryCommand;
            }
        }

        private ICommand _deleteItemSubCategoryCommand;
        public ICommand DeleteItemSubCategoryCommand
        {
            get
            {
                _deleteItemSubCategoryCommand ??= new AsyncCommand(DeleteItemSubCategoryAsync, CanDeleteItemSubCategory);
                return _deleteItemSubCategoryCommand;
            }
        }

        private ICommand _createItemCommand;
        public ICommand CreateItemCommand
        {
            get
            {
                _createItemCommand ??= new AsyncCommand(CreateItemAsync, CanCreateItem);
                return _createItemCommand;
            }
        }

        private ICommand _deleteItemCommand;
        public ICommand DeleteItemCommand
        {
            get
            {
                _deleteItemCommand ??= new AsyncCommand(DeleteItemAsync, CanDeleteItem);
                return _deleteItemCommand;
            }
        }

        private ICommand _discontinueItemCommand;
        public ICommand DiscontinueItemCommand
        {
            get
            {
                _discontinueItemCommand ??= new AsyncCommand(DiscontinueItemAsync, CanDiscontinueItem);
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

        public async Task UpdateItemTypeAsync()
        {
            if (SelectedItem is not ItemTypeDTO itemType) return;
            CurrentPanelEditor = ItemTypeEditor;
            ItemTypeEditor.SetForEdit(itemType);
            IsEditing = false;
            CanEdit = true;
            CanUndo = false;
        }

        public async Task UpdateItemCategoryAsync()
        {
            if (SelectedItem is not ItemCategoryDTO itemCategory) return;
            CurrentPanelEditor = ItemCategoryEditor;
            ItemCategoryEditor.SetForEdit(itemCategory);
            IsEditing = false;
            CanEdit = true;
            CanUndo = false;
        }

        public async Task UpdateItemSubCategoryAsync()
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

        public async Task DeleteCatalogAsync()
        {
            if (SelectedCatalog == null) return;

            try
            {
                IsBusy = true;
                Refresh();

                int id = SelectedCatalog.Id;
                string canDeleteQuery = GetCanDeleteQuery("canDeleteCatalog");
                object canDeleteVariables = new { canDeleteResponseId = id };
                var validation = await _catalogService.CanDeleteAsync(canDeleteQuery, canDeleteVariables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(
                        title: "Confirme...",
                        text: $"¿Confirma que desea eliminar el catálogo {SelectedCatalog.Name}?",
                        messageBoxButtons: MessageBoxButton.YesNo,
                        image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"El registro no puede ser eliminado\n\n{validation.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error));
                    return;
                }

                IsBusy = true;
                Refresh();

                string deleteQuery = GetDeleteMutationQuery("deleteCatalog");
                object deleteVariables = new { deleteResponseId = id };
                DeleteResponseType deleteResult = await _catalogService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVariables);

                if (!deleteResult.Success)
                {
                    ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"No se pudo eliminar el registro.\n\n{deleteResult.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error);
                    return;
                }

                SelectedItem = null;
                CurrentPanelEditor = null;

                await Context.EventAggregator.PublishOnUIThreadAsync(new CatalogDeleteMessage { DeletedCatalog = deleteResult });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(
                    exGraphQL.Content?.ToString() ?? "");
                if (graphQLError != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"{GetType().Name}.{nameof(DeleteCatalogAsync)} \r\n{graphQLError.Errors[0].Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error));
                }
                else { throw; }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(DeleteCatalogAsync)} \r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteItemTypeAsync()
        {
            if (SelectedItem is not ItemTypeDTO itemType) return;

            try
            {
                IsBusy = true;
                Refresh();

                int id = itemType.Id;
                string canDeleteQuery = GetCanDeleteQuery("canDeleteItemType");
                object canDeleteVariables = new { canDeleteResponseId = id };
                var validation = await _itemTypeService.CanDeleteAsync(canDeleteQuery, canDeleteVariables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(
                        title: "Confirme...",
                        text: $"¿Confirma que desea eliminar el tipo de item {itemType.Name}?",
                        messageBoxButtons: MessageBoxButton.YesNo,
                        image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"El registro no puede ser eliminado\n\n{validation.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error));
                    return;
                }

                IsBusy = true;
                Refresh();

                string deleteQuery = GetDeleteMutationQuery("deleteItemType");
                object deleteVariables = new { deleteResponseId = id };
                DeleteResponseType deleteResult = await _itemTypeService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVariables);

                if (!deleteResult.Success)
                {
                    ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"No se pudo eliminar el registro.\n\n{deleteResult.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error);
                    return;
                }

                SelectedItem = null;
                CurrentPanelEditor = null;

                await Context.EventAggregator.PublishOnUIThreadAsync(new ItemTypeDeleteMessage { DeletedItemType = deleteResult });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(
                    exGraphQL.Content?.ToString() ?? "");
                if (graphQLError != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"{GetType().Name}.{nameof(DeleteItemTypeAsync)} \r\n{graphQLError.Errors[0].Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error));
                }
                else { throw; }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(DeleteItemTypeAsync)} \r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteItemCategoryAsync()
        {
            if (SelectedItem is not ItemCategoryDTO itemCategory) return;

            try
            {
                IsBusy = true;
                Refresh();

                int id = itemCategory.Id;
                string canDeleteQuery = GetCanDeleteQuery("canDeleteItemCategory");
                object canDeleteVariables = new { canDeleteResponseId = id };
                var validation = await _itemCategoryService.CanDeleteAsync(canDeleteQuery, canDeleteVariables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(
                        title: "Confirme...",
                        text: $"¿Confirma que desea eliminar la categoría {itemCategory.Name}?",
                        messageBoxButtons: MessageBoxButton.YesNo,
                        image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"El registro no puede ser eliminado\n\n{validation.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error));
                    return;
                }

                IsBusy = true;
                Refresh();

                string deleteQuery = GetDeleteMutationQuery("deleteItemCategory");
                object deleteVariables = new { deleteResponseId = id };
                DeleteResponseType deleteResult = await _itemCategoryService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVariables);

                if (!deleteResult.Success)
                {
                    ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"No se pudo eliminar el registro.\n\n{deleteResult.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error);
                    return;
                }

                SelectedItem = null;
                CurrentPanelEditor = null;

                await Context.EventAggregator.PublishOnUIThreadAsync(new ItemCategoryDeleteMessage { DeletedItemCategory = deleteResult });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(
                    exGraphQL.Content?.ToString() ?? "");
                if (graphQLError != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"{GetType().Name}.{nameof(DeleteItemCategoryAsync)} \r\n{graphQLError.Errors[0].Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error));
                }
                else { throw; }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(DeleteItemCategoryAsync)} \r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteItemSubCategoryAsync()
        {
            if (SelectedItem is not ItemSubCategoryDTO itemSubCategory) return;

            try
            {
                IsBusy = true;
                Refresh();

                int id = itemSubCategory.Id;
                string canDeleteQuery = GetCanDeleteQuery("canDeleteItemSubCategory");
                object canDeleteVariables = new { canDeleteResponseId = id };
                var validation = await _itemSubCategoryService.CanDeleteAsync(canDeleteQuery, canDeleteVariables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(
                        title: "Confirme...",
                        text: $"¿Confirma que desea eliminar la subcategoría {itemSubCategory.Name}?",
                        messageBoxButtons: MessageBoxButton.YesNo,
                        image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"El registro no puede ser eliminado\n\n{validation.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error));
                    return;
                }

                IsBusy = true;
                Refresh();

                string deleteQuery = GetDeleteMutationQuery("deleteItemSubCategory");
                object deleteVariables = new { deleteResponseId = id };
                DeleteResponseType deleteResult = await _itemSubCategoryService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVariables);

                if (!deleteResult.Success)
                {
                    ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"No se pudo eliminar el registro.\n\n{deleteResult.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error);
                    return;
                }

                SelectedItem = null;
                CurrentPanelEditor = null;

                await Context.EventAggregator.PublishOnUIThreadAsync(new ItemSubCategoryDeleteMessage { DeletedItemSubCategory = deleteResult });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(
                    exGraphQL.Content?.ToString() ?? "");
                if (graphQLError != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"{GetType().Name}.{nameof(DeleteItemSubCategoryAsync)} \r\n{graphQLError.Errors[0].Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error));
                }
                else { throw; }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(DeleteItemSubCategoryAsync)} \r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteItemAsync()
        {
            if (SelectedItem is not ItemDTO itemDTO) return;

            try
            {
                IsBusy = true;
                Refresh();

                int id = itemDTO.Id;
                string canDeleteQuery = GetCanDeleteQuery("canDeleteItem");
                object canDeleteVariables = new { canDeleteResponseId = id };
                var validation = await _itemService.CanDeleteAsync(canDeleteQuery, canDeleteVariables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(
                        title: "Confirme...",
                        text: $"¿Confirma que desea eliminar el registro {itemDTO.Name}?",
                        messageBoxButtons: MessageBoxButton.YesNo,
                        image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"El registro no puede ser eliminado\n\n{validation.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error));
                    return;
                }

                IsBusy = true;
                Refresh();

                // Delete images from S3 and local repository before deleting the item
                if (ItemEditor.Images != null && ItemEditor.Images.Count > 0)
                {
                    foreach (ImageByItemDTO image in ItemEditor.Images)
                    {
                        string imagesLocalPath = Path.Combine(LocalImageCachePath, image.S3FileName);
                        if (Path.Exists(imagesLocalPath)) File.Delete(imagesLocalPath);
                        await S3Helper.DeleteFileAsync(image.S3FileName);
                    }
                }

                string deleteQuery = GetDeleteMutationQuery("deleteItem");
                object deleteVariables = new { deleteResponseId = id };
                DeleteResponseType deleteResult = await _itemService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVariables);

                if (!deleteResult.Success)
                {
                    ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"No se pudo eliminar el registro.\n\n{deleteResult.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error);
                    return;
                }

                SelectedItem = null;
                CurrentPanelEditor = null;

                await Context.EventAggregator.PublishOnUIThreadAsync(new ItemDeleteMessage { DeletedItem = deleteResult });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(
                    exGraphQL.Content?.ToString() ?? "");
                if (graphQLError != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"{GetType().Name}.{nameof(DeleteItemAsync)} \r\n{graphQLError.Errors[0].Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error));
                }
                else { throw; }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(DeleteItemAsync)} \r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
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
                IsBusy = true;
                Refresh();

                int id = itemDTO.Id;
                string canDeleteQuery = GetCanDeleteQuery("canDiscontinueItem");
                object canDeleteVariables = new { canDeleteResponseId = id };
                var validation = await _itemService.CanDeleteAsync(canDeleteQuery, canDeleteVariables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(
                        title: "Confirme...",
                        text: $"¿Confirma que desea descontinuar el registro {itemDTO.Name}?",
                        messageBoxButtons: MessageBoxButton.YesNo,
                        image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"El registro no puede ser descontinuado\n\n{validation.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error));
                    return;
                }

                IsBusy = true;
                Refresh();

                string deleteQuery = GetDeleteMutationQuery("deleteItem");
                object deleteVariables = new { deleteResponseId = id };
                DeleteResponseType deleteResult = await _itemService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVariables);

                if (!deleteResult.Success)
                {
                    ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"No se pudo descontinuar el registro.\n\n{deleteResult.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error);
                    return;
                }

                SelectedItem = null;
                CurrentPanelEditor = null;

                await Context.EventAggregator.PublishOnUIThreadAsync(new ItemDeleteMessage { DeletedItem = deleteResult });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(
                    exGraphQL.Content?.ToString() ?? "");
                if (graphQLError != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"{GetType().Name}.{nameof(DiscontinueItemAsync)} \r\n{graphQLError.Errors[0].Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error));
                }
                else { throw; }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(DiscontinueItemAsync)} \r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
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
            string query = GetSearchProductsQuery();

            string fieldHeader1 = "Código";
            string fieldHeader2 = "Nombre";
            string fieldHeader3 = "Referencia";
            string fieldData1 = "Code";
            string fieldData2 = "Name";
            string fieldData3 = "Reference";

            var viewModel = new SearchWithThreeColumnsGridViewModel<ItemGraphQLModel>(
                query, fieldHeader1, fieldHeader2, fieldHeader3, fieldData1, fieldData2, fieldData3,
                null, SearchWithThreeColumnsGridMessageToken.SearchProduct, _dialogService);

            await _dialogService.ShowDialogAsync(viewModel, "Búsqueda de productos");
        }

        private static string GetSearchProductsQuery()
        {
            var fields = FieldSpec<PageType<ItemGraphQLModel>>
                .Create()
                .Field(f => f.PageNumber)
                .Field(f => f.PageSize)
                .Field(f => f.TotalPages)
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Code)
                    .Field(e => e.Reference)
                    .Select(e => e.SubCategory, sub => sub
                        .Field(s => s.Id)
                        .Select(s => s.ItemCategory, ic => ic
                            .Field(c => c.Id)
                            .Select(c => c.ItemType, it => it
                                .Field(t => t.Id)))))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("filters", "ItemFilters"),
                new("pagination", "Pagination")
            };
            var fragment = new GraphQLQueryFragment("itemsPage", parameters, fields, "PageResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        public async void OnFindProductMessage(ReturnedDataFromModalWithThreeColumnsGridViewMessage<ItemGraphQLModel> message)
        {
            IsBusy = true;
            await OnFindProductMessageAsync(message);
            IsBusy = false;
        }

        public async Task OnFindProductMessageAsync(ReturnedDataFromModalWithThreeColumnsGridViewMessage<ItemGraphQLModel> message)
        {
            if (message.ReturnedData is null) return;
            ItemDTO itemDTO = Context.AutoMapper.Map<ItemDTO>(message.ReturnedData);
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemTypes.FirstOrDefault(x => x.Id == itemDTO.SubCategory.ItemCategory.ItemType.Id);
            if (itemTypeDTO is null) return;
            if (!itemTypeDTO.IsExpanded && itemTypeDTO.ItemsCategories.Count > 0 && itemTypeDTO.ItemsCategories[0].IsDummyChild)
            {
                await LoadItemsCategoriesAsync(itemTypeDTO);
                itemTypeDTO.IsExpanded = true;
            }
            if (!itemTypeDTO.IsExpanded) itemTypeDTO.IsExpanded = true;
            ItemCategoryDTO? itemCategoryDTO = itemTypeDTO.ItemsCategories.FirstOrDefault(x => x.Id == itemDTO.SubCategory.ItemCategory.Id);
            if (itemCategoryDTO is null) return;
            if (!itemCategoryDTO.IsExpanded && itemCategoryDTO.SubCategories.Count > 0 && itemCategoryDTO.SubCategories[0].IsDummyChild)
            {
                await LoadItemsSubCategoriesAsync(itemCategoryDTO);
                itemCategoryDTO.IsExpanded = true;
            }
            if (!itemCategoryDTO.IsExpanded) itemCategoryDTO.IsExpanded = true;
            ItemSubCategoryDTO? itemSubCategoryDTO = itemCategoryDTO.SubCategories.FirstOrDefault(x => x.Id == itemDTO.SubCategory.Id);
            if (itemSubCategoryDTO is null) return;
            if (!itemSubCategoryDTO.IsExpanded && itemSubCategoryDTO.Items.Count > 0 && itemSubCategoryDTO.Items[0].IsDummyChild)
            {
                await LoadItemsAsync(itemSubCategoryDTO);
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

        public async Task LoadS3ConfigAsync()
        {
            try
            {
                var fields = FieldSpec<S3StorageLocationGraphQLModel>
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

                var parameter = new GraphQLQueryParameter("key", "String!");
                var fragment = new GraphQLQueryFragment("s3StorageLocationByKey", [parameter], fields, "SingleItemResponse");
                var query = new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.QUERY);
                var location = await _s3LocationService.GetSingleItemAsync(query, new { singleItemResponseKey = "product_images" });

                S3Helper = Common.Helpers.S3Helper.FromStorageLocation(location);

                string appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                LocalImageCachePath = Path.Combine(appDir, "cache", location.Bucket, location.Directory);
                System.IO.Directory.CreateDirectory(LocalImageCachePath);
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

        private static string GetCanDeleteQuery(string fragmentName)
        {
            var fields = FieldSpec<CanDeleteType>.Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment(fragmentName, [parameter], fields, "CanDeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        private static string GetDeleteMutationQuery(string fragmentName)
        {
            var fields = FieldSpec<DeleteResponseType>.Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment(fragmentName, [parameter], fields, "DeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
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

        private static string GetLoadItemCategoriesQuery()
        {
            var fields = FieldSpec<PageType<ItemCategoryGraphQLModel>>
                .Create()
                .Field(f => f.PageNumber)
                .Field(f => f.PageSize)
                .Field(f => f.TotalPages)
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Select(e => e.ItemType, it => it
                        .Field(t => t.Id)))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("pagination", "Pagination"),
                new("filters", "ItemCategoryFilters")
            };
            var fragment = new GraphQLQueryFragment("itemCategoriesPage", parameters, fields, "PageResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        public async Task LoadItemsCategoriesAsync(ItemTypeDTO itemType)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    itemType.ItemsCategories.Remove(itemType.ItemsCategories[0]);
                });

                string query = GetLoadItemCategoriesQuery();

                dynamic variables = new ExpandoObject();
                variables.PageResponsePagination = new ExpandoObject();
                variables.PageResponsePagination.PageSize = -1;
                variables.PageResponseFilters = new ExpandoObject();
                variables.PageResponseFilters.ItemTypeId = itemType.Id;

                var result = await _itemCategoryService.GetPageAsync(query, variables);
                ItemsCategories = Context.AutoMapper.Map<ObservableCollection<ItemCategoryDTO>>(result.Entries);

                if (ItemsCategories.Count == 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        itemType.IsExpanded = false;
                    });
                    _notificationService.ShowInfo("Este tipo de item no tiene categorías registradas");
                }
                else
                {
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
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                if (graphQLError != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(LoadItemsCategoriesAsync)} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else { throw; }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{GetType().Name}.{nameof(LoadItemsCategoriesAsync)} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        private static string GetLoadItemSubCategoriesQuery()
        {
            var fields = FieldSpec<PageType<ItemSubCategoryGraphQLModel>>
                .Create()
                .Field(f => f.PageNumber)
                .Field(f => f.PageSize)
                .Field(f => f.TotalPages)
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Select(e => e.ItemCategory, ic => ic
                        .Field(c => c.Id)
                        .Select(c => c.ItemType, it => it
                            .Field(t => t.Id))))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("pagination", "Pagination"),
                new("filters", "ItemSubCategoryFilters")
            };
            var fragment = new GraphQLQueryFragment("itemSubCategoriesPage", parameters, fields, "PageResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        public async Task LoadItemsSubCategoriesAsync(ItemCategoryDTO itemCategory)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    itemCategory.SubCategories.Remove(itemCategory.SubCategories[0]);
                });

                string query = GetLoadItemSubCategoriesQuery();
                dynamic variables = new ExpandoObject();
                variables.PageResponsePagination = new ExpandoObject();
                variables.PageResponsePagination.PageSize = -1;
                variables.PageResponseFilters = new ExpandoObject();
                variables.PageResponseFilters.ItemCategoryId = itemCategory.Id;

                var result = await _itemSubCategoryService.GetPageAsync(query, variables);
                ItemsSubCategories = Context.AutoMapper.Map<ObservableCollection<ItemSubCategoryDTO>>(result.Entries);

                if (ItemsSubCategories.Count == 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        itemCategory.IsExpanded = false;
                    });
                    _notificationService.ShowInfo("Esta categoría no tiene subcategorías registradas");
                }
                else
                {
                    foreach (ItemSubCategoryDTO itemSubCategory in ItemsSubCategories)
                    {
                        itemSubCategory.Context = this;
                        itemSubCategory.Items.Add(new ItemDTO() { IsDummyChild = true, EanCodes = [], Name = "Dummy" });
                        itemCategory.SubCategories.Add(itemSubCategory);
                    }
                }
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(
                    exGraphQL.Content?.ToString() ?? "");
                if (graphQLError != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"{GetType().Name}.{nameof(LoadItemsSubCategoriesAsync)} \r\n{graphQLError.Errors[0].Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error));
                }
                else { throw; }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(LoadItemsSubCategoriesAsync)} \r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
            }
        }

        private static string GetLoadItemsQuery()
        {
            var fields = FieldSpec<PageType<ItemGraphQLModel>>
                .Create()
                .Field(f => f.PageNumber)
                .Field(f => f.PageSize)
                .Field(f => f.TotalPages)
                .Field(f => f.TotalEntries)
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
                    .Field(e => e.EanCodes)
                    .Select(e => e.MeasurementUnit, mu => mu
                        .Field(m => m.Id))
                    .Select(e => e.Brand, b => b
                        .Field(br => br.Id))
                    .Select(e => e.AccountingGroup, ag => ag
                        .Field(a => a.Id))
                    .Select(e => e.SizeCategory, sc => sc
                        .Field(s => s.Id))
                    .SelectList(e => e.Components, comp => comp
                        .Field(c => c.Quantity)
                        .Select(c => c.Component, ci => ci
                            .Field(i => i.Id)
                            .Field(i => i.Name)
                            .Field(i => i.Reference)
                            .Field(i => i.Code)
                            .Select(i => i.MeasurementUnit, mu => mu
                                .Field(m => m.Id)
                                .Field(m => m.Name))))
                    .SelectList(e => e.Images, img => img
                        .Field(i => i.DisplayOrder)
                        .Field(i => i.S3Bucket)
                        .Field(i => i.S3BucketDirectory)
                        .Field(i => i.S3FileName)
                        .Select(i => i.Item, item => item
                            .Field(it => it.Id)))
                    .Select(e => e.SubCategory, sub => sub
                        .Field(s => s.Id)
                        .Select(s => s.ItemCategory, ic => ic
                            .Field(c => c.Id)
                            .Select(c => c.ItemType, it => it
                                .Field(t => t.Id)
                                .Field(t => t.StockControl)))))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("pagination", "Pagination"),
                new("filters", "ItemFilters")
            };
            var fragment = new GraphQLQueryFragment("itemsPage", parameters, fields, "PageResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        public async Task LoadItemsAsync(ItemSubCategoryDTO itemSubCategory)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    itemSubCategory.Items.Remove(itemSubCategory.Items[0]);
                });

                string query = GetLoadItemsQuery();
                dynamic variables = new ExpandoObject();
                variables.PageResponsePagination = new ExpandoObject();
                variables.PageResponsePagination.PageSize = -1;
                variables.PageResponseFilters = new ExpandoObject();
                variables.PageResponseFilters.SubCategoryId = itemSubCategory.Id;

                var result = await _itemService.GetPageAsync(query, variables);
                Items = Context.AutoMapper.Map<ObservableCollection<ItemDTO>>(result.Entries);

                if (Items.Count == 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        itemSubCategory.IsExpanded = false;
                    });
                    _notificationService.ShowInfo("Esta subcategoría no tiene productos registrados");
                }
                else
                {
                    foreach (ItemDTO item in Items)
                    {
                        item.Context = this;
                        itemSubCategory.Items.Add(item);
                    }
                }
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(
                    exGraphQL.Content?.ToString() ?? "");
                if (graphQLError != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"{GetType().Name}.{nameof(LoadItemsAsync)} \r\n{graphQLError.Errors[0].Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error));
                }
                else { throw; }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(LoadItemsAsync)} \r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
            }
        }

        #endregion

        #region Message Handlers

        public Task HandleAsync(ItemTypeCreateMessage message, CancellationToken cancellationToken)
        {
            ItemTypeDTO itemTypeDTO = Context.AutoMapper.Map<ItemTypeDTO>(message.CreatedItemType.Entity);
            itemTypeDTO.Context = this;
            itemTypeDTO.ItemsCategories.Add(new ItemCategoryDTO() { IsDummyChild = true, Name = "Dummy", SubCategories = [] });
            if (SelectedCatalog.Id != itemTypeDTO.Catalog.Id) return Task.CompletedTask;
            SelectedCatalog.ItemTypes.Add(itemTypeDTO);
            SelectedItem = itemTypeDTO;
            NotifyOfPropertyChange(nameof(DeleteCatalogButtonEnable));
            _notificationService.ShowSuccess(message.CreatedItemType.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemTypeDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemTypes.FirstOrDefault(x => x.Id == message.DeletedItemType.DeletedId);
                if (itemTypeDTO is null) return;
                SelectedCatalog.ItemTypes.Remove(itemTypeDTO);
                SelectedItem = null;
                NotifyOfPropertyChange(nameof(DeleteCatalogButtonEnable));
            });
            _notificationService.ShowSuccess(message.DeletedItemType.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemTypeUpdateMessage message, CancellationToken cancellationToken)
        {
            ItemTypeDTO itemTypeDTO = Context.AutoMapper.Map<ItemTypeDTO>(message.UpdatedItemType.Entity);
            ItemTypeDTO? itemToUpdate = SelectedCatalog.ItemTypes.FirstOrDefault(x => x.Id == message.UpdatedItemType.Entity.Id);
            if (itemToUpdate == null) return Task.CompletedTask;
            itemToUpdate.Id = itemTypeDTO.Id;
            itemToUpdate.Name = itemTypeDTO.Name;
            itemToUpdate.PrefixChar = itemTypeDTO.PrefixChar;
            itemToUpdate.StockControl = itemTypeDTO.StockControl;
            itemToUpdate.DefaultMeasurementUnit = itemTypeDTO.DefaultMeasurementUnit;
            itemToUpdate.DefaultAccountingGroup = itemTypeDTO.DefaultAccountingGroup;
            _notificationService.ShowSuccess(message.UpdatedItemType.Message);
            return Task.CompletedTask;
        }

        public async Task HandleAsync(ItemCategoryCreateMessage message, CancellationToken cancellationToken)
        {
            ItemCategoryDTO itemCategoryDTO = Context.AutoMapper.Map<ItemCategoryDTO>(message.CreatedItemCategory.Entity);
            itemCategoryDTO.Context = this;
            itemCategoryDTO.SubCategories.Add(new ItemSubCategoryDTO() { IsDummyChild = true, Name = "Dummy", Items = [] });
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemTypes.FirstOrDefault(x => x.Id == itemCategoryDTO.ItemType.Id);
            if (itemTypeDTO is null) return;
            if (!itemTypeDTO.IsExpanded && itemTypeDTO.ItemsCategories.Count > 0 && itemTypeDTO.ItemsCategories[0].IsDummyChild)
            {
                await LoadItemsCategoriesAsync(itemTypeDTO);
                itemTypeDTO.IsExpanded = true;
                ItemCategoryDTO? itemCategory = itemTypeDTO.ItemsCategories.FirstOrDefault(x => x.Id == itemCategoryDTO.Id);
                if (itemCategory is null) return;
                SelectedItem = itemCategory;
                _notificationService.ShowSuccess(message.CreatedItemCategory.Message);
                return;
            }
            if (itemTypeDTO.IsExpanded == false)
            {
                itemTypeDTO.IsExpanded = true;
                itemTypeDTO.ItemsCategories.Add(itemCategoryDTO);
                SelectedItem = itemCategoryDTO;
                _notificationService.ShowSuccess(message.CreatedItemCategory.Message);
                return;
            }
            itemTypeDTO.ItemsCategories.Add(itemCategoryDTO);
            SelectedItem = itemCategoryDTO;
            _notificationService.ShowSuccess(message.CreatedItemCategory.Message);
        }

        public Task HandleAsync(ItemCategoryDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var itemTypeDTO in SelectedCatalog.ItemTypes)
                {
                    ItemCategoryDTO? categoryToRemove = itemTypeDTO.ItemsCategories.FirstOrDefault(x => x.Id == message.DeletedItemCategory.DeletedId);
                    if (categoryToRemove != null)
                    {
                        itemTypeDTO.ItemsCategories.Remove(categoryToRemove);
                        break;
                    }
                }
                SelectedItem = null;
            });
            _notificationService.ShowSuccess(message.DeletedItemCategory.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemCategoryUpdateMessage message, CancellationToken cancellationToken)
        {
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemTypes.FirstOrDefault(x => x.Id == message.UpdatedItemCategory.Entity.ItemType.Id);
            if (itemTypeDTO is null) return Task.CompletedTask;
            ItemCategoryDTO? itemCategoryDTOToUpdate = itemTypeDTO.ItemsCategories.FirstOrDefault(x => x.Id == message.UpdatedItemCategory.Entity.Id);
            if (itemCategoryDTOToUpdate is null) return Task.CompletedTask;
            itemCategoryDTOToUpdate.Id = message.UpdatedItemCategory.Entity.Id;
            itemCategoryDTOToUpdate.Name = message.UpdatedItemCategory.Entity.Name;
            _notificationService.ShowSuccess(message.UpdatedItemCategory.Message);
            return Task.CompletedTask;
        }

        public async Task HandleAsync(ItemSubCategoryCreateMessage message, CancellationToken cancellationToken)
        {
            ItemSubCategoryDTO itemSubCategoryDTO = Context.AutoMapper.Map<ItemSubCategoryDTO>(message.CreatedItemSubCategory.Entity);
            itemSubCategoryDTO.Context = this;
            itemSubCategoryDTO.Items.Add(new ItemDTO() { IsDummyChild = true, Name = "Dummy" });
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemTypes.FirstOrDefault(x => x.Id == itemSubCategoryDTO.ItemCategory.ItemType.Id);
            if (itemTypeDTO is null) return;
            ItemCategoryDTO? itemCategoryDTO = itemTypeDTO.ItemsCategories.FirstOrDefault(x => x.Id == itemSubCategoryDTO.ItemCategory.Id);
            if (itemCategoryDTO is null) return;
            if (!itemCategoryDTO.IsExpanded && itemCategoryDTO.SubCategories.Count > 0 && itemCategoryDTO.SubCategories[0].IsDummyChild)
            {
                await LoadItemsSubCategoriesAsync(itemCategoryDTO);
                itemCategoryDTO.IsExpanded = true;
                ItemSubCategoryDTO? itemSubCategory = itemCategoryDTO.SubCategories.FirstOrDefault(x => x.Id == itemSubCategoryDTO.Id);
                if (itemSubCategory is null) return;
                SelectedItem = itemSubCategory;
                _notificationService.ShowSuccess(message.CreatedItemSubCategory.Message);
                return;
            }
            if (!itemCategoryDTO.IsExpanded)
            {
                itemCategoryDTO.IsExpanded = true;
                itemCategoryDTO.SubCategories.Add(itemSubCategoryDTO);
                SelectedItem = itemSubCategoryDTO;
                _notificationService.ShowSuccess(message.CreatedItemSubCategory.Message);
                return;
            }
            itemCategoryDTO.SubCategories.Add(itemSubCategoryDTO);
            SelectedItem = itemSubCategoryDTO;
            _notificationService.ShowSuccess(message.CreatedItemSubCategory.Message);
        }

        public Task HandleAsync(ItemSubCategoryDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var itemTypeDTO in SelectedCatalog.ItemTypes)
                {
                    foreach (var itemCategoryDTO in itemTypeDTO.ItemsCategories)
                    {
                        ItemSubCategoryDTO? subCategoryToRemove = itemCategoryDTO.SubCategories.FirstOrDefault(x => x.Id == message.DeletedItemSubCategory.DeletedId);
                        if (subCategoryToRemove != null)
                        {
                            itemCategoryDTO.SubCategories.Remove(subCategoryToRemove);
                            SelectedItem = null;
                            return;
                        }
                    }
                }
            });
            _notificationService.ShowSuccess(message.DeletedItemSubCategory.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemSubCategoryUpdateMessage message, CancellationToken cancellationToken)
        {
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemTypes.FirstOrDefault(x => x.Id == message.UpdatedItemSubCategory.Entity.ItemCategory.ItemType.Id);
            if (itemTypeDTO is null) return Task.CompletedTask;
            ItemCategoryDTO? itemCategoryDTO = itemTypeDTO.ItemsCategories.FirstOrDefault(x => x.Id == message.UpdatedItemSubCategory.Entity.ItemCategory.Id);
            if (itemCategoryDTO is null) return Task.CompletedTask;
            ItemSubCategoryDTO? itemSubCategoryDTOToUpdate = itemCategoryDTO.SubCategories.FirstOrDefault(x => x.Id == message.UpdatedItemSubCategory.Entity.Id);
            if (itemSubCategoryDTOToUpdate is null) return Task.CompletedTask;
            itemSubCategoryDTOToUpdate.Id = message.UpdatedItemSubCategory.Entity.Id;
            itemSubCategoryDTOToUpdate.Name = message.UpdatedItemSubCategory.Entity.Name;
            _notificationService.ShowSuccess(message.UpdatedItemSubCategory.Message);
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
                CatalogDTO? catalogToRemove = Catalogs.FirstOrDefault(x => x.Id == message.DeletedCatalog.DeletedId);
                if (catalogToRemove != null)
                    Catalogs.Remove(catalogToRemove);

                SelectedCatalog = Catalogs.Count > 0 ? Catalogs.First() : null;
            });
            _notificationService.ShowSuccess(message.DeletedCatalog.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var itemTypeDTO in SelectedCatalog.ItemTypes)
                {
                    foreach (var itemCategoryDTO in itemTypeDTO.ItemsCategories)
                    {
                        foreach (var itemSubCategoryDTO in itemCategoryDTO.SubCategories)
                        {
                            ItemDTO? itemToRemove = itemSubCategoryDTO.Items.FirstOrDefault(x => x.Id == message.DeletedItem.DeletedId);
                            if (itemToRemove != null)
                            {
                                itemSubCategoryDTO.Items.Remove(itemToRemove);
                                SelectedItem = null;
                                return;
                            }
                        }
                    }
                }
            });
            _notificationService.ShowSuccess(message.DeletedItem.Message);
            return Task.CompletedTask;
        }

        public async Task HandleAsync(ItemCreateMessage message, CancellationToken cancellationToken)
        {
            IsNewRecord = false;
            ItemDTO itemDTO = Context.AutoMapper.Map<ItemDTO>(message.CreatedItem.Entity);
            itemDTO.Context = this;
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemTypes.FirstOrDefault(x => x.Id == itemDTO.SubCategory.ItemCategory.ItemType.Id);
            if (itemTypeDTO is null) return;
            ItemCategoryDTO? itemCategoryDTO = itemTypeDTO.ItemsCategories.FirstOrDefault(x => x.Id == itemDTO.SubCategory.ItemCategory.Id);
            if (itemCategoryDTO is null) return;
            ItemSubCategoryDTO? itemSubCategoryDTO = itemCategoryDTO.SubCategories.FirstOrDefault(x => x.Id == itemDTO.SubCategory.Id);
            if (itemSubCategoryDTO is null) return;
            if (!itemSubCategoryDTO.IsExpanded && itemSubCategoryDTO.Items.Count > 0 && itemSubCategoryDTO.Items[0].IsDummyChild)
            {
                await LoadItemsAsync(itemSubCategoryDTO);
                itemSubCategoryDTO.IsExpanded = true;
                ItemDTO? item = itemSubCategoryDTO.Items.FirstOrDefault(x => x.Id == itemDTO.Id);
                if (item is null) return;
                SelectedItem = item;
                _notificationService.ShowSuccess(message.CreatedItem.Message);
                return;
            }
            if (!itemSubCategoryDTO.IsExpanded)
            {
                itemSubCategoryDTO.IsExpanded = true;
                itemSubCategoryDTO.Items.Add(itemDTO);
                SelectedItem = itemDTO;
                _notificationService.ShowSuccess(message.CreatedItem.Message);
                return;
            }
            itemSubCategoryDTO.Items.Add(itemDTO);
            SelectedItem = itemDTO;
            _notificationService.ShowSuccess(message.CreatedItem.Message);
        }

        public Task HandleAsync(ItemUpdateMessage message, CancellationToken cancellationToken)
        {
            ItemDTO item = Context.AutoMapper.Map<ItemDTO>(message.UpdatedItem.Entity);
            item.Context = this;
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemTypes.FirstOrDefault(x => x.Id == message.UpdatedItem.Entity.SubCategory.ItemCategory.ItemType.Id);
            if (itemTypeDTO is null) return Task.CompletedTask;
            ItemCategoryDTO? itemCategoryDTO = itemTypeDTO.ItemsCategories.FirstOrDefault(x => x.Id == message.UpdatedItem.Entity.SubCategory.ItemCategory.Id);
            if (itemCategoryDTO is null) return Task.CompletedTask;
            ItemSubCategoryDTO? itemSubCategoryDTO = itemCategoryDTO.SubCategories.FirstOrDefault(x => x.Id == message.UpdatedItem.Entity.SubCategory.Id);
            if (itemSubCategoryDTO is null) return Task.CompletedTask;
            ItemDTO? itemDTOToUpdate = itemSubCategoryDTO.Items.FirstOrDefault(x => x.Id == message.UpdatedItem.Entity.Id);
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
            itemDTOToUpdate.Images = new ObservableCollection<ImageByItemDTO>(item.Images.OrderBy(x => x.DisplayOrder));
            _notificationService.ShowSuccess(message.UpdatedItem.Message);
            return Task.CompletedTask;
        }

        #endregion
    }
}
