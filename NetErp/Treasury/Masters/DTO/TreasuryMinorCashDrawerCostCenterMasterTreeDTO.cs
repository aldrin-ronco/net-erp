using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Treasury.Masters.DTO
{
    public class TreasuryMinorCashDrawerCostCenterMasterTreeDTO: TreasuryCostCenterMasterTreeDTO, ITreasuryTreeMasterSelectedItem
    {
        public bool AllowContentControlVisibility { get => false; }
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
                    if (_cashDrawers != null)
                    {
                        if (_isExpanded && _cashDrawers.Count > 0)
                        {
                            if (_cashDrawers[0].IsDummyChild)
                            {
                                _ = Context.LoadMinorCashDrawers(this);
                            }
                        }
                    }
                }
            }
        }

        private ObservableCollection<MinorCashDrawerMasterTreeDTO> _cashDrawers = [];

        public ObservableCollection<MinorCashDrawerMasterTreeDTO> CashDrawers
        {
            get { return _cashDrawers; }
            set
            {
                if (_cashDrawers != value)
                {
                    _cashDrawers = value;
                    NotifyOfPropertyChange(nameof(CashDrawers));
                }
            }
        }

        private TreasuryMinorCashDrawerCompanyLocationMasterTreeDTO _location;

        public TreasuryMinorCashDrawerCompanyLocationMasterTreeDTO Location
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
    }
}
