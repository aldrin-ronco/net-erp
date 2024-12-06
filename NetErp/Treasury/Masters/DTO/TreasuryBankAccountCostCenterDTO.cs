using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Treasury.Masters.DTO
{
    public class TreasuryBankAccountCostCenterDTO: Screen
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
					NotifyOfPropertyChange(nameof(Id));
				}
			}
		}

		private string _name;

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
                }
			}
		}

	}
}
