using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors.Native;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using NetErp;
using NetErp.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NetErp.Books.Reports.AuxiliaryBook.ViewModels
{
    public class AuxiliaryBookReportViewModel : Screen, INotifyDataErrorInfo
    {
        private readonly IRepository<AuxiliaryBookGraphQLModel> _auxiliaryBookService;

        #region Command's

        private ICommand _interactionCommand;
        public ICommand InteractionCommand
        {
            get
            {
                if (_interactionCommand is null)
                    _interactionCommand = new RelayCommand(CanExecuteUIChange, ExecuteUIChange);
                return _interactionCommand;
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
                return _paginationCommand;
            }
        }

        private ICommand _printCommand;
        public ICommand PrintCommand
        {
            get
            {
                if (_printCommand == null) this._printCommand = new RelayCommand(CanPrint, Print);
                return _printCommand;
            }
        }

        private async void ExecutePaginationChangeIndex(object parameter)
        {
            await Task.Run(() => this.SearchAsync());
        }
        private bool CanExecutePaginationChangeIndex(object parameter)
        {
            return true;
        }

        #endregion


        #region Properties


        // When search parameters change, page need back to 1
        public bool RestartPage { get; set; } = false;

        // Control de errores
        public Dictionary<string, List<string>> _errors;

        // Is Busy
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
                }
            }
        }
        // Presentaciones
        private ObservableCollection<AccountingPresentationGraphQLModel> _accountingPresentations;
        public ObservableCollection<AccountingPresentationGraphQLModel> AccountingPresentations
        {
            get { return _accountingPresentations; }
            set
            {
                if (_accountingPresentations != value)
                {
                    _accountingPresentations = value;
                    NotifyOfPropertyChange(nameof(AccountingPresentations));
                }
            }
        }

        // Centros de costos
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
        // Fuente Contable
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

        // Cuentas Contables para filtro de cuenta final, por alguna razon no me permite usar una sola fuente al usar el combo de autocompletado
        private ObservableCollection<AccountingAccountGraphQLModel> _accountingAccountsEnd;
        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccountsEnd
        {
            get { return _accountingAccountsEnd; }
            set
            {
                if (_accountingAccountsEnd != value)
                {
                    _accountingAccountsEnd = value;
                    NotifyOfPropertyChange(nameof(AccountingAccountsEnd));
                }
            }
        }
        // Auxiliary Book Data
        private ObservableCollection<AuxiliaryBookGraphQLModel> _results;
        public ObservableCollection<AuxiliaryBookGraphQLModel> Results
        {
            get { return _results; }
            set
            {
                if (_results != value)
                {
                    _results = value;
                    NotifyOfPropertyChange(nameof(Results));
                }
            }
        }

        // Selected AccountingPresentation Id
        private int _selectedAccountingPresentationId = 0;
        public int SelectedAccountingPresentationId
        {
            get { return _selectedAccountingPresentationId; }
            set
            {
                if (_selectedAccountingPresentationId != value)
                {
                    _selectedAccountingPresentationId = value;
                    RestartPage = true;
                    NotifyOfPropertyChange(nameof(SelectedAccountingPresentationId));
                    ValidateProperty(nameof(SelectedAccountingPresentationId));
                }
            }
        }

        // Selected CostCenter Id
        private int _selectedCostCenterId = 0;
        public int SelectedCostCenterId
        {
            get { return _selectedCostCenterId; }
            set
            {
                if (_selectedCostCenterId != value)
                {
                    _selectedCostCenterId = value;
                    RestartPage = true;
                    NotifyOfPropertyChange(nameof(SelectedCostCenterId));
                    ValidateProperty(nameof(SelectedCostCenterId));
                }
            }
        }

        // Selected AccountingSource Id
        private ObservableCollection<int> _selectedAccountingSourceIds;
        public ObservableCollection<int> SelectedAccountingSourceIds
        {
            get { return _selectedAccountingSourceIds; }
            set
            {
                if (_selectedAccountingSourceIds != value)
                {
                    _selectedAccountingSourceIds = value;
                    RestartPage = true;
                    NotifyOfPropertyChange(nameof(SelectedAccountingSourceIds));
                    ValidateProperty(nameof(SelectedAccountingSourceIds));
                }
            }
        }

        // Selected AccountingAccount Start Id
        private int _selectedAccountingAccountStartId = 0;
        public int SelectedAccountingAccountStartId
        {
            get { return _selectedAccountingAccountStartId; }
            set
            {
                if (_selectedAccountingAccountStartId != value)
                {
                    _selectedAccountingAccountStartId = value;
                    RestartPage = true;
                    NotifyOfPropertyChange(nameof(SelectedAccountingAccountStartId));
                    ValidateProperty(nameof(SelectedAccountingAccountStartId));
                }
            }
        }

        // Selected AccountingAccount End Id
        private int _selectedAccountingAccountEndId = 0;
        public int SelectedAccountingAccountEndId
        {
            get { return _selectedAccountingAccountEndId; }
            set
            {
                if (_selectedAccountingAccountEndId != value)
                {
                    _selectedAccountingAccountEndId = value;
                    RestartPage = true;
                    NotifyOfPropertyChange(nameof(SelectedAccountingAccountEndId));
                    ValidateProperty(nameof(SelectedAccountingAccountEndId));
                }
            }
        }

        // Initial Date
        private DateTime _initialDate = DateTime.Now;
        public DateTime InitialDate
        {
            get { return _initialDate; }
            set
            {
                if (_initialDate != value)
                {
                    _initialDate = value;
                    RestartPage = true;
                    NotifyOfPropertyChange(nameof(InitialDate));
                }
            }
        }

        // Final Date
        private DateTime _finalDate = DateTime.Now;
        public DateTime FinalDate
        {
            get { return _finalDate; }
            set
            {
                if (_finalDate != value)
                {
                    _finalDate = value;
                    RestartPage = true;
                    NotifyOfPropertyChange(nameof(FinalDate));
                }
            }
        }

        // Selected Cost Centers
        private ObservableCollection<CostCenterGraphQLModel> _selectedCostCenters = new ObservableCollection<CostCenterGraphQLModel>();
        public ObservableCollection<CostCenterGraphQLModel> SelectedCostCenters
        {
            get { return _selectedCostCenters; }
            set
            {
                if (_selectedCostCenters != value)
                {
                    _selectedCostCenters = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenters));
                }
            }
        }

        // Selected Accounting Sources
        private ObservableCollection<AccountingSourceGraphQLModel> _selectedAccountingSources;
        public ObservableCollection<AccountingSourceGraphQLModel> SelectedAccountingSources
        {
            get { return _selectedAccountingSources; }
            set
            {
                if (_selectedAccountingSources != value)
                {
                    _selectedAccountingSources = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingSources));
                    ValidateProperty(nameof(SelectedAccountingSources));
                }
            }
        }

        public AuxiliaryBookViewModel Context { get; set; }

        // Can Search

        public bool CanSearch
        {
            get
            {
                return _errors.Count == 0;
            }
        }

        #endregion

        #region Constructor

        public AuxiliaryBookReportViewModel(AuxiliaryBookViewModel context, IRepository<AuxiliaryBookGraphQLModel> auxiliaryBookService)
        {
            // Validaciones
            this._errors = new Dictionary<string, List<string>>();
            this._auxiliaryBookService = auxiliaryBookService;
            this.Context = context;
            _ = InitializeAsync();
        }

        #endregion

        #region Methods

        public void ExecuteUIChange(object p)
        {
            ValidateProperty(nameof(SelectedCostCenters));
        }

        public bool CanExecuteUIChange(object p)
        {
            return true;
        }

        public async Task SearchAsync()
        {
            try
            {
                // Si hay cambios en los parametros de busqueda, la paginacion debe regresar a la pagina uno 
                if (RestartPage) { PageIndex = 1; RestartPage = false; }

                this.TotalCount = 0;

                this.IsBusy = true;
                this.Refresh();

                // Iniciar cronometro
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                var result = await ExecuteSearchAsync();

                stopwatch.Stop();
                this.ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

                if (result != null)
                {
                    this.Results = new ObservableCollection<AuxiliaryBookGraphQLModel>(result.Rows);
                    this.TotalCount = result.Count;
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
        private async Task InitializeAsync()
        {
            string query = @"
            query ($accountingAccountFilter: AccountingAccountFilterInput, $accountingSourceFilter:AccountingSourceFilterInput ) {
              accountingPresentations{
                id
                name
              },
              costCenters{
                id
                name
              },
              accountingSources(filter: $accountingSourceFilter) {
                id
                reverseId
                name
              },
              accountingAccounts(filter: $accountingAccountFilter) {
                id
                code
                name
              }
            }";
            dynamic variables = new ExpandoObject();
            variables.AccountingSourceFilter = new ExpandoObject();
            variables.AccountingSourceFilter.Annulment = new ExpandoObject();
            variables.AccountingSourceFilter.Annulment.@operator = "=";
            variables.AccountingSourceFilter.Annulment.value = true;
            variables.AccountingAccountFilter = new ExpandoObject();
            variables.AccountingAccountFilter.Code = new ExpandoObject();
            variables.AccountingAccountFilter.Code.@operator = new List<string>() { "length", ">=" };
            variables.AccountingAccountFilter.Code.value = 8;
            var dataContext = await _auxiliaryBookService.GetDataContextAsync<AuxiliaryBookDataContext>(query, variables);
            if (dataContext != null)
            {
                this.AccountingPresentations = new ObservableCollection<AccountingPresentationGraphQLModel>(dataContext.AccountingPresentations);
                this.AccountingSources = new ObservableCollection<AccountingSourceGraphQLModel>(dataContext.AccountingSources);
                this.CostCenters = new ObservableCollection<CostCenterGraphQLModel>(dataContext.CostCenters);
                this.AccountingAccounts = new ObservableCollection<AccountingAccountGraphQLModel>(dataContext.AccountingAccounts);
                this.AccountingAccountsEnd = new ObservableCollection<AccountingAccountGraphQLModel>(dataContext.AccountingAccounts);

                // Initial Selected Values
                if (this.CostCenters != null)
                    this.SelectedCostCenters = new ObservableCollection<CostCenterGraphQLModel>(this.CostCenters);

                if (this.AccountingPresentations != null)
                    this.SelectedAccountingPresentationId = this.AccountingPresentations.FirstOrDefault().Id;

                if (this.AccountingSources != null)
                    this.SelectedAccountingSources = new ObservableCollection<AccountingSourceGraphQLModel>(this.AccountingSources);

            }
        }
        public async Task<PageResult<AuxiliaryBookGraphQLModel>> ExecuteSearchAsync()
        {
            try
            {
                string query = @"
                query ($filter:AuxiliaryBookFilterInput!) {
                  PageResponse: auxiliaryBookPage(filter:$filter) {
                    count
                    rows {
                      shortName
                      documentDate
                      fullCode
                      documentNumber
                      fullName
                      identificationNumber
                      verificationDigit    
                      recordDetail
                      debit
                      credit
                      balance
                      recordType    
                      accountingAccountCode
                      accountingAccountName      
                    }
                  }
                }";

                string accountingCodeStart = this.AccountingAccounts.Where(x => x.Id == this.SelectedAccountingAccountStartId).FirstOrDefault().Code;
                string accountingCodeEnd = this.AccountingAccounts.Where(x => x.Id == this.SelectedAccountingAccountEndId).FirstOrDefault().Code;
                int[] costCentersIds = SelectedCostCenters.Count == this.CostCenters.Count ? [] : (from c in SelectedCostCenters select c.Id).ToArray();
                int[] accountingSourcesIds = SelectedAccountingSources.Count == this.AccountingSources.Count ? [] : (from s in SelectedAccountingSources select s.Id).ToArray();

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.Pagination = new ExpandoObject();
                variables.filter.Pagination.Page = PageIndex;
                variables.filter.Pagination.PageSize = PageSize;
                variables.filter.AccountingPresentationId = this.SelectedAccountingPresentationId;
                variables.filter.StartDate = DateTimeHelper.DateTimeKindUTC(this.InitialDate);
                variables.filter.EndDate = DateTimeHelper.DateTimeKindUTC(this.FinalDate);
                variables.filter.CostCentersIds = costCentersIds;
                variables.filter.AccountingSourcesIds = accountingSourcesIds;
                variables.filter.AccountingCodeStart = accountingCodeStart;
                variables.filter.AccountingCodeEnd = accountingCodeEnd;
                var auxiliaryBookPage = await this._auxiliaryBookService.GetPageAsync(query, variables);
                return auxiliaryBookPage;
            }
            catch (Exception)
            {
                throw;
            }
        }

        // Print
        public async void Print(object parameter)
        {
            App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", "Esta función aun no está implementada", MessageBoxButton.OK, MessageBoxImage.Information));
        }

        private bool CanPrint(object parameter)
        {
            return true;
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
                case nameof(SelectedAccountingPresentationId):
                    if (this.SelectedAccountingPresentationId == 0) AddError(propertyName, "La presentación no puede estar vacía");
                    break;
                case nameof(SelectedCostCenters):
                    if (SelectedCostCenters is null || SelectedCostCenters.Count == 0) AddError(propertyName, "Debe seleccionar por lo menos un centro de costo");
                    break;
                case nameof(SelectedAccountingSources):
                    if (this.SelectedAccountingSources is null || this.SelectedAccountingSources.Count == 0) AddError(propertyName, "Debe seleccionar por lo menos una fuente contable");
                    break;
                case nameof(SelectedAccountingSourceIds):
                    if (this.SelectedAccountingSourceIds is null || this.SelectedAccountingSourceIds.Count == 0) AddError(propertyName, "Debe seleccionar por lo menos una fuente contable");
                    break;
                case nameof(SelectedAccountingAccountStartId):
                    if (this.SelectedAccountingAccountStartId == 0) AddError(propertyName, "Debe seleccionar la cuenta contable inicial");
                    break;
                case nameof(SelectedAccountingAccountEndId):
                    if (this.SelectedAccountingAccountEndId == 0) AddError(propertyName, "Debe seleccionar la cuenta contable final");
                    break;
                default:
                    break;
            }

            NotifyOfPropertyChange(nameof(CanSearch));
        }

        private void ValidateProperties()
        {
            ValidateProperty(nameof(SelectedAccountingPresentationId));
            ValidateProperty(nameof(SelectedCostCenters));
            ValidateProperty(nameof(SelectedAccountingSources));
            ValidateProperty(nameof(SelectedAccountingAccountStartId));
            ValidateProperty(nameof(SelectedAccountingAccountEndId));
        }

        #endregion

        #region Paginacion

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
                    NotifyOfPropertyChange(nameof(PageIndex));
                }
            }
        }
        /// <summary>
        /// PageSize
        /// </summary>
        private int _pageSize = 1000; // Default PageSize 1000
        public int PageSize
        {
            get { return _pageSize; }
            set
            {
                if (_pageSize != value)
                {
                    _pageSize = value;
                    NotifyOfPropertyChange(nameof(PageSize));
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
                    NotifyOfPropertyChange(nameof(TotalCount));
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
                    NotifyOfPropertyChange(nameof(ResponseTime));
                }
            }
        }

        #endregion

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperties();
            this.SetFocus(nameof(this.InitialDate));
        }
    }
}
