using Caliburn.Micro;
using Common.Extensions;
using Common.Interfaces;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Billing;
using Models.Books;
using Models.Suppliers;
using NetErp.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Converters;

namespace NetErp.Billing.Customers.ViewModels
{
    public class CustomerMasterViewModel : Screen, 
        IHandle<CustomerDeleteMessage>, 
        IHandle<CustomerCreateMessage>, 
        IHandle<CustomerUpdateMessage>,
        IHandle<AccountingEntityUpdateMessage>,
        IHandle<SellerUpdateMessage>,
        IHandle<SupplierUpdateMessage>
    {

        public readonly IGenericDataAccess<CustomerGraphQLModel> CustomerService = IoC.Get<IGenericDataAccess<CustomerGraphQLModel>>();
        public CustomerViewModel Context { get; private set; }

        private CustomerDTO? _selectedCustomer;
        public CustomerDTO? SelectedCustomer
        {
            get { return _selectedCustomer; }
            set
            {
                if (_selectedCustomer != value)
                {
                    _selectedCustomer = value;
                    NotifyOfPropertyChange(nameof(SelectedCustomer));
                    NotifyOfPropertyChange(nameof(CanDeleteCustomer));
                }
            }
        }

        private ObservableCollection<CustomerDTO> _customers = [];
        public ObservableCollection<CustomerDTO> Customers
        {
            get { return _customers; }
            set
            {
                if (_customers != value)
                {
                    _customers = value;
                    NotifyOfPropertyChange(nameof(Customers));
                    NotifyOfPropertyChange(nameof(CanDeleteCustomer));
                }
            }
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

        public bool CanDeleteCustomer
        {
            get
            {
                if (SelectedCustomer is null) return false;
                return true;
            }
        }

        public bool CanCreateCustomer() => !IsBusy;

        public CustomerMasterViewModel(CustomerViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnUIThread(this);
            //_ = Task.Run(() => LoadCustomers());
        }

        private ICommand _createCustomerCommand;
        public ICommand CreateCustomerCommand
        {
            get
            {
                if (_createCustomerCommand is null) _createCustomerCommand = new AsyncCommand(CreateCustomer, CanCreateCustomer);
                return _createCustomerCommand;
            }

        }

        private ICommand _deleteCustomerCommand;
        public ICommand DeleteCustomerCommand
        {
            get
            {
                if (_deleteCustomerCommand is null) _deleteCustomerCommand = new AsyncCommand(DeleteCustomer, CanDeleteCustomer);
                return _deleteCustomerCommand;
            }
        }

        #region Metodos

        public async Task CreateCustomer()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteCreateCustomer());
                SelectedCustomer = null;
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

        public async Task ExecuteCreateCustomer()
        {
            await Context.ActivateDetailViewForNew();
        }

        public async Task EditCustomer()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteEditCustomer());
                SelectedCustomer = null;
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

        public async Task ExecuteEditCustomer()
        {
            await Context.ActivateDetailViewForEdit(SelectedCustomer);
        }

        public async Task LoadCustomers()
        {
            try
            {

                IsBusy = true;
                Refresh();

                // Iniciar cronometro
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string query = @"
                query ($filter: CustomerFilterInput) {
                  PageResponse: customerPage(filter: $filter) {
                    count
                    rows {
                      id
                      creditTerm
                      isTaxFree
                      isActive
                      blockingReason
                      retainsAnyBasis
                      sellerId      
                      entity {
                        id
                        identificationNumber
                        verificationDigit
                        captureType
                        searchName
                        firstName
                        middleName
                        firstLastName
                        middleLastName
                        businessName
                        phone1
                        phone2
                        cellPhone1
                        cellPhone2
                        telephonicInformation
                        address
                        identificationType {
                            id
                            name
                        }
                        country {
                          id
                          name
                        }
                        department {
                          id
                          name
                        }
                        city {
                          id
                          name
                        }
                        emails {
                          id
                          name
                          email
                          isCorporate
                          sendElectronicInvoice
                        }
                      }
                      retentions {
                        id
                        name
                        margin
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

                //filtro searchName
                variables.filter.or[0].searchName = new ExpandoObject();
                variables.filter.or[0].searchName.@operator = "like";
                variables.filter.or[0].searchName.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                //filtro identificationNumber
                variables.filter.or[1].identificationNumber = new ExpandoObject();
                variables.filter.or[1].identificationNumber.@operator = "like";
                variables.filter.or[1].identificationNumber.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                // Pagination
                variables.filter.Pagination = new ExpandoObject();
                variables.filter.Pagination.Page = PageIndex;
                variables.filter.Pagination.PageSize = PageSize;
                var result = await CustomerService.GetPage(query, variables);

                TotalCount = result.PageResponse.Count;
                Customers = new ObservableCollection<CustomerDTO>(Context.AutoMapper.Map<ObservableCollection<CustomerDTO>>(result.PageResponse.Rows));
                stopwatch.Stop();

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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCustomers" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteCustomer()
        {
            try
            {
                IsBusy = true;
                int id = SelectedCustomer.Id;

                string query = @"query($id:Int!){
                  CanDeleteModel: canDeleteCustomer(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await this.CustomerService.CanDelete(query, variables);

                if (validation.CanDelete) 
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {SelectedCustomer.Entity.SearchName}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }

                Refresh();

                CustomerGraphQLModel deletedCustomer = await ExecuteDeleteCustomer(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new CustomerDeleteMessage() { DeletedCustomer = Context.AutoMapper.Map<CustomerDTO>(deletedCustomer) });

                NotifyOfPropertyChange(nameof(CanDeleteCustomer));
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

        public async Task<CustomerGraphQLModel> ExecuteDeleteCustomer(int id)
        {
            try
            {
                string query = @"
                mutation ($id: Int!) {
                  DeleteResponse: deleteCustomer(id: $id) {
                    id
                    creditTerm
                    isTaxFree
                    isActive
                    blockingReason
                    retainsAnyBasis
                  }
                }";

                object variables = new { Id = id };
                CustomerGraphQLModel deletedCustomer = await CustomerService.Delete(query, variables);
                this.SelectedCustomer = null;
                return deletedCustomer;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task ExecuteChangeIndex()
        {
            await LoadCustomers();
        }

        private bool CanExecuteChangeIndex()
        {
            return true;
        }

        protected override void OnViewReady(object view)
        {
            if (Context.EnableOnViewReady is false) return;
            base.OnViewReady(view);
            _ = Task.Run(() => LoadCustomers());
            _ = this.SetFocus(nameof(FilterSearch));
        }

        public void OnChecked()
        {
            NotifyOfPropertyChange(nameof(CanDeleteCustomer));
        }

        public void OnUnchecked()
        {
            NotifyOfPropertyChange(nameof(CanDeleteCustomer));
        }

        public Task HandleAsync(CustomerDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                CustomerDTO customerToDelete = Customers.First(c => c.Id == message.DeletedCustomer.Id);
                if (customerToDelete != null) _ = Application.Current.Dispatcher.Invoke(() => Customers.Remove(customerToDelete));
                return LoadCustomers();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Task HandleAsync(CustomerCreateMessage message, CancellationToken cancellationToken)
        {
            return Task.FromResult(Customers = new ObservableCollection<CustomerDTO>(Context.AutoMapper.Map<ObservableCollection<CustomerDTO>>(message.Customers)));
        }

        public Task HandleAsync(CustomerUpdateMessage message, CancellationToken cancellationToken)
        {
            return Task.FromResult(Customers = new ObservableCollection<CustomerDTO>(Context.AutoMapper.Map<ObservableCollection<CustomerDTO>>(message.Customers)));
        }

        public Task HandleAsync(AccountingEntityUpdateMessage message, CancellationToken cancellationToken)
        {
            return LoadCustomers();
        }

        public Task HandleAsync(SellerUpdateMessage message, CancellationToken cancellationToken)
        {
            return LoadCustomers();
        }

        public Task HandleAsync(SupplierUpdateMessage message, CancellationToken cancellationToken)
        {
            return LoadCustomers();
        }

        #endregion

        #region Paginacion

        /// <summary>
        /// PageIndex
        /// </summary>
        private int _pageIndex = 1; // DefaultPageIndex = 1
        public int PageIndex
        {
            get { return _pageIndex; }
            set
            {
                if (_pageIndex != value)
                {
                    _pageIndex = value;
                    NotifyOfPropertyChange(() => PageIndex);
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
                if (_pageSize != value)
                {
                    _pageSize = value;
                    NotifyOfPropertyChange(() => PageSize);
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
                if (_totalCount != value)
                {
                    _totalCount = value;
                    NotifyOfPropertyChange(() => TotalCount);
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
                if (_paginationCommand == null) this._paginationCommand = new AsyncCommand(ExecuteChangeIndex, CanExecuteChangeIndex);
                return _paginationCommand;
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
                    NotifyOfPropertyChange(() => ResponseTime);
                }
            }
        }

        // Filtro de busqueda
        private string _filterSearch = "";
        public string FilterSearch
        {
            get { return _filterSearch; }
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(() => FilterSearch);
                    // Solo ejecutamos la busqueda si esta vacio el filtro o si hay por lo menos 3 caracteres digitados
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) 
                    {
                        PageIndex = 1;
                        _ = Task.Run(this.LoadCustomers);
                    };
                }
            }
        }

        #endregion
    }
}
