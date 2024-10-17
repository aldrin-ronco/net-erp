using AutoMapper;
using Caliburn.Micro;
using NetErp.Billing.Customers.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Global.CostCenters.ViewModels
{
    public class CostCenterViewModel: Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; set; }

        private CostCenterMasterViewModel _costCenterMasterViewModel;
        public CostCenterMasterViewModel CostCenterMasteViewModel
        {
            get
            {
                if (_costCenterMasterViewModel is null) _costCenterMasterViewModel = new CostCenterMasterViewModel(this);
                return _costCenterMasterViewModel;
            }
        }

        public CostCenterViewModel(IMapper mapper,
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
                await ActivateItemAsync(CostCenterMasteViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
