using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
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

namespace NetErp.Books.Reports.AnnualIncomeStatement.ViewModels
{
    public class AnnualIncomeStatementReportViewModel : Screen
    {
        public AnnualIncomeStatementViewModel Context { get; set; }
        private readonly IRepository<AnnualIncomeStatementGraphQLModel> _annualIncomeStatementService;
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

        /// <summary>
        /// PaginationCommand para controlar evento
        /// </summary>
        private ICommand _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                if (_paginationCommand == null) this._paginationCommand = new RelayCommand(CanExecuteChangeIndex, ExecuteChangeIndex);
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

        private void ExecuteChangeIndex(object parameter)
        {
            Task.Run(() => true);
        }

        private bool CanExecuteChangeIndex(object parameter)
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
        private ObservableCollection<AnnualIncomeStatementGraphQLModel> _results;
        public ObservableCollection<AnnualIncomeStatementGraphQLModel> Results
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
                    ValidateProperty(nameof(SelectedAccountingPresentationId));
                    NotifyOfPropertyChange(nameof(SelectedAccountingPresentationId));
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
                    ValidateProperty(nameof(SelectedCostCenterId));
                    NotifyOfPropertyChange(nameof(SelectedCostCenterId));
                }
            }
        }

        // Selected Year
        private int _selectedYear = DateTime.Now.Year;
        public int SelectedYear
        {
            get { return _selectedYear; }
            set
            {
                if (_selectedYear != value)
                {
                    _selectedYear = value;
                    if (value >= DateTime.Now.Year)
                    {
                        SelectedMonth = DateTime.Now.Month;
                    }
                    else
                    {
                        SelectedMonth = 12;
                    }
                    RestartPage = true;
                    NotifyOfPropertyChange(nameof(SelectedYear));
                    NotifyOfPropertyChange(nameof(SelectedMonth));
                }
            }
        }

        // Selected Month
        private int _selectedMonth = DateTime.Now.Month;
        public int SelectedMonth
        {
            get { return _selectedMonth; }
            set
            {
                if (_selectedMonth != value)
                {
                    _selectedMonth = value;
                    RestartPage = true;
                    NotifyOfPropertyChange(nameof(SelectedMonth));
                }
            }
        }

        // Report Level
        private int _level = 5;
        public int Level
        {
            get { return _level; }
            set
            {
                if (_level != value)
                {
                    _level = value;
                    RestartPage = true;
                    NotifyOfPropertyChange(nameof(Level));
                    ValidateProperty(nameof(Level));
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
                    RestartPage = true;
                    NotifyOfPropertyChange(nameof(SelectedCostCenters));
                    ValidateProperty(nameof(SelectedCostCenters));
                }
            }
        }

        // Can Search
        public bool CanSearch
        {
            get
            {
                return _errors.Count == 0;
            }
        }

        // Meses
        public Dictionary<int, string> Months
        {
            get { return Dictionaries.GlobalDictionaries.MonthsDictionary; }
        }

        #endregion

        #region Metodos

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

                var result = await Task.Run(() => ExecuteSearch());

                stopwatch.Stop();
                this.ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

                if (result != null)
                {
                    this.Results = new ObservableCollection<AnnualIncomeStatementGraphQLModel>(result.Rows);
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
                this.IsBusy = false;
            }
        }

        public async Task<PageResult<AnnualIncomeStatementGraphQLModel>> ExecuteSearch()
        {
            try
            {
                string query = @"
                query($filter:AnnualIncomeStatementFilterInput!) {
                  PageResponse: annualIncomeStatementPage(filter:$filter) {
                    count
                    rows {
                      code
                      name
                      m1
                      m2
                      m3
                      m4
                      m5
                      m6
                      m7
                      m8
                      m9
                      m10
                      m11
                      m12
                      total
                      level
                      recordType
                    }
                  }
                }";

                // Si han seleccionado todos los centros de costos, mandar un array de enteros vacio
                int[] costCentersIds = SelectedCostCenters.Count == this.Context.CostCenters.Count ? new int[0] : (from c in SelectedCostCenters select c.Id).ToArray();

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();


                if (costCentersIds.Length > 0) 
                {
                    variables.filter.CostCentersIds = new ExpandoObject();
                    variables.filter.CostCentersIds.@operator = "=";
                    variables.filter.CostCentersIds.value = costCentersIds;
                }

                variables.filter.Year = new ExpandoObject();
                variables.filter.Year.@operator = "=";
                variables.filter.Year.value = this.SelectedYear;

                variables.filter.Month = new ExpandoObject();
                variables.filter.Month.@operator = "<=";
                variables.filter.Month.value = this.SelectedMonth;

                variables.filter.Level = new ExpandoObject();
                variables.filter.Level.@operator = "=";
                variables.filter.Level.value = this.Level;
                variables.filter.Level.exclude = true;


                //variables.filter.AccountingPresentationId = this.SelectedAccountingPresentationId; Filtro de usado de momento

                //Pagination
                variables.filter.Pagination = new ExpandoObject();
                variables.filter.Pagination.Page = PageIndex;
                variables.filter.Pagination.PageSize = PageSize;
                var annualIncomeStatementPage = await this._annualIncomeStatementService.GetPageAsync(query, variables);
                return annualIncomeStatementPage;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

        #region Constructor

        public AnnualIncomeStatementReportViewModel(AnnualIncomeStatementViewModel context, IRepository<AnnualIncomeStatementGraphQLModel> annualIncomeStatementService)
        {
            this._errors = new Dictionary<string, List<string>>();
            this.Context = context;
            _annualIncomeStatementService = annualIncomeStatementService;
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
                case nameof(SelectedCostCenterId):
                    if (SelectedCostCenters is null || SelectedCostCenters.Count == 0) AddError(propertyName, "Debe seleccionar por lo menos un centro de costo");
                    break;
                case nameof(Level):
                    if (Level <= 0) AddError(propertyName, "El nivel de la cosulta no es válido");
                    break;
                //case nameof(InitialDate):
                //    DateTime tempDate;
                //    if (!DateTime.TryParse(InitialDate, out tempDate)) AddError(propertyName, "El nivel de la cosulta no es válido");
                //    break;

                default:
                    break;
            }

            NotifyOfPropertyChange(nameof(CanSearch));
        }

        private void ValidateProperties()
        {
            ValidateProperty(nameof(SelectedAccountingPresentationId));
            ValidateProperty(nameof(SelectedCostCenters));
            ValidateProperty(nameof(Level));
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
        private void ExecutePaginationChangeIndex(object parameter)
        {
            Task.Run(() => 0);
        }
        private bool CanExecutePaginationChangeIndex(object parameter)
        {
            return true;
        }

        #endregion
    }
}
