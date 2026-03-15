using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Global;
using NetErp.Helpers;
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

namespace NetErp.Global.AuthorizationSequence.ViewModels
{
    public class AuthorizationSequenceViewModel : Screen,
        IHandle<AuthorizationSequenceCreateMessage>,
        IHandle<AuthorizationSequenceUpdateMessage>,
        IHandle<AuthorizationSequenceDeleteMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<AuthorizationSequenceGraphQLModel> _authorizationSequenceService;
        private readonly IRepository<DianSoftwareConfigGraphQLModel> _dianConfigService;
        private readonly IRepository<DianCertificateGraphQLModel> _dianCertService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly CostCenterCache _costCenterCache;
        private readonly AuthorizationSequenceTypeCache _authorizationSequenceTypeCache;

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

        private ObservableCollection<AuthorizationSequenceGraphQLModel> _authorizations = [];
        public ObservableCollection<AuthorizationSequenceGraphQLModel> Authorizations
        {
            get => _authorizations;
            set
            {
                if (_authorizations != value)
                {
                    _authorizations = value;
                    NotifyOfPropertyChange(nameof(Authorizations));
                }
            }
        }

        private AuthorizationSequenceGraphQLModel? _selectedAuthorizationSequence;
        public AuthorizationSequenceGraphQLModel? SelectedAuthorizationSequence
        {
            get => _selectedAuthorizationSequence;
            set
            {
                if (_selectedAuthorizationSequence != value)
                {
                    _selectedAuthorizationSequence = value;
                    NotifyOfPropertyChange(nameof(SelectedAuthorizationSequence));
                    NotifyOfPropertyChange(nameof(CanEditAuthorizationSequence));
                    NotifyOfPropertyChange(nameof(CanDeleteAuthorizationSequence));
                }
            }
        }

        private ObservableCollection<CostCenterGraphQLModel> _costCenters = [];
        public ObservableCollection<CostCenterGraphQLModel> CostCenters
        {
            get => _costCenters;
            set
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        }

        private CostCenterGraphQLModel? _selectedCostCenter;
        public CostCenterGraphQLModel? SelectedCostCenter
        {
            get => _selectedCostCenter;
            set
            {
                if (_selectedCostCenter != value)
                {
                    _selectedCostCenter = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenter));
                    if (_isInitialized)
                    {
                        PageIndex = 1;
                        _ = LoadAuthorizationSequencesAsync();
                    }
                }
            }
        }

        private bool _isActiveFilter = true;
        public bool IsActiveFilter
        {
            get => _isActiveFilter;
            set
            {
                if (_isActiveFilter != value)
                {
                    _isActiveFilter = value;
                    NotifyOfPropertyChange(nameof(IsActiveFilter));
                    if (_isInitialized)
                    {
                        PageIndex = 1;
                        _ = LoadAuthorizationSequencesAsync();
                    }
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
                    if (string.IsNullOrEmpty(value) || value.Length >= 3)
                    {
                        PageIndex = 1;
                        _ = LoadAuthorizationSequencesAsync();
                    }
                }
            }
        }

        #endregion

        #region Pagination

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

        public bool CanEditAuthorizationSequence => SelectedAuthorizationSequence != null;
        public bool CanDeleteAuthorizationSequence => SelectedAuthorizationSequence != null;

        #endregion

        #region Commands

        private ICommand? _createCommand;
        public ICommand CreateCommand
        {
            get
            {
                _createCommand ??= new AsyncCommand(CreateAuthorizationSequenceAsync);
                return _createCommand;
            }
        }

        private ICommand? _editCommand;
        public ICommand EditCommand
        {
            get
            {
                _editCommand ??= new AsyncCommand(EditAuthorizationSequenceAsync);
                return _editCommand;
            }
        }

        private ICommand? _deleteCommand;
        public ICommand DeleteCommand
        {
            get
            {
                _deleteCommand ??= new AsyncCommand(DeleteAuthorizationSequenceAsync);
                return _deleteCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadAuthorizationSequencesAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region State

        private bool _isInitialized;

        #endregion

        #region Constructor

        public AuthorizationSequenceViewModel(
            IEventAggregator eventAggregator,
            IRepository<AuthorizationSequenceGraphQLModel> authorizationSequenceService,
            IRepository<DianSoftwareConfigGraphQLModel> dianConfigService,
            IRepository<DianCertificateGraphQLModel> dianCertService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            CostCenterCache costCenterCache,
            AuthorizationSequenceTypeCache authorizationSequenceTypeCache)
        {
            _eventAggregator = eventAggregator;
            _authorizationSequenceService = authorizationSequenceService;
            _dianConfigService = dianConfigService;
            _dianCertService = dianCertService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _costCenterCache = costCenterCache;
            _authorizationSequenceTypeCache = authorizationSequenceTypeCache;
            _eventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                IsBusy = true;
                await _costCenterCache.EnsureLoadedAsync();
                CostCenters = [.. _costCenterCache.Items];
                await LoadAuthorizationSequencesAsync();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"{GetType().Name}.OnViewReady: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
            this.SetFocus(() => FilterSearch);
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

        public async Task CreateAuthorizationSequenceAsync()
        {
            try
            {
                IsBusy = true;
                var detail = new AuthorizationSequenceDetailViewModel(
                    _authorizationSequenceService,
                    _dianConfigService,
                    _dianCertService,
                    _eventAggregator,
                    _costCenterCache,
                    _authorizationSequenceTypeCache);
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Nueva autorización de numeración");
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

        public async Task EditAuthorizationSequenceAsync()
        {
            if (SelectedAuthorizationSequence == null) return;
            try
            {
                IsBusy = true;

                var detail = new AuthorizationSequenceDetailViewModel(
                    _authorizationSequenceService,
                    _dianConfigService,
                    _dianCertService,
                    _eventAggregator,
                    _costCenterCache,
                    _authorizationSequenceTypeCache);

                await detail.LoadDataForEditAsync(SelectedAuthorizationSequence.Id);
                await _dialogService.ShowDialogAsync(detail, "Editar autorización de numeración");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error al cargar la autorización: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteAuthorizationSequenceAsync()
        {
            if (SelectedAuthorizationSequence == null) return;
            try
            {
                IsBusy = true;

                var (canDeleteFragment, canDeleteQuery) = _canDeleteQuery.Value;
                var canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedAuthorizationSequence.Id)
                    .Build();
                var validation = await _authorizationSequenceService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                        "El registro no puede ser eliminado\r\n\r\n" + validation.Message,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                DeleteResponseType deletedRecord = await Task.Run(() => ExecuteDeleteAsync(SelectedAuthorizationSequence.Id));

                if (!deletedRecord.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro \n\n {deletedRecord.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await _eventAggregator.PublishOnUIThreadAsync(new AuthorizationSequenceDeleteMessage { DeletedAuthorizationSequence = deletedRecord });
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

        private async Task<DeleteResponseType> ExecuteDeleteAsync(int id)
        {
            var (fragment, query) = _deleteQuery.Value;
            var variables = new GraphQLVariables()
                .For(fragment, "id", id)
                .Build();
            return await _authorizationSequenceService.DeleteAsync<DeleteResponseType>(query, variables);
        }

        #endregion

        #region Load

        public async Task LoadAuthorizationSequencesAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = new();
                stopwatch.Start();

                var (fragment, query) = _loadQuery.Value;

                dynamic filters = new ExpandoObject();
                if (IsActiveFilter) filters.isActive = true;
                if (SelectedCostCenter != null && SelectedCostCenter.Id > 0)
                    filters.costCenterId = SelectedCostCenter.Id;
                if (!string.IsNullOrEmpty(FilterSearch))
                    filters.matching = FilterSearch.Trim().RemoveExtraSpaces();

                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<AuthorizationSequenceGraphQLModel> result = await _authorizationSequenceService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                Authorizations = [.. result.Entries];
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

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<AuthorizationSequenceGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    .Field(e => e.IsActive)
                    .Field(e => e.CurrentInvoiceNumber)
                    .Select(e => e.CostCenter, cat => cat
                        .Field(c => c.Id)
                        .Field(c => c.Name))
                    .Select(e => e.AuthorizationSequenceType, cat => cat
                        .Field(c => c.Id)
                        .Field(c => c.Name)))
                .Build();

            var fragment = new GraphQLQueryFragment("authorizationSequencesPage",
                [new("filters", "AuthorizationSequenceFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteAuthorizationSequence",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteAuthorizationSequence",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(AuthorizationSequenceCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadAuthorizationSequencesAsync();
            _notificationService.ShowSuccess(message.CreatedAuthorizationSequence.Message);
        }

        public async Task HandleAsync(AuthorizationSequenceUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadAuthorizationSequencesAsync();
            _notificationService.ShowSuccess(message.UpdatedAuthorizationSequence.Message);
        }

        public async Task HandleAsync(AuthorizationSequenceDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadAuthorizationSequencesAsync();
            SelectedAuthorizationSequence = null;
            _notificationService.ShowSuccess(message.DeletedAuthorizationSequence.Message);
        }

        #endregion
    }
}
