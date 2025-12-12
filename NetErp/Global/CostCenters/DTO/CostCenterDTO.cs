using Caliburn.Micro;
using Models.Books;
using Models.Global;
using NetErp.Global.CostCenters.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NetErp.Global.CostCenters.DTO
{
    public class CostCenterDTO : Screen, ICloneable, ICostCentersItems
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

        private string _tradeName;
        public string TradeName
        {
            get { return _tradeName; }
            set
            {
                if (_tradeName != value)
                {
                    _tradeName = value;
                    NotifyOfPropertyChange(nameof(TradeName));
                }
            }
        }
        public string FullName
        {
            get
            {
                string res = Name;
                if(!string.IsNullOrEmpty(Address)) { res = res + " " + Address; };
                if (!string.IsNullOrEmpty(City?.Name)) { res = res + " " + City; };
                if (!string.IsNullOrEmpty(City?.Department?.Name)) { res = res + " " + City?.Department.Name; };

                return res;
            }
        }
        private string _shortName;
        public string ShortName
        {
            get { return _shortName; }
            set
            {
                if (_shortName != value)
                {
                    _shortName = value;
                    NotifyOfPropertyChange(nameof(ShortName));
                }
            }
        }

        private string _state;
        public string State
        {
            get { return _state; }
            set
            {
                if (_state != value)
                {
                    _state = value;
                    NotifyOfPropertyChange(nameof(State));
                }
            }
        }

        private string _address;
        public string Address
        {
            get { return _address; }
            set
            {
                if (_address != value)
                {
                    _address = value;
                    NotifyOfPropertyChange(nameof(Address));
                }
            }
        }

        private string _primaryPhone;
        public string PrimaryPhone
        {
            get { return _primaryPhone; }
            set
            {
                if (_primaryPhone != value)
                {
                    _primaryPhone = value;
                    NotifyOfPropertyChange(nameof(PrimaryPhone));
                }
            }
        }

        private string _secondaryPhone;
        public string SecondaryPhone
        {
            get { return _secondaryPhone; }
            set
            {
                if (_secondaryPhone != value)
                {
                    _secondaryPhone = value;
                    NotifyOfPropertyChange(nameof(SecondaryPhone));
                }
            }
        }

        private string _primaryCellPhone;
        public string PrimaryCellPhone
        {
            get { return _primaryCellPhone; }
            set
            {
                if (_primaryCellPhone != value)
                {
                    _primaryCellPhone = value;
                    NotifyOfPropertyChange(nameof(PrimaryCellPhone));
                }
            }
        }

        private string _secondaryCellPhone;
        public string SecondaryCellPhone
        {
            get { return _secondaryCellPhone; }
            set
            {
                if (_secondaryCellPhone != value)
                {
                    _secondaryCellPhone = value;
                    NotifyOfPropertyChange(nameof(SecondaryCellPhone));
                }
            }
        }

        private string _dateControlType;
        public string DateControlType
        {
            get { return _dateControlType; }
            set
            {
                if (_dateControlType != value)
                {
                    _dateControlType = value;
                    NotifyOfPropertyChange(nameof(DateControlType));
                }
            }
        }

        private bool _isTaxable;
        public bool IsTaxable
        {
            get { return _isTaxable; }
            set
            {
                if (_isTaxable != value)
                {
                    _isTaxable = value;
                    NotifyOfPropertyChange(nameof(IsTaxable));
                }
            }
        }

        private bool _priceListIncludeTax;
        public bool PriceListIncludeTax
        {
            get { return _priceListIncludeTax; }
            set
            {
                if (_priceListIncludeTax != value)
                {
                    _priceListIncludeTax = value;
                    NotifyOfPropertyChange(nameof(PriceListIncludeTax));
                }
            }
        }

        private bool _invoicePriceIncludeTax;
        public bool InvoicePriceIncludeTax
        {
            get { return _invoicePriceIncludeTax; }
            set
            {
                if (_invoicePriceIncludeTax != value)
                {
                    _invoicePriceIncludeTax = value;
                    NotifyOfPropertyChange(nameof(InvoicePriceIncludeTax));
                }
            }
        }

        private bool _showChangeWindowOnCash;
        public bool ShowChangeWindowOnCash
        {
            get { return _showChangeWindowOnCash; }
            set
            {
                if (_showChangeWindowOnCash != value)
                {
                    _showChangeWindowOnCash = value;
                    NotifyOfPropertyChange(nameof(ShowChangeWindowOnCash));
                }
            }
        }

        private bool _allowRepeatItemsOnSales;
        public bool AllowRepeatItemsOnSales
        {
            get { return _allowRepeatItemsOnSales; }
            set
            {
                if (_allowRepeatItemsOnSales != value)
                {
                    _allowRepeatItemsOnSales = value;
                    NotifyOfPropertyChange(nameof(AllowRepeatItemsOnSales));
                }
            }
        }

        private bool _requiresConfirmationToPrintCopies;
        public bool RequiresConfirmationToPrintCopies
        {
            get { return _requiresConfirmationToPrintCopies; }
            set
            {
                if (_requiresConfirmationToPrintCopies != value)
                {
                    _requiresConfirmationToPrintCopies = value;
                    NotifyOfPropertyChange(nameof(RequiresConfirmationToPrintCopies));
                }
            }
        }

        private bool _allowBuy;
        public bool AllowBuy
        {
            get { return _allowBuy; }
            set
            {
                if (_allowBuy != value)
                {
                    _allowBuy = value;
                    NotifyOfPropertyChange(nameof(AllowBuy));
                }
            }
        }

        private bool _allowSell;
        public bool AllowSell
        {
            get { return _allowSell; }
            set
            {
                if (_allowSell != value)
                {
                    _allowSell = value;
                    NotifyOfPropertyChange(nameof(AllowSell));
                }
            }
        }

        private bool _taxToCost;
        public bool TaxToCost
        {
            get { return _taxToCost; }
            set
            {
                if (_taxToCost != value)
                {
                    _taxToCost = value;
                    NotifyOfPropertyChange(nameof(TaxToCost));
                }
            }
        }

        private string _defaultInvoiceObservation;
        public string DefaultInvoiceObservation
        {
            get { return _defaultInvoiceObservation; }
            set
            {
                if (_defaultInvoiceObservation != value)
                {
                    _defaultInvoiceObservation = value;
                    NotifyOfPropertyChange(nameof(DefaultInvoiceObservation));
                }
            }
        }

        private string _invoiceFooter;
        public string InvoiceFooter
        {
            get { return _invoiceFooter; }
            set
            {
                if (_invoiceFooter != value)
                {
                    _invoiceFooter = value;
                    NotifyOfPropertyChange(nameof(InvoiceFooter));
                }
            }
        }

        private string _remissionFooter;
        public string RemissionFooter
        {
            get { return _remissionFooter; }
            set
            {
                if (_remissionFooter != value)
                {
                    _remissionFooter = value;
                    NotifyOfPropertyChange(nameof(RemissionFooter));
                }
            }
        }

        private int _invoiceCopiesToPrint;
        public int InvoiceCopiesToPrint
        {
            get { return _invoiceCopiesToPrint; }
            set
            {
                if (_invoiceCopiesToPrint != value)
                {
                    _invoiceCopiesToPrint = value;
                    NotifyOfPropertyChange(nameof(InvoiceCopiesToPrint));
                }
            }
        }

        private CountryDTO _country;
        public CountryDTO Country
        {
            get { return _country; }
            set
            {
                if (_country != value)
                {
                    _country = value;
                    NotifyOfPropertyChange(nameof(Country));
                }
            }
        }

        private DepartmentDTO _department;
        public DepartmentDTO Department
        {
            get { return _department; }
            set
            {
                if (_department != value)
                {
                    _department = value;
                    NotifyOfPropertyChange(nameof(Department));
                }
            }
        }

        private CityDTO _city;
        public CityDTO City
        {
            get { return _city; }
            set
            {
                if (_city != value)
                {
                    _city = value;
                    NotifyOfPropertyChange(nameof(City));
                }
            }
        }
        public AuthorizationSequenceGraphQLModel FeCreditDefaultAuthorizationSequence { get; set; }
        public AuthorizationSequenceGraphQLModel FeCashDefaultAuthorizationSequence { get; set; }

        public AuthorizationSequenceGraphQLModel PeDefaultAuthorizationSequence { get; set; }
        public AuthorizationSequenceGraphQLModel DsDefaultAuthorizationSequence { get; set; }
        private CompanyLocationDTO _location;
        public CompanyLocationDTO Location
        {
            get { return _location; }
            set
            {
                if (_location != value)
                {
                    _location = value;
                    NotifyOfPropertyChange(nameof(Location));
                }
            }
        }

        private AccountingEntityDTO _relatedAccountingEntity;
        public AccountingEntityDTO RelatedAccountingEntity
        {
            get { return _relatedAccountingEntity; }
            set
            {
                if (_relatedAccountingEntity != value)
                {
                    _relatedAccountingEntity = value;
                    NotifyOfPropertyChange(nameof(RelatedAccountingEntity));
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

        public CostCenterDTO()
        {
        }

        public CostCenterDTO(int id, string name)
        {
            _id = id;
            _name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
