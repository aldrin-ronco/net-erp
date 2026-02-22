using Caliburn.Micro;
using Common.Extensions;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Books;
using NetErp.Helpers;
using System;
using Extensions.Books;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Dynamic;
using Common.Helpers;
using Models.Billing;
using Models.Suppliers;
using GraphQL.Client.Http;
using NetErp.Helpers.GraphQLQueryBuilder;
using static Models.Global.GraphQLResponseTypes;

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
        private AccountingEntityViewModel _context = null!;
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
        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                if (_paginationCommand == null) this._paginationCommand = new AsyncCommand(ExecuteChangeIndexAsync, CanExecuteChangeIndex);
                return _paginationCommand;
            }
        }

        private ICommand? _createAccountingEntityCommand;
        public ICommand CreateAccountingEntityCommand
        {
            get
            {
                if (_createAccountingEntityCommand is null) _createAccountingEntityCommand = new AsyncCommand(CreateAccountingEntityAsync, CanCreateAccountingEntity);
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
                if (_deleteAccountingEntityCommand is null) _deleteAccountingEntityCommand = new AsyncCommand(DeleteAccountingEntityAsync, CanDeleteAccountingEntity);
                return _deleteAccountingEntityCommand;
            }
        }

        #endregion

        #region Propiedades

        // Tiempo de respuesta
        private string _responseTime = string.Empty;
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
                    NotifyOfPropertyChange(nameof(CanEditAccountingEntity));
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
            base.OnViewReady(view);
            _ = LoadAccountingEntitiesAsync();
            _ = this.SetFocus(nameof(FilterSearch));
        }

        public AccountingEntityMasterViewModel(AccountingEntityViewModel context, Helpers.Services.INotificationService notificationService,
            IRepository<AccountingEntityGraphQLModel> accountingEntityService)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _accountingEntityService = accountingEntityService ?? throw new ArgumentNullException(nameof(accountingEntityService));
            Context.EventAggregator.SubscribeOnUIThread(this);
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

                // Iniciar cronometro
                Stopwatch stopwatch = new();
                stopwatch.Start();

                string query = GetLoadAccountingEntitiesQuery();

                dynamic variables = new ExpandoObject();
                variables.PageResponsePagination = new ExpandoObject();
                variables.PageResponsePagination.Page = PageIndex;
                variables.PageResponsePagination.PageSize = PageSize;
                variables.PageResponseFilters = new ExpandoObject();

                if (!string.IsNullOrEmpty(FilterSearch))
                {
                    variables.PageResponseFilters.matching = FilterSearch.Trim().RemoveExtraSpaces();
                }

                var result = await _accountingEntityService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                AccountingEntities = new ObservableCollection<AccountingEntityGraphQLModel>(result.Entries);
                stopwatch.Stop();

                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    await Execute.OnUIThreadAsync(() =>
                    {
                        ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                        return Task.CompletedTask;
                    });
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadAccountingEntities" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
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
                await Context.ActivateDetailViewForEditAsync(SelectedAccountingEntity!.Id);
                SelectedAccountingEntity = null;
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();

                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"{GetType().Name}.{(currentMethod is null ? "EditAccountingEntityAsync" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            finally
            {
                IsBusy = false;
            }
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
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "EditCustomer" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ExecuteCreateAccountingEntityAsync()
        {
            try
            {
                await Context.ActivateDetailViewForNewAsync();
            }
            catch (Exception ex)
            {
                // Construir mensaje completo recorriendo la cadena de excepciones
                StringBuilder fullMessage = new();
                Exception? currentEx = ex;
                
                while (currentEx is not null)
                {
                    fullMessage.AppendLine(currentEx.Message);
                    currentEx = currentEx.InnerException;
                }

                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(
                        title: "Error",
                        text: fullMessage.ToString().TrimEnd(),
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }

        public async Task DeleteAccountingEntityAsync()
        {
            try
            {
                if (SelectedAccountingEntity is null) return;
                int id = SelectedAccountingEntity.Id;
                IsBusy = true;
                Refresh();

                string query = GetCanDeleteAccountingEntityQuery();

                object variables = new { canDeleteResponseId = id };

                CanDeleteType validation = await _accountingEntityService.CanDeleteAsync(query, variables);

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
                DeleteResponseType deletedAccountingEntity = await Task.Run(() => this.ExecuteDeleteAccountingEntityAsync(id));

                if (!deletedAccountingEntity.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedAccountingEntity.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await Context.EventAggregator.PublishOnUIThreadAsync(new AccountingEntityDeleteMessage { DeletedAccountingEntity = deletedAccountingEntity });

                NotifyOfPropertyChange(nameof(CanDeleteAccountingEntity));
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                string contentString = exGraphQL.Content?.ToString() ?? string.Empty;
                GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(contentString);
                
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show("Atención!",
                    $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()?.Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError?.Errors[0].Message}",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show("Atención!",
                    $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()?.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<DeleteResponseType> ExecuteDeleteAccountingEntityAsync(int id)
        {
            string query = GetDeleteAccountingEntityQuery();
            object variables = new { deleteResponseId = id };
            DeleteResponseType deletedRecord = await _accountingEntityService.DeleteAsync<DeleteResponseType>(query, variables);
            return deletedRecord;
        }

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
            // ✅ Optimización: Remover de la lista en lugar de recargar todo
            var deletedEntity = AccountingEntities.FirstOrDefault(c => c.Id == message.DeletedAccountingEntity.DeletedId);
            if (deletedEntity != null)
            {
                AccountingEntities.Remove(deletedEntity);
                TotalCount--;
            }
            else
            {
                // Si no está en la lista visible, recargar para actualizar el TotalCount
                await LoadAccountingEntitiesAsync();
            }

            _notificationService.ShowSuccess(message.DeletedAccountingEntity.Message);
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

        public bool CanEditAccountingEntity => SelectedAccountingEntity != null;

        public bool CanDeleteAccountingEntity
        {
            get
            {
                if (SelectedAccountingEntity is null) return false;
                return true;
            }
        }

        #endregion

        #region QueryBuilder Methods

        public string GetLoadAccountingEntitiesQuery()
        {
            var fields = FieldSpec<PageType<AccountingEntityGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.IdentificationNumber)
                    .Field(e => e.VerificationDigit)
                    .Field(e => e.SearchName)
                    .Field(e => e.TelephonicInformation)
                    .Field(e => e.Address))
                .Build();

            var filterParameter = new GraphQLQueryParameter("filters", "AccountingEntityFilters");
            var paginationParameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("accountingEntitiesPage", [filterParameter, paginationParameter], fields, "PageResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        public string GetCanDeleteAccountingEntityQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("canDeleteAccountingEntity", [parameter], fields, "CanDeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        public string GetDeleteAccountingEntityQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("deleteAccountingEntity", [parameter], fields, "DeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        #endregion
    }
}
