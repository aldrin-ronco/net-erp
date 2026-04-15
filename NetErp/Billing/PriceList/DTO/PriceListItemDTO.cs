using Caliburn.Micro;
using Models.Billing;
using Models.Books;
using Models.Inventory;
using NetErp.Billing.PriceList.ViewModels;
using System.Buffers;
using System.Windows.Media;
using System.Linq;
using System.Threading.Tasks;

namespace NetErp.Billing.PriceList.DTO
{
    public class PriceListItemDTO : PropertyChangedBase
    {
        private ItemGraphQLModel _catalogItem = new();
        public ItemGraphQLModel Item
        {
            get { return _catalogItem; }
            set
            {
                if (_catalogItem != value)
                {
                    _catalogItem = value;
                    NotifyOfPropertyChange(nameof(Item));
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
                    NotifyOfPropertyChange(nameof(Profit));
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
                    if (!_suppressNotifications && Context != null) _ = Context.AddModifiedProductAsync(this, nameof(ProfitMargin));
                }
            }
        }

        public decimal Profit
        {
            get
            {
                if (Cost == 0) return 0;
                PriceListGraphQLModel? pl = Context?.SelectedPriceList;
                if (pl is null) return 0;
                decimal ivaRate = (pl.IsTaxable && pl.PriceListIncludeTax && IVA > 0) ? IVA : 0;
                decimal priceSinIva = ivaRate > 0 ? Price / (1 + ivaRate / 100m) : Price;
                return priceSinIva - Cost;
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
                    NotifyOfPropertyChange(nameof(Profit));
                    if (!_suppressNotifications && Context != null) _ = Context.AddModifiedProductAsync(this, nameof(Price));
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
                    if (!_suppressNotifications && Context != null) _ = Context.AddModifiedProductAsync(this, nameof(MinimumPrice));
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
                    if (!_suppressNotifications && Context != null) _ = Context.AddModifiedProductAsync(this, nameof(DiscountMargin));
                }
            }
        }

        private decimal _quantity;

        public decimal Quantity
        {
            get { return _quantity; }
            set 
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    NotifyOfPropertyChange(nameof(Quantity));
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
                    NotifyOfPropertyChange(nameof(StatusIndicator));
                    NotifyOfPropertyChange(nameof(StatusTooltip));

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
            }
        }

        private string? _statusTooltip;
        public string? StatusTooltip
        {
            get => _statusTooltip;
            set
            {
                if (_statusTooltip != value)
                {
                    _statusTooltip = value;
                    NotifyOfPropertyChange(nameof(StatusTooltip));
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
                    OperationStatus.Retrying => Brushes.DarkOrange,
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

        public PriceListItemDTO()
        {
        }

        private bool _suppressNotifications = false;

        public void UpdatePropertySilently(string propertyName, object value)
        {
            _suppressNotifications = true;
            try
            {
                switch (propertyName)
                {
                    case nameof(Price):
                        Price = (decimal)value;
                        break;
                    case nameof(MinimumPrice):
                        MinimumPrice = (decimal)value;
                        break;
                    case nameof(ProfitMargin):
                        ProfitMargin = (decimal)value;
                        break;
                    case nameof(DiscountMargin):
                        DiscountMargin = (decimal)value;
                        break;
                }
            }
            finally
            {
                _suppressNotifications = false;
            }
        }

        public decimal IVA
        {
            get
            {
                if (Item?.AccountingGroup is null) return -1;
                var primaryTax = Item.AccountingGroup.SalesPrimaryTax;
                var secondaryTax = Item.AccountingGroup.SalesSecondaryTax;
                if (primaryTax?.TaxCategory?.Prefix == "IVA") return primaryTax.Rate;
                if (secondaryTax?.TaxCategory?.Prefix == "IVA") return secondaryTax.Rate;
                return -1;
            }
        }

        public UpdatePromotionViewModel UpdatePromotionContext { get; set; } 

        private bool _isChecked = false;

        public bool IsChecked
        {
            get { return _isChecked; }
            set 
            {
                if (_isChecked != value)
                {
                    _isChecked = value; 
                    NotifyOfPropertyChange(nameof(IsChecked));
                    if(value) UpdatePromotionContext?.AddItemsToShadowList(Item.Id);
                    else UpdatePromotionContext?.RemoveItemsFromShadowList(Item.Id);
                }
            }
        }


        public void ResolveCost(PriceListGraphQLModel priceList)
        {
            if (Item?.Stocks is null || !Item.Stocks.Any()) return;

            if (priceList.CostMode == "COST_BY_STORAGE" && priceList.Storage != null)
            {
                var stock = Item.Stocks.FirstOrDefault(s => s.Storage?.Id == priceList.Storage.Id);
                if (stock != null) Cost = stock.Cost;
            }
            else // USE_AVERAGE_COST
            {
                var stockWithAvg = Item.Stocks.FirstOrDefault();
                if (stockWithAvg != null) Cost = stockWithAvg.AverageCost;
            }
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
}
