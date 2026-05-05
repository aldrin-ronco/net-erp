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
using NetErp.Helpers;
using System.Windows;
using System.Windows.Data;

namespace NetErp.Inventory.StockMovementsOut.ViewModels
{
    /// <summary>
    /// Modal de captura de lotes para SALIDAS.
    /// Lista lotes existentes con stock>0 ordenados por fecha de vencimiento ASC (FEFO).
    /// Columna "Cantidad" editable; valida Quantity ≤ Available via INotifyDataErrorInfo.
    /// </summary>
    public class LotsDimensionDialogOutViewModel : Screen
    {
        private readonly ItemGraphQLModel _item;
        private readonly int _storageId;
        private readonly Func<int, int, CancellationToken, Task<IReadOnlyList<LotAvailability>>> _availabilityProvider;
        private readonly Dictionary<int, decimal> _initialQty = [];

        public LotsDimensionDialogOutViewModel(
            ItemGraphQLModel item,
            int storageId,
            IEnumerable<LotDraft> initialState,
            Func<int, int, CancellationToken, Task<IReadOnlyList<LotAvailability>>> availabilityProvider)
        {
            _item = item ?? throw new ArgumentNullException(nameof(item));
            _storageId = storageId;
            _availabilityProvider = availabilityProvider ?? throw new ArgumentNullException(nameof(availabilityProvider));
            DialogWidth = 760;
            DialogHeight = 520;
            DisplayName = "Lotes (salida)";
            foreach (LotDraft l in initialState)
            {
                if (l.LotId is int id) _initialQty[id] = l.Quantity;
            }
        }

        public double DialogWidth { get; set; }
        public double DialogHeight { get; set; }
        public string ItemHeader => $"{_item.Code} · {_item.Name}";
        public bool AllowFraction => _item.AllowFraction;
        public string QuantityMask => _item.AllowFraction ? "N2" : "N0";
        public ObservableCollection<OutLotAvailabilityRow> Rows { get; } = [];

        // Vista filtrada por SearchTerm (LotNumber). Bindeada al DataGrid.
        public ICollectionView RowsView { get; private set; } = default!;

        public string SearchTerm
        {
            get;
            set
            {
                if (field == value) return;
                field = value ?? string.Empty;
                NotifyOfPropertyChange();
                RowsView?.Refresh();
            }
        } = string.Empty;

        public IReadOnlyList<LotDraft> Result { get; private set; } = [];

        public bool CanAccept
        {
            get
            {
                List<OutLotAvailabilityRow> taken = [.. Rows.Where(r => r.Quantity > 0)];
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
            IReadOnlyList<LotAvailability> available = await _availabilityProvider(_item.Id, _storageId, token);

            // FEFO: ordenar por ExpirationDate ASC. Nulls al final (sin vencimiento — usar último).
            IEnumerable<LotAvailability> sorted = available
                .OrderBy(a => a.ExpirationDate.HasValue ? 0 : 1)
                .ThenBy(a => a.ExpirationDate ?? DateTime.MaxValue)
                .ThenBy(a => a.LotNumber);

            Rows.Clear();
            foreach (LotAvailability r in sorted)
            {
                OutLotAvailabilityRow row = new(_item.AllowFraction, r.AvailableQuantity)
                {
                    LotId = r.LotId,
                    LotNumber = r.LotNumber,
                    ExpirationDate = r.ExpirationDate,
                    Quantity = _initialQty.TryGetValue(r.LotId, out decimal q) ? q : 0m
                };
                WireRow(row);
                Rows.Add(row);
            }
            RowsView = CollectionViewSource.GetDefaultView(Rows);
            RowsView.Filter = FilterRow;
            NotifyOfPropertyChange(nameof(RowsView));
            NotifyOfPropertyChange(nameof(CanAccept));
            _isLoaded = true;
        }

        private bool FilterRow(object o)
        {
            if (string.IsNullOrWhiteSpace(SearchTerm)) return true;
            if (o is not OutLotAvailabilityRow r) return false;
            string term = SearchTerm.Trim();
            return r.LotNumber.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        private bool _isLoaded;
        private bool _isDirty;

        private void WireRow(OutLotAvailabilityRow row)
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
            // Salidas: preservar LotNumber + ExpirationDate en el draft para que la
            // grilla principal los muestre sin esperar a un reload del server.
            Result = [.. Rows.Where(r => r.Quantity > 0)
                .Select(r => new LotDraft(r.LotId, r.LotNumber, r.ExpirationDate, r.Quantity))];
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
        public void Search()
        {
            // En modales el SetFocus por nombre falla — GetView() retorna Window
            // no UserControl. Toggle FocusBehavior.IsFocused via SearchFocus.
            SearchFocus = false;
            SearchFocus = true;
        }
        public bool CanSearch => true;

        public bool SearchFocus
        {
            get;
            set
            {
                field = value;
                NotifyOfPropertyChange();
            }
        }

        // FocusGrid en modal: GetView() retorna Window — la extension FocusFirstRow falla.
        // VM dispara evento; el code-behind del View resuelve el grid por nombre.
        public event System.Action? RequestFocusGrid;
        public void FocusGrid() => RequestFocusGrid?.Invoke();
        public bool CanFocusGrid => Rows.Count > 0;
    }

    /// <summary>
    /// Row del modal de lotes para SALIDAS. INotifyDataErrorInfo:
    /// reporta error si Quantity > Available o rompe regla de fracciones.
    /// </summary>
    public class OutLotAvailabilityRow : PropertyChangedBase, INotifyDataErrorInfo
    {
        private readonly bool _allowFraction;
        private readonly Dictionary<string, List<string>> _errors = [];

        public OutLotAvailabilityRow(bool allowFraction, decimal available)
        {
            _allowFraction = allowFraction;
            Available = available;
        }

        public int LotId { get; init; }
        public string LotNumber { get; init; } = string.Empty;
        public DateTime? ExpirationDate { get; init; }
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
