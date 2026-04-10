using Caliburn.Micro;
using Models.Billing;
using Models.DTO.Billing;
using NetErp.Billing.CreditLimit.ViewModels;
using NetErp.Billing.PriceList.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Billing.CreditLimit.DTO
{
    public class CreditLimitDTO : PropertyChangedBase
    {
        private int _id;

        public int Id
        {
            get { return _id; }
            set
            {
                if (_id != value)
                {
                    _id = value;
                }
            }
        }

        private CustomerGraphQLModel _customer = new();

        public CustomerGraphQLModel Customer
        {
            get { return _customer; }
            set
            {
                if (_customer != value)
                {
                    _customer = value;
                    NotifyOfPropertyChange(nameof(Customer));

                }
            }
        }

        private decimal _originalLimit;

        public decimal OriginalLimit
        {
            get { return _originalLimit; }
            set
            {
                if (_originalLimit != value)
                {
                    _originalLimit = value;
                    NotifyOfPropertyChange(nameof(OriginalLimit));

                }
            }
        }

        private decimal _creditLimit;

        public decimal CreditLimit
        {
            get { return _creditLimit; }
            set
            {
                if (_creditLimit != value)
                {
                    decimal oldValue = _creditLimit;
                    NotifyOfPropertyChange(nameof(CreditLimit));

                    _creditLimit = value;
                    if (!_suppressNotifications) Context?.AddModifiedLimit(this, nameof(CreditLimit));

                    // Solo notifica el cambio, sin validar ni mostrar UI
                      LimitChanged?.Invoke(this, new LimitChangedEventArgs
                      {
                          OldValue = oldValue,
                          NewValue = value
                      }); 
                }
            }
        }

        private decimal _used;

        public decimal Used
        {
            get { return _used; }
            set
            {
                if (_used != value)
                {
                    _used = value;
                    NotifyOfPropertyChange(nameof(Used));
                }
            }
        }

        public decimal Available
        {
            get
            {
                return CreditLimit - Used;
            }
        }

        public event EventHandler<LimitChangedEventArgs> LimitChanged;
        public CreditLimitMasterViewModel Context { get; set; }

        private bool _suppressNotifications = false;

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

    }

    public enum OperationStatus
    {
        Unchanged,
        Pending,
        Saved,
        Failed,
        Retrying
    }
    public class LimitChangedEventArgs : EventArgs
    {
        public decimal OldValue { get; set; }
        public decimal NewValue { get; set; }
    }
}
