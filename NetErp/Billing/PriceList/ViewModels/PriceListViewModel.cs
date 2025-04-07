using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using DevExpress.Xpf.Core;
using NetErp.Billing.CreditLimit.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Billing.PriceList.ViewModels
{
    public class PriceListViewModel: Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; set; }
        public IEventAggregator EventAggregator { get; set; }

        private PriceListMasterViewModel _priceListMasterViewModel;

        public PriceListMasterViewModel PriceListMasterViewModel
        {
            get 
            {
                if (_priceListMasterViewModel is null) _priceListMasterViewModel = new PriceListMasterViewModel(this);
                return _priceListMasterViewModel; 
            }
        }


        public PriceListViewModel(IMapper autoMapper, IEventAggregator eventAggregator)
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
                await ActivateItemAsync(PriceListMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }

        }
    }
}
