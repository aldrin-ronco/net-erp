using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using NetErp.Helpers;
using IDialogService = NetErp.Helpers.IDialogService;
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

namespace NetErp.Books.AccountingSources.ViewModels
{
    public class AccountingSourceViewModel : Screen,
        IHandle<AccountingSourceCreateMessage>,
        IHandle<AccountingSourceUpdateMessage>,
        IHandle<AccountingSourceDeleteMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<AccountingSourceGraphQLModel> _accountingSourceService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IDialogService _dialogService;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;
        private readonly ProcessTypeCache _processTypeCache;
        private readonly MenuModuleCache _menuModuleCache;

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

        private ObservableCollection<AccountingSourceGraphQLModel> _accountingSources = [];
        public ObservableCollection<AccountingSourceGraphQLModel> AccountingSources
        {
            get => _accountingSources;
            set
            {
                if (_accountingSources != value)
                {
                    _accountingSources = value;
                    NotifyOfPropertyChange(nameof(AccountingSources));
                }
            }
        }

        private AccountingSourceGraphQLModel? _selectedAccountingSource;
        public AccountingSourceGraphQLModel? SelectedAccountingSource
        {
            get => _selectedAccountingSource;
            set
            {
                if (_selectedAccountingSource != value)
                {
                    _selectedAccountingSource = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingSource));
                    NotifyOfPropertyChange(nameof(CanEditSource));
                    NotifyOfPropertyChange(nameof(CanDeleteSource));
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
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = LoadAccountingSourcesAsync();
                }
            }
        }

        private int? _selectedModuleId;
        public int? SelectedModuleId
        {
            get => _selectedModuleId;
            set
            {
                if (_selectedModuleId != value)
                {
                    _selectedModuleId = value;
                    NotifyOfPropertyChange(nameof(SelectedModuleId));
                    PageIndex = 1;
                    _ = LoadAccountingSourcesAsync();
                }
            }
        }

        private ObservableCollection<MenuModuleGraphQLModel> _modules = [];
        public ObservableCollection<MenuModuleGraphQLModel> Modules
        {
            get => _modules;
            set
            {
                if (_modules != value)
                {
                    _modules = value;
                    NotifyOfPropertyChange(nameof(Modules));
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

        public bool CanEditSource => SelectedAccountingSource != null;
        public bool CanDeleteSource => SelectedAccountingSource != null;

        #endregion

        #region Commands

        private ICommand? _createSourceCommand;
        public ICommand CreateSourceCommand
        {
            get
            {
                _createSourceCommand ??= new AsyncCommand(CreateSourceAsync);
                return _createSourceCommand;
            }
        }

        private ICommand? _editSourceCommand;
        public ICommand EditSourceCommand
        {
            get
            {
                _editSourceCommand ??= new AsyncCommand(EditSourceAsync);
                return _editSourceCommand;
            }
        }

        private ICommand? _deleteSourceCommand;
        public ICommand DeleteSourceCommand
        {
            get
            {
                _deleteSourceCommand ??= new AsyncCommand(DeleteSourceAsync);
                return _deleteSourceCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadAccountingSourcesAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public AccountingSourceViewModel(
            IEventAggregator eventAggregator,
            IRepository<AccountingSourceGraphQLModel> accountingSourceService,
            Helpers.Services.INotificationService notificationService,
            IDialogService dialogService,
            AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache,
            ProcessTypeCache processTypeCache,
            MenuModuleCache menuModuleCache)
        {
            _eventAggregator = eventAggregator;
            _accountingSourceService = accountingSourceService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;
            _processTypeCache = processTypeCache;
            _menuModuleCache = menuModuleCache;
            _eventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            await _menuModuleCache.EnsureLoadedAsync();
            Modules = _menuModuleCache.Items;
            await LoadAccountingSourcesAsync();
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

        public async Task CreateSourceAsync()
        {
            try
            {
                IsBusy = true;
                var detail = new AccountingSourceDetailViewModel(_accountingSourceService, _eventAggregator, _auxiliaryAccountingAccountCache, _processTypeCache);
                await detail.InitializeAsync();
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Nueva fuente contable");
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{currentMethod!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditSourceAsync()
        {
            if (SelectedAccountingSource == null) return;
            try
            {
                IsBusy = true;
                var detail = new AccountingSourceDetailViewModel(_accountingSourceService, _eventAggregator, _auxiliaryAccountingAccountCache, _processTypeCache);
                await detail.InitializeAsync();
                await detail.LoadDataForEditAsync(SelectedAccountingSource.Id);
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Editar fuente contable");
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{currentMethod!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteSourceAsync()
        {
            if (SelectedAccountingSource == null) return;
            try
            {
                IsBusy = true;
                Refresh();

                var (canDeleteFragment, canDeleteQuery) = _canDeleteAccountingSourceQuery.Value;
                var canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedAccountingSource.Id)
                    .Build();
                var validation = await _accountingSourceService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    if (ThemedMessageBox.Show("Atención !", "¿Confirma que desea eliminar el registro seleccionado?",
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
                var (deleteFragment, deleteQuery) = _deleteAccountingSourceQuery.Value;
                var deleteVars = new GraphQLVariables()
                    .For(deleteFragment, "id", SelectedAccountingSource.Id)
                    .Build();
                DeleteResponseType deletedSource = await _accountingSourceService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!deletedSource.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro \n\n {deletedSource.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await _eventAggregator.PublishOnUIThreadAsync(new AccountingSourceDeleteMessage { DeletedAccountingSource = deletedSource });
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

        #region Load

        public async Task LoadAccountingSourcesAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = new();
                stopwatch.Start();

                var (fragment, query) = _loadAccountingSourcesQuery.Value;

                dynamic filters = new ExpandoObject();
                filters.annulment = false;
                if (!string.IsNullOrEmpty(FilterSearch)) filters.name = FilterSearch.Trim().RemoveExtraSpaces();
                if (SelectedModuleId.HasValue && SelectedModuleId.Value != 0) filters.menuModuleId = SelectedModuleId.Value;

                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<AccountingSourceGraphQLModel> result = await _accountingSourceService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                AccountingSources = [.. result.Entries];
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

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadAccountingSourcesQuery = new(() =>
        {
            var fields = FieldSpec<PageType<AccountingSourceGraphQLModel>>
               .Create()
               .Field(f => f.TotalEntries)
               .SelectList(f => f.Entries, entries => entries
                   .Field(e => e.Id)
                   .Field(e => e.Code)
                   .Field(e => e.Name)
                   .Field(e => e.IsSystemSource)
                   .Select(e => e.ProcessType, cat => cat
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                            .Select(c => c.MenuModule, dep => dep
                                .Field(d => d.Id)
                                .Field(d => d.Name))))
               .Build();

            var fragment = new GraphQLQueryFragment("accountingSourcesPage",
                [new("filters", "AccountingSourceFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteAccountingSourceQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteAccountingSource",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteAccountingSourceQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteAccountingSource",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(AccountingSourceCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingSourcesAsync();
            _notificationService.ShowSuccess(message.CreatedAccountingSource.Message);
        }

        public async Task HandleAsync(AccountingSourceUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingSourcesAsync();
            _notificationService.ShowSuccess(message.UpdatedAccountingSource.Message);
        }

        public async Task HandleAsync(AccountingSourceDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingSourcesAsync();
            SelectedAccountingSource = null;
            _notificationService.ShowSuccess(message.DeletedAccountingSource.Message);
        }

        #endregion
    }
}
