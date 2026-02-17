using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using Extensions.Books;
using Extensions.Global;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
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
        IHandle<AccountingEntryDraftGraphQLModel>,
        IHandle<AccountingEntryDraftMasterDeleteMessage>,
        IHandle<AccountingEntryMasterDeleteMessage>,
        IHandle<AccountingEntryMasterCancellationMessage>,
      
      
        IHandle<AccountingEntryDraftMasterUpdateMessage>
    {
        #region Popiedades
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AccountingEntryGraphQLModel> _accountingEntryMasterService;
        private readonly IRepository<AccountingEntryDraftGraphQLModel> _accountingEntryDraftMasterService;
        private readonly IRepository<AccountingEntityGraphQLModel> _accountingEntityService;

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
        private ObservableCollection<AccountingEntryDraftMasterDTO> _accountingEntriesDraftMaster;
        public ObservableCollection<AccountingEntryDraftMasterDTO> AccountingEntriesDraftMaster
        {
            get { return _accountingEntriesDraftMaster; }
            set
            {
                if (_accountingEntriesDraftMaster != value)
                {
                    _accountingEntriesDraftMaster = value;
                    NotifyOfPropertyChange(nameof(AccountingEntriesDraftMaster));
                }
            }
        }

        // Listado de comprobantes
        private ObservableCollection<AccountingEntryMasterDTO> _accountingEntriesMaster;
        public ObservableCollection<AccountingEntryMasterDTO> AccountingEntriesMaster
        {
            get { return _accountingEntriesMaster; }
            set
            {
                if (_accountingEntriesMaster != value)
                {
                    _accountingEntriesMaster = value;
                    NotifyOfPropertyChange(nameof(AccountingEntriesMaster));
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

        // Filtro de busqueda de tercero
        private string _filterSearchAccountingEntity = "";
        public string FilterSearchAccountingEntity
        {
            get { return _filterSearchAccountingEntity; }
            set
            {
                if (_filterSearchAccountingEntity != value)
                {
                    _filterSearchAccountingEntity = value;
                    NotifyOfPropertyChange(nameof(FilterSearchAccountingEntity));
                    NotifyOfPropertyChange(nameof(CanSearchForAccountingEntityMatch));
                    NotifyOfPropertyChange(nameof(FilterSearchButtonInfo));
                    ValidateProperty(nameof(FilterSearchAccountingEntity));
                }
            }
        }

        // Estado de filtro de tercero
        private bool _isFilterSearchAccountinEntityOnEditMode = true;
        public bool IsFilterSearchAccountinEntityOnEditMode
        {
            get { return _isFilterSearchAccountinEntityOnEditMode; }
            set
            {
                if (_isFilterSearchAccountinEntityOnEditMode != value)
                {
                    _isFilterSearchAccountinEntityOnEditMode = value;
                    NotifyOfPropertyChange(nameof(IsFilterSearchAccountinEntityOnEditMode));
                    NotifyOfPropertyChange(nameof(FilterSearchButtonInfo));
                    NotifyOfPropertyChange(nameof(CanSearchForAccountingEntityMatch));
                }
            }
        }

        // Resultados de busqueda de terceros
        private ObservableCollection<AccountingEntityGraphQLModel> _accountingEntitiesSearchResults = new ObservableCollection<AccountingEntityGraphQLModel>();
        public ObservableCollection<AccountingEntityGraphQLModel> AccountingEntitiesSearchResults
        {
            get { return _accountingEntitiesSearchResults; }
            set
            {
                if (_accountingEntitiesSearchResults != value)
                {
                    _accountingEntitiesSearchResults = value;
                    NotifyOfPropertyChange(nameof(AccountingEntitiesSearchResults));
                }
            }
        }

        // Id del tercero seleccionado
        private int _selectedAccountingEntityId = 0;
        public int SelectedAccountingEntityId
        {
            get { return _selectedAccountingEntityId; }
            set
            {
                if (_selectedAccountingEntityId != value)
                {
                    _selectedAccountingEntityId = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingEntityId));
                    NotifyOfPropertyChange(nameof(CanSearchForAccountingEntityMatch));
                    ValidateProperty(nameof(SelectedAccountingEntityId));
                }
            }
        }

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

        // Texto dinamico delboton de busqueda
        public string FilterSearchButtonInfo
        {
            get
            {
                if (this.IsFilterSearchAccountinEntityOnEditMode)
                {
                    return "BUSCAR TERCERO";
                }
                else
                {
                    return "CAMBIAR FITRO";
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
                    NotifyOfPropertyChange(nameof(SearchOnDocumentNumber));
                    ValidateProperty(nameof(DocumentNumber));
                    if (_searchOnDocumentNumber) this.SetFocus(nameof(DocumentNumber));
                }
            }
        }

        // Busqueda en tercero
        private bool _searchOnAccountingEntity = false;
        public bool SearchOnAccountingEntity
        {
            get { return _searchOnAccountingEntity; }
            set
            {
                if (_searchOnAccountingEntity != value)
                {
                    _searchOnAccountingEntity = value;
                    NotifyOfPropertyChange(nameof(SearchOnAccountingEntity));
                    if (IsFilterSearchAccountinEntityOnEditMode)
                    {
                        ValidateProperty(nameof(FilterSearchAccountingEntity));
                    }
                    else
                    {
                        ValidateProperty(nameof(SelectedAccountingEntityId));
                    }
                    if (_searchOnAccountingEntity) this.SetFocus(nameof(this.FilterSearchAccountingEntity));
                }
            }
        }

        #endregion

        #region Selected Items or Ids

        // Accounting Book
        private int _selectedAccountingBookId;
        public int SelectedAccountingBookId
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
        private int _selectedCostCenterId;
        public int SelectedCostCenterId
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
        private int _selectedAccountingSourceId;
        public int SelectedAccountingSourceId
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
        private bool _IsSelectedTab1;
        public bool IsSelectedTab1
        {
            get { return _IsSelectedTab1; }
            set
            {
                if (_IsSelectedTab1 != value)
                {
                    _IsSelectedTab1 = value;
                    NotifyOfPropertyChange(nameof(IsSelectedTab1));
                    NotifyOfPropertyChange(nameof(CanDeleteEntry));
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
                    NotifyOfPropertyChange(nameof(CanDeleteEntry));
                }
            }
        }

        // Selected Accounting Entry 
        private AccountingEntryMasterDTO _selectedAccountingEntry;
        public AccountingEntryMasterDTO SelectedAccountingEntry
        {
            get { return _selectedAccountingEntry; }
            set
            {
                if (_selectedAccountingEntry != value)
                {
                    _selectedAccountingEntry = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingEntry));
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
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
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


        public async Task DeleteEntry()
        {
            if (ThemedMessageBox.Show("Atención !", "¿Confirma que desea eliminar los registros seleccionados?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

            this.IsBusy = true;
            this.Refresh();

            var deletedRecords = await Task.Run(() => this.ExecuteDeleteEntryAsync());

            if (deletedRecords > 0)
            {
                if (IsSelectedTab1)
                {
                    var recordsToDelete = (from e in this.AccountingEntriesMaster
                                           where e.IsChecked
                                           select e).ToList();

                    foreach (var item in recordsToDelete)
                    {
                        this.AccountingEntriesMaster.Remove(item);
                    }
                }
                else
                {
                    var recordsToDelete = (from e in AccountingEntriesDraftMaster
                                           where e.IsChecked
                                           select e).ToList();

                    foreach (var item in recordsToDelete)
                    {
                        AccountingEntriesDraftMaster.Remove(item);
                    }
                }
            }

            this.IsBusy = false;
            NotifyOfPropertyChange(nameof(CanDeleteEntry));
            _notificationService.ShowSuccess(IsSelectedTab1 ? "Comprobante(s) contable(s) eliminado(s) correctamente" : "Borrador(es) contable(s) eliminado(s) correctamente");
        }

        public async Task<int> ExecuteDeleteEntryAsync()
        {
            BigInteger[] ids;
            string query;
            int count = 0;

            if (IsSelectedTab1)
            {
                ids = [.. (from e in this.AccountingEntriesMaster
                       where e.IsChecked
                       select e.Id)];

                query = @"
                mutation($connectionId: String!, $masterIds:[ID!]!) {
                  bulkDeleteAccountingEntryMaster(connectionId: $connectionId, masterIds:$masterIds) {
                    count
                  }
                }";

            }
            else
            {
                ids = [.. (from e in this.AccountingEntriesDraftMaster
                       where e.IsChecked
                       select e.Id)];

                query = @"
                mutation($draftMasterIds:[ID!]!) {
                  bulkDeleteAccountingEntryDraftMaster(draftMasterIds:$draftMasterIds) {
                    count
                  }
                }";
            }

            object variables = new
            {
                MasterIds = ids,
                DraftMasterIds = ids
            };
            //TODO
            if (IsSelectedTab1)
            {
                var result = await this._accountingEntryMasterService.GetDataContextAsync<AccountingEntryCountDelete>(query, variables);
                count = result.Count;
            }
            else
            {
                var result = await this._accountingEntryDraftMasterService.GetDataContextAsync<AccountingEntryCountDelete>(query, variables);
                count = result.Count;
            }

            return count;
        }

        public bool CanDeleteEntry
        {
            get
            {
                int count = 0;
                if (this.IsSelectedTab1)
                {
                    if (this.AccountingEntriesMaster is null) return false;
                    var result = (from e in this.AccountingEntriesMaster
                                  where e.IsChecked
                                  select e).ToArray();
                    count = result is null ? 0 : result.Length;
                }
                else if (this.IsSelectedTab2)
                {
                    if (this.AccountingEntriesDraftMaster is null) return false;
                    var result = (from e in this.AccountingEntriesDraftMaster
                                  where e.IsChecked
                                  select e).ToArray();
                    count = result is null ? 0 : result.Length;
                }
                return (count > 0);
            }
        }

        public void OnChecked()
        {
            NotifyOfPropertyChange(nameof(CanDeleteEntry));
        }

        public void OnUnchecked()
        {
            NotifyOfPropertyChange(nameof(CanDeleteEntry));
        }

        public AccountingEntriesMasterViewModel(AccountingEntriesViewModel context,
            Helpers.Services.INotificationService notificationService,
            IRepository<AccountingEntryGraphQLModel> accountingEntryMasterService,
            IRepository<AccountingEntryDraftGraphQLModel> accountingEntryDraftMasterService,
            IRepository<AccountingEntityGraphQLModel> accountingEntityService,
             CostCenterCache costCenterCache,
             AccountingBookCache accountingBookCache,
             NotAnnulledAccountingSourceCache notAnnulledAccountingSourceCache)
        {
            this.Context = context;
            _notificationService = notificationService;
            _accountingEntryMasterService = accountingEntryMasterService;
            _accountingEntryDraftMasterService = accountingEntryDraftMasterService;
            _accountingEntityService = accountingEntityService;
            _costCenterCache = costCenterCache;
            _accountingBookCache = accountingBookCache;
            _notAnnulledAccountingSourceCache = notAnnulledAccountingSourceCache;
            // Validaciones
            this._errors = new Dictionary<string, List<string>>();

            // Mensajes
            this.Context.EventAggregator.SubscribeOnUIThread(this);

            var joinable = new JoinableTaskFactory(new JoinableTaskContext());
            joinable.Run(async () => await InitializeAsync());
            
        }

        public async Task InitializeAsync()
        {
            await Task.WhenAll(
               _costCenterCache.EnsureLoadedAsync(),
               _accountingBookCache.EnsureLoadedAsync(),
                _notAnnulledAccountingSourceCache.EnsureLoadedAsync()
               );
            CostCenters =[.. _costCenterCache.Items];
            this.CostCenters.Insert(0, new CostCenterGraphQLModel() { Id = 0, Name = "SELECCIONE CENTRO DE COSTO" });
            this.SelectedCostCenterId = this.CostCenters.FirstOrDefault().Id;
            this.AccountingBooks = [.._accountingBookCache.Items];
            this.SelectedAccountingBookId = this.AccountingBooks.Count > 0? this.AccountingBooks.FirstOrDefault().Id : 0;
            this.AccountingSources = [.. _notAnnulledAccountingSourceCache.Items];
            this.AccountingSources.Insert(0, new AccountingSourceGraphQLModel() { Id = 0, Name = "SELECCIONE FUENTE CONTABLE" });
            this.SelectedAccountingSourceId = this.AccountingSources.FirstOrDefault().Id;
            await LoadAccountingEntryDraftsAsync();
            
        }
        public async Task LoadAccountingEntryDraftsAsync()
        {
            try
            {

                IsBusy = true;

                dynamic variables = new ExpandoObject();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.page = 1;
                variables.pageResponsePagination.pageSize = 10;

                variables.pageResponseFilters = new ExpandoObject();
                
                string query = GetLoadAccountingEntryDraftsQuery();
                PageType<AccountingEntryDraftGraphQLModel> result = await _accountingEntryDraftMasterService.GetPageAsync(query, variables);
                this.AccountingEntriesDraftMaster = new ObservableCollection<AccountingEntryDraftMasterDTO>(this.Context.Mapper.Map<List<AccountingEntryDraftMasterDTO>>(result.Entries));
                PageIndex = result.PageNumber;
                PageSize = result.PageSize;
                TotalCount = result.TotalEntries;

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
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
        public string GetLoadAccountingEntryDraftsQuery()
        {
           
                var accountingEntryDraftFields = FieldSpec<PageType<AccountingEntryDraftGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    .Field(e => e.DocumentDate)
                    .Field(e => e.DocumentNumber)
                    .Select(e => e.AccountingBook, cat => cat
                    .Field(c => c.Id)
                    .Field(c => c.Name)
                    
                    )
                    .Select(e => e.CostCenter, cat => cat
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                        )
                    .Select(e => e.AccountingSource, cat => cat
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                        )
                    

                )
                .Field(o => o.PageNumber)
                .Field(o => o.PageSize)
                .Field(o => o.TotalPages)
                .Field(o => o.TotalEntries)
                .Build();


            var accountingEntryDraftPagParameters = new GraphQLQueryParameter("pagination", "Pagination");
            var accountingEntryDraftfilterParameters = new GraphQLQueryParameter("filters", "AccountingEntryDraftFilters");

            var accountingEntryDraftFragment = new GraphQLQueryFragment("accountingEntryDraftsPage", [accountingEntryDraftPagParameters, accountingEntryDraftfilterParameters], accountingEntryDraftFields,  "PageResponse");

            var builder =  new GraphQLQueryBuilder([accountingEntryDraftFragment]);
            return builder.GetQuery();
        }
        
        public async Task SearchAccountingEntriesAsync()
        {
            try
            {

                IsBusy = true;

                dynamic variables = new ExpandoObject();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.page = 1;
                variables.pageResponsePagination.pageSize = 10;

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

                if (SearchOnAccountingEntity)
                {
                    variables.pageResponseFilters.AccountingEntityId = SelectedAccountingEntityId;
                }
                if (SearchOnDocumentNumber)
                {
                    variables.pageResponseFilters.DocumentNumber = DocumentNumber.Trim().RemoveExtraSpaces();
                }

                if (SearchOnDate && !IsDateRange)
                {
                    variables.pageResponseFilters.DocumentDate = DateTimeHelper.DateTimeKindUTC(StartDateFilter);
                }

                if (SearchOnDate && IsDateRange)
                {
                    variables.pageResponseFilters.DocumentDate = new List<DateTime> { DateTimeHelper.DateTimeKindUTC(StartDateFilter), DateTimeHelper.DateTimeKindUTC(EndDateFilter) };
                }

                string query = GetSearchAccountingEntriesQuery();
                PageType<AccountingEntryGraphQLModel> result = await _accountingEntryMasterService.GetPageAsync(query, variables);
                this.AccountingEntriesMaster = new ObservableCollection<AccountingEntryMasterDTO>(this.Context.Mapper.Map<List<AccountingEntryMasterDTO>>(result.Entries));
                PageIndex = result.PageNumber;
                PageSize = result.PageSize;
                TotalCount = result.TotalEntries;
                if(TotalCount == 0) {
                    _notificationService.ShowInfo("No se encontraron registros");
                }

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
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
        public string GetSearchAccountingEntriesQuery()
        {
             
      
            var accountingEntryFields = FieldSpec<PageType<AccountingEntryGraphQLModel>>
            .Create()
            .SelectList(it => it.Entries, entries => entries
                .Field(e => e.Id)
                .Field(e => e.Description)
                .Field(e => e.DocumentDate)
                .Field(e => e.DocumentNumber)
                .Field(e => e.InsertedAt)
                .Field(e => e.State)
                .Field(e => e.Annulment)
                .Select(e => e.AccountingBook, cat => cat
                .Field(c => c.Id)
                .Field(c => c.Name)

                )
                .Select(e => e.CostCenter, cat => cat
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                    )
                .Select(e => e.AccountingSource, cat => cat
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                    )
                .Select(e => e.CreatedBy, cat => cat
                        .Field(c => c.Id)
                        .Field(c => c.FullName)
                    )


            )
            .Field(o => o.PageNumber)
            .Field(o => o.PageSize)
            .Field(o => o.TotalPages)
            .Field(o => o.TotalEntries)
            .Build();
  

            var accountingEntriesPagParameters = new GraphQLQueryParameter("pagination", "Pagination");
            var accountingEntriesfilterParameters = new GraphQLQueryParameter("filters", "AccountingEntryFilters");

            var accountingEntryFragment = new GraphQLQueryFragment("accountingEntriesPage", [accountingEntriesPagParameters, accountingEntriesfilterParameters], accountingEntryFields, "PageResponse");

            var builder = new GraphQLQueryBuilder([accountingEntryFragment]);
            return builder.GetQuery();
        }

        public bool CanSearchForAccountingEntityMatch
        {
            get
            {
                if (this.IsFilterSearchAccountinEntityOnEditMode)
                {
                    return (this.FilterSearchAccountingEntity.Trim().Length >= 3) && !this.IsBusy;
                }
                else
                {
                    return true;
                }
            }
        }

        public async Task SearchForAccountingEntityMatch()
        {
            try
            {
                this.IsBusy = true;
                NotifyOfPropertyChange(nameof(CanSearchForAccountingEntityMatch));
                this.Refresh();
                await this.ExecuteSearchForAccountingEntityMatch();
                this.Refresh();

                NotifyOfPropertyChange(nameof(FilterSearchButtonInfo));
                NotifyOfPropertyChange(nameof(CanSearchForAccountingEntityMatch));

                if (this.IsFilterSearchAccountinEntityOnEditMode)
                {
                    App.Current.Dispatcher.Invoke(() => this.SetFocus(() => FilterSearchAccountingEntity));
                }
                else
                {
                    App.Current.Dispatcher.Invoke(() => this.SetFocus(() => SelectedAccountingEntityId));
                }
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ExecuteSearchForAccountingEntityMatch()
        {
            try
            {
                if (this.IsFilterSearchAccountinEntityOnEditMode)
                {
                    string query = @"
                    query ($filter: AccountingEntityFilterInput) {
                      ListResponse: accountingEntities(filter: $filter) {
                        id
                        searchName
                        identificationNumber
                        verificationDigit
                      }
                    }";

                    dynamic variables = new ExpandoObject();
                    variables.filter = new ExpandoObject();
                    variables.filter.SearchName = new ExpandoObject();
                    variables.filter.SearchName.@operator = "like";
                    // Reemplazo los espacios por % para que la busqueda sea mas flexible
                    variables.filter.SearchName.value = this.FilterSearchAccountingEntity.Replace(" ", "%").Trim().RemoveExtraSpaces();
                    var accountingEntities = await this._accountingEntityService.GetListAsync(query, variables);
                    this.AccountingEntitiesSearchResults = new ObservableCollection<AccountingEntityGraphQLModel>(accountingEntities);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        AccountingEntitiesSearchResults.Insert(0, new AccountingEntityGraphQLModel() { Id = 0, SearchName = "SELECCIONE UN TERCERO" });
                        if (AccountingEntitiesSearchResults.ToList().Count == 2) AccountingEntitiesSearchResults = AccountingEntitiesSearchResults.Where(x => x.Id != 0).ToObservableCollection();
                    });

                }

                this.IsFilterSearchAccountinEntityOnEditMode = !this.IsFilterSearchAccountinEntityOnEditMode;
                this.SelectedAccountingEntityId = -1; // Necesario para que siempre se ejecute el property change
                this.SelectedAccountingEntityId = this.AccountingEntitiesSearchResults.FirstOrDefault().Id;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task CreateAccountingEntry()
        {
            await this.Context.ActivateDetailViewForNewAsync(AccountingBooks, CostCenters, AccountingSources);
        }

        public async void EditDraftEntry(object p)
        {
            try
            {
                IsBusy = true;
                await ExecuteEditDraftEntry(p);
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
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

        public async Task ExecuteEditDraftEntry(object p)
        {
            try
            {
                await this.Context.ActivateDetailViewForEditAsync(p as AccountingEntryDraftGraphQLModel);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool CanEditDraftEntry(object p)
        {
            return !IsBusy;
        }

        private ICommand _editDraftEntryCommand;
        public ICommand EditDraftEntryCommand
        {
            get
            {
                if (_editDraftEntryCommand == null) this._editDraftEntryCommand = new RelayCommand(CanEditDraftEntry, EditDraftEntry);
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
        private ICommand _deleteEntryCommand;

        public ICommand DeleteEntryCommand
        {
            get
            {
                if (_deleteEntryCommand is null) _deleteEntryCommand = new AsyncCommand(DeleteEntry, CanDeleteEntry);
                return _deleteEntryCommand;
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
                    if (this.SearchOnAccountingBook && this.SelectedAccountingBookId == 0) AddError(propertyName, "Si ha elegido buscar por libro contable, debe seleccionar un libro contable");
                    break;
                case nameof(SelectedAccountingSourceId):
                    if (this.SearchOnAccountingSource && this.SelectedAccountingSourceId == 0) AddError(propertyName, "Si ha elegido buscar por fuente contable, debe seleccionar una fuente contable");
                    break;
                case nameof(SelectedCostCenterId):
                    if (this.SearchOnCostCenter && this.SelectedCostCenterId == 0) AddError(propertyName, "Si ha elegido buscar por centro de costo, debe seleccionar un centro de costo");
                    break;
                case nameof(DocumentNumber):
                    if (this.SearchOnDocumentNumber && string.IsNullOrEmpty(this.DocumentNumber)) AddError(propertyName, "Si ha elegido buscar por número de documento, debe digitar un número de documento");
                    break;
                case nameof(SelectedAccountingEntityId):
                    if (this.SearchOnAccountingEntity && !IsFilterSearchAccountinEntityOnEditMode && SelectedAccountingEntityId == 0) AddError(propertyName, "Si ha elegido buscar por tercero, debe seleccionar un tercero");
                    break;
                case (nameof(FilterSearchAccountingEntity)):
                    if (this.SearchOnAccountingEntity && IsFilterSearchAccountinEntityOnEditMode && (string.IsNullOrEmpty(FilterSearchAccountingEntity) || FilterSearchAccountingEntity.Length < 3)) AddError(propertyName, "Digite por lo menos 3 caracteres para poder buscar");
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
            if (SearchOnAccountingEntity) ValidateProperty(nameof(SelectedAccountingEntityId));
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
                if (this.AccountingEntriesMaster is null) return Task.CompletedTask;
                AccountingEntryMasterDTO entry = this.AccountingEntriesMaster.Where(x => x.Id == message.Id).FirstOrDefault();
                if (entry != null) Application.Current.Dispatcher.Invoke(() => AccountingEntriesMaster.Replace(this.Context.Mapper.Map<AccountingEntryMasterDTO>(message)));
                this.AccountingEntriesMaster = new ObservableCollection<AccountingEntryMasterDTO>(this.AccountingEntriesMaster);
                _notificationService.ShowSuccess("Comprobante contable guardado correctamente");
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingEntryDraftGraphQLModel message, CancellationToken cancellationToken)
        {
            try
            {
                // Actualiza listado de borradores
                AccountingEntryDraftMasterDTO draft = this.AccountingEntriesDraftMaster.Where(x => x.Id == message.Id).FirstOrDefault();
                if (draft is null)
                {
                    this.AccountingEntriesDraftMaster.Add(this.Context.Mapper.Map<AccountingEntryDraftMasterDTO>(message));
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() => this.AccountingEntriesDraftMaster.Replace(this.Context.Mapper.Map<AccountingEntryDraftMasterDTO>(message)));
                }

                // Actualiza listado de comprobantes
                if (this.AccountingEntriesMaster is null) return Task.CompletedTask;
                AccountingEntryMasterDTO entry = this.AccountingEntriesMaster.Where(x => x.Id == message.MasterId).FirstOrDefault();
                if (entry != null && entry.DraftMasterId != message.Id) entry.DraftMasterId = message.Id;
                this.AccountingEntriesMaster = new ObservableCollection<AccountingEntryMasterDTO>(this.AccountingEntriesMaster);
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingEntryDraftMasterDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                AccountingEntryDraftMasterDTO draft = this.Context.AccountingEntriesMasterViewModel.AccountingEntriesDraftMaster.Where(x => x.Id == message.Id).FirstOrDefault();
                if (draft != null) this.AccountingEntriesDraftMaster.Remove(draft);
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingEntryMasterDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                AccountingEntryMasterDTO entry = this.AccountingEntriesMaster.Where(x => x.Id == message.Id).FirstOrDefault();
                if (entry != null) this.AccountingEntriesMaster.Remove(entry);
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingEntryMasterCancellationMessage message, CancellationToken cancellationToken)
        {
            try
            {
                AccountingEntryMasterDTO entry = this.AccountingEntriesMaster.Where(x => x.Id == message.CancelledAccountingEntry.Id).FirstOrDefault();
                if (entry != null) Application.Current.Dispatcher.Invoke(() => this.AccountingEntriesMaster.Replace(this.Context.Mapper.Map<AccountingEntryMasterDTO>(message.CancelledAccountingEntry)));
                _notificationService.ShowSuccess("Comprobante contable anulado exitosamente");
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

       

      

       

       
       
       

        public Task HandleAsync(AccountingEntryDraftMasterUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                AccountingEntryDraftMasterDTO updatedAccountigEntryDraftMaster = this.AccountingEntriesDraftMaster.Where(x => x.Id == message.UpdatedAccountingEntryDraftMaster.Id).FirstOrDefault();
                if (updatedAccountigEntryDraftMaster != null) Application.Current.Dispatcher.Invoke(() => this.AccountingEntriesDraftMaster.Replace(message.UpdatedAccountingEntryDraftMaster));
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
