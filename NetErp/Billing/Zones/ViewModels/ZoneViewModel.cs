using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using DevExpress.Xpf.Core;
using Models.Billing;
using NetErp.Billing.Sellers.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Billing.Zones.ViewModels
{
    public class ZoneViewModel : Conductor<Screen>.Collection.OneActive
    {
        public IEventAggregator EventAggregator { get; set; }

        private ZoneMasterViewModel _zoneMasterViewModel;
        public ZoneMasterViewModel ZoneMasterViewModel
        {
            get
            {
                if (_zoneMasterViewModel is null) _zoneMasterViewModel = new ZoneMasterViewModel(this);
                return _zoneMasterViewModel;
            }
        }
        private ZoneDetailViewModel? _zoneDetailViewModel;
        public ZoneDetailViewModel ZoneDetailViewModel
        {
            get
            {
                if (_zoneDetailViewModel is null) _zoneDetailViewModel = new ZoneDetailViewModel(this);
                return _zoneDetailViewModel;
            }
        }
        public async Task ActivateMasterViewAsync()
        {
            try
            {
                await ActivateItemAsync(ZoneMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception ex)
            {

                throw new AsyncException(innerException: ex);
            }
        }

        public async Task ActivateDetailViewForNewAsync()
        {
            try
            {
                await ActivateItemAsync(ZoneDetailViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception ex)
            {

                throw new AsyncException(innerException: ex);
            }
        }
        public async Task ActivateDetailViewForEditAsync(ZoneGraphQLModel zone)
        {
            try
            {
                ZoneDetailViewModel instance = new(this) {
                    ZoneId = zone.Id,
                    ZoneName = zone.Name,
                    ZoneIsActive = zone.IsActive
                };
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception ex)
            {

                throw new AsyncException(innerException: ex);
            }
        }
        public ZoneViewModel(IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator;
            _zoneMasterViewModel = new ZoneMasterViewModel(this);
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
    }
}
