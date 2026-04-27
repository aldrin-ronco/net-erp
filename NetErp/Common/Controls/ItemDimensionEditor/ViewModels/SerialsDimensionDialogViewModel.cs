using Caliburn.Micro;
using Models.Inventory;
using NetErp.UserControls.ItemDimensionEditor.DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetErp.UserControls.ItemDimensionEditor.ViewModels
{
    /// <summary>
    /// Modal de captura de seriales.
    /// Entradas (I): input texto + lista; ENTER apila un serial.
    /// Salidas (O): grilla de seriales disponibles + búsqueda por número/barcode (mueve a seleccionados).
    /// </summary>
    public class SerialsDimensionDialogViewModel : Screen
    {
        private readonly ItemGraphQLModel _item;
        private readonly DimensionDirection _direction;
        private readonly int _storageId;
        private readonly Func<int, int, CancellationToken, Task<IReadOnlyList<SerialAvailability>>>? _availabilityProvider;

        public SerialsDimensionDialogViewModel(
            ItemGraphQLModel item,
            DimensionDirection direction,
            int storageId,
            IEnumerable<SerialDraft> initialState,
            Func<int, int, CancellationToken, Task<IReadOnlyList<SerialAvailability>>>? availabilityProvider)
        {
            _item = item ?? throw new ArgumentNullException(nameof(item));
            _direction = direction;
            _storageId = storageId;
            _availabilityProvider = availabilityProvider;
            DialogWidth = 720;
            DialogHeight = 540;
            DisplayName = direction == DimensionDirection.In ? "Seriales (entrada)" : "Seriales (salida)";

            if (direction == DimensionDirection.In)
            {
                foreach (var s in initialState)
                    if (!string.IsNullOrWhiteSpace(s.SerialNumber)) NewSerials.Add(s.SerialNumber!);
            }
            else
            {
                foreach (var s in initialState)
                    if (s.SerialId is int id) _preselectedIds.Add(id);
            }
        }

        public double DialogWidth { get; set; }
        public double DialogHeight { get; set; }
        public string ItemHeader => $"{_item.Code} · {_item.Name}";

        public bool IsInbound => _direction == DimensionDirection.In;
        public bool IsOutbound => _direction == DimensionDirection.Out;

        // Entradas: lista mutable de seriales nuevos
        public ObservableCollection<string> NewSerials { get; } = [];

        // Salidas: disponibles + seleccionados
        public ObservableCollection<SerialAvailabilityRow> AvailableRows { get; } = [];
        public ObservableCollection<SerialAvailabilityRow> SelectedRows { get; } = [];
        private readonly HashSet<int> _preselectedIds = [];

        private string _captureInput = string.Empty;
        /// <summary>Input principal: número de serial (entrada) o término de búsqueda/barcode (salida).</summary>
        public string CaptureInput
        {
            get => _captureInput;
            set { _captureInput = value ?? string.Empty; NotifyOfPropertyChange(); }
        }

        private bool _captureFocus;
        public bool CaptureFocus { get => _captureFocus; set { _captureFocus = value; NotifyOfPropertyChange(); } }

        public IReadOnlyList<SerialDraft> Result { get; private set; } = [];

        public bool CanAccept => IsInbound ? NewSerials.Count > 0 : SelectedRows.Count > 0;

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            await base.OnInitializeAsync(cancellationToken);
            if (IsOutbound) await LoadAvailabilityAsync(cancellationToken);
            CaptureFocus = true;
        }

        private async Task LoadAvailabilityAsync(CancellationToken token)
        {
            if (_availabilityProvider == null) return;
            var rows = await _availabilityProvider(_item.Id, _storageId, token);
            AvailableRows.Clear();
            SelectedRows.Clear();
            foreach (var r in rows)
            {
                var row = new SerialAvailabilityRow { SerialId = r.SerialId, SerialNumber = r.SerialNumber, Cost = r.Cost };
                if (_preselectedIds.Contains(r.SerialId)) SelectedRows.Add(row);
                else AvailableRows.Add(row);
            }
            NotifyOfPropertyChange(nameof(CanAccept));
        }

        public void OnCaptureKeyDown(KeyEventArgs e)
        {
            if (e == null) return;
            if (e.Key == Key.Enter) { e.Handled = true; SubmitCapture(); }
        }

        /// <summary>ENTER en el input.</summary>
        public void SubmitCapture()
        {
            var token = _captureInput?.Trim();
            if (string.IsNullOrEmpty(token)) return;

            if (IsInbound)
            {
                if (NewSerials.Contains(token, StringComparer.OrdinalIgnoreCase)) { CaptureInput = string.Empty; return; }
                NewSerials.Add(token);
                CaptureInput = string.Empty;
                NotifyOfPropertyChange(nameof(CanAccept));
            }
            else
            {
                var match = AvailableRows.FirstOrDefault(r => string.Equals(r.SerialNumber, token, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    AvailableRows.Remove(match);
                    SelectedRows.Add(match);
                    CaptureInput = string.Empty;
                    NotifyOfPropertyChange(nameof(CanAccept));
                }
            }
        }

        public void RemoveNewSerial(string serial)
        {
            if (serial == null) return;
            NewSerials.Remove(serial);
            NotifyOfPropertyChange(nameof(CanAccept));
        }

        public void MoveToSelected(SerialAvailabilityRow row)
        {
            if (row == null) return;
            AvailableRows.Remove(row);
            SelectedRows.Add(row);
            NotifyOfPropertyChange(nameof(CanAccept));
        }

        public void MoveToAvailable(SerialAvailabilityRow row)
        {
            if (row == null) return;
            SelectedRows.Remove(row);
            AvailableRows.Add(row);
            NotifyOfPropertyChange(nameof(CanAccept));
        }

        public async Task AcceptAsync()
        {
            if (!CanAccept) return;
            if (IsInbound)
                Result = [.. NewSerials.Select(s => new SerialDraft(null, s))];
            else
                Result = [.. SelectedRows.Select(r => new SerialDraft(r.SerialId, null))];
            await TryCloseAsync(true);
        }

        public Task CancelAsync() => TryCloseAsync(false);
    }

    public class SerialAvailabilityRow
    {
        public int SerialId { get; init; }
        public string SerialNumber { get; init; } = string.Empty;
        public decimal Cost { get; init; }
    }
}
