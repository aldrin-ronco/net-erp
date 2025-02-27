using AutoMapper;
using Caliburn.Micro;
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
        //public IMapper AutoMapper { get; private set; }

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
            catch (Exception)
            {

                throw;
            }
        }

        public async Task ActivateDetailViewForNewAsync()
        {
            await ActivateItemAsync(ZoneDetailViewModel, new System.Threading.CancellationToken());
        }
        public async Task ActivateDetailViewForEditAsync(ZoneGraphQLModel Zone)
        {
            try
            {
                ZoneDetailViewModel instance = new(this) {
                    ZoneId = Zone.Id,
                    ZoneName = Zone.Name,
                    ZoneIsActive = Zone.IsActive
                };
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {
                throw;
            }
        }
        public ZoneViewModel(IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator;
            _zoneMasterViewModel = new ZoneMasterViewModel(this);
            _ = Task.Run(ActivateMasterViewAsync);
        }
    }
}
