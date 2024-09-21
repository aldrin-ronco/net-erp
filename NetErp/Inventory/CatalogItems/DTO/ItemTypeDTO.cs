using Caliburn.Micro;
using Models.Books;
using Models.Inventory;
using NetErp.Books.AccountingAccounts.ViewModels;
using NetErp.Inventory.CatalogItems.ViewModels;
using NetErp.Inventory.ItemSizes.DTO;
using NetErp.Inventory.ItemSizes.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Inventory.CatalogItems.DTO
{
    public class ItemTypeDTO: Screen, ICatalogItem, ICloneable
    {
        private CatalogMasterViewModel _context;
        public CatalogMasterViewModel Context
        {
            get { return _context; }
            set 
            { 
                if(_context != value)
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
                if(_isDummyChild != value)
                {
                    _isDummyChild = value;
                    NotifyOfPropertyChange(nameof(IsDummyChild));
                }
            }
        }

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

        private string _prefixChar = string.Empty;
        public string PrefixChar
        {
            get { return _prefixChar; }
            set
            {
                if (_prefixChar != value)
                {
                    _prefixChar = value;
                    NotifyOfPropertyChange(nameof(PrefixChar));
                }
            }
        }

        private bool _stockControl;
        public bool StockControl
        {
            get { return _stockControl; }
            set
            {
                if (_stockControl != value)
                {
                    _stockControl = value;
                    NotifyOfPropertyChange(nameof(StockControl));
                }
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set 
            { 
                if(_isSelected != value)
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
                    if (_itemsCategories != null)
                    {
                        if (_isExpanded && _itemsCategories.Count > 0)
                        {
                            if (_itemsCategories[0].IsDummyChild) 
                            {
                                _ = _context.LoadItemsCategories(this);
                            }
                        }
                    }
                }
            }
        }


        private ObservableCollection<ItemCategoryDTO> _itemsCategories = [];

        public ObservableCollection<ItemCategoryDTO> ItemsCategories
        {
            get { return _itemsCategories; }
            set 
            {
                if(_itemsCategories != value)
                {
                    _itemsCategories = value;
                    NotifyOfPropertyChange(nameof(ItemsCategories));
                }
            }
        }

        private CatalogDTO _catalog;

        public CatalogDTO Catalog
        {
            get { return _catalog; }
            set 
            {
                if (_catalog != value)
                {
                    _catalog = value;
                    NotifyOfPropertyChange(nameof(Catalog));
                }
            }
        }

        private MeasurementUnitDTO _measurementUnitByDefault;

        public MeasurementUnitDTO MeasurementUnitByDefault
        {
            get { return _measurementUnitByDefault; }
            set 
            {
                if (_measurementUnitByDefault != value)
                {
                    _measurementUnitByDefault = value;
                    NotifyOfPropertyChange(nameof(MeasurementUnitByDefault));
                }
            }
        }

        private AccountingGroupDTO _accountingGroupByDefault;

        public AccountingGroupDTO AccountingGroupByDefault
        {
            get { return _accountingGroupByDefault; }
            set 
            {
                if (_accountingGroupByDefault != value)
                {
                    _accountingGroupByDefault = value;
                    NotifyOfPropertyChange(nameof(AccountingGroupByDefault));
                }
            }
        }


        public ItemTypeDTO()
        {

        }

        public ItemTypeDTO(int id, CatalogMasterViewModel context, string name, string prefixChar, bool stockControl, ObservableCollection<ItemCategoryDTO> itemsCategories)
        {
            _id = id;
            _name = name;
            _prefixChar = prefixChar;
            _stockControl = stockControl;
            _itemsCategories = itemsCategories;
            _context = context;
        }
        public override string ToString()
        {
            return $"{_name}";
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
