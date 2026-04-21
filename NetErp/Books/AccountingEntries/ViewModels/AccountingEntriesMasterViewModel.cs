using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using Extensions.Books;
using Extensions.Global;

using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Global.Modals.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.AccountingEntries.ViewModels
{
    public class AccountingEntriesMasterViewModel : Screen,
        INotifyDataErrorInfo,
        IHandle<AccountingEntryGraphQLModel>,
        IHandle<DraftAccountingEntryGraphQLModel>,
        IHandle<DraftAccountingEntryDeleteMessage>,
        IHandle<AccountingEntryDeleteMessage>,
        IHandle<AccountingEntryCancellationMessage>,
      
      
        IHandle<DraftAccountingEntryFinalizeMessage>,
        IHandle<DraftAccountingEntryUpdateMessage>
    {
        #region Popiedades
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AccountingEntryGraphQLModel> _accountingEntryMasterService;
        private readonly IRepository<DraftAccountingEntryGraphQLModel> _draftAccountingEntryService;
        private readonly IRepository<AccountingEntityGraphQLModel> _accountingEntityService;

        private readonly IGraphQLClient _graphQLClient;
        private readonly Helpers.IDialogService _dialogService;
        private readonly CostCenterCache _costCenterCache;
        private readonly AccountingBookCache _accountingBookCache;
        private readonly NotAnnulledAccountingSourceCache _notAnnulledAccountingSourceCache;
        // Context
        public AccountingEntriesViewModel Context { get; set; }
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

       
        // Control de errores
        public Dictionary<string, List<string>> _errors = [];

        // Filtros de fecha
        public Dictionary<char, string> DateFilterOptions
        {
            get { return Dictionaries.GlobalDictionaries.DateFilterOptionsDictionary; }
        }

        // Listado de borradores
        private ObservableCollection<DraftAccountingEntryGraphQLModel> _accountingEntryDrafts;
        public ObservableCollection<DraftAccountingEntryGraphQLModel> AccountingEntryDrafts
        {
            get { return _accountingEntryDrafts; }
            set
            {
                if (_accountingEntryDrafts != value)
                {
                    _accountingEntryDrafts = value;
                    NotifyOfPropertyChange(nameof(AccountingEntryDrafts));
                }
            }
        }

        // Listado de comprobantes
        private ObservableCollection<AccountingEntryGraphQLModel> _accountingEntries;
        public ObservableCollection<AccountingEntryGraphQLModel> AccountingEntries
        {
            get { return _accountingEntries; }
            set
            {
                if (_accountingEntries != value)
                {
                    _accountingEntries = value;
                    NotifyOfPropertyChange(nameof(AccountingEntries));
                }
            }
        }

        // Selected Filter Date Option
        private char _selectedDateFilterOption = '=';
        public char SelectedDateFilterOption
        {
            get { return _selectedDateFilterOption; }
            set
            {
                if (_selectedDateFilterOption != value)
                {
                    _selectedDateFilterOption = value;
                    NotifyOfPropertyChange(nameof(SelectedDateFilterOption));
                    NotifyOfPropertyChange(nameof(IsDateRange));
                }
            }
        }

        private AccountingEntityGraphQLModel _filterSelectedAccountingEntity;
        public AccountingEntityGraphQLModel FilterSelectedAccountingEntity
        {
            get => _filterSelectedAccountingEntity;
            set
            {
                if (_filterSelectedAccountingEntity != value)
                {
                    _filterSelectedAccountingEntity = value;
                    NotifyOfPropertyChange(nameof(FilterSelectedAccountingEntity));
                    NotifyOfPropertyChange(nameof(FilterSelectedAccountingEntityDisplay));
                    ValidateProperty(nameof(FilterSelectedAccountingEntity));
                }
            }
        }

        public string FilterSelectedAccountingEntityDisplay =>
            FilterSelectedAccountingEntity is null
                ? string.Empty
                : $"{FilterSelectedAccountingEntity.IdentificationNumber} — {FilterSelectedAccountingEntity.SearchName}";

        // Indicador de aplicacion ocupada
        private bool _isBusy = false;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    NotifyOfPropertyChange(nameof(CanEditDraftEntry));
                }
            }
        }

        /////////////////////////////////////////////////////////////////
        // Checks de Filtros
        /////////////////////////////////////////////////////////////////
        private bool _searchOnAccountingBook = true;
        public bool SearchOnAccountingBook
        {
            get { return _searchOnAccountingBook; }
            set
            {
                if (_searchOnAccountingBook != value)
                {
                    _searchOnAccountingBook = value;
                    if (!value) SelectedAccountingBookId = null;
                    NotifyOfPropertyChange(nameof(SearchOnAccountingBook));
                    ValidateProperty(nameof(SelectedAccountingBookId));
                }
            }
        }

        // Busqueda en Centros de Costos
        private bool _searchOnCostCenter = true;
        public bool SearchOnCostCenter
        {
            get { return _searchOnCostCenter; }
            set
            {
                if (_searchOnCostCenter != value)
                {
                    _searchOnCostCenter = value;
                    if (!value) SelectedCostCenterId = null;
                    NotifyOfPropertyChange(nameof(SearchOnCostCenter));
                    ValidateProperty(nameof(SelectedCostCenterId));
                }
            }
        }

        // Busqueda en Fuentes Contables
        private bool _searchOnAccountingSource = true;
        public bool SearchOnAccountingSource
        {
            get { return _searchOnAccountingSource; }
            set
            {
                if (_searchOnAccountingSource != value)
                {
                    _searchOnAccountingSource = value;
                    if (!value) SelectedAccountingSourceId = null;
                    NotifyOfPropertyChange(nameof(SearchOnAccountingSource));
                    ValidateProperty(nameof(SelectedAccountingSourceId));
                }
            }
        }

        // Buscar en fechas
        private bool _searchOnDate;
        public bool SearchOnDate
        {
            get { return _searchOnDate; }
            set
            {
                if (_searchOnDate != value)
                {
                    _searchOnDate = value;
                    if (!value)
                    {
                        StartDateFilter = DateTime.Now;
                        EndDateFilter = DateTime.Now;
                    }
                    NotifyOfPropertyChange(nameof(SearchOnDate));
                }
            }
        }

        // Busqueda de fecha por rango
        public bool IsDateRange
        {
            get { return this.SelectedDateFilterOption == 'B'; }
        }

        // Fecha Inicial
        private DateTime? _startDateFilter = DateTime.Now;
        public DateTime? StartDateFilter
        {
            get { return _startDateFilter; }
            set
            {
                _startDateFilter = value;
                NotifyOfPropertyChange(nameof(StartDateFilter));
            }
        }

        // Fecha Final
        private DateTime? _endDateFilter = DateTime.Now;
        public DateTime? EndDateFilter
        {
            get { return _endDateFilter; }
            set
            {
                if (_endDateFilter != value)
                {
                    _endDateFilter = value;
                    NotifyOfPropertyChange(nameof(EndDateFilter));
                }
            }
        }

        // Busqueda en numero de dcumento
        private bool _searchOnDocumentNumber;
        public bool SearchOnDocumentNumber
        {
            get { return _searchOnDocumentNumber; }
            set
            {
                if (_searchOnDocumentNumber != value)
                {
                    _searchOnDocumentNumber = value;
                    if (!value) DocumentNumber = string.Empty;
                    NotifyOfPropertyChange(nameof(SearchOnDocumentNumber));
                    ValidateProperty(nameof(DocumentNumber));
                    if (_searchOnDocumentNumber) this.SetFocus(nameof(DocumentNumber));
                }
            }
        }

        private bool _searchOnAccountingEntity = false;
        public bool SearchOnAccountingEntity
        {
            get { return _searchOnAccountingEntity; }
            set
            {
                if (_searchOnAccountingEntity != value)
                {
                    _searchOnAccountingEntity = value;
                    if (!value) FilterSelectedAccountingEntity = null;
                    NotifyOfPropertyChange(nameof(SearchOnAccountingEntity));
                    ValidateProperty(nameof(FilterSelectedAccountingEntity));
                }
            }
        }

        #endregion

        #region Selected Items or Ids

        // Accounting Book
        private int? _selectedAccountingBookId;
        public int? SelectedAccountingBookId
        {
            get { return _selectedAccountingBookId; }
            set
            {
                if (_selectedAccountingBookId != value)
                {
                    _selectedAccountingBookId = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingBookId));
                    ValidateProperty(nameof(SelectedAccountingBookId));
                }
            }
        }

        // Cost Center
        private int? _selectedCostCenterId;
        public int? SelectedCostCenterId
        {
            get { return _selectedCostCenterId; }
            set
            {
                if (_selectedCostCenterId != value)
                {
                    _selectedCostCenterId = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenterId));
                    ValidateProperty(nameof(SelectedCostCenterId));
                }
            }
        }

        // Accounting Source
        private int? _selectedAccountingSourceId;
        public int? SelectedAccountingSourceId
        {
            get { return _selectedAccountingSourceId; }
            set
            {
                if (_selectedAccountingSourceId != value)
                {
                    _selectedAccountingSourceId = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingSourceId));
                    ValidateProperty(nameof(SelectedAccountingSourceId));
                }
            }
        }

        // Document Number
        private string _documentNumber = string.Empty;
        public string DocumentNumber
        {
            get { return _documentNumber; }
            set
            {
                if (_documentNumber != value)
                {
                    _documentNumber = value;
                    NotifyOfPropertyChange(nameof(DocumentNumber));
                    ValidateProperty(nameof(DocumentNumber));
                }
            }
        }

        // Selected Tab Item Acoounting Entries
        private bool _IsSelectedTab1 = true;
        public bool IsSelectedTab1
        {
            get { return _IsSelectedTab1; }
            set
            {
                if (_IsSelectedTab1 != value)
                {
                    _IsSelectedTab1 = value;
                    NotifyOfPropertyChange(nameof(IsSelectedTab1));
                    NotifyOfPropertyChange(nameof(CanDeleteSelected));
                    ValidateProperties();
                }
            }
        }

        // Selected tab Item Draft
        private bool _isSelectedTab2;
        public bool IsSelectedTab2
        {
            get { return _isSelectedTab2; }
            set
            {
                if (_isSelectedTab2 != value)
                {
                    _isSelectedTab2 = value;
                    NotifyOfPropertyChange(nameof(IsSelectedTab2));
                    NotifyOfPropertyChange(nameof(CanDeleteSelected));
                }
            }
        }

        private AccountingEntryGraphQLModel _selectedAccountingEntry;
        public AccountingEntryGraphQLModel SelectedAccountingEntry
        {
            get { return _selectedAccountingEntry; }
            set
            {
                if (_selectedAccountingEntry != value)
                {
                    _selectedAccountingEntry = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingEntry));
                    NotifyOfPropertyChange(nameof(CanDeleteEntry));
                    NotifyOfPropertyChange(nameof(CanDeleteSelected));
                }
            }
        }

        private DraftAccountingEntryGraphQLModel _selectedDraftAccountingEntry;
        public DraftAccountingEntryGraphQLModel SelectedDraftAccountingEntry
        {
            get { return _selectedDraftAccountingEntry; }
            set
            {
                if (_selectedDraftAccountingEntry != value)
                {
                    _selectedDraftAccountingEntry = value;
                    NotifyOfPropertyChange(nameof(SelectedDraftAccountingEntry));
                    NotifyOfPropertyChange(nameof(CanDeleteDraft));
                    NotifyOfPropertyChange(nameof(CanDeleteSelected));
                }
            }
        }

        #endregion

        #region Metodos

        public async Task ZoomDoc()
        {
            try
            {
                this.IsBusy = true;
                this.Refresh();
                await Task.Run(() => this.ExecuteZoomDoc());
                this.Refresh();
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public bool CanZoomDoc
        {
            get
            {
                return true;
            }
        }

        public async Task ExecuteZoomDoc()
        {
            try
            {
                await this.Context.ActivateDocumentPreviewViewAsync(SelectedAccountingEntry);
            }
            catch (Exception)
            {

                throw;
            }
        }

       

        public bool CanSearchAccountingEntries
        {
            get
            {
                return (_errors.Count == 0);
            }
        }


        public async Task DeleteEntryAsync()
        {
            if (SelectedAccountingEntry is null) return;
            if (ThemedMessageBox.Show("Atención !", "¿Confirma que desea eliminar el comprobante seleccionado?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

            try
            {
                this.IsBusy = true;
                var (fragment, query) = _deleteEntryQuery.Value;
                object variables = new GraphQLVariables()
                    .For(fragment, "id", (int)SelectedAccountingEntry.Id)
                    .Build();

                var result = await _accountingEntryMasterService.DeleteAsync<DeleteResponseType>(query, variables);

                if (!result.Success)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", result.Message, MessageBoxButton.OK, MessageBoxImage.Error));
                    return;
                }

                await this.Context.EventAggregator.PublishOnUIThreadAsync(new AccountingEntryDeleteMessage { DeletedAccountingEntry = result });
                SelectedAccountingEntry = null;
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public bool CanDeleteEntry => SelectedAccountingEntry is not null;

        public async Task DeleteSelectedAsync()
        {
            if (IsSelectedTab1)
                await DeleteEntryAsync();
            else
                await DeleteDraftAsync();
        }

        public bool CanDeleteSelected => IsSelectedTab1 ? CanDeleteEntry : CanDeleteDraft;

        public async Task DeleteDraftAsync()
        {
            if (SelectedDraftAccountingEntry is null) return;
            if (ThemedMessageBox.Show("Atención !", "¿Confirma que desea eliminar el borrador seleccionado?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

            try
            {
                this.IsBusy = true;
                var (fragment, query) = _deleteDraftQuery.Value;
                object variables = new GraphQLVariables()
                    .For(fragment, "id", (int)SelectedDraftAccountingEntry.Id)
                    .Build();

                var result = await _draftAccountingEntryService.DeleteAsync<DeleteResponseType>(query, variables);

                if (!result.Success)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", result.Message, MessageBoxButton.OK, MessageBoxImage.Error));
                    return;
                }

                await this.Context.EventAggregator.PublishOnUIThreadAsync(new DraftAccountingEntryDeleteMessage { DeletedDraftAccountingEntry = result });
                SelectedDraftAccountingEntry = null;
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public bool CanDeleteDraft => SelectedDraftAccountingEntry is not null;

        public AccountingEntriesMasterViewModel(AccountingEntriesViewModel context,
            Helpers.Services.INotificationService notificationService,
            IRepository<AccountingEntryGraphQLModel> accountingEntryMasterService,
            IRepository<DraftAccountingEntryGraphQLModel> accountingEntryDraftMasterService,
            IRepository<AccountingEntityGraphQLModel> accountingEntityService,
             CostCenterCache costCenterCache,
             AccountingBookCache accountingBookCache,
             NotAnnulledAccountingSourceCache notAnnulledAccountingSourceCache,
             Helpers.IDialogService dialogService,
             IGraphQLClient graphQLClient)
        {
            this.Context = context;
            _notificationService = notificationService;
            _accountingEntryMasterService = accountingEntryMasterService;
            _draftAccountingEntryService = accountingEntryDraftMasterService;
            _accountingEntityService = accountingEntityService;
            _costCenterCache = costCenterCache;
            _accountingBookCache = accountingBookCache;
            _notAnnulledAccountingSourceCache = notAnnulledAccountingSourceCache;
            _dialogService = dialogService;
            _graphQLClient = graphQLClient;
            // Validaciones
            this._errors = new Dictionary<string, List<string>>();

            // Mensajes
            this.Context.EventAggregator.SubscribeOnUIThread(this);
            Messenger.Default.Register<ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel>>(
                this,
                SearchWithTwoColumnsGridMessageToken.AccountingEntryFilterAccountingEntity,
                false,
                OnFilterAccountingEntityMessage);
        }

        protected override async Task OnInitializedAsync(CancellationToken cancellationToken)
        {
            await InitializeAsync();
            await base.OnInitializedAsync(cancellationToken);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                // Solo al cerrar el tab del módulo. En cambios internos entre Master/Detail
                // (close=false) la suscripción se conserva para seguir recibiendo mensajes
                // de Create/Update/Delete publicados por el Detail y DocumentPreview.
                this.Context.EventAggregator.Unsubscribe(this);
                Messenger.Default.Unregister(this);
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        public async Task InitializeAsync()
        {
            // Los caches ya fueron cargados centralizadamente por el Conductor en
            // OnInitializedAsync vía CacheBatchLoader. Aquí solo se materializan las
            // colecciones locales que la vista bindea.
            CostCenters = [.. _costCenterCache.Items];
            AccountingBooks = [.. _accountingBookCache.Items];
            AccountingSources = [.. _notAnnulledAccountingSourceCache.Items];
            await LoadAccountingEntryDraftsAsync();
            
        }
        public async Task LoadAccountingEntryDraftsAsync()
        {
            try
            {

                IsBusy = true;

                dynamic variables = new ExpandoObject();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.page = DraftPageIndex;
                variables.pageResponsePagination.pageSize = DraftPageSize;

                variables.pageResponseFilters = new ExpandoObject();

                string query = _draftsPageQuery.Value;
                PageType<DraftAccountingEntryGraphQLModel> result = await _draftAccountingEntryService.GetPageAsync(query, variables);
                this.AccountingEntryDrafts = new ObservableCollection<DraftAccountingEntryGraphQLModel>(result.Entries);
                DraftTotalCount = result.TotalEntries;

            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }

        }
        private static readonly Lazy<string> _draftsPageQuery = new(() =>
        {
            var fields = FieldSpec<PageType<DraftAccountingEntryGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    .Field(e => e.DocumentDate)
                    .Field(e => e.DocumentNumber)
                    .Select(e => e.AccountingBook, ab => ab.Field(c => c.Id).Field(c => c.Name))
                    .Select(e => e.CostCenter, cc => cc.Field(c => c.Id).Field(c => c.Name))
                    .Select(e => e.AccountingSource, src => src.Field(c => c.Id).Field(c => c.Name)))
                .Field(o => o.PageNumber)
                .Field(o => o.PageSize)
                .Field(o => o.TotalPages)
                .Field(o => o.TotalEntries)
                .Build();

            var fragment = new GraphQLQueryFragment("draftAccountingEntriesPage",
                [new("pagination", "Pagination"), new("filters", "DraftAccountingEntryFilters")],
                fields, "PageResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        });
        
        public async Task SearchAccountingEntriesAsync()
        {
            try
            {

                IsBusy = true;

                dynamic variables = new ExpandoObject();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.page = PageIndex;
                variables.pageResponsePagination.pageSize = PageSize;

                variables.pageResponseFilters = new ExpandoObject();
                if (SearchOnAccountingBook)
                {
                    variables.pageResponseFilters.AccountingBookId = SelectedAccountingBookId;
                }
                if (SearchOnCostCenter)
                {
                    variables.pageResponseFilters.CostCenterId = SelectedCostCenterId;
                }
                if (SearchOnAccountingSource)
                {
                    variables.pageResponseFilters.AccountingSourceId = SelectedAccountingSourceId;
                }

                if (SearchOnAccountingEntity && FilterSelectedAccountingEntity is not null)
                {
                    variables.pageResponseFilters.AccountingEntityId = (int)FilterSelectedAccountingEntity.Id;
                }
                if (SearchOnDocumentNumber)
                {
                    variables.pageResponseFilters.DocumentNumber = DocumentNumber.Trim().RemoveExtraSpaces();
                }

                if (SearchOnDate)
                {
                    switch (SelectedDateFilterOption)
                    {
                        case '=':
                            variables.pageResponseFilters.FromDocumentDate = StartDateFilter.ToIsoDate();
                            variables.pageResponseFilters.ToDocumentDate = StartDateFilter.ToIsoDate();
                            break;
                        case '>':
                            variables.pageResponseFilters.FromDocumentDate = StartDateFilter.ToIsoDate();
                            break;
                        case '<':
                            variables.pageResponseFilters.ToDocumentDate = StartDateFilter.ToIsoDate();
                            break;
                        case 'B':
                            variables.pageResponseFilters.FromDocumentDate = StartDateFilter.ToIsoDate();
                            variables.pageResponseFilters.ToDocumentDate = EndDateFilter.ToIsoDate();
                            break;
                    }
                }

                string query = _entriesPageQuery.Value;
                PageType<AccountingEntryGraphQLModel> result = await _accountingEntryMasterService.GetPageAsync(query, variables);
                this.AccountingEntries = new ObservableCollection<AccountingEntryGraphQLModel>(result.Entries);
                TotalCount = result.TotalEntries;
                if(TotalCount == 0) {
                    _notificationService.ShowInfo("No se encontraron registros");
                }

            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }

        }
        private static readonly Lazy<string> _entriesPageQuery = new(() =>
        {
            var fields = FieldSpec<PageType<AccountingEntryGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    .Field(e => e.DocumentDate)
                    .Field(e => e.DocumentNumber)
                    .Field(e => e.InsertedAt)
                    .Field(e => e.Status)
                    .Field(e => e.Annulment)
                    .Select(e => e.AccountingBook, ab => ab.Field(c => c.Id).Field(c => c.Name))
                    .Select(e => e.CostCenter, cc => cc.Field(c => c.Id).Field(c => c.Name))
                    .Select(e => e.AccountingSource, src => src.Field(c => c.Id).Field(c => c.Name))
                    .Select(e => e.CreatedBy, cb => cb.Field(c => c.Id).Field(c => c.FullName)))
                .Field(o => o.PageNumber)
                .Field(o => o.PageSize)
                .Field(o => o.TotalPages)
                .Field(o => o.TotalEntries)
                .Build();

            var fragment = new GraphQLQueryFragment("accountingEntriesPage",
                [new("pagination", "Pagination"), new("filters", "AccountingEntryFilters")],
                fields, "PageResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteEntryQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteAccountingEntry",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteDraftQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteDraftAccountingEntry",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static string GetSearchAccountingEntityQuery()
        {
            var fields = FieldSpec<PageType<AccountingEntityGraphQLModel>>
                .Create()
                .Field(f => f.PageNumber)
                .Field(f => f.TotalEntries)
                .Field(f => f.PageSize)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.IdentificationNumber)
                    .Field(e => e.VerificationDigit)
                    .Field(e => e.SearchName))
                .Build();

            var filterParameter = new GraphQLQueryParameter("filters", "AccountingEntityFilters");
            var paginationParameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("accountingEntitiesPage", [filterParameter, paginationParameter], fields, "PageResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        public async Task OpenFilterAccountingEntitySearchAsync()
        {
            string query = GetSearchAccountingEntityQuery();

            var viewModel = new SearchWithTwoColumnsGridViewModel<AccountingEntityGraphQLModel>(
                query,
                fieldHeader1: "Identificación",
                fieldHeader2: "Nombre / Razón Social",
                fieldData1: "IdentificationNumberWithVerificationDigit",
                fieldData2: "SearchName",
                variables: null,
                SearchWithTwoColumnsGridMessageToken.AccountingEntryFilterAccountingEntity,
                _dialogService);

            await _dialogService.ShowDialogAsync(viewModel, "Búsqueda de terceros");
        }

        private void OnFilterAccountingEntityMessage(ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel> message)
        {
            if (message?.ReturnedData is null) return;
            FilterSelectedAccountingEntity = message.ReturnedData;
        }

        public async Task CreateAccountingEntry()
        {
            await this.Context.ActivateDetailViewForNewAsync(AccountingBooks, CostCenters, AccountingSources);
        }

        public async Task EditDraftEntryAsync(DraftAccountingEntryGraphQLModel draft)
        {
            try
            {
                IsBusy = true;
                await this.Context.ActivateDetailViewForEditAsync(draft);
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public bool CanEditDraftEntry => !IsBusy;

        private ICommand _editDraftEntryCommand;
        public ICommand EditDraftEntryCommand
        {
            get
            {
                if (_editDraftEntryCommand is null) _editDraftEntryCommand = new AsyncCommand<DraftAccountingEntryGraphQLModel>(EditDraftEntryAsync, _ => CanEditDraftEntry);
                return _editDraftEntryCommand;
            }
        }
      
        private ICommand _createAccountingEntryCommand;

        public ICommand CreateAccountingEntryCommand
        {
            get
            {
                if (_createAccountingEntryCommand is null) _createAccountingEntryCommand = new AsyncCommand(CreateAccountingEntry, CanCreateAccountingEntry);
                return _createAccountingEntryCommand;
            }
        }
        private ICommand _deleteCommand;
        public ICommand DeleteCommand
        {
            get
            {
                _deleteCommand ??= new AsyncCommand(DeleteSelectedAsync);
                return _deleteCommand;
            }
        }
        public bool CanCreateAccountingEntry => true;

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperties();
        }

        #endregion

        #region Validaciones

        public bool HasErrors => (_errors.Count > 0);

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null;
            return _errors[propertyName];
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ValidateProperty(string propertyName)
        {
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(SelectedAccountingBookId):
                    if (this.SearchOnAccountingBook && this.SelectedAccountingBookId is null or 0) AddError(propertyName, "Si ha elegido buscar por libro contable, debe seleccionar un libro contable");
                    break;
                case nameof(SelectedAccountingSourceId):
                    if (this.SearchOnAccountingSource && this.SelectedAccountingSourceId is null or 0) AddError(propertyName, "Si ha elegido buscar por fuente contable, debe seleccionar una fuente contable");
                    break;
                case nameof(SelectedCostCenterId):
                    if (this.SearchOnCostCenter && this.SelectedCostCenterId is null or 0) AddError(propertyName, "Si ha elegido buscar por centro de costo, debe seleccionar un centro de costo");
                    break;
                case nameof(DocumentNumber):
                    if (this.SearchOnDocumentNumber && string.IsNullOrEmpty(this.DocumentNumber)) AddError(propertyName, "Si ha elegido buscar por número de documento, debe digitar un número de documento");
                    break;
                case nameof(FilterSelectedAccountingEntity):
                    if (this.SearchOnAccountingEntity && FilterSelectedAccountingEntity is null) AddError(propertyName, "Si ha elegido buscar por tercero, debe seleccionar un tercero");
                    break;
                default:
                    break;
            }
            NotifyOfPropertyChange(nameof(CanSearchAccountingEntries));
        }

        private void ValidateProperties()
        {
            if (SearchOnAccountingBook) ValidateProperty(nameof(SelectedAccountingBookId));
            if (SearchOnCostCenter) ValidateProperty(nameof(SelectedCostCenterId));
            if (SearchOnAccountingSource) ValidateProperty(nameof(SelectedAccountingSourceId));
            if (SearchOnDocumentNumber) ValidateProperty(nameof(DocumentNumber));
            if (SearchOnAccountingEntity) ValidateProperty(nameof(FilterSelectedAccountingEntity));
        }
        #endregion

        #region Paginacion Borradores
        /// <summary>
        /// PageIndex
        /// </summary>
        private int _draftPageIndex = 1; // DefaultPageIndex = 1
        public int DraftPageIndex
        {
            get { return _draftPageIndex; }
            set
            {
                if (_draftPageIndex != value)
                {
                    _draftPageIndex = value;
                    NotifyOfPropertyChange(() => DraftPageIndex);
                }
            }
        }

        /// <summary>
        /// PageSize
        /// </summary>
        private int _draftPageSize = 50; // Default PageSize 50
        public int DraftPageSize
        {
            get { return _draftPageSize; }
            set
            {
                if (_draftPageSize != value)
                {
                    _draftPageSize = value;
                    NotifyOfPropertyChange(() => DraftPageSize);
                }
            }
        }

        /// <summary>
        /// TotalCount
        /// </summary>
        private int _draftTotalCount = 0;
        public int DraftTotalCount
        {
            get { return _draftTotalCount; }
            set
            {
                if (_draftTotalCount != value)
                {
                    _draftTotalCount = value;
                    NotifyOfPropertyChange(() => DraftTotalCount);
                }
            }
        }

        // Tiempo de respuesta
        private string _draftResponseTime;
        public string DraftResponseTime
        {
            get { return _draftResponseTime; }
            set
            {
                if (_draftResponseTime != value)
                {
                    _draftResponseTime = value;
                    NotifyOfPropertyChange(() => DraftResponseTime);
                }
            }
        }


        /// <summary>
        /// PaginationCommand para controlar evento
        /// </summary>
        private ICommand _draftPaginationCommand;
        public ICommand DraftPaginationCommand
        {
            get
            {
                if (_draftPaginationCommand == null) this._draftPaginationCommand = new RelayCommand(CanExecutePaginationDraftChangeIndex, ExecutePaginationDraftChangeIndex);
                return _draftPaginationCommand;
            }
        }

        private void ExecutePaginationDraftChangeIndex(object parameter)
        {
            Task.Run(() => 0);
        }

        private bool CanExecutePaginationDraftChangeIndex(object parameter)
        {
            return true;
        }

        #endregion

        #region Paginacion Comprobantes
        /// <summary>
        /// PageIndex
        /// </summary>
        private int _pageIndex = 1; // DefaultPageIndex = 1
        public int PageIndex
        {
            get { return _pageIndex; }
            set
            {
                if (_pageIndex != value)
                {
                    _pageIndex = value;
                    NotifyOfPropertyChange(() => PageIndex);
                }
            }
        }
        /// <summary>
        /// PageSize
        /// </summary>
        private int _pageSize = 50; // Default PageSize 50
        public int PageSize
        {
            get { return _pageSize; }
            set
            {
                if (_pageSize != value)
                {
                    _pageSize = value;
                    NotifyOfPropertyChange(() => PageSize);
                }
            }
        }
        /// <summary>
        /// TotalCount
        /// </summary>
        private int _totalCount = 0;
        public int TotalCount
        {
            get { return _totalCount; }
            set
            {
                if (_totalCount != value)
                {
                    _totalCount = value;
                    NotifyOfPropertyChange(() => TotalCount);
                }
            }
        }
        // Tiempo de respuesta
        private string _responseTime;
        public string ResponseTime
        {
            get { return _responseTime; }
            set
            {
                if (_responseTime != value)
                {
                    _responseTime = value;
                    NotifyOfPropertyChange(() => ResponseTime);
                }
            }
        }
        /// <summary>
        /// PaginationCommand para controlar evento
        /// </summary>
        private ICommand _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                if (_paginationCommand == null) this._paginationCommand = new RelayCommand(CanExecutePaginationChangeIndex, ExecutePaginationChangeIndex);
                return _draftPaginationCommand;
            }
        }
        private void ExecutePaginationChangeIndex(object parameter)
        {
            Task.Run(() => 0);
        }
        private bool CanExecutePaginationChangeIndex(object parameter)
        {
            return true;
        }

        public Task HandleAsync(AccountingEntryGraphQLModel message, CancellationToken cancellationToken)
        {
            try
            {
                if (this.AccountingEntries is null) return Task.CompletedTask;
                var mapped = message;
                AccountingEntryGraphQLModel entry = this.AccountingEntries.FirstOrDefault(x => x.Id == message.Id);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (entry != null)
                        AccountingEntries.Replace(mapped);
                    else
                        AccountingEntries.Insert(0, mapped);
                });
                _notificationService.ShowSuccess("Comprobante contable guardado correctamente");
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(DraftAccountingEntryGraphQLModel message, CancellationToken cancellationToken)
        {
            try
            {
                // Actualiza listado de borradores
                DraftAccountingEntryGraphQLModel draft = this.AccountingEntryDrafts.Where(x => x.Id == message.Id).FirstOrDefault();
                if (draft is null)
                {
                    this.AccountingEntryDrafts.Add(message);
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() => this.AccountingEntryDrafts.Replace(message));
                }

                // Nota (refactor schema): el schema actual ya no tiene el back-link
                // AccountingEntry.draftMasterId. La relación ahora es forward: AccountingEntryDraft.accountingEntry.
                // Si se necesita el indicador visual "este comprobante publicado tiene un borrador asociado"
                // hay que implementarlo con una query distinta o cambiar el modelo visual.
                // Por ahora se omite el cross-link; la lista de comprobantes publicados no cambia al
                // crearse un borrador nuevo.
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(DraftAccountingEntryFinalizeMessage message, CancellationToken cancellationToken)
        {
            try
            {
                DraftAccountingEntryGraphQLModel draft = this.AccountingEntryDrafts.FirstOrDefault(x => x.Id == message.DraftId);
                if (draft != null) this.AccountingEntryDrafts.Remove(draft);
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(DraftAccountingEntryDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                DraftAccountingEntryGraphQLModel draft = this.AccountingEntryDrafts.FirstOrDefault(x => x.Id == message.DeletedDraftAccountingEntry.DeletedId);
                if (draft != null) this.AccountingEntryDrafts.Remove(draft);
                _notificationService.ShowSuccess(message.DeletedDraftAccountingEntry.Message);
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingEntryDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                AccountingEntryGraphQLModel entry = this.AccountingEntries.FirstOrDefault(x => x.Id == message.DeletedAccountingEntry.DeletedId);
                if (entry != null) this.AccountingEntries.Remove(entry);
                _notificationService.ShowSuccess(message.DeletedAccountingEntry.Message);
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingEntryCancellationMessage message, CancellationToken cancellationToken)
        {
            try
            {
                AccountingEntryGraphQLModel entry = this.AccountingEntries.Where(x => x.Id == message.CancelledAccountingEntry.Id).FirstOrDefault();
                if (entry != null) Application.Current.Dispatcher.Invoke(() => this.AccountingEntries.Replace(message.CancelledAccountingEntry));
                _notificationService.ShowSuccess("Comprobante contable anulado exitosamente");
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

       

      

       

       
       
       

        public Task HandleAsync(DraftAccountingEntryUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                DraftAccountingEntryGraphQLModel updatedAccountigEntryDraftMaster = this.AccountingEntryDrafts.Where(x => x.Id == message.UpdatedDraftAccountingEntry.Id).FirstOrDefault();
                if (updatedAccountigEntryDraftMaster != null) Application.Current.Dispatcher.Invoke(() => this.AccountingEntryDrafts.Replace(message.UpdatedDraftAccountingEntry));
                _notificationService.ShowSuccess("Actualización exitosa");
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

        #endregion
    }
}
