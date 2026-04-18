using Caliburn.Micro;
using NetErp.Treasury.Masters.ViewModels;

namespace NetErp.Treasury.Masters.DTO
{
    public class TreasuryCostCenterMasterTreeDTO : PropertyChangedBase
    {
        public int Id { get; set; }

        public TreasuryRootMasterViewModel? Context { get; set; }

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

        public TreasuryCostCenterMasterTreeDTO() { }

        public TreasuryCostCenterMasterTreeDTO(int id, string name, TreasuryRootMasterViewModel context)
        {
            Id = id;
            Name = name;
            Context = context;
        }
    }
}
