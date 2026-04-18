namespace NetErp.Treasury.Masters.DTO
{
    public class TreasuryAuxiliaryCashDrawerMasterTreeDTO : CashDrawerMasterTreeDTO, ITreasuryTreeMasterSelectedItem
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
