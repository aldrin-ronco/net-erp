using Caliburn.Micro;
using NetErp.Treasury.Masters.ViewModels;
using System.Collections.ObjectModel;

namespace NetErp.Treasury.Masters.DTO
{
    public class BankDummyDTO : PropertyChangedBase, ITreasuryTreeMasterSelectedItem
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

        public ObservableCollection<TreasuryBankMasterTreeDTO> Banks
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Banks));
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
