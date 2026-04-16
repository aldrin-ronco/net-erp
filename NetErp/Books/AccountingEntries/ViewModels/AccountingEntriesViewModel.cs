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
using NetErp.Helpers.GraphQLQueryBuilder;
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


        private readonly IGraphQLClient _graphQLClient;
        private readonly IRepository<AccountingEntityGraphQLModel> _accountingEntityService;
        private readonly IRepository<DraftAccountingEntryLineGraphQLModel> _draftAccountingEntryLineService;
        private readonly IRepository<DraftAccountingEntryGraphQLModel> _draftAccountingEntryService;
        private readonly IRepository<AccountingEntryGraphQLModel> _accountingEntryMasterService;
        private readonly NotAnnulledAccountingSourceCache _notAnnulledAccountingSourceCache;

        private readonly CostCenterCache _costCenterCache;
        private readonly AccountingBookCache _accountingBookCache;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly Helpers.IDialogService _dialogService;


        private readonly Helpers.Services.INotificationService _notificationService;

        public IMapper Mapper { get; private set; }

        public IEventAggregator EventAggregator;

       

      

        private AccountingEntriesMasterViewModel _accountingEntriesMasterViewModel;
        public AccountingEntriesMasterViewModel AccountingEntriesMasterViewModel
        {
            get
            {
                if (_accountingEntriesMasterViewModel == null) this._accountingEntriesMasterViewModel = new AccountingEntriesMasterViewModel(this, _notificationService, this._accountingEntryMasterService, this._draftAccountingEntryService, this._accountingEntityService, _costCenterCache, _accountingBookCache, _notAnnulledAccountingSourceCache, _graphQLClient);
                return _accountingEntriesMasterViewModel;
            }
        }

        public AccountingEntriesViewModel(IMapper mapper,
                                          IEventAggregator eventAggregator,
                                          Helpers.Services.INotificationService notificationService,
                                          IRepository<AccountingEntityGraphQLModel> accountingEntityService,
                                          IRepository<DraftAccountingEntryLineGraphQLModel> accountingEntryDraftLineService,
                                          IRepository<DraftAccountingEntryGraphQLModel> accountingEntryDraftMasterService,
                                          IRepository<AccountingEntryGraphQLModel> accountingEntryMasterService,
             CostCenterCache costCenterCache,
             AccountingBookCache accountingBookCache,
             NotAnnulledAccountingSourceCache notAnnulledAccountingSourceCache,
             AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache,
             StringLengthCache stringLengthCache,
             Helpers.IDialogService dialogService,
             IGraphQLClient graphQLClient
                                          )
        {
            this.EventAggregator = eventAggregator;
            this.Mapper = mapper;
            this._accountingEntityService = accountingEntityService;
            this._draftAccountingEntryLineService = accountingEntryDraftLineService;
            this._draftAccountingEntryService = accountingEntryDraftMasterService;
            this._accountingEntryMasterService = accountingEntryMasterService;
            this._notificationService = notificationService;
            _costCenterCache = costCenterCache;
            _accountingBookCache = accountingBookCache;
            _notAnnulledAccountingSourceCache = notAnnulledAccountingSourceCache;
            _auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;
            _stringLengthCache = stringLengthCache;
            _dialogService = dialogService;
            _graphQLClient = graphQLClient;
        }

        public StringLengthCache StringLengthCache => _stringLengthCache;

        protected override async Task OnInitializedAsync(CancellationToken cancellationToken)
        {
            // Carga centralizada: una sola request HTTP para todos los caches del módulo.
            // Los cache singletons ya inicializados en otra ventana se saltan automáticamente.
            // Master y Detail leen las colecciones locales de estos mismos caches ya poblados.
            await CacheBatchLoader.LoadAsync(
                _graphQLClient,
                cancellationToken,
                _costCenterCache,
                _accountingBookCache,
                _notAnnulledAccountingSourceCache,
                _auxiliaryAccountingAccountCache);

            await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.AccountingEntries);

            await ActivateMasterViewAsync();
            await base.OnInitializedAsync(cancellationToken);
        }

        public async Task ActivateMasterViewAsync()
        {
            await ActivateItemAsync(this.AccountingEntriesMasterViewModel, new System.Threading.CancellationToken());
        }

        public async Task ActivateDocumentPreviewViewAsync(AccountingEntryDTO selectedAccountingEntry)
        {
            AccountingEntriesDocumentPreviewViewModel instance = new(this, selectedAccountingEntry, this._accountingEntryMasterService, this._draftAccountingEntryService);
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForNewAsync(ObservableCollection<AccountingBookGraphQLModel> accountingBooks,
            ObservableCollection<CostCenterGraphQLModel> costCenters,
            ObservableCollection<AccountingSourceGraphQLModel> accountingSources)
        {
            // Las colecciones recibidas del Master se ignoran porque SetForNew lee directo
            // del cache singleton (mismo dato). Se conservan en la firma por compatibilidad
            // con el call site del Master; pueden eliminarse en una iteración posterior.
            AccountingEntriesDetailViewModel instance = new(this,
                this._accountingEntryMasterService,
                this._accountingEntityService,
                this._draftAccountingEntryService,
                this._draftAccountingEntryLineService,
                this._costCenterCache, this._accountingBookCache, this._notAnnulledAccountingSourceCache, this._auxiliaryAccountingAccountCache, _dialogService, _graphQLClient);

            instance.SetForNew();

            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadDraftWithLinesQuery = new(() =>
        {
            var fields = FieldSpec<DraftAccountingEntryGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Field(f => f.Description)
                .Field(f => f.DocumentDate)
                .Field(f => f.DocumentNumber)
                .SelectList(f => f.Lines, l => l
                    .Field(x => x.Id)
                    .Field(x => x.RecordDetail)
                    .Field(x => x.Debit)
                    .Field(x => x.Credit)
                    .Field(x => x.Base)
                    .Select(x => x.AccountingAccount, a => a.Field(y => y.Id).Field(y => y.Code).Field(y => y.Name))
                    .Select(x => x.AccountingEntity, a => a.Field(y => y.Id).Field(y => y.IdentificationNumber).Field(y => y.SearchName))
                    .Select(x => x.CostCenter, c => c.Field(y => y.Id).Field(y => y.Name)))
                .Build();

            var fragment = new GraphQLQueryFragment("draftAccountingEntry",
                [new("id", "ID!")],
                fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        public async Task ActivateDetailViewForEditAsync(DraftAccountingEntryGraphQLModel model)
        {
            AccountingEntriesDetailViewModel instance = new(this, this._accountingEntryMasterService, this._accountingEntityService, this._draftAccountingEntryService, this._draftAccountingEntryLineService, this._costCenterCache, this._accountingBookCache, this._notAnnulledAccountingSourceCache, this._auxiliaryAccountingAccountCache, _dialogService, _graphQLClient);

            var (fragment, query) = _loadDraftWithLinesQuery.Value;
            object variables = new GraphQLVariables()
                .For(fragment, "id", (int)model.Id)
                .Build();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var draft = await this._draftAccountingEntryService.FindByIdAsync(query, variables);
            stopwatch.Stop();

            IEnumerable<DraftAccountingEntryLineGraphQLModel> lines = draft?.Lines ?? [];
            var mappedEntries = this.Mapper.Map<IEnumerable<DraftAccountingEntryLineDTO>>(lines);
            decimal totalDebit = lines.Sum(e => e.Debit);
            decimal totalCredit = lines.Sum(e => e.Credit);
            string responseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

            instance.SetForEdit(model, mappedEntries, totalDebit, totalCredit, responseTime);

            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

      
        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                // Desconectar eventos para evitar memory leaks (solo cuando el tab se cierra,
                // no al cambiar de tab — el sistema MDI mantiene la instancia viva).
                EventAggregator.Unsubscribe(this);
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }
    }
}
