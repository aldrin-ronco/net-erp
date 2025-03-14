﻿using Caliburn.Micro;
using NetErp.Inventory.CatalogItems.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Inventory.CatalogItems.DTO
{
    public class ItemCategoryDTO: Screen, ICatalogItem
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

		private ObservableCollection<ItemSubCategoryDTO> _subCategories = [];

		public ObservableCollection<ItemSubCategoryDTO> SubCategories
		{
			get { return _subCategories; }
			set 
			{ 
				if(_subCategories != value)
				{
					_subCategories = value;
					NotifyOfPropertyChange(nameof(SubCategories));
                }
			}
		}

        private ItemTypeDTO _itemType;

        public ItemTypeDTO ItemType
        {
            get { return _itemType; }
            set 
            {
                if (_itemType != value)
                {
                    _itemType = value;
                    NotifyOfPropertyChange(nameof(ItemType));
                }
            }
        }


        private CatalogMasterViewModel _context;
        public CatalogMasterViewModel Context
        {
            get { return _context; }
            set
            {
                if (_context != value)
                {
                    _context = value;
                    NotifyOfPropertyChange(nameof(Context));
                }
            }
        }

        private bool _isDummyChild = false;
        public bool IsDummyChild
        {
            get { return _isDummyChild; }
            set
            {
                if (_isDummyChild != value)
                {
                    _isDummyChild = value;
                    NotifyOfPropertyChange(nameof(IsDummyChild));
                }
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    NotifyOfPropertyChange(nameof(IsSelected));
                }
            }
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    NotifyOfPropertyChange(nameof(IsExpanded));
                    if (_subCategories != null)
                    {
                        if (_isExpanded && _subCategories.Count > 0)
                        {
                            if (_subCategories[0].IsDummyChild) 
                            {
                                _ = _context.LoadItemsSubCategories(this);
                            }
                        }
                    }
                }
            }
        }

        public ItemCategoryDTO()
        {
            
        }

        public ItemCategoryDTO(int id, CatalogMasterViewModel context, string name, ObservableCollection<ItemSubCategoryDTO> subCategories)
        {
            _id = id;
            _name = name;
            _subCategories = subCategories;
            _context = context;
        }
        public override string ToString()
        {
            return $"{_name}";
        }
    }
}
