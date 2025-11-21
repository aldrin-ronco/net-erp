using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Controls;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using Models.Suppliers;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using Services.Books.DAL.PostgreSQL;
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
using static Models.Global.GraphQLResponseTypes;
using static NetErp.Books.AccountingSources.ViewModels.AccountingSourceDetailViewModel;

namespace NetErp.Books.AccountingSources.ViewModels
{
    public class AccountingSourceMasterViewModel : Screen, IHandle<AccountingSourceCreateMessage>, IHandle<AccountingSourceUpdateMessage>, IHandle<AccountingSourceDeleteMessage>
    {

        private readonly Helpers.Services.INotificationService _notificationService;

        private readonly IRepository<AccountingSourceGraphQLModel> _accountingSourceService;
        // Context
        private AccountingSourceViewModel _context;
        public AccountingSourceViewModel Context
        {
            get { return _context; }
            set
            {
                if (_context != value)
                {
                    _context = value;
                    NotifyOfPropertyChange(nameof(Context));
                }
            }
        }
        private ObservableCollection<ProcessTypeGraphQLModel> _processTypes;
        public ObservableCollection<ProcessTypeGraphQLModel> ProcessTypes
        {
            get => _processTypes;
            set
            {
                if (_processTypes != value)
                {
                    _processTypes = value;
                    NotifyOfPropertyChange(nameof(ProcessTypes));
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
                if (_isBusy != value)
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
        private int _pageIndex = 1; // DefaultPageIndex = 1
        public int PageIndex
        {
            get { return _pageIndex; }
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
            get { return _pageSize; }
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
            get { return _totalCount; }
            set
            {
                if (_totalCount != value)
                {
                    _totalCount = value;
                    NotifyOfPropertyChange(nameof(TotalCount));
                }
            }
        }

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

        /// <summary>
        /// PaginationCommand para controlar evento
        /// </summary>
        private ICommand _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                if (_paginationCommand == null) this._paginationCommand = new RelayCommand(CanExecuteChangeIndex, ExecuteChangeIndex);
                return _paginationCommand;
            }
        }

        #endregion

        #region Propiedades

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
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(FilterSearch) || FilterSearch.Length >= 3) _ = Task.Run(LoadAccountingSourcesAsync);
                }
            }
        }

        // SelectedGlobalModuleId
        private int _selectedModuleId = 0;
        public int SelectedModuleId
        {
            get { return _selectedModuleId; }
            set
            {
                if (_selectedModuleId != value)
                {
                    _selectedModuleId = value;
                    NotifyOfPropertyChange(nameof(SelectedModuleId));
                    _ = Task.Run(LoadAccountingSourcesAsync);
                }
            }
        }

        public bool CanCreateSource() => !IsBusy;

        private ICommand _createSellerCommand;
        public ICommand CreateSellerCommand
        {
            get
            {
                if (_createSellerCommand is null) _createSellerCommand = new AsyncCommand(CreateSourceAsync, CanCreateSource);
                return _createSellerCommand;
            }

        }

        private ICommand _deleteSellerCommand;
        public ICommand DeleteSellerCommand
        {
            get
            {
                if (_deleteSellerCommand is null) _deleteSellerCommand = new AsyncCommand(DeleteSourceAsync, CanDeleteSource);
                return _deleteSellerCommand;
            }
        }

        public bool CanDeleteSource
        {
            get
            {
                if (SelectedAccountingSource is null) return false;
                return true;
            }
        }

        #endregion

        #region Colecciones

        private AccountingSourceDTO? _selectedAccountingSource;
        public AccountingSourceDTO? SelectedAccountingSource
        {
            get { return _selectedAccountingSource; }
            set
            {
                if (_selectedAccountingSource != value)
                {
                    _selectedAccountingSource = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingSource));
                    NotifyOfPropertyChange(nameof(CanDeleteSource));
                }
            }
        }

        private ObservableCollection<AccountingSourceDTO> _accountingSources;
        public ObservableCollection<AccountingSourceDTO> AccountingSources
        {
            get { return _accountingSources; }
            set
            {
                if (_accountingSources != value)
                {
                    _accountingSources = value;
                    NotifyOfPropertyChange(nameof(AccountingSources));
                    NotifyOfPropertyChange(nameof(CanDeleteSource));
                }
            }
        }

        // Listado filtrado para permitir busqueda
        private ObservableCollection<AccountingSourceDTO> _accountingSourceFiltered;
        public ObservableCollection<AccountingSourceDTO> AccountingSourceFiltered
        {
            get { return _accountingSourceFiltered; }
            set
            {
                if (_accountingSourceFiltered != value)
                {
                    _accountingSourceFiltered = value;
                    NotifyOfPropertyChange(nameof(AccountingSourceFiltered));
                }
            }
        }

        // Cuentas contables
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

        // Modulos del sistema administrativo
        private ObservableCollection<ModuleGraphQLModel> _modules;
        public ObservableCollection<ModuleGraphQLModel> Modules
        {
            get { return _modules; }
            set
            {
                if (_modules != value)
                {
                    _modules = value;
                    NotifyOfPropertyChange(nameof(Modules));
                }
            }
        }

        #endregion

        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                
                string query = GetLoadAccountingSourceQuery(true);

                //AccountingSource Filter
                dynamic variables = new ExpandoObject();
                variables.accountingSourcesFilters = new ExpandoObject();

                variables.accountingSourcesFilters.annulment =  false;
               if(!string.IsNullOrEmpty(FilterSearch))
                {
                    variables.accountingSourcesFilters.name =  FilterSearch.Trim().RemoveExtraSpaces();

                }

                if(SelectedModuleId != 0)
                {
                 
                    variables.accountingSourcesFilters.moduleId = SelectedModuleId;
                }

                // Filtro de cuentas contables
                variables.accountingAccountsFilters = new ExpandoObject();
                variables.accountingAccountsFilters.only_auxiliary_accounts = true;
               

                //Pagination
                variables.accountingSourcesPagination = new ExpandoObject();
                variables.accountingSourcesPagination.page = PageIndex;
                variables.accountingSourcesPagination.pageSize = PageSize;

                // Iniciar cronometro
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                AccountingSourceDataContext result = await _accountingSourceService.GetDataContextAsync<AccountingSourceDataContext>(query, variables);

                // Detener cronometro
                stopwatch.Stop();
                this.ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
                this.ProcessTypes = new ObservableCollection<ProcessTypeGraphQLModel>(result.ProcessTypesPage.Entries);
                App.Current.Dispatcher.Invoke(() =>
                {
                    this.Modules = result.ModulesPage.Entries;
                    this.Modules.Insert(0, new ModuleGraphQLModel() { Id = 0, Name = "MOSTRAR TODOS LOS MODULOS" });
                    this.AccountingSources = new ObservableCollection<AccountingSourceDTO>(this.Context.AutoMapper.Map<IEnumerable<AccountingSourceDTO>>(result.AccountingSourcesPage.Entries));
                    this.AccountingAccounts = result.AccountingAccountsPage.Entries;
                });
                this.TotalCount = result.AccountingSourcesPage.TotalEntries;
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

        public AccountingSourceMasterViewModel(AccountingSourceViewModel context, IRepository<AccountingSourceGraphQLModel> accountingSourceService, Helpers.Services.INotificationService notificationService)
        {
            this._notificationService = notificationService;
            this._accountingSourceService = accountingSourceService;
            this.Context = context;
            Context.EventAggregator.SubscribeOnUIThread(this);
            _ =  InitializeAsync();
        }

        #region Metodos

        protected override void OnViewReady(object view)
        {
            if (Context.EnableOnViewReady is false) return;
            base.OnViewReady(view);
            this.SetFocus(() => FilterSearch);
        }

        public async Task LoadAccountingSourcesAsync()
        {

            try
            {
                // Ocupado
                this.IsBusy = true;
                this.Refresh();

                // Iniciar cronometro
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string query = GetLoadAccountingSourceQuery();

                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();

                variables.pageResponseFilters.annulment = false;

                variables.pageResponseFilters.name =  string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                if (SelectedModuleId != 0)
                {
                    variables.pageResponseFilters.moduleId =  SelectedModuleId;
                }
                
                //Pagination
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.page = PageIndex;
                variables.pageResponsePagination.pageSize = PageSize;

                PageType<AccountingSourceGraphQLModel> source = await _accountingSourceService.GetPageAsync(query, variables);

                // Detener cronometro
                stopwatch.Stop();
                this.ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
                this.TotalCount = source.TotalEntries;
                this.AccountingSources = this.Context.AutoMapper.Map<ObservableCollection<AccountingSourceDTO>>(source.Entries);


                this.IsBusy = false;
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
        public string GetLoadAccountingSourceQuery(bool withDependencies = false)
        {
            
            var moduleFields = FieldSpec<PageType<ModuleGraphQLModel>>
              .Create()
              .SelectList(it => it.Entries, entries => entries
                  .Field(e => e.Id)

                  .Field(e => e.Code)
                  .Field(e => e.Name)
                  .Field(e => e.Abbreviation)
              )
              .Field(o => o.PageNumber)
              .Field(o => o.PageSize)
              .Field(o => o.TotalPages)
              .Field(o => o.TotalEntries)
              .Build();
            
            var accountingSourceFields = FieldSpec<PageType<AccountingSourceGraphQLModel>>
               .Create()
               .SelectList(it => it.Entries, entries => entries
                   .Field(e => e.Id)
                   .Field(e => e.AnnulmentCode)
                   .Field(e => e.Code)
                   .Field(e => e.Name)
                   .Field(e => e.IsSystemSource)

                   .Field(e => e.AnnulmentCharacter)
                   .Field(e => e.IsKardexTransaction)
                   .Field(e => e.KardexFlow)
                    .Select(e => e.AccountingAccount, acc => acc
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                            )
                   .Select(e => e.ProcessType, cat => cat
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                            .Select(c => c.Module, dep => dep
                                .Field(d => d.Id)
                                .Field(d => d.Name)
                            )

                    )

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
                  .Field(e => e.Margin)
                  .Field(e => e.Code)
                  .Field(e => e.Name)
                  .Field(e => e.MarginBasis)
              )
              .Field(o => o.PageNumber)
              .Field(o => o.PageSize)
              .Field(o => o.TotalPages)
              .Field(o => o.TotalEntries)
              .Build();
            var processTypeFields = FieldSpec<PageType<ProcessTypeGraphQLModel>>
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

            var accountingSourcePagParameters = new GraphQLQueryParameter("pagination", "Pagination");
            var accountingSourceParameters = new GraphQLQueryParameter("filters", "AccountingSourceFilters");
            var accountingAccountParameters = new GraphQLQueryParameter("filters", "AccountingAccountFilters");

            var accountingSourceFragment = new GraphQLQueryFragment("accountingSourcesPage", [accountingSourcePagParameters, accountingSourceParameters], accountingSourceFields, withDependencies ? "AccountingSourcesPage" : "PageResponse");
            var accountingAccountFragment = new GraphQLQueryFragment("accountingAccountsPage", [accountingAccountParameters], accountingAccountFields, "AccountingAccountsPage");
            var moduleFragment = new GraphQLQueryFragment("modulesPage", [], moduleFields, "ModulesPage");
            var processTypeFragment = new GraphQLQueryFragment("processTypesPage", [], processTypeFields, "ProcessTypesPage");

            var builder = withDependencies ? new GraphQLQueryBuilder([accountingSourceFragment, accountingAccountFragment, moduleFragment, processTypeFragment]) : new GraphQLQueryBuilder([accountingSourceFragment]);
            return builder.GetQuery();
        }
        public void OnChecked()
        {
            NotifyOfPropertyChange(nameof(CanDeleteSource));
        }

        public void OnUnchecked()
        {
            NotifyOfPropertyChange(nameof(CanDeleteSource));
        }

        public async Task EditSourceAsync()
        {
            try
            {
                this.IsBusy = true;
                this.Refresh();
                await Task.Run(() => this.ExecuteEditSourceAsync());
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

        public async Task ExecuteEditSourceAsync()
        {
            try
            {
                var auxiliaryAccounts = from account in this.AccountingAccounts
                                        select new AccountingAccountPOCO { Id = account.Id, Code = account.Code, Name = account.Name };

                await this.Context.ActivateDetailViewForEditAsync(this.SelectedAccountingSource, ProcessTypes, auxiliaryAccounts);
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        public async Task CreateSourceAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteCreateSourceAsync());
                SelectedAccountingSource = null;
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

        public async Task ExecuteCreateSourceAsync()
        {
            var auxiliaryAccounts = from account in this.AccountingAccounts
                                    select new AccountingAccountPOCO { Id = account.Id, Code = account.Code, Name = account.Name };
            await Context.ActivateDetailViewForNewAsync(ProcessTypes, auxiliaryAccounts);
        }

        public async Task DeleteSourceAsync()
        {
            try
            {

                this.IsBusy = true;
                this.Refresh();

                string query = GetCanDeleteAccountingSourceQuery();

                object variables = new { canDeleteResponseId = SelectedAccountingSource.Id };

                var validation = await _accountingSourceService.CanDeleteAsync(query, variables);

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
                DeleteResponseType deletedAccountingSource = await Task.Run(() => this.ExecuteDeleteSourceAsync(SelectedAccountingSource.Id));

                if (!deletedAccountingSource.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedAccountingSource.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await Context.EventAggregator.PublishOnUIThreadAsync(new AccountingSourceDeleteMessage { DeletedAccountingSource = deletedAccountingSource });

                NotifyOfPropertyChange(nameof(CanDeleteSource));
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
        public string GetCanDeleteAccountingSourceQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("canDeleteAccountingSource", [parameter], fields, alias: "CanDeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }
        public async Task<DeleteResponseType> ExecuteDeleteSourceAsync(int id)
        {
            try
            {

                string query = GetDeleteAccountingSourceQuery();

                object variables = new
                {
                    deleteResponseId = id
                };

                // Eliminar registros
                DeleteResponseType deletedRecord = await _accountingSourceService.DeleteAsync<DeleteResponseType>(query, variables);
                return deletedRecord;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public string GetDeleteAccountingSourceQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("deleteAccountingSource", [parameter], fields, alias: "DeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
       

        private async void ExecuteChangeIndex(object parameter)
        {
            await LoadAccountingSourcesAsync();
        }

        private bool CanExecuteChangeIndex(object parameter)
        {
            return true;
        }

        public async Task HandleAsync(AccountingSourceCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadAccountingSourcesAsync();
                _notificationService.ShowSuccess("Fuente contable creada correctamente");
                return;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task HandleAsync(AccountingSourceUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadAccountingSourcesAsync();
                _notificationService.ShowSuccess("Fuente contable actualizada correctamente");
                return;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task HandleAsync(AccountingSourceDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadAccountingSourcesAsync();
                _notificationService.ShowSuccess("Fuente contable eliminada correctamente");
                return;
            }
            catch (Exception)
            {

                throw;
            }
        }

        #endregion
    }
}
