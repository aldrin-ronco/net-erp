using Caliburn.Micro;
using Models.Inventory;
using System;
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
            set { if (field != value) { field = value; NotifyOfPropertyChange(); } }
        } = new();

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
            return dto;
        }
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
