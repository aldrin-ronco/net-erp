using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using NetErp.Helpers;
using IDialogService = NetErp.Helpers.IDialogService;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.ObjectModel;
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
        private readonly IDialogService _dialogService;

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

        #endregion

        #region Constructor

        public AccountingBookViewModel(
            IEventAggregator eventAggregator,
            IRepository<AccountingBookGraphQLModel> accountingBookService,
            Helpers.Services.INotificationService notificationService,
            IDialogService dialogService)
        {
            _eventAggregator = eventAggregator;
            _accountingBookService = accountingBookService;
            _notificationService = notificationService;
            _dialogService = dialogService;
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
                var detail = new AccountingBookDetailViewModel(_accountingBookService, _eventAggregator);
                await _dialogService.ShowDialogAsync(detail, "Nuevo libro contable");
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{currentMethod!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        public async Task EditBookAsync()
        {
            if (SelectedAccountingBook == null) return;
            try
            {
                var detail = new AccountingBookDetailViewModel(_accountingBookService, _eventAggregator);
                detail.LoadForEdit(SelectedAccountingBook);
                await _dialogService.ShowDialogAsync(detail, "Editar libro contable");
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{currentMethod!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        public async Task DeleteBookAsync()
        {
            if (SelectedAccountingBook == null) return;
            try
            {
                IsBusy = true;
                Refresh();

                string query = GetCanDeleteAccountingBookQuery();
                object variables = new { canDeleteResponseId = SelectedAccountingBook.Id };
                var validation = await _accountingBookService.CanDeleteAsync(query, variables);

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
                string deleteQuery = GetDeleteAccountingBookQuery();
                object deleteVars = new { deleteResponseId = SelectedAccountingBook.Id };
                DeleteResponseType deletedBook = await _accountingBookService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!deletedBook.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro \n\n {deletedBook.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await _eventAggregator.PublishOnUIThreadAsync(new AccountingBookDeleteMessage { DeletedAccountingBook = deletedBook });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content!.ToString()!);
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()!.Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
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

                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponseFilters.matching = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                string query = GetLoadAccountingBooksQuery();
                PageType<AccountingBookGraphQLModel> result = await _accountingBookService.GetPageAsync(query, variables);

                AccountingBooks = [.. result.Entries];
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content!.ToString()!);
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()!.Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        public string GetLoadAccountingBooksQuery()
        {
            var fields = FieldSpec<PageType<AccountingBookGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name))
                .Build();

            var parameter = new GraphQLQueryParameter("filters", "AccountingBookFilters");
            var fragment = new GraphQLQueryFragment("accountingBooksPage", [parameter], fields, "PageResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        public string GetDeleteAccountingBookQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("deleteAccountingBook", [parameter], fields, alias: "DeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetCanDeleteAccountingBookQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("canDeleteAccountingBook", [parameter], fields, alias: "CanDeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

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
