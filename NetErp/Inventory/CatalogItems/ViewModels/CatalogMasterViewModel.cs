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
using System.Linq.Expressions;
using Microsoft.Win32;
using System.IO;
using Xceed.Wpf.Toolkit.Primitives;
using Amazon;
using Dictionaries;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DevExpress.DocumentServices.ServiceModel.DataContracts;

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

        #region "Global"

        #region "Services"
        public readonly IGenericDataAccess<CatalogGraphQLModel> CatalogService = IoC.Get<IGenericDataAccess<CatalogGraphQLModel>>();

        public readonly IGenericDataAccess<ItemTypeGraphQLModel> ItemTypeService = IoC.Get<IGenericDataAccess<ItemTypeGraphQLModel>>();

        public readonly IGenericDataAccess<ItemCategoryGraphQLModel> ItemCategoryService = IoC.Get<IGenericDataAccess<ItemCategoryGraphQLModel>>();

        public readonly IGenericDataAccess<ItemSubCategoryGraphQLModel> ItemSubCategoryService = IoC.Get<IGenericDataAccess<ItemSubCategoryGraphQLModel>>();

        public readonly IGenericDataAccess<ItemGraphQLModel> ItemService = IoC.Get<IGenericDataAccess<ItemGraphQLModel>>();

        public readonly IGenericDataAccess<MeasurementUnitGraphQLModel> MeasurementUnitService = IoC.Get<IGenericDataAccess<MeasurementUnitGraphQLModel>>();

        public readonly IGenericDataAccess<AwsS3ConfigGraphQLModel> AwsS3Service = IoC.Get<IGenericDataAccess<AwsS3ConfigGraphQLModel>>();

        Helpers.IDialogService _dialogService = IoC.Get<Helpers.IDialogService>();
        private readonly Helpers.Services.INotificationService _notificationService = IoC.Get<Helpers.Services.INotificationService>();
        #endregion

        #region "Properties"
        //public SearchItemModalViewModel<ItemDTO, ItemGraphQLModel> SearchItemModalViewModel { get; set; }
        private bool _isNewRecord = false;

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

        public AwsS3ConfigGraphQLModel AwsS3Config { get; set; }

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
                    NotifyOfPropertyChange(nameof(SelectedCatalogIsEnable));
                    NotifyOfPropertyChange(nameof(MainRibbonPageIsEnable));
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

        #endregion

        #region "Methods"
        public BitmapImage ConvertBitMapImage(string imageFilePath)
        {
            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(imageFilePath, UriKind.Absolute);
            bitmap.EndInit();
            return bitmap;
        }

        //TODO S3 master pending and variables declaration
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
                }
                ";
                //Código para posible cambio
                AwsS3Config = await AwsS3Service.FindById(query, new { });
                S3Helper.Initialize("qtsattachments".ToLower(), "berdic/products_images".ToLower(), AwsS3Config.AccessKey, AwsS3Config.SecretKey, GlobalDictionaries.AwsSesRegionDictionary[AwsS3Config.Region]);
                // Get the path of the executable


            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task OnStartUp()
        {
            IsBusy = true;
            await InitializeComboBoxes();
            await LoadAwsS3Credentials();
            await LoadCatalogs();
            IsBusy = false;
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            if (Context.EnableOnActivateAsync is false) return;
            await base.OnActivateAsync(cancellationToken);
            await OnStartUp();
        }

        void SetFocus(Expression<Func<object>> propertyExpression)
        {
            string controlName = propertyExpression.GetMemberInfo().Name;
            NameIsFocused = false;

            NameIsFocused = controlName == nameof(Name);
        }
        #endregion

        #region "Constructor"

        public CatalogMasterViewModel(CatalogViewModel context)
        {
            Messenger.Default.Register<ReturnedItemFromModalViewMessage>(this, MessageToken.RelatedProduct, false, OnFindRelatedProductMessage);
            Messenger.Default.Register<ReturnedItemFromModalViewMessage>(this, MessageToken.SearchProduct, false, OnFindProductMessage);
            Context = context;
            _errors = new Dictionary<string, List<string>>();
            Context.EventAggregator.SubscribeOnUIThread(this);

        }
        #endregion

        #region "Errors"


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
        #endregion

        #region "Messages"

        public Task HandleAsync(ItemTypeCreateMessage message, CancellationToken cancellationToken)
        {
            ItemTypeDTO itemTypeDTO = Context.AutoMapper.Map<ItemTypeDTO>(message.CreatedItemType);
            itemTypeDTO.ItemsCategories.Add(new ItemCategoryDTO() { IsDummyChild = true, Name = "Fucking Dummy", SubCategories = [] });
            if (SelectedCatalog.Id != itemTypeDTO.Catalog.Id) return Task.CompletedTask;
            SelectedCatalog.ItemsTypes.Add(itemTypeDTO);
            SelectedItem = itemTypeDTO;
            _notificationService.ShowSuccess("Tipo de item creado correctamente");
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
            _notificationService.ShowSuccess("Tipo de item eliminado correctamente");
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
            _notificationService.ShowSuccess("Tipo de item actualizado correctamente");
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
                _notificationService.ShowSuccess("Categoría de item creada correctamente");
                return;
            }
            //si el nodo no está expandido, pero ya fueron cargados sus hijos
            if (itemTypeDTO.IsExpanded == false)
            {
                itemTypeDTO.IsExpanded = true;
                itemTypeDTO.ItemsCategories.Add(itemCategoryDTO);
                SelectedItem = itemCategoryDTO;
                _notificationService.ShowSuccess("Categoría de item creada correctamente");
                return;
            }
            //si el nodo está expandido
            itemTypeDTO.ItemsCategories.Add(itemCategoryDTO);
            SelectedItem = itemCategoryDTO;
            _notificationService.ShowSuccess("Categoría de item creada correctamente");
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
            _notificationService.ShowSuccess("Categoría de item eliminada correctamente");
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
            _notificationService.ShowSuccess("Categoría de item actualizada correctamente");
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
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemsTypes.Where(x => x.Id == message.UpdatedItemSubCategory.ItemCategory.ItemType.Id).FirstOrDefault();
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
            Catalogs.Add(Context.AutoMapper.Map<CatalogDTO>(message.CreatedCatalog));
            SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == message.CreatedCatalog.Id);
            _notificationService.ShowSuccess("Catálogo creado correctamente");
            return Task.CompletedTask;
        }

        public Task HandleAsync(CatalogUpdateMessage message, CancellationToken cancellationToken)
        {
            CatalogDTO catalogDTO = Context.AutoMapper.Map<CatalogDTO>(message.UpdatedCatalog);
            CatalogDTO? catalogToUpdate = Catalogs.FirstOrDefault(x => x.Id == catalogDTO.Id);
            if (catalogToUpdate is null) return Task.CompletedTask;
            catalogToUpdate.Id = catalogDTO.Id;
            catalogToUpdate.Name = catalogDTO.Name;
            _notificationService.ShowSuccess("Catálogo actualizado correctamente");
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
            _notificationService.ShowSuccess("Item eliminado correctamente");
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
            itemDTOToUpdate.RelatedProducts = new ObservableCollection<ItemDetailDTO>(item.RelatedProducts);
            itemDTOToUpdate.Images = new ObservableCollection<ItemImageDTO>(item.Images);
            _notificationService.ShowSuccess("Item actualizado correctamente");
            return Task.CompletedTask;
        }
        #endregion

        #endregion

        #region "TabControls"

        #region "Properties"

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

        private ObservableCollection<ItemDetailDTO> _relatedProducts;

        public ObservableCollection<ItemDetailDTO> RelatedProducts
        {
            get { return _relatedProducts; }
            set
            {
                if (_relatedProducts != value)
                {
                    _relatedProducts = value;
                    NotifyOfPropertyChange(nameof(RelatedProducts));
                }
            }
        }

        private ObservableCollection<ItemImageDTO> _itemImages;

        public ObservableCollection<ItemImageDTO> ItemImages
        {
            get { return _itemImages; }
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

        private ItemDetailDTO _selectedRelatedProduct;

        public ItemDetailDTO SelectedRelatedProduct
        {
            get { return _selectedRelatedProduct; }
            set
            {
                if (_selectedRelatedProduct != value)
                {
                    _selectedRelatedProduct = value;
                    NotifyOfPropertyChange(nameof(SelectedRelatedProduct));
                }
            }
        }

        private ItemDTO _returnedItemFromModal;

        public ItemDTO ReturnedItemFromModal
        {
            get { return _returnedItemFromModal; }
            set
            {
                if (_returnedItemFromModal != value)
                {
                    _returnedItemFromModal = value;
                    NotifyOfPropertyChange(nameof(ReturnedItemFromModal));
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

        private bool _hasRelatedProducts = false;

        public bool HasRelatedProducts
        {
            get { return _hasRelatedProducts; }
            set
            {
                if (_hasRelatedProducts != value)
                {
                    _hasRelatedProducts = value;
                    NotifyOfPropertyChange(nameof(HasRelatedProducts));
                }
            }
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

        private string _relatedProductName = string.Empty;

        public string RelatedProductName
        {
            get { return _relatedProductName; }
            set
            {
                if (_relatedProductName != value)
                {
                    _relatedProductName = value;
                    NotifyOfPropertyChange(nameof(RelatedProductName));
                }
            }
        }

        private string _relatedProductReference = string.Empty;

        public string RelatedProductReference
        {
            get { return _relatedProductReference; }
            set
            {
                if (_relatedProductReference != value)
                {
                    _relatedProductReference = value;
                    NotifyOfPropertyChange(nameof(RelatedProductReference));
                }
            }
        }

        private string _relatedProductCode = string.Empty;

        public string RelatedProductCode
        {
            get { return _relatedProductCode; }
            set
            {
                if (_relatedProductCode != value)
                {
                    _relatedProductCode = value;
                    NotifyOfPropertyChange(nameof(RelatedProductCode));
                }
            }
        }

        private decimal _relatedProductQuantity = 0;

        public decimal RelatedProductQuantity
        {
            get { return _relatedProductQuantity; }
            set
            {
                if (_relatedProductQuantity != value)
                {
                    _relatedProductQuantity = value;
                    NotifyOfPropertyChange(nameof(RelatedProductQuantity));
                }
            }
        }



        private bool _relatedProductQuantityIsEnable = false;

        public bool RelatedProductQuantityIsEnable
        {
            get { return _relatedProductQuantityIsEnable; }
            set
            {
                if (_relatedProductQuantityIsEnable != value)
                {
                    _relatedProductQuantityIsEnable = value;
                    NotifyOfPropertyChange(nameof(RelatedProductQuantityIsEnable));
                }
            }
        }

        private bool _relatedProductAllowFraction;

        public bool RelatedProductAllowFraction
        {
            get { return _relatedProductAllowFraction; }
            set
            {
                if (_relatedProductAllowFraction != value)
                {
                    _relatedProductAllowFraction = value;
                    NotifyOfPropertyChange(nameof(RelatedProductAllowFraction));
                }
            }
        }

        public bool CanSaveItem => IsEditing == true && _errors.Count <= 0;

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

        private bool _nameIsFocused;

        public bool NameIsFocused
        {
            get { return _nameIsFocused; }
            set 
            {
                if (_nameIsFocused != value)
                {
                    _nameIsFocused = value;
                    NotifyOfPropertyChange(nameof(NameIsFocused));
                }
            }
        }

        public bool SelectedCatalogIsEnable
        {
            get
            {
                return !IsEditing;
            }
        }

        public bool MainRibbonPageIsEnable
        {
            get
            {
                return !IsEditing;
            }
        }

        #endregion

        #region "Methods"
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
            HasRelatedProducts = itemTypeDTO.StockControl == false ? true : false;
            RelatedProductName = string.Empty;
            RelatedProductCode = string.Empty;
            RelatedProductReference = string.Empty;
            RelatedProductQuantity = 0;
        }

        public async Task SetItemForEdit(ItemDTO selectedItem)
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
            RelatedProducts = selectedItem.RelatedProducts is null ? [] : new ObservableCollection<ItemDetailDTO>(selectedItem.RelatedProducts.Select(x => (ItemDetailDTO)x.Clone()).ToList());
            ItemImages = selectedItem.Images is null ? [] : new ObservableCollection<ItemImageDTO>(selectedItem.Images.Select(x => (ItemImageDTO)x.Clone()).ToList());
            //search images into local repository, if not exist, download from S3
            if (ItemImages.Count > 0)
            {
                foreach (ItemImageDTO image in ItemImages)
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
            SelectedIndex = 0;
            EanCode = string.Empty;
            if (selectedItem.SubCategory != null) HasRelatedProducts = selectedItem.SubCategory.ItemCategory.ItemType.StockControl == false ? true : false;
            RelatedProductName = string.Empty;
            RelatedProductCode = string.Empty;
            RelatedProductReference = string.Empty;
            RelatedProductQuantity = 0;
            RelatedProductQuantityIsEnable = false;
        }


        public void AddImage(object p)
        {
            OpenFileDialog fileDialog = new();
            fileDialog.Filter = "Image Files (*.jpg; *.jpeg; *.png; *.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
            if (fileDialog.ShowDialog() == true)
            {
                FileInfo fileInfo = new(fileDialog.FileName);
                long fileSizeLimit = 400 * 1024; //400KB Limit
                if (fileInfo.Length > fileSizeLimit)
                {
                    ThemedMessageBox.Show(title: "Archivo demasiado grande", text: "El archivo seleccionado es demasiado grande. Por favor, selecciona un archivo de menos de 400KB", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Warning);
                }
                else
                {
                    string selectedFilePath = fileDialog.FileName;
                    string fileName = Path.GetFileName(selectedFilePath);
                    BitmapImage bitmap = ConvertBitMapImage(selectedFilePath);
                    ItemImageDTO itemImage = new() { ImagePath = selectedFilePath, SourceImage = bitmap, S3FileName = fileName.Replace(" ", "_").ToLower(), S3Bucket = S3Helper.S3Bucket, S3BucketDirectory = S3Helper.S3Directory, ItemId = ((ItemDTO)SelectedItem).Id };
                    ItemImages.Add(itemImage);
                }
            }
        }
        public bool CanAddImage(object p) => ItemImages != null && ItemImages.Count < 4;

        public void DeleteImage(object p)
        {
            if (p is ItemImageDTO itemImageDTO)
            {
                ItemImages.Remove(itemImageDTO);
            }
        }
        public bool CanDeleteImage(object p) => true;

        public async void SearchRelatedProducts(object p)
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
            var viewModel = new SearchItemModalViewModel<ItemDTO, ItemGraphQLModel>(query, fieldHeader1, fieldHeader2, fieldHeader3, fieldData1, fieldData2, fieldData3, variables, MessageToken.RelatedProduct, Context, _dialogService);

            await _dialogService.ShowDialogAsync(viewModel, "Búsqueda de productos");

        }

        public bool CanOpenSearchRelatedProducts(object p) => true;

        public void DeleteRelatedProduct(object p)
        {
            try
            {
                if (ThemedMessageBox.Show("Confirme ...", $"¿Confirma que desea eliminar el producto relacionado: {SelectedRelatedProduct.Item.Name} ?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                if (SelectedRelatedProduct != null)
                {
                    ItemDetailDTO? relatedProductToDelete = RelatedProducts.FirstOrDefault(relatedProduct => relatedProduct.Id == SelectedRelatedProduct.Id);
                    if (relatedProductToDelete is null) return;
                    RelatedProducts.Remove(relatedProductToDelete);
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
        }

        public bool CanDeleteRelatedProduct(object p) => true;

        public void AddRelatedProduct(object p)
        {
            try
            {
                ItemDetailDTO relatedProduct = new ItemDetailDTO() { Item = ReturnedItemFromModal, Parent = (ItemDTO)SelectedItem, Quantity = RelatedProductQuantity };
                RelatedProductName = string.Empty;
                RelatedProductReference = string.Empty;
                RelatedProductCode = string.Empty;
                RelatedProductQuantity = 0;
                RelatedProductQuantityIsEnable = false;
                RelatedProducts.Add(relatedProduct);
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
        }

        public bool CanAddRelatedProduct(object p) => RelatedProductQuantity != 0;

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

        public void AddEanCode(object p)
        {
            try
            {
                EanCodeDTO eanCode = new EanCodeDTO() { EanCode = EanCode };
                EanCode = string.Empty;
                EanCodes.Add(eanCode);
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
        }

        public bool CanAddEanCode(object p) => !string.IsNullOrEmpty(EanCode);

        public async Task SaveItem()
        {
            try
            {
                IsBusy = true;
                Refresh();
                ItemGraphQLModel result = await ExecuteSaveItem();
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new ItemCreateMessage() { CreatedItem = result });
                }
                else
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new ItemUpdateMessage() { UpdatedItem = result });

                }
                IsEditing = false;
                CanUndo = false;
                CanEditItem = true;
                SelectedIndex = 0;
                EanCode = string.Empty;
                RelatedProductName = string.Empty;
                RelatedProductCode = string.Empty;
                RelatedProductReference = string.Empty;
                RelatedProductQuantity = 0;
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
                List<object> relatedProducts = [];
                List<object> images = [];
                if (ItemImages != null)
                {
                    if (IsNewRecord)
                    {
                        if (ItemImages.Count > 0)
                        {
                            foreach (ItemImageDTO image in ItemImages)
                            {
                                S3Helper.LocalFilePath = image.ImagePath;
                                S3Helper.S3FileName = image.S3FileName;
                                await S3Helper.UploadFileToS3Async();
                                string sourceFilePath = image.ImagePath;
                                string directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                                string destinationPath = Path.Combine(directoryPath, "custom", "catalog_item_images", "bd_berdic", image.S3FileName);
                                File.Copy(sourceFilePath, destinationPath, true);

                            }
                        }
                    }
                    else
                    {
                        //Clonación de imagenes para evaaluar eliminaciones o inserciones
                        List<ItemImageDTO> itemImages = new(((ItemDTO)SelectedItem).Images.Select(x => (ItemImageDTO)x.Clone()).ToList());

                        List<ItemImageDTO> itemsToDelete = itemImages.Where(item => !ItemImages.Select(i => i.S3FileName).Contains(item.S3FileName)).ToList();
                        List<ItemImageDTO> itemsToAdd = ItemImages.Where(item => !itemImages.Select(i => i.S3FileName).Contains(item.S3FileName)).ToList();

                        if (itemsToDelete.Count > 0)
                        {
                            foreach (ItemImageDTO image in itemsToDelete)
                            {
                                S3Helper.S3FileName = image.S3FileName;
                                await S3Helper.DeleteFileFromS3Async();
                                string directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                                string destinationPath = Path.Combine(directoryPath, "custom", "catalog_item_images", "bd_berdic", image.S3FileName);
                                if (Path.Exists(destinationPath)) File.Delete(destinationPath);
                            }
                        }
                        if (itemsToAdd.Count > 0)
                        {
                            foreach (ItemImageDTO image in itemsToAdd)
                            {
                                S3Helper.LocalFilePath = image.ImagePath;
                                S3Helper.S3FileName = image.S3FileName;
                                await S3Helper.UploadFileToS3Async();
                                string sourceFilePath = image.ImagePath;
                                string directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                                string destinationPath = Path.Combine(directoryPath, "custom", "catalog_item_images", "bd_berdic", image.S3FileName);
                                File.Copy(sourceFilePath, destinationPath, true);
                            }
                        }


                    }
                    foreach (ItemImageDTO image in ItemImages)
                    {
                        images.Add(new { image.S3Bucket, image.S3BucketDirectory, image.S3FileName, Order = ItemImages.IndexOf(image) });
                    }
                }
                if (EanCodes != null)
                {
                    foreach (EanCodeDTO eanCode in EanCodes)
                    {
                        eanCodes.Add(new { eanCode.EanCode });
                    }
                }

                if (RelatedProducts != null)
                {
                    foreach (ItemDetailDTO relatedProduct in RelatedProducts)
                    {
                        relatedProducts.Add(new { itemId = relatedProduct.Item.Id, relatedProduct.Quantity });
                    }
                }
                dynamic variables = new ExpandoObject();

                variables.Data = new ExpandoObject();
                if (!IsNewRecord) variables.Id = Id;
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
                if (IsNewRecord) variables.Data.ItemSubCategoryId = SelectedSubCategoryIdBeforeNewItem;

                if (eanCodes.Count == 0) variables.Data.EanCodes = new List<object>();
                if (eanCodes.Count > 0)
                {
                    variables.Data.EanCodes = new List<object>();
                    variables.Data.EanCodes = eanCodes;
                }
                if (relatedProducts.Count == 0) variables.Data.RelatedProducts = new List<object>();
                if (relatedProducts.Count > 0)
                {
                    variables.Data.RelatedProducts = new List<object>();
                    variables.Data.RelatedProducts = relatedProducts;
                }

                if (images.Count == 0) variables.Data.Images = new List<object>();
                if (images.Count > 0)
                {
                    variables.Data.Images = new List<object>();
                    variables.Data.Images = images;
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
                        images{
                            id
                            s3Bucket
                            s3BucketDirectory
                            s3FileName
                            order
                            item{
                                id
                            }
                        }
                        relatedProducts{
                            id
                            quantity
                            item{
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
                            parent{
                                id
                            }
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
                        images{
                            id
                            s3Bucket
                            s3BucketDirectory
                            s3FileName
                            order
                            item{
                                id
                            }
                        }
                        relatedProducts{
                            id
                            quantity
                            item{
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
                            parent{
                                id
                            }
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

        public async Task EditItem()
        {
            IsEditing = true;
            CanUndo = true;
            CanEditItem = false;
            IsNewRecord = false;

            this.SetFocus(() => Name);
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
            await SetItemForEdit((ItemDTO)SelectedItem);
            IsEditing = false;
            CanUndo = false;
            CanEditItem = true;
        }

        public async Task InitializeComboBoxes()
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

        #endregion

        #region "Commands"
        private ICommand _addImageCommand;

        public ICommand AddImageCommand
        {
            get
            {
                if (_addImageCommand is null) _addImageCommand = new RelayCommand(CanAddImage, AddImage);
                return _addImageCommand;
            }
        }

        private ICommand _deleteImageCommand;

        public ICommand DeleteImageCommand
        {
            get
            {
                if (_deleteImageCommand is null) _deleteImageCommand = new RelayCommand(CanDeleteImage, DeleteImage);
                return _deleteImageCommand;
            }
        }


        private ICommand _openSearchRelatedProducts;

        public ICommand OpenSearchRelatedProducts
        {
            get
            {
                if (_openSearchRelatedProducts is null) _openSearchRelatedProducts = new RelayCommand(CanOpenSearchRelatedProducts, SearchRelatedProducts);
                return _openSearchRelatedProducts;
            }
        }

        private ICommand _deleteRelatedProductCommand;

        public ICommand DeleteRelatedProductCommand
        {
            get
            {
                if (_deleteRelatedProductCommand is null) _deleteRelatedProductCommand = new RelayCommand(CanDeleteRelatedProduct, DeleteRelatedProduct);
                return _deleteRelatedProductCommand;
            }
        }

        private ICommand _addRelatedProductCommand;

        public ICommand AddRelatedProductCommand
        {
            get
            {
                if (_addRelatedProductCommand is null) _addRelatedProductCommand = new RelayCommand(CanAddRelatedProduct, AddRelatedProduct);
                return _addRelatedProductCommand;
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

        private ICommand _addEanCodeCommand;

        public ICommand AddEanCodeCommand
        {
            get
            {
                if (_addEanCodeCommand is null) _addEanCodeCommand = new RelayCommand(CanAddEanCode, AddEanCode);
                return _addEanCodeCommand;
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

        private ICommand _editItemCommand;

        public ICommand EditItemCommand
        {
            get
            {
                if (_editItemCommand is null) _editItemCommand = new AsyncCommand(EditItem, CanEditItem);
                return _editItemCommand;
            }
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
        #endregion

        #region "Messages"

        void OnFindRelatedProductMessage(ReturnedItemFromModalViewMessage message)
        {
            ReturnedItemFromModal = Context.AutoMapper.Map<ItemDTO>(message.ReturnedItem);
            RelatedProductName = ReturnedItemFromModal.Name;
            RelatedProductReference = ReturnedItemFromModal.Reference;
            RelatedProductCode = ReturnedItemFromModal.Code;
            RelatedProductAllowFraction = ReturnedItemFromModal.AllowFraction;
        }

        #endregion

        #endregion

        #region "TreView"

        #region "Properties"
        public bool TreeViewEnable => !IsEditing;
        public bool ItemDTOIsSelected
        {
            get
            {
                if (SelectedItem is ItemDTO && SelectedCatalog.Id != 0)
                {
                    NotifyOfPropertyChange(nameof(HasRelatedProducts));
                    return true;
                }
                return false;
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

                    _ = HandleSelectedItemChangedAsync();
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

        public bool CanDiscontinueItem => true;

        public bool CanDeleteItem => true;

        public bool CanCreateItem => true;

        public bool CatalogIsSelected => SelectedCatalog != null && SelectedCatalog.Id != 0;

        public bool DeleteCatalogButtonEnable => SelectedCatalog != null && SelectedCatalog.Id != 0 && SelectedCatalog.ItemsTypes.Count == 0;

        public bool CanDeleteCatalog => true;

        public bool CanUpdateCatalog => true;

        public bool CanCreateCatalog => true;

        public bool CanUpdateItemSubCategory => true;

        public bool CanCreateItemSubCategory => true;

        public bool CanCreateItemCategory => true;

        public bool CanUpdateItemCategory => true;

        public bool CanCreateItemType => true;

        public bool CanUpdateItemType => true;

        public bool CanDeleteItemSubCategory => true;

        public bool CanDeleteItemType => true;

        public bool CanDeleteItemCategory => true;

        #endregion

        #region "Methods"


        public async Task LoadCatalogs()
        {

            try
            {
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

                IEnumerable<CatalogGraphQLModel> source = await CatalogService.GetList(query, variables);
                Catalogs = Context.AutoMapper.Map<ObservableCollection<CatalogDTO>>(source);
                if (Catalogs.Count > 0)
                {
                    foreach (CatalogDTO catalog in Catalogs)
                    {
                        foreach (ItemTypeDTO itemType in catalog.ItemsTypes)
                        {
                            itemType.Context = this;
                            itemType.ItemsCategories.Add(new ItemCategoryDTO() { IsDummyChild = true, SubCategories = [], Name = "Fucking Dummy" });
                        }
                    }

                    SelectedCatalog = Catalogs.FirstOrDefault() ?? throw new Exception("");
                    return;
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Catalogs = Context.AutoMapper.Map<ObservableCollection<CatalogDTO>>(source);
                        Catalogs.Insert(0, new CatalogDTO() { Id = 0, Name = "<< NO EXISTE NINGÚN CATÁLOGO CREADO ACTUALMENTE >> " });

                    });

                    foreach (CatalogDTO catalog in Catalogs)
                    {
                        foreach (ItemTypeDTO itemType in catalog.ItemsTypes)
                        {
                            itemType.Context = this;
                            itemType.ItemsCategories.Add(new ItemCategoryDTO() { IsDummyChild = true, SubCategories = [], Name = "Fucking Dummy" });
                        }
                    }

                    SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == 0) ?? throw new Exception("");
                    return;
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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCatalogs" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
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
                        images{
                            id
                            s3Bucket
                            s3BucketDirectory
                            s3FileName
                            order
                            item{
                                id
                            }
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
        public async Task HandleSelectedItemChangedAsync()
        {
            if (_selectedItem != null)
            {
                if (_selectedItem is ItemTypeDTO itemTypeDTO)
                {
                    SelectedMeasurementUnitByDefault = itemTypeDTO.MeasurementUnitByDefault;
                    SelectedAccountingGroupByDefault = itemTypeDTO.AccountingGroupByDefault;
                }
                if (_selectedItem is ItemDTO itemDTO)
                {
                    IsEditing = false;
                    CanEditItem = true;
                    CanUndo = false;
                    await SetItemForEdit(itemDTO);
                }
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

                var validation = await this.ItemService.CanDelete(query, variables);

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

                ItemGraphQLModel discontinuedItem = await ExecuteDiscontinueItem(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new ItemDeleteMessage() { DeletedItem = discontinuedItem });

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


        public async Task<ItemGraphQLModel> ExecuteDiscontinueItem(int id)
        {
            try
            {
                string query = @"
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
                    }
                  }
                }";

                object variables = new
                {
                    Id = id,
                    Data = new
                    {
                        IsActive = false
                    }
                };
                ItemGraphQLModel discontinuedItem = await ItemService.Update(query, variables);
                this.SelectedItem = null;
                return discontinuedItem;
            }
            catch (Exception)
            {
                throw;
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

                IsBusy = true;
                Refresh();


                //Delete images from S3 and local repository
                if (ItemImages.Count > 0)
                {
                    foreach (ItemImageDTO image in ItemImages)
                    {
                        string directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        string imagesLocalPath = Path.Combine(directoryPath, "custom", "catalog_item_images", "bd_berdic", image.S3FileName);
                        if (Path.Exists(imagesLocalPath)) File.Delete(imagesLocalPath);
                        S3Helper.S3FileName = image.S3FileName;
                        await S3Helper.DeleteFileFromS3Async();
                    }
                }

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

        public async Task CreateItem()
        {
            SetItemForNew();
            SelectedItem = new ItemDTO();
            IsEditing = true;
            CanUndo = true;
            CanEditItem = false;
            IsNewRecord = true;
            ValidateProperty(nameof(Name), Name);
            NotifyOfPropertyChange(nameof(CanSaveItem));
            this.SetFocus(() => Name);
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
        public bool CanOpenSearchProducts(object p) => true; 

        #endregion

        #region "Commands"

        private ICommand _discontinueItemCommand;

        public ICommand DiscontinueItemCommand
        {
            get
            {
                if (_discontinueItemCommand is null) _discontinueItemCommand = new AsyncCommand(DiscontinueItem, CanDiscontinueItem);
                return _discontinueItemCommand;
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

        private ICommand _createItemCommand;

        public ICommand CreateItemCommand
        {
            get
            {
                if (_createItemCommand is null) _createItemCommand = new AsyncCommand(CreateItem, CanCreateItem);
                return _createItemCommand;
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

        private ICommand _updateCatalogCommand;

        public ICommand UpdateCatalogCommand
        {
            get
            {
                if (_updateCatalogCommand is null) _updateCatalogCommand = new AsyncCommand(UpdateCatatalog, CanUpdateCatalog);
                return _updateCatalogCommand;
            }
        }

        private ICommand _createCatalogCommand;

        public ICommand CreateCatalogCommand
        {
            get
            {
                if (_createCatalogCommand is null) _createCatalogCommand = new AsyncCommand(CreateCatalog, CanCreateCatalog);
                return _createCatalogCommand;
            }
        }

        private ICommand _updateItemSubCategoryCommand;

        public ICommand UpdateItemSubCategoryCommand
        {
            get
            {
                if (_updateItemSubCategoryCommand is null) _updateItemSubCategoryCommand = new AsyncCommand(UpdateItemSubCategory, CanUpdateItemSubCategory);
                return _updateItemSubCategoryCommand;
            }
        }

        private ICommand _createItemSubCategoryCommand;

        public ICommand CreateItemSubCategoryCommand
        {
            get
            {
                if (_createItemSubCategoryCommand is null) _createItemSubCategoryCommand = new AsyncCommand(CreateItemSubCategory, CanCreateItemSubCategory);
                return _createItemSubCategoryCommand;
            }
        }

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

        private ICommand _updateItemCategoryCommand;

        public ICommand UpdateItemCategoryCommand
        {
            get
            {
                if (_updateItemCategoryCommand is null) _updateItemCategoryCommand = new AsyncCommand(UpdateItemCategory, CanUpdateItemCategory);
                return _updateItemCategoryCommand;
            }
        }

        private ICommand _updateItemTypeCommand;

        public ICommand UpdateItemTypeCommand
        {
            get
            {
                if (_updateItemTypeCommand is null) _updateItemTypeCommand = new AsyncCommand(UpdateItemType, CanUpdateItemType);
                return _updateItemTypeCommand;
            }
        }
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

        private ICommand _deleteItemCategoryCommand;

        public ICommand DeleteItemCategoryCommand
        {
            get
            {
                if (_deleteItemCategoryCommand is null) _deleteItemCategoryCommand = new AsyncCommand(DeleteItemCategory, CanDeleteItemCategory);
                return _deleteItemCategoryCommand;
            }
        }

        private ICommand _openSearchProducts;

        public ICommand OpenSearchProducts
        {
            get
            {
                if (_openSearchProducts is null) _openSearchProducts = new RelayCommand(CanOpenSearchProducts, SearchProducts);
                return _openSearchProducts;
            }
        }
        #endregion

        #region "Commands"

        public async void OnFindProductMessage(ReturnedItemFromModalViewMessage message)
        {
            IsBusy = true;
            await OnFindProductMessageAsync(message);
            IsBusy = false;
        }

        public async Task OnFindProductMessageAsync(ReturnedItemFromModalViewMessage message)
        {
            ItemDTO itemDTO = Context.AutoMapper.Map<ItemDTO>(message.ReturnedItem);
            ItemTypeDTO? itemTypeDTO = SelectedCatalog.ItemsTypes.FirstOrDefault(x => x.Id == itemDTO.SubCategory.ItemCategory.ItemType.Id);
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
            return;
        }
        #endregion

        #endregion

    }


}
