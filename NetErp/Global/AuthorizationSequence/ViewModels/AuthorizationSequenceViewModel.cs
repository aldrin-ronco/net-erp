using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.AuthorizationSequence.ViewModels
{
    public class AuthorizationSequenceViewModel : Screen,
        IHandle<AuthorizationSequenceCreateMessage>,
        IHandle<AuthorizationSequenceUpdateMessage>,
        IHandle<AuthorizationSequenceDeleteMessage>,
        IHandle<PermissionsCacheRefreshedMessage>,
        IHandle<CostCenterCreateMessage>,
        IHandle<CostCenterDeleteMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<AuthorizationSequenceGraphQLModel> _authorizationSequenceService;
        private readonly IRepository<DianSoftwareConfigGraphQLModel> _dianConfigService;
        private readonly IRepository<DianCertificateGraphQLModel> _dianCertService;
        private readonly Helpers.Dian.IDianSoapClient _dianSoapClient;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly CostCenterCache _costCenterCache;
        private readonly AuthorizationSequenceTypeCache _authorizationSequenceTypeCache;
        private readonly PermissionCache _permissionCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly DebouncedAction _searchDebounce = new();

        #endregion

        #region Grid Properties

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        public ObservableCollection<AuthorizationSequenceGraphQLModel> Authorizations
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Authorizations));
                }
            }
        } = [];

        public AuthorizationSequenceGraphQLModel? SelectedAuthorizationSequence
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedAuthorizationSequence));
                    NotifyOfPropertyChange(nameof(CanEditAuthorizationSequence));
                    NotifyOfPropertyChange(nameof(CanDeleteAuthorizationSequence));
                }
            }
        }

        public ObservableCollection<CostCenterGraphQLModel> CostCenters
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        } = [];

        public CostCenterGraphQLModel? SelectedCostCenter
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenter));
                    if (_isInitialized)
                    {
                        PageIndex = 1;
                        _ = LoadAuthorizationSequencesAsync();
                    }
                }
            }
        }

        public bool IsActiveFilter
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsActiveFilter));
                    if (_isInitialized)
                    {
                        PageIndex = 1;
                        _ = LoadAuthorizationSequencesAsync();
                    }
                }
            }
        } = true;

        public string FilterSearch
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 3)
                    {
                        PageIndex = 1;
                        _ = _searchDebounce.RunAsync(LoadAuthorizationSequencesAsync);
                    }
                }
            }
        } = string.Empty;

        #endregion

        #region Pagination

        public int PageIndex
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PageIndex));
                }
            }
        } = 1;

        public int PageSize
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PageSize));
                }
            }
        } = 50;

        public int TotalCount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(TotalCount));
                }
            }
        }

        public string ResponseTime
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ResponseTime));
                }
            }
        } = string.Empty;

        #endregion

        #region Permissions

        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.AuthorizationSequence.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.AuthorizationSequence.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.AuthorizationSequence.Delete);

        #endregion

        #region Button States

        public bool CanCreateAuthorizationSequence => HasCreatePermission && !IsBusy;
        public bool CanEditAuthorizationSequence => HasEditPermission && SelectedAuthorizationSequence != null;
        public bool CanDeleteAuthorizationSequence => HasDeletePermission && SelectedAuthorizationSequence != null;

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

        public bool HasRecords => _isInitialized && !ShowEmptyState && !HasUnmetDependencies;

        public bool CanShowEmptyState => ShowEmptyState && !HasUnmetDependencies;

        public bool ShowEmptyState
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ShowEmptyState));
                    NotifyOfPropertyChange(nameof(CanShowEmptyState));
                    NotifyOfPropertyChange(nameof(HasRecords));
                }
            }
        }

        private List<DependencyItem>? _dependencies;
        public List<DependencyItem>? Dependencies
        {
            get => _dependencies;
            private set
            {
                _dependencies = value;
                NotifyOfPropertyChange(nameof(Dependencies));
                NotifyOfPropertyChange(nameof(HasUnmetDependencies));
                NotifyOfPropertyChange(nameof(CanShowEmptyState));
                NotifyOfPropertyChange(nameof(HasRecords));
            }
        }

        public bool HasUnmetDependencies => Dependencies?.Any(d => !d.IsMet) == true;

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
            AuthorizationSequenceTypeCache authorizationSequenceTypeCache,
            PermissionCache permissionCache,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            Helpers.Dian.IDianSoapClient dianSoapClient)
        {
            _eventAggregator = eventAggregator;
            _authorizationSequenceService = authorizationSequenceService;
            _dianConfigService = dianConfigService;
            _dianCertService = dianCertService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _costCenterCache = costCenterCache;
            _authorizationSequenceTypeCache = authorizationSequenceTypeCache;
            _permissionCache = permissionCache;
            _stringLengthCache = stringLengthCache;
            _joinableTaskFactory = joinableTaskFactory;
            _dianSoapClient = dianSoapClient;
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
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.AuthorizationSequence);
                await _costCenterCache.EnsureLoadedAsync();

                EvaluateDependencies();
                if (HasUnmetDependencies)
                {
                    _isInitialized = true;
                    NotifyOfPropertyChange(nameof(HasRecords));
                    return;
                }

                await PerformInitialLoadAsync();
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al inicializar el módulo.\r\n{GetType().Name}.{nameof(OnViewReady)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
                await TryCloseAsync();
            }
            finally
            {
                IsBusy = false;
            }
            if (!HasUnmetDependencies)
            {
                NotifyOfPropertyChange(nameof(HasCreatePermission));
                NotifyOfPropertyChange(nameof(HasEditPermission));
                NotifyOfPropertyChange(nameof(HasDeletePermission));
                NotifyOfPropertyChange(nameof(CanCreateAuthorizationSequence));
                NotifyOfPropertyChange(nameof(CanEditAuthorizationSequence));
                NotifyOfPropertyChange(nameof(CanDeleteAuthorizationSequence));
                this.SetFocus(() => FilterSearch);
            }
        }

        protected override async Task OnActivatedAsync(CancellationToken cancellationToken)
        {
            if (_isInitialized && HasUnmetDependencies)
            {
                EvaluateDependencies();
                if (!HasUnmetDependencies)
                    await PerformInitialLoadAsync();
            }
            await base.OnActivatedAsync(cancellationToken);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                _eventAggregator.Unsubscribe(this);
                Authorizations.Clear();
                CostCenters.Clear();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region CRUD Operations

        public async Task CreateAuthorizationSequenceAsync()
        {
            try
            {
                IsBusy = true;
                AuthorizationSequenceDetailViewModel detail = new(
                    _authorizationSequenceService,
                    _dianConfigService,
                    _dianCertService,
                    _eventAggregator,
                    _costCenterCache,
                    _authorizationSequenceTypeCache,
                    _stringLengthCache,
                    _joinableTaskFactory,
                    _dianSoapClient);
                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.65;
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Nueva autorización de numeración");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(CreateAuthorizationSequenceAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
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

                AuthorizationSequenceDetailViewModel detail = new(
                    _authorizationSequenceService,
                    _dianConfigService,
                    _dianCertService,
                    _eventAggregator,
                    _costCenterCache,
                    _authorizationSequenceTypeCache,
                    _stringLengthCache,
                    _joinableTaskFactory,
                    _dianSoapClient);

                await detail.LoadDataForEditAsync(SelectedAuthorizationSequence.Id);
                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.65;
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Editar autorización de numeración");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(EditAuthorizationSequenceAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
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
                ExpandoObject canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedAuthorizationSequence.Id)
                    .Build();
                CanDeleteType validation = await _authorizationSequenceService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                DeleteResponseType deletedRecord = await ExecuteDeleteAsync(SelectedAuthorizationSequence.Id);

                if (!deletedRecord.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro \n\n {deletedRecord.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await _eventAggregator.PublishOnUIThreadAsync(new AuthorizationSequenceDeleteMessage { DeletedAuthorizationSequence = deletedRecord });
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(DeleteAuthorizationSequenceAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<DeleteResponseType> ExecuteDeleteAsync(int id)
        {
            var (fragment, query) = _deleteQuery.Value;
            ExpandoObject variables = new GraphQLVariables()
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
                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadQuery.Value;

                dynamic filters = new ExpandoObject();
                if (IsActiveFilter) filters.isActive = true;
                if (SelectedCostCenter != null && SelectedCostCenter.Id > 0)
                    filters.costCenterId = SelectedCostCenter.Id;
                if (!string.IsNullOrEmpty(FilterSearch))
                    filters.matching = FilterSearch.Trim().RemoveExtraSpaces();

                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<AuthorizationSequenceGraphQLModel> result = await _authorizationSequenceService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                Authorizations = [.. result.Entries];
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(LoadAuthorizationSequencesAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Dependencies

        private void EvaluateDependencies()
        {
            Dependencies =
            [
                DependencyDefinitions.CostCenters(_costCenterCache),
            ];
        }

        private async Task PerformInitialLoadAsync()
        {
            CostCenters = [.. _costCenterCache.Items];
            await LoadAuthorizationSequencesAsync();
            _isInitialized = true;
            ShowEmptyState = Authorizations == null || Authorizations.Count == 0;
            NotifyOfPropertyChange(nameof(HasRecords));
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
                    .Field(e => e.Mode)
                    .Select(e => e.CostCenter, cat => cat
                        .Field(c => c!.Id)
                        .Field(c => c!.Name))
                    .Select(e => e.AuthorizationSequenceType, cat => cat
                        .Field(c => c!.Id)
                        .Field(c => c!.Name)))
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
            ShowEmptyState = false;
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
            ShowEmptyState = Authorizations == null || Authorizations.Count == 0;
            SelectedAuthorizationSequence = null;
            _notificationService.ShowSuccess(message.DeletedAuthorizationSequence.Message);
        }

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateAuthorizationSequence));
            NotifyOfPropertyChange(nameof(CanEditAuthorizationSequence));
            NotifyOfPropertyChange(nameof(CanDeleteAuthorizationSequence));
            return Task.CompletedTask;
        }

        public async Task HandleAsync(CostCenterCreateMessage message, CancellationToken cancellationToken)
        {
            if (!HasUnmetDependencies) return;

            // Ceder al dispatcher para que el cache procese el mensaje primero.
            // ContextIdle (prioridad 3) se ejecuta después de que todas las
            // operaciones de mayor prioridad (incluido el handler del cache) completen.
            // JoinableTaskFactory.SwitchToMainThreadAsync() no soporta prioridades.
#pragma warning disable VSTHRD001
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                () => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
#pragma warning restore VSTHRD001

            EvaluateDependencies();
            if (!HasUnmetDependencies)
                await PerformInitialLoadAsync();
        }

        public async Task HandleAsync(CostCenterDeleteMessage message, CancellationToken cancellationToken)
        {
            if (HasUnmetDependencies) return;

#pragma warning disable VSTHRD001
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                () => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
#pragma warning restore VSTHRD001

            EvaluateDependencies();
        }

        #endregion
    }
}
