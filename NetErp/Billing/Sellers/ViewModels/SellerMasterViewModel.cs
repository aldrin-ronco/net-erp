using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Data.Utils;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using Extensions.Sellers;
using GraphQL.Client.Http;
using Models.Billing;
using Models.Books;
using Models.Global;
using Models.Suppliers;
using NetErp.Billing.Customers.ViewModels;
using NetErp.Billing.Zones.DTO;
using NetErp.Global.CostCenters.DTO;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using Ninject.Activation;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Billing.Sellers.ViewModels
{
    public class SellerMasterViewModel : Screen,
        IHandle<SellerCreateMessage>,
        IHandle<SellerUpdateMessage>,
        IHandle<SellerDeleteMessage>
       
    {
        private readonly IRepository<SellerGraphQLModel> _sellerService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly CostCenterCache _costCenterCache;
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
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = LoadSellersAsync();
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
                    _ = LoadSellersAsync();
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
                    _ = LoadSellersAsync();
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
                if (_createSellerCommand is null) _createSellerCommand = new AsyncCommand(CreateSellerAsync, CanCreateSeller);
                return _createSellerCommand;
            }

        }

        private ICommand _deleteSellerCommand;
        public ICommand DeleteSellerCommand
        {
            get
            {
                if (_deleteSellerCommand is null) _deleteSellerCommand = new AsyncCommand(DeleteSellerAsync, CanDeleteSeller);
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

        public async Task CreateSellerAsync()
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

        public async Task DeleteSellerAsync()
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
                DeleteResponseType deletedSeller = await Task.Run(() => this.ExecuteDeleteSellerAsync(SelectedSeller.Id));

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
        public async Task<DeleteResponseType> ExecuteDeleteSellerAsync(int id)
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

        private async Task ExecuteChangeIndexAsync()
        {
            await LoadSellersAsync();
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
                await Context.ActivateDetailViewForEdit(SelectedSeller.Id);
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
            CostCenterCache costCenterCache,
            Helpers.Services.INotificationService notificationService)
        {
            Context = context;
            _sellerService = sellerService;
            _notificationService = notificationService;
            _costCenterCache = costCenterCache;
            Context.EventAggregator.SubscribeOnUIThread(this);
            _ = Task.Run(async () => 
            {
                await Task.WhenAll(
              _costCenterCache.EnsureLoadedAsync()
              );
                try
                {
                    await InitializeAsync();
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

        public async Task InitializeAsync()
        {
           
            CostCenters = _costCenterCache.Items;
            CostCenters.Insert(0, new CostCenterGraphQLModel() { Id = 0, Name = "MOSTRAR TODOS LOS CENTROS DE COSTOS" });
           await LoadSellersAsync();
            
        }
        public string GetLoadSellersDataQuery()
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
                            .Field(c => c.Address)                           
                            .Field(c => c.SearchName)
                            .Field(c => c.TelephonicInformation)

                            )
                   


              )
              .Field(o => o.PageNumber)
              .Field(o => o.PageSize)
              .Field(o => o.TotalPages)
              .Field(o => o.TotalEntries)
              .Build();



           

            var sellersPagParameters = new GraphQLQueryParameter("pagination", "Pagination");
            var sellersParameters = new GraphQLQueryParameter("filters", "SellerFilters");
            var sellersFragment = new GraphQLQueryFragment("sellersPage", [sellersPagParameters, sellersParameters], sellersFields, "pageResponse");

           
            var builder =  new GraphQLQueryBuilder([sellersFragment]);
            return builder.GetQuery();
        }
        public async Task LoadSellersAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string query = GetLoadSellersDataQuery();

                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();

                
                if (!string.IsNullOrEmpty(FilterSearch))
                {
                    variables.pageResponseFilters.Matching = FilterSearch;
                }
                
                if (ShowActiveSellersOnly)
                {

                    variables.pageResponseFilters.isActive = true;
                }

                if (SelectedCostCenterId != 0)
                {

                    variables.pageResponseFilters.costCenterId = SelectedCostCenterId;
                }



                //Paginación
                
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.Page = PageIndex;
                variables.pageResponsePagination.PageSize = PageSize;
                
                 PageType<SellerGraphQLModel> result = await _sellerService.GetPageAsync(query, variables);
                stopwatch.Stop();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Sellers = new ObservableCollection<SellerDTO>(Context.AutoMapper.Map<ObservableCollection<SellerDTO>>(result.Entries));
                });
                TotalCount = result.TotalEntries;
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
                await LoadSellersAsync();
                _notificationService.ShowSuccess(message.CreatedSeller.Message);
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
                await LoadSellersAsync();
                _notificationService.ShowSuccess(message.UpdatedSeller.Message);
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
                await LoadSellersAsync();
                _notificationService.ShowSuccess(message.DeletedSeller.Message);
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
                if (_paginationCommand == null) _paginationCommand = new AsyncCommand(ExecuteChangeIndexAsync, CanExecuteChangeIndex);
                return _paginationCommand;
            }
        }

        
    }
}
