using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using Models.Treasury;
using Models.Global;
using NetErp.Global.CostCenters.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Treasury.Masters.ViewModels
{
    public class TreasuryRootViewModel : Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; set; }

        private readonly IRepository<CashDrawerGraphQLModel> _cashDrawerService;
        private readonly IRepository<BankGraphQLModel> _bankService;
        private readonly IRepository<BankAccountGraphQLModel> _bankAccountService;
        private readonly IRepository<FranchiseGraphQLModel> _franchiseService;
        private readonly NetErp.Helpers.IDialogService _dialogService;
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

        private TreasuryRootMasterViewModel _treasuryRootMasterViewModel;
        public TreasuryRootMasterViewModel TreasuryRootMasterViewModel
        {
            get
            {
                if (_treasuryRootMasterViewModel is null)
                    _treasuryRootMasterViewModel = new TreasuryRootMasterViewModel(
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
                        _graphQLClient);
                return _treasuryRootMasterViewModel;
            }
        }

        public TreasuryRootViewModel(
            IMapper mapper,
            IEventAggregator eventAggregator,
            IRepository<CashDrawerGraphQLModel> cashDrawerService,
            IRepository<BankGraphQLModel> bankService,
            IRepository<BankAccountGraphQLModel> bankAccountService,
            IRepository<FranchiseGraphQLModel> franchiseService,
            NetErp.Helpers.IDialogService dialogService,
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
            IGraphQLClient graphQLClient)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            _cashDrawerService = cashDrawerService;
            _bankService = bankService;
            _bankAccountService = bankAccountService;
            _franchiseService = franchiseService;
            _dialogService = dialogService;
            _notificationService = notificationService;
            _auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;
            _companyLocationCache = companyLocationCache;
            _costCenterCache = costCenterCache;
            _bankAccountCache = bankAccountCache;
            _majorCashDrawerCache = majorCashDrawerCache;
            _minorCashDrawerCache = minorCashDrawerCache;
            _auxiliaryCashDrawerCache = auxiliaryCashDrawerCache;
            _bankCache = bankCache;
            _franchiseCache = franchiseCache;
            _graphQLClient = graphQLClient;
            _ = Task.Run(ActivateMasterView);
        }

        public async Task ActivateMasterView()
        {
            try
            {
                await ActivateItemAsync(TreasuryRootMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
