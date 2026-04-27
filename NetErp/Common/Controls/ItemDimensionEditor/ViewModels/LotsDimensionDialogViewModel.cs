using Caliburn.Micro;
using Models.Inventory;
using NetErp.UserControls.ItemDimensionEditor.DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

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
                    Rows.Add(new LotEntryRow { LotNumber = lot.LotNumber ?? string.Empty, ExpirationDate = lot.ExpirationDate, Quantity = lot.Quantity });
                if (Rows.Count == 0) Rows.Add(new LotEntryRow());
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
                    var valid = Rows.Where(r => !string.IsNullOrWhiteSpace(r.LotNumber) && r.Quantity > 0).ToList();
                    if (valid.Count == 0) return false;
                    var distinct = valid.Select(r => r.LotNumber.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                    if (distinct != valid.Count) return false; // duplicados
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

        public void AddRow()
        {
            var row = new LotEntryRow();
            row.PropertyChanged += (_, __) => NotifyOfPropertyChange(nameof(CanAccept));
            Rows.Add(row);
            NotifyOfPropertyChange(nameof(CanAccept));
        }

        public void RemoveRow(LotEntryRow row)
        {
            if (row == null) return;
            Rows.Remove(row);
            NotifyOfPropertyChange(nameof(CanAccept));
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

        public Task CancelAsync() => TryCloseAsync(false);
    }

    public class LotEntryRow : PropertyChangedBase
    {
        private string _lotNumber = string.Empty;
        public string LotNumber { get => _lotNumber; set { _lotNumber = value; NotifyOfPropertyChange(); } }

        private DateTime? _expirationDate;
        public DateTime? ExpirationDate { get => _expirationDate; set { _expirationDate = value; NotifyOfPropertyChange(); } }

        private decimal _quantity;
        public decimal Quantity { get => _quantity; set { _quantity = value; NotifyOfPropertyChange(); } }
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
