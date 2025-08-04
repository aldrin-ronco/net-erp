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
                    NotifyOfPropertyChange(nameof(AccountingSources));
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

        public AuxiliaryBookReportViewModel(AuxiliaryBookViewModel context)
        {
            // Validaciones
            this._errors = new Dictionary<string, List<string>>();

            this.Context = context;
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
                    this.Results = new ObservableCollection<AuxiliaryBookGraphQLModel>(result.PageResponse.Rows);
                    this.TotalCount = result.PageResponse.Count;
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

        public async Task<IGenericDataAccess<AuxiliaryBookGraphQLModel>.PageResponseType> ExecuteSearch()
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

                string accountingCodeStart = this.Context.AccountingAccounts.Where(x => x.Id == this.SelectedAccountingAccountStartId).FirstOrDefault().Code;
                string accountingCodeEnd = this.Context.AccountingAccounts.Where(x => x.Id == this.SelectedAccountingAccountEndId).FirstOrDefault().Code;
                int[] costCentersIds = SelectedCostCenters.Count == this.Context.CostCenters.Count ? [] : (from c in SelectedCostCenters select c.Id).ToArray();
                int[] accountingSourcesIds = SelectedAccountingSources.Count == this.Context.AccountingSources.Count ? [] : (from s in SelectedAccountingSources select s.Id).ToArray();

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
                var auxiliaryBookPage = await this.Context.AuxiliaryBookService.GetPage(query, variables);
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
