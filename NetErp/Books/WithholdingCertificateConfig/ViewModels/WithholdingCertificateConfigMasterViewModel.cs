using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Models.Books;
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
using Microsoft.VisualStudio.Threading;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.WithholdingCertificateConfig.ViewModels
{
    public class WithholdingCertificateConfigMasterViewModel : Screen,
        IHandle<WithholdingCertificateConfigDeleteMessage>,
        IHandle<WithholdingCertificateConfigUpdateMessage>,
        IHandle<WithholdingCertificateConfigCreateMessage>,
        IHandle<PermissionsCacheRefreshedMessage>,
        IHandle<CostCenterCreateMessage>,
        IHandle<CostCenterDeleteMessage>
    {
        #region Dependencies

        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly IRepository<WithholdingCertificateConfigGraphQLModel> _withholdingCertificateConfigService;
        private readonly IRepository<AccountingAccountGroupGraphQLModel> _accountingAccountGroupService;
        private readonly AccountingAccountGroupCache _accountingAccountGroupCache;
        private readonly CostCenterCache _costCenterCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly PermissionCache _permissionCache;
        private readonly DebouncedAction _searchDebounce = new();

        public WithholdingCertificateConfigViewModel Context { get; set; }

        #endregion

        #region Grid Properties

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

        public ObservableCollection<WithholdingCertificateConfigGraphQLModel> Certificates
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Certificates));
                }
            }
        } = [];

        public WithholdingCertificateConfigGraphQLModel? SelectedCertificate
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCertificate));
                    NotifyOfPropertyChange(nameof(CanEditCertificate));
                    NotifyOfPropertyChange(nameof(CanDeleteCertificate));
                }
            }
        }

        public string FilterSearch
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = _searchDebounce.RunAsync(LoadCertificatesAsync);
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

        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.WithholdingCertificate.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.WithholdingCertificate.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.WithholdingCertificate.Delete);

        #endregion

        #region Button States

        public bool CanCreateCertificate => HasCreatePermission && !IsBusy;
        public bool CanEditCertificate => HasEditPermission && SelectedCertificate != null;
        public bool CanDeleteCertificate => HasDeletePermission && SelectedCertificate != null;

        #endregion

        #region Commands

        private ICommand? _createCommand;
        public ICommand CreateCommand
        {
            get
            {
                _createCommand ??= new AsyncCommand(CreateAsync);
                return _createCommand;
            }
        }

        private ICommand? _editCommand;
        public ICommand EditCommand
        {
            get
            {
                _editCommand ??= new AsyncCommand(EditAsync);
                return _editCommand;
            }
        }

        private ICommand? _deleteCommand;
        public ICommand DeleteCommand
        {
            get
            {
                _deleteCommand ??= new AsyncCommand(DeleteAsync);
                return _deleteCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadCertificatesAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public WithholdingCertificateConfigMasterViewModel(
            WithholdingCertificateConfigViewModel context,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            IRepository<WithholdingCertificateConfigGraphQLModel> withholdingCertificateConfigService,
            IRepository<AccountingAccountGroupGraphQLModel> accountingAccountGroupService,
            AccountingAccountGroupCache accountingAccountGroupCache,
            CostCenterCache costCenterCache,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            PermissionCache permissionCache)
        {
            Context = context;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _withholdingCertificateConfigService = withholdingCertificateConfigService;
            _accountingAccountGroupService = accountingAccountGroupService;
            _accountingAccountGroupCache = accountingAccountGroupCache;
            _costCenterCache = costCenterCache;
            _stringLengthCache = stringLengthCache;
            _joinableTaskFactory = joinableTaskFactory;
            _permissionCache = permissionCache;
            Context.EventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
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
                return;
            }
            if (!HasUnmetDependencies)
            {
                NotifyOfPropertyChange(nameof(HasCreatePermission));
                NotifyOfPropertyChange(nameof(HasEditPermission));
                NotifyOfPropertyChange(nameof(HasDeletePermission));
                NotifyOfPropertyChange(nameof(CanCreateCertificate));
                NotifyOfPropertyChange(nameof(CanEditCertificate));
                NotifyOfPropertyChange(nameof(CanDeleteCertificate));
                this.SetFocus(() => FilterSearch);
            }
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                Context.EventAggregator.Unsubscribe(this);
                Certificates.Clear();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region CRUD Operations

        public async Task CreateAsync()
        {
            try
            {
                IsBusy = true;
                WithholdingCertificateConfigDetailViewModel detail = new(
                    Context, _withholdingCertificateConfigService,
                    _accountingAccountGroupService, _accountingAccountGroupCache,
                    _costCenterCache, _stringLengthCache, _joinableTaskFactory);
                await detail.InitializeAsync();
                detail.SetForNew();
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.75;
                    detail.DialogHeight = parentView.ActualHeight * 0.90;
                }

                await _dialogService.ShowDialogAsync(detail, "Nuevo certificado de retención");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(CreateAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditAsync()
        {
            if (SelectedCertificate == null) return;
            try
            {
                IsBusy = true;
                WithholdingCertificateConfigDetailViewModel detail = new(
                    Context, _withholdingCertificateConfigService,
                    _accountingAccountGroupService, _accountingAccountGroupCache,
                    _costCenterCache, _stringLengthCache, _joinableTaskFactory);
                await detail.LoadDataForEditAsync(SelectedCertificate.Id);
                await detail.InitializeAsync();
                detail.SetForEdit();
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.75;
                    detail.DialogHeight = parentView.ActualHeight * 0.90;
                }

                await _dialogService.ShowDialogAsync(detail, "Editar certificado de retención");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(EditAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteAsync()
        {
            if (SelectedCertificate == null) return;
            try
            {
                IsBusy = true;

                var (canDeleteFragment, canDeleteQuery) = _canDeleteQuery.Value;
                ExpandoObject canDeleteVariables = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedCertificate.Id)
                    .Build();
                CanDeleteType validation = await _withholdingCertificateConfigService.CanDeleteAsync(canDeleteQuery, canDeleteVariables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    if (ThemedMessageBox.Show("Atención!",
                        "¿Confirma que desea eliminar el registro seleccionado?",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                }
                else
                {
                    IsBusy = false;
                    ThemedMessageBox.Show("Atención!",
                        $"El registro no puede ser eliminado\r\n\r\n{validation.Message}",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                var (deleteFragment, deleteQuery) = _deleteQuery.Value;
                ExpandoObject deleteVariables = new GraphQLVariables()
                    .For(deleteFragment, "id", SelectedCertificate.Id)
                    .Build();
                DeleteResponseType deletedCertificate = await _withholdingCertificateConfigService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVariables);

                if (!deletedCertificate.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedCertificate.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    new WithholdingCertificateConfigDeleteMessage { DeletedWithholdingCertificateConfig = deletedCertificate },
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(DeleteAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Load

        public async Task LoadCertificatesAsync()
        {
            try
            {
                IsBusy = true;
                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadQuery.Value;
                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "filters", new
                    {
                        name = string.IsNullOrEmpty(FilterSearch)
                            ? ""
                            : FilterSearch.Trim().RemoveExtraSpaces()
                    })
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .Build();

                PageType<WithholdingCertificateConfigGraphQLModel> result = await _withholdingCertificateConfigService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                Certificates = [.. result.Entries];
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(LoadCertificatesAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
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
            await LoadCertificatesAsync();
            _isInitialized = true;
            ShowEmptyState = Certificates == null || Certificates.Count == 0;
            NotifyOfPropertyChange(nameof(HasRecords));
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<WithholdingCertificateConfigGraphQLModel>>
                .Create()
                .Field(o => o.TotalEntries)
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Description))
                .Build();

            var fragment = new GraphQLQueryFragment("withholdingCertificatesPage",
                [new("filters", "WithholdingCertificateFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteWithholdingCertificate",
                [new("id", "ID!")],
                fields, "CanDeleteResponse");
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

            var fragment = new GraphQLQueryFragment("deleteWithholdingCertificate",
                [new("id", "ID!")],
                fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(WithholdingCertificateConfigCreateMessage message, CancellationToken cancellationToken)
        {
            ShowEmptyState = false;
            await LoadCertificatesAsync();
            _notificationService.ShowSuccess(message.CreatedWithholdingCertificateConfig.Message);
        }

        public async Task HandleAsync(WithholdingCertificateConfigUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadCertificatesAsync();
            _notificationService.ShowSuccess(message.UpdatedWithholdingCertificateConfig.Message);
        }

        public async Task HandleAsync(WithholdingCertificateConfigDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadCertificatesAsync();
            ShowEmptyState = Certificates == null || Certificates.Count == 0;
            SelectedCertificate = null;
            _notificationService.ShowSuccess(message.DeletedWithholdingCertificateConfig.Message);
        }

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateCertificate));
            NotifyOfPropertyChange(nameof(CanEditCertificate));
            NotifyOfPropertyChange(nameof(CanDeleteCertificate));
            return Task.CompletedTask;
        }

        public async Task HandleAsync(CostCenterCreateMessage message, CancellationToken cancellationToken)
        {
            if (!HasUnmetDependencies) return;

            // Ceder al dispatcher para que el cache procese el mensaje primero.
            // JoinableTaskFactory.SwitchToMainThreadAsync() no soporta prioridades.
            #pragma warning disable VSTHRD001
            await Application.Current.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle, cancellationToken);
            #pragma warning restore VSTHRD001

            EvaluateDependencies();
            if (!HasUnmetDependencies)
                await PerformInitialLoadAsync();
        }

        public async Task HandleAsync(CostCenterDeleteMessage message, CancellationToken cancellationToken)
        {
            if (HasUnmetDependencies) return;

            #pragma warning disable VSTHRD001
            await Application.Current.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle, cancellationToken);
            #pragma warning restore VSTHRD001

            EvaluateDependencies();
        }

        #endregion
    }
}
