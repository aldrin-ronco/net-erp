namespace NetErp.Treasury.Masters.DTO
{
    public class MinorCashDrawerMasterTreeDTO : CashDrawerMasterTreeDTO, ITreasuryTreeMasterSelectedItem
    {
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
