using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using NetErp.Books.AccountingEntries.DTO;
using NetErp.Helpers.Cache;
using Ninject.Activation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NetErp.Books.AccountingEntries.ViewModels
{
    public class AccountingEntriesViewModel : Conductor<Screen>.Collection.OneActive
       
        


    {


        private readonly IRepository<AccountingEntityGraphQLModel> _accountingEntityService;
        private readonly IRepository<AccountingAccountGraphQLModel> _accountingAccountService;
        private readonly IRepository<AccountingEntryDraftDetailGraphQLModel> _accountingEntryDraftDetailService;
        private readonly IRepository<AccountingEntryDraftGraphQLModel> _accountingEntryDraftMasterService;
        private readonly IRepository<AccountingEntryGraphQLModel> _accountingEntryMasterService;
        private readonly NotAnnulledAccountingSourceCache _notAnnulledAccountingSourceCache;

        private readonly CostCenterCache _costCenterCache;
        private readonly AccountingBookCache _accountingBookCache;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;


        private readonly Helpers.Services.INotificationService _notificationService;

        public IMapper Mapper { get; private set; }

        public IEventAggregator EventAggregator;

       

      

        private AccountingEntriesMasterViewModel _accountingEntriesMasterViewModel;
        public AccountingEntriesMasterViewModel AccountingEntriesMasterViewModel
        {
            get
            {
                if (_accountingEntriesMasterViewModel == null) this._accountingEntriesMasterViewModel = new AccountingEntriesMasterViewModel(this, _notificationService, this._accountingEntryMasterService, this._accountingEntryDraftMasterService, this._accountingEntityService, _costCenterCache, _accountingBookCache, _notAnnulledAccountingSourceCache);
                return _accountingEntriesMasterViewModel;
            }
        }

        public AccountingEntriesViewModel(IMapper mapper,
                                          IEventAggregator eventAggregator,
                                          Helpers.Services.INotificationService notificationService,
                                          IRepository<AccountingEntityGraphQLModel> accountingEntityService,
                                          IRepository<AccountingAccountGraphQLModel> accountingAccountService,
                                          IRepository<AccountingEntryDraftDetailGraphQLModel> accountingEntryDraftDetailService,
                                          IRepository<AccountingEntryDraftGraphQLModel> accountingEntryDraftMasterService,
                                          IRepository<AccountingEntryGraphQLModel> accountingEntryMasterService,
                                          IRepository<AccountingEntryDetailGraphQLModel> accountingEntryDetailService,
             CostCenterCache costCenterCache,
             AccountingBookCache accountingBookCache,
             NotAnnulledAccountingSourceCache notAnnulledAccountingSourceCache,
             AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache
                                          )
        {
            this.EventAggregator = eventAggregator;
            this.Mapper = mapper;
            this._accountingEntityService = accountingEntityService;
            this._accountingAccountService = accountingAccountService;
            this._accountingEntryDraftDetailService = accountingEntryDraftDetailService;
            this._accountingEntryDraftMasterService = accountingEntryDraftMasterService;
            this._accountingEntryMasterService = accountingEntryMasterService;
            this._notificationService = notificationService;
            _costCenterCache = costCenterCache;
            _accountingBookCache = accountingBookCache;
            _notAnnulledAccountingSourceCache = notAnnulledAccountingSourceCache;
            _auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;

            _ = this.ActivateMasterViewAsync();
            
        }

        public async Task ActivateMasterViewAsync()
        {
            await ActivateItemAsync(this.AccountingEntriesMasterViewModel, new System.Threading.CancellationToken());
        }

        public async Task ActivateDocumentPreviewViewAsync(AccountingEntryMasterDTO selectedAccountingEntry)
        {
            AccountingEntriesDocumentPreviewViewModel instance = new(this, selectedAccountingEntry, this._accountingEntryMasterService, this._accountingEntryDraftMasterService);
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForNewAsync(ObservableCollection<AccountingBookGraphQLModel> accountingBooks,
            ObservableCollection<CostCenterGraphQLModel> costCenters,
            ObservableCollection<AccountingSourceGraphQLModel> accountingSources)
        {
            AccountingEntriesDetailViewModel instance = new(this,
                this._accountingEntryMasterService,
                this._accountingEntityService,
                this._accountingEntryDraftMasterService,
                this._accountingEntryDraftDetailService,
                this._accountingAccountService,
                this._costCenterCache, this._accountingBookCache, this._notAnnulledAccountingSourceCache, this._auxiliaryAccountingAccountCache );
            // Header
            instance.SelectedAccountingEntryDraftMaster = null;
            instance.DraftMasterId = 0;
            instance.SelectedAccountingBookId = accountingBooks.FirstOrDefault().Id;
            instance.SelectedCostCenterId = costCenters.FirstOrDefault().Id;
            instance.SelectedAccountingSourceId = accountingSources.FirstOrDefault().Id;
            instance.SelectedCostCenterOnEntryId = costCenters.FirstOrDefault().Id;
            instance.AccountingEntries = new ObservableCollection<AccountingEntryDraftDetailDTO>();
            instance.EntriesPageIndex = 1;
            instance.EntriesPageSize = 50;
            instance.EntriesTotalCount = 0;
            instance.EntriesResponseTime = "";
            instance.TotalDebit = 0;
            instance.TotalCredit = 0;
            instance.DocumentDate = DateTime.Now.Date;
            instance.Description = "";

            // Entry Point
            instance.SelectedAccountingAccountOnEntryId = 0;
            instance.SelectedAccountingEntityOnEntryId = 0;
            instance.SelectedCostCenterOnEntryId = 0;
            instance.RecordDetail = "";
            instance.Debit = 0;
            instance.Credit = 0;
            instance.Base = 0;
            instance.IsFilterSearchAccountinEntityOnEditMode = true;
            instance.FilterSearchAccountingEntity = "";

            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForEditAsync(AccountingEntryDraftGraphQLModel model)
        {
            try
            {
                object variables;
                AccountingEntriesDetailViewModel instance = new(this, this._accountingEntryMasterService, this._accountingEntityService, this._accountingEntryDraftMasterService, this._accountingEntryDraftDetailService, this._accountingAccountService, this._costCenterCache, this._accountingBookCache, this._notAnnulledAccountingSourceCache, this._auxiliaryAccountingAccountCache);
                string query = @"
                query($draftMasterId:ID) {
                  ListResponse: accountingEntriesDraftDetail(draftMasterId: $draftMasterId) {
                    id
                    costCenter {
                    id
                    name
                    }
                    accountingEntity {
                    id
                    identificationNumber
                    searchName
                    }
                    accountingAccount {
                    id
                    code
                    name
                    }
                    draftMasterId
                    recordDetail
                    debit
                    credit
                    base
                    }
                }";

                variables = new
                {
                    draftMasterId = model.Id,
                };

                // Iniciar cronometro
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                // Get Entries
                var entries = await this._accountingEntryDraftDetailService.GetListAsync(query, variables);

                // Totals
                var totals =(
                    from entry in entries
                    select new{entry.Credit, entry.Debit}).ToList();

                //query = @"
                //query($draftMasterId:ID){
                //    AccountingEntryTotals: accountingEntryDraftTotals(draftMasterId:$draftMasterId) {
                //    debit
                //    credit
                //    }
                //}";

                //variables = new
                //{
                //    DraftMasterId = model.Id,
                //};
                //var totals = await AccountingEntryDraftMasterService.GetDataContext<AccountingEntryTotals>(query, variables);
                //stopwatch.Stop();


                instance.TotalCredit = totals.Sum(c => c.Credit);
                instance.TotalDebit = totals.Sum(d => d.Debit);

                //instance.EntriesTotalCount = entries.PageResponse.Count;
                // Others            

                // Header
                instance.SelectedAccountingEntryDraftMaster = model;
                instance.EntriesResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
                instance.DraftMasterId = model.Id;
                instance.SelectedAccountingBookId = model.AccountingBook.Id;
                instance.SelectedCostCenterId = model.CostCenter.Id;
                instance.SelectedAccountingSourceId = model.AccountingSource.Id;
                instance.DocumentDate = model.DocumentDate;
                instance.Description = model.Description;
                instance.AccountingEntries = new ObservableCollection<AccountingEntryDraftDetailDTO>(this.Mapper.Map<IEnumerable<AccountingEntryDraftDetailDTO>>(entries));

                // Entry Point
                instance.SelectedAccountingAccountOnEntryId = 0;
                instance.SelectedAccountingEntityOnEntryId = 0;
                instance.SelectedCostCenterOnEntryId = 0;
                instance.RecordDetail = "";
                instance.Debit = 0;
                instance.Credit = 0;
                instance.Base = 0;
                instance.IsFilterSearchAccountinEntityOnEditMode = true;
                instance.FilterSearchAccountingEntity = "";

                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {
                throw;
            }
        }

      
        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {

            // Desconectar eventos para evitar memory leaks
            EventAggregator.Unsubscribe(this);
            return base.OnDeactivateAsync(close, cancellationToken);
        }
    }
}
