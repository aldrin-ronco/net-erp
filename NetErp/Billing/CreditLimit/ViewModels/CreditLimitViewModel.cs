using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using DevExpress.Xpf.Core;
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

        private CreditLimitMasterViewModel? _creditLimitMasterViewModel;

        public CreditLimitMasterViewModel? CreditLimitMasterViewModel
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
            _ = Task.Run(async () => 
            {
                try
                {
                    await ActivateMasterViewAsync();
                }
                catch (AsyncException ex)
                {
                    await Execute.OnUIThreadAsync(() =>
                    {
                        ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                        return Task.CompletedTask;
                    });
                }
            });
        }

        public async Task ActivateMasterViewAsync()
        {
            try
            {
                await ActivateItemAsync(CreditLimitMasterViewModel ?? new CreditLimitMasterViewModel(this), new System.Threading.CancellationToken());
            }
            catch(Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }

        }
    }
}
