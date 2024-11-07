using Models.Treasury;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Treasury.Masters.DTO
{
    public class MajorCashDrawerMasterTreeDTO : CashDrawerMasterTreeDTO, ITreasuryTreeMasterSelectedItem
    {
		private ObservableCollection<TreasuryAuxiliaryCashDrawerMasterTreeDTO> _auxiliaryCashDrawers = [];

		public ObservableCollection<TreasuryAuxiliaryCashDrawerMasterTreeDTO> AuxiliaryCashDrawers 
		{
			get { return _auxiliaryCashDrawers; }
			set 
			{
				if(_auxiliaryCashDrawers != value)
				{
					_auxiliaryCashDrawers = value;
					NotifyOfPropertyChange(nameof(AuxiliaryCashDrawers));
				}
			}
		}

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
                    if (_auxiliaryCashDrawers != null)
                    {
                        if (_isExpanded && _auxiliaryCashDrawers.Count > 0)
                        {
                            if (_auxiliaryCashDrawers[0].IsDummyChild)
                            {
                                _ = Context.LoadAuxiliaryCashDrawers(this);
                            }
                        }
                    }
                }
            }
        }
    }
}
