using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Inventory;
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
using Models.Global;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Inventory.MeasurementUnits.ViewModels
{
    public class MeasurementUnitViewModel : Screen,
        IHandle<MeasurementUnitCreateMessage>,
        IHandle<MeasurementUnitUpdateMessage>,
        IHandle<MeasurementUnitDeleteMessage>,
        IHandle<PermissionsCacheRefreshedMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<MeasurementUnitGraphQLModel> _measurementUnitService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly StringLengthCache _stringLengthCache;
        private readonly PermissionCache _permissionCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;

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
                }
            }
        }

        public ObservableCollection<MeasurementUnitGraphQLModel> MeasurementUnits
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(MeasurementUnits));
                }
            }
        } = [];

        public MeasurementUnitGraphQLModel? SelectedMeasurementUnit
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedMeasurementUnit));
                    NotifyOfPropertyChange(nameof(CanEditMeasurementUnit));
                    NotifyOfPropertyChange(nameof(CanDeleteMeasurementUnit));
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
                        _ = _searchDebounce.RunAsync(LoadMeasurementUnitsAsync);
                    }
                }
            }
        } = string.Empty;

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

        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.MeasurementUnit.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.MeasurementUnit.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.MeasurementUnit.Delete);

        #endregion

        #region Button States

        public bool CanCreateMeasurementUnit => HasCreatePermission && !IsBusy;
        public bool CanEditMeasurementUnit => HasEditPermission && SelectedMeasurementUnit != null;
        public bool CanDeleteMeasurementUnit => HasDeletePermission && SelectedMeasurementUnit != null;

        #endregion

        #region Commands

        private ICommand? _createMeasurementUnitCommand;
        public ICommand CreateMeasurementUnitCommand
        {
            get
            {
                _createMeasurementUnitCommand ??= new AsyncCommand(CreateMeasurementUnitAsync);
                return _createMeasurementUnitCommand;
            }
        }

        private ICommand? _editMeasurementUnitCommand;
        public ICommand EditMeasurementUnitCommand
        {
            get
            {
                _editMeasurementUnitCommand ??= new AsyncCommand(EditMeasurementUnitAsync);
                return _editMeasurementUnitCommand;
            }
        }

        private ICommand? _deleteMeasurementUnitCommand;
        public ICommand DeleteMeasurementUnitCommand
        {
            get
            {
                _deleteMeasurementUnitCommand ??= new AsyncCommand(DeleteMeasurementUnitAsync);
                return _deleteMeasurementUnitCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadMeasurementUnitsAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public MeasurementUnitViewModel(
            IEventAggregator eventAggregator,
            IRepository<MeasurementUnitGraphQLModel> measurementUnitService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            StringLengthCache stringLengthCache,
            PermissionCache permissionCache,
            JoinableTaskFactory joinableTaskFactory)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _measurementUnitService = measurementUnitService ?? throw new ArgumentNullException(nameof(measurementUnitService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _permissionCache = permissionCache;
            _joinableTaskFactory = joinableTaskFactory;

            _eventAggregator.SubscribeOnUIThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                await Task.WhenAll(
                    _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.MeasurementUnit),
                    _permissionCache.EnsureLoadedAsync());
                NotifyOfPropertyChange(nameof(HasCreatePermission));
                NotifyOfPropertyChange(nameof(HasEditPermission));
                NotifyOfPropertyChange(nameof(HasDeletePermission));
                NotifyOfPropertyChange(nameof(CanCreateMeasurementUnit));
                NotifyOfPropertyChange(nameof(CanEditMeasurementUnit));
                NotifyOfPropertyChange(nameof(CanDeleteMeasurementUnit));
                await LoadMeasurementUnitsAsync();
                _isInitialized = true;
                ShowEmptyState = MeasurementUnits == null || MeasurementUnits.Count == 0;
                NotifyOfPropertyChange(nameof(HasRecords));
                this.SetFocus(() => FilterSearch);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al inicializar el módulo.\r\n{GetType().Name}.{nameof(OnViewReady)}: {ex.Message}",
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
                MeasurementUnits.Clear();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region CRUD Operations

        public async Task CreateMeasurementUnitAsync()
        {
            try
            {
                IsBusy = true;
                MeasurementUnitDetailViewModel detail = new(_measurementUnitService, _eventAggregator, _stringLengthCache, _joinableTaskFactory);
                detail.SetForNew();
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.40;
                    detail.DialogHeight = parentView.ActualHeight * 0.55;
                }

                await _dialogService.ShowDialogAsync(detail, "Nueva unidad de medida");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(CreateMeasurementUnitAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditMeasurementUnitAsync()
        {
            if (SelectedMeasurementUnit == null) return;
            try
            {
                IsBusy = true;
                MeasurementUnitDetailViewModel detail = new(_measurementUnitService, _eventAggregator, _stringLengthCache, _joinableTaskFactory);
                detail.SetForEdit(SelectedMeasurementUnit);
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.40;
                    detail.DialogHeight = parentView.ActualHeight * 0.55;
                }

                await _dialogService.ShowDialogAsync(detail, "Editar unidad de medida");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(EditMeasurementUnitAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteMeasurementUnitAsync()
        {
            if (SelectedMeasurementUnit == null) return;
            try
            {
                IsBusy = true;

                (GraphQLQueryFragment canDeleteFragment, string canDeleteQuery) = _canDeleteQuery.Value;
                dynamic canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedMeasurementUnit.Id)
                    .Build();
                CanDeleteType validation = await _measurementUnitService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                        "El registro no puede ser eliminado" + (char)13 + (char)13 + validation.Message,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                DeleteResponseType deletedMeasurementUnit = await ExecuteDeleteAsync(SelectedMeasurementUnit.Id);

                if (!deletedMeasurementUnit.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedMeasurementUnit.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new MeasurementUnitDeleteMessage { DeletedMeasurementUnit = deletedMeasurementUnit },
                    CancellationToken.None);
            }
            catch (AsyncException ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"Error al eliminar el registro.\r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(DeleteMeasurementUnitAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                (GraphQLQueryFragment fragment, string query) = _deleteQuery.Value;
                dynamic variables = new GraphQLVariables()
                    .For(fragment, "id", id)
                    .Build();
                return await _measurementUnitService.DeleteAsync<DeleteResponseType>(query, variables);
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        #endregion

        #region Load

        public async Task LoadMeasurementUnitsAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = Stopwatch.StartNew();

                (GraphQLQueryFragment fragment, string query) = _loadQuery.Value;

                dynamic filters = new ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.name = FilterSearch.Trim().RemoveExtraSpaces();

                dynamic variables = new GraphQLVariables()
                    .For(fragment, "filters", filters)
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .Build();

                PageType<MeasurementUnitGraphQLModel> result = await _measurementUnitService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                MeasurementUnits = new ObservableCollection<MeasurementUnitGraphQLModel>(result.Entries);
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(LoadMeasurementUnitsAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
            var fields = FieldSpec<PageType<MeasurementUnitGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Abbreviation)
                    .Field(e => e.Name)
                    .Field(e => e.Type)
                    .Field(e => e.DianCode))
                .Build();

            var fragment = new GraphQLQueryFragment("measurementUnitsPage",
                [new("filters", "MeasurementUnitFilters"), new("pagination", "Pagination")],
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

            var fragment = new GraphQLQueryFragment("canDeleteMeasurementUnit",
                [new("id", "ID!")], fields, "CanDeleteResponse");
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

            var fragment = new GraphQLQueryFragment("deleteMeasurementUnit",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(MeasurementUnitCreateMessage message, CancellationToken cancellationToken)
        {
            ShowEmptyState = false;
            await LoadMeasurementUnitsAsync();
            _notificationService.ShowSuccess(message.CreatedMeasurementUnit.Message);
        }

        public async Task HandleAsync(MeasurementUnitUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadMeasurementUnitsAsync();
            _notificationService.ShowSuccess(message.UpdatedMeasurementUnit.Message);
        }

        public async Task HandleAsync(MeasurementUnitDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadMeasurementUnitsAsync();
            ShowEmptyState = MeasurementUnits == null || MeasurementUnits.Count == 0;
            SelectedMeasurementUnit = null;
            _notificationService.ShowSuccess(message.DeletedMeasurementUnit.Message);
        }

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateMeasurementUnit));
            NotifyOfPropertyChange(nameof(CanEditMeasurementUnit));
            NotifyOfPropertyChange(nameof(CanDeleteMeasurementUnit));
            return Task.CompletedTask;
        }

        #endregion
    }
}
