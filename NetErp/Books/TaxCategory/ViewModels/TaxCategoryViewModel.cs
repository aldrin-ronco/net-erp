using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Books;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.TaxCategory.ViewModels
{
    public class TaxCategoryViewModel : Screen,
        IHandle<TaxCategoryCreateMessage>,
        IHandle<TaxCategoryUpdateMessage>,
        IHandle<TaxCategoryDeleteMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<TaxCategoryGraphQLModel> _taxCategoryService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;

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

        private ObservableCollection<TaxCategoryGraphQLModel> _taxCategories = [];
        public ObservableCollection<TaxCategoryGraphQLModel> TaxCategories
        {
            get => _taxCategories;
            set
            {
                if (_taxCategories != value)
                {
                    _taxCategories = value;
                    NotifyOfPropertyChange(nameof(TaxCategories));
                }
            }
        }

        private TaxCategoryGraphQLModel? _selectedTaxCategory;
        public TaxCategoryGraphQLModel? SelectedTaxCategory
        {
            get => _selectedTaxCategory;
            set
            {
                if (_selectedTaxCategory != value)
                {
                    _selectedTaxCategory = value;
                    NotifyOfPropertyChange(nameof(SelectedTaxCategory));
                    NotifyOfPropertyChange(nameof(CanEditTaxCategory));
                    NotifyOfPropertyChange(nameof(CanDeleteTaxCategory));
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
                    if (string.IsNullOrEmpty(value) || value.Length >= 2) _ = LoadTaxCategoriesAsync();
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

        public bool CanEditTaxCategory => SelectedTaxCategory != null;
        public bool CanDeleteTaxCategory => SelectedTaxCategory != null;

        #endregion

        #region Commands

        private ICommand? _createTaxCategoryCommand;
        public ICommand CreateTaxCategoryCommand
        {
            get
            {
                _createTaxCategoryCommand ??= new AsyncCommand(CreateTaxCategoryAsync);
                return _createTaxCategoryCommand;
            }
        }

        private ICommand? _editTaxCategoryCommand;
        public ICommand EditTaxCategoryCommand
        {
            get
            {
                _editTaxCategoryCommand ??= new AsyncCommand(EditTaxCategoryAsync);
                return _editTaxCategoryCommand;
            }
        }

        private ICommand? _deleteTaxCategoryCommand;
        public ICommand DeleteTaxCategoryCommand
        {
            get
            {
                _deleteTaxCategoryCommand ??= new AsyncCommand(DeleteTaxCategoryAsync);
                return _deleteTaxCategoryCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadTaxCategoriesAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public TaxCategoryViewModel(
            IEventAggregator eventAggregator,
            IRepository<TaxCategoryGraphQLModel> taxCategoryService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService)
        {
            _eventAggregator = eventAggregator;
            _taxCategoryService = taxCategoryService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _eventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            await LoadTaxCategoriesAsync();
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

        public async Task CreateTaxCategoryAsync()
        {
            var detail = new TaxCategoryDetailViewModel(_taxCategoryService, _eventAggregator);
            await _dialogService.ShowDialogAsync(detail, "Nueva categoría de impuesto");
        }

        public async Task EditTaxCategoryAsync()
        {
            if (SelectedTaxCategory == null) return;
            var detail = new TaxCategoryDetailViewModel(_taxCategoryService, _eventAggregator);
            detail.TaxCategoryId = SelectedTaxCategory.Id;
            detail.Code = SelectedTaxCategory.Code;
            detail.Name = SelectedTaxCategory.Name;
            detail.Prefix = SelectedTaxCategory.Prefix;
            detail.UsesPercentage = SelectedTaxCategory.UsesPercentage;
            detail.GeneratedTaxAccountIsRequired = SelectedTaxCategory.GeneratedTaxAccountIsRequired;
            detail.GeneratedTaxRefundAccountIsRequired = SelectedTaxCategory.GeneratedTaxRefundAccountIsRequired;
            detail.DeductibleTaxAccountIsRequired = SelectedTaxCategory.DeductibleTaxAccountIsRequired;
            detail.DeductibleTaxRefundAccountIsRequired = SelectedTaxCategory.DeductibleTaxRefundAccountIsRequired;
            detail.AcceptChanges();
            await _dialogService.ShowDialogAsync(detail, "Editar categoría de impuesto");
        }

        public async Task DeleteTaxCategoryAsync()
        {
            if (SelectedTaxCategory == null) return;
            try
            {
                IsBusy = true;
                Refresh();

                string query = GetCanDeleteTaxCategoryQuery();
                object variables = new { canDeleteResponseId = SelectedTaxCategory.Id };
                var validation = await _taxCategoryService.CanDeleteAsync(query, variables);

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
                DeleteResponseType deletedTaxCategory = await ExecuteDeleteTaxCategoryAsync(SelectedTaxCategory.Id);

                if (!deletedTaxCategory.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro \n\n {deletedTaxCategory.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await _eventAggregator.PublishOnUIThreadAsync(new TaxCategoryDeleteMessage { DeletedTaxCategory = deletedTaxCategory });
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

        public async Task<DeleteResponseType> ExecuteDeleteTaxCategoryAsync(int id)
        {
            string query = GetDeleteTaxCategoryQuery();
            object variables = new { deleteResponseId = id };
            return await _taxCategoryService.DeleteAsync<DeleteResponseType>(query, variables);
        }

        #endregion

        #region Load

        public async Task LoadTaxCategoriesAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = new();
                stopwatch.Start();

                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponseFilters.matching = string.IsNullOrEmpty(FilterSearch)
                    ? ""
                    : FilterSearch.Trim().RemoveExtraSpaces();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.Page = PageIndex;
                variables.pageResponsePagination.PageSize = PageSize;

                string query = GetLoadTaxCategoriesQuery();
                PageType<TaxCategoryGraphQLModel> result = await _taxCategoryService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                TaxCategories = [.. result.Entries];
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

        public string GetLoadTaxCategoriesQuery()
        {
            var fields = FieldSpec<PageType<TaxCategoryGraphQLModel>>
                .Create()
                .Field(it => it.TotalEntries)
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Code)
                    .Field(e => e.Name)
                    .Field(e => e.Prefix)
                    .Field(e => e.UsesPercentage)
                    .Field(e => e.GeneratedTaxAccountIsRequired)
                    .Field(e => e.GeneratedTaxRefundAccountIsRequired)
                    .Field(e => e.DeductibleTaxAccountIsRequired)
                    .Field(e => e.DeductibleTaxRefundAccountIsRequired))
                .Build();

            var filtersParameter = new GraphQLQueryParameter("filters", "TaxCategoryFilters");
            var paginationParameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("taxCategoriesPage", [filtersParameter, paginationParameter], fields, "PageResponse");

            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        public string GetDeleteTaxCategoryQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("deleteTaxCategory", [parameter], fields, alias: "DeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetCanDeleteTaxCategoryQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("canDeleteTaxCategory", [parameter], fields, alias: "CanDeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        #endregion

        #region Event Handlers

        public async Task HandleAsync(TaxCategoryCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadTaxCategoriesAsync();
            _notificationService.ShowSuccess(message.CreatedTaxCategory.Message);
        }

        public async Task HandleAsync(TaxCategoryUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadTaxCategoriesAsync();
            _notificationService.ShowSuccess(message.UpdatedTaxCategory.Message);
        }

        public async Task HandleAsync(TaxCategoryDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadTaxCategoriesAsync();
            SelectedTaxCategory = null;
            _notificationService.ShowSuccess(message.DeletedTaxCategory.Message);
        }

        #endregion
    }
}
