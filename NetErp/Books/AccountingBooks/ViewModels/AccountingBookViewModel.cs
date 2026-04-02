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
using Extensions.Global;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.AccountingBooks.ViewModels
{
    public class AccountingBookViewModel : Screen,
        IHandle<AccountingBookCreateMessage>,
        IHandle<AccountingBookUpdateMessage>,
        IHandle<AccountingBookDeleteMessage>,
        IHandle<PermissionsCacheRefreshedMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<AccountingBookGraphQLModel> _accountingBookService;
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

        public ObservableCollection<AccountingBookGraphQLModel> AccountingBooks
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountingBooks));
                }
            }
        } = [];

        public AccountingBookGraphQLModel? SelectedAccountingBook
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingBook));
                    NotifyOfPropertyChange(nameof(CanEditBook));
                    NotifyOfPropertyChange(nameof(CanDeleteBook));
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
                        _ = _searchDebounce.RunAsync(LoadAccountingBooksAsync);
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

        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.AccountingBook.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.AccountingBook.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.AccountingBook.Delete);

        #endregion

        #region Button States

        public bool CanCreateBook => HasCreatePermission && !IsBusy;
        public bool CanEditBook => HasEditPermission && SelectedAccountingBook != null;
        public bool CanDeleteBook => HasDeletePermission && SelectedAccountingBook != null;

        #endregion

        #region Commands

        private ICommand? _createBookCommand;
        public ICommand CreateBookCommand
        {
            get
            {
                _createBookCommand ??= new AsyncCommand(CreateBookAsync);
                return _createBookCommand;
            }
        }

        private ICommand? _editBookCommand;
        public ICommand EditBookCommand
        {
            get
            {
                _editBookCommand ??= new AsyncCommand(EditBookAsync);
                return _editBookCommand;
            }
        }

        private ICommand? _deleteBookCommand;
        public ICommand DeleteBookCommand
        {
            get
            {
                _deleteBookCommand ??= new AsyncCommand(DeleteBookAsync);
                return _deleteBookCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadAccountingBooksAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public AccountingBookViewModel(
            IEventAggregator eventAggregator,
            IRepository<AccountingBookGraphQLModel> accountingBookService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            JoinableTaskFactory joinableTaskFactory,
            PermissionCache permissionCache,
            StringLengthCache stringLengthCache)
        {
            _eventAggregator = eventAggregator;
            _accountingBookService = accountingBookService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _joinableTaskFactory = joinableTaskFactory;
            _permissionCache = permissionCache;
            _stringLengthCache = stringLengthCache;
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
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.AccountingBook);
                await LoadAccountingBooksAsync();
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
            NotifyOfPropertyChange(nameof(CanCreateBook));
            NotifyOfPropertyChange(nameof(CanEditBook));
            NotifyOfPropertyChange(nameof(CanDeleteBook));
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

        public async Task CreateBookAsync()
        {
            try
            {
                IsBusy = true;
                AccountingBookDetailViewModel detail = new(_accountingBookService, _eventAggregator, _joinableTaskFactory, _stringLengthCache);
                detail.SetForNew();
                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.40;
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Nuevo libro contable");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(CreateBookAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditBookAsync()
        {
            if (SelectedAccountingBook == null) return;
            try
            {
                IsBusy = true;
                AccountingBookDetailViewModel detail = new(_accountingBookService, _eventAggregator, _joinableTaskFactory, _stringLengthCache);
                detail.SetForEdit(SelectedAccountingBook);
                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.40;
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Editar libro contable");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(EditBookAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteBookAsync()
        {
            if (SelectedAccountingBook == null) return;
            try
            {
                IsBusy = true;

                var (canDeleteFragment, canDeleteQuery) = _canDeleteAccountingBookQuery.Value;
                ExpandoObject canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedAccountingBook.Id)
                    .Build();
                CanDeleteType validation = await _accountingBookService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                var (deleteFragment, deleteQuery) = _deleteAccountingBookQuery.Value;
                ExpandoObject deleteVars = new GraphQLVariables()
                    .For(deleteFragment, "id", SelectedAccountingBook.Id)
                    .Build();
                DeleteResponseType deletedBook = await _accountingBookService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!deletedBook.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedBook.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnUIThreadAsync(new AccountingBookDeleteMessage { DeletedAccountingBook = deletedBook });
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(DeleteBookAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Load

        public async Task LoadAccountingBooksAsync()
        {
            try
            {
                IsBusy = true;
                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadAccountingBooksQuery.Value;

                dynamic filters = new ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.matching = FilterSearch.Trim().RemoveExtraSpaces();

                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<AccountingBookGraphQLModel> result = await _accountingBookService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                AccountingBooks = [.. result.Entries];
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(LoadAccountingBooksAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadAccountingBooksQuery = new(() =>
        {
            var fields = FieldSpec<PageType<AccountingBookGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name))
                .Build();

            var fragment = new GraphQLQueryFragment("accountingBooksPage",
                [new("filters", "AccountingBookFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteAccountingBookQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteAccountingBook",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteAccountingBookQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteAccountingBook",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(AccountingBookCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingBooksAsync();
            _notificationService.ShowSuccess(message.CreatedAccountingBook.Message);
        }

        public async Task HandleAsync(AccountingBookUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingBooksAsync();
            _notificationService.ShowSuccess(message.UpdatedAccountingBook.Message);
        }

        public async Task HandleAsync(AccountingBookDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingBooksAsync();
            SelectedAccountingBook = null;
            _notificationService.ShowSuccess(message.DeletedAccountingBook.Message);
        }

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateBook));
            NotifyOfPropertyChange(nameof(CanEditBook));
            NotifyOfPropertyChange(nameof(CanDeleteBook));
            return Task.CompletedTask;
        }

        #endregion
    }
}
