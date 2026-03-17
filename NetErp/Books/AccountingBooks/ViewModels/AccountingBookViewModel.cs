using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using NetErp.Helpers.GraphQLQueryBuilder;
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
        IHandle<AccountingBookDeleteMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<AccountingBookGraphQLModel> _accountingBookService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        #endregion

        #region Grid Properties

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        private ObservableCollection<AccountingBookGraphQLModel> _accountingBooks = [];
        public ObservableCollection<AccountingBookGraphQLModel> AccountingBooks
        {
            get => _accountingBooks;
            set
            {
                if (_accountingBooks != value)
                {
                    _accountingBooks = value;
                    NotifyOfPropertyChange(nameof(AccountingBooks));
                }
            }
        }

        private AccountingBookGraphQLModel? _selectedAccountingBook;
        public AccountingBookGraphQLModel? SelectedAccountingBook
        {
            get => _selectedAccountingBook;
            set
            {
                if (_selectedAccountingBook != value)
                {
                    _selectedAccountingBook = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingBook));
                    NotifyOfPropertyChange(nameof(CanEditBook));
                    NotifyOfPropertyChange(nameof(CanDeleteBook));
                }
            }
        }

        private string _filterSearch = string.Empty;
        public string FilterSearch
        {
            get => _filterSearch;
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = LoadAccountingBooksAsync();
                }
            }
        }

        private int _pageIndex = 1;
        public int PageIndex
        {
            get => _pageIndex;
            set
            {
                if (_pageIndex != value)
                {
                    _pageIndex = value;
                    NotifyOfPropertyChange(nameof(PageIndex));
                }
            }
        }

        private int _pageSize = 50;
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (_pageSize != value)
                {
                    _pageSize = value;
                    NotifyOfPropertyChange(nameof(PageSize));
                }
            }
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (_totalCount != value)
                {
                    _totalCount = value;
                    NotifyOfPropertyChange(nameof(TotalCount));
                }
            }
        }

        private string _responseTime = string.Empty;
        public string ResponseTime
        {
            get => _responseTime;
            set
            {
                if (_responseTime != value)
                {
                    _responseTime = value;
                    NotifyOfPropertyChange(nameof(ResponseTime));
                }
            }
        }

        #endregion

        #region Button States

        public bool CanEditBook => SelectedAccountingBook != null;
        public bool CanDeleteBook => SelectedAccountingBook != null;

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
            JoinableTaskFactory joinableTaskFactory)
        {
            _eventAggregator = eventAggregator;
            _accountingBookService = accountingBookService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _joinableTaskFactory = joinableTaskFactory;
            _eventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            await LoadAccountingBooksAsync();
        }

        protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                _eventAggregator.Unsubscribe(this);
            }
            await base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region CRUD Operations

        public async Task CreateBookAsync()
        {
            try
            {
                IsBusy = true;
                var detail = new AccountingBookDetailViewModel(_accountingBookService, _eventAggregator, _joinableTaskFactory);
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Nuevo libro contable");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(EditBookAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                var detail = new AccountingBookDetailViewModel(_accountingBookService, _eventAggregator, _joinableTaskFactory);
                detail.LoadForEdit(SelectedAccountingBook);
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Editar libro contable");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(CreateBookAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                Refresh();

                var (canDeleteFragment, canDeleteQuery) = _canDeleteAccountingBookQuery.Value;
                var canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedAccountingBook.Id)
                    .Build();
                var validation = await _accountingBookService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    if (ThemedMessageBox.Show("Atención !", "¿Confirma que desea eliminar el registro seleccionado?",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                }
                else
                {
                    IsBusy = false;
                    ThemedMessageBox.Show("Atención !",
                        $"El registro no puede ser eliminado\r\n\r\n{validation.Message}",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                var (deleteFragment, deleteQuery) = _deleteAccountingBookQuery.Value;
                var deleteVars = new GraphQLVariables()
                    .For(deleteFragment, "id", SelectedAccountingBook.Id)
                    .Build();
                DeleteResponseType deletedBook = await _accountingBookService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!deletedBook.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro \n\n {deletedBook.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await _eventAggregator.PublishOnUIThreadAsync(new AccountingBookDeleteMessage { DeletedAccountingBook = deletedBook });
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(DeleteBookAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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

                Stopwatch stopwatch = new();
                stopwatch.Start();

                var (fragment, query) = _loadAccountingBooksQuery.Value;

                dynamic filters = new ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.matching = FilterSearch.Trim().RemoveExtraSpaces();

                var variables = new GraphQLVariables()
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
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(LoadAccountingBooksAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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

        #endregion
    }
}
