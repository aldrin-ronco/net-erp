using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
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
using IDialogService = NetErp.Helpers.IDialogService;

namespace NetErp.Books.AccountingPresentations.ViewModels
{
    public class AccountingPresentationViewModel : Screen,
        IHandle<AccountingPresentationCreateMessage>,
        IHandle<AccountingPresentationUpdateMessage>,
        IHandle<AccountingPresentationDeleteMessage>,
        IHandle<PermissionsCacheRefreshedMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<AccountingPresentationGraphQLModel> _accountingPresentationService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IDialogService _dialogService;
        private readonly AccountingBookCache _accountingBookCache;
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

        public ObservableCollection<AccountingPresentationGraphQLModel> AccountingPresentations
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountingPresentations));
                }
            }
        } = [];

        public AccountingPresentationGraphQLModel? SelectedPresentation
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedPresentation));
                    NotifyOfPropertyChange(nameof(CanEditPresentation));
                    NotifyOfPropertyChange(nameof(CanDeletePresentation));
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
                        _ = _searchDebounce.RunAsync(LoadAccountingPresentationsAsync);
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

        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.AccountingPresentation.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.AccountingPresentation.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.AccountingPresentation.Delete);

        #endregion

        #region Button States

        public bool CanCreatePresentation => HasCreatePermission && !IsBusy;
        public bool CanEditPresentation => HasEditPermission && SelectedPresentation != null;
        public bool CanDeletePresentation => HasDeletePermission && SelectedPresentation != null;

        #endregion

        #region Commands

        private ICommand? _createPresentationCommand;
        public ICommand CreatePresentationCommand
        {
            get
            {
                _createPresentationCommand ??= new AsyncCommand(CreatePresentationAsync);
                return _createPresentationCommand;
            }
        }

        private ICommand? _editPresentationCommand;
        public ICommand EditPresentationCommand
        {
            get
            {
                _editPresentationCommand ??= new AsyncCommand(EditPresentationAsync);
                return _editPresentationCommand;
            }
        }

        private ICommand? _deletePresentationCommand;
        public ICommand DeletePresentationCommand
        {
            get
            {
                _deletePresentationCommand ??= new AsyncCommand(DeletePresentationAsync);
                return _deletePresentationCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadAccountingPresentationsAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public AccountingPresentationViewModel(
            IEventAggregator eventAggregator,
            IRepository<AccountingPresentationGraphQLModel> accountingPresentationService,
            Helpers.Services.INotificationService notificationService,
            IDialogService dialogService,
            AccountingBookCache accountingBookCache,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            PermissionCache permissionCache)
        {
            _eventAggregator = eventAggregator;
            _accountingPresentationService = accountingPresentationService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _accountingBookCache = accountingBookCache;
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
                IsBusy = true;
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.AccountingPresentation);
                await LoadAccountingPresentationsAsync();
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
            NotifyOfPropertyChange(nameof(CanCreatePresentation));
            NotifyOfPropertyChange(nameof(CanEditPresentation));
            NotifyOfPropertyChange(nameof(CanDeletePresentation));
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

        public async Task CreatePresentationAsync()
        {
            try
            {
                IsBusy = true;
                AccountingPresentationDetailViewModel detail = new(_accountingPresentationService, _eventAggregator, _accountingBookCache, _joinableTaskFactory, _stringLengthCache);
                await detail.InitializeAsync();
                detail.SetForNew();
                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.50;
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Nueva presentación contable");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(CreatePresentationAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditPresentationAsync()
        {
            if (SelectedPresentation == null) return;
            try
            {
                IsBusy = true;
                AccountingPresentationDetailViewModel detail = new(_accountingPresentationService, _eventAggregator, _accountingBookCache, _joinableTaskFactory, _stringLengthCache);
                await detail.InitializeAsync();
                await detail.LoadDataForEditAsync(SelectedPresentation.Id);
                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.50;
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Editar presentación contable");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(EditPresentationAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeletePresentationAsync()
        {
            if (SelectedPresentation == null) return;
            try
            {
                IsBusy = true;

                var (canDeleteFragment, canDeleteQuery) = _canDeleteAccountingPresentationQuery.Value;
                ExpandoObject canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedPresentation.Id)
                    .Build();
                CanDeleteType validation = await _accountingPresentationService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                var (deleteFragment, deleteQuery) = _deleteAccountingPresentationQuery.Value;
                ExpandoObject deleteVars = new GraphQLVariables()
                    .For(deleteFragment, "id", SelectedPresentation.Id)
                    .Build();
                DeleteResponseType deletedPresentation = await _accountingPresentationService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!deletedPresentation.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedPresentation.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnUIThreadAsync(new AccountingPresentationDeleteMessage { DeletedAccountingPresentation = deletedPresentation });
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(DeletePresentationAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Load

        public async Task LoadAccountingPresentationsAsync()
        {
            try
            {
                IsBusy = true;
                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadAccountingPresentationsQuery.Value;

                dynamic filters = new ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.name = FilterSearch.Trim().RemoveExtraSpaces();

                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<AccountingPresentationGraphQLModel> result = await _accountingPresentationService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                AccountingPresentations = [.. result.Entries];
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(LoadAccountingPresentationsAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadAccountingPresentationsQuery = new(() =>
        {
            var fields = FieldSpec<PageType<AccountingPresentationGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.AllowsClosure)
                    .Select(e => e.ClosureAccountingBook, acc => acc
                        .Field(c => c.Id)
                        .Field(c => c.Name)))
                .Build();

            var fragment = new GraphQLQueryFragment("accountingPresentationsPage",
                [new("filters", "AccountingPresentationFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteAccountingPresentationQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteAccountingPresentation",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteAccountingPresentationQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteAccountingPresentation",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(AccountingPresentationCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingPresentationsAsync();
            _notificationService.ShowSuccess(message.CreatedAccountingPresentation.Message);
        }

        public async Task HandleAsync(AccountingPresentationUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingPresentationsAsync();
            _notificationService.ShowSuccess(message.UpdatedAccountingPresentation.Message);
        }

        public async Task HandleAsync(AccountingPresentationDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingPresentationsAsync();
            SelectedPresentation = null;
            _notificationService.ShowSuccess(message.DeletedAccountingPresentation.Message);
        }

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreatePresentation));
            NotifyOfPropertyChange(nameof(CanEditPresentation));
            NotifyOfPropertyChange(nameof(CanDeletePresentation));
            return Task.CompletedTask;
        }

        #endregion
    }
}
