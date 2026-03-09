using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using Models.Books;
using NetErp.Helpers.Cache;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Books.WithholdingCertificateConfig.ViewModels
{
    public class WithholdingCertificateConfigViewModel : Conductor<object>.Collection.OneActive
    {
        #region Dependencies

        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; private set; }

        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<WithholdingCertificateConfigGraphQLModel> _withholdingCertificateConfigService;
        private readonly AccountingAccountGroupCache _accountingAccountGroupCache;
        private readonly CostCenterCache _costCenterCache;

        #endregion

        #region Child ViewModels

        private WithholdingCertificateConfigMasterViewModel? _withholdingCertificateConfigMasterViewModel;
        public WithholdingCertificateConfigMasterViewModel WithholdingCertificateConfigMasterViewModel
        {
            get
            {
                _withholdingCertificateConfigMasterViewModel ??= new WithholdingCertificateConfigMasterViewModel(
                    this, _notificationService, _withholdingCertificateConfigService);
                return _withholdingCertificateConfigMasterViewModel;
            }
        }

        #endregion

        #region Constructor

        public WithholdingCertificateConfigViewModel(
            IMapper mapper,
            IEventAggregator eventAggregator,
            Helpers.Services.INotificationService notificationService,
            IRepository<WithholdingCertificateConfigGraphQLModel> withholdingCertificateConfigService,
            AccountingAccountGroupCache accountingAccountGroupCache,
            CostCenterCache costCenterCache)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
            _notificationService = notificationService;
            _withholdingCertificateConfigService = withholdingCertificateConfigService;
            _accountingAccountGroupCache = accountingAccountGroupCache;
            _costCenterCache = costCenterCache;

            _ = ActivateMasterViewModelAsync();
        }

        #endregion

        #region Navigation

        public async Task ActivateMasterViewModelAsync()
        {
            await ActivateItemAsync(WithholdingCertificateConfigMasterViewModel, new CancellationToken());
        }

        public async Task ActivateDetailViewForEdit(WithholdingCertificateConfigGraphQLModel selectedItem)
        {
            var instance = new WithholdingCertificateConfigDetailViewModel(
                this, _withholdingCertificateConfigService, _accountingAccountGroupCache, _costCenterCache,
                editId: selectedItem.Id);
            await ActivateItemAsync(instance, new CancellationToken());
        }

        public async Task ActivateDetailViewForNew()
        {
            var instance = new WithholdingCertificateConfigDetailViewModel(
                this, _withholdingCertificateConfigService, _accountingAccountGroupCache, _costCenterCache);
            await ActivateItemAsync(instance, new CancellationToken());
        }

        #endregion
    }
}
