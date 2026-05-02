using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Payroll;
using NetErp.Books.AccountingBooks.ViewModels;
using NetErp.Books.AccountingPeriods.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Payroll.JobPosition.ViewModels
{
    public class JobPositionViewModel : Screen,
        IHandle<JobPositionCreateMessage>,
        IHandle<JobPositionUpdateMessage>,
        IHandle<JobPositionDeleteMessage>   
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<JobPositionGraphQLModel> _jobPositionService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly PermissionCache _permissionCache;
        private readonly StringLengthCache _stringLengthCache;
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

        public ObservableCollection<JobPositionGraphQLModel> JobPositions
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(JobPositions));
                }
            }
        } = [];

        public JobPositionGraphQLModel? SelectedJobPosition
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedJobPosition));
                    NotifyOfPropertyChange(nameof(CanEditJobPosition));
                    NotifyOfPropertyChange(nameof(CanDeleteJobPosition));
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
                    if (string.IsNullOrEmpty(value) || value.Length >= 3)
                    {
                        PageIndex = 1;
                        _ = _searchDebounce.RunAsync(LoadJobPositionsAsync);
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
        
        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.JobPosition.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.JobPosition.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.JobPosition.Delete);
        
        #endregion

        #region Button States

        public bool CanCreateJobPosition => HasCreatePermission && !IsBusy;
        public bool CanEditJobPosition => HasEditPermission && SelectedJobPosition != null;
        public bool CanDeleteJobPosition => HasDeletePermission && SelectedJobPosition != null;

        #endregion

        #region Commands

        private ICommand? _createJobPositionCommand;
        public ICommand CreateJobPositionCommand
        {
            get
            {
                _createJobPositionCommand ??= new AsyncCommand(CreateJobPositionAsync);
                return _createJobPositionCommand;
            }
        }

        private ICommand? _editJobPositionCommand;
        public ICommand EditJobPositionCommand
        {
            get
            {
                _editJobPositionCommand ??= new AsyncCommand(EditJobPositionAsync);
                return _editJobPositionCommand;
            }
        }

        private ICommand? _deleteJobPositionCommand;
        public ICommand DeleteJobPositionCommand
        {
            get
            {
                _deleteJobPositionCommand ??= new AsyncCommand(DeleteJobPositionAsync);
                return _deleteJobPositionCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadJobPositionsAsync);
                return _paginationCommand;
            }
        }

        #endregion
        public JobPositionViewModel(IEventAggregator eventAggregator,
            IRepository<JobPositionGraphQLModel> jobPositionService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            JoinableTaskFactory joinableTaskFactory,
            PermissionCache permissionCache,
            StringLengthCache stringLengthCache)
        {
            _eventAggregator = eventAggregator;
            _jobPositionService = jobPositionService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _joinableTaskFactory = joinableTaskFactory;
            _permissionCache = permissionCache;
            _stringLengthCache = stringLengthCache;
            _eventAggregator.SubscribeOnPublishedThread(this);
        }
        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                IsBusy = true;
              await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.JobPosition);
                await LoadJobPositionsAsync();
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
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateJobPosition));
            NotifyOfPropertyChange(nameof(CanEditJobPosition));
            NotifyOfPropertyChange(nameof(CanDeleteJobPosition));
            this.SetFocus(() => FilterSearch);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                _eventAggregator.Unsubscribe(this);
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region CRUD Operations

        public async Task CreateJobPositionAsync()
        {
            try
            {
                IsBusy = true;
                JobPositionDetailViewModel detail = new(_jobPositionService, _eventAggregator, _joinableTaskFactory, _stringLengthCache);
                detail.SetForNew();
                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.40;
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Nuevo libro contable");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(CreateJobPositionAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditJobPositionAsync()
        {
            if (SelectedJobPosition == null) return;
            try
            {
                IsBusy = true;
                JobPositionDetailViewModel detail = new(_jobPositionService, _eventAggregator, _joinableTaskFactory, _stringLengthCache);
                detail.SetForEdit(SelectedJobPosition);
                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.40;
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Editar Puesto de Trabajo");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(EditJobPositionAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteJobPositionAsync()
        {
            if (SelectedJobPosition == null) return;
            try
            {
                IsBusy = true;

                var (canDeleteFragment, canDeleteQuery) = _canDeleteJobPositionQuery.Value;
                ExpandoObject canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedJobPosition.Id)
                    .Build();
                CanDeleteType validation = await _jobPositionService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                var (deleteFragment, deleteQuery) = _deleteJobPositionQuery.Value;
                ExpandoObject deleteVars = new GraphQLVariables()
                    .For(deleteFragment, "id", SelectedJobPosition.Id)
                    .Build();
                DeleteResponseType deletedJobPosition = await _jobPositionService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!deletedJobPosition.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedJobPosition.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnUIThreadAsync(new JobPositionDeleteMessage { DeletedJobPosition = deletedJobPosition });
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(DeleteJobPositionAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Load

        public async Task LoadJobPositionsAsync()
        {
            try
            {
                IsBusy = true;
                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadJobPositionsQuery.Value;

                dynamic filters = new ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.name = FilterSearch.Trim().RemoveExtraSpaces();

                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<JobPositionGraphQLModel> result = await _jobPositionService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                JobPositions = [.. result.Entries];
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(LoadJobPositionsAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task HandleAsync(JobPositionDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadJobPositionsAsync();
            _notificationService.ShowSuccess(message.DeletedJobPosition.Message);
        }

        public async Task HandleAsync(JobPositionUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadJobPositionsAsync();
            _notificationService.ShowSuccess(message.UpdatedJobPosition.Message);
        }

        public async Task HandleAsync(JobPositionCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadJobPositionsAsync();
            _notificationService.ShowSuccess(message.CreatedJobPosition.Message);
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadJobPositionsQuery = new(() =>
        {
            var fields = FieldSpec<PageType<JobPositionGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Description)
                    .Field(e => e.IsActive)
                    )
                .Build();

            var fragment = new GraphQLQueryFragment("jobPositionsPage",
                [new("filters", "JobPositionFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteJobPositionQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteJobPosition",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteJobPositionQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteJobPosition",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion

    }
}
