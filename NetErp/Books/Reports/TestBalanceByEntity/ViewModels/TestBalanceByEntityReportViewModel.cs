using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NetErp.Books.Reports.TestBalanceByEntity.ViewModels
{
    public class TestBalanceByEntityReportViewModel : Screen, INotifyDataErrorInfo
    {
        public TestBalanceByEntityViewModel Context { get; set; }

        #region Command's

        public void ExecuteUIChange(object p)
        {
            ValidateProperty(nameof(SelectedCostCenters));
        }

        public bool CanExecuteUIChange(object p)
        {
            return true;
        }

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
                if (_printCommand == null) this._printCommand = new DelegateCommand(Print);
                return _printCommand;
            }
        }

        private async void ExecutePaginationChangeIndex(object parameter)
        {
            await Task.Run(() => this.Search());
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
                    NotifyOfPropertyChange(nameof(CanSearchForAccountingEntityMatch));
                }
            }
        }

        // Auxiliary Book Data
        private ObservableCollection<TestBalanceByEntityGraphQLModel> _results;
        public ObservableCollection<TestBalanceByEntityGraphQLModel> Results
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
                    NotifyOfPropertyChange(nameof(CanSearchForAccountingEntityMatch));
                    NotifyOfPropertyChange(nameof(SelectedAccountingEntityId));
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
        private int _selectedAccountingSourceId;
        public int SelectedAccountingSourceId
        {
            get { return _selectedAccountingSourceId; }
            set
            {
                if (_selectedAccountingSourceId != value)
                {
                    _selectedAccountingSourceId = value;
                    RestartPage = true;
                    NotifyOfPropertyChange(nameof(SelectedAccountingSourceId));
                    ValidateProperty(nameof(SelectedAccountingSourceId));
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

        // Selected Accounting Entry
        private int _selectedAccountingEntityId = 0;
        public int SelectedAccountingEntityId
        {
            get { return _selectedAccountingEntityId; }
            set
            {
                if (_selectedAccountingEntityId != value)
                {
                    _selectedAccountingEntityId = value;
                    RestartPage = true;
                    NotifyOfPropertyChange(nameof(SelectedAccountingEntityId));
                    NotifyOfPropertyChange(nameof(CanSearchForAccountingEntityMatch));
                    NotifyOfPropertyChange(nameof(CanSearch));
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
                    NotifyOfPropertyChange(nameof(CanSearch));
                }
            }
        }

        // Initial Date
        private DateTime _initialDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
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
        private DateTime _finalDate = DateTime.Now.Date;
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
                    NotifyOfPropertyChange(nameof(AccountingSources));
                }
            }
        }

        // Can Search

        public bool CanSearch
        {
            get
            {
                return _errors.Count == 0 && ((FilterSearchAccountingEntity.Trim() == "") || (SelectedAccountingEntityId > 0));
            }
        }

        // Habilitar Search Entity
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

        //
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
                    NotifyOfPropertyChange(nameof(CanSearchForAccountingEntityMatch));
                }
            }
        }

        #endregion

        #region Constructor

        public TestBalanceByEntityReportViewModel(TestBalanceByEntityViewModel context)
        {
            this._errors = new Dictionary<string, List<string>>();
            this.Context = context;
        }

        #endregion

        #region Metodos

        public async void SearchForAccountingEntityMatch()
        {
            try
            {
                this.IsBusy = true;
                this.Refresh();

                if (this.IsFilterSearchAccountinEntityOnEditMode)
                {
                    this.IsFilterSearchAccountinEntityOnEditMode = false;
                    await Task.Run(() => this.ExecuteSearchForAccountingEntityMatch());
                    App.Current.Dispatcher.Invoke(() => this.SetFocus(nameof(SelectedAccountingEntityId)));
                }
                else
                {
                    this.IsFilterSearchAccountinEntityOnEditMode = true;
                    this.SelectedAccountingEntityId = 0;
                    App.Current.Dispatcher.Invoke(() => this.SetFocus(nameof(FilterSearchAccountingEntity)));
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
                if (string.IsNullOrEmpty(this.FilterSearchAccountingEntity))
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        this.AccountingEntitiesSearchResults.Clear();
                    });
                    return;
                }

                string query = @"
                query ($filter: AccountingEntityFilterInput) {
                  ListResponse: accountingEntities(filter: $filter) {
                    id
                    identificationNumber
                    searchName
                  }
                }";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.searchName = new ExpandoObject();
                variables.filter.searchName.@operator = "like";
                variables.filter.searchName.value = this.FilterSearchAccountingEntity.Replace(" ", "%").Trim().RemoveExtraSpaces();
                var accountingEntities = await this.Context.AccountingEntityService.GetList(query, variables);
                this.AccountingEntitiesSearchResults = new ObservableCollection<AccountingEntityGraphQLModel>(accountingEntities);
                App.Current.Dispatcher.Invoke(() =>
                {
                    AccountingEntitiesSearchResults.Insert(0, new AccountingEntityGraphQLModel() { Id = 0, SearchName = "SELECCIONE UN TERCERO" });
                    // Si el resultado es un solo registro, evitamos el mensaje de : seleccione
                    if (AccountingEntitiesSearchResults.ToList().Count == 2) AccountingEntitiesSearchResults = AccountingEntitiesSearchResults.Where(x => x.Id != 0).ToObservableCollection();
                });

                this.SelectedAccountingEntityId = -1;
                this.SelectedAccountingEntityId = this.AccountingEntitiesSearchResults.FirstOrDefault().Id;

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
        }

        // Print
        public void Print()
        {
            App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", "Esta función aun no está implementada", MessageBoxButton.OK, MessageBoxImage.Information));
        }

        public async Task Search()
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

                var result = await ExecuteSearch();

                stopwatch.Stop();
                this.ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

                if (result != null)
                {
                    this.Results = new ObservableCollection<TestBalanceByEntityGraphQLModel>(result.PageResponse.Rows);
                    this.TotalCount = result.PageResponse.Count;
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

        public async Task<IGenericDataAccess<TestBalanceByEntityGraphQLModel>.PageResponseType> ExecuteSearch()
        {
            try
            {
                string query = @"
                query ($filter: TestBalanceByEntityFilterInput!) {
                  PageResponse: testBalanceByEntityPage(filter: $filter) {
                    count
                    rows {
                      nature
                      code
                      name
                      accountingEntityId
                      identificationNumber
                      verificationDigit
                      searchName
                      previousBalance
                      debit
                      credit
                      newBalance
                      level  
                    }
                  }
                }";

                string accountingCodeStart = this.Context.AccountingAccounts.Where(x => x.Id == this.SelectedAccountingAccountStartId).FirstOrDefault().Code;
                string accountingCodeEnd = this.Context.AccountingAccounts.Where(x => x.Id == this.SelectedAccountingAccountEndId).FirstOrDefault().Code;

                // Si han seleccionado todos los centros de costos, mandar un array de enteros vacio
                int[] costCentersIds = SelectedCostCenters.Count == this.Context.CostCenters.Count ? new int[0] : (from c in SelectedCostCenters select c.Id).ToArray();

                // Si han seleccionado todas las fuentes contables, mandar un array de enteros vacio
                int[] accountingSourcesIds = SelectedAccountingSources.Count == this.Context.AccountingSources.Count ? new int[0] : (from s in SelectedAccountingSources select s.Id).ToArray();

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.Pagination = new ExpandoObject();
                variables.filter.Pagination.Page = PageIndex;
                variables.filter.Pagination.PageSize = PageSize;
                variables.filter.AccountingPresentationId = SelectedAccountingPresentationId;
                variables.filter.StartDate = InitialDate.ToUniversalTime();
                variables.filter.EndDate = FinalDate.ToUniversalTime();
                variables.filter.CostCentersIds = costCentersIds;
                variables.filter.AccountingEntityId = SelectedAccountingEntityId;
                variables.filter.AccountingSourcesIds = accountingSourcesIds;
                variables.filter.AccountingCodeStart = accountingCodeStart;
                variables.filter.AccountingCodeEnd = accountingCodeEnd;
                var testBalanceByEntityPage = await this.Context.TestBalanceByEntityService.GetPage(query, variables);
                return testBalanceByEntityPage;
            }
            catch (Exception)
            {
                throw;
            }
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
        /// <summary>
        /// PaginationCommand para controlar evento
        /// </summary>

        #endregion

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperties();
            this.SetFocus(nameof(this.InitialDate));
        }

    }
}
