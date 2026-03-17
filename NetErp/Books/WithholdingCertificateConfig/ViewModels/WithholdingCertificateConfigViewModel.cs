using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using Models.Books;
using Microsoft.VisualStudio.Threading;
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
        private readonly Helpers.IDialogService _dialogService;
        private readonly IRepository<WithholdingCertificateConfigGraphQLModel> _withholdingCertificateConfigService;
        private readonly IRepository<AccountingAccountGroupGraphQLModel> _accountingAccountGroupService;
        private readonly AccountingAccountGroupCache _accountingAccountGroupCache;
        private readonly CostCenterCache _costCenterCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        #endregion

        #region Child ViewModels

        private WithholdingCertificateConfigMasterViewModel? _withholdingCertificateConfigMasterViewModel;
        public WithholdingCertificateConfigMasterViewModel WithholdingCertificateConfigMasterViewModel
        {
            get
            {
                _withholdingCertificateConfigMasterViewModel ??= new WithholdingCertificateConfigMasterViewModel(
                    this, _notificationService, _dialogService, _withholdingCertificateConfigService,
                    _accountingAccountGroupService, _accountingAccountGroupCache, _costCenterCache, _stringLengthCache, _joinableTaskFactory);
                return _withholdingCertificateConfigMasterViewModel;
            }
        }

        #endregion

        #region Constructor

        public WithholdingCertificateConfigViewModel(
            IMapper mapper,
            IEventAggregator eventAggregator,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            IRepository<WithholdingCertificateConfigGraphQLModel> withholdingCertificateConfigService,
            IRepository<AccountingAccountGroupGraphQLModel> accountingAccountGroupService,
            AccountingAccountGroupCache accountingAccountGroupCache,
            CostCenterCache costCenterCache,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _withholdingCertificateConfigService = withholdingCertificateConfigService;
            _accountingAccountGroupService = accountingAccountGroupService;
            _accountingAccountGroupCache = accountingAccountGroupCache;
            _costCenterCache = costCenterCache;
            _stringLengthCache = stringLengthCache;
            _joinableTaskFactory = joinableTaskFactory;

            _ = ActivateMasterViewModelAsync();
        }

        #endregion

        #region Navigation

        public async Task ActivateMasterViewModelAsync()
        {
            await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.WithholdingCertificateConfig);
            await ActivateItemAsync(WithholdingCertificateConfigMasterViewModel, new CancellationToken());
        }

        #endregion
    }
}
