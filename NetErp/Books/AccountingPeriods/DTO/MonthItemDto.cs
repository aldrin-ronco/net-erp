using Caliburn.Micro;
using Common.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace NetErp.Books.AccountingPeriods.DTO
{
    public class MonthItemDto :  INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }

        private string _status;

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public string Status
        {
            get { return _status; }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();

                }
            }
        }
    }
}
