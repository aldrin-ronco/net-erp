using Caliburn.Micro;
using DTOLibrary.Books;
using NetErp.Treasury.Masters.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Treasury.Masters.DTO
{
    public class TreasuryBankMasterTreeDTO: Screen, ITreasuryTreeMasterSelectedItem
    {
        public bool AllowContentControlVisibility { get => true; }
        public int Id { get; set; }

        //Propiedad creada para no tener errores de binding en la vista
        public string Name { get; set; } = string.Empty;
        public TreasuryRootMasterViewModel Context { get; set; }

        private AccountingEntityDTO _accountingEntity = new();

        public AccountingEntityDTO AccountingEntity
        {
            get { return _accountingEntity; }
            set 
            {
                if(_accountingEntity != value)
                {
                    _accountingEntity = value;
                    NotifyOfPropertyChange(nameof(AccountingEntity));
                }
            }
        }

        private string _paymentMethodPrefix =string.Empty;

        public string PaymentMethodPrefix
        {
            get { return _paymentMethodPrefix; }
            set 
            {
                if (_paymentMethodPrefix != value)
                {
                    _paymentMethodPrefix = value;
                    NotifyOfPropertyChange(nameof(PaymentMethodPrefix));
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

        private ObservableCollection<TreasuryBankAccountMasterTreeDTO> _bankAccounts = [];

        public ObservableCollection<TreasuryBankAccountMasterTreeDTO> BankAccounts
        {
            get { return _bankAccounts; }
            set
            {
                if (_bankAccounts != value)
                {
                    _bankAccounts = value;
                    NotifyOfPropertyChange(nameof(BankAccounts));
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
                    if (_bankAccounts != null)
                    {
                        if (_isExpanded && _bankAccounts.Count > 0)
                        {
                            if (_bankAccounts[0].IsDummyChild)
                            {
                                _ = Context.LoadBankAccounts(this);
                            }
                        }
                    }
                }
            }
        }

        private BankDummyDTO _dummyParent = new();

        public BankDummyDTO DummyParent
        {
            get { return _dummyParent; }
            set
            {
                if (_dummyParent != value)
                {
                    _dummyParent = value;
                    NotifyOfPropertyChange(nameof(DummyParent));
                }
            }
        }


        public TreasuryBankMasterTreeDTO()
        {

        }

    }
}
