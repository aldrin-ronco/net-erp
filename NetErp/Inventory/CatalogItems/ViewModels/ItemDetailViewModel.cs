using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Force.DeepCloner;
using Microsoft.VisualStudio.Threading;
using Microsoft.Win32;
using Models.Books;
using Models.Global;
using Models.Inventory;
using NetErp.Global.Modals.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Inventory.CatalogItems.DTO;
using NetErp.Inventory.CatalogItems.Validators;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Inventory.CatalogItems.ViewModels
{
    /// <summary>
    /// Detail dialog ViewModel para Item.
    /// Maneja 4 tabs: Básicos (scalars + imágenes + combos), Códigos de barras (EAN),
    /// Productos relacionados (componentes, solo si HasComponents), Otros datos (flags).
    /// </summary>
    public class ItemDetailViewModel : CatalogItemsDetailViewModelBase
    {
        #region Dependencies

        private readonly IRepository<ItemGraphQLModel> _itemService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly StringLengthCache _stringLengthCache;
        private readonly MeasurementUnitCache _measurementUnitCache;
        private readonly ItemBrandCache _itemBrandCache;
        private readonly AccountingGroupCache _accountingGroupCache;
        private readonly ItemSizeCategoryCache _itemSizeCategoryCache;
        private readonly ItemValidator _validator;
        private readonly IMapper _mapper;

        // S3 config (nullable when S3 not available)
        private readonly S3Helper? _s3Helper;
        private readonly string _localImageCachePath;
        public bool IsS3Available => _s3Helper != null;

        // Snapshot of original image S3 file names for delta calculation on save
        private readonly HashSet<string> _originalImageS3Names = [];

        // Original entity snapshot for Undo in panel mode
        private ItemGraphQLModel? _originalEntity;
        private bool _panelHasComponents;
        private bool _panelStockControl;

        #endregion

        #region Constructor

        public ItemDetailViewModel(
            IRepository<ItemGraphQLModel> itemService,
            IEventAggregator eventAggregator,
            Helpers.IDialogService dialogService,
            StringLengthCache stringLengthCache,
            MeasurementUnitCache measurementUnitCache,
            ItemBrandCache itemBrandCache,
            AccountingGroupCache accountingGroupCache,
            ItemSizeCategoryCache itemSizeCategoryCache,
            JoinableTaskFactory joinableTaskFactory,
            ItemValidator validator,
            IMapper mapper,
            S3Helper? s3Helper,
            string localImageCachePath)
            : base(joinableTaskFactory, eventAggregator)
        {
            _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _measurementUnitCache = measurementUnitCache ?? throw new ArgumentNullException(nameof(measurementUnitCache));
            _itemBrandCache = itemBrandCache ?? throw new ArgumentNullException(nameof(itemBrandCache));
            _accountingGroupCache = accountingGroupCache ?? throw new ArgumentNullException(nameof(accountingGroupCache));
            _itemSizeCategoryCache = itemSizeCategoryCache ?? throw new ArgumentNullException(nameof(itemSizeCategoryCache));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _s3Helper = s3Helper;
            _localImageCachePath = localImageCachePath ?? string.Empty;

            DialogWidth = 900;
            DialogHeight = 650;
        }

        #endregion

        #region MaxLength

        public int NameMaxLength => _stringLengthCache.GetMaxLength<ItemGraphQLModel>(nameof(ItemGraphQLModel.Name));
        public int ReferenceMaxLength => _stringLengthCache.GetMaxLength<ItemGraphQLModel>(nameof(ItemGraphQLModel.Reference));

        #endregion

        #region Combo Sources

        public ObservableCollection<MeasurementUnitGraphQLModel> MeasurementUnits { get; private set; } = [];
        public ObservableCollection<ItemBrandGraphQLModel> ItemBrands { get; private set; } = [];
        public ObservableCollection<AccountingGroupGraphQLModel> AccountingGroups { get; private set; } = [];
        public ObservableCollection<ItemSizeCategoryGraphQLModel> Sizes { get; private set; } = [];

        private void LoadComboSources()
        {
            MeasurementUnits = [.. _measurementUnitCache.Items];
            ItemBrands = [.. _itemBrandCache.Items];
            AccountingGroups = [.. _accountingGroupCache.Items];
            Sizes = [.. _itemSizeCategoryCache.Items];
            NotifyOfPropertyChange(nameof(MeasurementUnits));
            NotifyOfPropertyChange(nameof(ItemBrands));
            NotifyOfPropertyChange(nameof(AccountingGroups));
            NotifyOfPropertyChange(nameof(Sizes));
        }

        #endregion

        #region Scalar Form Properties

        public string Code
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Code));
                }
            }
        } = string.Empty;

        [ExpandoPath("name")]
        public string Name
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Name));
                    ValidateProperty(nameof(Name), value);
                    this.TrackChange(nameof(Name), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("reference")]
        public string Reference
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Reference));
                    ValidateProperty(nameof(Reference), value);
                    this.TrackChange(nameof(Reference), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        // IsActive es manejado exclusivamente desde la acción "Descontinuar" del context menu.
        // No se expone en la UI de edición; se mantiene como propiedad read-only para rehidratar
        // desde la API en SetForEdit pero NO se trackea ni se envía en el payload.
        public bool IsActive { get; private set; } = true;

        [ExpandoPath("allowFraction")]
        public bool AllowFraction
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AllowFraction));
                    this.TrackChange(nameof(AllowFraction), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("billable")]
        public bool Billable
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Billable));
                    this.TrackChange(nameof(Billable), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("amountBasedOnWeight")]
        public bool AmountBasedOnWeight
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AmountBasedOnWeight));
                    this.TrackChange(nameof(AmountBasedOnWeight), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("hasExtendedInformation")]
        public bool HasExtendedInformation
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(HasExtendedInformation));
                    this.TrackChange(nameof(HasExtendedInformation), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("aiuBasedService")]
        public bool AiuBasedService
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AiuBasedService));
                    this.TrackChange(nameof(AiuBasedService), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("subCategoryId")]
        public int SubCategoryId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SubCategoryId));
                    this.TrackChange(nameof(SubCategoryId), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        #endregion

        #region Combo Selections (SerializeAsId)

        [ExpandoPath("measurementUnitId", SerializeAsId = true)]
        public MeasurementUnitGraphQLModel? SelectedMeasurementUnit
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedMeasurementUnit));
                    ValidateProperty(nameof(SelectedMeasurementUnit), value);
                    this.TrackChange(nameof(SelectedMeasurementUnit), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("brandId", SerializeAsId = true)]
        public ItemBrandGraphQLModel? SelectedBrand
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedBrand));
                    this.TrackChange(nameof(SelectedBrand), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("accountingGroupId", SerializeAsId = true)]
        public AccountingGroupGraphQLModel? SelectedAccountingGroup
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingGroup));
                    ValidateProperty(nameof(SelectedAccountingGroup), value);
                    this.TrackChange(nameof(SelectedAccountingGroup), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("sizeCategoryId", SerializeAsId = true)]
        public ItemSizeCategoryGraphQLModel? SelectedSize
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedSize));
                    this.TrackChange(nameof(SelectedSize), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        #endregion

        #region Collections

        public ObservableCollection<EanCodeByItemDTO> EanCodes
        {
            get;
            set
            {
                if (field != value)
                {
                    if (field != null) field.CollectionChanged -= OnEanCodesChanged;
                    field = value;
                    if (field != null) field.CollectionChanged += OnEanCodesChanged;
                    NotifyOfPropertyChange(nameof(EanCodes));
                    this.TrackChange(nameof(EanCodes));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = [];

        private void OnEanCodesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            this.TrackChange(nameof(EanCodes));
            NotifyOfPropertyChange(nameof(CanSave));
        }

        public ObservableCollection<ComponentsByItemDTO> Components
        {
            get;
            set
            {
                if (field != value)
                {
                    if (field != null) field.CollectionChanged -= OnComponentsChanged;
                    field = value;
                    if (field != null) field.CollectionChanged += OnComponentsChanged;
                    NotifyOfPropertyChange(nameof(Components));
                    this.TrackChange(nameof(Components));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = [];

        private void OnComponentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            this.TrackChange(nameof(Components));
            NotifyOfPropertyChange(nameof(CanSave));
        }

        public ObservableCollection<ImageByItemDTO> Images
        {
            get;
            set
            {
                if (field != value)
                {
                    if (field != null) field.CollectionChanged -= OnImagesChanged;
                    field = value;
                    if (field != null) field.CollectionChanged += OnImagesChanged;
                    NotifyOfPropertyChange(nameof(Images));
                    NotifyOfPropertyChange(nameof(CanAddImage));
                    this.TrackChange(nameof(Images));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = [];

        private void OnImagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            this.TrackChange(nameof(Images));
            NotifyOfPropertyChange(nameof(CanAddImage));
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion

        #region EAN Draft

        public string EanCodeDraft
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(EanCodeDraft));
                    NotifyOfPropertyChange(nameof(CanAddEanCode));
                }
            }
        } = string.Empty;

        public EanCodeByItemDTO? SelectedEanCode
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedEanCode));
                    NotifyOfPropertyChange(nameof(CanDeleteEanCode));
                }
            }
        }

        public bool CanDeleteEanCode => SelectedEanCode != null && !SelectedEanCode.IsInternal;

        public bool CanAddEanCode => !string.IsNullOrWhiteSpace(EanCodeDraft);

        #endregion

        #region Component Draft (simplificado)

        public ItemGraphQLModel? DraftComponentItem
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    // Notify all derived props
                    NotifyOfPropertyChange(nameof(DraftComponentItem));
                    NotifyOfPropertyChange(nameof(DraftComponentName));
                    NotifyOfPropertyChange(nameof(DraftComponentReference));
                    NotifyOfPropertyChange(nameof(DraftComponentCode));
                    NotifyOfPropertyChange(nameof(DraftComponentAllowFraction));
                    NotifyOfPropertyChange(nameof(DraftComponentQuantityEnabled));
                    NotifyOfPropertyChange(nameof(CanAddComponent));
                }
            }
        }

        public decimal DraftComponentQuantity
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DraftComponentQuantity));
                    NotifyOfPropertyChange(nameof(CanAddComponent));
                }
            }
        }

        public string DraftComponentName => DraftComponentItem?.Name ?? string.Empty;
        public string DraftComponentReference => DraftComponentItem?.Reference ?? string.Empty;
        public string DraftComponentCode => DraftComponentItem?.Code ?? string.Empty;
        public bool DraftComponentAllowFraction => DraftComponentItem?.AllowFraction ?? false;
        public bool DraftComponentQuantityEnabled => DraftComponentItem != null;
        public bool CanAddComponent => DraftComponentItem != null && DraftComponentQuantity > 0;

        public ComponentsByItemDTO? SelectedComponent
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedComponent));
                }
            }
        }

        #endregion

        #region Mode flags (modal vs panel)

        /// <summary>
        /// True = modo modal (diálogo). Save cierra. Siempre IsEditing=true al abrirse.
        /// False = modo panel lateral. Save vuelve a read-only.
        /// </summary>
        public bool IsModal
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsModal));
                }
            }
        } = true;

        /// <summary>
        /// True cuando el formulario está habilitado para edición.
        /// En modal siempre true; en panel, false por defecto hasta que el usuario presiona "Editar".
        /// </summary>
        public bool IsEditing
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsEditing));
                    NotifyOfPropertyChange(nameof(CanSave));
                    NotifyOfPropertyChange(nameof(CanEnterEditMode));
                    NotifyOfPropertyChange(nameof(CanUndoEdit));
                }
            }
        }

        /// <summary>
        /// True cuando el panel tiene un item cargado (para toggle de placeholder).
        /// </summary>
        public bool HasLoadedItem
        {
            get;
            private set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(HasLoadedItem));
                    NotifyOfPropertyChange(nameof(CanEnterEditMode));
                }
            }
        }

        /// <summary>
        /// Pushed from the owning master (<c>CatalogRootMasterViewModel</c>) after the
        /// permission cache resolves. Used only in panel mode to gate the "Editar" button.
        /// Defaults to true so dialog-mode usages (where permissions are checked upstream
        /// via <c>CanNewItem</c>) are not affected.
        /// </summary>
        public bool HasEditPermission
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(HasEditPermission));
                    NotifyOfPropertyChange(nameof(CanEnterEditMode));
                }
            }
        } = true;

        public bool CanEnterEditMode => !IsModal && HasEditPermission && HasLoadedItem && !IsEditing && !IsBusy;
        public bool CanUndoEdit => !IsModal && HasLoadedItem && IsEditing && !IsBusy;

        #endregion

        #region Other UI State

        public int SelectedTabIndex
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedTabIndex));
                }
            }
        }

        /// <summary>True si el ItemType padre NO controla inventario (permite componentes).</summary>
        public bool HasComponents
        {
            get;
            private set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(HasComponents));
                }
            }
        }

        /// <summary>True si el ItemType padre controla inventario.</summary>
        public bool ControlsStock
        {
            get;
            private set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ControlsStock));
                }
            }
        }

        public bool CanAddImage => IsS3Available && Images != null && Images.Count < 4;

        #endregion

        #region CanSave

        #region Tab error indicators

        // Todos los campos validados viven en el tab "Básicos". Si se agregan validaciones
        // a campos de otros tabs (Códigos, Componentes, Otros datos), mover aquí.
        private static readonly string[] _basicDataFields =
        [
            nameof(Name),
            nameof(Reference),
            nameof(SelectedMeasurementUnit),
            nameof(SelectedAccountingGroup)
        ];

        public bool HasBasicDataErrors => _basicDataFields.Any(f => _errors.ContainsKey(f));
        public string? BasicDataTabTooltip => GetTabTooltip(_basicDataFields);

        protected override void RaiseErrorsChanged(string propertyName)
        {
            base.RaiseErrorsChanged(propertyName);
            if (_basicDataFields.Contains(propertyName))
            {
                NotifyOfPropertyChange(nameof(HasBasicDataErrors));
                NotifyOfPropertyChange(nameof(BasicDataTabTooltip));
            }
        }

        #endregion

        public override bool CanSave => IsEditing && _validator.CanSave(new ItemCanSaveContext
        {
            IsBusy = IsBusy,
            Name = Name,
            Reference = Reference,
            HasMeasurementUnit = SelectedMeasurementUnit != null,
            HasAccountingGroup = SelectedAccountingGroup != null,
            HasChanges = this.HasChanges(),
            HasErrors = _errors.Count > 0
        });

        #endregion

        #region Commands

        private ICommand? _saveCommand;
        public ICommand SaveCommand => _saveCommand ??= new AsyncCommand(SaveAsync);

        private ICommand? _cancelCommand;
        public ICommand CancelCommand => _cancelCommand ??= new AsyncCommand(CancelAsync);

        private ICommand? _enterEditModeCommand;
        public ICommand EnterEditModeCommand => _enterEditModeCommand ??= new DelegateCommand(EnterEditMode, () => CanEnterEditMode);

        private ICommand? _undoEditCommand;
        public ICommand UndoEditCommand => _undoEditCommand ??= new DelegateCommand(UndoEdit, () => CanUndoEdit);

        private ICommand? _addEanCodeCommand;
        public ICommand AddEanCodeCommand => _addEanCodeCommand ??= new DelegateCommand(AddEanCode, () => CanAddEanCode);

        private ICommand? _deleteEanCodeCommand;
        public ICommand DeleteEanCodeCommand => _deleteEanCodeCommand ??= new DelegateCommand(DeleteEanCode, () => CanDeleteEanCode);

        private ICommand? _addImageCommand;
        public ICommand AddImageCommand => _addImageCommand ??= new DelegateCommand(AddImage, () => CanAddImage);

        private ICommand? _deleteImageCommand;
        public ICommand DeleteImageCommand => _deleteImageCommand ??= new DelegateCommand<object>(DeleteImage);

        private ICommand? _openSearchComponentsCommand;
        public ICommand OpenSearchComponentsCommand => _openSearchComponentsCommand ??= new AsyncCommand(OpenSearchComponentsAsync);

        private ICommand? _addComponentCommand;
        public ICommand AddComponentCommand => _addComponentCommand ??= new DelegateCommand(AddComponent, () => CanAddComponent);

        private ICommand? _deleteComponentCommand;
        public ICommand DeleteComponentCommand => _deleteComponentCommand ??= new DelegateCommand(DeleteComponent);

        #endregion

        #region EAN handlers

        private void AddEanCode()
        {
            if (string.IsNullOrWhiteSpace(EanCodeDraft)) return;
            EanCodes.Add(new EanCodeByItemDTO { EanCode = EanCodeDraft.Trim(), IsInternal = false });
            EanCodeDraft = string.Empty;
        }

        private void DeleteEanCode()
        {
            if (SelectedEanCode is null || SelectedEanCode.IsInternal) return;
            if (ThemedMessageBox.Show("Atención!",
                $"¿Confirma que desea eliminar el código de barras: {SelectedEanCode.EanCode}?",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            EanCodes.Remove(SelectedEanCode);
        }

        #endregion

        #region Image handlers

        private void AddImage()
        {
            if (!IsS3Available) return;

            OpenFileDialog fileDialog = new()
            {
                Filter = "Image Files (*.jpg; *.jpeg; *.png; *.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
            };
            if (fileDialog.ShowDialog() != true) return;

            FileInfo fileInfo = new(fileDialog.FileName);
            const long fileSizeLimit = 400 * 1024;
            if (fileInfo.Length > fileSizeLimit)
            {
                ThemedMessageBox.Show(
                    title: "Archivo demasiado grande",
                    text: "El archivo seleccionado es demasiado grande. Por favor, selecciona un archivo de menos de 400KB",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Warning);
                return;
            }

            string fileName = Path.GetFileName(fileDialog.FileName);
            BitmapImage bitmap = LoadBitmap(fileDialog.FileName);
            Images.Add(new ImageByItemDTO
            {
                ImagePath = fileDialog.FileName,
                SourceImage = bitmap,
                S3FileName = fileName.Replace(" ", "_").ToLower(),
                S3Bucket = _s3Helper!.Bucket,
                S3BucketDirectory = _s3Helper.Directory
            });
        }

        private void DeleteImage(object? parameter)
        {
            if (parameter is ImageByItemDTO image)
                Images.Remove(image);
        }

        private static BitmapImage LoadBitmap(string filePath)
        {
            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.EndInit();
            return bitmap;
        }

        private async Task LoadImagesFromS3Async()
        {
            if (!IsS3Available || Images.Count == 0) return;

            foreach (ImageByItemDTO image in Images)
            {
                string localPath = Path.Combine(_localImageCachePath, image.S3FileName);
                if (!Path.Exists(localPath))
                    await _s3Helper!.DownloadFileAsync(localPath, image.S3FileName);
                image.SourceImage = LoadBitmap(localPath);
                image.ImagePath = localPath;
            }
        }

        private async Task ApplyS3ChangesAsync()
        {
            if (!IsS3Available || Images == null) return;

            if (IsNewRecord)
            {
                foreach (ImageByItemDTO image in Images)
                {
                    await _s3Helper!.UploadFileAsync(image.ImagePath, image.S3FileName);
                    string dst = Path.Combine(_localImageCachePath, image.S3FileName);
                    File.Copy(image.ImagePath, dst, true);
                }
                return;
            }

            // Update: diff against original snapshot
            HashSet<string> currentNames = [.. Images.Select(i => i.S3FileName)];
            IEnumerable<string> toDelete = _originalImageS3Names.Where(n => !currentNames.Contains(n));
            IEnumerable<ImageByItemDTO> toAdd = Images.Where(i => !_originalImageS3Names.Contains(i.S3FileName));

            foreach (string fileName in toDelete)
            {
                await _s3Helper!.DeleteFileAsync(fileName);
                string dst = Path.Combine(_localImageCachePath, fileName);
                if (Path.Exists(dst)) File.Delete(dst);
            }

            foreach (ImageByItemDTO image in toAdd)
            {
                await _s3Helper!.UploadFileAsync(image.ImagePath, image.S3FileName);
                string dst = Path.Combine(_localImageCachePath, image.S3FileName);
                File.Copy(image.ImagePath, dst, true);
            }
        }

        #endregion

        #region Component handlers (search via callback)

        private async Task OpenSearchComponentsAsync()
        {
            string query = BuildSearchComponentsQuery();

            SearchWithThreeColumnsGridViewModel<ItemGraphQLModel> searchVm = new(
                query: query,
                fieldHeader1: "Código", fieldHeader2: "Nombre", fieldHeader3: "Referencia",
                fieldData1: "Code", fieldData2: "Name", fieldData3: "Reference",
                variables: null,
                dialogService: _dialogService,
                onSelectedAsync: OnComponentSelectedAsync);

            await _dialogService.ShowDialogAsync(searchVm, "Búsqueda de productos");
        }

        private Task OnComponentSelectedAsync(ItemGraphQLModel? selected)
        {
            if (selected is null) return Task.CompletedTask;
            DraftComponentItem = selected;
            DraftComponentQuantity = 0;
            return Task.CompletedTask;
        }

        private void AddComponent()
        {
            if (DraftComponentItem is null || DraftComponentQuantity <= 0) return;

            Components.Add(new ComponentsByItemDTO
            {
                Component = _mapper.Map<ItemDTO>(DraftComponentItem),
                Quantity = DraftComponentQuantity
            });

            DraftComponentItem = null;
            DraftComponentQuantity = 0;
        }

        private void DeleteComponent()
        {
            if (SelectedComponent is null) return;
            if (ThemedMessageBox.Show("Atención!",
                $"¿Confirma que desea eliminar el producto relacionado: {SelectedComponent.Component?.Name}?",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            Components.Remove(SelectedComponent);
        }

        private static string BuildSearchComponentsQuery()
        {
            Dictionary<string, object> fields = FieldSpec<PageType<ItemGraphQLModel>>
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
                    .Field(e => e.AllowFraction)
                    .Select(e => e.MeasurementUnit, mu => mu.Field(m => m.Id).Field(m => m.Name)))
                .Build();

            GraphQLQueryFragment fragment = new("itemsPage",
                [new("filters", "ItemFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        #endregion

        #region SetForNew / SetForEdit

        public void SetForNew(int subCategoryId, bool hasComponents, bool stockControl, int? defaultMeasurementUnitId, int? defaultAccountingGroupId)
        {
            IsModal = true;
            IsEditing = true;
            HasLoadedItem = true;

            LoadComboSources();

            Id = 0;
            SubCategoryId = subCategoryId;
            HasComponents = hasComponents;
            ControlsStock = stockControl;
            Code = string.Empty;
            Name = string.Empty;
            Reference = string.Empty;
            IsActive = true;
            AllowFraction = false;
            Billable = true;
            AmountBasedOnWeight = false;
            HasExtendedInformation = false;
            AiuBasedService = false;

            EanCodes = [];
            Components = [];
            Images = [];
            _originalImageS3Names.Clear();
            SelectedTabIndex = 0;
            EanCodeDraft = string.Empty;
            DraftComponentItem = null;
            DraftComponentQuantity = 0;

            SelectedMeasurementUnit = defaultMeasurementUnitId.HasValue
                ? MeasurementUnits.FirstOrDefault(x => x.Id == defaultMeasurementUnitId.Value)
                : null;
            SelectedAccountingGroup = defaultAccountingGroupId.HasValue
                ? AccountingGroups.FirstOrDefault(x => x.Id == defaultAccountingGroupId.Value)
                : null;
            SelectedBrand = null;
            SelectedSize = null;

            SeedDefaultValues();
        }

        public void SetForEdit(ItemGraphQLModel entity, bool hasComponents, bool stockControl)
        {
            IsModal = true;
            IsEditing = true;
            HasLoadedItem = true;
            _panelHasComponents = hasComponents;
            _panelStockControl = stockControl;
            _originalEntity = entity;

            LoadComboSources();

            Id = entity.Id;
            SubCategoryId = entity.SubCategory?.Id ?? 0;
            HasComponents = hasComponents;
            ControlsStock = stockControl;
            Code = entity.Code ?? string.Empty;
            Name = entity.Name ?? string.Empty;
            Reference = entity.Reference ?? string.Empty;
            IsActive = entity.IsActive;
            AllowFraction = entity.AllowFraction;
            Billable = entity.Billable;
            AmountBasedOnWeight = entity.AmountBasedOnWeight;
            HasExtendedInformation = entity.HasExtendedInformation;
            AiuBasedService = entity.AiuBasedService;

            SelectedMeasurementUnit = entity.MeasurementUnit != null
                ? MeasurementUnits.FirstOrDefault(x => x.Id == entity.MeasurementUnit.Id)
                : null;
            SelectedAccountingGroup = entity.AccountingGroup != null
                ? AccountingGroups.FirstOrDefault(x => x.Id == entity.AccountingGroup.Id)
                : null;
            SelectedBrand = entity.Brand != null
                ? ItemBrands.FirstOrDefault(x => x.Id == entity.Brand.Id)
                : null;
            SelectedSize = entity.SizeCategory != null
                ? Sizes.FirstOrDefault(x => x.Id == entity.SizeCategory.Id)
                : null;

            EanCodes = entity.EanCodes != null
                ? [.. entity.EanCodes.Select(e => new EanCodeByItemDTO { EanCode = e.EanCode, IsInternal = e.IsInternal })]
                : [];

            Components = entity.Components != null
                ? [.. entity.Components.Select(c => new ComponentsByItemDTO
                {
                    Component = _mapper.Map<ItemDTO>(c.Component),
                    Quantity = c.Quantity
                })]
                : [];

            Images = entity.Images != null
                ? [.. entity.Images.OrderBy(i => i.DisplayOrder).Select(i => new ImageByItemDTO
                {
                    DisplayOrder = i.DisplayOrder,
                    S3Bucket = i.S3Bucket,
                    S3BucketDirectory = i.S3BucketDirectory,
                    S3FileName = i.S3FileName
                })]
                : [];

            _originalImageS3Names.Clear();
            foreach (ImageByItemDTO img in Images) _originalImageS3Names.Add(img.S3FileName);

            _ = LoadImagesFromS3Async();

            EanCodeDraft = string.Empty;
            DraftComponentItem = null;
            DraftComponentQuantity = 0;

            SeedCurrentValues();
        }

        /// <summary>
        /// Carga un item en modo panel lateral (read-only). El usuario puede presionar
        /// "Editar" para habilitar el formulario.
        /// </summary>
        public void LoadForPanel(ItemGraphQLModel entity, bool hasComponents, bool stockControl)
        {
            IsModal = false;
            _panelHasComponents = hasComponents;
            _panelStockControl = stockControl;
            _originalEntity = entity;

            // Populate fields reusing SetForEdit logic, then flip to read-only
            SetForEdit(entity, hasComponents, stockControl);
            IsModal = false;
            IsEditing = false;
            HasLoadedItem = true;
        }

        /// <summary>
        /// Vacía el panel (cuando la selección del árbol deja de ser un ItemDTO).
        /// </summary>
        public void ClearPanel()
        {
            IsModal = false;
            IsEditing = false;
            HasLoadedItem = false;
            _originalEntity = null;
            _originalImageS3Names.Clear();

            Id = 0;
            Code = string.Empty;
            Name = string.Empty;
            Reference = string.Empty;
            IsActive = true;
            AllowFraction = false;
            Billable = false;
            AmountBasedOnWeight = false;
            HasExtendedInformation = false;
            AiuBasedService = false;
            SelectedMeasurementUnit = null;
            SelectedBrand = null;
            SelectedAccountingGroup = null;
            SelectedSize = null;
            EanCodes = [];
            Components = [];
            Images = [];
            EanCodeDraft = string.Empty;
            DraftComponentItem = null;
            DraftComponentQuantity = 0;
            SelectedTabIndex = 0;

            this.ClearSeeds();
            this.AcceptChanges();
        }

        public void EnterEditMode()
        {
            if (!CanEnterEditMode) return;
            IsEditing = true;
        }

        public void UndoEdit()
        {
            if (!CanUndoEdit) return;
            if (_originalEntity is null) { IsEditing = false; return; }
            // Re-cargar desde el snapshot original y volver a read-only
            LoadForPanel(_originalEntity, _panelHasComponents, _panelStockControl);
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(SubCategoryId), SubCategoryId);
            this.SeedValue(nameof(AllowFraction), AllowFraction);
            this.SeedValue(nameof(Billable), Billable);
            this.SeedValue(nameof(AmountBasedOnWeight), AmountBasedOnWeight);
            this.SeedValue(nameof(HasExtendedInformation), HasExtendedInformation);
            this.SeedValue(nameof(AiuBasedService), AiuBasedService);
            this.SeedValue(nameof(SelectedMeasurementUnit), SelectedMeasurementUnit);
            this.SeedValue(nameof(SelectedAccountingGroup), SelectedAccountingGroup);
            this.SeedValue(nameof(SelectedBrand), SelectedBrand);
            this.SeedValue(nameof(SelectedSize), SelectedSize);
            this.AcceptChanges();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(Reference), Reference);
            this.SeedValue(nameof(AllowFraction), AllowFraction);
            this.SeedValue(nameof(Billable), Billable);
            this.SeedValue(nameof(AmountBasedOnWeight), AmountBasedOnWeight);
            this.SeedValue(nameof(HasExtendedInformation), HasExtendedInformation);
            this.SeedValue(nameof(AiuBasedService), AiuBasedService);
            this.SeedValue(nameof(SelectedMeasurementUnit), SelectedMeasurementUnit);
            this.SeedValue(nameof(SelectedBrand), SelectedBrand);
            this.SeedValue(nameof(SelectedAccountingGroup), SelectedAccountingGroup);
            this.SeedValue(nameof(SelectedSize), SelectedSize);
            this.AcceptChanges();
        }

        #endregion

        #region Lifecycle

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperties();
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                await ApplyS3ChangesAsync();

                UpsertResponseType<ItemGraphQLModel> result = await ExecuteSaveAsync();

                if (!result.Success)
                {
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    ThemedMessageBox.Show(
                        text: $"El guardado no ha sido exitoso\r\n\r\n{result.Errors.ToUserMessage()}\r\n\r\nVerifique los datos y vuelva a intentarlo",
                        title: $"{result.Message}!",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new ItemCreateMessage { CreatedItem = result }
                        : new ItemUpdateMessage { UpdatedItem = result },
                    CancellationToken.None);

                if (IsModal)
                {
                    await TryCloseAsync(true);
                }
                else
                {
                    // Panel mode: actualizar snapshot y volver a read-only
                    _originalEntity = result.Entity;
                    foreach (string n in _originalImageS3Names.ToList()) { }
                    _originalImageS3Names.Clear();
                    foreach (ImageByItemDTO img in Images) _originalImageS3Names.Add(img.S3FileName);
                    IsEditing = false;
                    this.AcceptChanges();
                    NotifyOfPropertyChange(nameof(CanSave));
                    NotifyOfPropertyChange(nameof(CanEnterEditMode));
                    NotifyOfPropertyChange(nameof(CanUndoEdit));
                }
            }
            catch (AsyncException ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"Error al realizar operación.\r\n{ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(SaveAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
                NotifyOfPropertyChange(nameof(CanEnterEditMode));
                NotifyOfPropertyChange(nameof(CanUndoEdit));
            }
        }

        private async Task<UpsertResponseType<ItemGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                Dictionary<string, Func<object?, object?>> transformers = new()
                {
                    [nameof(Components)] = item =>
                    {
                        ComponentsByItemDTO c = (ComponentsByItemDTO)item!;
                        return new { itemId = c.Component?.Id ?? 0, quantity = c.Quantity };
                    },
                    [nameof(Images)] = item =>
                    {
                        ImageByItemDTO i = (ImageByItemDTO)item!;
                        return new
                        {
                            s3Bucket = i.S3Bucket,
                            s3BucketDirectory = i.S3BucketDirectory,
                            s3FileName = i.S3FileName,
                            displayOrder = Images.IndexOf(i)
                        };
                    }
                };

                string query = IsNewRecord ? _createQuery.Value.Query : _updateQuery.Value.Query;
                string prefix = IsNewRecord ? "createResponseInput" : "updateResponseData";
                dynamic variables = ChangeCollector.CollectChanges(
                    this,
                    prefix: prefix,
                    transformers,
                    excludeProperties: [nameof(EanCodes)]);

                // EanCodes: send only external (non-internal) codes as plain strings
                List<string> externalEanCodes = EanCodes
                    .Where(e => !e.IsInternal)
                    .Select(e => e.EanCode)
                    .ToList();
                ExpandoHelper.SetNestedProperty(variables, $"{prefix}.eanCodes", externalEanCodes);

                if (IsNewRecord)
                    return await _itemService.CreateAsync<UpsertResponseType<ItemGraphQLModel>>(query, variables);

                variables.updateResponseId = Id;
                return await _itemService.UpdateAsync<UpsertResponseType<ItemGraphQLModel>>(query, variables);
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public async Task CancelAsync()
        {
            if (IsModal)
                await TryCloseAsync(false);
            else
                UndoEdit();
        }

        #endregion

        #region Validation

        private void ValidateProperty(string propertyName, object? value)
        {
            ItemValidationContext context = new()
            {
                Name = Name,
                Reference = Reference,
                HasMeasurementUnit = SelectedMeasurementUnit != null,
                HasAccountingGroup = SelectedAccountingGroup != null
            };
            IReadOnlyList<string> errors = _validator.Validate(propertyName, value, context);
            SetPropertyErrors(propertyName, errors);
        }

        private void ValidateProperties()
        {
            ItemValidationContext context = new()
            {
                Name = Name,
                Reference = Reference,
                HasMeasurementUnit = SelectedMeasurementUnit != null,
                HasAccountingGroup = SelectedAccountingGroup != null
            };
            Dictionary<string, IReadOnlyList<string>> all = _validator.ValidateAll(context);
            SetPropertyErrors(nameof(Name), all.TryGetValue(nameof(Name), out IReadOnlyList<string>? e1) ? e1 : []);
            SetPropertyErrors(nameof(Reference), all.TryGetValue(nameof(Reference), out IReadOnlyList<string>? e2) ? e2 : []);
            SetPropertyErrors(nameof(SelectedMeasurementUnit), all.TryGetValue("SelectedMeasurementUnit", out IReadOnlyList<string>? e3) ? e3 : []);
            SetPropertyErrors(nameof(SelectedAccountingGroup), all.TryGetValue("SelectedAccountingGroup", out IReadOnlyList<string>? e4) ? e4 : []);
        }

        #endregion

        #region GraphQL Queries

        private static Dictionary<string, object> BuildItemResponseFields()
        {
            return FieldSpec<UpsertResponseType<ItemGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "item", nested: entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Reference)
                    .Field(e => e.Code)
                    .Field(e => e.IsActive)
                    .Field(e => e.AllowFraction)
                    .Field(e => e.HasExtendedInformation)
                    .Field(e => e.AiuBasedService)
                    .Field(e => e.AmountBasedOnWeight)
                    .Field(e => e.Billable)
                    .SelectList(e => e.EanCodes, ean => ean
                        .Field(ec => ec.EanCode)
                        .Field(ec => ec.IsInternal))
                    .Select(e => e.AccountingGroup, ag => ag.Field(a => a.Id))
                    .Select(e => e.Brand, b => b.Field(br => br.Id))
                    .Select(e => e.MeasurementUnit, mu => mu.Field(m => m.Id))
                    .Select(e => e.SizeCategory, sc => sc.Field(s => s.Id))
                    .Select(e => e.SubCategory, sub => sub
                        .Field(s => s.Id)
                        .Select(s => s.ItemCategory, ic => ic
                            .Field(c => c.Id)
                            .Select(c => c.ItemType, it => it.Field(t => t.Id))))
                    .SelectList(e => e.Components, comp => comp
                        .Field(c => c.Quantity)
                        .Select(c => c.Component, ci => ci
                            .Field(i => i.Id)
                            .Field(i => i.Name)
                            .Field(i => i.Code)
                            .Field(i => i.Reference)
                            .Field(i => i.AllowFraction)
                            .Select(i => i.MeasurementUnit, mu => mu.Field(m => m.Id).Field(m => m.Name)))
                        .Select(c => c.Parent, p => p.Field(pp => pp.Id)))
                    .SelectList(e => e.Images, img => img
                        .Field(i => i.DisplayOrder)
                        .Field(i => i.S3Bucket)
                        .Field(i => i.S3BucketDirectory)
                        .Field(i => i.S3FileName)
                        .Select(i => i.Item, item => item.Field(it => it.Id))))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();
        }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            Dictionary<string, object> fields = BuildItemResponseFields();
            GraphQLQueryFragment fragment = new("createItem",
                [new("input", "CreateItemInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            Dictionary<string, object> fields = BuildItemResponseFields();
            GraphQLQueryFragment fragment = new("updateItem",
                [new("data", "UpdateItemInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
