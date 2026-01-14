
using Caliburn.Micro;
using Common.Helpers;
using DevExpress.Mvvm;
using Common.Interfaces;
using GraphQL.Client.Http;
using Models.Suppliers;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Common.Extensions;
using NetErp.Helpers;
using DevExpress.Xpf.Core;
using System.Dynamic;
using Models.Books;
using static Models.Global.GraphQLResponseTypes;
using NetErp.Helpers.GraphQLQueryBuilder;


namespace NetErp.Suppliers.Suppliers.ViewModels
{
    public class SupplierMasterViewModel : Screen,
        IHandle<SupplierCreateMessage>,
        IHandle<SupplierUpdateMessage>,
        IHandle<SupplierDeleteMessage>
       
    {

        #region Properties

        private readonly IRepository<SupplierGraphQLModel> _supplierService;
        private readonly Helpers.Services.INotificationService _notificationService;
        public SupplierViewModel Context { get; private set; }

        private ICommand checkRowCommand;
        public ICommand CheckRowCommand
        {
            get
            {
                if (checkRowCommand is null) checkRowCommand = new RelayCommand(CanCheckRow, CheckRow);
                return checkRowCommand;
            }
        }

        private ICommand _createSupplierCommand;
        public ICommand CreateSupplierCommand
        {
            get
            {
                if (_createSupplierCommand is null) _createSupplierCommand = new AsyncCommand(CreateSupplierAsync, CanCreateSupplier);
                return _createSupplierCommand;
            }

        }

        private ICommand _deleteSupplierCommand;
        public ICommand DeleteSupplierCommand
        {
            get
            {
                if (_deleteSupplierCommand is null) _deleteSupplierCommand = new AsyncCommand(DeleteSupplierAsync, CanDeleteSupplier);
                return _deleteSupplierCommand;
            }
        }

        private bool _isBusy = true;
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

        private SupplierDTO _selectedSupplier;
        public SupplierDTO SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                _selectedSupplier = value;
                NotifyOfPropertyChange(nameof(SelectedSupplier));
                NotifyOfPropertyChange(nameof(CanDeleteSupplier));
            }
        }

        private ObservableCollection<SupplierDTO> _suppliers;
        public ObservableCollection<SupplierDTO> Suppliers
        {
            get => _suppliers;
            set
            {
                _suppliers = value;
                NotifyOfPropertyChange(nameof(Suppliers));
            }
        }

        private ObservableCollection<AccountingAccountGraphQLModel> _accountingAccounts;

        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccounts
        {
            get { return _accountingAccounts; }
            set
            {
                if (_accountingAccounts != value)
                {
                    _accountingAccounts = value;
                    NotifyOfPropertyChange(nameof(AccountingAccounts));
                }
            }
        }
        private ObservableCollection<WithholdingTypeDTO> _withholdingTypes;
        public ObservableCollection<WithholdingTypeDTO> WithholdingTypes
        {
            get => _withholdingTypes;
            set
            {
                if (_withholdingTypes != value)
                {
                    _withholdingTypes = value;
                    NotifyOfPropertyChange(nameof(WithholdingTypes));
                }
            }
        }

       
        public bool CanEditSupplier => true;

        public bool CanDeleteSupplier
        {
            get
            {
                if (SelectedSupplier is null) return false;
                return true;
            }
        }

        #endregion

        #region Methods

        public bool CanEditRecord()
        {
            return true;
        }

        public async Task EditSupplier()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteEditSupplier());
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

        public bool CanCheckRow(object p)
        {
            return true;
        }

        public void CheckRow(object p)
        {
            NotifyOfPropertyChange(nameof(CanDeleteSupplier));
        }

        public void OnChecked()
        {
            NotifyOfPropertyChange(nameof(CanDeleteSupplier));
        }

        public void OnUnchecked()
        {
            NotifyOfPropertyChange(nameof(CanDeleteSupplier));
        }

      
        
        public async Task DeleteSupplierAsync()
        {
            try
            {

                this.IsBusy = true;
                this.Refresh();

                string query = GetCanDeleteSupplierQuery();

                object variables = new { canDeleteResponseId = SelectedSupplier.Id };

                var validation = await _supplierService.CanDeleteAsync(query, variables);

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
                DeleteResponseType deletedSuplier = await Task.Run(() => this.ExecuteDeleteSupplierAsync(SelectedSupplier.Id));

                if (!deletedSuplier.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedSuplier.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await Context.EventAggregator.PublishOnUIThreadAsync(new SupplierDeleteMessage { DeletedSupplier = deletedSuplier });

                NotifyOfPropertyChange(nameof(CanDeleteSupplier));
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
        public string GetCanDeleteSupplierQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("canDeleteSupplier", [parameter], fields, alias: "CanDeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }
        public async Task<DeleteResponseType> ExecuteDeleteSupplierAsync(int id)
        {
            try
            {

                string query = GetDeleteSupplierQuery();

                object variables = new
                {
                    deleteResponseId = id
                };

                // Eliminar registros
                DeleteResponseType deletedRecord = await _supplierService.DeleteAsync<DeleteResponseType>(query, variables);
                return deletedRecord;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public string GetDeleteSupplierQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("deleteSupplier", [parameter], fields, alias: "DeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public bool CanCreateSupplier() => !IsBusy;

        public async Task CreateSupplierAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteCreateSupplierAsync());
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ExecuteCreateSupplierAsync()
        {
            await Context.ActivateDetailViewForNewAsync(AccountingAccounts);
        }

        private bool CanExecuteChangeIndex()
        {
            return true;
        }
      
        public async Task LoadSuppliersAsync(bool withDependencies = false)
        {
            try
            {
                IsBusy = true;
                Refresh();
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string query = GetLoadSuppliersDataQuery(withDependencies);

                dynamic variables = new ExpandoObject();

                variables.pageResponseFilters = new ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch))
                {
                    variables.pageResponseFilters.Matching = FilterSearch;
                }


                //Paginación
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.Page = PageIndex;
                variables.pageResponsePagination.PageSize = PageSize;

                
                if (withDependencies)
                {
                    
                    variables.accountingAccountsFilters = new ExpandoObject();
                    variables.accountingAccountsFilters.only_auxiliary_accounts = true;
                    SupplierDataContext result = await _supplierService.GetDataContextAsync<SupplierDataContext>(query, variables);
                    TotalCount = result.Suppliers.TotalEntries;
                    Suppliers = Context.AutoMapper.Map<ObservableCollection<SupplierDTO>>(result.Suppliers.Entries);
                    AccountingAccounts = result.AccountingAccounts.Entries;


                }
                else
                {
                    PageType<SupplierGraphQLModel> result = await _supplierService.GetPageAsync(query, variables);
                    TotalCount = result.TotalEntries;
                    Suppliers = Context.AutoMapper.Map<ObservableCollection<SupplierDTO>>(result.Entries);

                }


                stopwatch.Stop();
               
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

            }
            catch (Exception ex)
            {

                throw new AsyncException(innerException: ex);
            }
            finally
            {
                IsBusy = false;
            }
           
        }
        public string GetLoadSuppliersDataQuery(bool withDependencies = false)
        {
            var suppliersFields = FieldSpec<PageType<SupplierGraphQLModel>>
             .Create()
             .SelectList(it => it.Entries, entries => entries
                 .Field(e => e.Id)
                 .Field(e => e.IsTaxFree)
                 .Field(e => e.IcaWithholdingRate)
                
                      
                 .Select(e => e.IcaAccountingAccount, acc => acc
                           .Field(c => c.Id)
                           .Field(c => c.Code)
                           .Field(c => c.Name)
                 )
                 .Select(e => e.AccountingEntity, acc => acc
                           .Field(c => c.Id)
                           .Field(c => c.IdentificationNumber)
                           .Field(c => c.VerificationDigit)
                           .Field(c => c.CaptureType)
                           .Field(c => c.SearchName)
                           .Field(c => c.FirstLastName)
                           .Field(c => c.MiddleLastName)
                           .Field(c => c.BusinessName)
                           .Field(c => c.PrimaryPhone)
                           .Field(c => c.SecondaryPhone)
                           .Field(c => c.PrimaryCellPhone)
                           .Field(c => c.SecondaryCellPhone)
                           .Field(c => c.Address)
                           .Field(c => c.TelephonicInformation)
                           .SelectList(e => e.Emails, acc => acc
                               .Field(c => c.Id)
                               .Field(c => c.Email)
                               .Field(c => c.Description)
                               
                               .Field(c => c.isElectronicInvoiceRecipient)
                           

                           )
                 )
                 /*.SelectList(e => e.Retentions, acc => acc
                           .Field(c => c.Id)
                           
                           .Field(c => c.Name)
                 )*/


             )
             .Field(o => o.PageNumber)
             .Field(o => o.PageSize)
             .Field(o => o.TotalPages)
             .Field(o => o.TotalEntries)
             .Build();

           
            var withholdingTypeFields = FieldSpec<PageType<WithholdingTypeGraphQLModel>>
             .Create()
             .SelectList(it => it.Entries, entries => entries
                 .Field(e => e.Id)
                 .Field(e => e.Name)
                
             )
             .Field(o => o.PageNumber)
             .Field(o => o.PageSize)
             .Field(o => o.TotalPages)
             .Field(o => o.TotalEntries)
             .Build();

            var accountingAccountFields = FieldSpec<PageType<AccountingAccountGraphQLModel>>
               .Create()
               .SelectList(it => it.Entries, entries => entries
                   .Field(e => e.Id)
                   .Field(e => e.Name)
                   .Field(e => e.Code)
                   )
               .Build();
            var accountingAccountParameters = new GraphQLQueryParameter("filters", "AccountingAccountFilters");
            var AccountingAccountFragment = new GraphQLQueryFragment("accountingAccountsPage", [accountingAccountParameters], accountingAccountFields, "AccountingAccounts");

            var suppliersPagParameters = new GraphQLQueryParameter("pagination", "Pagination");
            var suppliersParameters = new GraphQLQueryParameter("filters", "SupplierFilters");
            var suppliersFragment = new GraphQLQueryFragment("suppliersPage", [suppliersPagParameters, suppliersParameters], suppliersFields, withDependencies ? "suppliers" : "pageResponse");




            var builder = withDependencies? new GraphQLQueryBuilder([suppliersFragment, AccountingAccountFragment]) : new GraphQLQueryBuilder([suppliersFragment]);
            return builder.GetQuery();

           
        }
        private async Task ExecuteChangeIndexAsync()
        {
            await LoadSuppliersAsync();
        }

        public SupplierMasterViewModel(
            SupplierViewModel context,
            IRepository<SupplierGraphQLModel> supplierService,
            Helpers.Services.INotificationService notificationService)
        {
            Context = context;
            _supplierService = supplierService;
            _notificationService = notificationService;
            Context.EventAggregator.SubscribeOnUIThread(this);
            _ = Task.Run(() => LoadSuppliersAsync(true));
        }

        protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                Context.EventAggregator.Unsubscribe(this);
            }
           
            await base.OnDeactivateAsync(close, cancellationToken);
        }

        public async Task ExecuteEditSupplier()
        {
            await Context.ActivateDetailViewForEditAsync(SelectedSupplier.Id, AccountingAccounts);
        }

        public async Task HandleAsync(SupplierCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadSuppliersAsync();
                _notificationService.ShowSuccess(message.CreatedSupplier.Message, "Éxito");
                return;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task HandleAsync(SupplierUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadSuppliersAsync();
                _notificationService.ShowSuccess(message.UpdatedSupplier.Message, "Éxito");
                return;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public Task HandleAsync(SupplierDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                _notificationService.ShowSuccess(message.DeletedSupplier.Message, "Éxito");
                return LoadSuppliersAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }

        protected override void OnViewReady(object view)
        {
            if (Context.EnableOnViewReady is false) return;
            base.OnViewReady(view);
           
            _ = this.SetFocus(nameof(FilterSearch));
        }

       

        #endregion

        #region Paginacion

        /// <summary>
        /// PageIndex
        /// </summary>

        private int _pageIndex = 1; // DefaultPageIndex = 1
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

        /// <summary>
        /// PageSize
        /// </summary>
        private int _pageSize = 50; // Default PageSize 50
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

        /// <summary>
        /// TotalCount
        /// </summary>
        private int _totalCount = 0;
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

        /// <summary>
        /// PaginationCommand para controlar evento
        /// </summary>
        private ICommand _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                if (_paginationCommand == null) _paginationCommand = new AsyncCommand(ExecuteChangeIndexAsync, CanExecuteChangeIndex);
                return _paginationCommand;
            }
        }

        #endregion

        #region Propiedades

        // Tiempo de respuesta
        private string _responseTime;
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

        // Filtro de busqueda
        private string _filterSearch = "";
        public string FilterSearch
        {
            get 
            {
                if (_filterSearch is null) return "";
                return _filterSearch;
            } 
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    // Solo ejecutamos la busqueda si esta vacio el filtro o si hay por lo menos 3 caracteres digitados
                    if (string.IsNullOrEmpty(value) || value.Length >= 3)
                    {
                        PageIndex = 1;
                        _ = Task.Run(() => LoadSuppliersAsync());
                    }
                }
            }
        }

        #endregion
    }
}
