using Models.Billing;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Models.DTO.Billing
{
    public class LimitChangedEventArgs : EventArgs
    {
        public decimal OldValue { get; set; }
        public decimal NewValue { get; set; }
    }

    public class CreditLimitDTO : INotifyPropertyChanged
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
                    OnPropertyChanged();
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
                    OnPropertyChanged();
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
                    OnPropertyChanged();
                }
            }
        }

        private decimal _limit;

        public decimal Limit
        {
            get { return _limit; }
            set
            {
                if (_limit != value)
                {
                    decimal oldValue = _limit;
                    _limit = value;
                    OnPropertyChanged();
                    
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
                    OnPropertyChanged();
                }
            }
        }

        public decimal Available
        {
            get
            {
                return Limit - Used;
            }
        }

        public event EventHandler<LimitChangedEventArgs> LimitChanged;

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public CreditLimitDTO()
        {
        }
    }
}