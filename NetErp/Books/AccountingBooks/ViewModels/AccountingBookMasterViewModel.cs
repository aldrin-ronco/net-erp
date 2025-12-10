using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.AccountingBooks.ViewModels
{
    public class AccountingBookMasterViewModel: Screen,
        IHandle<AccountingBookDeleteMessage>,
        IHandle<AccountingBookUpdateMessage>,
        IHandle<AccountingBookCreateMessage>
    {

        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AccountingBookGraphQLModel> _accountingBookService;

        public AccountingBookMasterViewModel(AccountingBookViewModel context, Helpers.Services.INotificationService notificationService,
            IRepository<AccountingBookGraphQLModel> accountingBookService)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnPublishedThread(this);
            _accountingBookService = accountingBookService;
            _notificationService = notificationService;
            this.SetFocus(() => FilterSearch);
        }
  
        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }
        private string _filterSearch;
        public string FilterSearch
        {
            get { return _filterSearch; }
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = Task.Run(() => LoadAccountingBooksAsync());
                }
            }
        }
        public bool CanDeleteAccountingBook
        {
            get
            {
                if (SelectedItem is null) return false;
                return true;
            }
        }
        private ICommand _deleteAccountingBookCommand;
        public ICommand DeleteAccountingBookCommand
        {
            get
            {
                if (_deleteAccountingBookCommand is null) _deleteAccountingBookCommand = new AsyncCommand(DeleteAccountingBook);
                return _deleteAccountingBookCommand;
            }
        }
        private ICommand _createAccountingBookCommand;
        public ICommand CreateAccountingBookCommand
        {
            get
            {
                if (_createAccountingBookCommand is null) _createAccountingBookCommand = new AsyncCommand(CreateAccountingBookAsync);
                return _createAccountingBookCommand;
            }
        }
        public AccountingBookViewModel Context { get; set; }
        private AccountingBookGraphQLModel? _selectedItem = null;
        public AccountingBookGraphQLModel? SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(CanDeleteAccountingBook));
                }
            }
        }
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            IsBusy = true;
            _ = Task.Run(() => LoadAccountingBooksAsync());
            this.SetFocus(() => FilterSearch);
        }

        
        public ObservableCollection<AccountingBookGraphQLModel> _accountingBooks;
        public ObservableCollection<AccountingBookGraphQLModel> AccountingBooks
        {
            get { return _accountingBooks; }
            set
            {
                if (_accountingBooks != value)
                {
                    _accountingBooks = value;
                    NotifyOfPropertyChange(nameof(AccountingBooks));
                }
            }
        }
        public async Task CreateAccountingBookAsync()
        {
            await Context.ActivateDetailViewForNewAsync();
        }
        public string GetLoadAccountingBooksQuery()
        {
            var accountingBookFields = FieldSpec<PageType<AccountingBookGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    )
                .Build();

            var accountingBookParameters = new GraphQLQueryParameter("filters", "AccountingBookFilters");

            var accountingBookFragment = new GraphQLQueryFragment("accountingBooksPage", [accountingBookParameters], accountingBookFields, "PageResponse");

            var builder = new GraphQLQueryBuilder([accountingBookFragment]);

            return builder.GetQuery();
        }
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
                this.AccountingBooks = [.. result.Entries];
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
           
        }
       
        public async Task EditAccountingBook()
        {
            await Context.ActivateDetailViewForEditAsync(SelectedItem ?? new ());

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

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }

        public async Task DeleteAccountingBook()
        {
            try
            {

                this.IsBusy = true;
                this.Refresh();

                string query = GetCanDeleteAccountingBookQuery();

                object variables = new { canDeleteResponseId = SelectedItem.Id };

                var validation = await _accountingBookService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    if (ThemedMessageBox.Show("Atención !", "¿Confirma que desea eliminar el registro seleccionado?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                }
                else
                {
                    IsBusy = false;
                    ThemedMessageBox.Show("Atención !", "El registro no puede ser eliminado" +
                        (char)13 + (char)13 + validation.Message, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                DeleteResponseType deletedAccountingBook = await Task.Run(() => this.ExecuteDeleteAccountingBookAsync(SelectedItem.Id));

                if (!deletedAccountingBook.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedAccountingBook.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await Context.EventAggregator.PublishOnUIThreadAsync(new AccountingBookDeleteMessage { DeletedAccountingBook = deletedAccountingBook });

                NotifyOfPropertyChange(nameof(CanDeleteAccountingBook));
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
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

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public async Task<DeleteResponseType> ExecuteDeleteAccountingBookAsync(int id)
        {
            try
            {

                string query = GetDeleteAccountingBookQuery();

                object variables = new
                {
                    deleteResponseId = id
                };

                // Eliminar registros
                DeleteResponseType deletedRecord = await _accountingBookService.DeleteAsync<DeleteResponseType>(query, variables);
                return deletedRecord;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task HandleAsync(AccountingBookDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadAccountingBooksAsync();
                _notificationService.ShowSuccess(message.DeletedAccountingBook.Message);
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task HandleAsync(AccountingBookUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadAccountingBooksAsync();
                _notificationService.ShowSuccess(message.UpdatedAccountingBook.Message);
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task HandleAsync(AccountingBookCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadAccountingBooksAsync();
                _notificationService.ShowSuccess(message.CreatedAccountingBook.Message);
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
