using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
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

namespace NetErp.Books.AccountingSources.ViewModels
{
    public class AccountingSourceMasterViewModel : Screen, IHandle<AccountingSourceCreateMessage>, IHandle<AccountingSourceUpdateMessage>, IHandle<AccountingSourceDeleteMessage>
    {
        public readonly IGenericDataAccess<AccountingSourceGraphQLModel> AccountingSourceService = IoC.Get<IGenericDataAccess<AccountingSourceGraphQLModel>>();
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
                    Task.Run(() => UpdateAccountingSourceDataGridView());
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
                    Task.Run(() => UpdateAccountingSourceDataGridView())
                        .ContinueWith(antecedent => NotifyOfPropertyChange(nameof(CanDeleteSource)));
                }
            }
        }

        public bool CanCreateSource() => !IsBusy;

        private ICommand _createSellerCommand;
        public ICommand CreateSellerCommand
        {
            get
            {
                if (_createSellerCommand is null) _createSellerCommand = new AsyncCommand(CreateSource, CanCreateSource);
                return _createSellerCommand;
            }

        }

        private ICommand _deleteSellerCommand;
        public ICommand DeleteSellerCommand
        {
            get
            {
                if (_deleteSellerCommand is null) _deleteSellerCommand = new AsyncCommand(DeleteSource, CanDeleteSource);
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

        public async Task Initialize()
        {
            try
            {
                IsBusy = true;
                Refresh();
                string query = @"
                query ($filter:AccountingSourceFilterInput, $accountFilter: AccountingAccountFilterInput) {
                  modules{
                    id
                    code
                    name
                    abbreviation
                }
                accountingSourcePage(filter: $filter) {
                    count
                    rows {
                      id
                      code
                      fullCode
                      annulmentCode
                      name
                      isSystemSource
                      annulmentCharacter
                      isKardexTransaction
                      kardexFlow
                      accountingAccount {
                        id
                      }
                      processType {
                        id
                        name
                        module {
                          id
                          name
                        }
                      }
                    }
                }
                accountingAccounts(filter: $accountFilter){
                    id
                    code
                    name
                    margin
                    marginBasis
                  }
                
                processTypes{
                    id
                    name
                }
                }";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.Pagination = new ExpandoObject();
                variables.filter.Pagination.Page = PageIndex;
                variables.filter.Pagination.PageSize = PageSize;
                variables.filter.Annulment = false;
                variables.accountFilter = new ExpandoObject();
                variables.accountFilter.includeOnlyAuxiliaryAccounts = true;
                //if (!string.IsNullOrEmpty(FilterSearch)) variables.filter.QueryFilter = $"entity.search_name like '%{FilterSearch.Trim().Replace(" ", "%")}%' OR entity.identification_number like '%{FilterSearch.Trim().Replace(" ", "%")}%'";
                variables.filter.QueryFilter = "";

                // Iniciar cronometro
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                var result = await AccountingSourceService.GetDataContext<AccountingSourceDataContext>(query, variables);

                // Detener cronometro
                stopwatch.Stop();
                this.ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
                this.Context.ProcessTypes = new ObservableCollection<ProcessTypeGraphQLModel>(result.ProcessTypes);
                App.Current.Dispatcher.Invoke(() =>
                {
                    this.Modules = new ObservableCollection<ModuleGraphQLModel>(result.Modules);
                    this.Modules.Insert(0, new ModuleGraphQLModel() { Id = 0, Name = "MOSTRAR TODOS LOS MODULOS" });
                    this.AccountingSources = new ObservableCollection<AccountingSourceDTO>(this.Context.AutoMapper.Map<IEnumerable<AccountingSourceDTO>>(result.AccountingSourcePage.Rows));
                    this.AccountingAccounts = new ObservableCollection<AccountingAccountGraphQLModel>(result.AccountingAccounts);
                });
                this.TotalCount = result.AccountingSourcePage.Count;
                this.UpdateAccountingSourceDataGridView();
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

        public AccountingSourceMasterViewModel(AccountingSourceViewModel context)
        {
            this.Context = context;
            Context.EventAggregator.SubscribeOnUIThread(this);
            _ = Task.Run(() => Initialize());
        }

        #region Metodos

        protected override void OnViewReady(object view)
        {
            if (Context.EnableOnViewReady is false) return;
            base.OnViewReady(view);
            this.SetFocus(() => FilterSearch);
        }

        public async Task LoadAccountingSources()
        {

            try
            {
                // Ocupado
                this.IsBusy = true;
                this.Refresh();

                // Iniciar cronometro
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string query = @"
                query ($filter:AccountingSourceFilterInput) {
                  pageResponse: accountingSourcePage(filter:$filter) {
                    count
                    rows {
                      id
                      code
                      fullCode
                      annulmentCode
                      name
                      isSystemSource
                      annulmentCharacter
                      isKardexTransaction
                      kardexFlow
                      accountingAccount {
                        id
                      }
                      processType {
                        id
                        name
                        module {
                          id
                          name
                        }
                      }
                    }
                  }
                }";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.Pagination = new ExpandoObject();
                variables.filter.Pagination.Page = PageIndex;
                variables.filter.Pagination.PageSize = PageSize;
                variables.filter.Annulment = false;
                //if (!string.IsNullOrEmpty(FilterSearch)) variables.filter.QueryFilter = $"entity.search_name like '%{FilterSearch.Trim().Replace(" ", "%")}%' OR entity.identification_number like '%{FilterSearch.Trim().Replace(" ", "%")}%'";
                variables.filter.QueryFilter = "";

                var source = await AccountingSourceService.GetPage(query, variables);

                // Detener cronometro
                stopwatch.Stop();
                this.ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
                this.TotalCount = source.PageResponse.Count;
                this.AccountingSources = this.Context.AutoMapper.Map<ObservableCollection<AccountingSourceDTO>>(source.PageResponse.Rows);
                //this.UpdateAccountingSourceDataGridView();

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

        public void UpdateAccountingSourceDataGridView()
        {
            // Aplicamos filtro de texto
            try
            {
                if (string.IsNullOrEmpty(this.FilterSearch))
                {
                    this.AccountingSourceFiltered = new ObservableCollection<AccountingSourceDTO>(this.AccountingSources);
                }
                else
                {
                    this.AccountingSourceFiltered = new ObservableCollection<AccountingSourceDTO>(
                                                    from source
                                                    in this.AccountingSources
                                                    where source.Name.IsSqlLikeMatch(this.FilterSearch)
                                                    select source);
                }

                // Aplicamos filtro de modulo
                if (this.SelectedModuleId > 0)
                {
                    var data = from source in this.AccountingSourceFiltered
                               where source.ProcessType.Module.Id == this.SelectedModuleId
                               select source;
                    this.AccountingSourceFiltered = new ObservableCollection<AccountingSourceDTO>(data);
                }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }

        }

        public void OnChecked()
        {
            NotifyOfPropertyChange(nameof(CanDeleteSource));
        }

        public void OnUnchecked()
        {
            NotifyOfPropertyChange(nameof(CanDeleteSource));
        }

        public async Task EditSource()
        {
            try
            {
                this.IsBusy = true;
                this.Refresh();
                await Task.Run(() => this.ExecuteEditSource());
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

        public async Task ExecuteEditSource()
        {
            try
            {
                await this.Context.ActivateDetailViewForEdit(this.SelectedAccountingSource);
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        public async Task CreateSource()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteCreateSource());
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

        public async Task ExecuteCreateSource()
        {
            await Context.ActivateDetailViewForNew();
        }

        public async Task DeleteSource()
        {
            try
            {
                // Checkear si el registro puede ser eliminado
                this.IsBusy = true;
                int id = SelectedAccountingSource.Id;

                string query = @"
                query($id:Int!){
                  CanDeleteModel: canDeleteAccountingSource(id:$id) {
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await AccountingSourceService.CanDelete(query, variables);
                if (validation.CanDelete)
                {
                    this.IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show("Confirme ...", $"¿Confirma que desea eliminar el registro {SelectedAccountingSource.Name}?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    this.IsBusy = false;
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", "El regisgtro seleccionado no puede ser eliminado" + (char)13 + (char)13 + validation.Message, MessageBoxButton.OK, MessageBoxImage.Information));
                    return;
                }

                this.IsBusy = true;
                this.Refresh();

                var deletedAccountingSource = await this.ExecuteDeleteSource(id);

                await this.Context.EventAggregator.PublishOnUIThreadAsync(new AccountingSourceDeleteMessage() { DeletedAccountingSource = Context.AutoMapper.Map<AccountingSourceDTO>(deletedAccountingSource) });

                // Desactivar opcion de eliminar registros
                NotifyOfPropertyChange(nameof(CanDeleteSource));
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
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

        public async Task<AccountingSourceGraphQLModel> ExecuteDeleteSource(int id)
        {
            try
            {
                string query = @"
                mutation ($id: Int!) {
                  deleteResponse: deleteAccountingSource(id: $id) {
                    id
                    reverseId
                    code
                    fullCode
                    annulmentCode
                    name
                    annulment
                    isSystemSource
                    annulmentCharacter
                    isKardexTransaction
                    kardexFlow
                    processType {
                      id
                      name
                      module {
                        id
                        code
                        name
                      }
                    }
                  }
                }";

                object variables = new
                {
                    Id = id
                };

                // Eliminar registros
                var accountingSourceDeleted = await AccountingSourceService.Delete(query, variables);
                return accountingSourceDeleted;
            }
            catch (Exception)
            {
                throw;
            }
        }


        private async void ExecuteChangeIndex(object parameter)
        {
            await LoadAccountingSources();
        }

        private bool CanExecuteChangeIndex(object parameter)
        {
            return true;
        }

        public Task HandleAsync(AccountingSourceCreateMessage message, CancellationToken cancellationToken)
        {
            return Task.FromResult(AccountingSources = new ObservableCollection<AccountingSourceDTO>(Context.AutoMapper.Map<ObservableCollection<AccountingSourceDTO>>(message.AccountingSources)));
        }

        public Task HandleAsync(AccountingSourceUpdateMessage message, CancellationToken cancellationToken)
        {
            return Task.FromResult(AccountingSources = new ObservableCollection<AccountingSourceDTO>(Context.AutoMapper.Map<ObservableCollection<AccountingSourceDTO>>(message.AccountingSources)));
        }

        public async Task HandleAsync(AccountingSourceDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingSources();
        }

        #endregion
    }
}
