using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Inventory.CatalogItems.DTO
{
    public class EanCodeDTO: Screen, ICloneable
    {
		private string _id = string.Empty;

		public string Id
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

		private string _eanCode = string.Empty;

		public string EanCode
		{
			get { return _eanCode; }
			set 
			{
                if (_eanCode != value)
                {
                    _eanCode = value;
                    NotifyOfPropertyChange(nameof(EanCode));
                }
			}
		}

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
