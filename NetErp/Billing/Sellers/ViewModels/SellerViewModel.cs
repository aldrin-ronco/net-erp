using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.Books;
using Models.DTO.Global;
using Models.Global;
using NetErp.Billing.Customers.ViewModels;
using NetErp.Billing.Zones.DTO;
using NetErp.Global.CostCenters.DTO;
using NetErp.Helpers.Cache;
using Ninject.Activation;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NetErp.Billing.Sellers.ViewModels
{
    public class SellerViewModel : Conductor<Screen>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; private set; }
        private readonly IRepository<SellerGraphQLModel> _sellerService;
        private readonly IRepository<ZoneGraphQLModel> _zoneService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly CostCenterCache _costCenterCache;
        private readonly IdentificationTypeCache _identificationTypeCache;
        private readonly CountryCache _countryCache;
        private readonly ZoneCache _zoneCache;

      

        private SellerMasterViewModel _sellerMasterViewModel;
        public SellerMasterViewModel SellerMasterViewModel 
        { 
            get 
            {
                if (_sellerMasterViewModel is null) _sellerMasterViewModel = new SellerMasterViewModel(this, _sellerService, _costCenterCache, _notificationService );
                return _sellerMasterViewModel;
            } 
        }
        
        private bool _enableOnViewReady = true;

        public bool EnableOnViewReady
        {
            get { return _enableOnViewReady; }
            set
            {
                _enableOnViewReady = value;
            }
        }

        public SellerViewModel(
            IMapper mapper,
            IEventAggregator eventAggregator,
            IRepository<SellerGraphQLModel> sellerService,
            IRepository<ZoneGraphQLModel> zoneService,
            Helpers.Services.INotificationService notificationService,
            CostCenterCache costCenterCache,
            IdentificationTypeCache identificationTypeCache,
            CountryCache countryCache,
            ZoneCache zoneCache)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
            _sellerService = sellerService;
            _zoneService = zoneService;
            _notificationService = notificationService;
            _costCenterCache = costCenterCache;
            _countryCache = countryCache;
            _identificationTypeCache = identificationTypeCache;
            _zoneCache = zoneCache;
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
                        ThemedMessageBox.Show(title: "Error de inicialización", text: $"Error al activar vista de vendedores: {ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                        return Task.CompletedTask;
                    });
                }
            });
        }

        public async Task ActivateMasterViewAsync()
        {
            try
            {
                await ActivateItemAsync(SellerMasterViewModel, new System.Threading.CancellationToken());
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
                SellerDetailViewModel instance = new SellerDetailViewModel(this, _sellerService, _zoneService, _costCenterCache, _identificationTypeCache, _countryCache, _zoneCache);
                await instance.InitializeAsync();
                instance.CleanUpControls();
                
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
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

                throw new AsyncException(innerException: ex);
            }
        }

        public async Task ActivateDetailViewForEditAsync(int sellerId)
        {
            try
            {
               
                SellerDetailViewModel instance = new SellerDetailViewModel(this, _sellerService, _zoneService, _costCenterCache, _identificationTypeCache, _countryCache, _zoneCache);
                await instance.InitializeAsync();
                SellerGraphQLModel seller =  await instance.LoadDataForEditAsync(sellerId);
                instance.AcceptChanges();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
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

                throw new AsyncException(innerException: ex);
            }
        }
    }
}
