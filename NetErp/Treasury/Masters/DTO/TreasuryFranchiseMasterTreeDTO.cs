using Caliburn.Micro;
using Models.Treasury;
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

        private decimal _commissionRate;
        public decimal CommissionRate
        {
            get { return _commissionRate; }
            set
            {
                if (_commissionRate != value)
                {
                    _commissionRate = value;
                    NotifyOfPropertyChange(nameof(CommissionRate));
                }
            }
        }

        private decimal _reteivaRate;
        public decimal ReteivaRate
        {
            get { return _reteivaRate; }
            set
            {
                if (_reteivaRate != value)
                {
                    _reteivaRate = value;
                    NotifyOfPropertyChange(nameof(ReteivaRate));
                }
            }
        }

        private decimal _reteicaRate;
        public decimal ReteicaRate
        {
            get { return _reteicaRate; }
            set
            {
                if (_reteicaRate != value)
                {
                    _reteicaRate = value;
                    NotifyOfPropertyChange(nameof(ReteicaRate));
                }
            }
        }

        private decimal _retefteRate;
        public decimal RetefteRate
        {
            get { return _retefteRate; }
            set
            {
                if (_retefteRate != value)
                {
                    _retefteRate = value;
                    NotifyOfPropertyChange(nameof(RetefteRate));
                }
            }
        }

        private decimal _taxRate;
        public decimal TaxRate
        {
            get { return _taxRate; }
            set
            {
                if (_taxRate != value)
                {
                    _taxRate = value;
                    NotifyOfPropertyChange(nameof(TaxRate));
                }
            }
        }

        private AccountingAccountDTO _commissionAccountingAccount = new();
        public AccountingAccountDTO CommissionAccountingAccount
        {
            get { return _commissionAccountingAccount; }
            set
            {
                if (_commissionAccountingAccount != value)
                {
                    _commissionAccountingAccount = value;
                    NotifyOfPropertyChange(nameof(CommissionAccountingAccount));
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

        public List<FranchiseByCostCenterGraphQLModel> FranchisesByCostCenter { get; set; }
    }
}
