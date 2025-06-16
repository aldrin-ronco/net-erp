using Caliburn.Micro;
using Models.Inventory;
using NetErp.Billing.PriceList.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Billing.PriceList.DTO
{
    public class PromotionCatalogItemDTO: PropertyChangedBase
    {
		public AddPromotionProductsModalViewModel Context { get; set; }

        private int _id;

		public int Id
		{
			get { return _id; }
			set 
			{
				if(_id != value)
				{
					_id = value;
					NotifyOfPropertyChange(nameof(Id));
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

		private string _reference = string.Empty;

		public string Reference
		{
			get { return _reference; }
			set 
			{
				if(_reference != value)
				{
					_reference = value;
					NotifyOfPropertyChange(nameof(Reference));
                }
			}
		}

		private string _code = string.Empty;

		public string Code
		{
			get { return _code; }
			set 
			{
				if(_code != value)
				{
					_code = value;
					NotifyOfPropertyChange(nameof(Code));
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
					Context.NotifyOfPropertyChange(nameof(Context.CanAddItemList));
                    Context.NotifyOfPropertyChange(nameof(Context.CanRemoveAddedItemList));
                }
			}
		}


		private ItemSubCategoryGraphQLModel _subCategory = new();

		public ItemSubCategoryGraphQLModel SubCategory
		{
			get { return _subCategory; }
			set 
			{
				if(_subCategory != value)
				{
					_subCategory = value; 
					NotifyOfPropertyChange(nameof(SubCategory));
                }
			}
		}

	}
}
