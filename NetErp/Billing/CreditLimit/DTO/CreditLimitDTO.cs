using Caliburn.Micro;
using Models.Billing;
using NetErp.Billing.CreditLimit.ViewModels;
using System;

namespace NetErp.Billing.CreditLimit.DTO
{
    public class CreditLimitDTO : PropertyChangedBase
    {
        private bool _suppressLimitChangedEvent;

        public int Id
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Id));
                }
            }
        }

        public CustomerGraphQLModel Customer
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Customer));
                }
            }
        } = new();

        public decimal OriginalLimit
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(OriginalLimit));
                }
            }
        }

        public decimal CreditLimit
        {
            get;
            set
            {
                if (field == value) return;
                decimal oldValue = field;
                field = value;
                NotifyOfPropertyChange(nameof(CreditLimit));
                NotifyOfPropertyChange(nameof(Available));

                if (_suppressLimitChangedEvent) return;
                LimitChanged?.Invoke(this, new LimitChangedEventArgs
                {
                    OldValue = oldValue,
                    NewValue = value
                });
            }
        }

        public decimal Used
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Used));
                    NotifyOfPropertyChange(nameof(Available));
                }
            }
        }

        public decimal Available => CreditLimit - Used;

        public event EventHandler<LimitChangedEventArgs>? LimitChanged;
        public CreditLimitMasterViewModel? Context { get; set; }

        public void SetCreditLimitSilently(decimal value)
        {
            _suppressLimitChangedEvent = true;
            try { CreditLimit = value; }
            finally { _suppressLimitChangedEvent = false; }
        }

        public OperationStatus Status
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange();

                    if (value == OperationStatus.Saved || value == OperationStatus.Unchanged)
                    {
                        StatusTooltip = null;
                    }
                }
            }
        } = OperationStatus.Unchanged;

        public string? StatusTooltip
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(StatusTooltip));
                }
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

    public class LimitChangedEventArgs : EventArgs
    {
        public decimal OldValue { get; init; }
        public decimal NewValue { get; init; }
    }
}
