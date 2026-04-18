using Caliburn.Micro;

namespace NetErp.Treasury.Masters.DTO
{
    public class TreasuryFranchiseCostCenterDTO : PropertyChangedBase
    {
        public int Id
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Id));
                }
            }
        }

        public string Name
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Name));
                }
            }
        } = string.Empty;
    }
}
