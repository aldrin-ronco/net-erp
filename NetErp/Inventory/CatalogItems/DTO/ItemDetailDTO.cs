using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NetErp.Inventory.CatalogItems.DTO
{
	public class ItemDetailDTO : Screen, ICloneable
	{
		private string _id;

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

		private ItemDTO _parent;

		public ItemDTO Parent
		{
			get { return _parent; }
			set
			{
				if (_parent != value)
				{
					_parent = value;
					NotifyOfPropertyChange(nameof(Parent));
				}
			}
		}

		private ItemDTO _item;
		public ItemDTO Item
		{
			get { return _item; }
			set
			{
				if (_item != value)
				{
					_item = value;
					NotifyOfPropertyChange(nameof(Item));
				}
			}
		}

        public object Clone()
        {
			return this.MemberwiseClone();
        }
    }
}
