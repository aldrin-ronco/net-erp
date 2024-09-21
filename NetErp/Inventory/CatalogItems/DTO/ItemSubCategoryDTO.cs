using Caliburn.Micro;
using NetErp.Inventory.CatalogItems.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Inventory.CatalogItems.DTO
{
    public class ItemSubCategoryDTO: Screen, ICatalogItem
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

        private ObservableCollection<ItemDTO> _items = [];

        public ObservableCollection<ItemDTO> Items
        {
            get { return _items; }
            set
            {
                if (_items != value)
                {
                    _items = value;
                    NotifyOfPropertyChange(nameof(Items));
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
                    if (_items != null)
                    {
                        if (_isExpanded && _items.Count > 0)
                        {
                            if (_items[0].IsDummyChild) 
                            {
                                _items.Clear();
                                _ = _context.LoadItems(this);
                            }
                        }
                    }
                }
            }
        }
        private ItemCategoryDTO _itemCategory;

        public ItemCategoryDTO ItemCategory
        {
            get { return _itemCategory; }
            set 
            {
                if (_itemCategory != value) 
                {
                    _itemCategory = value;
                    NotifyOfPropertyChange(nameof(ItemCategory));
                } 
            }
        }

        public ItemSubCategoryDTO()
        {

        }

        public ItemSubCategoryDTO(int id, CatalogMasterViewModel context, string name, ObservableCollection<ItemDTO> items)
        {
            _id = id;
            _name = name;
            _items = items;
            _context = context;
        }
        public override string ToString()
        {
            return $"{_name}";
        }
    }
}
