using Caliburn.Micro;
using Models.Treasury;
using NetErp.Books.AccountingAccounts.DTO;
using NetErp.Treasury.Masters.ViewModels;
using System.Collections.Generic;

namespace NetErp.Treasury.Masters.DTO
{
    public class TreasuryFranchiseMasterTreeDTO : PropertyChangedBase, ITreasuryTreeMasterSelectedItem
    {
        public int Id { get; set; }
        public TreasuryRootMasterViewModel? Context { get; set; }

        public string Name
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Name));
                }
            }
        } = string.Empty;

        public string Type
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Type));
                }
            }
        } = string.Empty;

        public decimal CommissionRate
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CommissionRate));
                }
            }
        }

        public decimal ReteivaRate
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ReteivaRate));
                }
            }
        }

        public decimal ReteicaRate
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ReteicaRate));
                }
            }
        }

        public decimal RetefteRate
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(RetefteRate));
                }
            }
        }

        public decimal TaxRate
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(TaxRate));
                }
            }
        }

        public AccountingAccountDTO CommissionAccountingAccount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CommissionAccountingAccount));
                }
            }
        } = new();

        public TreasuryBankAccountMasterTreeDTO BankAccount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(BankAccount));
                }
            }
        } = new();

        public string FormulaCommission
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FormulaCommission));
                }
            }
        } = string.Empty;

        public string FormulaReteiva
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FormulaReteiva));
                }
            }
        } = string.Empty;

        public string FormulaReteica
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FormulaReteica));
                }
            }
        } = string.Empty;

        public string FormulaRetefte
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FormulaRetefte));
                }
            }
        } = string.Empty;

        public bool IsExpanded
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsExpanded));
                }
            }
        }

        public FranchiseDummyDTO? DummyParent
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DummyParent));
                }
            }
        }

        public List<FranchiseByCostCenterGraphQLModel> FranchisesByCostCenter { get; set; } = [];

        public bool AllowContentControlVisibility => true;
    }
}
