using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Billing;
using Models.Books;
using NetErp.Books.AccountingBooks.ViewModels;
using NetErp.Helpers.Cache;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Books.AccountingGroups.ViewModels
{
    public class AccountingGroupViewModel : Conductor<object>.Collection.OneActive
    {
       
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; set; }
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AccountingGroupGraphQLModel> _accountingGroupService;
        private AccountingGroupMasterViewModel _accountingGroupMasterViewModel;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;
        private readonly TaxCache _taxCache;


        private AccountingGroupMasterViewModel AccountingGroupMasterViewModel
        {
            get
            {
                if (_accountingGroupMasterViewModel is null) _accountingGroupMasterViewModel = new AccountingGroupMasterViewModel(this, _notificationService, _accountingGroupService);
                return _accountingGroupMasterViewModel;

            }
        }
        public AccountingGroupViewModel(IMapper mapper, IEventAggregator eventAggregator, Helpers.Services.INotificationService notificationService,
            IRepository<AccountingGroupGraphQLModel> accountingGroupService, AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache, TaxCache taxCache)
        {
            _accountingGroupService = accountingGroupService;
            _notificationService = notificationService;
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            _auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;
            _ = Task.Run(ActivateMasterViewAsync);
            _taxCache = taxCache;
        }

        public async Task ActivateMasterViewAsync()
        {
            try
            {
                await ActivateItemAsync(AccountingGroupMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task ActivateDetailViewForEditAsync(int id)
        {
            try
            {
                AccountingGroupDetailViewModel instance = new(this, _accountingGroupService, _auxiliaryAccountingAccountCache, _taxCache);
                AccountingGroupGraphQLModel group = await instance.LoadDataForEditAsync(id);
                instance.AcceptChanges();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch
            {

            }
        }
        public async Task ActivateDetailViewForNewAsync()
        {
            try
            {
                AccountingGroupDetailViewModel instance = new(this, _accountingGroupService, _auxiliaryAccountingAccountCache, _taxCache);
                //instance.CleanUpControls();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}

