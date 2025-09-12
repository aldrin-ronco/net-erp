using Caliburn.Micro;
using Common.Extensions;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Books;
using NetErp.Helpers;
using System;
using Extensions.Books;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Dynamic;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Xpf;
using DevExpress.Xpf.Data;
using Services.Books.DAL.PostgreSQL;
using Common.Helpers;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;
using Models.Billing;
using Models.Suppliers;
using GraphQL.Client.Http;

namespace NetErp.Books.AccountingEntities.ViewModels
{
    public class AccountingEntityMasterViewModel : Screen, 
        IHandle<AccountingEntityCreateMessage>, 
        IHandle<AccountingEntityDeleteMessage>, 
        IHandle<AccountingEntityUpdateMessage>,
        IHandle<CustomerCreateMessage>,
        IHandle<CustomerUpdateMessage>,
        IHandle<SellerCreateMessage>,
        IHandle<SellerUpdateMessage>,
        IHandle<SupplierCreateMessage>,
        IHandle<SupplierUpdateMessage>
    {


        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AccountingEntityGraphQLModel> _accountingEntityService;

        // Context
        private AccountingEntityViewModel _context;
        public AccountingEntityViewModel Context
        {
            get { return _context; }
            set
            {
                if(_context != value)
                {
                    _context = value;
                    NotifyOfPropertyChange(nameof(Context));
                }
            }
        }

        /// <summary>
        /// Establece cuando la aplicacion esta ocupada
        /// </summary>
        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if(_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }       
            }
        }

        #region Paginacion
        /// <summary>
        /// PageIndex
        /// </summary>
        private int _pageIndex = 1; // DevExpress first page is index zero
        public int PageIndex
        {
            get { return _pageIndex; }
            set
            {
                if(_pageIndex != value)
                {
                    _pageIndex = value;
                    NotifyOfPropertyChange(nameof(PageIndex));
                }
            }
        }

        /// <summary>
        /// PageSize
        /// </summary>
        private int _pageSize = 50; // Default PageSize 50
        public int PageSize
        {
            get { return _pageSize; }
            set
            {
                if(_pageSize != value)
                {
                    _pageSize = value;
                    NotifyOfPropertyChange(nameof(PageSize));
                }
            }
            
        }


        /// <summary>
        /// TotalCount
        /// </summary>
        private int _totalCount = 0;
        public int TotalCount
        {
            get { return _totalCount; }
            set
            {
                if(_totalCount != value)
                {
                    _totalCount = value;
                    NotifyOfPropertyChange(nameof(TotalCount));
                }
            }
        }

        /// <summary>
        /// PaginationCommand para controlar evento
        /// </summary>
        private ICommand _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                if (_paginationCommand == null) this._paginationCommand = new AsyncCommand(ExecuteChangeIndexAsync, CanExecuteChangeIndex);
                return _paginationCommand;
            }
        }

        private ICommand _createAccountingEntityCommand;
        public ICommand CreateAccountingEntityCommand
        {
            get
            {
                if (_createAccountingEntityCommand is null) _createAccountingEntityCommand = new AsyncCommand(CreateAccountingEntityAsync, CanCreateAccountingEntity);
                return _createAccountingEntityCommand;
            }

        }

        private ICommand _deleteAccountingEntityCommand;
        public ICommand DeleteAccountingEntityCommand
        {
            get
            {
                if (_deleteAccountingEntityCommand is null) _deleteAccountingEntityCommand = new AsyncCommand(DeleteAccountingEntityAsync, CanDeleteAccountingEntity);
                return _deleteAccountingEntityCommand;
            }
        }

        #endregion

        #region Propiedades

        // Tiempo de respuesta
        private string _responseTime;
        public string ResponseTime
        {
            get { return _responseTime; }
            set
            {
                if (_responseTime != value)
                {
                    _responseTime = value;
                    NotifyOfPropertyChange(nameof(ResponseTime));
                }
            }
        }

        // Filtro de busqueda
        private string _filterSearch = "";
        public string FilterSearch
        {
            get 
            {
                if (_filterSearch is null) return string.Empty;
                return _filterSearch; 
            }
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(FilterSearch) || FilterSearch.Length >= 3)
                    {
                        IsBusy = true;
                        PageIndex = 1;
                        _ = Task.Run(() => LoadAccountingEntitiesAsync());
                        IsBusy = false;
                    };
                }                  
            }
        }

        public bool CanCreateAccountingEntity() => !IsBusy;

        #endregion

        #region Colecciones

        private AccountingEntityGraphQLModel? _selectedAccountingEntity;
        public AccountingEntityGraphQLModel? SelectedAccountingEntity
        {
            get { return _selectedAccountingEntity; }
            set
            {
                if (_selectedAccountingEntity != value)
                {
                    _selectedAccountingEntity = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingEntity));
                    NotifyOfPropertyChange(nameof(CanDeleteAccountingEntity));
                }
            }
        }

        private ObservableCollection<AccountingEntityGraphQLModel> _accountingEntities = [];
        public ObservableCollection<AccountingEntityGraphQLModel> AccountingEntities
        {
            get { return this._accountingEntities; }
            set
            {
                if (this._accountingEntities != value)
                {
                    this._accountingEntities = value;
                    NotifyOfPropertyChange(nameof(AccountingEntities));
                    NotifyOfPropertyChange(nameof(CanDeleteAccountingEntity));
                }
            }
        }

        #endregion



        protected override void OnViewReady(object view)
        {
            if (Context.EnableOnViewReady is false) return;
            base.OnViewReady(view);
            _ = Task.Run(() => LoadAccountingEntitiesAsync());
            _ = this.SetFocus(nameof(FilterSearch));
        }

        public AccountingEntityMasterViewModel(AccountingEntityViewModel context, Helpers.Services.INotificationService notificationService,
            IRepository<AccountingEntityGraphQLModel> accountingEntityService)
        {
            try
            {

                Context = context;
                _notificationService = notificationService;
                _accountingEntityService = accountingEntityService;

                Context.EventAggregator.SubscribeOnUIThread(this);
            }
            catch (Exception)
            {

                throw;
            }

        }

        private async Task ExecuteChangeIndexAsync()
        {
            IsBusy = true;
            await LoadAccountingEntitiesAsync();
            IsBusy = false;
        }
        private bool CanExecuteChangeIndex()
        {
            return true;
        }

        #region Metodos 

        public async Task LoadAccountingEntitiesAsync()
        {

            try
            {
                IsBusy = true;
                Refresh();
                string query = @"
                query ($filter: AccountingEntityFilterInput) {
                  PageResponse:accountingEntityPage(filter: $filter) {
		                count
                        rows {
                            id
                            identificationNumber
                            verificationDigit
                            captureType
                            businessName
                            firstName
                            middleName
                            firstLastName
                            middleLastName
                            phone1
                            phone2
                            cellPhone1
                            cellPhone2
                            address
                            regime
                            fullName
                            tradeName
                            searchName
                            telephonicInformation
                            commercialCode
                            identificationType {
                               id
                            }
                            country {
                               id 
                            }
                            department {
                               id
                            }
                            city {
                               id 
                            }
                            emails {
                              id
                              description
                              email
                            }
                        }
                    }
                 }";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.or = new ExpandoObject[]
                {
                    new(),
                    new()
                };

                //SearhName
                variables.filter.or[0].searchName = new ExpandoObject();
                variables.filter.or[0].searchName.@operator = "like";
                variables.filter.or[0].searchName.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                //IdentificationNumber
                variables.filter.or[1].identificationNumber = new ExpandoObject();
                variables.filter.or[1].identificationNumber.@operator = "like";
                variables.filter.or[1].identificationNumber.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                //Paginación
                variables.filter.Pagination = new ExpandoObject();
                variables.filter.Pagination.Page = PageIndex;
                variables.filter.Pagination.PageSize = PageSize;

                // Iniciar cronometro
                Stopwatch stopwatch = new();
                stopwatch.Start();

                var source = await _accountingEntityService.GetPageAsync(query, variables);
                TotalCount = source.Count;
                AccountingEntities = new ObservableCollection<AccountingEntityGraphQLModel>(source.Rows);
                stopwatch.Stop();

                // Detener cronometro
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadAccountingEntities" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditAccountingEntityAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteEditAccountingEntityAsync());
                SelectedAccountingEntity = null;
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "EditAccountingEntity" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ExecuteEditAccountingEntityAsync()
        {
            await Context.ActivateDetailViewForEdit(SelectedAccountingEntity);
        }

        public async Task CreateAccountingEntityAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteCreateAccountingEntityAsync());
                SelectedAccountingEntity = null;
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "EditCustomer" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ExecuteCreateAccountingEntityAsync()
        {
            await Context.ActivateDetailViewForNew(); // Mostrar la Vista
        }

        public async Task DeleteAccountingEntityAsync()
        {
            try
            {
                IsBusy = true;
                int id = SelectedAccountingEntity.Id;

                string query = @"
                query($id:Int!) {
                  CanDeleteModel: canDeleteAccountingEntity(id:$id) {
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await this._accountingEntityService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Atención!", text: $"¿Confirma que desea eliminar el registro {SelectedAccountingEntity.SearchName}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El tercero no puede ser eliminado" +
                        (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }


                Refresh();

                var deletedAccountingEntity = await ExecuteDeleteAccountingEntityAsync(id);

                await Context.EventAggregator.PublishOnCurrentThreadAsync(new AccountingEntityDeleteMessage() { DeletedAccountingEntity = deletedAccountingEntity});

                // Desactivar opcion de eliminar registros
                NotifyOfPropertyChange(nameof(CanDeleteAccountingEntity));

                
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
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteAccountingEntity" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }

        }

        public async Task<AccountingEntityGraphQLModel> ExecuteDeleteAccountingEntityAsync(int id)
        {
            try
            {
                string query = @"
                mutation ($id: Int!) {
                  DeleteResponse: deleteAccountingEntity(id: $id) {
                    id
                  }
                }";
                object variables = new { Id = id };
                var deletedEntity = await this._accountingEntityService.DeleteAsync(query, variables);
                this.SelectedAccountingEntity = null;
                return deletedEntity;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task HandleAsync(AccountingEntityCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadAccountingEntitiesAsync();
                _notificationService.ShowSuccess("Tercero creado correctamente", "Éxito");
                return;
            }
            catch (Exception)
            {

                throw;
            }
        }


        public async Task HandleAsync(AccountingEntityUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadAccountingEntitiesAsync();
                _notificationService.ShowSuccess("Tercero actualizado correctamente", "Éxito");
                return;
            }
            catch (Exception)
            {

                throw;
            }
        }
        public Task HandleAsync(AccountingEntityDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                AccountingEntityGraphQLModel accountingAccountToDelete = AccountingEntities.First(c => c.Id == message.DeletedAccountingEntity.Id);
                if (accountingAccountToDelete != null) _ = Application.Current.Dispatcher.Invoke(() => AccountingEntities.Remove(accountingAccountToDelete));
                _notificationService.ShowSuccess("Tercero eliminado correctamente", "Éxito");
                return LoadAccountingEntitiesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Task HandleAsync(CustomerCreateMessage message, CancellationToken cancellationToken)
        {
            return LoadAccountingEntitiesAsync();
        }

        public Task HandleAsync(CustomerUpdateMessage message, CancellationToken cancellationToken)
        {
            return LoadAccountingEntitiesAsync();
        }

        public Task HandleAsync(SellerCreateMessage message, CancellationToken cancellationToken)
        {
            return LoadAccountingEntitiesAsync();
        }

        public Task HandleAsync(SellerUpdateMessage message, CancellationToken cancellationToken)
        {
            return LoadAccountingEntitiesAsync();
        }

        public Task HandleAsync(SupplierCreateMessage message, CancellationToken cancellationToken)
        {
            return LoadAccountingEntitiesAsync();
        }

        public Task HandleAsync(SupplierUpdateMessage message, CancellationToken cancellationToken)
        {
            return LoadAccountingEntitiesAsync();
        }

        public bool CanDeleteAccountingEntity
        {
            get
            {
                if (SelectedAccountingEntity is null) return false;
                return true;
            }
        }

        #endregion
    }
}
