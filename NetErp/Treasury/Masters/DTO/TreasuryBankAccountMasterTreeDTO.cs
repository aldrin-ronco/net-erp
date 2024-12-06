using Caliburn.Micro;
using Models.Billing;
using Models.Books;
using NetErp.Books.AccountingAccounts.DTO;
using NetErp.Treasury.Masters.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Treasury.Masters.DTO
{
    public class TreasuryBankAccountMasterTreeDTO : Screen, ITreasuryTreeMasterSelectedItem
    {
        public bool AllowContentControlVisibility { get => true; }
        public int Id { get; set; }
        public TreasuryRootMasterViewModel Context { get; set; }

        //Propiedad creada para no tener errores de binding en la vista
        public string Name { get; set; } = string.Empty;

        //Propiedad creada para no tener errores de binding en la vista
        public AccountingEntityGraphQLModel AccountingEntity { get; set; } = new();

        private string _type = string.Empty;

        public string Type
        {
            get { return _type; }
            set
            {
                if (_type != value)
                {
                    _type = value;
                    NotifyOfPropertyChange(nameof(Type));
                }
            }
        }

        private string _number = string.Empty;

        public string Number
        {
            get { return _number; }
            set
            {
                if (_number != value)
                {
                    _number = value;
                    NotifyOfPropertyChange(nameof(Number));
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

        private string _description = string.Empty;

        public string Description
        {
            get { return _description; }
            set
            {
                if (_description != value)
                {
                    _description = value;
                    NotifyOfPropertyChange(nameof(Description));
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

        private int _displayOrder;

        public int DisplayOrder
        {
            get { return _displayOrder; }
            set
            {
                if (_displayOrder != value)
                {
                    _displayOrder = value;
                    NotifyOfPropertyChange(nameof(DisplayOrder));
                }
            }
        }

        private AccountingAccountGraphQLModel _accountingAccount = new();

        public AccountingAccountGraphQLModel AccountingAccount
        {
            get { return _accountingAccount; }
            set
            {
                if (_accountingAccount != value)
                {
                    _accountingAccount = value;
                    NotifyOfPropertyChange(nameof(AccountingAccount));
                }
            }
        }

        private TreasuryBankMasterTreeDTO _bank = new();

        public TreasuryBankMasterTreeDTO Bank
        {
            get { return _bank; }
            set
            {
                if (_bank != value)
                {
                    _bank = value;
                    NotifyOfPropertyChange(nameof(Bank));
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

        private string _provider;

        public string Provider
        {
            get { return _provider; }
            set 
            {
                if (_provider != value)
                {
                    _provider = value;
                    NotifyOfPropertyChange(nameof(Provider));
                }
            }
        }

        private PaymentMethodGraphQLModel _paymentMethod;

        public PaymentMethodGraphQLModel PaymentMethod
        {
            get { return _paymentMethod; }
            set
            {
                if (_paymentMethod != value)
                {
                    _paymentMethod = value;
                    NotifyOfPropertyChange(nameof(PaymentMethod));
                }
            }
        }

        public ObservableCollection<TreasuryBankAccountCostCenterDTO> AllowedCostCenters { get; set; } = [];

        public TreasuryBankAccountMasterTreeDTO()
        {

        }
    }
}
