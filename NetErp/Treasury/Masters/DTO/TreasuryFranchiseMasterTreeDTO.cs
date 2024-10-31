using Caliburn.Micro;
using NetErp.Books.AccountingAccounts.DTO;
using NetErp.Treasury.Masters.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Treasury.Masters.DTO
{
    public class TreasuryFranchiseMasterTreeDTO: Screen, ITreasuryTreeMasterSelectedItem
    {
        public bool AllowContentControlVisibility { get => true; }
        public int Id { get; set; }
        public TreasuryRootMasterViewModel Context { get; set; }

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

        private float _commissionMargin;
        public float CommissionMargin
        {
            get { return _commissionMargin; }
            set
            {
                if (_commissionMargin != value)
                {
                    _commissionMargin = value;
                    NotifyOfPropertyChange(nameof(CommissionMargin));
                }
            }
        }

        private float _reteivaMargin;
        public float ReteivaMargin
        {
            get { return _reteivaMargin; }
            set
            {
                if (_reteivaMargin != value)
                {
                    _reteivaMargin = value;
                    NotifyOfPropertyChange(nameof(ReteivaMargin));
                }
            }
        }

        private float _reteicaMargin;
        public float ReteicaMargin
        {
            get { return _reteicaMargin; }
            set
            {
                if (_reteicaMargin != value)
                {
                    _reteicaMargin = value;
                    NotifyOfPropertyChange(nameof(ReteicaMargin));
                }
            }
        }

        private float _retefteMargin;
        public float RetefteMargin
        {
            get { return _retefteMargin; }
            set
            {
                if (_retefteMargin != value)
                {
                    _retefteMargin = value;
                    NotifyOfPropertyChange(nameof(RetefteMargin));
                }
            }
        }

        private float _ivaMargin;
        public float IvaMargin
        {
            get { return _ivaMargin; }
            set
            {
                if (_ivaMargin != value)
                {
                    _ivaMargin = value;
                    NotifyOfPropertyChange(nameof(IvaMargin));
                }
            }
        }

        private AccountingAccountDTO _accountingAccountCommission = new();
        public AccountingAccountDTO AccountingAccountCommission
        {
            get { return _accountingAccountCommission; }
            set
            {
                if (_accountingAccountCommission != value)
                {
                    _accountingAccountCommission = value;
                    NotifyOfPropertyChange(nameof(AccountingAccountCommission));
                }
            }
        }

        private TreasuryBankAccountMasterTreeDTO _bankAccount = new();

        public TreasuryBankAccountMasterTreeDTO BankAccount
        {
            get { return _bankAccount; }
            set 
            {
                if (_bankAccount != value)
                {
                    _bankAccount = value;
                    NotifyOfPropertyChange(nameof(BankAccount));
                }
            }
        }


        private string _formulaCommission = string.Empty;
        public string FormulaCommission
        {
            get { return _formulaCommission; }
            set
            {
                if (_formulaCommission != value)
                {
                    _formulaCommission = value;
                    NotifyOfPropertyChange(nameof(FormulaCommission));
                }
            }
        }

        private string _formulaReteiva = string.Empty;
        public string FormulaReteiva
        {
            get { return _formulaReteiva; }
            set
            {
                if (_formulaReteiva != value)
                {
                    _formulaReteiva = value;
                    NotifyOfPropertyChange(nameof(FormulaReteiva));
                }
            }
        }

        private string _formulaReteica = string.Empty;
        public string FormulaReteica
        {
            get { return _formulaReteica; }
            set
            {
                if (_formulaReteica != value)
                {
                    _formulaReteica = value;
                    NotifyOfPropertyChange(nameof(FormulaReteica));
                }
            }
        }

        private string _formulaRetefte = string.Empty;
        public string FormulaRetefte
        {
            get { return _formulaRetefte; }
            set
            {
                if (_formulaRetefte != value)
                {
                    _formulaRetefte = value;
                    NotifyOfPropertyChange(nameof(FormulaRetefte));
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

        private FranchiseDummyDTO _dummyParent = new();

        public FranchiseDummyDTO DummyParent
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
    }
}
