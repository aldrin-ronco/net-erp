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

namespace NetErp.Books.AccountingBooks.ViewModels
{
    public class AccountingBookMasterViewModel: Screen,
        IHandle<AccountingBookDeleteMessage>,
        IHandle<AccountingBookUpdateMessage>,
        IHandle<AccountingBookCreateMessage>
    {
        public IGenericDataAccess<AccountingBookGraphQLModel> AccountingBookService { get; set; } = IoC.Get<IGenericDataAccess<AccountingBookGraphQLModel>>();
        private readonly Helpers.Services.INotificationService _notificationService = IoC.Get<Helpers.Services.INotificationService>();
        public AccountingBookMasterViewModel(AccountingBookViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnPublishedThread(this);               
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
            await Context.ActivateDetailViewForNew();
        }
        public async Task LoadAccountingBooksAsync()
        {
            try
            {
                IsBusy = true;
                string query;
                query = @"
                query ($filter: AccountingBookFilterInput) {
                  ListResponse:accountingBooks(filter: $filter) {
                    id
                    name
                  }
                }";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.name = new ExpandoObject();
                variables.filter.name.@operator = "like";
                variables.filter.name.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();
                var result = await AccountingBookService.GetList(query, variables);
                AccountingBooks = new ObservableCollection<AccountingBookGraphQLModel>(result);
                IsBusy = false;     
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en LoadAccountingBooksAsync: {ex.Message}"); ;
            }         
            finally
            {
                IsBusy = false;
            }
        }
        public async Task DeleteAccountingBook()
        {
            try
            {
                IsBusy = true;
                int id = SelectedItem.Id;
                string query = @"query($id:Int!){
                CanDeleteModel: canDeleteAccountingBook(id: $id){
                    canDelete
                    message
                    }
                }";                
                object variables = new { Id = id };
                var validation = await this.AccountingBookService.CanDelete(query, variables);
                if (validation.CanDelete)
                {
                    IsBusy = false; 
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {SelectedItem.Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }
                this.IsBusy = true;
                Refresh();
                AccountingBookGraphQLModel deletedAccountingBook = await ExecuteDeleteAccountingBookAsync(id);
                await Context.EventAggregator.PublishOnUIThreadAsync(new AccountingBookDeleteMessage() { DeletedAccountingBook = deletedAccountingBook });
                NotifyOfPropertyChange(nameof(CanDeleteAccountingBook));

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteCustomer" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }        
        public async Task EditAccountingBook()
        {
            await Context.ActivateDetailViewForEdit(SelectedItem ?? new ());

        }
        public async Task<AccountingBookGraphQLModel> ExecuteDeleteAccountingBookAsync(int id)
        {
            try
            {
                string query = @"mutation($id: Int!){
                DeleteResponse: deleteAccountingBook(id: $id){
                    id
                    name
                    }
                }";
                dynamic variables = new ExpandoObject();
                variables.id = id;
                var result = await AccountingBookService.Delete(query, variables);
                return result;
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
                _notificationService.ShowSuccess("Libro contable eliminado correctamente", "Éxito");
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
                _notificationService.ShowSuccess("Libro contable actualizado correctamente", "Éxito");
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
                _notificationService.ShowSuccess("Libro contable creado correctamente", "Éxito");
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
