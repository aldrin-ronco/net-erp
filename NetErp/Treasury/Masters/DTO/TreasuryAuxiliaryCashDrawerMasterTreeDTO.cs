using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Treasury.Masters.DTO
{
    public class TreasuryAuxiliaryCashDrawerMasterTreeDTO : CashDrawerMasterTreeDTO, ITreasuryTreeMasterSelectedItem
    {
        public bool AllowContentControlVisibility { get => true; }

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

    }
}
