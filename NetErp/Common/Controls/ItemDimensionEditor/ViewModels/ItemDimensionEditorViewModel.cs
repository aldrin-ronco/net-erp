using Caliburn.Micro;
using Models.Inventory;
using NetErp.Helpers;
using NetErp.UserControls.ItemDimensionEditor.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetErp.UserControls.ItemDimensionEditor.ViewModels
{
    /// <summary>
    /// ViewModel del UC <c>ItemDimensionEditor</c>.
    /// Flujo: TextBox de búsqueda → ENTER intenta resolver match exacto (Code / Reference / EAN).
    /// Si match → panel de info se llena, foco va a Cantidad.
    /// Si no match → abre modal de búsqueda pasando el término escrito.
    /// </summary>
    public class ItemDimensionEditorViewModel : PropertyChangedBase
    {
        private readonly Func<ItemSearchFilters, CancellationToken, Task<IReadOnlyList<ItemGraphQLModel>>> _searchProvider;
        private readonly Func<int, int, CancellationToken, Task<IReadOnlyList<LotAvailability>>>? _lotProvider;
        private readonly Func<int, int, CancellationToken, Task<IReadOnlyList<SerialAvailability>>>? _serialProvider;
        private readonly Func<int, int, CancellationToken, Task<IReadOnlyList<SizeAvailability>>>? _sizeStockProvider;
        private readonly IDialogService _dialogService;

        /// <summary>
        /// Func que el caller proporciona para abrir el modal de búsqueda de ítems.
        /// Recibe el término inicial, devuelve el ítem seleccionado o null si se cancela.
        /// El caller decide qué modal usa (recomendado: SearchWithThreeColumnsGridViewModel).
        /// </summary>
        public Func<string, Task<ItemGraphQLModel?>>? ItemPickerProvider { get; set; }

        public ItemDimensionEditorViewModel(
            Func<ItemSearchFilters, CancellationToken, Task<IReadOnlyList<ItemGraphQLModel>>> searchProvider,
            DimensionDirection direction,
            IDialogService dialogService,
            Func<int, int, CancellationToken, Task<IReadOnlyList<LotAvailability>>>? lotProvider = null,
            Func<int, int, CancellationToken, Task<IReadOnlyList<SerialAvailability>>>? serialProvider = null,
            Func<int, int, CancellationToken, Task<IReadOnlyList<SizeAvailability>>>? sizeStockProvider = null)
        {
            _searchProvider = searchProvider ?? throw new ArgumentNullException(nameof(searchProvider));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _lotProvider = lotProvider;
            _serialProvider = serialProvider;
            _sizeStockProvider = sizeStockProvider;
            Direction = direction;
        }

        #region Config

        public DimensionDirection Direction { get; set; }

        private int _storageId;
        public int StorageId
        {
            get => _storageId;
            set
            {
                if (_storageId == value) return;
                _storageId = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(IsStorageReady));
            }
        }
        public bool IsStorageReady => _storageId > 0;

        #endregion

        #region Búsqueda

        private string _searchTerm = string.Empty;
        public string SearchTerm
        {
            get => _searchTerm;
            set { if (_searchTerm != value) { _searchTerm = value ?? string.Empty; NotifyOfPropertyChange(); } }
        }

        private bool _searchFieldFocus;
        public bool SearchFieldFocus
        {
            get => _searchFieldFocus;
            set { _searchFieldFocus = value; NotifyOfPropertyChange(); }
        }

        private bool _quantityFieldFocus;
        public bool QuantityFieldFocus
        {
            get => _quantityFieldFocus;
            set { _quantityFieldFocus = value; NotifyOfPropertyChange(); }
        }

        public async Task OnSearchKeyDown(KeyEventArgs e)
        {
            if (e == null) return;
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await ResolveOrSearchAsync();
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                ClearSelection();
            }
        }

        /// <summary>
        /// ENTER en el TextBox: intenta match exacto (Code / Reference / EAN).
        /// Si encuentra exactamente uno, lo selecciona. Si no, abre modal pasando term como filtro.
        /// </summary>
        public async Task ResolveOrSearchAsync()
        {
            var term = _searchTerm?.Trim();
            if (string.IsNullOrEmpty(term))
            {
                await OpenSearchModalAsync(string.Empty);
                return;
            }

            // Pide al caller items que matcheen el término. Usa ExactMatchOnly como hint.
            var filters = new ItemSearchFilters { Term = term, ExactMatchOnly = true };
            IReadOnlyList<ItemGraphQLModel> candidates;
            try
            {
                candidates = await _searchProvider(filters, CancellationToken.None);
            }
            catch
            {
                candidates = [];
            }

            // Filtrado client-side por exact match en Code / Reference / EAN
            var match = candidates.FirstOrDefault(IsExactMatch);
            if (match != null)
            {
                SelectItem(match);
                return;
            }

            // Sin match: abre modal con term como filtro inicial
            await OpenSearchModalAsync(term);
        }

        private bool IsExactMatch(ItemGraphQLModel item)
        {
            if (item == null) return false;
            var t = _searchTerm.Trim();
            if (string.Equals(item.Code, t, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(item.Reference, t, StringComparison.OrdinalIgnoreCase)) return true;
            if (item.EanCodes != null && item.EanCodes.Any(e => string.Equals(e?.EanCode, t, StringComparison.OrdinalIgnoreCase))) return true;
            return false;
        }

        public Task OpenSearchModalAsync() => OpenSearchModalAsync(_searchTerm?.Trim() ?? string.Empty);

        private async Task OpenSearchModalAsync(string initialTerm)
        {
            ItemGraphQLModel? picked = null;
            if (ItemPickerProvider != null)
                picked = await ItemPickerProvider(initialTerm);
            else
            {
                // Fallback al modal interno si el caller no proveyó picker.
                var modal = new ItemSearchModalViewModel(_searchProvider, initialTerm);
                var result = await _dialogService.ShowDialogAsync(modal, "Buscar producto");
                if (result == true) picked = modal.SelectedItem;
            }
            if (picked != null) SelectItem(picked);
            else SearchFieldFocus = true;
        }

        #endregion

        #region Selección y dimensiones

        private ItemGraphQLModel? _selectedItem;
        public ItemGraphQLModel? SelectedItem
        {
            get => _selectedItem;
            private set
            {
                if (ReferenceEquals(_selectedItem, value)) return;
                _selectedItem = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(HasSelectedItem));
                NotifyOfPropertyChange(nameof(SelectedItemCode));
                NotifyOfPropertyChange(nameof(SelectedItemName));
                NotifyOfPropertyChange(nameof(SelectedItemReference));
                NotifyOfPropertyChange(nameof(SelectedItemMeasurementUnit));
                NotifyOfPropertyChange(nameof(DimensionType));
                NotifyOfPropertyChange(nameof(IsBaseDimension));
                NotifyOfPropertyChange(nameof(IsDimensioned));
                NotifyOfPropertyChange(nameof(QuantityIsReadOnly));
                NotifyOfPropertyChange(nameof(DimensionsButtonVisible));
                NotifyOfPropertyChange(nameof(IsLineComplete));
            }
        }

        public bool HasSelectedItem => _selectedItem != null;
        public string SelectedItemCode => _selectedItem?.Code ?? string.Empty;
        public string SelectedItemName => _selectedItem?.Name ?? string.Empty;
        public string SelectedItemReference => _selectedItem?.Reference ?? string.Empty;
        public string SelectedItemMeasurementUnit => _selectedItem?.MeasurementUnit?.Abbreviation ?? string.Empty;

        public DimensionType DimensionType
        {
            get
            {
                if (_selectedItem == null) return DimensionType.Base;
                if (_selectedItem.IsLotTracked) return DimensionType.Lot;
                if (_selectedItem.IsSerialTracked) return DimensionType.Serial;
                if (_selectedItem.SizeCategory != null && _selectedItem.SizeCategory.Id > 0) return DimensionType.Size;
                return DimensionType.Base;
            }
        }

        public bool IsBaseDimension => DimensionType == DimensionType.Base;
        public bool IsDimensioned => DimensionType != DimensionType.Base;
        public bool QuantityIsReadOnly => IsDimensioned;
        public bool DimensionsButtonVisible => HasSelectedItem && IsDimensioned;

        public void SelectItem(ItemGraphQLModel item)
        {
            if (item == null) return;
            SelectedItem = item;
            _lots.Clear();
            _serials.Clear();
            _sizes.Clear();
            _baseQuantity = 0m;
            NotifyOfPropertyChange(nameof(BaseQuantity));
            NotifyOfPropertyChange(nameof(TotalQuantity));
            NotifyOfPropertyChange(nameof(DimensionSummary));
            NotifyOfPropertyChange(nameof(IsLineComplete));
            // Pasa foco a cantidad para captura inmediata
            QuantityFieldFocus = false;
            QuantityFieldFocus = true;
        }

        public void ClearSelection()
        {
            SelectedItem = null;
            _lots.Clear();
            _serials.Clear();
            _sizes.Clear();
            _baseQuantity = 0m;
            _searchTerm = string.Empty;
            NotifyOfPropertyChange(nameof(SearchTerm));
            NotifyOfPropertyChange(nameof(BaseQuantity));
            NotifyOfPropertyChange(nameof(TotalQuantity));
            NotifyOfPropertyChange(nameof(DimensionSummary));
            NotifyOfPropertyChange(nameof(IsLineComplete));
            SearchFieldFocus = false;
            SearchFieldFocus = true;
        }

        #endregion

        #region Cantidad / Dimensiones

        private decimal _baseQuantity;
        public decimal BaseQuantity
        {
            get => _baseQuantity;
            set
            {
                if (_baseQuantity == value) return;
                _baseQuantity = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(TotalQuantity));
                NotifyOfPropertyChange(nameof(IsLineComplete));
                NotifyOfPropertyChange(nameof(CanComplete));
            }
        }

        public bool CanComplete => IsLineComplete;

        private readonly List<LotDraft> _lots = [];
        private readonly List<SerialDraft> _serials = [];
        private readonly List<SizeDraft> _sizes = [];

        public decimal TotalQuantity => DimensionType switch
        {
            DimensionType.Base => _baseQuantity,
            DimensionType.Lot => _lots.Sum(l => l.Quantity),
            DimensionType.Serial => _serials.Count,
            DimensionType.Size => _sizes.Sum(s => s.Quantity),
            _ => 0m
        };

        public string DimensionSummary => DimensionType switch
        {
            DimensionType.Lot when _lots.Count > 0 => $"{_lots.Count} lote(s) · {TotalQuantity:0.##} u",
            DimensionType.Serial when _serials.Count > 0 => $"{_serials.Count} serial(es)",
            DimensionType.Size when _sizes.Count > 0 => $"{_sizes.Count} talla(s) · {TotalQuantity:0.##} u",
            _ => string.Empty
        };

        public bool IsLineComplete
        {
            get
            {
                if (_selectedItem == null) return false;
                return DimensionType switch
                {
                    DimensionType.Base => _baseQuantity > 0,
                    DimensionType.Lot => _lots.Count > 0 && _lots.All(l => l.Quantity > 0),
                    DimensionType.Serial => _serials.Count > 0,
                    DimensionType.Size => _sizes.Count > 0 && _sizes.All(s => s.Quantity > 0),
                    _ => false
                };
            }
        }

        public async Task OpenDimensionsDialogAsync()
        {
            if (_selectedItem == null) return;
            switch (DimensionType)
            {
                case DimensionType.Lot: await OpenLotsDialogAsync(); break;
                case DimensionType.Serial: await OpenSerialsDialogAsync(); break;
                case DimensionType.Size: await OpenSizesDialogAsync(); break;
            }
        }

        private async Task OpenLotsDialogAsync()
        {
            if (Direction == DimensionDirection.Out && _lotProvider == null) return;
            var dialogVm = new LotsDimensionDialogViewModel(_selectedItem!, Direction, _storageId, _lots, _lotProvider);
            var result = await _dialogService.ShowDialogAsync(dialogVm, "Asignar lotes");
            if (result == true)
            {
                _lots.Clear();
                _lots.AddRange(dialogVm.Result);
                NotifyAfterDimensions();
            }
        }

        private async Task OpenSerialsDialogAsync()
        {
            if (Direction == DimensionDirection.Out && _serialProvider == null) return;
            var dialogVm = new SerialsDimensionDialogViewModel(_selectedItem!, Direction, _storageId, _serials, _serialProvider);
            var result = await _dialogService.ShowDialogAsync(dialogVm, "Asignar seriales");
            if (result == true)
            {
                _serials.Clear();
                _serials.AddRange(dialogVm.Result);
                NotifyAfterDimensions();
            }
        }

        private async Task OpenSizesDialogAsync()
        {
            var dialogVm = new SizesDimensionDialogViewModel(_selectedItem!, Direction, _storageId, _sizes, _sizeStockProvider);
            var result = await _dialogService.ShowDialogAsync(dialogVm, "Asignar tallas");
            if (result == true)
            {
                _sizes.Clear();
                _sizes.AddRange(dialogVm.Result);
                NotifyAfterDimensions();
            }
        }

        private void NotifyAfterDimensions()
        {
            NotifyOfPropertyChange(nameof(TotalQuantity));
            NotifyOfPropertyChange(nameof(DimensionSummary));
            NotifyOfPropertyChange(nameof(IsLineComplete));
            NotifyOfPropertyChange(nameof(CanComplete));
        }

        /// <summary>
        /// El caller invoca este método cuando su botón de confirmación se activa.
        /// Dispara <see cref="LineCompleted"/> con los datos de qty + dimensiones.
        /// El costo / precio / descuento los maneja el caller en su propia UI y los combina
        /// con este payload al persistir.
        /// </summary>
        public void RaiseCompleted()
        {
            if (!CanComplete) return;
            LineCompleted?.Invoke(this, new LineCompletedEventArgs
            {
                Item = _selectedItem!,
                TotalQuantity = TotalQuantity,
                Lots = [.. _lots],
                Serials = [.. _serials],
                Sizes = [.. _sizes],
                Direction = Direction
            });
        }

        #endregion

        #region Evento + Reset

        public event EventHandler<LineCompletedEventArgs>? LineCompleted;

        public void Reset()
        {
            ClearSelection();
        }

        public void FocusSearch()
        {
            SearchFieldFocus = false;
            SearchFieldFocus = true;
        }

        #endregion
    }

    public enum DimensionType
    {
        Base,
        Lot,
        Serial,
        Size
    }
}
