using Caliburn.Micro;
using DevExpress.Xpf.Core;
using Models.Inventory;
using NetErp.UserControls.ItemDimensionEditor.DTO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NetErp.UserControls.ItemDimensionEditor.ViewModels
{
    /// <summary>
    /// Modal de captura de tallas.
    /// Entradas (I): lista todas las tallas de la <c>sizeCategory</c>; usuario digita cantidad por talla.
    /// Salidas (O): sólo tallas con stock&gt;0 vía <c>stockBySizesPage</c>; muestra disponible.
    /// </summary>
    public class SizesDimensionDialogViewModel : Screen
    {
        private readonly ItemGraphQLModel _item;
        private readonly DimensionDirection _direction;
        private readonly int _storageId;
        private readonly Func<int, int, CancellationToken, Task<IReadOnlyList<SizeAvailability>>>? _availabilityProvider;

        public SizesDimensionDialogViewModel(
            ItemGraphQLModel item,
            DimensionDirection direction,
            int storageId,
            IEnumerable<SizeDraft> initialState,
            Func<int, int, CancellationToken, Task<IReadOnlyList<SizeAvailability>>>? availabilityProvider)
        {
            _item = item ?? throw new ArgumentNullException(nameof(item));
            _direction = direction;
            _storageId = storageId;
            _availabilityProvider = availabilityProvider;
            DialogWidth = 640;
            DialogHeight = 520;
            DisplayName = direction == DimensionDirection.In ? "Tallas (entrada)" : "Tallas (salida)";

            foreach (var s in initialState) _initialQty[s.SizeId] = s.Quantity;
        }

        public double DialogWidth { get; set; }
        public double DialogHeight { get; set; }
        public string ItemHeader => $"{_item.Code} · {_item.Name}";

        public bool IsInbound => _direction == DimensionDirection.In;
        public bool IsOutbound => _direction == DimensionDirection.Out;

        /// <summary>Si el item permite fracciones; usa N2, si no, N0.</summary>
        public bool AllowFraction => _item.AllowFraction;
        public string QuantityMask => _item.AllowFraction ? "N2" : "N0";

        public ObservableCollection<SizeRow> Rows { get; } = [];
        private readonly Dictionary<int, decimal> _initialQty = [];

        public IReadOnlyList<SizeDraft> Result { get; private set; } = [];

        public bool CanAccept
        {
            get
            {
                var taken = Rows.Where(r => r.Quantity > 0).ToList();
                if (taken.Count == 0) return false;
                if (Rows.Any(r => r.HasErrors)) return false;
                if (IsOutbound && taken.Any(r => r.Quantity > r.Available)) return false;
                return true;
            }
        }

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            await base.OnInitializeAsync(cancellationToken);
            await LoadRowsAsync(cancellationToken);
        }

        private async Task LoadRowsAsync(CancellationToken token)
        {
            Rows.Clear();
            if (IsInbound)
            {
                // Todas las tallas de la sizeCategory del ítem
                var values = _item.SizeCategory?.ItemSizeValues ?? [];
                foreach (var sv in values)
                {
                    var row = new SizeRow(_item.AllowFraction)
                    {
                        SizeId = sv.Id,
                        SizeName = sv.Name,
                        Available = 0m,
                        Quantity = _initialQty.TryGetValue(sv.Id, out var q) ? q : 0m
                    };
                    WireRow(row);
                    Rows.Add(row);
                }
            }
            else if (_availabilityProvider != null)
            {
                var rows = await _availabilityProvider(_item.Id, _storageId, token);
                foreach (var r in rows)
                {
                    var row = new SizeRow(_item.AllowFraction)
                    {
                        SizeId = r.SizeId,
                        SizeName = r.SizeName,
                        Available = r.AvailableQuantity,
                        Quantity = _initialQty.TryGetValue(r.SizeId, out var q) ? q : 0m
                    };
                    WireRow(row);
                    Rows.Add(row);
                }
            }
            NotifyOfPropertyChange(nameof(CanAccept));
            _isLoaded = true;
        }

        private bool _isLoaded;
        private bool _isDirty;

        private void WireRow(SizeRow row)
        {
            row.PropertyChanged += (_, __) =>
            {
                if (_isLoaded) _isDirty = true;
                NotifyOfPropertyChange(nameof(CanAccept));
            };
            row.ErrorsChanged += (_, __) => NotifyOfPropertyChange(nameof(CanAccept));
        }

        public async Task AcceptAsync()
        {
            if (!CanAccept) return;
            Result = [.. Rows.Where(r => r.Quantity > 0)
                .Select(r => new SizeDraft(r.SizeId, r.SizeName, r.Quantity))];
            await TryCloseAsync(true);
        }

        public Task CancelAsync()
        {
            if (_isDirty)
            {
                MessageBoxResult res = DXMessageBox.Show(
                    "Hay cambios sin guardar. ¿Desea descartarlos y salir?",
                    "Confirmar",
                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                if (res != MessageBoxResult.Yes) return Task.CompletedTask;
            }
            return TryCloseAsync(false);
        }
    }

    public class SizeRow : PropertyChangedBase, INotifyDataErrorInfo
    {
        private readonly bool _allowFraction;
        private readonly Dictionary<string, List<string>> _errors = [];

        public SizeRow(bool allowFraction = true)
        {
            _allowFraction = allowFraction;
        }

        public int SizeId { get; init; }
        public string SizeName { get; init; } = string.Empty;
        public decimal Available { get; init; }

        private decimal _quantity;
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity == value) return;
                _quantity = value;
                NotifyOfPropertyChange();
                ValidateQuantity();
            }
        }

        private void ValidateQuantity()
        {
            ClearErrors(nameof(Quantity));
            if (_quantity < 0)
                AddError(nameof(Quantity), "La cantidad no puede ser negativa.");
            else if (!_allowFraction && _quantity != decimal.Truncate(_quantity))
                AddError(nameof(Quantity), "Este ítem no admite fracciones.");
        }

        public bool HasErrors => _errors.Count > 0;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return Enumerable.Empty<string>();
            return _errors.TryGetValue(propertyName, out var list) ? list : Enumerable.Empty<string>();
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.TryGetValue(propertyName, out var list))
            {
                list = [];
                _errors[propertyName] = list;
            }
            if (!list.Contains(error))
            {
                list.Add(error);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.Remove(propertyName))
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
    }
}
