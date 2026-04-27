using Caliburn.Micro;
using Models.Inventory;
using NetErp.UserControls.ItemDimensionEditor.DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        public ObservableCollection<SizeRow> Rows { get; } = [];
        private readonly Dictionary<int, decimal> _initialQty = [];

        public IReadOnlyList<SizeDraft> Result { get; private set; } = [];

        public bool CanAccept
        {
            get
            {
                var taken = Rows.Where(r => r.Quantity > 0).ToList();
                if (taken.Count == 0) return false;
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
                    var row = new SizeRow
                    {
                        SizeId = sv.Id,
                        SizeName = sv.Name,
                        Available = 0m,
                        Quantity = _initialQty.TryGetValue(sv.Id, out var q) ? q : 0m
                    };
                    row.PropertyChanged += (_, __) => NotifyOfPropertyChange(nameof(CanAccept));
                    Rows.Add(row);
                }
            }
            else if (_availabilityProvider != null)
            {
                var rows = await _availabilityProvider(_item.Id, _storageId, token);
                foreach (var r in rows)
                {
                    var row = new SizeRow
                    {
                        SizeId = r.SizeId,
                        SizeName = r.SizeName,
                        Available = r.AvailableQuantity,
                        Quantity = _initialQty.TryGetValue(r.SizeId, out var q) ? q : 0m
                    };
                    row.PropertyChanged += (_, __) => NotifyOfPropertyChange(nameof(CanAccept));
                    Rows.Add(row);
                }
            }
            NotifyOfPropertyChange(nameof(CanAccept));
        }

        public async Task AcceptAsync()
        {
            if (!CanAccept) return;
            Result = [.. Rows.Where(r => r.Quantity > 0)
                .Select(r => new SizeDraft(r.SizeId, r.SizeName, r.Quantity))];
            await TryCloseAsync(true);
        }

        public Task CancelAsync() => TryCloseAsync(false);
    }

    public class SizeRow : PropertyChangedBase
    {
        public int SizeId { get; init; }
        public string SizeName { get; init; } = string.Empty;
        public decimal Available { get; init; }

        private decimal _quantity;
        public decimal Quantity { get => _quantity; set { _quantity = value; NotifyOfPropertyChange(); } }
    }
}
