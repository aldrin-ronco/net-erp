using Caliburn.Micro;

namespace NetErp.Treasury.Masters.DTO
{
    public class TreasuryBankAccountCostCenterDTO : PropertyChangedBase
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

        public bool IsChecked
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsChecked));
                }
            }
        }
    }
}
