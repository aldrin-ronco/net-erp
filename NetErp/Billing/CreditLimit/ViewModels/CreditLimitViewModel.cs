using AutoMapper;
using Caliburn.Micro;
using NetErp.Billing.Customers.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Billing.CreditLimit.ViewModels
{
    public class CreditLimitViewModel: Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; set; }
        public IEventAggregator EventAggregator { get; set; }

        private CreditLimitMasterViewModel _creditLimitMasterViewModel;

        public CreditLimitMasterViewModel CreditLimitMasterViewModel
        {
            get 
            {
                if(_creditLimitMasterViewModel is null) _creditLimitMasterViewModel = new CreditLimitMasterViewModel(this);
                return _creditLimitMasterViewModel;
            }
        }

        public CreditLimitViewModel(IMapper autoMapper, IEventAggregator eventAggregator)
        {
            AutoMapper = autoMapper;
            EventAggregator = eventAggregator;
            _ = Task.Run(ActivateMasterView);
        }

        public async Task ActivateMasterView()
        {
            try
            {
                await ActivateItemAsync(CreditLimitMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
