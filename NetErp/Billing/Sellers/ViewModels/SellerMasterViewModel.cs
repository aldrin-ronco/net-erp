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
                await Task.Run(() => ExecuteCreateSeller());
                SelectedSeller = null;
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show(caption: "Atención!", messageBoxText: $"{this.GetType().Name}.{(currentMethod is null ? "EditSeller" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", button: MessageBoxButton.OK, icon: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task ExecuteCreateSeller()
        {
            await Context.ActivateDetailViewForNew();
        }

        public async Task DeleteSeller()
        {
            try
            {
                IsBusy = true;
                int id = SelectedSeller.Id;

                string query = @"query($id:Int!){
                  CanDeleteModel: canDeleteSeller(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await this.SellerService.CanDelete(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = DXMessageBox.Show(caption: "Confirme...", messageBoxText: $"¿Confirma que desea eliminar el registro {SelectedSeller.Entity.SearchName}?", button: MessageBoxButton.YesNo, icon: MessageBoxImage.Question);
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

                SellerGraphQLModel deletedSeller = await ExecuteDeleteSeller(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new SellerDeleteMessage() { DeletedSeller = Context.AutoMapper.Map<SellerDTO>(deletedSeller) });

                NotifyOfPropertyChange(nameof(CanDeleteSeller));
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => DXMessageBox.Show(caption: "Atención!", messageBoxText: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", button: MessageBoxButton.OK, icon: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => DXMessageBox.Show(caption: "Atención!", messageBoxText: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteCustomer" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", button: MessageBoxButton.OK, icon: MessageBoxImage.Error));
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
                    isActive
                  }
                }
                ";
                object variables = new { Id = id };
                SellerGraphQLModel deletedSeller = await SellerService.Delete(query, variables);
                SelectedSeller = null;
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
                await Task.Run(() => ExecuteEditSeller());
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

        //TODO definir la query compleja para realizar filtros con multiples condiciones.
        public async Task Initialize()
        {
            try
            {
                IsBusy = true;
                Refresh();
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string query = @"
                query ($filter: SellerFilterInput){
                  sellerPage(filter: $filter) {
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
                  identificationTypes {
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
                  costCenters{
                    id
                    name
                  }
                }";

                
                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.Pagination = new ExpandoObject();
                variables.filter.Pagination.Page = PageIndex;
                variables.filter.Pagination.PageSize = PageSize;              
                if (!string.IsNullOrEmpty(FilterSearch)) variables.filter.QueryFilter = $"entity.search_name like '%{FilterSearch.Trim().Replace(" ", "%")}%' OR entity.identification_number like '%{FilterSearch.Trim().Replace(" ", "%")}%'";
                variables.filter.QueryFilter = "";
                var result = await SellerService.GetDataContext<SellersDataContext>(query, variables);
                stopwatch.Stop();
                Context.CostCenters = new ObservableCollection<CostCenterDTO>(Context.AutoMapper.Map<ObservableCollection<CostCenterDTO>>(result.CostCenters));
                Context.IdentificationTypes = new ObservableCollection<Models.Books.IdentificationTypeGraphQLModel>(result.IdentificationTypes);
                Context.Countries = result.Countries;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CostCenters = new ObservableCollection<CostCenterGraphQLModel>(result.CostCenters);
                    CostCenters.Insert(0, new CostCenterGraphQLModel() { Id = 0, Name = "MOSTRAR TODOS LOS CENTROS DE COSTOS" });
                    Sellers = new ObservableCollection<SellerDTO>(Context.AutoMapper.Map<ObservableCollection<SellerDTO>>(result.SellerPage.Rows));
                });
                TotalCount = result.SellerPage.Count;
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

                string costCenterFilter = string.Empty;
                string isActiveFilter = string.Empty;
                string filterSearch = string.Empty;
                if (SelectedCostCenterId != 0) costCenterFilter = $"seller.id in (select distinct seller_id from billing_sellers_by_cost_center where cost_center_id = any(array[{SelectedCostCenterId}]))";
                if (ShowActiveSellersOnly) isActiveFilter = $"seller.is_active = {ShowActiveSellersOnly}";
                if (!string.IsNullOrEmpty(FilterSearch)) filterSearch = $"entity.search_name like '%{FilterSearch.Trim().Replace(" ", "%")}%' OR entity.identification_number like '%{FilterSearch.Trim().Replace(" ", "%")}%'";
                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.Pagination = new ExpandoObject();
                variables.filter.Pagination.Page = PageIndex;
                variables.filter.Pagination.PageSize = PageSize;
                variables.filter.QueryFilter = @$"";
                variables.filter.CostCenters = SelectedCostCenterId == 0 ? null : new int[] { SelectedCostCenterId };

                var result = await SellerService.GetPage(query, variables);
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

        protected override void OnViewReady(object view)
        {
            if (Context.EnableOnViewReady is false) return;
            base.OnViewReady(view);
            _ = this.SetFocus(nameof(FilterSearch));
        }

        public Task HandleAsync(SellerCreateMessage message, CancellationToken cancellationToken)
        {
            return Task.FromResult(Sellers = new ObservableCollection<SellerDTO>(Context.AutoMapper.Map<ObservableCollection<SellerDTO>>(message.Sellers)));
        }

        public Task HandleAsync(SellerUpdateMessage message, CancellationToken cancellationToken)
        {
            return Task.FromResult(Sellers = new ObservableCollection<SellerDTO>(Context.AutoMapper.Map<ObservableCollection<SellerDTO>>(message.Sellers)));
        }

        public Task HandleAsync(SellerDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                SellerDTO sellerToDelete = Sellers.FirstOrDefault(seller => seller.Id == message.DeletedSeller.Id);
                if (sellerToDelete != null) _ = Application.Current.Dispatcher.Invoke(() => Sellers.Remove(sellerToDelete));
                return LoadSellers();
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
