using System.Collections.ObjectModel;

namespace NetErp.Treasury.Masters.DTO
{
    /// <summary>
    /// DTO unificado para CostCenter en el árbol de Cajas.
    /// Reemplaza TreasuryMajorCashDrawerCostCenterMasterTreeDTO y TreasuryMinorCashDrawerCostCenterMasterTreeDTO.
    /// </summary>
    public class CashDrawerCostCenterDTO : TreasuryCostCenterMasterTreeDTO, ITreasuryTreeMasterSelectedItem
    {
        public bool AllowContentControlVisibility => false;

        public CashDrawerType Type { get; set; }

        private CashDrawerCompanyLocationDTO _location;
        public CashDrawerCompanyLocationDTO Location
        {
            get => _location;
            set
            {
                if (_location != value)
                {
                    _location = value;
                    NotifyOfPropertyChange(nameof(Location));
                }
            }
        }

        // Usamos la clase base para poder contener tanto Major como Minor CashDrawers
        private ObservableCollection<CashDrawerMasterTreeDTO> _cashDrawers = [];
        public ObservableCollection<CashDrawerMasterTreeDTO> CashDrawers
        {
            get => _cashDrawers;
            set
            {
                if (_cashDrawers != value)
                {
                    _cashDrawers = value;
                    NotifyOfPropertyChange(nameof(CashDrawers));
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

                    if (_isExpanded && _cashDrawers != null && _cashDrawers.Count > 0 && _cashDrawers[0].IsDummyChild)
                    {
                        _ = Context.LoadCashDrawers(this);
                    }
                }
            }
        }
    }
}
