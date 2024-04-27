
using Caliburn.Micro;
using Common.Helpers;
using DevExpress.Mvvm;
using Common.Interfaces;
using GraphQL.Client.Http;
using Models.Suppliers;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Extensions.Suppliers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Common.Extensions;
using NetErp.Helpers;
using DevExpress.Xpf.Core;
using System.Dynamic;

namespace NetErp.Suppliers.Suppliers.ViewModels
{
    public class SupplierMasterViewModel : Screen,
        IHandle<SupplierCreateMessage>,
        IHandle<SupplierUpdateMessage>,
        IHandle<SupplierDeleteMessage>
    {

        #region Properties

        public readonly IGenericDataAccess<SupplierGraphQLModel> SupplierService = IoC.Get<IGenericDataAccess<SupplierGraphQLModel>>();
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

        //private ICommand editRecordCommand;
        //public ICommand EditRecordCommand
        //{
        //    get
        //    {
        //        if (editRecordCommand is null) editRecordCommand = new AsyncCommand(EditRecord, CanEditRecord);
        //        return editRecordCommand;
        //    }
        //}

        private ICommand _createSupplierCommand;
        public ICommand CreateSupplierCommand
        {
            get
            {
                if (_createSupplierCommand is null) _createSupplierCommand = new AsyncCommand(CreateSupplier, CanCreateSupplier);
                return _createSupplierCommand;
            }

        }

        private ICommand _deleteSupplierCommand;
        public ICommand DeleteSupplierCommand
        {
            get
            {
                if (_deleteSupplierCommand is null) _deleteSupplierCommand = new AsyncCommand(DeleteSupplier, CanDeleteSupplier);
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
                Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show(caption: "Atención!", messageBoxText: $"{this.GetType().Name}.{(currentMethod is null ? "EditCustomer" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", button: MessageBoxButton.OK, icon: MessageBoxImage.Error));
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

        public async Task DeleteSupplier()
        {
            try
            {
                IsBusy = true;
                int id = SelectedSupplier.Id;

                string query = @"query($id:Int!){
                    CanDeleteModel: canDeleteSupplier(id:$id){
                        canDelete
                        message
                    }
                }";

                object variables = new { id };

                var validation = await SupplierService.CanDelete(query, variables);
                if (validation.CanDelete) 
                {
                    IsBusy = false;
                    MessageBoxResult result = DXMessageBox.Show($"¿ Confirma que desea eliminar el registro {SelectedSupplier.Entity.SearchName}?", "Confirme ...", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show(caption: "Atención!", messageBoxText: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, button: MessageBoxButton.OK, icon: MessageBoxImage.Error));
                    return;
                }
                this.IsBusy = true;

                Refresh();

                SupplierGraphQLModel deletedSupplier = await ExecuteDeleteSupplier(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new SupplierDeleteMessage() { DeletedSupplier = Context.AutoMapper.Map<SupplierDTO>(deletedSupplier) });

                NotifyOfPropertyChange(nameof(CanDeleteSupplier));
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<SupplierGraphQLModel> ExecuteDeleteSupplier(int id)
        {
            try
            {
                string query = @"mutation($id:Int!) {
                                  deleteResponse: deleteSupplier(id:$id) {
                                    id
                                    isTaxFree
                                    icaRetentionMargin
                                    icaRetentionMarginBasis
                                    retainsAnyBasis    
                                  }
                                }";
                object variables = new { Id = id };
                SupplierGraphQLModel result = await SupplierService.Delete(query, variables);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool CanCreateSupplier() => !IsBusy;

        public async Task CreateSupplier()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteCreateSupplier());
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

        public async Task ExecuteCreateSupplier()
        {
            await Context.ActivateDetailViewForNew();
        }

        private bool CanExecuteChangeIndex()
        {
            return true;
        }

        public async Task LoadSuppliers()
        {
            try
            {

                IsBusy = true;
                Refresh();

                string query = @"
                query ($filter: SupplierFilterInput) {
                  pageResponse : supplierPage(filter: $filter) {
                    count
                    rows {
                      id
                      isTaxFree
                      icaRetentionMargin
                      icaRetentionMarginBasis
                      retainsAnyBasis
                      icaAccountingAccount {
                        id
                        code
                        name
                      }
                      retentions {
                        id
                        name
                        initialBase
                        margin
                        marginBase
                        retentionGroup
                      }
                      entity {
                        id
                        identificationType {
                          id
                          code
                        }
                        country {
                          id
                          code
                        }
                        department {
                          id
                          code
                        }
                        city {
                          id
                          code
                        }
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
                        address
                        telephonicInformation
                        emails {
                          id
                          name
                          email
                          password
                          sendElectronicInvoice
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
                variables.filter.QueryFilter = FilterSearch == "" ? "" : $"WHERE entity.search_name like '%{FilterSearch.Trim().Replace(" ", "%")}%' ";

                // Iniciar cronometro
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                IGenericDataAccess<SupplierGraphQLModel>.PageResponseType result = await SupplierService.GetPage(query, variables);


                TotalCount = result.PageResponse.Count;
                Suppliers = new ObservableCollection<SupplierDTO>(Context.AutoMapper.Map<ObservableCollection<SupplierDTO>>(result.PageResponse.Rows));

                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteChangeIndex()
        {
            await LoadSuppliers();
        }

        public SupplierMasterViewModel(SupplierViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnUIThread(this);
            //_ = Task.Run(() => LoadSuppliers());
        }

        public async Task ExecuteEditSupplier()
        {
            await Context.ActivateDetailViewForEdit(SelectedSupplier);
        }

        public Task HandleAsync(SupplierCreateMessage message, CancellationToken cancellationToken)
        {
            return Task.Run(() => Application.Current.Dispatcher.Invoke(() => Suppliers.Add(message.CreatedSupplier)));
        }

        public Task HandleAsync(SupplierUpdateMessage message, CancellationToken cancellationToken)
        {
            PageIndex = 1;
            return LoadSuppliers();
        }

        public Task HandleAsync(SupplierDeleteMessage message, CancellationToken cancellationToken)
        {
            SupplierDTO supplierToDelete = Suppliers.First(s => s.Id == message.DeletedSupplier.Id);
            if (supplierToDelete != null) _ = Application.Current.Dispatcher.Invoke(() => Suppliers.Remove(supplierToDelete));
            return LoadSuppliers();
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = Task.Run(() => LoadSuppliers());
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
                if (_paginationCommand == null) _paginationCommand = new AsyncCommand(ExecuteChangeIndex, CanExecuteChangeIndex);
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
                        _ = Task.Run(() => LoadSuppliers());
                    }
                }
            }
        }

        #endregion
    }
}
