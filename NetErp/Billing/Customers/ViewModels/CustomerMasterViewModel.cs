using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
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
using NetErp.Helpers.GraphQLQueryBuilder;
using static Models.Global.GraphQLResponseTypes;

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

        private readonly IRepository<CustomerGraphQLModel> _customerService;
        private readonly Helpers.Services.INotificationService _notificationService;
        public CustomerViewModel Context { get; private set; }

        private CustomerGraphQLModel? _selectedCustomer;
        public CustomerGraphQLModel? SelectedCustomer
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

        private ObservableCollection<CustomerGraphQLModel> _customers = [];
        public ObservableCollection<CustomerGraphQLModel> Customers
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

        public CustomerMasterViewModel(CustomerViewModel context,
                                      Helpers.Services.INotificationService notificationService,
                                      IRepository<CustomerGraphQLModel> customerService)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            
            Context.EventAggregator.SubscribeOnUIThread(this);
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
                SelectedCustomer = null;
                await Context.ActivateDetailViewForNewAsync();
            }
            catch(AsyncException ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditCustomer()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Context.ActivateDetailViewForEditAsync(SelectedCustomer!.Id);
                SelectedCustomer = null;
            }
            catch (AsyncException ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            finally
            {
                IsBusy = false;
            }
        }


        public async Task LoadCustomersAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();

                // Iniciar cronometro
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string query = GetLoadCustomersQuery();

                dynamic variables = new ExpandoObject();
                variables.PageResponsePagination = new ExpandoObject();
                variables.PageResponsePagination.Page = PageIndex;
                variables.PageResponsePagination.PageSize = PageSize;
                variables.PageResponseFilters = new ExpandoObject();

                // Aplicar filtro de clientes activos si está habilitado
                if (ShowActiveCustomersOnly)
                {
                    variables.PageResponseFilters.isActive = true;
                }

                if(!string.IsNullOrEmpty(FilterSearch))
                {
                    variables.PageResponseFilters.matching = FilterSearch.Trim().RemoveExtraSpaces();
                }

                var result = await _customerService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                Customers = new ObservableCollection<CustomerGraphQLModel>(result.Entries);
                stopwatch.Stop();

                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
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
                if (SelectedCustomer is null) return;
                int id = SelectedCustomer.Id;
                IsBusy = true;
                Refresh();

                string query = GetCanDeleteCustomerQuery();

                object variables = new { canDeleteResponseId = id };

                CanDeleteType validation = await _customerService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    if (ThemedMessageBox.Show("Atención!", "¿Confirma que desea eliminar el registro seleccionado?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                }
                else
                {
                    IsBusy = false;
                    ThemedMessageBox.Show("Atención!", "El registro no puede ser eliminado" +
                        (char)13 + (char)13 + validation.Message, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                DeleteResponseType deletedCustomer = await Task.Run(() => this.ExecuteDeleteCustomer(id));

                if (!deletedCustomer.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedCustomer.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await Context.EventAggregator.PublishOnUIThreadAsync(new CustomerDeleteMessage { DeletedCustomer = deletedCustomer });

                NotifyOfPropertyChange(nameof(CanDeleteCustomer));
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención!", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención!", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<DeleteResponseType> ExecuteDeleteCustomer(int id)
        {
            try
            {
                string query = GetDeleteCustomerQuery();

                object variables = new { deleteResponseId = id };

                DeleteResponseType deletedRecord = await _customerService.DeleteAsync<DeleteResponseType>(query, variables);
                return deletedRecord;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task ExecuteChangeIndex()
        {
            await LoadCustomersAsync();
        }

        private bool CanExecuteChangeIndex()
        {
            return true;
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = LoadCustomersAsync();
            _ = this.SetFocus(nameof(FilterSearch));
        }

        public async Task HandleAsync(CustomerDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                // ✅ Optimización: Remover de la lista en lugar de recargar todo
                // Es seguro porque solo estamos eliminando un item visible
                var deletedCustomer = Customers.FirstOrDefault(c => c.Id == message.DeletedCustomer.DeletedId);
                if (deletedCustomer != null)
                {
                    Customers.Remove(deletedCustomer);
                    TotalCount--;
                }
                else
                {
                    // Si no está en la lista visible, recargar para actualizar el TotalCount
                    await LoadCustomersAsync();
                }

                _notificationService.ShowSuccess(message.DeletedCustomer.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task HandleAsync(CustomerCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadCustomersAsync();
                _notificationService.ShowSuccess(message.CreatedCustomer.Message);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task HandleAsync(CustomerUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadCustomersAsync();
                _notificationService.ShowSuccess(message.UpdatedCustomer.Message);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public Task HandleAsync(AccountingEntityUpdateMessage message, CancellationToken cancellationToken)
        {
            return LoadCustomersAsync();
        }

        public Task HandleAsync(SellerUpdateMessage message, CancellationToken cancellationToken)
        {
            return LoadCustomersAsync();
        }

        public Task HandleAsync(SupplierUpdateMessage message, CancellationToken cancellationToken)
        {
            return LoadCustomersAsync();
        }

        #endregion

        #region QueryBuilder Methods

        public string GetLoadCustomersQuery()
        {
            var fields = FieldSpec<PageType<CustomerGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.IsActive)
                    .Select(selector: e => e.AccountingEntity, nested: entity => entity
                        .Field(en => en.IdentificationNumber)
                        .Field(en => en.VerificationDigit)
                        .Field(en => en.SearchName)
                        .Field(en => en.TelephonicInformation)
                        .Field(en => en.Address)))
                .Build();

            var filterParameter = new GraphQLQueryParameter("filters", "CustomerFilters");
            var paginationParameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("customersPage", [filterParameter, paginationParameter], fields, "PageResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        public string GetCanDeleteCustomerQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("canDeleteCustomer", [parameter], fields, "CanDeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        public string GetDeleteCustomerQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("deleteCustomer", [parameter], fields, "DeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
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
                        _ = LoadCustomersAsync();
                    }
                }
            }
        }

        // Filtro mostrar solo clientes activos
        private bool _showActiveCustomersOnly = true;
        public bool ShowActiveCustomersOnly
        {
            get => _showActiveCustomersOnly;
            set
            {
                if (_showActiveCustomersOnly != value)
                {
                    _showActiveCustomersOnly = value;
                    NotifyOfPropertyChange(nameof(ShowActiveCustomersOnly));
                    _ = LoadCustomersAsync();
                }
            }
        }

        #endregion

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            // Desuscribirse del EventAggregator para evitar memory leaks
            Context.EventAggregator.Unsubscribe(this);
            
            // Limpiar colecciones
            Customers.Clear();
            
            return base.OnDeactivateAsync(close, cancellationToken);
        }
    }
}
