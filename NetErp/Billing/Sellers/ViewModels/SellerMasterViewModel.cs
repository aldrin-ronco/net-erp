using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Common.Extensions;
using GraphQL.Client.Http;
using Models.Billing;
using Models.Global;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using Extensions.Sellers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using NetErp.Billing.Customers.ViewModels;
using NetErp.Helpers;
using Models.Books;
using Models.Suppliers;
using NetErp.Global.CostCenters.DTO;
using Ninject.Activation;
using NetErp.Billing.Zones.DTO;
using NetErp.Helpers.GraphQLQueryBuilder;
using static Models.Global.GraphQLResponseTypes;
using DevExpress.Mvvm.Native;
using DevExpress.Data.Utils;
using Services.Books.DAL.PostgreSQL;

namespace NetErp.Billing.Sellers.ViewModels
{
    public class SellerMasterViewModel : Screen,
        IHandle<SellerCreateMessage>,
        IHandle<SellerUpdateMessage>,
        IHandle<SellerDeleteMessage>,
        IHandle<AccountingEntityUpdateMessage>,
        IHandle<CustomerUpdateMessage>,
        IHandle<SupplierUpdateMessage>
    {
        private readonly IRepository<SellerGraphQLModel> _sellerService;
        private readonly Helpers.Services.INotificationService _notificationService;
        public SellerViewModel Context { get; set; }

        private bool _isBusy = false;
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

        private string _filterSearch = "";
        public string FilterSearch
        {
            get => _filterSearch;
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    // Solo ejecutamos la busqueda si esta vacio el filtro o si hay por lo menos 3 caracteres digitados
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = LoadSellers();
                }
            }
        }

        private bool _showActiveSellersOnly = true;
        public bool ShowActiveSellersOnly
        {
            get => _showActiveSellersOnly;
            set
            {
                if (_showActiveSellersOnly != value)
                {
                    _showActiveSellersOnly = value;
                    NotifyOfPropertyChange(nameof(ShowActiveSellersOnly));
                    _ = LoadSellers();
                }
            }
        }

        private int _selectedCostCenterId = 0;
        public int SelectedCostCenterId
        {
            get => _selectedCostCenterId;
            set
            {
                if (_selectedCostCenterId != value)
                {
                    _selectedCostCenterId = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenterId));
                    _ = LoadSellers();
                }
            }
        }

        private SellerDTO? _selectedSeller;
        public SellerDTO? SelectedSeller
        {
            get => _selectedSeller;
            set
            {
                if (_selectedSeller != value)
                {
                    _selectedSeller = value;
                    NotifyOfPropertyChange(nameof(SelectedSeller));
                    NotifyOfPropertyChange(nameof(CanDeleteSeller));
                }
            }
        }

        private ObservableCollection<CostCenterGraphQLModel> _costCenter;
        public ObservableCollection<CostCenterGraphQLModel> CostCenters
        {
            get => _costCenter;
            set
            {
                if (_costCenter != value)
                {
                    _costCenter = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        }

        private ObservableCollection<SellerDTO> _sellers;
        public ObservableCollection<SellerDTO> Sellers
        {
            get => _sellers;
            set
            {
                if (_sellers != value)
                {
                    _sellers = value;
                    NotifyOfPropertyChange(nameof(Sellers));
                }
            }
        }
        public bool CanCreateSeller() => !IsBusy;

        private ICommand _createSellerCommand;
        public ICommand CreateSellerCommand
        {
            get
            {
                if (_createSellerCommand is null) _createSellerCommand = new AsyncCommand(CreateSeller, CanCreateSeller);
                return _createSellerCommand;
            }

        }

        private ICommand _deleteSellerCommand;
        public ICommand DeleteSellerCommand
        {
            get
            {
                if (_deleteSellerCommand is null) _deleteSellerCommand = new AsyncCommand(DeleteSeller, CanDeleteSeller);
                return _deleteSellerCommand;
            }
        }

        public bool CanDeleteSeller
        {
            get
            {
                if (SelectedSeller is null) return false;
                return true;
            }
        }

        public async Task CreateSeller()
        {
            try
            {
                IsBusy = true;
                Refresh();
                SelectedSeller = null;
                await Context.ActivateDetailViewForNew();
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

        public async Task DeleteSeller()
        {
            try
            {

                this.IsBusy = true;
                this.Refresh();

                string query = GetCanDeleteSellerQuery();

                object variables = new { canDeleteResponseId = SelectedSeller.Id };

                var validation = await _sellerService.CanDeleteAsync(query, variables);

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
                DeleteResponseType deletedSeller = await Task.Run(() => this.ExecuteDeleteSeller(SelectedSeller.Id));

                if (!deletedSeller.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedSeller.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await Context.EventAggregator.PublishOnUIThreadAsync(new SellerDeleteMessage { DeletedSeller = deletedSeller });

                NotifyOfPropertyChange(nameof(CanDeleteSeller));
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
        public string GetCanDeleteSellerQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("canDeleteSeller", [parameter], fields, alias: "CanDeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }
        public async Task<DeleteResponseType> ExecuteDeleteSeller(int id)
        {
            try
            {

                string query = GetDeleteSellerQuery();

                object variables = new
                {
                    deleteResponseId = id
                };

                // Eliminar registros
                DeleteResponseType deletedRecord = await _sellerService.DeleteAsync<DeleteResponseType>(query, variables);
                return deletedRecord;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public string GetDeleteSellerQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("deleteSeller", [parameter], fields, alias: "DeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        private async Task ExecuteChangeIndex()
        {
            await LoadSellers();
        }

        private bool CanExecuteChangeIndex()
        {
            return true;
        }

        public async Task EditSeller()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Context.ActivateDetailViewForEdit(SelectedSeller);
                SelectedSeller = null;
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

        public void OnChecked()
        {
            NotifyOfPropertyChange(nameof(CanDeleteSeller));
        }

        public void OnUnchecked()
        {
            NotifyOfPropertyChange(nameof(CanDeleteSeller));
        }

        public SellerMasterViewModel(
            SellerViewModel context,
            IRepository<SellerGraphQLModel> sellerService,
            Helpers.Services.INotificationService notificationService)
        {
            Context = context;
            _sellerService = sellerService;
            _notificationService = notificationService;
            Context.EventAggregator.SubscribeOnUIThread(this);
            _ = Task.Run(async () => 
            {
                try
                {
                    await Initialize();
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
                        ThemedMessageBox.Show(title: "Error de inicialización", text: $"Error al cargar vendedores: {ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                        return Task.CompletedTask;
                    });
                }
            });
        }

        public async Task Initialize()
        {
            try
            {
                IsBusy = true;
                Refresh();
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

               
                string query = GetLoadSellersDataQuery(true);
                
                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();

                

                variables.filter.isActive =  true;

               /* variables.filter.searchName = "";

                variables.filter.identificationNumber = "";*/

                //Paginación
                variables.Pagination = new ExpandoObject();
                variables.Pagination.Page = PageIndex;
                variables.Pagination.PageSize = PageSize;

                SellersDataContext result = await _sellerService.GetDataContextAsync<SellersDataContext>(query, variables);
                stopwatch.Stop();
                Context.CostCenters = Context.AutoMapper.Map<ObservableCollection<CostCenterDTO>>(result.CostCenters.Entries);
                Context.IdentificationTypes = result.IdentificationTypes.Entries;
                Context.Countries = result.Countries.Entries;
                Context.Zones = Context.AutoMapper.Map<ObservableCollection<ZoneDTO>>(result.Zones.Entries);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CostCenters = result.CostCenters.Entries;
                    CostCenters.Insert(0, new CostCenterGraphQLModel() { Id = 0, Name = "MOSTRAR TODOS LOS CENTROS DE COSTOS" });
                    Sellers = Context.AutoMapper.Map<ObservableCollection<SellerDTO>>(result.sellersPage.Entries);
                });
                TotalCount = result.sellersPage.TotalEntries;
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
        public string GetLoadSellersDataQuery(bool withDependencies = false)
        {

            var sellersFields = FieldSpec<PageType<SellerGraphQLModel>>
              .Create()
              .SelectList(it => it.Entries, entries => entries
                  .Field(e => e.Id)

                  .Field(e => e.IsActive)
                  .Select(e => e.AccountingEntity, acc => acc
                            .Field(c => c.Id)
                            .Field(c => c.VerificationDigit)
                            .Field(c => c.IdentificationNumber)
                            .Field(c => c.FirstName)
                            .Field(c => c.MiddleName)
                            .Field(c => c.FirstLastName)
                            .Field(c => c.MiddleLastName)
                            .Field(c => c.SearchName)
                            .Field(c => c.PrimaryPhone)
                            .Field(c => c.SecondaryPhone)
                            .Field(c => c.PrimaryCellPhone)
                            .Field(c => c.SecondaryCellPhone)
                            .Field(c => c.Address)
                            .Field(c => c.TelephonicInformation)
                            .Select(e => e.Country, co => co 
                                    .Field (x => x.Id)
                                )
                            .Select(e => e.City, co => co
                                    .Field(x => x.Id)
                                )
                            .Select(e => e.Department, co => co
                                    .Field(x => x.Id)
                                    )
                            .SelectList(e => e.Emails, co => co
                                    .Field(x => x.Id)
                                    .Field(x => x.Description)
                                    .Field(x => x.Email)
                                    .Field(x => x.isElectronicInvoiceRecipient)
                                    )
                            )
                   .SelectList(e => e.CostCenters, acc => acc
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                   )
                   .Select(e => e.Zone, acc => acc
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                   )


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


            var costCentersFields = FieldSpec<PageType<CostCenterGraphQLModel>>
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

            var zonesFields = FieldSpec<PageType<ZoneGraphQLModel>>
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

            var sellersPagParameters = new GraphQLQueryParameter("pagination", "Pagination");
            var sellersParameters = new GraphQLQueryParameter("filters", "SellerFilters");
            var sellersFragment = new GraphQLQueryFragment("sellersPage", [sellersPagParameters, sellersParameters], sellersFields, "sellersPage");

            var identificationTypesFragment = new GraphQLQueryFragment("identificationTypesPage", [], identificationTypesFields, "IdentificationTypes");
            var countriesFragment = new GraphQLQueryFragment("countriesPage", [], countriesFields, "Countries");
            var costCentersFragment = new GraphQLQueryFragment("costCentersPage", [], costCentersFields, "costCenters");

            var zonesFragment = new GraphQLQueryFragment("zonesPage", [], zonesFields, "zones");


            var builder = withDependencies ? new GraphQLQueryBuilder([sellersFragment, identificationTypesFragment, countriesFragment, costCentersFragment, zonesFragment]) : new GraphQLQueryBuilder([sellersFragment]);
            return builder.GetQuery();
        }
        public async Task LoadSellers()
        {
            try
            {
                IsBusy = true;
                Refresh();
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string query = @"
                query ($filter: SellerFilterInput) {
                  pageResponse: sellerPage(filter: $filter) {
                    count
                    rows {
                      id
                      isActive
                      entity {
                        id
                        verificationDigit
                        identificationNumber
                        firstName
                        middleName
                        firstLastName
                        middleLastName
                        searchName
                        phone1
                        phone2
                        cellPhone1
                        cellPhone2
                        address
                        telephonicInformation
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
                          sendElectronicInvoice
                        }
                      }
                    zones {
                        id
                        name
                      }
                    costCenters {
                    id
                    name
                      }
                    }
                  }
                }";


                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();

                variables.filter.and = new ExpandoObject[]
                {
                    new(),
                    new(),
                    new()
                };

                if (ShowActiveSellersOnly)
                {
                    variables.filter.and[0].isActive = new ExpandoObject();
                    variables.filter.and[0].isActive.@operator = "=";
                    variables.filter.and[0].isActive.value = true;
                }

                if(SelectedCostCenterId != 0)
                {
                    variables.filter.and[1].costCenterIds = new ExpandoObject();
                    variables.filter.and[1].costCenterIds.@operator = "=";
                    variables.filter.and[1].costCenterIds.value = new int[] {SelectedCostCenterId};
                }

                variables.filter.and[2].or = new ExpandoObject[]
                {
                    new(),
                    new()
                };

                variables.filter.and[2].or[0].searchName = new ExpandoObject();
                variables.filter.and[2].or[0].searchName.@operator = "like";
                variables.filter.and[2].or[0].searchName.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                variables.filter.and[2].or[1].identificationNumber = new ExpandoObject();
                variables.filter.and[2].or[1].identificationNumber.@operator = "like";
                variables.filter.and[2].or[1].identificationNumber.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                //Paginación
                variables.filter.Pagination = new ExpandoObject();
                variables.filter.Pagination.Page = PageIndex;
                variables.filter.Pagination.PageSize = PageSize;

                var result = await _sellerService.GetPageAsync(query, variables);
                stopwatch.Stop();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Sellers = new ObservableCollection<SellerDTO>(Context.AutoMapper.Map<ObservableCollection<SellerDTO>>(result.PageResponse.Rows));
                });
                TotalCount = result.PageResponse.Count;
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

        protected override void OnViewReady(object view)
        {
            if (Context.EnableOnViewReady is false) return;
            base.OnViewReady(view);
            _ = this.SetFocus(nameof(FilterSearch));
        }

        protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {

                Context.EventAggregator.Unsubscribe(this);
            }
            await base.OnDeactivateAsync(close, cancellationToken);
        }

        public async Task HandleAsync(SellerCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadSellers();
                _notificationService.ShowSuccess("Vendedor creado correctamente.");
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }

        public async Task HandleAsync(SellerUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadSellers();
                _notificationService.ShowSuccess("Vendedor actualizado correctamente.");
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }

        public async Task HandleAsync(SellerDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadSellers();
                _notificationService.ShowSuccess("Vendedor eliminado correctamente.");
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }

        public Task HandleAsync(AccountingEntityUpdateMessage message, CancellationToken cancellationToken)
        {
            return LoadSellers();
        }

        public Task HandleAsync(CustomerUpdateMessage message, CancellationToken cancellationToken)
        {
            return LoadSellers();
        }

        public Task HandleAsync(SupplierUpdateMessage message, CancellationToken cancellationToken)
        {
            return LoadSellers();
        }

       

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

        private ICommand _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                if (_paginationCommand == null) _paginationCommand = new AsyncCommand(ExecuteChangeIndex, CanExecuteChangeIndex);
                return _paginationCommand;
            }
        }

        
    }
}
