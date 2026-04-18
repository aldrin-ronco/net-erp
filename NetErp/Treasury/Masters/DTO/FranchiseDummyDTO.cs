using Caliburn.Micro;
using NetErp.Treasury.Masters.ViewModels;
using System.Collections.ObjectModel;

namespace NetErp.Treasury.Masters.DTO
{
    public class FranchiseDummyDTO : PropertyChangedBase, ITreasuryTreeMasterSelectedItem
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

        public ObservableCollection<TreasuryFranchiseMasterTreeDTO> Franchises
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Franchises));
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

        public bool AllowContentControlVisibility => false;
    }
}
