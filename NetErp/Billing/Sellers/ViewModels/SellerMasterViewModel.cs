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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;

namespace NetErp.Billing.Sellers.ViewModels
{
    public class SellerMasterViewModel : Screen,
        IHandle<SellerCreateMessage>,
        IHandle<SellerUpdateMessage>,
        IHandle<SellerDeleteMessage>
    {
        public readonly IGenericDataAccess<SellerGraphQLModel> SellerService = IoC.Get<IGenericDataAccess<SellerGraphQLModel>>();
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
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = Task.Run(() => LoadSellers());
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
                    _ = Task.Run(() => LoadSellers());
                }
            }
        }

        private SellerDTO _selectedSeller;
        public SellerDTO SelectedSeller
        {
            get => _selectedSeller;
            set
            {
                if (_selectedSeller != value)
                {
                    _selectedSeller = value;
                    NotifyOfPropertyChange(nameof(SelectedSeller));
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

        public bool CanDeleteSeller
        {
            get
            {
                if (Sellers == null) return false;
                var selectedItems = from seller
                                    in Sellers
                                    where seller.IsChecked
                                    select new { seller.Id };
                return selectedItems.ToList().Count == 1;
            }
        }

        public async Task CreateSeller()
        {
            await Context.ActivateDetailViewForNew();
        }

        public async Task DeleteSeller()
        {
            try
            {
                if (Xceed.Wpf.Toolkit.MessageBox.Show("¿ Confirma que desea eliminar el registro seleccionado ?", "Confirme ...", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

                int id = Sellers.First(c => c.IsChecked).Id;

                IsBusy = true;

                Refresh();

                SellerGraphQLModel deletedSeller = await ExecuteDeleteSeller(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new SellerDeleteMessage { DeletedSeller = Context.AutoMapper.Map<SellerDTO>(deletedSeller) });

                TotalCount--;

                NotifyOfPropertyChange(nameof(CanDeleteSeller));
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", "Atención !", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<SellerGraphQLModel> ExecuteDeleteSeller(int id)
        {
            try
            {
                string query = @"
                mutation ($id: Int!) {
                  deleteResponse: deleteSeller(id: $id) {
                    id
                  }
                }
                ";
                object variables = new { Id = id };
                SellerGraphQLModel deletedSeller = await SellerService.Delete(query, variables);
                return deletedSeller;
            }
            catch (Exception)
            {
                throw;
            }
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
                await ExecuteEditSeller();
                IsBusy = false;
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        public async Task ExecuteEditSeller()
        {
            await Context.ActivateDetailViewForEdit(SelectedSeller);
        }

        public void OnChecked()
        {
            NotifyOfPropertyChange(nameof(CanDeleteSeller));
        }

        public void OnUnchecked()
        {
            NotifyOfPropertyChange(nameof(CanDeleteSeller));
        }

        public SellerMasterViewModel(SellerViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnUIThread(this);
            _ = Task.Run(() => Initialize());
        }

        public async Task Initialize()
        {
            try
            {
                IsBusy = true;
                Refresh();
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string query = @"
                query ($sellerWhereInput: SellersWhereInput, $costCentersWhereInput: CostCentersWhereInput, $identificationTypesWhereInput: IdentificationTypesWhereInput) {
                  sellersPage(where: $sellerWhereInput) {
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
                          name
                          email
                          sendElectronicInvoice
                        }
                      }
                      costCenters {
                        id
                        name
                      }
                    }
                  }
                  identificationTypes(where: $identificationTypesWhereInput) {
                    id
                    code
                    name
                    hasVerificationDigit
                    minimumDocumentLength
                  }
                  countries{
                    id
                    code
                    name
                    departments {
                      id
                      code
                      name
                      cities {
                        id
                        code
                        name
                      }
                    }
                  }
                  costCenters(where: $costCentersWhereInput) {
                    id
                    name
                  }
                }";

                object variables = new { Config.ConnectionId, Pagination = new { Page = 1, PageSize = 50 }, SellerWhereInput = new { }, CostCentersWhereInput = new { }, IdentificationTypesWhereInput = new { Code = "13" } };
                var result = await SellerService.GetDataContext<>(query, variables); //pasar el tipo del batch a consultar
                stopwatch.Stop();
                Context.CostCenters = new ObservableCollection<CostCenterDTO>(Context.AutoMapper.Map<ObservableCollection<CostCenterDTO>>(result.CostCenters));
                Context.IdentificationTypes = new ObservableCollection<Models.Books.IdentificationTypeGraphQLModel>(result.IdentificationTypes);
                Context.Countries = result.Countries;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CostCenters = new ObservableCollection<CostCenterGraphQLModel>(result.CostCenters);
                    CostCenters.Insert(0, new CostCenterGraphQLModel() { Id = 0, Name = "MOSTRAR TODOS LOS CENTROS DE COSTOS" });
                    Sellers = new ObservableCollection<SellerDTO>(Context.AutoMapper.Map<ObservableCollection<SellerDTO>>(result.SellersPage.Rows));
                });
                TotalCount = result.SellersPage.Count;
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
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

        public async Task LoadSellers()
        {
            try
            {
                IsBusy = true;
                Refresh();
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string query = @"
                query ($sellerWhereInput: SellersWhereInput) {
                  pageResponse: sellersPage(where: $sellerWhereInput) {
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
                          name
                          email
                          sendElectronicInvoice
                        }
                      }
                      costCenters {
                        id
                        name
                      }
                    }
                  }
                }";

                dynamic variables = new ExpandoObject();
                variables.Pagination = new ExpandoObject();
                variables.Pagination.Page = PageIndex;
                variables.Pagination.PageSize = PageSize;
                variables.SellerWhereInput = new ExpandoObject();
                variables.SellerWhereInput.SearchName = FilterSearch;
                variables.SellerWhereInput.CostCenters = SelectedCostCenterId == 0 ? null : new int[] { SelectedCostCenterId };

                IGenericDataAccess<SellerGraphQLModel>.PageResponseType result = await Context.BillingSeller.GetPage(query, variables);
                stopwatch.Stop();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Sellers = new ObservableCollection<SellerDTO>(Context.AutoMapper.Map<ObservableCollection<SellerDTO>>(result.PageResponse.Rows));
                });
                TotalCount = result.PageResponse.Count;
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
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

        public Task HandleAsync(SellerCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                Sellers.Add(Context.AutoMapper.Map<SellerDTO>(message.CreatedSeller));
                return Task.CompletedTask;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Task HandleAsync(SellerUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                Sellers.Replace(Context.AutoMapper.Map<SellerDTO>(message.UpdatedSeller));
                return Task.CompletedTask;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Task HandleAsync(SellerDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                SellerDTO sellerToDelete = Sellers.FirstOrDefault(seller => seller.Id == message.DeletedSeller.Id);
                if (sellerToDelete != null) _ = Application.Current.Dispatcher.Invoke(() => Sellers.Remove(sellerToDelete));
                return Task.CompletedTask;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #region Paginacion

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

        #endregion
    }
}
