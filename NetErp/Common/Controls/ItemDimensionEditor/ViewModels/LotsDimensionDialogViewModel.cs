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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace NetErp.UserControls.ItemDimensionEditor.ViewModels
{
    /// <summary>
    /// Modal de captura de lotes.
    /// Entradas (I): grilla editable; cada fila es <c>lotNumber + expirationDate? + quantity</c>.
    /// Salidas (O): grilla con lotes existentes en (item, storage) y cantidad a sacar.
    /// </summary>
    public class LotsDimensionDialogViewModel : Screen
    {
        private readonly ItemGraphQLModel _item;
        private readonly DimensionDirection _direction;
        private readonly int _storageId;
        private readonly Func<int, int, System.Threading.CancellationToken, Task<IReadOnlyList<LotAvailability>>>? _availabilityProvider;

        public LotsDimensionDialogViewModel(
            ItemGraphQLModel item,
            DimensionDirection direction,
            int storageId,
            IEnumerable<LotDraft> initialState,
            Func<int, int, System.Threading.CancellationToken, Task<IReadOnlyList<LotAvailability>>>? availabilityProvider)
        {
            _item = item ?? throw new ArgumentNullException(nameof(item));
            _direction = direction;
            _storageId = storageId;
            _availabilityProvider = availabilityProvider;
            DialogWidth = 720;
            DialogHeight = 520;
            DisplayName = direction == DimensionDirection.In ? "Lotes (entrada)" : "Lotes (salida)";

            if (direction == DimensionDirection.In)
            {
                foreach (var lot in initialState)
                {
                    LotEntryRow row = new(_item.AllowFraction) { LotNumber = lot.LotNumber ?? string.Empty, ExpirationDate = lot.ExpirationDate, Quantity = lot.Quantity };
                    WireRow(row);
                    Rows.Add(row);
                }
                if (Rows.Count == 0)
                {
                    LotEntryRow first = new(_item.AllowFraction);
                    WireRow(first);
                    Rows.Add(first);
                }
            }
            else
            {
                foreach (var lot in initialState)
                {
                    if (lot.LotId is int id)
                        _preselectedQty[id] = lot.Quantity;
                }
            }
        }

        public double DialogWidth { get; set; }
        public double DialogHeight { get; set; }

        /// <summary>Texto de cabecera del modal.</summary>
        public string ItemHeader => $"{_item.Code} · {_item.Name}";

        public bool IsInbound => _direction == DimensionDirection.In;
        public bool IsOutbound => _direction == DimensionDirection.Out;

        /// <summary>Si el item permite fracciones; usa N2, si no, N0.</summary>
        public bool AllowFraction => _item.AllowFraction;
        public string QuantityMask => _item.AllowFraction ? "N2" : "N0";

        /// <summary>Filas para entradas (lote nuevo).</summary>
        public ObservableCollection<LotEntryRow> Rows { get; } = [];

        /// <summary>Lotes disponibles (sólo salidas).</summary>
        public ObservableCollection<LotAvailabilityRow> AvailableRows { get; } = [];

        private readonly Dictionary<int, decimal> _preselectedQty = [];

        public IReadOnlyList<LotDraft> Result { get; private set; } = [];

        public bool CanAccept
        {
            get
            {
                if (IsInbound)
                {
                    if (Rows.Count == 0) return false;
                    // Validación dura: TODAS las filas deben estar completas.
                    if (Rows.Any(r => string.IsNullOrWhiteSpace(r.LotNumber) || r.Quantity <= 0)) return false;
                    if (Rows.Any(r => r.HasErrors)) return false;
                    int distinct = Rows.Select(r => r.LotNumber.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                    if (distinct != Rows.Count) return false; // duplicados
                    return true;
                }
                else
                {
                    var taken = AvailableRows.Where(r => r.Quantity > 0).ToList();
                    if (taken.Count == 0) return false;
                    if (taken.Any(r => r.Quantity > r.Available)) return false;
                    return true;
                }
            }
        }

        public string ValidationMessage
        {
            get
            {
                if (!IsInbound) return string.Empty;
                if (Rows.Count == 0) return "Agregue al menos un lote.";
                if (Rows.Any(r => string.IsNullOrWhiteSpace(r.LotNumber)))
                    return "Hay lotes sin número — complete o elimine la fila.";
                if (Rows.Any(r => r.HasErrors))
                    return "Hay lotes con formato inválido — corrija las filas marcadas.";
                if (Rows.Any(r => r.Quantity <= 0))
                    return "Hay lotes sin cantidad — complete o elimine la fila.";
                int distinct = Rows.Select(r => r.LotNumber.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                if (distinct != Rows.Count) return "Hay números de lote duplicados.";
                return string.Empty;
            }
        }

        protected override async Task OnInitializeAsync(System.Threading.CancellationToken cancellationToken)
        {
            await base.OnInitializeAsync(cancellationToken);
            if (IsOutbound) await LoadAvailabilityAsync(cancellationToken);
        }

        private async Task LoadAvailabilityAsync(System.Threading.CancellationToken token)
        {
            if (_availabilityProvider == null) return;
            var rows = await _availabilityProvider(_item.Id, _storageId, token);
            AvailableRows.Clear();
            foreach (var r in rows)
            {
                var row = new LotAvailabilityRow
                {
                    LotId = r.LotId,
                    LotNumber = r.LotNumber,
                    ExpirationDate = r.ExpirationDate,
                    Available = r.AvailableQuantity,
                    Quantity = _preselectedQty.TryGetValue(r.LotId, out var q) ? q : 0m
                };
                row.PropertyChanged += (_, __) => NotifyOfPropertyChange(nameof(CanAccept));
                AvailableRows.Add(row);
            }
            NotifyOfPropertyChange(nameof(CanAccept));
        }

        /// <summary>
        /// Disparado cuando AddRow detecta que ya existe una fila vacía y solicita
        /// enfocarla en lugar de crear otra.
        /// </summary>
        public event Action<int>? RequestFocusRow;

        public void AddRow()
        {
            for (int i = 0; i < Rows.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(Rows[i].LotNumber))
                {
                    RequestFocusRow?.Invoke(i);
                    return;
                }
            }
            var row = new LotEntryRow(_item.AllowFraction);
            WireRow(row);
            Rows.Add(row);
            _isDirty = true;
            NotifyAcceptState();
        }

        public void RemoveRow(LotEntryRow row)
        {
            if (row == null) return;
            UnwireRow(row);
            Rows.Remove(row);
            _isDirty = true;
            NotifyAcceptState();
        }

        private void WireRow(LotEntryRow row)
        {
            row.PropertyChanged += OnRowChanged;
            row.ErrorsChanged += OnRowErrorsChanged;
        }

        private void UnwireRow(LotEntryRow row)
        {
            row.PropertyChanged -= OnRowChanged;
            row.ErrorsChanged -= OnRowErrorsChanged;
        }

        private void OnRowChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _isDirty = true;
            NotifyAcceptState();
        }
        private void OnRowErrorsChanged(object? sender, DataErrorsChangedEventArgs e) => NotifyAcceptState();

        private bool _isDirty;

        private void NotifyAcceptState()
        {
            NotifyOfPropertyChange(nameof(CanAccept));
            NotifyOfPropertyChange(nameof(ValidationMessage));
        }

        public async Task AcceptAsync()
        {
            if (!CanAccept) return;
            if (IsInbound)
            {
                Result = [.. Rows.Where(r => !string.IsNullOrWhiteSpace(r.LotNumber) && r.Quantity > 0)
                    .Select(r => new LotDraft(null, r.LotNumber.Trim(), r.ExpirationDate, r.Quantity))];
            }
            else
            {
                Result = [.. AvailableRows.Where(r => r.Quantity > 0)
                    .Select(r => new LotDraft(r.LotId, null, null, r.Quantity))];
            }
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

    public class LotEntryRow : PropertyChangedBase, INotifyDataErrorInfo
    {
        private static readonly Regex LotPattern = new(@"^[A-Z0-9.\-_/]+$", RegexOptions.Compiled);
        private const int MaxLotLength = 30;

        private readonly bool _allowFraction;
        private readonly Dictionary<string, List<string>> _errors = [];

        public LotEntryRow(bool allowFraction = true)
        {
            _allowFraction = allowFraction;
        }

        private string _lotNumber = string.Empty;
        public string LotNumber
        {
            get => _lotNumber;
            set
            {
                string normalized = value ?? string.Empty;
                if (_lotNumber == normalized) return;
                _lotNumber = normalized;
                NotifyOfPropertyChange();
                ValidateLotNumber();
            }
        }

        private DateTime? _expirationDate;
        public DateTime? ExpirationDate
        {
            get => _expirationDate;
            set { _expirationDate = value; NotifyOfPropertyChange(); }
        }

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

        private void ValidateLotNumber()
        {
            ClearErrors(nameof(LotNumber));
            if (string.IsNullOrEmpty(_lotNumber))
            {
                AddError(nameof(LotNumber), "El número de lote es obligatorio.");
                return;
            }
            if (_lotNumber.Length > MaxLotLength)
                AddError(nameof(LotNumber), $"Máximo {MaxLotLength} caracteres.");
            if (_lotNumber.Any(char.IsWhiteSpace))
                AddError(nameof(LotNumber), "El número de lote no puede contener espacios.");
            else if (!LotPattern.IsMatch(_lotNumber))
                AddError(nameof(LotNumber), "Solo se permiten letras, números y . - _ /");
        }

        private void ValidateQuantity()
        {
            ClearErrors(nameof(Quantity));
            if (_quantity <= 0)
                AddError(nameof(Quantity), "La cantidad debe ser mayor a cero.");
            else if (!_allowFraction && _quantity != decimal.Truncate(_quantity))
                AddError(nameof(Quantity), "Este ítem no admite fracciones.");
        }

        public bool HasErrors => _errors.Count > 0;
        public event System.EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

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

    public class LotAvailabilityRow : PropertyChangedBase
    {
        public int LotId { get; init; }
        public string LotNumber { get; init; } = string.Empty;
        public DateTime? ExpirationDate { get; init; }
        public decimal Available { get; init; }

        private decimal _quantity;
        public decimal Quantity { get => _quantity; set { _quantity = value; NotifyOfPropertyChange(); } }
    }
}
