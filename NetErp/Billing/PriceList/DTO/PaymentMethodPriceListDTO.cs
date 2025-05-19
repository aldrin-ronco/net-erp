using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Billing.PriceList.DTO
{
    public class PaymentMethodPriceListDTO: PropertyChangedBase
    {

		public int Id { get; set; }
        private string _abbreviation = string.Empty;

		public string Abbreviation
		{
			get { return _abbreviation; }
			set 
			{
				if(_abbreviation != value)
				{
					_abbreviation = value;
					NotifyOfPropertyChange(nameof(Abbreviation));
				}
			}
		}

		private string _name = string.Empty;

		public string Name
		{
			get { return _name; }
			set 
			{
				if (_name != value)
				{
					_name = value;
                    NotifyOfPropertyChange(nameof(Name));
				}
			}
		}

		private bool _isChecked = true;

		public bool IsChecked
		{
			get { return _isChecked; }
			set 
			{
                if (_isChecked != value)
                {
					_isChecked = value; 
                    NotifyOfPropertyChange(nameof(IsChecked));
                }
			}
		}

	}
}
