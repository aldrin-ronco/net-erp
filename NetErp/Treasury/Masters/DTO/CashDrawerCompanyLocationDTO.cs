using Models.Books;
using System.Collections.ObjectModel;

namespace NetErp.Treasury.Masters.DTO
{
    /// <summary>
    /// DTO unificado para CompanyLocation en el árbol de Cajas.
    /// Reemplaza TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO y TreasuryMinorCashDrawerCompanyLocationMasterTreeDTO.
    /// </summary>
    public class CashDrawerCompanyLocationDTO : TreasuryCompanyLocationMasterTreeDTO, ITreasuryTreeMasterSelectedItem
    {
        public bool AllowContentControlVisibility => false;

        public CashDrawerType Type { get; set; }

        private CashDrawerDummyDTO _dummyParent;
        public CashDrawerDummyDTO DummyParent
        {
            get => _dummyParent;
            set
            {
                if (_dummyParent != value)
                {
                    _dummyParent = value;
                    NotifyOfPropertyChange(nameof(DummyParent));
                }
            }
        }

        private ObservableCollection<CashDrawerCostCenterDTO> _costCenters = [];
        public ObservableCollection<CashDrawerCostCenterDTO> CostCenters
        {
            get => _costCenters;
            set
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    NotifyOfPropertyChange(nameof(IsExpanded));

                    if (_isExpanded && _costCenters != null && _costCenters.Count > 0 && _costCenters[0].IsDummyChild)
                    {
                        _ = Context.LoadCashDrawerCostCenters(this);
                    }
                }
            }
        }

        // Propiedad dummy para evitar comportamiento poco probable en el árbol
        private AccountingEntityGraphQLModel _accountingEntity = new();
        public AccountingEntityGraphQLModel AccountingEntity
        {
            get => _accountingEntity;
            set => _accountingEntity = value;
        }
    }
}
