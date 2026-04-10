using Caliburn.Micro;

namespace NetErp.Inventory.CatalogItems.DTO
{
    public class EanCodeByItemDTO : PropertyChangedBase
    {
        public string EanCode
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(EanCode));
                }
            }
        } = string.Empty;

        public bool IsInternal
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsInternal));
                }
            }
        }
    }
}
