using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
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

namespace NetErp.Global.Smtp.ViewModels
{
    public class SmtpViewModel : Screen,
        IHandle<SmtpCreateMessage>,
        IHandle<SmtpUpdateMessage>,
        IHandle<SmtpDeleteMessage>,
        IHandle<PermissionsCacheRefreshedMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<SmtpGraphQLModel> _smtpService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly PermissionCache _permissionCache;

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
                    NotifyOfPropertyChange(nameof(CanCreateSmtp));
                }
            }
        }

        public ObservableCollection<SmtpGraphQLModel> Smtps
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Smtps));
                }
            }
        } = [];

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

        public SmtpGraphQLModel? SelectedSmtp
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedSmtp));
                    NotifyOfPropertyChange(nameof(CanEditSmtp));
                    NotifyOfPropertyChange(nameof(CanDeleteSmtp));
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
                        _ = _searchDebounce.RunAsync(LoadSmtpsAsync);
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

        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.Smtp.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.Smtp.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.Smtp.Delete);

        #endregion

        #region Button States

        public bool CanCreateSmtp => HasCreatePermission && !IsBusy;
        public bool CanEditSmtp => HasEditPermission && SelectedSmtp != null;
        public bool CanDeleteSmtp => HasDeletePermission && SelectedSmtp != null;

        #endregion

        #region Commands

        private ICommand? _createSmtpCommand;
        public ICommand CreateSmtpCommand
        {
            get
            {
                _createSmtpCommand ??= new AsyncCommand(CreateSmtpAsync);
                return _createSmtpCommand;
            }
        }

        private ICommand? _editSmtpCommand;
        public ICommand EditSmtpCommand
        {
            get
            {
                _editSmtpCommand ??= new AsyncCommand(EditSmtpAsync);
                return _editSmtpCommand;
            }
        }

        private ICommand? _deleteSmtpCommand;
        public ICommand DeleteSmtpCommand
        {
            get
            {
                _deleteSmtpCommand ??= new AsyncCommand(DeleteSmtpAsync);
                return _deleteSmtpCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadSmtpsAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public SmtpViewModel(
            IEventAggregator eventAggregator,
            IRepository<SmtpGraphQLModel> smtpService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            PermissionCache permissionCache)
        {
            _eventAggregator = eventAggregator;
            _smtpService = smtpService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _stringLengthCache = stringLengthCache;
            _joinableTaskFactory = joinableTaskFactory;
            _permissionCache = permissionCache;
            _eventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.Smtp);

                NotifyOfPropertyChange(nameof(HasCreatePermission));
                NotifyOfPropertyChange(nameof(HasEditPermission));
                NotifyOfPropertyChange(nameof(HasDeletePermission));
                NotifyOfPropertyChange(nameof(CanCreateSmtp));
                NotifyOfPropertyChange(nameof(CanEditSmtp));
                NotifyOfPropertyChange(nameof(CanDeleteSmtp));

                await LoadSmtpsAsync();
                _isInitialized = true;
                ShowEmptyState = Smtps == null || Smtps.Count == 0;
                NotifyOfPropertyChange(nameof(HasRecords));
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
                Smtps.Clear();
                SelectedSmtp = null;
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region CRUD Operations

        public async Task CreateSmtpAsync()
        {
            try
            {
                IsBusy = true;
                SmtpDetailViewModel detail = new(_smtpService, _eventAggregator, _stringLengthCache, _joinableTaskFactory);
                detail.SetForNew();
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.50;

                await _dialogService.ShowDialogAsync(detail, "Nuevo SMTP");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(CreateSmtpAsync)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditSmtpAsync()
        {
            if (SelectedSmtp == null) return;
            try
            {
                IsBusy = true;
                SmtpDetailViewModel detail = new(_smtpService, _eventAggregator, _stringLengthCache, _joinableTaskFactory);
                detail.SetForEdit(SelectedSmtp);
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.50;

                await _dialogService.ShowDialogAsync(detail, "Editar SMTP");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(EditSmtpAsync)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteSmtpAsync()
        {
            if (SelectedSmtp == null) return;
            try
            {
                IsBusy = true;

                var (canDeleteFragment, canDeleteQuery) = _canDeleteSmtpQuery.Value;
                ExpandoObject canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedSmtp.Id)
                    .Build();
                CanDeleteType validation = await _smtpService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                DeleteResponseType deletedSmtp = await ExecuteDeleteAsync(SelectedSmtp.Id);

                if (!deletedSmtp.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedSmtp.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new SmtpDeleteMessage { DeletedSmtp = deletedSmtp },
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(DeleteSmtpAsync)}: {ex.GetErrorMessage()}",
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
                var (fragment, query) = _deleteSmtpQuery.Value;
                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "id", id)
                    .Build();
                return await _smtpService.DeleteAsync<DeleteResponseType>(query, variables);
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        #endregion

        #region Load

        public async Task LoadSmtpsAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadSmtpsQuery.Value;

                dynamic filters = new ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.name = FilterSearch.Trim().RemoveExtraSpaces();

                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<SmtpGraphQLModel> result = await _smtpService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                Smtps = new ObservableCollection<SmtpGraphQLModel>(result.Entries);
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(LoadSmtpsAsync)}: {ex.GetErrorMessage()}",
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

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadSmtpsQuery = new(() =>
        {
            var fields = FieldSpec<PageType<SmtpGraphQLModel>>
                .Create()
                .Field(it => it.TotalEntries)
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Host)
                    .Field(e => e.Name)
                    .Field(e => e.Port))
                .Build();

            var fragment = new GraphQLQueryFragment("smtpsPage",
                [new("filters", "SmtpFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteSmtpQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteSmtp",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteSmtpQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteSmtp",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(SmtpCreateMessage message, CancellationToken cancellationToken)
        {
            ShowEmptyState = false;
            await LoadSmtpsAsync();
            _notificationService.ShowSuccess(message.CreatedSmtp.Message);
        }

        public async Task HandleAsync(SmtpUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadSmtpsAsync();
            _notificationService.ShowSuccess(message.UpdatedSmtp.Message);
        }

        public async Task HandleAsync(SmtpDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadSmtpsAsync();
            ShowEmptyState = Smtps == null || Smtps.Count == 0;
            SelectedSmtp = null;
            _notificationService.ShowSuccess(message.DeletedSmtp.Message);
        }

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateSmtp));
            NotifyOfPropertyChange(nameof(CanEditSmtp));
            NotifyOfPropertyChange(nameof(CanDeleteSmtp));
            return Task.CompletedTask;
        }

        #endregion
    }
}
