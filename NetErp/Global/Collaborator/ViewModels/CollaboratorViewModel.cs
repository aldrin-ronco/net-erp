using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using IDialogService = NetErp.Helpers.IDialogService;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using Models.Login;
using NetErp.Global.Collaborator.DTO;
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

namespace NetErp.Global.Collaborator.ViewModels
{
    public class CollaboratorViewModel : Screen,
        IHandle<PermissionsCacheRefreshedMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<AccountGraphQLModel> _accountService;
        private readonly IAuthApiClient _authApiClient;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IDialogService _dialogService;
        private readonly CollaboratorCache _collaboratorCache;
        private readonly EmailCache _emailCache;
        private readonly AccessProfileCache _accessProfileCache;
        private readonly CostCenterCache _costCenterCache;
        private readonly PermissionCache _permissionCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly DebouncedAction _searchDebounce = new();

        #endregion

        #region Constructor

        public CollaboratorViewModel(
            IEventAggregator eventAggregator,
            IRepository<AccountGraphQLModel> accountService,
            IAuthApiClient authApiClient,
            Helpers.Services.INotificationService notificationService,
            IDialogService dialogService,
            CollaboratorCache collaboratorCache,
            EmailCache emailCache,
            AccessProfileCache accessProfileCache,
            CostCenterCache costCenterCache,
            PermissionCache permissionCache,
            JoinableTaskFactory joinableTaskFactory)
        {
            _eventAggregator = eventAggregator;
            _accountService = accountService;
            _authApiClient = authApiClient;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _collaboratorCache = collaboratorCache;
            _emailCache = emailCache;
            _accessProfileCache = accessProfileCache;
            _costCenterCache = costCenterCache;
            _permissionCache = permissionCache;
            _joinableTaskFactory = joinableTaskFactory;
            _eventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Properties

        public ObservableCollection<CollaboratorDisplayDTO> Collaborators
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Collaborators));
                }
            }
        } = [];

        public ObservableCollection<CollaboratorDisplayDTO> FilteredCollaborators
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FilteredCollaborators));
                }
            }
        } = [];

        public CollaboratorDisplayDTO? SelectedItem
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(CanEditCollaborator));
                    NotifyOfPropertyChange(nameof(CanDeleteCollaborator));
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
                        _ = _searchDebounce.RunAsync(() => { ApplyFilter(); return Task.CompletedTask; });
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

        #region Permissions

        public bool HasInvitePermission => _permissionCache.IsAllowed(PermissionCodes.Collaborator.Invite);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.Collaborator.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.Collaborator.Delete);

        #endregion

        public bool CanInviteCollaborator => HasInvitePermission;
        public bool CanEditCollaborator => HasEditPermission && SelectedItem is not null;
        public bool CanDeleteCollaborator => HasDeletePermission && SelectedItem is not null && !SelectedItem.IsOwner;

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

        private ICommand? _inviteCollaboratorCommand;
        public ICommand InviteCollaboratorCommand
        {
            get
            {
                _inviteCollaboratorCommand ??= new AsyncCommand(InviteCollaboratorAsync);
                return _inviteCollaboratorCommand;
            }
        }

        private ICommand? _editCollaboratorCommand;
        public ICommand EditCollaboratorCommand
        {
            get
            {
                _editCollaboratorCommand ??= new AsyncCommand(EditCollaboratorAsync);
                return _editCollaboratorCommand;
            }
        }

        private ICommand? _deleteCollaboratorCommand;
        public ICommand DeleteCollaboratorCommand
        {
            get
            {
                _deleteCollaboratorCommand ??= new AsyncCommand(DeleteCollaboratorAsync);
                return _deleteCollaboratorCommand;
            }
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                IsBusy = true;
                await Task.WhenAll(
                    _collaboratorCache.EnsureLoadedAsync(),
                    _emailCache.EnsureLoadedAsync(),
                    _accessProfileCache.EnsureLoadedAsync(),
                    _costCenterCache.EnsureLoadedAsync());

                NotifyOfPropertyChange(nameof(HasInvitePermission));
                NotifyOfPropertyChange(nameof(HasEditPermission));
                NotifyOfPropertyChange(nameof(HasDeletePermission));
                NotifyOfPropertyChange(nameof(CanInviteCollaborator));
                NotifyOfPropertyChange(nameof(CanEditCollaborator));
                NotifyOfPropertyChange(nameof(CanDeleteCollaborator));

                await LoadCollaboratorsAsync();
                _isInitialized = true;
                ShowEmptyState = Collaborators == null || Collaborators.Count == 0;
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
                Collaborators?.Clear();
                FilteredCollaborators?.Clear();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region Data Operations

        public async Task LoadCollaboratorsAsync()
        {
            try
            {
                IsBusy = true;
                Stopwatch stopwatch = Stopwatch.StartNew();

                // Build DTOs from collaborator cache
                ObservableCollection<CollaboratorDisplayDTO> collaborators = [];
                foreach (CollaboratorGraphQLModel collaborator in _collaboratorCache.Items)
                {
                    collaborators.Add(new CollaboratorDisplayDTO
                    {
                        AccountId = collaborator.Account.Id,
                        FullName = collaborator.Account.FullName,
                        Email = collaborator.Account.Email,
                        Profession = collaborator.Account.Profession,
                        PhotoUrl = collaborator.Account.PhotoUrl,
                        InvitedBy = collaborator.Inviter?.FullName ?? string.Empty,
                        IsOwner = collaborator.IsOwner,
                        JoinedAt = collaborator.InsertedAt
                    });
                }

                // Enrich with access profiles from Main API
                await EnrichWithProfilesAsync(collaborators);

                Collaborators = collaborators;
                ApplyFilter();

                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(LoadCollaboratorsAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task EnrichWithProfilesAsync(ObservableCollection<CollaboratorDisplayDTO> collaborators)
        {
            try
            {
                var (fragment, query) = _accountsWithProfilesQuery.Value;

                List<int> accountIds = [.. collaborators.Select(c => c.AccountId)];

                dynamic filters = new ExpandoObject();
                filters.ids = accountIds;

                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "filters", filters)
                    .For(fragment, "pagination", new { pageSize = accountIds.Count })
                    .Build();

                PageType<AccountGraphQLModel> result = await _accountService.GetPageAsync(query, variables);

                Dictionary<int, string> profilesByAccountId = result.Entries
                    .ToDictionary(
                        a => a.Id,
                        a => string.Join(", ", a.AccessProfiles.Select(p => p.Name)));

                foreach (CollaboratorDisplayDTO dto in collaborators)
                {
                    if (profilesByAccountId.TryGetValue(dto.AccountId, out string? profiles))
                        dto.ProfileNames = profiles;
                }
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(EnrichWithProfilesAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(FilterSearch))
            {
                FilteredCollaborators = [.. Collaborators];
                return;
            }

            string search = FilterSearch.Trim();
            FilteredCollaborators = [.. Collaborators.Where(c =>
                c.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                c.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                c.Profession.Contains(search, StringComparison.OrdinalIgnoreCase))];
        }

        public async Task InviteCollaboratorAsync()
        {
            try
            {
                IsBusy = true;
                CollaboratorInviteViewModel detail = new(_authApiClient, _accountService, _joinableTaskFactory);
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.50;

                bool? result = await _dialogService.ShowDialogAsync(detail, "Invitar colaborador");
                if (result == true)
                {
                    _collaboratorCache.Clear();
                    await _collaboratorCache.EnsureLoadedAsync();
                    await LoadCollaboratorsAsync();
                    ShowEmptyState = false;
                    _notificationService.ShowSuccess("Colaborador invitado exitosamente");
                }
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(InviteCollaboratorAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditCollaboratorAsync()
        {
            try
            {
                IsBusy = true;
                CollaboratorDetailViewModel detail = new(_accountService, _accessProfileCache, _emailCache, _costCenterCache, _joinableTaskFactory);
                await detail.LoadDataAsync(SelectedItem!.AccountId);
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.55;
                    detail.DialogHeight = parentView.ActualHeight * 0.80;
                }

                bool? result = await _dialogService.ShowDialogAsync(detail, "Editar colaborador");
                if (result == true)
                {
                    await LoadCollaboratorsAsync();
                    _notificationService.ShowSuccess("Colaborador actualizado exitosamente");
                }
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(EditCollaboratorAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteCollaboratorAsync()
        {
            try
            {
                IsBusy = false;
                if (ThemedMessageBox.Show("Atención !",
                    $"¿Confirma que desea eliminar al colaborador {SelectedItem!.FullName}?",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

                IsBusy = true;

                // Delete from Auth API
                GraphQL.GraphQLResponse<DeleteCollaboratorResponse> authResult = await _authApiClient.SendMutationAsync<DeleteCollaboratorResponse>(
                    new GraphQL.GraphQLRequest
                    {
                        Query = _deleteCollaboratorMutation.Value,
                        Variables = new
                        {
                            input = new
                            {
                                accountId = SelectedItem.AccountId,
                                companyId = SessionInfo.LoginCompanyId
                            }
                        }
                    });

                if (authResult.Errors != null && authResult.Errors.Length > 0)
                {
                    ThemedMessageBox.Show("Atención !", $"No se pudo eliminar el colaborador.\n\n{authResult.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _collaboratorCache.Clear();
                await _collaboratorCache.EnsureLoadedAsync();
                await LoadCollaboratorsAsync();
                ShowEmptyState = Collaborators == null || Collaborators.Count == 0;
                _notificationService.ShowSuccess("Colaborador eliminado exitosamente");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(DeleteCollaboratorAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Event Handlers

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasInvitePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanInviteCollaborator));
            NotifyOfPropertyChange(nameof(CanEditCollaborator));
            NotifyOfPropertyChange(nameof(CanDeleteCollaborator));
            return Task.CompletedTask;
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _accountsWithProfilesQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<PageType<AccountGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .SelectList(e => e.AccessProfiles, sq => sq
                        .Field(p => p.Id)
                        .Field(p => p.Name)))
                .Build();

            GraphQLQueryParameter filtersParam = new("filters", "AccountFilters");
            GraphQLQueryParameter paginationParam = new("pagination", "Pagination");
            GraphQLQueryFragment fragment = new("accountsPage", [filtersParam, paginationParam], fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<string> _deleteCollaboratorMutation = new(() => @"
            mutation ($input: DeleteCollaboratorInput!) {
                deleteCollaborator(input: $input) {
                    success
                    message
                }
            }");

        private class DeleteCollaboratorResponse
        {
            public DeleteCollaboratorPayload DeleteCollaborator { get; set; } = new();
        }

        private class DeleteCollaboratorPayload
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        #endregion
    }
}
