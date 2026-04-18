using Caliburn.Micro;
using NetErp.Treasury.Masters.ViewModels;

namespace NetErp.Treasury.Masters.DTO
{
    public class TreasuryCompanyLocationMasterTreeDTO : PropertyChangedBase
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

        public TreasuryCompanyLocationMasterTreeDTO() { }

        public TreasuryCompanyLocationMasterTreeDTO(int id, string name, TreasuryRootMasterViewModel context)
        {
            Id = id;
            Name = name;
            Context = context;
        }
    }
}
