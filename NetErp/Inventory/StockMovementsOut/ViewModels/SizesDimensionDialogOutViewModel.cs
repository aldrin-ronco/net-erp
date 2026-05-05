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

namespace NetErp.Inventory.StockMovementsOut.ViewModels
{
    /// <summary>
    /// Modal de captura de tallas para SALIDAS.
    /// Lista todas las tallas de la sizeCategory del item (igual que entradas).
    /// Agrega columna informativa "Disponible" — restringe Quantity ≤ Available.
    /// VM dedicado al flujo de salidas — no comparte con entradas.
    /// </summary>
    public class SizesDimensionDialogOutViewModel : Screen
    {
        private readonly ItemGraphQLModel _item;
        private readonly int _storageId;
        private readonly Func<int, int, CancellationToken, Task<IReadOnlyList<SizeAvailability>>> _availabilityProvider;
        private readonly Dictionary<int, decimal> _initialQty = [];

        public SizesDimensionDialogOutViewModel(
            ItemGraphQLModel item,
            int storageId,
            IEnumerable<SizeDraft> initialState,
            Func<int, int, CancellationToken, Task<IReadOnlyList<SizeAvailability>>> availabilityProvider)
        {
            _item = item ?? throw new ArgumentNullException(nameof(item));
            _storageId = storageId;
            _availabilityProvider = availabilityProvider ?? throw new ArgumentNullException(nameof(availabilityProvider));
            DialogWidth = 720;
            DialogHeight = 520;
            DisplayName = "Tallas (salida)";
            foreach (SizeDraft s in initialState) _initialQty[s.SizeId] = s.Quantity;
        }

        public double DialogWidth { get; set; }
        public double DialogHeight { get; set; }
        public string ItemHeader => $"{_item.Code} · {_item.Name}";
        public bool AllowFraction => _item.AllowFraction;
        public string QuantityMask => _item.AllowFraction ? "N2" : "N0";
        public ObservableCollection<OutSizeRow> Rows { get; } = [];
        public IReadOnlyList<SizeDraft> Result { get; private set; } = [];

        public bool CanAccept
        {
            get
            {
                List<OutSizeRow> taken = [.. Rows.Where(r => r.Quantity > 0)];
                if (taken.Count == 0) return false;
                if (Rows.Any(r => r.HasErrors)) return false;
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
            // Disponibilidad por sizeId. Puede no incluir tallas sin stock.
            IReadOnlyList<SizeAvailability> available = await _availabilityProvider(_item.Id, _storageId, token);
            Dictionary<int, decimal> availableBySizeId = available.ToDictionary(a => a.SizeId, a => a.AvailableQuantity);

            Rows.Clear();
            // Iterar TODAS las tallas de la sizeCategory (igual que entradas).
            // Tallas sin stock → Available=0, Quantity bloqueada por validación.
            IEnumerable<ItemSizeValueGraphQLModel> values = _item.SizeCategory?.ItemSizeValues ?? [];
            foreach (ItemSizeValueGraphQLModel sv in values)
            {
                decimal availQty = availableBySizeId.TryGetValue(sv.Id, out decimal a) ? a : 0m;
                OutSizeRow row = new(_item.AllowFraction, availQty)
                {
                    SizeId = sv.Id,
                    SizeName = sv.Name,
                    Quantity = _initialQty.TryGetValue(sv.Id, out decimal q) ? q : 0m
                };
                WireRow(row);
                Rows.Add(row);
            }
            NotifyOfPropertyChange(nameof(CanAccept));
            _isLoaded = true;
        }

        private bool _isLoaded;
        private bool _isDirty;

        private void WireRow(OutSizeRow row)
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

        // Aliases AppCommands
        public Task SaveAsync() => AcceptAsync();
        public bool CanSave => CanAccept;
        public Task CloseAsync() => CancelAsync();
        public bool CanClose => true;

        // FocusGrid en modal: GetView() retorna Window — extension FocusFirstRow falla.
        // VM dispara evento; el code-behind del View resuelve el grid por nombre.
        public event System.Action? RequestFocusGrid;
        public void FocusGrid() => RequestFocusGrid?.Invoke();
        public bool CanFocusGrid => Rows.Count > 0;
    }

    /// <summary>
    /// Row del modal de tallas para SALIDAS. Implementa INotifyDataErrorInfo:
    /// reporta error si Quantity > Available o si rompe regla de fracciones.
    /// </summary>
    public class OutSizeRow : PropertyChangedBase, INotifyDataErrorInfo
    {
        private readonly bool _allowFraction;
        private readonly Dictionary<string, List<string>> _errors = [];

        public OutSizeRow(bool allowFraction, decimal available)
        {
            _allowFraction = allowFraction;
            Available = available;
        }

        public int SizeId { get; init; }
        public string SizeName { get; init; } = string.Empty;
        public decimal Available { get; }

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
            else if (_quantity > Available)
                AddError(nameof(Quantity), $"Excede stock disponible ({Available:N2}).");
            NotifyOfPropertyChange(nameof(QuantityErrorTooltip));
        }

        public string QuantityErrorTooltip
        {
            get
            {
                if (!_errors.TryGetValue(nameof(Quantity), out List<string>? list)) return string.Empty;
                return string.Join("\n", list);
            }
        }

        public bool HasErrors => _errors.Count > 0;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return Enumerable.Empty<string>();
            return _errors.TryGetValue(propertyName, out List<string>? list) ? list : Enumerable.Empty<string>();
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.TryGetValue(propertyName, out List<string>? list))
            {
                list = [];
                _errors[propertyName] = list;
            }
            if (!list.Contains(error))
            {
                list.Add(error);
                NotifyOfPropertyChange(nameof(HasErrors));
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.Remove(propertyName))
            {
                NotifyOfPropertyChange(nameof(HasErrors));
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }
    }
}
