using System.Collections.ObjectModel;

namespace NetErp.Treasury.Masters.DTO
{
    public class MajorCashDrawerMasterTreeDTO : CashDrawerMasterTreeDTO, ITreasuryTreeMasterSelectedItem
    {
        public ObservableCollection<TreasuryAuxiliaryCashDrawerMasterTreeDTO> AuxiliaryCashDrawers
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AuxiliaryCashDrawers));
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

        public bool AllowContentControlVisibility => true;
    }
}
