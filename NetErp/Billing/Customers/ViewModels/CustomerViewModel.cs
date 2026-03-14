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
using NetErp.Billing.Zones.DTO;
using NetErp.Helpers.Cache;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dictionaries.BooksDictionaries;

namespace NetErp.Billing.Customers.ViewModels
{
    public class CustomerViewModel : Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; set; }

        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<CustomerGraphQLModel> _customerService;

        // Caches
        private readonly IdentificationTypeCache _identificationTypeCache;
        private readonly CountryCache _countryCache;
        private readonly WithholdingTypeCache _withholdingTypeCache;
        private readonly ZoneCache _zoneCache;
        private readonly StringLengthCache _stringLengthCache;

        private CustomerMasterViewModel? _customerMasterViewModel;
        public CustomerMasterViewModel CustomerMasterViewModel
        {
            get
            {
                if (_customerMasterViewModel is null) _customerMasterViewModel = new CustomerMasterViewModel(this, _notificationService, _customerService);
                return _customerMasterViewModel;
            }
        }

        public CustomerViewModel(IMapper mapper,
                                 IEventAggregator eventAggregator,
                                 Helpers.Services.INotificationService notificationService,
                                 IRepository<CustomerGraphQLModel> customerService,
                                 IdentificationTypeCache identificationTypeCache,
                                 CountryCache countryCache,
                                 WithholdingTypeCache withholdingTypeCache,
                                 ZoneCache zoneCache,
                                 StringLengthCache stringLengthCache)
        {
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            AutoMapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _identificationTypeCache = identificationTypeCache ?? throw new ArgumentNullException(nameof(identificationTypeCache));
            _countryCache = countryCache ?? throw new ArgumentNullException(nameof(countryCache));
            _withholdingTypeCache = withholdingTypeCache ?? throw new ArgumentNullException(nameof(withholdingTypeCache));
            _zoneCache = zoneCache ?? throw new ArgumentNullException(nameof(zoneCache));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));

            _ = ActivateMasterViewAsync();
        }

        public async Task ActivateMasterViewAsync()
        {
            try
            {
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.Customer);
                await ActivateItemAsync(CustomerMasterViewModel ?? new CustomerMasterViewModel(this, _notificationService, _customerService), new System.Threading.CancellationToken());
            }
            catch (Exception ex)
            {

                throw new AsyncException(innerException: ex);
            }
        }

        public async Task ActivateDetailViewForEditAsync(int customerId)
        {
            try
            {
                CustomerDetailViewModel instance = new(this, _customerService, _identificationTypeCache, _countryCache, _withholdingTypeCache, _zoneCache, _stringLengthCache);
                await instance.LoadCachesAsync();
                await instance.LoadDataForEditAsync(customerId);
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

        public async Task ActivateDetailViewForNewAsync()
        {
            try
            {
                CustomerDetailViewModel instance = new(this, _customerService, _identificationTypeCache, _countryCache, _withholdingTypeCache, _zoneCache, _stringLengthCache);
                await instance.LoadCachesAsync();
                instance.SetForNew();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch(AsyncException ex)
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
