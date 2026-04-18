using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Treasury;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.Services;
using NetErp.Treasury.Masters.Validators;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NetErp.Treasury.Masters.ViewModels
{
    public class TreasuryRootViewModel : Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; }
        public IEventAggregator EventAggregator { get; }

        private readonly IRepository<CashDrawerGraphQLModel> _cashDrawerService;
        private readonly IRepository<BankGraphQLModel> _bankService;
        private readonly IRepository<BankAccountGraphQLModel> _bankAccountService;
        private readonly IRepository<FranchiseGraphQLModel> _franchiseService;
        private readonly IDialogService _dialogService;
        private readonly INotificationService _notificationService;
        private readonly IGraphQLClient _graphQLClient;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;
        private readonly CompanyLocationCache _companyLocationCache;
        private readonly CostCenterCache _costCenterCache;
        private readonly BankAccountCache _bankAccountCache;
        private readonly MajorCashDrawerCache _majorCashDrawerCache;
        private readonly MinorCashDrawerCache _minorCashDrawerCache;
        private readonly AuxiliaryCashDrawerCache _auxiliaryCashDrawerCache;
        private readonly BankCache _bankCache;
        private readonly FranchiseCache _franchiseCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly BankValidator _bankValidator;
        private readonly BankAccountValidator _bankAccountValidator;
        private readonly FranchiseValidator _franchiseValidator;
        private readonly MajorCashDrawerValidator _majorCashDrawerValidator;
        private readonly MinorCashDrawerValidator _minorCashDrawerValidator;
        private readonly AuxiliaryCashDrawerValidator _auxiliaryCashDrawerValidator;

        public TreasuryRootMasterViewModel TreasuryRootMasterViewModel =>
            field ??= new TreasuryRootMasterViewModel(
                this,
                _cashDrawerService,
                _bankService,
                _bankAccountService,
                _franchiseService,
                _dialogService,
                _notificationService,
                _auxiliaryAccountingAccountCache,
                _companyLocationCache,
                _costCenterCache,
                _bankAccountCache,
                _majorCashDrawerCache,
                _minorCashDrawerCache,
                _auxiliaryCashDrawerCache,
                _bankCache,
                _franchiseCache,
                _graphQLClient,
                _stringLengthCache,
                _joinableTaskFactory,
                _bankValidator,
                _bankAccountValidator,
                _franchiseValidator,
                _majorCashDrawerValidator,
                _minorCashDrawerValidator,
                _auxiliaryCashDrawerValidator);

        public TreasuryRootViewModel(
            IMapper mapper,
            IEventAggregator eventAggregator,
            IRepository<CashDrawerGraphQLModel> cashDrawerService,
            IRepository<BankGraphQLModel> bankService,
            IRepository<BankAccountGraphQLModel> bankAccountService,
            IRepository<FranchiseGraphQLModel> franchiseService,
            IDialogService dialogService,
            INotificationService notificationService,
            AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache,
            CompanyLocationCache companyLocationCache,
            CostCenterCache costCenterCache,
            BankAccountCache bankAccountCache,
            MajorCashDrawerCache majorCashDrawerCache,
            MinorCashDrawerCache minorCashDrawerCache,
            AuxiliaryCashDrawerCache auxiliaryCashDrawerCache,
            BankCache bankCache,
            FranchiseCache franchiseCache,
            IGraphQLClient graphQLClient,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            BankValidator bankValidator,
            BankAccountValidator bankAccountValidator,
            FranchiseValidator franchiseValidator,
            MajorCashDrawerValidator majorCashDrawerValidator,
            MinorCashDrawerValidator minorCashDrawerValidator,
            AuxiliaryCashDrawerValidator auxiliaryCashDrawerValidator)
        {
            AutoMapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _cashDrawerService = cashDrawerService ?? throw new ArgumentNullException(nameof(cashDrawerService));
            _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
            _bankAccountService = bankAccountService ?? throw new ArgumentNullException(nameof(bankAccountService));
            _franchiseService = franchiseService ?? throw new ArgumentNullException(nameof(franchiseService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache ?? throw new ArgumentNullException(nameof(auxiliaryAccountingAccountCache));
            _companyLocationCache = companyLocationCache ?? throw new ArgumentNullException(nameof(companyLocationCache));
            _costCenterCache = costCenterCache ?? throw new ArgumentNullException(nameof(costCenterCache));
            _bankAccountCache = bankAccountCache ?? throw new ArgumentNullException(nameof(bankAccountCache));
            _majorCashDrawerCache = majorCashDrawerCache ?? throw new ArgumentNullException(nameof(majorCashDrawerCache));
            _minorCashDrawerCache = minorCashDrawerCache ?? throw new ArgumentNullException(nameof(minorCashDrawerCache));
            _auxiliaryCashDrawerCache = auxiliaryCashDrawerCache ?? throw new ArgumentNullException(nameof(auxiliaryCashDrawerCache));
            _bankCache = bankCache ?? throw new ArgumentNullException(nameof(bankCache));
            _franchiseCache = franchiseCache ?? throw new ArgumentNullException(nameof(franchiseCache));
            _graphQLClient = graphQLClient ?? throw new ArgumentNullException(nameof(graphQLClient));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _joinableTaskFactory = joinableTaskFactory ?? throw new ArgumentNullException(nameof(joinableTaskFactory));
            _bankValidator = bankValidator ?? throw new ArgumentNullException(nameof(bankValidator));
            _bankAccountValidator = bankAccountValidator ?? throw new ArgumentNullException(nameof(bankAccountValidator));
            _franchiseValidator = franchiseValidator ?? throw new ArgumentNullException(nameof(franchiseValidator));
            _majorCashDrawerValidator = majorCashDrawerValidator ?? throw new ArgumentNullException(nameof(majorCashDrawerValidator));
            _minorCashDrawerValidator = minorCashDrawerValidator ?? throw new ArgumentNullException(nameof(minorCashDrawerValidator));
            _auxiliaryCashDrawerValidator = auxiliaryCashDrawerValidator ?? throw new ArgumentNullException(nameof(auxiliaryCashDrawerValidator));
        }

        protected override async Task OnActivatedAsync(CancellationToken cancellationToken)
        {
            await base.OnActivatedAsync(cancellationToken);
            try
            {
                await ActivateItemAsync(TreasuryRootMasterViewModel, cancellationToken);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Error de inicialización",
                    text: $"{GetType().Name}.{nameof(OnActivatedAsync)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
        }
    }
}
