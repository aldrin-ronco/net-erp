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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NetErp.Books.AccountingEntries.ViewModels
{
    public class AccountingEntriesViewModel : Conductor<Screen>.Collection.OneActive
    {

  
        public readonly IRepository<AccountingEntityGraphQLModel> _accountingEntityService;
        public readonly IRepository<AccountingAccountGraphQLModel> _accountingAccountService;
        public readonly IRepository<AccountingEntryDraftDetailGraphQLModel> _accountingEntryDraftDetailService;
        public readonly IRepository<AccountingEntryDraftMasterGraphQLModel> _accountingEntryDraftMasterService;
        public readonly IRepository<AccountingEntryMasterGraphQLModel> _accountingEntryMasterService;
        public readonly IRepository<AccountingEntryDetailGraphQLModel> _accountingEntryDetailService;



        private readonly Helpers.Services.INotificationService _notificationService;

        public IMapper Mapper { get; private set; }

        public IEventAggregator EventAggregator;

        // Libros contables
        private ObservableCollection<AccountingBookGraphQLModel> _accountingBooks;
        public ObservableCollection<AccountingBookGraphQLModel> AccountingBooks
        {
            get { return _accountingBooks; }
            set
            {
                if (_accountingBooks != value)
                {
                    _accountingBooks = value;
                    NotifyOfPropertyChange(nameof(AccountingBooks));
                }
            }
        }

        //CostCenters
        private ObservableCollection<CostCenterGraphQLModel> _costCenters;
        public ObservableCollection<CostCenterGraphQLModel> CostCenters
        {
            get { return _costCenters; }
            set
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        }

        // AccountingSources
        private ObservableCollection<AccountingSourceGraphQLModel> _accountingSources;
        public ObservableCollection<AccountingSourceGraphQLModel> AccountingSources
        {
            get { return _accountingSources; }
            set
            {
                if (_accountingSources != value)
                {
                    _accountingSources = value;
                    NotifyOfPropertyChange(nameof(AccountingSources));
                }
            }
        }

        // Cuentas Contables
        private ObservableCollection<AccountingAccountGraphQLModel> _accountingAccounts;
        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccounts
        {
            get { return _accountingAccounts; }
            set
            {
                if (_accountingAccounts != value)
                {
                    _accountingAccounts = value;
                    NotifyOfPropertyChange(nameof(AccountingAccounts));
                }
            }
        }

        private AccountingEntriesMasterViewModel _accountingEntriesMasterViewModel;
        public AccountingEntriesMasterViewModel AccountingEntriesMasterViewModel
        {
            get
            {
                if (_accountingEntriesMasterViewModel == null) this._accountingEntriesMasterViewModel = new AccountingEntriesMasterViewModel(this, _notificationService);
                return _accountingEntriesMasterViewModel;
            }
        }

        public AccountingEntriesViewModel(IMapper mapper,
                                          IEventAggregator eventAggregator,
                                          Helpers.Services.INotificationService notificationService,
                                          IRepository<AccountingEntityGraphQLModel> accountingEntityService,
                                          IRepository<AccountingAccountGraphQLModel> accountingAccountService,
                                          IRepository<AccountingEntryDraftDetailGraphQLModel> accountingEntryDraftDetailService,
                                          IRepository<AccountingEntryDraftMasterGraphQLModel> accountingEntryDraftMasterService,
                                          IRepository<AccountingEntryMasterGraphQLModel> accountingEntryMasterService,
                                          IRepository<AccountingEntryDetailGraphQLModel> accountingEntryDetailService
                                          )
        {
            this.EventAggregator = eventAggregator;
            this.Mapper = mapper;
            this._accountingEntityService = accountingEntityService;
            this._accountingAccountService = accountingAccountService;
            this._accountingEntryDraftDetailService = accountingEntryDraftDetailService;
            this._accountingEntryDraftMasterService = accountingEntryDraftMasterService;
            this._accountingEntryMasterService = accountingEntryMasterService;
            this._accountingEntryDetailService = accountingEntryDetailService;
            this._notificationService = notificationService;
            _ = this.ActivateMasterViewAsync();
            
        }

        public async Task ActivateMasterViewAsync()
        {
            await ActivateItemAsync(this.AccountingEntriesMasterViewModel, new System.Threading.CancellationToken());
        }

        public async Task ActivateDocumentPreviewViewAsync(AccountingEntryMasterDTO selectedAccountingEntry)
        {
            AccountingEntriesDocumentPreviewViewModel instance = new(this, selectedAccountingEntry);
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForNewAsync()
        {
            AccountingEntriesDetailViewModel instance = new(this);
            // Header
            instance.SelectedAccountingEntryDraftMaster = null;
            instance.DraftMasterId = 0;
            instance.SelectedAccountingBookId = this.AccountingBooks.FirstOrDefault().Id;
            instance.SelectedCostCenterId = this.CostCenters.FirstOrDefault().Id;
            instance.SelectedAccountingSourceId = this.AccountingSources.FirstOrDefault().Id;
            instance.SelectedCostCenterOnEntryId = this.CostCenters.FirstOrDefault().Id;
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

        public async Task ActivateDetailViewForEdit(AccountingEntryDraftMasterGraphQLModel model)
        {
            try
            {
                object variables;
                AccountingEntriesDetailViewModel instance = new(this);
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
    }
}
