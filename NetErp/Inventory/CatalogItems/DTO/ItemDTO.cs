using Caliburn.Micro;
using Models.Books;
using Models.Inventory;
using NetErp.Inventory.CatalogItems.ViewModels;
using NetErp.Inventory.ItemSizes.DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.Primitives;

namespace NetErp.Inventory.CatalogItems.DTO
{
    public class ItemDTO : Screen, ICloneable, ICatalogItem
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

        private string _code = string.Empty;

        public string Code
        {
            get { return _code; }
            set
            {
                if (_code != value)
                {
                    _code = value;
                    NotifyOfPropertyChange(nameof(Code));
                }
            }
        }

        private string _reference = string.Empty;

        public string Reference
        {
            get { return _reference; }
            set
            {
                if (_reference != value)
                {
                    _reference = value;
                    NotifyOfPropertyChange(nameof(Reference));
                }
            }
        }

        private bool _isActive;

        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    NotifyOfPropertyChange(nameof(IsActive));
                }
            }
        }

        private bool _allowFraction;

        public bool AllowFraction
        {
            get { return _allowFraction; }
            set
            {
                if (_allowFraction != value)
                {
                    _allowFraction = value;
                    NotifyOfPropertyChange(nameof(AllowFraction));
                }
            }
        }

        private bool _billable;

        public bool Billable
        {
            get { return _billable; }
            set
            {
                if (_billable != value)
                {
                    _billable = value;
                    NotifyOfPropertyChange(nameof(Billable));
                }
            }
        }

        private bool _amountBasedOnWeight;

        public bool AmountBasedOnWeight
        {
            get { return _amountBasedOnWeight; }
            set
            {
                if (_amountBasedOnWeight != value)
                {
                    _amountBasedOnWeight = value;
                    NotifyOfPropertyChange(nameof(AmountBasedOnWeight));
                }
            }
        }

        private bool _hasExtendedInformation;

        public bool HasExtendedInformation
        {
            get { return _hasExtendedInformation; }
            set
            {
                if (_hasExtendedInformation != value)
                {
                    _hasExtendedInformation = value;
                    NotifyOfPropertyChange(nameof(HasExtendedInformation));
                }
            }
        }

        private bool _aiuBasedService;

        public bool AiuBasedService
        {
            get { return _aiuBasedService; }
            set
            {
                if (_aiuBasedService != value)
                {
                    _aiuBasedService = value;
                    NotifyOfPropertyChange(nameof(AiuBasedService));
                }
            }
        }

        private ObservableCollection<EanCodeDTO> _eanCodes;

        public ObservableCollection<EanCodeDTO> EanCodes
        {
            get { return _eanCodes; }
            set
            {
                if (_eanCodes != value)
                {
                    _eanCodes = value;
                    NotifyOfPropertyChange(nameof(EanCodes));
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

        private bool _isSelected = false;
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

        private bool _isExpanded = false;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    NotifyOfPropertyChange(nameof(IsExpanded));
                }
            }
        }

        private MeasurementUnitDTO _measurementUnit;

        public MeasurementUnitDTO MeasurementUnit
        {
            get { return _measurementUnit; }
            set
            {
                if (_measurementUnit != value)
                {
                    _measurementUnit = value;
                    NotifyOfPropertyChange(nameof(MeasurementUnit));
                }
            }
        }

        private BrandDTO _brand;

        public BrandDTO Brand
        {
            get { return _brand; }
            set
            {
                if (_brand != value)
                {
                    _brand = value;
                    NotifyOfPropertyChange(nameof(Brand));
                }
            }
        }

        private ItemSizeMasterDTO _size;

        public ItemSizeMasterDTO Size
        {
            get { return _size; }
            set
            {
                if (_size != value)
                {
                    _size = value;
                    NotifyOfPropertyChange(nameof(Size));
                }
            }
        }

        private AccountingGroupDTO _accountingGroup;

        public AccountingGroupDTO AccountingGroup
        {
            get { return _accountingGroup; }
            set
            {
                if (_accountingGroup != value)
                {
                    _accountingGroup = value;
                    NotifyOfPropertyChange(nameof(AccountingGroup));
                }
            }
        }

        private ItemSubCategoryDTO _subCategory;

        public ItemSubCategoryDTO SubCategory
        {
            get { return _subCategory; }
            set 
            {
                if (_subCategory != value)
                {
                    _subCategory = value;
                    NotifyOfPropertyChange(nameof(SubCategory));
                }
            }
        }


        public ItemDTO()
        {

        }

        public ItemDTO(int id, CatalogMasterViewModel context, string name)
        {
            _id = id;
            _name = name;
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
