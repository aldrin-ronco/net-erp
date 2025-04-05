using Caliburn.Micro;
using Models.Inventory;
using NetErp.Billing.PriceList.ViewModels;
using System.Buffers;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace NetErp.Billing.PriceList.DTO
{
    public class PriceListDetailDTO : PropertyChangedBase
    {
        private ItemGraphQLModel _catalogItem = new();
        public ItemGraphQLModel CatalogItem
        {
            get { return _catalogItem; }
            set
            {
                if (_catalogItem != value)
                {
                    _catalogItem = value;
                    NotifyOfPropertyChange(nameof(CatalogItem));
                }
            }
        }

        private MeasurementUnitGraphQLModel _measurement = new();
        public MeasurementUnitGraphQLModel Measurement
        {
            get { return _measurement; }
            set
            {
                if (_measurement != value)
                {
                    _measurement = value;
                    NotifyOfPropertyChange(nameof(Measurement));
                }
            }
        }

        private decimal _cost;
        public decimal Cost
        {
            get { return _cost; }
            set
            {
                if (_cost != value)
                {
                    _cost = value;
                    NotifyOfPropertyChange(nameof(Cost));
                }
            }
        }

        private decimal _profitMargin;
        public decimal ProfitMargin
        {
            get { return _profitMargin; }
            set
            {
                if (_profitMargin != value)
                {
                    _profitMargin = value;
                    NotifyOfPropertyChange(nameof(ProfitMargin));
                    Context?.AddModifiedProduct(this);
                }
            }
        }

        private decimal _price;
        public decimal Price
        {
            get { return _price; }
            set
            {
                if (_price != value)
                {
                    _price = value;
                    NotifyOfPropertyChange(nameof(Price));
                    Context?.AddModifiedProduct(this);
                }
            }
        }

        private decimal _minimumPrice;
        public decimal MinimumPrice
        {
            get { return _minimumPrice; }
            set
            {
                if (_minimumPrice != value)
                {
                    _minimumPrice = value;
                    NotifyOfPropertyChange(nameof(MinimumPrice));
                    Context?.AddModifiedProduct(this);
                }
            }
        }

        private decimal _discountMargin;
        public decimal DiscountMargin
        {
            get { return _discountMargin; }
            set
            {
                if (_discountMargin != value)
                {
                    _discountMargin = value;
                    NotifyOfPropertyChange(nameof(DiscountMargin));
                    Context?.AddModifiedProduct(this);
                }
            }
        }

        public PriceListMasterViewModel Context { get; set; }


        private OperationStatus _status = OperationStatus.Unchanged;
        public OperationStatus Status
        {
            get { return _status; }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    NotifyOfPropertyChange();

                    // Si pasa a guardado, programar un cambio a Unchanged tras un tiempo
                    if (value == OperationStatus.Saved)
                    {
                        ScheduleResetStatus();
                    }
                }
            }
        }

        // Propiedad para mostrar un indicador visual del estado
        public Brush StatusIndicator
        {
            get
            {
                return Status switch
                {
                    OperationStatus.Pending => Brushes.Orange,
                    OperationStatus.Saved => Brushes.Green,
                    OperationStatus.Failed => Brushes.Red,
                    _ => Brushes.Transparent
                };
            }
        }

        // Restaurar el estado visual después de un tiempo
        private void ScheduleResetStatus()
        {
            // Después de 5 segundos, cambiar a Unchanged
            _ = Task.Delay(5000).ContinueWith(_ =>
            {
                Execute.OnUIThread(() =>
                {
                    if (Status == OperationStatus.Saved)
                    {
                        Status = OperationStatus.Unchanged;
                    }
                });
            });
        }

        public PriceListDetailDTO()
        {
        }
    }

    public enum OperationStatus
    {
        Unchanged,
        Pending,
        Saved,
        Failed
    }
}
