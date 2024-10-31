using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Treasury.Masters.DTO
{
    public class TreasuryMajorCashDrawerCostCenterMasterTreeDTO: TreasuryCostCenterMasterTreeDTO, ITreasuryTreeMasterSelectedItem
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
                                _ = Context.LoadMajorCashDrawers(this);
                            }
                        }
                    }
                }
            }
        }

        private ObservableCollection<MajorCashDrawerMasterTreeDTO> _cashDrawers = [];

        public ObservableCollection<MajorCashDrawerMasterTreeDTO> CashDrawers
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

        private TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO _location;

        public TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO Location
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
