using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Books;
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

namespace NetErp.Books.AccountingGroups.ViewModels
{
    public class AccountingGroupViewModel : Screen,
        IHandle<AccountingGroupCreateMessage>,
        IHandle<AccountingGroupUpdateMessage>,
        IHandle<AccountingGroupDeleteMessage>,
        IHandle<PermissionsCacheRefreshedMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<AccountingGroupGraphQLModel> _accountingGroupService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly IGraphQLClient _graphQLClient;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;
        private readonly TaxCache _taxCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly PermissionCache _permissionCache;
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

        public ObservableCollection<AccountingGroupGraphQLModel> AccountingGroups
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountingGroups));
                }
            }
        } = [];

        public AccountingGroupGraphQLModel? SelectedItem
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(CanEditAccountingGroup));
                    NotifyOfPropertyChange(nameof(CanDeleteAccountingGroup));
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
                        _ = _searchDebounce.RunAsync(LoadAccountingGroupsAsync);
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

        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.AccountingGroup.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.AccountingGroup.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.AccountingGroup.Delete);

        #endregion

        #region Button States

        public bool CanCreateAccountingGroup => HasCreatePermission && !IsBusy;
        public bool CanEditAccountingGroup => HasEditPermission && SelectedItem != null;
        public bool CanDeleteAccountingGroup => HasDeletePermission && SelectedItem != null;

        #endregion

        #region Commands

        private ICommand? _createAccountingGroupCommand;
        public ICommand CreateAccountingGroupCommand
        {
            get
            {
                _createAccountingGroupCommand ??= new AsyncCommand(CreateAccountingGroupAsync);
                return _createAccountingGroupCommand;
            }
        }

        private ICommand? _editAccountingGroupCommand;
        public ICommand EditAccountingGroupCommand
        {
            get
            {
                _editAccountingGroupCommand ??= new AsyncCommand(EditAccountingGroupAsync);
                return _editAccountingGroupCommand;
            }
        }

        private ICommand? _deleteAccountingGroupCommand;
        public ICommand DeleteAccountingGroupCommand
        {
            get
            {
                _deleteAccountingGroupCommand ??= new AsyncCommand(DeleteAccountingGroupAsync);
                return _deleteAccountingGroupCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadAccountingGroupsAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public AccountingGroupViewModel(
            IEventAggregator eventAggregator,
            IRepository<AccountingGroupGraphQLModel> accountingGroupService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache,
            TaxCache taxCache,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            IGraphQLClient graphQLClient,
            PermissionCache permissionCache)
        {
            _eventAggregator = eventAggregator;
            _accountingGroupService = accountingGroupService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;
            _taxCache = taxCache;
            _stringLengthCache = stringLengthCache;
            _joinableTaskFactory = joinableTaskFactory;
            _graphQLClient = graphQLClient;
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
                IsBusy = true;
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.AccountingGroup);
                await LoadAccountingGroupsAsync();
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
            NotifyOfPropertyChange(nameof(CanCreateAccountingGroup));
            NotifyOfPropertyChange(nameof(CanEditAccountingGroup));
            NotifyOfPropertyChange(nameof(CanDeleteAccountingGroup));
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

        public async Task CreateAccountingGroupAsync()
        {
            try
            {
                IsBusy = true;
                AccountingGroupDetailViewModel detail = new(
                    _accountingGroupService, _eventAggregator, _auxiliaryAccountingAccountCache,
                    _taxCache, _stringLengthCache, _joinableTaskFactory, _graphQLClient);
                await detail.InitializeAsync();
                detail.SetForNew();
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.80;

                await _dialogService.ShowDialogAsync(detail, "Nuevo grupo contable");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(CreateAccountingGroupAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditAccountingGroupAsync()
        {
            if (SelectedItem == null) return;
            try
            {
                IsBusy = true;
                AccountingGroupDetailViewModel detail = new(
                    _accountingGroupService, _eventAggregator, _auxiliaryAccountingAccountCache,
                    _taxCache, _stringLengthCache, _joinableTaskFactory, _graphQLClient);
                await detail.InitializeAsync();
                await detail.LoadDataForEditAsync(SelectedItem.Id);
                detail.SetForEdit();
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.80;

                await _dialogService.ShowDialogAsync(detail, "Editar grupo contable");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(EditAccountingGroupAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteAccountingGroupAsync()
        {
            if (SelectedItem == null) return;
            try
            {
                IsBusy = true;

                var (canDeleteFragment, canDeleteQuery) = _canDeleteQuery.Value;
                ExpandoObject canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedItem.Id)
                    .Build();
                CanDeleteType validation = await _accountingGroupService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                ExpandoObject deleteVars = new GraphQLVariables()
                    .For(deleteFragment, "id", SelectedItem.Id)
                    .Build();
                DeleteResponseType deletedAccountingGroup = await _accountingGroupService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!deletedAccountingGroup.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedAccountingGroup.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new AccountingGroupDeleteMessage { DeletedAccountingGroup = deletedAccountingGroup },
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(DeleteAccountingGroupAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Load

        public async Task LoadAccountingGroupsAsync()
        {
            try
            {
                IsBusy = true;
                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadQuery.Value;

                dynamic filters = new ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.name = FilterSearch.Trim().RemoveExtraSpaces();

                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<AccountingGroupGraphQLModel> result = await _accountingGroupService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                AccountingGroups = [.. result.Entries];
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(LoadAccountingGroupsAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
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
            var fields = FieldSpec<PageType<AccountingGroupGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Select(e => e.AccountInventory, acc => acc
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                        .Field(c => c.Code))
                    .Select(e => e.AccountCost, acc => acc
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                        .Field(c => c.Code))
                    .Select(e => e.AccountIncome, acc => acc
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                        .Field(c => c.Code)))
                .Build();

            var fragment = new GraphQLQueryFragment("accountingGroupsPage",
                [new("filters", "AccountingGroupFilters")],
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

            var fragment = new GraphQLQueryFragment("deleteAccountingGroup",
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

            var fragment = new GraphQLQueryFragment("canDeleteAccountingGroup",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(AccountingGroupCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingGroupsAsync();
            _notificationService.ShowSuccess(message.CreatedAccountingGroup.Message);
        }

        public async Task HandleAsync(AccountingGroupUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingGroupsAsync();
            _notificationService.ShowSuccess(message.UpdatedAccountingGroup.Message);
        }

        public async Task HandleAsync(AccountingGroupDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingGroupsAsync();
            SelectedItem = null;
            _notificationService.ShowSuccess(message.DeletedAccountingGroup.Message);
        }

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateAccountingGroup));
            NotifyOfPropertyChange(nameof(CanEditAccountingGroup));
            NotifyOfPropertyChange(nameof(CanDeleteAccountingGroup));
            return Task.CompletedTask;
        }

        #endregion
    }
}
