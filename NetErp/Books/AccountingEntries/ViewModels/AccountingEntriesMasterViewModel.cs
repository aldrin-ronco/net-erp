using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using NetErp.Helpers;
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
using Extensions.Books;
using Extensions.Global;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.Threading;
using DevExpress.Mvvm;

namespace NetErp.Books.AccountingEntries.ViewModels
{
    public class AccountingEntriesMasterViewModel : Screen,
        INotifyDataErrorInfo,
        IHandle<AccountingEntryMasterGraphQLModel>,
        IHandle<AccountingEntryDraftMasterGraphQLModel>,
        IHandle<AccountingEntryDraftMasterDeleteMessage>,
        IHandle<AccountingEntryMasterDeleteMessage>,
        IHandle<AccountingEntryMasterCancellationMessage>,
        IHandle<CostCenterCreateMessage>,
        IHandle<CostCenterUpdateMessage>,
        IHandle<CostCenterDeleteMessage>,
        IHandle<AccountingSourceCreateMessage>,
        IHandle<AccountingSourceUpdateMessage>,
        IHandle<AccountingSourceDeleteMessage>,
        IHandle<AccountingEntryDraftMasterUpdateMessage>
    {
        #region Popiedades
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AccountingEntryMasterGraphQLModel> _accountingEntryMasterService;
        private readonly IRepository<AccountingEntryDraftMasterGraphQLModel> _accountingEntryDraftMasterService;
        private readonly IRepository<AccountingEntityGraphQLModel> _accountingEntityService;


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

        public async Task SearchAccountingEntries()
        {
            try
            {
                this.IsBusy = true;
                this.Refresh();

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                var accountingEntriesPage = await this.ExecuteSearchAccountingEntriesAsync();

                stopwatch.Stop();

                this.ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

                if (accountingEntriesPage.Count > 0)
                {
                    this.TotalCount = accountingEntriesPage.Count;
                    this.AccountingEntriesMaster = new ObservableCollection<AccountingEntryMasterDTO>(this.Context.Mapper.Map<List<AccountingEntryMasterDTO>>(accountingEntriesPage.Rows));
                }
                else
                {
                    _notificationService.ShowInfo("No se encontraron registros");
                }
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"\r\n{graphQLError.Errors[0].Message}\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<PageResult<AccountingEntryMasterGraphQLModel>> ExecuteSearchAccountingEntriesAsync()
        {
            try
            {
                string query = @"
                query($filter:AccountingEntryMasterFilterInput){
                  PageResponse: accountingEntryMasterPage(filter:$filter){
                    count
                    rows {
                      id
                      accountingBook {
                        id
                        name
                      }
                      costCenter {
                        id
                        name
                      }
                      accountingSource {
                        id
                        name
                      }      
                      state
                      annulment  
                      draftMasterId
                      documentDate
                      createdAt
                      description      
                      documentNumber
                      createdBy
                    }
                  }
                }";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();


                if (SearchOnAccountingBook) 
                {
                    variables.filter.AccountingBookId = new ExpandoObject();
                    variables.filter.AccountingBookId.@operator = "=";
                    variables.filter.AccountingBookId.value = SelectedAccountingBookId;
                }
                if(SearchOnCostCenter)
                {
                    variables.filter.CostCenterId = new ExpandoObject();
                    variables.filter.CostCenterId.@operator = "=";
                    variables.filter.CostCenterId.value = SelectedCostCenterId;
                }
                if (SearchOnAccountingSource)
                {
                    variables.filter.AccountingSourceId = new ExpandoObject();
                    variables.filter.AccountingSourceId.@operator = "=";
                    variables.filter.AccountingSourceId.value = SelectedAccountingSourceId;
                }

                if(SearchOnAccountingEntity)
                {
                    variables.filter.AccountingEntityId = new ExpandoObject();
                    variables.filter.AccountingEntityId.@operator = "=";
                    variables.filter.AccountingEntityId.value = SelectedAccountingEntityId;
                }
                if (SearchOnDocumentNumber)
                {
                    variables.filter.DocumentNumber = new ExpandoObject();
                    variables.filter.DocumentNumber.@operator = "like";
                    variables.filter.DocumentNumber.value = DocumentNumber.Trim().RemoveExtraSpaces();
                }

                if (SearchOnDate && !IsDateRange) 
                {
                    variables.filter.DocumentDate = new ExpandoObject();
                    variables.filter.DocumentDate.@operator = SelectedDateFilterOption.ToString();
                    variables.filter.DocumentDate.value = DateTimeHelper.DateTimeKindUTC(StartDateFilter);
                }

                if(SearchOnDate && IsDateRange)
                {
                    variables.filter.DocumentDate = new ExpandoObject();
                    variables.filter.DocumentDate.@operator = "between";
                    variables.filter.DocumentDate.value = new List<DateTime>{ DateTimeHelper.DateTimeKindUTC(StartDateFilter), DateTimeHelper.DateTimeKindUTC(EndDateFilter) };
                }

                // Paginacion
                variables.filter.Pagination = new ExpandoObject();
                variables.filter.Pagination.Page = PageIndex;
                variables.filter.Pagination.PageSize = PageSize;
                var result = await this._accountingEntryMasterService.GetPageAsync(query, variables);
                return result;
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
            IRepository<AccountingEntryMasterGraphQLModel> accountingEntryMasterService,
            IRepository<AccountingEntryDraftMasterGraphQLModel> accountingEntryDraftMasterService,
            IRepository<AccountingEntityGraphQLModel> accountingEntityService)
        {
            this.Context = context;
            _notificationService = notificationService;
            _accountingEntryMasterService = accountingEntryMasterService;
            _accountingEntryDraftMasterService = accountingEntryDraftMasterService;
            _accountingEntityService = accountingEntityService;
          
            // Validaciones
            this._errors = new Dictionary<string, List<string>>();

            // Mensajes
            this.Context.EventAggregator.SubscribeOnUIThread(this);

            var joinable = new JoinableTaskFactory(new JoinableTaskContext());
            joinable.Run(async () => await Initialize());
        }

        public async Task Initialize()
        {
            try
            {

                string query = @"
                query ($accountingSourceFilter: AccountingSourceFilterInput) {
                accountingBooks{
                    id
                    name
                },
                costCenters{
                    id
                    name
                    tradeName
                },
                accountingSources(filter: $accountingSourceFilter){
                    id
                    name
                },
                accountingEntryDraftMasterPage{
                    count
                    rows {
                    id
                    masterId  
                    accountingBook {
                        id
                        name
                    }
                    costCenter {
                        id
                        name
                    }
                    accountingSource {
                        id
                        name
                    }
                    documentNumber
                    documentDate
                    createdAt
                    description
                    createdBy
                    }
                }
                }";

                if (this.AccountingBooks == null)
                {
                    dynamic variables = new ExpandoObject();
                    variables.accountingSourceFilter = new ExpandoObject();
                    variables.accountingSourceFilter.annulment = new ExpandoObject();
                    variables.accountingSourceFilter.annulment.@operator = "=";
                    variables.accountingSourceFilter.annulment.value = false;

                    this.IsBusy = true;
                    this.Refresh();

                    // Iniciar cronometro
                    Stopwatch stopwatch = new();
                    stopwatch.Start();

                    var data = await this._accountingEntryMasterService.GetDataContextAsync<AccountingEntriesDataContext>(query, variables);

                    stopwatch.Stop();
                    this.DraftResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

                    // Inserto los placeholders
                    this.AccountingEntriesDraftMaster = new ObservableCollection<AccountingEntryDraftMasterDTO>(this.Context.Mapper.Map<List<AccountingEntryDraftMasterDTO>>(data.AccountingEntryDraftMasterPage.Rows));
                    this.DraftTotalCount = data.AccountingEntryDraftMasterPage.Count;
                    this.CostCenters = data.CostCenters;
                    this.CostCenters.Insert(0, new CostCenterGraphQLModel() { Id = 0, Name = "SELECCIONE CENTRO DE COSTO" });

                    Context.EventAggregator.PublishOnCurrentThreadAsync(new CostCenterUpdateMasterListMessage() { CostCenters = [.. this.CostCenters.ToList()] });

                    this.AccountingSources = data.AccountingSources;
                    this.AccountingSources.Insert(0, new AccountingSourceGraphQLModel() { Id = 0, Name = "SELECCIONE FUENTE CONTABLE" });
                    Context.EventAggregator.PublishOnCurrentThreadAsync(new AccountingSourceUpdateMasterListMessage() { AccountingSources = [.. this.AccountingSources.ToList()] });

                    
                    this.AccountingBooks = data.AccountingBooks;
                    Context.EventAggregator.PublishOnCurrentThreadAsync(new AccountingBookUpdateMasterListMessage() { AccountingBooks = [.. this.AccountingBooks.ToList()] });

                    
                    // Filters
                    this.SelectedAccountingBookId = this.AccountingBooks.FirstOrDefault().Id;
                    this.SelectedCostCenterId = this.CostCenters.FirstOrDefault().Id;
                    this.SelectedAccountingSourceId = this.AccountingSources.FirstOrDefault().Id;
                    this.Refresh();
                }
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
                await this.Context.ActivateDetailViewForEditAsync(p as AccountingEntryDraftMasterGraphQLModel);
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

        public Task HandleAsync(AccountingEntryMasterGraphQLModel message, CancellationToken cancellationToken)
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

        public Task HandleAsync(AccountingEntryDraftMasterGraphQLModel message, CancellationToken cancellationToken)
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

        public Task HandleAsync(CostCenterCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                CostCenterGraphQLModel costCenter = this.CostCenters.Where(x => x.Id == message.CreatedCostCenter.Id).FirstOrDefault();
                if (costCenter == null) this.CostCenters.Add(message.CreatedCostCenter);
                Context.EventAggregator.PublishOnCurrentThreadAsync(new CostCenterUpdateMasterListMessage() { CostCenters = [.. this.CostCenters.ToList()] });

            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(CostCenterUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                CostCenterGraphQLModel updatedCostCenter = this.CostCenters.Where(x => x.Id == message.UpdatedCostCenter.Id).FirstOrDefault();
                if (updatedCostCenter != null) this.CostCenters.Replace(message.UpdatedCostCenter);
                Context.EventAggregator.PublishOnCurrentThreadAsync(new CostCenterUpdateMasterListMessage() { CostCenters = [.. this.CostCenters.ToList()] });
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(CostCenterDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                CostCenterGraphQLModel deletedCostCenter = this.CostCenters.Where(x => x.Id == message.DeletedCostCenter.Id).FirstOrDefault();
                if (deletedCostCenter != null) this.CostCenters.Remove(deletedCostCenter);
                Context.EventAggregator.PublishOnCurrentThreadAsync(new CostCenterUpdateMasterListMessage() { CostCenters = [.. this.CostCenters.ToList()] });

            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingSourceCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                AccountingSourceGraphQLModel createdAccountingSource = this.AccountingSources.Where(x => x.Id == message.CreatedAccountingSource.Id).FirstOrDefault();
                if (createdAccountingSource is null) this.AccountingSources.Add(message.CreatedAccountingSource);
                Context.EventAggregator.PublishOnCurrentThreadAsync(new AccountingSourceUpdateMasterListMessage() { AccountingSources = [.. this.AccountingSources.ToList()] });

            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingSourceUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                AccountingSourceGraphQLModel updatedAccountingSource = this.AccountingSources.Where(x => x.Id == message.UpdatedAccountingSource.Id).FirstOrDefault();
                if (updatedAccountingSource != null) this.AccountingSources.Replace(message.UpdatedAccountingSource);
                Context.EventAggregator.PublishOnCurrentThreadAsync(new AccountingSourceUpdateMasterListMessage() { AccountingSources = [.. this.AccountingSources.ToList()] });
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingSourceDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                AccountingSourceGraphQLModel deletedAccountingSource = this.AccountingSources.Where(x => x.Id == message.DeletedAccountingSource.Id).FirstOrDefault();
                if (deletedAccountingSource != null) this.AccountingSources.Remove(deletedAccountingSource);
                Context.EventAggregator.PublishOnCurrentThreadAsync(new AccountingSourceUpdateMasterListMessage() { AccountingSources = [.. this.AccountingSources.ToList()] });

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
