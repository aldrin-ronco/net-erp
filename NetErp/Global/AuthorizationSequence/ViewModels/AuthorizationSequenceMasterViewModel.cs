using Amazon.S3.Model;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using Common.Validators;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Pdf.Drawing.DirectX;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Billing;
using Models.Books;
using Models.Global;
using NetErp.Books.WithholdingCertificateConfig.ViewModels;
using NetErp.Global.CostCenters.DTO;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using Services.Books.DAL.PostgreSQL;
using Services.Global.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Amazon.S3.Util.S3EventNotification;
using static Chilkat.Http;
using static DevExpress.Drawing.Printing.Internal.DXPageSizeInfo;
using static Models.Global.GraphQLResponseTypes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NetErp.Global.AuthorizationSequence.ViewModels
{
    public class AuthorizationSequenceMasterViewModel  : Screen,
      
         IHandle<AuthorizationSequenceDeleteMessage>,
        IHandle<AuthorizationSequenceUpdateMessage>,
        IHandle<AuthorizationSequenceCreateMessage>
    {
        public AuthorizationSequenceViewModel Context { get; set; }
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AuthorizationSequenceGraphQLModel> _authorizationSequenceService;
        public AuthorizationSequenceMasterViewModel(AuthorizationSequenceViewModel context, Helpers.Services.INotificationService notificationService, IRepository<AuthorizationSequenceGraphQLModel> authorizationSequenceService)
        {
            Context = context;
            _notificationService = notificationService;
            _authorizationSequenceService = authorizationSequenceService;
            Context.EventAggregator.SubscribeOnUIThread(this);

            _ = InitializeAsync();
        }

        #region Properties

        private ObservableCollection<AuthorizationSequenceGraphQLModel> _authorizations;

        public ObservableCollection<AuthorizationSequenceGraphQLModel> Authorizations
        {
            get { return _authorizations; }
            set
            {
                if (_authorizations != value)
                {
                    _authorizations = value;
                    NotifyOfPropertyChange(nameof(Authorizations));
                }
            }
        }
        private CostCenterGraphQLModel _selectedCostCenter;
        public CostCenterGraphQLModel SelectedCostCenter
        {
            get { return _selectedCostCenter; }
            set
            {
                if (_selectedCostCenter != value)
                {
                    _selectedCostCenter = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenter));
                    PageIndex = 1;
                    if (_isLoaded)
                    {
                        _ = this.LoadAuthorizationSequenceAsync();
                    }
                   
                }
            }
        }
        private bool _isActive = true;
        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    NotifyOfPropertyChange(nameof(IsActive));
                   
                        PageIndex = 1;
                        _ = this.LoadAuthorizationSequenceAsync();
                    
                }
            }
        }
        private bool _isLoaded = false;
        private ObservableCollection<CostCenterGraphQLModel> _costCenters;

        public ObservableCollection<CostCenterGraphQLModel> CostCenters
        {
            get { return _costCenters; }
            set
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        }
        private AuthorizationSequenceGraphQLModel? _selectedAuthorizationSequenceGraphQLModel;
        public AuthorizationSequenceGraphQLModel? SelectedAuthorizationSequenceGraphQLModel
        {
            get { return _selectedAuthorizationSequenceGraphQLModel; }
            set
            {
                if (_selectedAuthorizationSequenceGraphQLModel != value)
                {
                    _selectedAuthorizationSequenceGraphQLModel = value;
                    NotifyOfPropertyChange(nameof(CanDeleteAuthorizationSequence));
                    NotifyOfPropertyChange(nameof(SelectedAuthorizationSequenceGraphQLModel));
                }
            }
        }
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

        #endregion



        #region Command
        private ICommand _newCommand;

        public ICommand NewCommand
        {
            get
            {
                if (_newCommand is null) _newCommand = new AsyncCommand(NewAsync);
                return _newCommand;
            }
        }
        
        public async Task EditAuthorizationSequenceAsync()
             {
                try
                {
                    IsBusy = true;
                    Refresh();
                await  ExecuteActivateDetailViewForEditAsync();
                   
                    SelectedAuthorizationSequenceGraphQLModel = null;
                }
                catch (Exception ex)
                {
                    System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "EditAuthorizationSequencenc" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                finally
                {
                    IsBusy = false;
                }
          }
        public async Task NewAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                SelectedAuthorizationSequenceGraphQLModel = null;
                await Task.Run(() => ExecuteActivateDetailViewForEditAsync());
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "NewAuthorizationSequence" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
        #endregion
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
        private async void ExecuteChangeIndex(object parameter)
        {
            await LoadAuthorizationSequenceAsync();
        }

        private bool CanExecuteChangeIndex(object parameter)
        {
            return true;
        }
        public bool CanDeleteAuthorizationSequence
        {
            get
            {
                if (SelectedAuthorizationSequenceGraphQLModel is null) return false;
                return true;
            }
        }
        private ICommand _deleteAuthorizationSequenceCommand;
        public ICommand DeleteAuthorizationSequenceCommand
        {
            get
            {
                if (_deleteAuthorizationSequenceCommand is null) _deleteAuthorizationSequenceCommand = new AsyncCommand(DeleteAuthorizationSequenceAsync, CanDeleteAuthorizationSequence);
                return _deleteAuthorizationSequenceCommand;
            }
        }

        #endregion
        #region Filtro
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
                    NotifyOfPropertyChange(() => FilterSearch);
                    // Solo ejecutamos la busqueda si esta vacio el filtro o si hay por lo menos 3 caracteres digitados
                    if (string.IsNullOrEmpty(value) || value.Length >= 3)
                    {
                        PageIndex = 1;
                        _ = this.LoadAuthorizationSequenceAsync();
                    }
                    ;
                }
            }
        }

#endregion
        #region methods
        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
           
        }
        public async Task ExecuteActivateDetailViewForEditAsync()
        {
            await Context.ActivateDetailViewForEdit(SelectedAuthorizationSequenceGraphQLModel, Context.AutoMapper.Map<ObservableCollection<CostCenterDTO>>(CostCenters));
        }

        public async Task InitializeAsync()
        {
           
            await LoadListAsync();
            this.SetFocus(() => FilterSearch);
        }

        private async Task LoadListAsync()
        {

            this.Refresh();

            // Iniciar cronometro
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            IsBusy = true;
           
            string query = GetLoadAuthorizationSequenceQuery(true);

            dynamic variables = new ExpandoObject();
            variables.authorizationSequencesFilters = new ExpandoObject();
            variables.pageResponsePagination = new ExpandoObject();
            variables.pageResponsePagination.page = PageIndex;
            variables.pageResponsePagination.pageSize = PageSize;

           
            variables.authorizationSequencesFilters.isActive = IsActive;
            variables.authorizationSequencesFilters.number = FilterSearch?.Length > 0 ? FilterSearch.Trim().RemoveExtraSpaces() : "";
           
            
            try
            {
               
                AuthorizationSequenceDataContext source = await _authorizationSequenceService.GetDataContextAsync<AuthorizationSequenceDataContext>(query, variables);

                ObservableCollection<CostCenterGraphQLModel> costCenter = source.CostCenters.Entries;
                costCenter.Insert(0, new CostCenterGraphQLModel() { Id = 0, Name = "SELECCIONE CENTRO DE COSTO" });
                CostCenters = [.. costCenter];
                stopwatch.Stop();
                this.ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
                Authorizations = Context.AutoMapper.Map<ObservableCollection<AuthorizationSequenceGraphQLModel>>(source.AuthorizationSequences.Entries);
                TotalCount = source.AuthorizationSequences.Count;
                _isLoaded = true;
            }
            catch (Exception e)
            {
                var a = 3;
            }
            finally
            {
                IsBusy = false;
            }

        }

        public async Task LoadAuthorizationSequenceAsync()
        {
            

            try
            {
                this.Refresh();

                // Iniciar cronometro
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                IsBusy = true;

                string query = GetLoadAuthorizationSequenceQuery();

                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.page = PageIndex;
                variables.pageResponsePagination.pageSize = PageSize;

                if (IsActive) { variables.pageResponseFilters.isActive = IsActive; }
                
                if (SelectedCostCenter != null && SelectedCostCenter?.Id > 0)
                {
                    variables.pageResponseFilters.costCenterId = SelectedCostCenter?.Id;

                }
                variables.pageResponseFilters.matching = FilterSearch?.Length > 0 ? FilterSearch.Trim().RemoveExtraSpaces() : "";

                PageType<AuthorizationSequenceGraphQLModel> result = await _authorizationSequenceService.GetPageAsync(query, variables);
                this.Authorizations = [.. result.Entries ];
                this.ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
                TotalCount = result.Count;
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
                //Initialized = true;
                IsBusy = false;
            }
            
        }
        public string GetLoadAuthorizationSequenceQuery(bool withCostCenter = false)
        {
              var authorizationFields = FieldSpec<PageType<AuthorizationSequenceGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)

                    .Field(e => e.Description)
                    .Field(e => e.Number)
                    .Field(e => e.IsActive)
                    .Field(e => e.Prefix)
                    .Field(e => e.CurrentInvoiceNumber)
                    .Field(e => e.Mode)
                    .Field(e => e.TechnicalKey)
                    .Field(e => e.Reference)
                    .Field(e => e.StartDate)
                    .Field(e => e.EndDate)
                    .Field(e => e.StartRange)
                    .Field(e => e.EndRange)
                    .Select(e => e.NextAuthorizationSequence, cat => cat
                        .Field(c => c.Id)
                        .Field(c => c.Description)

                    )
                    .Select(e => e.CostCenter, cat => cat
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                        .Select(e => e.FeCreditDefaultAuthorizationSequence, dep => dep
                                .Field(d => d.Id)
                                .Field(d => d.Description)
                            )
                        .Select(e => e.FeCashDefaultAuthorizationSequence, dep => dep
                                .Field(d => d.Id)
                                .Field(d => d.Description)
                            )
                    )
                    .Select(e => e.AuthorizationSequenceType, cat => cat
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                    )
                )
                .Field(o => o.PageNumber)
                .Field(o => o.PageSize)
                .Field(o => o.TotalPages)
                .Field(o => o.TotalEntries)
                .Build();
            var costCenterFields = FieldSpec<PageType<CostCenterGraphQLModel>>
               .Create()
               .SelectList(it => it.Entries, entries => entries
                   .Field(e => e.Id)
                   .Field(e => e.Name)
                   .Field(e => e.Address)
                   .Select(e => e.City, cat => cat
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                            .Select(c => c.Department, dep => dep
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
            var authorizationPagParameters = new GraphQLQueryParameter("pagination", "Pagination");
            var authorizationfilterParameters = new GraphQLQueryParameter("filters", "AuthorizationSequenceFilters");
            var authorizationFragment = new GraphQLQueryFragment("authorizationSequencesPage", [authorizationPagParameters, authorizationfilterParameters], authorizationFields, withCostCenter? "AuthorizationSequences" : "PageResponse");
            var costCenterFragment = new GraphQLQueryFragment("costCentersPage", [], costCenterFields, "CostCenters");

            var builder = withCostCenter ? new GraphQLQueryBuilder([authorizationFragment, costCenterFragment]) :  new GraphQLQueryBuilder([authorizationFragment]);
            return builder.GetQuery();
        }
        public async Task DeleteAuthorizationSequenceAsync()
        {
            try
            {
                if (SelectedAuthorizationSequenceGraphQLModel is null) return;
                int id = SelectedAuthorizationSequenceGraphQLModel.Id;
                this.IsBusy = true;
                this.Refresh();

                string query = GetCanDeleteAuthorizationSequence();

                object variables = new { canDeleteResponseId = SelectedAuthorizationSequenceGraphQLModel.Id };

                var validation = await _authorizationSequenceService.CanDeleteAsync(query, variables);

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
                DeleteResponseType deletedAuthorizationSequence = await Task.Run(() => this.ExecuteDeleteAuthorizationSequenceAsync(id));

                if (!deletedAuthorizationSequence.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedAuthorizationSequence.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await Context.EventAggregator.PublishOnUIThreadAsync(new AuthorizationSequenceDeleteMessage { DeletedAuthorizationSequence = deletedAuthorizationSequence });

                NotifyOfPropertyChange(nameof(CanDeleteAuthorizationSequence));
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
        public string GetDeleteAuthorizationSequenceQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("deleteAuthorizationSequence", [parameter], fields, alias: "DeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public string GetCanDeleteAuthorizationSequence()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("canDeleteAuthorizationSequence", [parameter], fields, alias: "CanDeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }
        public async Task<DeleteResponseType> ExecuteDeleteAuthorizationSequenceAsync(int id)
        {
            try
            {

                string query = GetDeleteAuthorizationSequenceQuery();

                object variables = new
                {
                    deleteResponseId = id
                };

                // Eliminar registros
                DeleteResponseType deletedRecord = await _authorizationSequenceService.DeleteAsync<DeleteResponseType>(query, variables);
                return deletedRecord;
            }
            catch (Exception)
            {
                throw;
            }
            
            
        }
        public async Task HandleAsync(AuthorizationSequenceCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadAuthorizationSequenceAsync();
                _notificationService.ShowSuccess(message.CreatedAuthorizationSequence.Message);
            }
            catch (Exception)
            {

                throw;
            }
        }
        
        public Task HandleAsync(AuthorizationSequenceUpdateMessage message, CancellationToken cancellationToken)
        {
            _notificationService.ShowSuccess(message.UpdatedAuthorizationSequence.Message);
            return LoadAuthorizationSequenceAsync();
             
        }
        public Task HandleAsync(AuthorizationSequenceDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                _notificationService.ShowSuccess(message.DeletedAuthorizationSequence.Message);
                AuthorizationSequenceGraphQLModel authorizationSequenceToDelete = Authorizations.First(c => c.Id == message.DeletedAuthorizationSequence.DeletedId);
                if (authorizationSequenceToDelete != null) _ = Application.Current.Dispatcher.Invoke(() => Authorizations.Remove(authorizationSequenceToDelete));
                return LoadAuthorizationSequenceAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion
        protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                Context.EventAggregator.Unsubscribe(this);
            }
            await base.OnDeactivateAsync(close, cancellationToken);
        }

    }

    
}
