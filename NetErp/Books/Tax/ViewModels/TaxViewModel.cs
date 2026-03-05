using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Books;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.Tax.ViewModels
{
    public class TaxViewModel : Screen,
        IHandle<TaxCreateMessage>,
        IHandle<TaxUpdateMessage>,
        IHandle<TaxDeleteMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<TaxGraphQLModel> _taxService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;
        private readonly TaxCategoryCache _taxCategoryCache;

        #endregion

        #region Grid Properties

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        private ObservableCollection<TaxGraphQLModel> _taxes = [];
        public ObservableCollection<TaxGraphQLModel> Taxes
        {
            get => _taxes;
            set
            {
                if (_taxes != value)
                {
                    _taxes = value;
                    NotifyOfPropertyChange(nameof(Taxes));
                }
            }
        }

        private TaxGraphQLModel? _selectedTax;
        public TaxGraphQLModel? SelectedTax
        {
            get => _selectedTax;
            set
            {
                if (_selectedTax != value)
                {
                    _selectedTax = value;
                    NotifyOfPropertyChange(nameof(SelectedTax));
                    NotifyOfPropertyChange(nameof(CanEditTax));
                    NotifyOfPropertyChange(nameof(CanDeleteTax));
                }
            }
        }

        private string _filterSearch = string.Empty;
        public string FilterSearch
        {
            get => _filterSearch;
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 2) _ = LoadTaxesAsync();
                }
            }
        }

        private bool _showOnlyActive = true;
        public bool ShowOnlyActive
        {
            get => _showOnlyActive;
            set
            {
                if (_showOnlyActive != value)
                {
                    _showOnlyActive = value;
                    NotifyOfPropertyChange(nameof(ShowOnlyActive));
                    PageIndex = 1;
                    _ = LoadTaxesAsync();
                }
            }
        }

        private int _pageIndex = 1;
        public int PageIndex
        {
            get => _pageIndex;
            set
            {
                if (_pageIndex != value)
                {
                    _pageIndex = value;
                    NotifyOfPropertyChange(nameof(PageIndex));
                }
            }
        }

        private int _pageSize = 50;
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (_pageSize != value)
                {
                    _pageSize = value;
                    NotifyOfPropertyChange(nameof(PageSize));
                }
            }
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (_totalCount != value)
                {
                    _totalCount = value;
                    NotifyOfPropertyChange(nameof(TotalCount));
                }
            }
        }

        private string _responseTime = string.Empty;
        public string ResponseTime
        {
            get => _responseTime;
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

        #region Button States

        public bool CanEditTax => SelectedTax != null;
        public bool CanDeleteTax => SelectedTax != null;

        #endregion

        #region Commands

        private ICommand? _createTaxCommand;
        public ICommand CreateTaxCommand
        {
            get
            {
                _createTaxCommand ??= new AsyncCommand(CreateTaxAsync);
                return _createTaxCommand;
            }
        }

        private ICommand? _editTaxCommand;
        public ICommand EditTaxCommand
        {
            get
            {
                _editTaxCommand ??= new AsyncCommand(EditTaxAsync);
                return _editTaxCommand;
            }
        }

        private ICommand? _deleteTaxCommand;
        public ICommand DeleteTaxCommand
        {
            get
            {
                _deleteTaxCommand ??= new AsyncCommand(DeleteTaxAsync);
                return _deleteTaxCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadTaxesAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public TaxViewModel(
            IEventAggregator eventAggregator,
            IRepository<TaxGraphQLModel> taxService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache,
            TaxCategoryCache taxCategoryCache)
        {
            _eventAggregator = eventAggregator;
            _taxService = taxService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;
            _taxCategoryCache = taxCategoryCache;
            _eventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            await LoadTaxesAsync();
        }

        protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                _eventAggregator.Unsubscribe(this);
            }
            await base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region CRUD Operations

        public async Task CreateTaxAsync()
        {
            try
            {
                var detail = new TaxDetailViewModel(_taxService, _eventAggregator, _auxiliaryAccountingAccountCache, _taxCategoryCache);
                await detail.InitializeAsync();
                await _dialogService.ShowDialogAsync(detail, "Nuevo impuesto");
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{currentMethod!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        public async Task EditTaxAsync()
        {
            if (SelectedTax == null) return;
            try
            {
                var detail = new TaxDetailViewModel(_taxService, _eventAggregator, _auxiliaryAccountingAccountCache, _taxCategoryCache);
                await detail.InitializeAsync();
                await detail.LoadDataForEditAsync(SelectedTax.Id);
                await _dialogService.ShowDialogAsync(detail, "Editar impuesto");
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{currentMethod!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        public async Task DeleteTaxAsync()
        {
            if (SelectedTax == null) return;
            try
            {
                IsBusy = true;
                Refresh();

                string query = GetCanDeleteTaxQuery();
                object variables = new { canDeleteResponseId = SelectedTax.Id };
                var validation = await _taxService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    if (ThemedMessageBox.Show("Atención !",
                        "¿Confirma que desea eliminar el registro seleccionado?",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                }
                else
                {
                    IsBusy = false;
                    ThemedMessageBox.Show("Atención !",
                        $"El registro no puede ser eliminado\r\n\r\n{validation.Message}",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                DeleteResponseType deletedTax = await ExecuteDeleteTaxAsync(SelectedTax.Id);

                if (!deletedTax.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro \n\n {deletedTax.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await _eventAggregator.PublishOnUIThreadAsync(new TaxDeleteMessage { DeletedTax = deletedTax });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content!.ToString()!);
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()!.Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<DeleteResponseType> ExecuteDeleteTaxAsync(int id)
        {
            string query = GetDeleteTaxQuery();
            object variables = new { deleteResponseId = id };
            return await _taxService.DeleteAsync<DeleteResponseType>(query, variables);
        }

        #endregion

        #region Load

        public async Task LoadTaxesAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = new();
                stopwatch.Start();

                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch))
                    variables.pageResponseFilters.name = FilterSearch.Trim().RemoveExtraSpaces();
                if (ShowOnlyActive)
                    variables.pageResponseFilters.isActive = true;
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.Page = PageIndex;
                variables.pageResponsePagination.PageSize = PageSize;

                string query = GetLoadTaxesQuery();
                PageType<TaxGraphQLModel> result = await _taxService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                Taxes = [.. result.Entries];
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content!.ToString()!);
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()!.Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        public string GetLoadTaxesQuery()
        {
            var fields = FieldSpec<PageType<TaxGraphQLModel>>
                .Create()
                .Field(it => it.TotalEntries)
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Rate)
                    .Field(e => e.IsActive)
                    .Field(e => e.Formula)
                    .Select(e => e.TaxCategory, cat => cat
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                        .Field(c => c.GeneratedTaxAccountIsRequired)
                        .Field(c => c.GeneratedTaxRefundAccountIsRequired)
                        .Field(c => c.DeductibleTaxAccountIsRequired)
                        .Field(c => c.DeductibleTaxRefundAccountIsRequired))
                    .Select(e => e.GeneratedTaxAccount, acc => acc
                        .Field(a => a.Id)
                        .Field(a => a.Name))
                    .Select(e => e.GeneratedTaxRefundAccount, acc => acc
                        .Field(a => a.Id)
                        .Field(a => a.Name))
                    .Select(e => e.DeductibleTaxAccount, acc => acc
                        .Field(a => a.Id)
                        .Field(a => a.Name))
                    .Select(e => e.DeductibleTaxRefundAccount, acc => acc
                        .Field(a => a.Id)
                        .Field(a => a.Name)))
                .Build();

            var filtersParameter = new GraphQLQueryParameter("filters", "TaxFilters");
            var paginationParameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("taxesPage", [filtersParameter, paginationParameter], fields, "PageResponse");

            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        public string GetDeleteTaxQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("deleteTax", [parameter], fields, alias: "DeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetCanDeleteTaxQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("canDeleteTax", [parameter], fields, alias: "CanDeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        #endregion

        #region Event Handlers

        public async Task HandleAsync(TaxCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadTaxesAsync();
            _notificationService.ShowSuccess(message.CreatedTax.Message);
        }

        public async Task HandleAsync(TaxUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadTaxesAsync();
            _notificationService.ShowSuccess(message.UpdatedTax.Message);
        }

        public async Task HandleAsync(TaxDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadTaxesAsync();
            SelectedTax = null;
            _notificationService.ShowSuccess(message.DeletedTax.Message);
        }

        #endregion
    }
}
