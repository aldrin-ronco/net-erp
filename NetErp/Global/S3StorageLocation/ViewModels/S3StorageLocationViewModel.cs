using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Global.S3StorageLocation.Validators;
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

namespace NetErp.Global.S3StorageLocation.ViewModels
{
    public class S3StorageLocationViewModel : Screen,
        IHandle<S3StorageLocationCreateMessage>,
        IHandle<S3StorageLocationDeleteMessage>,
        IHandle<S3StorageLocationUpdateMessage>,
        IHandle<PermissionsCacheRefreshedMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<S3StorageLocationGraphQLModel> _service;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly StringLengthCache _stringLengthCache;
        private readonly AwsS3ConfigCache _awsS3ConfigCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly PermissionCache _permissionCache;
        private readonly S3StorageLocationValidator _validator;

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
                    NotifyOfPropertyChange(nameof(CanCreateS3StorageLocation));
                }
            }
        }

        public ObservableCollection<S3StorageLocationGraphQLModel> S3StorageLocations
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(S3StorageLocations));
                }
            }
        } = [];

        public S3StorageLocationGraphQLModel? SelectedItem
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(CanEditS3StorageLocation));
                    NotifyOfPropertyChange(nameof(CanDeleteS3StorageLocation));
                }
            }
        }

        private readonly DebouncedAction _searchDebounce;

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
                        _ = _searchDebounce.RunAsync(LoadDataAsync);
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

        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.S3StorageLocation.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.S3StorageLocation.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.S3StorageLocation.Delete);

        #endregion

        #region Button States

        public bool CanCreateS3StorageLocation => HasCreatePermission && !IsBusy;
        public bool CanEditS3StorageLocation => HasEditPermission && SelectedItem != null;
        public bool CanDeleteS3StorageLocation => HasDeletePermission && SelectedItem != null;

        #endregion

        #region Commands

        private ICommand? _createCommand;
        public ICommand CreateS3StorageLocationCommand
        {
            get
            {
                _createCommand ??= new AsyncCommand(CreateAsync);
                return _createCommand;
            }
        }

        private ICommand? _editCommand;
        public ICommand EditS3StorageLocationCommand
        {
            get
            {
                _editCommand ??= new AsyncCommand(EditAsync);
                return _editCommand;
            }
        }

        private ICommand? _deleteCommand;
        public ICommand DeleteS3StorageLocationCommand
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
                _paginationCommand ??= new AsyncCommand(LoadDataAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public S3StorageLocationViewModel(
            IEventAggregator eventAggregator,
            IRepository<S3StorageLocationGraphQLModel> service,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            StringLengthCache stringLengthCache,
            AwsS3ConfigCache awsS3ConfigCache,
            JoinableTaskFactory joinableTaskFactory,
            PermissionCache permissionCache,
            S3StorageLocationValidator validator,
            DebouncedAction searchDebounce)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _awsS3ConfigCache = awsS3ConfigCache ?? throw new ArgumentNullException(nameof(awsS3ConfigCache));
            _joinableTaskFactory = joinableTaskFactory;
            _permissionCache = permissionCache;
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _searchDebounce = searchDebounce ?? throw new ArgumentNullException(nameof(searchDebounce));

            _eventAggregator.SubscribeOnUIThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.S3StorageLocation);
                await _awsS3ConfigCache.EnsureLoadedAsync();

                NotifyOfPropertyChange(nameof(HasCreatePermission));
                NotifyOfPropertyChange(nameof(HasEditPermission));
                NotifyOfPropertyChange(nameof(HasDeletePermission));
                NotifyOfPropertyChange(nameof(CanCreateS3StorageLocation));
                NotifyOfPropertyChange(nameof(CanEditS3StorageLocation));
                NotifyOfPropertyChange(nameof(CanDeleteS3StorageLocation));

                await LoadDataAsync();
                _isInitialized = true;
                ShowEmptyState = S3StorageLocations == null || S3StorageLocations.Count == 0;
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
                S3StorageLocations.Clear();
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
                S3StorageLocationDetailViewModel detail = new(
                    _service, _eventAggregator, _stringLengthCache, _awsS3ConfigCache, _joinableTaskFactory, _validator);
                detail.SetForNew();
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.40;
                    detail.DialogHeight = parentView.ActualHeight * 0.65;
                }

                await _dialogService.ShowDialogAsync(detail, "Nueva ubicación S3");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(CreateAsync)} \r\n{ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditAsync()
        {
            if (SelectedItem == null) return;
            try
            {
                IsBusy = true;
                S3StorageLocationDetailViewModel detail = new(
                    _service, _eventAggregator, _stringLengthCache, _awsS3ConfigCache, _joinableTaskFactory, _validator);
                detail.SetForEdit(SelectedItem);
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.40;
                    detail.DialogHeight = parentView.ActualHeight * 0.65;
                }

                await _dialogService.ShowDialogAsync(detail, "Editar ubicación S3");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(EditAsync)} \r\n{ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteAsync()
        {
            if (SelectedItem == null) return;
            try
            {
                IsBusy = true;

                var (canDeleteFragment, canDeleteQuery) = _canDeleteQuery.Value;
                ExpandoObject canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedItem.Id)
                    .Build();
                CanDeleteType validation = await _service.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                DeleteResponseType deletedRecord = await ExecuteDeleteAsync(SelectedItem.Id);

                if (!deletedRecord.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedRecord.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new S3StorageLocationDeleteMessage { DeletedS3StorageLocation = deletedRecord },
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(DeleteAsync)} \r\n{ex.GetErrorMessage()}",
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
                var (fragment, query) = _deleteQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "id", id)
                    .Build();
                return await _service.DeleteAsync<DeleteResponseType>(query, variables);
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        #endregion

        #region Load

        public async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadQuery.Value;

                dynamic filters = new ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.matching = FilterSearch.Trim().RemoveExtraSpaces();

                var variables = new GraphQLVariables()
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<S3StorageLocationGraphQLModel> result = await _service.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                S3StorageLocations = new ObservableCollection<S3StorageLocationGraphQLModel>(result.Entries);
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(LoadDataAsync)} \r\n{ex.GetErrorMessage()}",
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

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<S3StorageLocationGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    .Field(e => e.Key)
                    .Field(e => e.Bucket)
                    .Field(e => e.Directory)
                    .Select(e => e.AwsS3Config, aws => aws
                        .Field(a => a.Id)
                        .Field(a => a.Description)))
                .Build();

            var fragment = new GraphQLQueryFragment("s3StorageLocationsPage",
                [new("filters", "S3StorageLocationFilters")],
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

            var fragment = new GraphQLQueryFragment("canDeleteS3StorageLocation",
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

            var fragment = new GraphQLQueryFragment("deleteS3StorageLocation",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(S3StorageLocationCreateMessage message, CancellationToken cancellationToken)
        {
            ShowEmptyState = false;
            await LoadDataAsync();
            _notificationService.ShowSuccess(message.CreatedS3StorageLocation.Message);
        }

        public async Task HandleAsync(S3StorageLocationUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadDataAsync();
            _notificationService.ShowSuccess(message.UpdatedS3StorageLocation.Message);
        }

        public async Task HandleAsync(S3StorageLocationDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadDataAsync();
            ShowEmptyState = S3StorageLocations == null || S3StorageLocations.Count == 0;
            SelectedItem = null;
            _notificationService.ShowSuccess(message.DeletedS3StorageLocation.Message);
        }

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateS3StorageLocation));
            NotifyOfPropertyChange(nameof(CanEditS3StorageLocation));
            NotifyOfPropertyChange(nameof(CanDeleteS3StorageLocation));
            return Task.CompletedTask;
        }

        #endregion
    }
}
