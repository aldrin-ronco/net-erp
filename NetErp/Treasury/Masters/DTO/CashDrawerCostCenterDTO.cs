using System.Collections.ObjectModel;

namespace NetErp.Treasury.Masters.DTO
{
    /// <summary>
    /// DTO unificado para CostCenter en el árbol de Cajas.
    /// </summary>
    public class CashDrawerCostCenterDTO : TreasuryCostCenterMasterTreeDTO, ITreasuryTreeMasterSelectedItem
    {
        public CashDrawerType Type { get; set; }

        public CashDrawerCompanyLocationDTO? Location
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Location));
                }
            }
        }

        // Usa la clase base para poder contener tanto Major como Minor cash drawers
        public ObservableCollection<CashDrawerMasterTreeDTO> CashDrawers
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CashDrawers));
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
