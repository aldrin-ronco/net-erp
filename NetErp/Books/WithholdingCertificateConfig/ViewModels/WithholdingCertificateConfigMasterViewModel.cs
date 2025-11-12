using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.DirectX.Common.DirectWrite;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using NetErp.Books.AccountingAccountGroups.DTO;
using NetErp.Books.AccountingAccountGroups.ViewModels;
using NetErp.Books.AccountingEntities.ViewModels;
using NetErp.Helpers.GraphQLQueryBuilder;
using Ninject.Activation;
using Services.Books.DAL.PostgreSQL;
using Services.Global.DAL.PostgreSQL;
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
using static DevExpress.Drawing.Printing.Internal.DXPageSizeInfo;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.WithholdingCertificateConfig.ViewModels
{
   public class WithholdingCertificateConfigMasterViewModel : Screen,
         IHandle<WithholdingCertificateConfigDeleteMessage>,
        IHandle<WithholdingCertificateConfigUpdateMessage>,
        IHandle<WithholdingCertificateConfigCreateMessage>
    {
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<WithholdingCertificateConfigGraphQLModel> _withholdingCertificateConfigService;

        public WithholdingCertificateConfigViewModel Context { get; set; }
        public WithholdingCertificateConfigMasterViewModel(WithholdingCertificateConfigViewModel context, Helpers.Services.INotificationService notificationService, IRepository<WithholdingCertificateConfigGraphQLModel> withholdingCertificateConfigService)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnUIThread(this);
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _withholdingCertificateConfigService = withholdingCertificateConfigService ?? throw new ArgumentNullException(nameof(withholdingCertificateConfigService));
        }
        private ObservableCollection<WithholdingCertificateConfigGraphQLModel> _certificates;

        public ObservableCollection<WithholdingCertificateConfigGraphQLModel> Certificates
        {
            get { return _certificates; }
            set
            {
                if (_certificates != value)
                {
                    _certificates = value;
                    NotifyOfPropertyChange(nameof(Certificates));
                }
            }
        }

        private WithholdingCertificateConfigGraphQLModel? _selectedWithholdingCertificateConfigGraphQLModel;
        public WithholdingCertificateConfigGraphQLModel? SelectedWithholdingCertificateConfigGraphQLModel
        {
            get { return _selectedWithholdingCertificateConfigGraphQLModel; }
            set
            {
                if (_selectedWithholdingCertificateConfigGraphQLModel != value)
                {
                    _selectedWithholdingCertificateConfigGraphQLModel = value;
                    NotifyOfPropertyChange(nameof(_selectedWithholdingCertificateConfigGraphQLModel));
                    // NotifyOfPropertyChange(nameof(CanDelete_selectedWithholdingCertificateConfigGraphQLModel));
                }
            }
        }
        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            _ = Task.Run(() => InitializeAsync());
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
            await LoadWithholdingCertificateConfig();
        }

        private bool CanExecuteChangeIndex(object parameter)
        {
            return true;
        }


        #endregion
        private ICommand _deleteCommand;
        public ICommand DeleteCommand
        {
            get
            {
                if (_deleteCommand is null) _deleteCommand = new AsyncCommand(DeleteCertificate, CanDeleteCertificate);
                return _deleteCommand;
            }
        }

        public bool CanDeleteCertificate
        {
            get
            {
                if (SelectedWithholdingCertificateConfigGraphQLModel is null) return false;
                return true;
            }
        }
        private ICommand _newCommand;

        public ICommand NewCommand
        {
            get
            {
                if (_newCommand is null) _newCommand = new AsyncCommand(NewAsync);
                return _newCommand;
            }
        }
        public async Task DeleteCertificate()
        {
            try
            {
                IsBusy = true;
                int id = SelectedWithholdingCertificateConfigGraphQLModel.Id;
                string query = GetCanDeleteWithholdingCertificateQuery();

                object variables = new { canDeleteResponseId = SelectedWithholdingCertificateConfigGraphQLModel.Id };

                var validation = await _withholdingCertificateConfigService.CanDeleteAsync(query, variables);

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
               
                Refresh();
                var deletedCertificate = await ExecuteDeleteCertificate(id);

                await Context.EventAggregator.PublishOnCurrentThreadAsync(new     WithholdingCertificateConfigDeleteMessage { DeletedWithholdingCertificateConfig = deletedCertificate });

                // Desactivar opcion de eliminar registros
                NotifyOfPropertyChange(nameof(CanDeleteCertificate));
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteWithholdingCertificateConfig" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
        public string GetCanDeleteWithholdingCertificateQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("canDeleteWithholdingCertificate", [parameter], fields, alias: "CanDeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }
        public string  GetDeleteWithholdingCertificateQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
               .Create()
               .Field(f => f.DeletedId)
               .Field(f => f.Message)
               .Field(f => f.Success)
               .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("deleteWithholdingCertificate", [parameter], fields, alias: "DeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public async Task<DeleteResponseType> ExecuteDeleteCertificate(int id)
        {
            try
            {

                string query = GetDeleteWithholdingCertificateQuery();

                object variables = new
                {
                    deleteResponseId = id
                };

                // Eliminar registros
                DeleteResponseType deletedRecord = await _withholdingCertificateConfigService.DeleteAsync<DeleteResponseType>(query, variables);
                return deletedRecord;
            }
            catch (Exception)
            {
                throw;
            }
           

        }
        public async Task NewAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                SelectedWithholdingCertificateConfigGraphQLModel = null;
                await Task.Run(() => ExecuteActivateWithholdingCertificateConfig());
                SelectedWithholdingCertificateConfigGraphQLModel = null;
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "NewWithholdingCertificateConfigEntity" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task EditWithholdingCertificateConfig()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteActivateWithholdingCertificateConfig());
                SelectedWithholdingCertificateConfigGraphQLModel = null;
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "EditWithholdingCertificateConfig" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task ExecuteActivateWithholdingCertificateConfig()
        {
            await Context.ActivateDetailViewForEdit(SelectedWithholdingCertificateConfigGraphQLModel);
        }
       
        public async Task InitializeAsync()
        {
           await LoadWithholdingCertificateConfig();
        }
        public async Task LoadWithholdingCertificateConfig()
        {
            try
            {
                this.Refresh();

                // Iniciar cronometro
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                IsBusy = true;
                
                string query = GetLoadWithholdingCertificateConfigQuery();
                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.Page = PageIndex;
                variables.pageResponsePagination.PageSize = PageSize;
                PageType<WithholdingCertificateConfigGraphQLModel> result =     await _withholdingCertificateConfigService.GetPageAsync(query, variables);
                stopwatch.Stop();
                this.ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
                Certificates =result.Entries;
                TotalCount = result.TotalEntries;

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
        public string GetLoadWithholdingCertificateConfigQuery()
        {
            var withholdingCertificateFields = FieldSpec<PageType<WithholdingCertificateConfigGraphQLModel>>
              .Create()
              .SelectList(it => it.Entries, entries => entries
                  .Field(e => e.Id)
                  .Field(e => e.Description)
                  .Field(e => e.Name)

                  
                  .SelectList(e => e.AccountingAccounts, cat => cat
                      .Field(c => c.Id)
                      .Field(c => c.Name)

                  )
                  .Select(e => e.CostCenter, cat => cat
                      .Field(c => c.Id)
                      .Field(c => c.Name)
                      .Field(c => c.Address)
                      .Select(e => e.City, cit => cit
                              .Field(d => d.Id)
                              .Field(d => d.Name)
                              .Select(d => d.Department, dep => dep
                              .Field(d => d.Id)
                              .Field(d => d.Name)
                          )
                          )
                      
                  
              )
                  )
              .Field(o => o.PageNumber)
              .Field(o => o.PageSize)
              .Field(o => o.TotalPages)
              .Field(o => o.TotalEntries)
              .Build();
            var withholdingCertificatePagParameters = new GraphQLQueryParameter("pagination", "Pagination");
            var withholdingCertificatefilterParameters = new GraphQLQueryParameter("filters", "WithholdingCertificateFilters");
            var withholdingCertificateFragment = new GraphQLQueryFragment("withholdingCertificatesPage", [withholdingCertificatePagParameters, withholdingCertificatefilterParameters], withholdingCertificateFields, "PageResponse");
            var builder =  new GraphQLQueryBuilder([withholdingCertificateFragment]);
            return builder.GetQuery();
        }
        public async Task HandleAsync(WithholdingCertificateConfigDeleteMessage message, CancellationToken cancellationToken)
        {
            _notificationService.ShowSuccess("El Certificado fue eliminado correctamente");
            await LoadWithholdingCertificateConfig();
           
        }

        public async Task HandleAsync(WithholdingCertificateConfigUpdateMessage message, CancellationToken cancellationToken)
        {
            _notificationService.ShowSuccess("El Certificado fue actualizado correctamente");
            await LoadWithholdingCertificateConfig();
            
        }

        public async Task HandleAsync(WithholdingCertificateConfigCreateMessage message, CancellationToken cancellationToken)
        {
            _notificationService.ShowSuccess("El Certificado fue creado correctamente");
            await LoadWithholdingCertificateConfig();
            
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {

            // Desconectar eventos para evitar memory leaks
            if (close)
            {
                Context.EventAggregator.Unsubscribe(this);
            }
            
            return base.OnDeactivateAsync(close, cancellationToken);
        }
    }
}
