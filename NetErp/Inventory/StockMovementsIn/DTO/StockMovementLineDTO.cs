using Caliburn.Micro;
using Models.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NetErp.Inventory.StockMovementsIn.DTO
{
    /// <summary>
    /// DTO observable que envuelve una <see cref="StockMovementLineGraphQLModel"/>.
    /// Sus setters disparan <see cref="LineChanged"/> con old/new value para que
    /// el ViewModel valide y delegue persistencia al BackgroundQueueService
    /// (patrón consistente con CreditLimitDTO / PriceListItemDTO).
    /// </summary>
    public class StockMovementLineDTO : PropertyChangedBase
    {
        private bool _suppressChangedEvent;

        public int Id
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(); } }
        }

        public int StockMovementId
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(); } }
        }

        public ItemGraphQLModel Item
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange(nameof(QuantityMask));
                    NotifyOfPropertyChange(nameof(QuantityFormat));
                    NotifyOfPropertyChange(nameof(QuantityDisplay));
                }
            }
        } = new();

        /// <summary>Máscara numérica (N0 entero / N2 decimal) según <c>Item.AllowFraction</c>.</summary>
        public string QuantityMask => Item?.AllowFraction == true ? "N2" : "N0";

        /// <summary>Format string para visualización (`N0` / `N2`) según <c>Item.AllowFraction</c>.</summary>
        public string QuantityFormat => Item?.AllowFraction == true ? "N2" : "N0";

        /// <summary>Cantidad formateada según <c>QuantityFormat</c> — listo para binding directo en TextBlock.</summary>
        public string QuantityDisplay => Quantity.ToString(QuantityFormat);

        public decimal Quantity
        {
            get;
            set
            {
                if (field == value) return;
                decimal oldValue = field;
                field = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(Subtotal));
                NotifyOfPropertyChange(nameof(QuantityDisplay));
                if (_suppressChangedEvent) return;
                LineChanged?.Invoke(this, new LineChangedEventArgs
                {
                    PropertyName = nameof(Quantity),
                    OldValue = oldValue,
                    NewValue = value
                });
            }
        }

        public decimal? UnitCost
        {
            get;
            set
            {
                if (field == value) return;
                decimal? oldValue = field;
                field = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(Subtotal));
                if (_suppressChangedEvent) return;
                LineChanged?.Invoke(this, new LineChangedEventArgs
                {
                    PropertyName = nameof(UnitCost),
                    OldValue = oldValue ?? 0m,
                    NewValue = value ?? 0m
                });
            }
        }

        public int DisplayOrder
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(); } }
        }

        public decimal Subtotal => Quantity * (UnitCost ?? 0m);

        public OperationStatus Status
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(StatusIndicator));
                if (value == OperationStatus.Saved)
                {
                    StatusTooltip = null;
                    ScheduleResetStatus();
                }
                else if (value == OperationStatus.Unchanged)
                {
                    StatusTooltip = null;
                }
            }
        } = OperationStatus.Unchanged;

        public string? StatusTooltip
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(); } }
        }

        public Brush StatusIndicator => Status switch
        {
            OperationStatus.Pending => Brushes.Orange,
            OperationStatus.Retrying => Brushes.DarkOrange,
            OperationStatus.Saved => Brushes.Green,
            OperationStatus.Failed => Brushes.Red,
            _ => Brushes.Transparent
        };

        public event EventHandler<LineChangedEventArgs>? LineChanged;

        public void SetQuantitySilently(decimal value)
        {
            _suppressChangedEvent = true;
            try { Quantity = value; }
            finally { _suppressChangedEvent = false; }
        }

        public void SetUnitCostSilently(decimal? value)
        {
            _suppressChangedEvent = true;
            try { UnitCost = value; }
            finally { _suppressChangedEvent = false; }
        }

        private void ScheduleResetStatus()
        {
            _ = Task.Delay(3000).ContinueWith(_ =>
            {
                Execute.OnUIThread(() =>
                {
                    if (Status == OperationStatus.Saved)
                        Status = OperationStatus.Unchanged;
                });
            });
        }

        public IReadOnlyList<DimensionLotRow> LotRows { get; private set; } = [];
        public IReadOnlyList<DimensionSerialRow> SerialRows { get; private set; } = [];
        public IReadOnlyList<DimensionSizeRow> SizeRows { get; private set; } = [];

        public LineDimensionType DimensionType { get; private set; } = LineDimensionType.Generic;
        public bool HasDimensions => DimensionType != LineDimensionType.Generic;
        public bool IsLot => DimensionType == LineDimensionType.Lot;
        public bool IsSerial => DimensionType == LineDimensionType.Serial;
        public bool IsSize => DimensionType == LineDimensionType.Size;

        /// <summary>
        /// Aplica las preselecciones capturadas localmente (optimistic add). Mapea drafts del
        /// UC a las rows visualizadas en grilla. Notifica visibilidad por dimensión.
        /// </summary>
        public void ApplyLocalDimensions(
            IReadOnlyList<NetErp.UserControls.ItemDimensionEditor.DTO.LotDraft> lots,
            IReadOnlyList<NetErp.UserControls.ItemDimensionEditor.DTO.SerialDraft> serials,
            IReadOnlyList<NetErp.UserControls.ItemDimensionEditor.DTO.SizeDraft> sizes)
        {
            if (lots != null && lots.Count > 0)
            {
                DimensionType = LineDimensionType.Lot;
                LotRows = [.. lots.Select(l => new DimensionLotRow
                {
                    LotId = l.LotId,
                    LotNumber = l.LotNumber ?? "(s/lote)",
                    Quantity = l.Quantity,
                    ExpirationDate = l.ExpirationDate
                })];
            }
            else if (serials != null && serials.Count > 0)
            {
                DimensionType = LineDimensionType.Serial;
                SerialRows = [.. serials.Select(s => new DimensionSerialRow
                {
                    SerialId = s.SerialId,
                    SerialNumber = s.SerialNumber ?? "(s/serial)"
                })];
            }
            else if (sizes != null && sizes.Count > 0)
            {
                DimensionType = LineDimensionType.Size;
                SizeRows = [.. sizes.Select(s => new DimensionSizeRow
                {
                    SizeId = s.SizeId,
                    SizeName = s.SizeName,
                    Quantity = s.Quantity
                })];
            }
            NotifyOfPropertyChange(nameof(DimensionType));
            NotifyOfPropertyChange(nameof(HasDimensions));
            NotifyOfPropertyChange(nameof(IsLot));
            NotifyOfPropertyChange(nameof(IsSerial));
            NotifyOfPropertyChange(nameof(IsSize));
            NotifyOfPropertyChange(nameof(LotRows));
            NotifyOfPropertyChange(nameof(SerialRows));
            NotifyOfPropertyChange(nameof(SizeRows));
        }

        public static StockMovementLineDTO FromModel(StockMovementLineGraphQLModel m)
        {
            StockMovementLineDTO dto = new()
            {
                Id = m.Id,
                StockMovementId = m.StockMovementId,
                Item = m.Item,
                DisplayOrder = m.DisplayOrder,
                Status = OperationStatus.Unchanged
            };
            dto.SetQuantitySilently(m.Quantity);
            dto.SetUnitCostSilently(m.UnitCost);

            // Preselecciones → DTO rows + tipo dimensión.
            if (m.LotPreselections != null && m.LotPreselections.Any())
            {
                dto.DimensionType = LineDimensionType.Lot;
                dto.LotRows = [.. m.LotPreselections
                    .OrderBy(l => l.DisplayOrder)
                    .Select(l => new DimensionLotRow
                    {
                        LotId = l.LotId,
                        LotNumber = l.LotNumber ?? l.Lot?.LotNumber ?? "(s/lote)",
                        Quantity = l.Quantity,
                        ExpirationDate = l.ExpirationDate
                    })];
            }
            else if (m.SerialPreselections != null && m.SerialPreselections.Any())
            {
                dto.DimensionType = LineDimensionType.Serial;
                dto.SerialRows = [.. m.SerialPreselections
                    .OrderBy(s => s.DisplayOrder)
                    .Select(s => new DimensionSerialRow
                    {
                        SerialId = s.SerialId,
                        SerialNumber = s.SerialNumber ?? s.Serial?.SerialNumber ?? "(s/serial)"
                    })];
            }
            else if (m.SizePreselections != null && m.SizePreselections.Any())
            {
                dto.DimensionType = LineDimensionType.Size;
                dto.SizeRows = [.. m.SizePreselections
                    .OrderBy(s => s.DisplayOrder)
                    .Select(s => new DimensionSizeRow
                    {
                        SizeId = s.Size?.Id ?? s.SizeId,
                        SizeName = s.Size?.Name ?? "(s/talla)",
                        Quantity = s.Quantity
                    })];
            }
            return dto;
        }
    }

    public enum LineDimensionType { Generic, Lot, Serial, Size }

    public class DimensionLotRow
    {
        public int? LotId { get; init; }
        public string LotNumber { get; init; } = string.Empty;
        public decimal Quantity { get; init; }
        public DateTime? ExpirationDate { get; init; }
        public string Display => ExpirationDate.HasValue
            ? $"{LotNumber} · {Quantity:0.##} UND · vence {ExpirationDate.Value:yyyy-MM-dd}"
            : $"{LotNumber} · {Quantity:0.##} UND";
    }

    public class DimensionSerialRow
    {
        public int? SerialId { get; init; }
        public string SerialNumber { get; init; } = string.Empty;
    }

    public class DimensionSizeRow
    {
        public int SizeId { get; init; }
        public string SizeName { get; init; } = string.Empty;
        public decimal Quantity { get; init; }
        public string Display => $"{SizeName}, {Quantity:0.##} UND";
    }

    public enum OperationStatus
    {
        Unchanged,
        Pending,
        Saved,
        Failed,
        Retrying
    }

    public class LineChangedEventArgs : EventArgs
    {
        public string PropertyName { get; init; } = string.Empty;
        public decimal OldValue { get; init; }
        public decimal NewValue { get; init; }
    }
}
