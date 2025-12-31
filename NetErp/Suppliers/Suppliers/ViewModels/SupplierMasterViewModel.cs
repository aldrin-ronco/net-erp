
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
using Models.Billing;
using Models.Books;
using static Models.Global.GraphQLResponseTypes;
using NetErp.Helpers.GraphQLQueryBuilder;
using DevExpress.Data.Utils;
using Models.Global;
using NetErp.Billing.Zones.DTO;
using NetErp.Global.CostCenters.DTO;
using Services.Billing.DAL.PostgreSQL;

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

        private ObservableCollection<IdentificationTypeGraphQLModel> _identificationTypes;
        public ObservableCollection<IdentificationTypeGraphQLModel> IdentificationTypes
        {
            get => _identificationTypes;
            set
            {
                if (_identificationTypes != value)
                {
                    _identificationTypes = value;
                    NotifyOfPropertyChange(nameof(IdentificationTypes));
                }
            }
        }
        private ObservableCollection<CountryGraphQLModel> _countries;
        public ObservableCollection<CountryGraphQLModel> Countries
        {
            get => _countries;
            set
            {
                if (_countries != value)
                {
                    _countries = value;
                    NotifyOfPropertyChange(nameof(Countries));
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
                IsBusy = true;
                int id = SelectedSupplier.Id;

                string query = @"query($id:Int!){
                    CanDeleteModel: canDeleteSupplier(id:$id){
                        canDelete
                        message
                    }
                }";

                object variables = new { id };

                var validation = await _supplierService.CanDeleteAsync(query, variables);
                if (validation.CanDelete) 
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show("Confirme ...", $"¿ Confirma que desea eliminar el registro {SelectedSupplier.AccountingEntity.SearchName}?", MessageBoxButton.YesNo, MessageBoxImage.Question);
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

                SupplierGraphQLModel deletedSupplier = await ExecuteDeleteSupplierAsync(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new SupplierDeleteMessage() { DeletedSupplier = Context.AutoMapper.Map<SupplierDTO>(deletedSupplier) });

                NotifyOfPropertyChange(nameof(CanDeleteSupplier));
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<SupplierGraphQLModel> ExecuteDeleteSupplierAsync(int id)
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
                SupplierGraphQLModel result = await _supplierService.DeleteAsync(query, variables);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
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
            await Context.ActivateDetailViewForNew();
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

                variables.pageResponsefilters = new ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch))
                {
                    variables.pageResponsefilters.Matching = FilterSearch;
                }


                //Paginación
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.Page = PageIndex;
                variables.pageResponsePagination.PageSize = PageSize;

                if (withDependencies)
                {
                    SupplierDataContext result = await _supplierService.GetDataContextAsync<SupplierDataContext>(query, variables);
                    Suppliers = Context.AutoMapper.Map<ObservableCollection<SupplierDTO>>(result.Suppliers.Entries);

                }else
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
            /************************************************////
            /*try
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
                          description
                          email
                          password
                          sendElectronicInvoice
                        }
                      }
                    }
                  }
                }";
            string query = GetLoadSuppliersDataQuery();
                dynamic variables = new ExpandoObject();
                variables.pageResponsefilters = new ExpandoObject();

                
                /*
                variables.filter.or[0].searchName = new ExpandoObject();
                variables.filter.or[0].searchName.@operator = "like";
                variables.filter.or[0].searchName.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                variables.filter.or[1].identificationNumber = new ExpandoObject();
                variables.filter.or[1].identificationNumber.@operator = "like";
                variables.filter.or[1].identificationNumber.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                variables.filter.Pagination = new ExpandoObject();
                variables.filter.Pagination.Page = PageIndex;
                variables.filter.Pagination.PageSize = PageSize;
                // Iniciar cronometro
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                PageType<SupplierGraphQLModel> result = await _supplierService.GetPageAsync(query, variables);


                TotalCount = result.TotalEntries;
                Suppliers = Context.AutoMapper.Map<ObservableCollection<SupplierDTO>>(result.Entries);

                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
                */
        }
        public string GetLoadSuppliersDataQuery(bool withDependencies = false)
        {
            var suppliersFields = FieldSpec<PageType<SupplierGraphQLModel>>
             .Create()
             .SelectList(it => it.Entries, entries => entries
                 .Field(e => e.Id)
                 .Field(e => e.IsTaxFree)
                 .Field(e => e.IcaRetentionMargin)
                 .Field(e => e.IcaRetentionMarginBasis)
                 .Field(e => e.RetainsAnyBasis)
                      
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

            var identificationTypesFields = FieldSpec<PageType<IdentificationTypeGraphQLModel>>
             .Create()
             .SelectList(it => it.Entries, entries => entries
                 .Field(e => e.Id)
                 .Field(e => e.Name)
                 .Field(e => e.Code)
                 .Field(e => e.HasVerificationDigit)
                 .Field(e => e.MinimumDocumentLength)
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
            var countriesFields = FieldSpec<PageType<CountryGraphQLModel>>
              .Create()
              .SelectList(it => it.Entries, entries => entries
                  .Field(e => e.Id)
                  .Field(e => e.Name)
                  .Field(e => e.Code)
                  .SelectList(e => e.Departments, co => co
                                    .Field(x => x.Id)
                                    .Field(x => x.Code)
                                    .Field(x => x.Name)
                                    .SelectList(e => e.Cities, co => co
                                        .Field(x => x.Id)
                                        .Field(x => x.Code)
                                        .Field(x => x.Name)
                                )
                                )
              )
              .Field(o => o.PageNumber)
              .Field(o => o.PageSize)
              .Field(o => o.TotalPages)
              .Field(o => o.TotalEntries)
              .Build();

            var suppliersPagParameters = new GraphQLQueryParameter("pagination", "Pagination");
            var suppliersParameters = new GraphQLQueryParameter("filters", "SupplierFilters");
            var suppliersFragment = new GraphQLQueryFragment("suppliersPage", [suppliersPagParameters, suppliersParameters], suppliersFields, withDependencies ? "suppliers" : "pageResponse");


            var identificationTypesFragment = new GraphQLQueryFragment("identificationTypesPage", [], identificationTypesFields, "IdentificationTypes");
            var countriesFragment = new GraphQLQueryFragment("countriesPage", [], countriesFields, "Countries");
            var withholdingTypeFragment = new GraphQLQueryFragment("withholdingTypesPage", [], withholdingTypeFields, "withholdingTypes");



            var builder = withDependencies ? new GraphQLQueryBuilder([suppliersFragment, identificationTypesFragment, countriesFragment, withholdingTypeFragment]) : new GraphQLQueryBuilder([suppliersFragment]);
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
        }

        protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            Context.EventAggregator.Unsubscribe(this);
            await base.OnDeactivateAsync(close, cancellationToken);
        }

        public async Task ExecuteEditSupplier()
        {
            await Context.ActivateDetailViewForEdit(SelectedSupplier);
        }

        public async Task HandleAsync(SupplierCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadSuppliersAsync();
                _notificationService.ShowSuccess("Proveedor creado correctamente", "Éxito");
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
                _notificationService.ShowSuccess("Proveedor actualizado correctamente", "Éxito");
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
                SupplierDTO supplierToDelete = Suppliers.First(s => s.Id == message.DeletedSupplier.Id);
                if (supplierToDelete != null) _ = Application.Current.Dispatcher.Invoke(() => Suppliers.Remove(supplierToDelete));
                _notificationService.ShowSuccess("Proveedor eliminado correctamente", "Éxito");
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
            _ = Task.Run(() => LoadSuppliersAsync(true));
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
