using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Models.Billing;
using NetErp.Billing.Sellers.ViewModels;
using NetErp.Helpers.Cache;
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
        private readonly StringLengthCache _stringLengthCache;

        private ZoneMasterViewModel _zoneMasterViewModel;
        public ZoneMasterViewModel ZoneMasterViewModel
        {
            get
            {
                if (_zoneMasterViewModel is null) _zoneMasterViewModel = new ZoneMasterViewModel(this, _zoneService, _notificationService);
                return _zoneMasterViewModel;
            }
        }
        public async Task ActivateMasterViewAsync()
        {
            try
            {
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.Zone);
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
                ZoneDetailViewModel instance = new(this, _zoneService, _stringLengthCache);
                instance.SetForNew();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
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
                ZoneDetailViewModel instance = new(this, _zoneService, _stringLengthCache);
                instance.SetForEdit(zone);
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
            Helpers.Services.INotificationService notificationService,
            StringLengthCache stringLengthCache)
        {
            EventAggregator = eventAggregator;
            _zoneService = zoneService;
            _notificationService = notificationService;
            _stringLengthCache = stringLengthCache;
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
