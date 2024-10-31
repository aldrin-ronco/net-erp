using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Treasury.Masters.DTO
{
    public class TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO: TreasuryCompanyLocationMasterTreeDTO, ITreasuryTreeMasterSelectedItem
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
                    if (_costCenters != null)
                    {
                        if (_isExpanded && _costCenters.Count > 0)
                        {
                            if (_costCenters[0].IsDummyChild)
                            {
                                _ = Context.LoadMajorCashDrawerCostCenters(this);
                            }
                        }
                    }
                }
            }
        }

        private MajorCashDrawerDummyDTO _dummyParent = new();

        public MajorCashDrawerDummyDTO DummyParent
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


        private ObservableCollection<TreasuryMajorCashDrawerCostCenterMasterTreeDTO> _costCenters = [];

        public ObservableCollection<TreasuryMajorCashDrawerCostCenterMasterTreeDTO> CostCenters
        {
            get { return _costCenters; }
            set
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        }
    }
}
