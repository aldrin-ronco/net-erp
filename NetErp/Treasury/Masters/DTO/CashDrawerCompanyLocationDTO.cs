using System.Collections.ObjectModel;

namespace NetErp.Treasury.Masters.DTO
{
    /// <summary>
    /// DTO unificado para CompanyLocation en el árbol de Cajas.
    /// </summary>
    public class CashDrawerCompanyLocationDTO : TreasuryCompanyLocationMasterTreeDTO, ITreasuryTreeMasterSelectedItem
    {
        public CashDrawerType Type { get; set; }

        public CashDrawerDummyDTO? DummyParent
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

        public ObservableCollection<CashDrawerCostCenterDTO> CostCenters
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        } = [];

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

        public bool AllowContentControlVisibility => false;
    }
}
