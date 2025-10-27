using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
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
        private readonly IRepository<ZoneGraphQLModel> _zoneService;
        private readonly Helpers.Services.INotificationService _notificationService;

        private ZoneMasterViewModel _zoneMasterViewModel;
        public ZoneMasterViewModel ZoneMasterViewModel
        {
            get
            {
                if (_zoneMasterViewModel is null) _zoneMasterViewModel = new ZoneMasterViewModel(this, _zoneService, _notificationService);
                return _zoneMasterViewModel;
            }
        }
        private ZoneDetailViewModel? _zoneDetailViewModel;
        public ZoneDetailViewModel ZoneDetailViewModel
        {
            get
            {
                if (_zoneDetailViewModel is null) _zoneDetailViewModel = new ZoneDetailViewModel(this, _zoneService);
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
                ZoneDetailViewModel instance = new(this, _zoneService) {
                    Id = zone.Id,
                    Name = zone.Name,
                    IsActive = zone.IsActive
                };

                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception ex)
            {

                throw new AsyncException(innerException: ex);
            }
        }
        public ZoneViewModel(
            IEventAggregator eventAggregator,
            IRepository<ZoneGraphQLModel> zoneService,
            Helpers.Services.INotificationService notificationService)
        {
            EventAggregator = eventAggregator;
            _zoneService = zoneService;
            _notificationService = notificationService;
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
                catch (Exception ex)
                {
                    await Execute.OnUIThreadAsync(() =>
                    {
                        ThemedMessageBox.Show(title: "Error de inicialización", text: $"Error al activar vista de zonas: {ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                        return Task.CompletedTask;
                    });
                }
            });
        }
    }
}
