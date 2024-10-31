using AutoMapper;
using Caliburn.Micro;
using NetErp.Global.CostCenters.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Treasury.Masters.ViewModels
{
    public class TreasuryRootViewModel : Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; set; }

        private TreasuryRootMasterViewModel _treasuryRootMasterViewModel;
        public TreasuryRootMasterViewModel TreasuryRootMasterViewModel
        {
            get
            {
                if (_treasuryRootMasterViewModel is null) _treasuryRootMasterViewModel = new TreasuryRootMasterViewModel(this);
                return _treasuryRootMasterViewModel;
            }
        }

        public TreasuryRootViewModel(IMapper mapper,
                                 IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            _ = Task.Run(ActivateMasterView);
        }

        public async Task ActivateMasterView()
        {
            try
            {
                await ActivateItemAsync(TreasuryRootMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
