using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using Models.Books;
using Models.Suppliers;
using NetErp.Books.AccountingEntities.Validators;
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

namespace NetErp.Books.AccountingEntities.ViewModels
{
    public class AccountingEntityViewModel : Screen,
        IHandle<AccountingEntityCreateMessage>,
        IHandle<AccountingEntityDeleteMessage>,
        IHandle<AccountingEntityUpdateMessage>,
        IHandle<CustomerCreateMessage>,
        IHandle<CustomerUpdateMessage>,
        IHandle<SellerCreateMessage>,
        IHandle<SellerUpdateMessage>,
        IHandle<SupplierCreateMessage>,
        IHandle<SupplierUpdateMessage>,
        IHandle<PermissionsCacheRefreshedMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AccountingEntityGraphQLModel> _accountingEntityService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly IdentificationTypeCache _identificationTypeCache;
        private readonly CountryCache _countryCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly PermissionCache _permissionCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly AccountingEntityValidator _validator;
        private readonly DebouncedAction _searchDebounce;

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
                    // CanCreateAccountingEntity depends on !IsBusy. Without this
                    // explicit notification the binding goes stale whenever a
                    // DeleteAccountingEntityAsync (or any caller that triggers a
                    // Refresh() while IsBusy is true) evaluates CanCreate while
                    // IsBusy is true and caches "false" — the subsequent IsBusy
                    // reset in the finally block would then never propagate.
                    NotifyOfPropertyChange(nameof(CanCreateAccountingEntity));
                }
            }
        }

        public ObservableCollection<AccountingEntityGraphQLModel> AccountingEntities
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountingEntities));
                    NotifyOfPropertyChange(nameof(CanDeleteAccountingEntity));
                }
            }
        } = [];

        public AccountingEntityGraphQLModel? SelectedAccountingEntity
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingEntity));
                    NotifyOfPropertyChange(nameof(CanEditAccountingEntity));
                    NotifyOfPropertyChange(nameof(CanDeleteAccountingEntity));
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
                        _ = _searchDebounce.RunAsync(LoadAccountingEntitiesAsync);
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

        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.AccountingEntity.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.AccountingEntity.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.AccountingEntity.Delete);

        #endregion

        #region Button States

        public bool CanCreateAccountingEntity => HasCreatePermission && !IsBusy;
        public bool CanEditAccountingEntity => HasEditPermission && SelectedAccountingEntity != null;
        public bool CanDeleteAccountingEntity => HasDeletePermission && SelectedAccountingEntity != null;

        #endregion

        #region Commands

        private ICommand? _createAccountingEntityCommand;
        public ICommand CreateAccountingEntityCommand
        {
            get
            {
                _createAccountingEntityCommand ??= new AsyncCommand(CreateAccountingEntityAsync);
                return _createAccountingEntityCommand;
            }
        }

        private ICommand? _editAccountingEntityCommand;
        public ICommand EditAccountingEntityCommand
        {
            get
            {
                _editAccountingEntityCommand ??= new AsyncCommand(EditAccountingEntityAsync);
                return _editAccountingEntityCommand;
            }
        }

        private ICommand? _deleteAccountingEntityCommand;
        public ICommand DeleteAccountingEntityCommand
        {
            get
            {
                _deleteAccountingEntityCommand ??= new AsyncCommand(DeleteAccountingEntityAsync);
                return _deleteAccountingEntityCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadAccountingEntitiesAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public AccountingEntityViewModel(
            IEventAggregator eventAggregator,
            Helpers.Services.INotificationService notificationService,
            IRepository<AccountingEntityGraphQLModel> accountingEntityService,
            Helpers.IDialogService dialogService,
            IdentificationTypeCache identificationTypeCache,
            CountryCache countryCache,
            StringLengthCache stringLengthCache,
            PermissionCache permissionCache,
            JoinableTaskFactory joinableTaskFactory,
            AccountingEntityValidator validator,
            DebouncedAction searchDebounce)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _accountingEntityService = accountingEntityService ?? throw new ArgumentNullException(nameof(accountingEntityService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _identificationTypeCache = identificationTypeCache ?? throw new ArgumentNullException(nameof(identificationTypeCache));
            _countryCache = countryCache ?? throw new ArgumentNullException(nameof(countryCache));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _permissionCache = permissionCache;
            _joinableTaskFactory = joinableTaskFactory;
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
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.AccountingEntity);
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
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateAccountingEntity));
            NotifyOfPropertyChange(nameof(CanEditAccountingEntity));
            NotifyOfPropertyChange(nameof(CanDeleteAccountingEntity));
            await LoadAccountingEntitiesAsync();
            this.SetFocus(() => FilterSearch);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                _eventAggregator.Unsubscribe(this);
                AccountingEntities.Clear();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region CRUD Operations

        public async Task CreateAccountingEntityAsync()
        {
            try
            {
                IsBusy = true;
                AccountingEntityDetailViewModel detail = new(_accountingEntityService, _eventAggregator, _identificationTypeCache, _countryCache, _stringLengthCache, _joinableTaskFactory, _validator);
                await detail.LoadCachesAsync();
                detail.SetForNew();

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.55;
                    detail.DialogHeight = parentView.ActualHeight * 0.95;
                }

                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Nuevo tercero");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(CreateAccountingEntityAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditAccountingEntityAsync()
        {
            if (SelectedAccountingEntity == null) return;
            try
            {
                IsBusy = true;
                AccountingEntityDetailViewModel detail = new(_accountingEntityService, _eventAggregator, _identificationTypeCache, _countryCache, _stringLengthCache, _joinableTaskFactory, _validator);
                await detail.LoadCachesAsync();
                await detail.LoadDataForEditAsync(SelectedAccountingEntity.Id);

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.55;
                    detail.DialogHeight = parentView.ActualHeight * 0.95;
                }

                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Editar tercero");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(EditAccountingEntityAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteAccountingEntityAsync()
        {
            if (SelectedAccountingEntity == null) return;
            try
            {
                IsBusy = true;
                Refresh();

                (GraphQLQueryFragment canDeleteFragment, string canDeleteQuery) = _canDeleteAccountingEntityQuery.Value;
                object canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedAccountingEntity.Id)
                    .Build();
                CanDeleteType validation = await _accountingEntityService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                (GraphQLQueryFragment deleteFragment, string deleteQuery) = _deleteAccountingEntityQuery.Value;
                object deleteVars = new GraphQLVariables()
                    .For(deleteFragment, "id", SelectedAccountingEntity.Id)
                    .Build();
                DeleteResponseType deletedAccountingEntity = await _accountingEntityService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!deletedAccountingEntity.Success)
                {
                    ThemedMessageBox.Show(title: "Atención !",
                        text: $"No pudo ser eliminado el registro \n\n {deletedAccountingEntity.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await _eventAggregator.PublishOnUIThreadAsync(new AccountingEntityDeleteMessage { DeletedAccountingEntity = deletedAccountingEntity });
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(DeleteAccountingEntityAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Load

        public async Task LoadAccountingEntitiesAsync()
        {
            try
            {
                IsBusy = true;
                Stopwatch stopwatch = Stopwatch.StartNew();

                (GraphQLQueryFragment fragment, string query) = _loadAccountingEntitiesQuery.Value;

                dynamic filters = new ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.matching = FilterSearch.Trim().RemoveExtraSpaces();

                object variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<AccountingEntityGraphQLModel> result = await _accountingEntityService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                AccountingEntities = new ObservableCollection<AccountingEntityGraphQLModel>(result.Entries);
                stopwatch.Stop();

                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(LoadAccountingEntitiesAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadAccountingEntitiesQuery = new(() =>
        {
            var fields = FieldSpec<PageType<AccountingEntityGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.IdentificationNumber)
                    .Field(e => e.VerificationDigit)
                    .Field(e => e.SearchName)
                    .Field(e => e.Regime)
                    .Field(e => e.TelephonicInformation)
                    .Field(e => e.Address))
                .Build();

            var fragment = new GraphQLQueryFragment("accountingEntitiesPage",
                [new("filters", "AccountingEntityFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteAccountingEntityQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteAccountingEntity",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteAccountingEntityQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteAccountingEntity",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(AccountingEntityCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingEntitiesAsync();
            _notificationService.ShowSuccess(message.CreatedAccountingEntity.Message);
        }

        public async Task HandleAsync(AccountingEntityUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingEntitiesAsync();
            _notificationService.ShowSuccess(message.UpdatedAccountingEntity.Message);
        }

        public async Task HandleAsync(AccountingEntityDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingEntitiesAsync();
            SelectedAccountingEntity = null;
            _notificationService.ShowSuccess(message.DeletedAccountingEntity.Message);
        }

        public Task HandleAsync(CustomerCreateMessage message, CancellationToken cancellationToken) => LoadAccountingEntitiesAsync();
        public Task HandleAsync(CustomerUpdateMessage message, CancellationToken cancellationToken) => LoadAccountingEntitiesAsync();
        public Task HandleAsync(SellerCreateMessage message, CancellationToken cancellationToken) => LoadAccountingEntitiesAsync();
        public Task HandleAsync(SellerUpdateMessage message, CancellationToken cancellationToken) => LoadAccountingEntitiesAsync();
        public Task HandleAsync(SupplierCreateMessage message, CancellationToken cancellationToken) => LoadAccountingEntitiesAsync();
        public Task HandleAsync(SupplierUpdateMessage message, CancellationToken cancellationToken) => LoadAccountingEntitiesAsync();

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateAccountingEntity));
            NotifyOfPropertyChange(nameof(CanEditAccountingEntity));
            NotifyOfPropertyChange(nameof(CanDeleteAccountingEntity));
            return Task.CompletedTask;
        }

        #endregion
    }
}
