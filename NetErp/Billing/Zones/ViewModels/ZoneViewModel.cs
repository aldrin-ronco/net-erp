using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using Models.Global;
using NetErp.Billing.Zones.Validators;
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

namespace NetErp.Billing.Zones.ViewModels
{
    public class ZoneViewModel : Screen,
        IHandle<ZoneCreateMessage>,
        IHandle<ZoneDeleteMessage>,
        IHandle<ZoneUpdateMessage>,
        IHandle<PermissionsCacheRefreshedMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<ZoneGraphQLModel> _zoneService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly PermissionCache _permissionCache;
        private readonly ZoneValidator _validator;

        #endregion

        #region Grid Properties

        private bool _isInitialized;

        public bool HasRecords => _isInitialized && !ShowEmptyState;

        public bool ShowEmptyState
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ShowEmptyState));
                    NotifyOfPropertyChange(nameof(HasRecords));
                }
            }
        }

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    NotifyOfPropertyChange(nameof(CanCreateZone));
                }
            }
        }

        public ObservableCollection<ZoneGraphQLModel> Zones
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Zones));
                }
            }
        } = [];

        public ZoneGraphQLModel? SelectedZone
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedZone));
                    NotifyOfPropertyChange(nameof(CanEditZone));
                    NotifyOfPropertyChange(nameof(CanDeleteZone));
                }
            }
        }

        private readonly DebouncedAction _searchDebounce = new();

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
                        _ = _searchDebounce.RunAsync(LoadZonesAsync);
                    }
                }
            }
        } = string.Empty;

        public bool ShowActiveZonesOnly
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ShowActiveZonesOnly));
                    _ = LoadZonesAsync();
                }
            }
        } = true;

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

        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.Zone.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.Zone.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.Zone.Delete);

        #endregion

        #region Button States

        public bool CanCreateZone => HasCreatePermission && !IsBusy;
        public bool CanEditZone => HasEditPermission && SelectedZone != null;
        public bool CanDeleteZone => HasDeletePermission && SelectedZone != null;

        #endregion

        #region Commands

        private ICommand? _createZoneCommand;
        public ICommand CreateZoneCommand
        {
            get
            {
                _createZoneCommand ??= new AsyncCommand(CreateZoneAsync);
                return _createZoneCommand;
            }
        }

        private ICommand? _editZoneCommand;
        public ICommand EditZoneCommand
        {
            get
            {
                _editZoneCommand ??= new AsyncCommand(EditZoneAsync);
                return _editZoneCommand;
            }
        }

        private ICommand? _deleteZoneCommand;
        public ICommand DeleteZoneCommand
        {
            get
            {
                _deleteZoneCommand ??= new AsyncCommand(DeleteZoneAsync);
                return _deleteZoneCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadZonesAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public ZoneViewModel(
            IEventAggregator eventAggregator,
            IRepository<ZoneGraphQLModel> zoneService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            PermissionCache permissionCache,
            ZoneValidator validator)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _joinableTaskFactory = joinableTaskFactory;
            _permissionCache = permissionCache;
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));

            _eventAggregator.SubscribeOnUIThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.Zone);

                NotifyOfPropertyChange(nameof(HasCreatePermission));
                NotifyOfPropertyChange(nameof(HasEditPermission));
                NotifyOfPropertyChange(nameof(HasDeletePermission));
                NotifyOfPropertyChange(nameof(CanCreateZone));
                NotifyOfPropertyChange(nameof(CanEditZone));
                NotifyOfPropertyChange(nameof(CanDeleteZone));

                await LoadZonesAsync();
                _isInitialized = true;
                ShowEmptyState = Zones == null || Zones.Count == 0;
                NotifyOfPropertyChange(nameof(HasRecords));
                this.SetFocus(() => FilterSearch);
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
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                _eventAggregator.Unsubscribe(this);
                Zones.Clear();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region CRUD Operations

        public async Task CreateZoneAsync()
        {
            try
            {
                IsBusy = true;
                var detail = new ZoneDetailViewModel(_zoneService, _eventAggregator, _stringLengthCache, _joinableTaskFactory, _validator);
                detail.SetForNew();
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.40;
                    detail.DialogHeight = parentView.ActualHeight * 0.30;
                }

                await _dialogService.ShowDialogAsync(detail, "Nueva zona");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(CreateZoneAsync)} \r\n{ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditZoneAsync()
        {
            if (SelectedZone == null) return;
            try
            {
                IsBusy = true;
                var detail = new ZoneDetailViewModel(_zoneService, _eventAggregator, _stringLengthCache, _joinableTaskFactory, _validator);
                detail.SetForEdit(SelectedZone);
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.40;
                    detail.DialogHeight = parentView.ActualHeight * 0.30;
                }

                await _dialogService.ShowDialogAsync(detail, "Editar zona");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(EditZoneAsync)} \r\n{ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteZoneAsync()
        {
            if (SelectedZone == null) return;
            try
            {
                IsBusy = true;

                var (canDeleteFragment, canDeleteQuery) = _canDeleteZoneQuery.Value;
                var canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedZone.Id)
                    .Build();
                CanDeleteType validation = await _zoneService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                        "El registro no puede ser eliminado" + (char)13 + (char)13 + validation.Message,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                DeleteResponseType deletedZone = await ExecuteDeleteAsync(SelectedZone.Id);

                if (!deletedZone.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedZone.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new ZoneDeleteMessage { DeletedZone = deletedZone },
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(DeleteZoneAsync)} \r\n{ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<DeleteResponseType> ExecuteDeleteAsync(int id)
        {
            try
            {
                var (fragment, query) = _deleteZoneQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "id", id)
                    .Build();
                return await _zoneService.DeleteAsync<DeleteResponseType>(query, variables);
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        #endregion

        #region Load

        public async Task LoadZonesAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadZonesQuery.Value;

                dynamic filters = new ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.name = FilterSearch.Trim().RemoveExtraSpaces();
                if (ShowActiveZonesOnly) filters.isActive = true;

                var variables = new GraphQLVariables()
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<ZoneGraphQLModel> result = await _zoneService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                Zones = new ObservableCollection<ZoneGraphQLModel>(result.Entries);
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(LoadZonesAsync)} \r\n{ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadZonesQuery = new(() =>
        {
            var fields = FieldSpec<PageType<ZoneGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.IsActive))
                .Build();

            var fragment = new GraphQLQueryFragment("zonesPage",
                [new("filters", "ZoneFilters")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteZoneQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteZone",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteZoneQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteZone",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(ZoneCreateMessage message, CancellationToken cancellationToken)
        {
            ShowEmptyState = false;
            await LoadZonesAsync();
            _notificationService.ShowSuccess(message.CreatedZone.Message);
        }

        public async Task HandleAsync(ZoneUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadZonesAsync();
            _notificationService.ShowSuccess(message.UpdatedZone.Message);
        }

        public async Task HandleAsync(ZoneDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadZonesAsync();
            ShowEmptyState = Zones == null || Zones.Count == 0;
            SelectedZone = null;
            _notificationService.ShowSuccess(message.DeletedZone.Message);
        }

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateZone));
            NotifyOfPropertyChange(nameof(CanEditZone));
            NotifyOfPropertyChange(nameof(CanDeleteZone));
            return Task.CompletedTask;
        }

        #endregion
    }
}
