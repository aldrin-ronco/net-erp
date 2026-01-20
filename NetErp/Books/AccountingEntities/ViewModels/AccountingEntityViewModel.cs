using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using Models.Books;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using System;
using System.Threading.Tasks;

namespace NetErp.Books.AccountingEntities.ViewModels
{
    public class AccountingEntityViewModel : Conductor<object>
    {
        public IMapper AutoMapper { get; private set; }

        public IEventAggregator EventAggregator { get; set; }
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AccountingEntityGraphQLModel> _accountingEntityService;
        private readonly IdentificationTypeCache _identificationTypeCache;
        private readonly CountryCache _countryCache;

        // MasterVM
        private AccountingEntityMasterViewModel? _accountingEntityMasterViewModel;
        public AccountingEntityMasterViewModel AccountingEntityMasterViewModel
        {
            get
            {
                if (_accountingEntityMasterViewModel is null) _accountingEntityMasterViewModel = new AccountingEntityMasterViewModel(this, _notificationService, _accountingEntityService);
                return _accountingEntityMasterViewModel;
            }
        }

        public AccountingEntityViewModel(IMapper mapper,
                                         IEventAggregator eventAggregator,
                                         Helpers.Services.INotificationService notificationService,
                                         IRepository<AccountingEntityGraphQLModel> accountingEntityService,
                                         IdentificationTypeCache identificationTypeCache,
                                         CountryCache countryCache)
        {
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            AutoMapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _accountingEntityService = accountingEntityService ?? throw new ArgumentNullException(nameof(accountingEntityService));
            _identificationTypeCache = identificationTypeCache ?? throw new ArgumentNullException(nameof(identificationTypeCache));
            _countryCache = countryCache ?? throw new ArgumentNullException(nameof(countryCache));

            _ = ActivateMasterViewAsync();
        }

        public async Task ActivateMasterViewAsync()
        {
            await ActivateItemAsync(AccountingEntityMasterViewModel ?? new(this, _notificationService, _accountingEntityService), new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForEditAsync(int id)
        {
            AccountingEntityDetailViewModel instance = new(this, _accountingEntityService, _identificationTypeCache, _countryCache);
            await instance.LoadDataForEditAsync(id);
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForNewAsync()
        {
            AccountingEntityDetailViewModel instance = new(this, _accountingEntityService, _identificationTypeCache, _countryCache);
            await instance.SetDataForNewAsync();
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }
    }
}
