using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DevExpress.XtraPrinting.Native;
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

namespace NetErp.Books.Reports.TestBalance.ViewModels
{
    public class TestBalanceReportViewModel : Screen
    {
        private readonly IRepository<TestBalanceGraphQLModel> _testBalanceService;

        public TestBalanceViewModel Context { get; set; }

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

        // Auxiliary Book Data
        private ObservableCollection<TestBalanceGraphQLModel> _results;
        public ObservableCollection<TestBalanceGraphQLModel> Results
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
                    ValidateProperty(nameof(InitialDate));
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
                    ValidateProperty(nameof(FinalDate));
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

        #endregion

        #region Constructor

        public TestBalanceReportViewModel(TestBalanceViewModel context, IRepository<TestBalanceGraphQLModel> testBalanceService)
        {
            this._errors = new Dictionary<string, List<string>>();
            this.Context = context;
            _testBalanceService = testBalanceService;
        }

        #endregion

        #region Metodos

        // Print
        public void Print()
        {
            App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", "Esta función aun no está implementada", MessageBoxButton.OK, MessageBoxImage.Information));
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

                var result = await Task.Run(() => ExecuteSearch());

                stopwatch.Stop();
                this.ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

                if (result != null)
                {
                    this.Results = new ObservableCollection<TestBalanceGraphQLModel>(result.Rows);
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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención !", text: $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<PageResult<TestBalanceGraphQLModel>> ExecuteSearch()
        {
            try
            {
                string query = @"
                query($filter:TestBalanceFilterInput!) {
                  PageResponse: testBalancePage(filter:$filter) {
                    count
                    rows {
                     code
                     name
                     previousBalance
                     debit
                     credit
                     newBalance
                     level      
                    }
                  }
                }";

                // Si han seleccionado todos los centros de costos, mandar un array de enteros vacio
                int[] costCentersIds = SelectedCostCenters.Count == this.Context.CostCenters.Count ? new int[0] : (from c in SelectedCostCenters select c.Id).ToArray();

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.Pagination = new ExpandoObject();
                variables.filter.Pagination.Page = PageIndex;
                variables.filter.Pagination.PageSize = PageSize;
                variables.filter.AccountingPresentationId = this.SelectedAccountingPresentationId;
                variables.filter.StartDate = Helpers.DateTimeHelper.DateTimeKindUTC(InitialDate);
                variables.filter.EndDate = Helpers.DateTimeHelper.DateTimeKindUTC(FinalDate);
                variables.filter.CostCentersIds = costCentersIds;
                variables.filter.Level = Level;
                var testBalancePage = await this._testBalanceService.GetPageAsync(query, variables);
                return testBalancePage;
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
            ValidateProperty(nameof(InitialDate));
            ValidateProperty(nameof(FinalDate));
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
        private int _pageSize = 1000; // Default PageSize 50
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

    }
}
