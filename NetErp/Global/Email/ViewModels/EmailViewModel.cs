using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using IDialogService = NetErp.Helpers.IDialogService;
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.EmailGraphQLModel;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.Email.ViewModels
{
    public class EmailViewModel : Screen,
        IHandle<EmailDeleteMessage>,
        IHandle<EmailUpdateMessage>,
        IHandle<EmailCreateMessage>,
        IHandle<PermissionsCacheRefreshedMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<EmailGraphQLModel> _emailService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IDialogService _dialogService;
        private readonly SmtpCache _smtpCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly PermissionCache _permissionCache;
        private readonly DebouncedAction _searchDebounce = new();

        #endregion

        #region Constructor

        public EmailViewModel(
            IEventAggregator eventAggregator,
            IRepository<EmailGraphQLModel> emailService,
            Helpers.Services.INotificationService notificationService,
            IDialogService dialogService,
            SmtpCache smtpCache,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            PermissionCache permissionCache)
        {
            _eventAggregator = eventAggregator;
            _emailService = emailService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _smtpCache = smtpCache;
            _stringLengthCache = stringLengthCache;
            _joinableTaskFactory = joinableTaskFactory;
            _permissionCache = permissionCache;
            _eventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Properties

        public ObservableCollection<EmailGraphQLModel> Emails
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Emails));
                }
            }
        } = [];

        public EmailGraphQLModel? SelectedItem
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(CanEditEmail));
                    NotifyOfPropertyChange(nameof(CanDeleteEmail));
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
                        _ = _searchDebounce.RunAsync(LoadEmailsAsync);
                    }
                }
            }
        } = string.Empty;

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    NotifyOfPropertyChange(nameof(CanCreateEmail));
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

        public bool ShowActiveEmailsOnly
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ShowActiveEmailsOnly));
                    _ = LoadEmailsAsync();
                }
            }
        } = true;

        #region Permissions

        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.Email.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.Email.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.Email.Delete);

        #endregion

        public bool CanCreateEmail => HasCreatePermission && !IsBusy;
        public bool CanEditEmail => HasEditPermission && SelectedItem is not null;
        public bool CanDeleteEmail => HasDeletePermission && SelectedItem is not null;

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

        #endregion

        #region Commands

        private ICommand? _createEmailCommand;
        public ICommand CreateEmailCommand
        {
            get
            {
                _createEmailCommand ??= new AsyncCommand(CreateEmailAsync);
                return _createEmailCommand;
            }
        }

        private ICommand? _editEmailCommand;
        public ICommand EditEmailCommand
        {
            get
            {
                _editEmailCommand ??= new AsyncCommand(EditEmailAsync);
                return _editEmailCommand;
            }
        }

        private ICommand? _deleteEmailCommand;
        public ICommand DeleteEmailCommand
        {
            get
            {
                _deleteEmailCommand ??= new AsyncCommand(DeleteEmailAsync);
                return _deleteEmailCommand;
            }
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.Email);

                NotifyOfPropertyChange(nameof(HasCreatePermission));
                NotifyOfPropertyChange(nameof(HasEditPermission));
                NotifyOfPropertyChange(nameof(HasDeletePermission));
                NotifyOfPropertyChange(nameof(CanCreateEmail));
                NotifyOfPropertyChange(nameof(CanEditEmail));
                NotifyOfPropertyChange(nameof(CanDeleteEmail));

                await LoadEmailsAsync();
                _isInitialized = true;
                ShowEmptyState = Emails == null || Emails.Count == 0;
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
                Emails?.Clear();
                SelectedItem = null;
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region Data Operations

        public async Task LoadEmailsAsync()
        {
            try
            {
                IsBusy = true;
                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadEmailsQuery.Value;

                dynamic filters = new ExpandoObject();
                if (ShowActiveEmailsOnly) filters.isActive = true;
                if (!string.IsNullOrEmpty(FilterSearch)) filters.matching = FilterSearch.Trim().RemoveExtraSpaces();

                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<EmailGraphQLModel> result = await _emailService.GetPageAsync(query, variables);
                Emails = [.. result.Entries];

                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(LoadEmailsAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task CreateEmailAsync()
        {
            try
            {
                IsBusy = true;
                EmailDetailViewModel detail = new(_emailService, _eventAggregator, _smtpCache, _stringLengthCache, _joinableTaskFactory);
                await detail.InitializeAsync();
                detail.SetForNew();
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.40;
                }

                await _dialogService.ShowDialogAsync(detail, "Nuevo correo electrónico");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(CreateEmailAsync)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditEmailAsync()
        {
            try
            {
                IsBusy = true;
                EmailDetailViewModel detail = new(_emailService, _eventAggregator, _smtpCache, _stringLengthCache, _joinableTaskFactory);
                await detail.InitializeAsync();
                await detail.LoadDataForEditAsync(SelectedItem!.Id);
                detail.SetForEdit();
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.40;
                }

                await _dialogService.ShowDialogAsync(detail, "Editar correo electrónico");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(EditEmailAsync)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteEmailAsync()
        {
            try
            {
                IsBusy = true;

                var (canDeleteFragment, canDeleteQuery) = _canDeleteEmailQuery.Value;
                ExpandoObject canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedItem?.Id)
                    .Build();
                CanDeleteType validation = await _emailService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                var (deleteFragment, deleteQuery) = _deleteEmailQuery.Value;
                ExpandoObject deleteVars = new GraphQLVariables()
                    .For(deleteFragment, "id", SelectedItem?.Id)
                    .Build();
                DeleteResponseType deletedEmail = await _emailService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!deletedEmail.Success)
                {
                    ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedEmail.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(new EmailDeleteMessage { DeletedEmail = deletedEmail });
                NotifyOfPropertyChange(nameof(CanDeleteEmail));
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(DeleteEmailAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Event Handlers

        public async Task HandleAsync(EmailCreateMessage message, CancellationToken cancellationToken)
        {
            ShowEmptyState = false;
            await LoadEmailsAsync();
            _notificationService.ShowSuccess(message.CreatedEmail.Message);
        }

        public async Task HandleAsync(EmailUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadEmailsAsync();
            _notificationService.ShowSuccess(message.UpdatedEmail.Message);
        }

        public async Task HandleAsync(EmailDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadEmailsAsync();
            ShowEmptyState = Emails == null || Emails.Count == 0;
            _notificationService.ShowSuccess(message.DeletedEmail.Message);
        }

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateEmail));
            NotifyOfPropertyChange(nameof(CanEditEmail));
            NotifyOfPropertyChange(nameof(CanDeleteEmail));
            return Task.CompletedTask;
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadEmailsQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<PageType<EmailGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    .Field(e => e.Email)
                    .Field(e => e.IsActive)
                    .Select(e => e.Smtp, acc => acc
                        .Field(x => x!.Name)
                        .Field(x => x!.Host)
                        .Field(x => x!.Port)
                        .Field(x => x!.Id)))
                .Build();

            GraphQLQueryParameter parameter = new("filters", "EmailFilters");
            GraphQLQueryFragment fragment = new("emailsPage", [parameter], fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteEmailQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            GraphQLQueryParameter parameter = new("id", "ID!");
            GraphQLQueryFragment fragment = new("deleteEmail", [parameter], fields, alias: "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteEmailQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            GraphQLQueryParameter parameter = new("id", "ID!");
            GraphQLQueryFragment fragment = new("canDeleteEmail", [parameter], fields, alias: "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion
    }
}
