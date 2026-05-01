using Caliburn.Micro;
using Models.Inventory;
using NetErp.UserControls.ItemDimensionEditor.DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace NetErp.UserControls.ItemDimensionEditor.ViewModels
{
    /// <summary>
    /// Modal de captura de seriales.
    /// Entradas (I): input texto + lista; ENTER apila un serial. Aceptar pre-valida contra
    /// master + drafts ajenos (vía <see cref="InboundSerialValidator"/>) y bloquea si hay conflictos.
    /// Salidas (O): grilla de seriales disponibles + búsqueda por número/barcode (mueve a seleccionados).
    /// </summary>
    public class SerialsDimensionDialogViewModel : Screen
    {
        private readonly ItemGraphQLModel _item;
        private readonly DimensionDirection _direction;
        private readonly int _storageId;
        private readonly Func<int, int, CancellationToken, Task<IReadOnlyList<SerialAvailability>>>? _availabilityProvider;
        private readonly InboundSerialValidator? _inboundValidator;
        private readonly int? _excludeStockMovementId;

        public SerialsDimensionDialogViewModel(
            ItemGraphQLModel item,
            DimensionDirection direction,
            int storageId,
            IEnumerable<SerialDraft> initialState,
            Func<int, int, CancellationToken, Task<IReadOnlyList<SerialAvailability>>>? availabilityProvider,
            InboundSerialValidator? inboundValidator = null,
            int? excludeStockMovementId = null)
        {
            _item = item ?? throw new ArgumentNullException(nameof(item));
            _direction = direction;
            _storageId = storageId;
            _availabilityProvider = availabilityProvider;
            _inboundValidator = inboundValidator;
            _excludeStockMovementId = excludeStockMovementId;
            DialogWidth = 760;
            DialogHeight = 560;
            DisplayName = direction == DimensionDirection.In ? "Seriales (entrada)" : "Seriales (salida)";

            if (direction == DimensionDirection.In)
            {
                foreach (var s in initialState)
                    if (!string.IsNullOrWhiteSpace(s.SerialNumber))
                        NewSerials.Add(new NewSerialRow { SerialNumber = s.SerialNumber!.Trim().ToUpperInvariant() });
            }
            else
            {
                foreach (var s in initialState)
                    if (s.SerialId is int id) _preselectedIds.Add(id);
            }

            NewSerialsView = (ListCollectionView)CollectionViewSource.GetDefaultView(NewSerials);
            NewSerialsView.Filter = item =>
            {
                if (string.IsNullOrWhiteSpace(_searchTerm)) return true;
                return item is NewSerialRow r && r.SerialNumber.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase);
            };

            NewSerials.CollectionChanged += OnNewSerialsCollectionChanged;
            foreach (var r in NewSerials) r.PropertyChanged += OnRowPropertyChanged;
        }

        private void OnNewSerialsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (NewSerialRow r in e.NewItems) r.PropertyChanged += OnRowPropertyChanged;
            if (e.OldItems != null)
                foreach (NewSerialRow r in e.OldItems) r.PropertyChanged -= OnRowPropertyChanged;
            NotifyOfPropertyChange(nameof(HasSelected));
            NotifyOfPropertyChange(nameof(HasConflicts));
            NotifyOfPropertyChange(nameof(AllSelected));
        }

        private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(NewSerialRow.IsSelected))
            {
                NotifyOfPropertyChange(nameof(HasSelected));
                NotifyOfPropertyChange(nameof(AllSelected));
            }
            else if (e.PropertyName == nameof(NewSerialRow.Status))
            {
                NotifyOfPropertyChange(nameof(HasConflicts));
            }
        }

        public ListCollectionView NewSerialsView { get; }

        public double DialogWidth { get; set; }
        public double DialogHeight { get; set; }
        public string ItemHeader => $"{_item.Code} · {_item.Name}";

        public bool IsInbound => _direction == DimensionDirection.In;
        public bool IsOutbound => _direction == DimensionDirection.Out;

        // Entradas: lista mutable de filas con estado de validación
        public ObservableCollection<NewSerialRow> NewSerials { get; } = [];

        // Salidas: disponibles + seleccionados
        public ObservableCollection<SerialAvailabilityRow> AvailableRows { get; } = [];
        public ObservableCollection<SerialAvailabilityRow> SelectedRows { get; } = [];
        private readonly HashSet<int> _preselectedIds = [];

        private string _captureInput = string.Empty;
        public string CaptureInput
        {
            get => _captureInput;
            set { _captureInput = value ?? string.Empty; NotifyOfPropertyChange(); }
        }

        private bool _captureFocus;
        public bool CaptureFocus { get => _captureFocus; set { _captureFocus = value; NotifyOfPropertyChange(); } }

        private string _searchTerm = string.Empty;
        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                _searchTerm = value ?? string.Empty;
                NotifyOfPropertyChange();
                NewSerialsView?.Refresh();
            }
        }

        private bool _isValidating;
        public bool IsValidating
        {
            get => _isValidating;
            set { _isValidating = value; NotifyOfPropertyChange(); NotifyOfPropertyChange(nameof(CanAccept)); }
        }

        public IReadOnlyList<SerialDraft> Result { get; private set; } = [];

        public bool CanAccept => !IsValidating && (IsInbound ? NewSerials.Count > 0 : SelectedRows.Count > 0);

        public bool HasSelected => NewSerials.Any(r => r.IsSelected);
        public bool HasConflicts => NewSerials.Any(IsConflict);

        /// <summary>Estado tri-estado para checkbox cabecera. true=todas, false=ninguna, null=parcial.</summary>
        public bool? AllSelected
        {
            get
            {
                if (NewSerials.Count == 0) return false;
                int selected = NewSerials.Count(r => r.IsSelected);
                if (selected == 0) return false;
                if (selected == NewSerials.Count) return true;
                return null;
            }
            set
            {
                if (value == null) return;
                bool target = value.Value;
                foreach (var r in NewSerials) r.IsSelected = target;
            }
        }

        private static bool IsConflict(NewSerialRow r) =>
            r.Status == SerialValidationStatus.AlreadyActive
            || r.Status == SerialValidationStatus.PreselectedInDraft
            || r.Status == SerialValidationStatus.DuplicateInList;

        public void RemoveSelected()
        {
            if (!HasSelected) return;
            var toRemove = NewSerials.Where(r => r.IsSelected).ToList();
            foreach (var r in toRemove) NewSerials.Remove(r);
            NotifyOfPropertyChange(nameof(CanAccept));
            ResetAllStatus();
        }

        public void RemoveConflicts()
        {
            if (!HasConflicts) return;
            var toRemove = NewSerials.Where(IsConflict).ToList();
            foreach (var r in toRemove) NewSerials.Remove(r);
            NotifyOfPropertyChange(nameof(CanAccept));
        }

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

        public void SubmitCapture()
        {
            var token = _captureInput?.Trim();
            if (string.IsNullOrEmpty(token)) return;

            if (IsInbound)
            {
                string normalized = token.ToUpperInvariant();
                if (NewSerials.Any(r => string.Equals(r.SerialNumber, normalized, StringComparison.OrdinalIgnoreCase)))
                {
                    CaptureInput = string.Empty;
                    return;
                }
                NewSerials.Add(new NewSerialRow { SerialNumber = normalized });
                CaptureInput = string.Empty;
                NotifyOfPropertyChange(nameof(CanAccept));
                ResetAllStatus();
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

        public void RemoveNewSerial(NewSerialRow row)
        {
            if (row == null) return;
            NewSerials.Remove(row);
            NotifyOfPropertyChange(nameof(CanAccept));
            ResetAllStatus();
        }

        /// <summary>Cualquier mutación a la lista invalida estados de validación previos.</summary>
        private void ResetAllStatus()
        {
            foreach (var r in NewSerials)
            {
                r.Status = SerialValidationStatus.Pending;
                r.Detail = null;
            }
        }

        public async Task AddFromCsvAsync()
        {
            if (!IsInbound) return;
            Microsoft.Win32.OpenFileDialog dlg = new()
            {
                Title = "Seleccionar CSV de seriales",
                Filter = "CSV files (*.csv)|*.csv|Todos (*.*)|*.*",
                CheckFileExists = true
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                string[] lines = await File.ReadAllLinesAsync(dlg.FileName);
                int added = 0, skipped = 0;
                foreach (string raw in lines)
                {
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    string token = raw.Split(',')[0].Trim().Trim('"').ToUpperInvariant();
                    if (string.IsNullOrEmpty(token)) continue;
                    if (NewSerials.Any(r => string.Equals(r.SerialNumber, token, StringComparison.OrdinalIgnoreCase))) { skipped++; continue; }
                    NewSerials.Add(new NewSerialRow { SerialNumber = token });
                    added++;
                }
                NotifyOfPropertyChange(nameof(CanAccept));
                ResetAllStatus();
                MessageBox.Show($"Cargados {added} seriales · omitidos {skipped} duplicados.",
                    "Carga CSV", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al leer CSV: {ex.Message}", "Carga CSV",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            {
                if (!await ValidateInboundAsync()) return;
                Result = [.. NewSerials.Select(r => new SerialDraft(null, r.SerialNumber))];
            }
            else
            {
                Result = [.. SelectedRows.Select(r => new SerialDraft(r.SerialId, null))];
            }
            await TryCloseAsync(true);
        }

        /// <summary>
        /// Pre-valida lista contra master + drafts ajenos. Marca filas con su estado.
        /// Retorna true si todas las filas quedan AVAILABLE.
        /// </summary>
        private async Task<bool> ValidateInboundAsync()
        {
            // 1. Detectar duplicados locales
            var groups = NewSerials.GroupBy(r => r.SerialNumber, StringComparer.OrdinalIgnoreCase).ToList();
            HashSet<NewSerialRow> localDuplicates = [];
            foreach (var g in groups)
            {
                if (g.Count() > 1)
                    foreach (var r in g) localDuplicates.Add(r);
            }

            // 2. Llamar al validador externo (si está disponible)
            IReadOnlyList<SerialInboundConflict> conflicts = [];
            if (_inboundValidator != null)
            {
                try
                {
                    IsValidating = true;
                    var unique = groups.Select(g => g.Key).ToArray();
                    conflicts = await _inboundValidator(_item.Id, unique, _excludeStockMovementId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al validar seriales: {ex.Message}",
                        "Validación", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                finally { IsValidating = false; }
            }

            var conflictMap = conflicts.ToDictionary(c => c.SerialNumber, c => c, StringComparer.OrdinalIgnoreCase);

            // 3. Aplicar estado por fila
            bool allOk = true;
            foreach (var row in NewSerials)
            {
                if (localDuplicates.Contains(row))
                {
                    row.Status = SerialValidationStatus.DuplicateInList;
                    row.Detail = "Repetido en la lista";
                    allOk = false;
                    continue;
                }
                if (conflictMap.TryGetValue(row.SerialNumber, out var c))
                {
                    row.Status = c.Status;
                    row.Detail = c.Status switch
                    {
                        SerialValidationStatus.AlreadyActive => $"Activo en bodega {c.StorageName ?? "?"}",
                        SerialValidationStatus.PreselectedInDraft => $"Reservado en borrador {(string.IsNullOrEmpty(c.DraftDocumentNumber) ? $"B#{c.DraftId}" : c.DraftDocumentNumber)}",
                        _ => "Conflicto"
                    };
                    allOk = false;
                    continue;
                }
                row.Status = SerialValidationStatus.Available;
                row.Detail = null;
            }
            return allOk;
        }

        public Task CancelAsync() => TryCloseAsync(false);
    }

    /// <summary>Fila de un serial nuevo con estado de validación reactivo.</summary>
    public class NewSerialRow : PropertyChangedBase
    {
        private string _serialNumber = string.Empty;
        public string SerialNumber
        {
            get => _serialNumber;
            set { _serialNumber = value ?? string.Empty; NotifyOfPropertyChange(); }
        }

        private SerialValidationStatus _status = SerialValidationStatus.Pending;
        public SerialValidationStatus Status
        {
            get => _status;
            set { _status = value; NotifyOfPropertyChange(); NotifyOfPropertyChange(nameof(DisplayText)); }
        }

        private string? _detail;
        public string? Detail
        {
            get => _detail;
            set { _detail = value; NotifyOfPropertyChange(); NotifyOfPropertyChange(nameof(DisplayText)); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; NotifyOfPropertyChange(); }
        }

        public string DisplayText => Status switch
        {
            SerialValidationStatus.Pending => "Pendiente",
            SerialValidationStatus.Available => "✓ Disponible",
            SerialValidationStatus.AlreadyActive => string.IsNullOrEmpty(Detail) ? "⚠ Activo en otra bodega" : $"⚠ {Detail}",
            SerialValidationStatus.PreselectedInDraft => string.IsNullOrEmpty(Detail) ? "⚠ Reservado en borrador" : $"⚠ {Detail}",
            SerialValidationStatus.DuplicateInList => "⊘ Duplicado en lista",
            _ => string.Empty
        };
    }

    public class SerialAvailabilityRow
    {
        public int SerialId { get; init; }
        public string SerialNumber { get; init; } = string.Empty;
        public decimal Cost { get; init; }
    }
}
