using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DTOLibrary.Books;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using NetErp.Books.WithholdingCertificateConfig.ViewModels;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static DevExpress.Drawing.Printing.Internal.DXPageSizeInfo;

namespace NetErp.Global.AuthorizationSequence.ViewModels
{
    public class AuthorizationSequenceMasterViewModel  : Screen,
         IHandle<AuthorizationSequenceDeleteMessage>,
        IHandle<AuthorizationSequenceUpdateMessage>,
        IHandle<AuthorizationSequenceCreateMessage>
    {
        public AuthorizationSequenceViewModel Context { get; set; }
        public IGenericDataAccess<AuthorizationSequenceGraphQLModel> AuthorizationSequenceService { get; set; } = IoC.Get<IGenericDataAccess<AuthorizationSequenceGraphQLModel>>();

        public AuthorizationSequenceMasterViewModel(AuthorizationSequenceViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnUIThread(this);
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

        private AuthorizationSequenceGraphQLModel? _selectedAuthorizationSequenceGraphQLModel;
        public AuthorizationSequenceGraphQLModel? SelectedAuthorizationSequenceGraphQLModel
        {
            get { return _selectedAuthorizationSequenceGraphQLModel; }
            set
            {
                if (_selectedAuthorizationSequenceGraphQLModel != value)
                {
                    _selectedAuthorizationSequenceGraphQLModel = value;
                    NotifyOfPropertyChange(nameof(CanDeleteAuthorizationSecuence));
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
        
        public async Task EditAuthorizationSequence()
             {
                try
                {
                    IsBusy = true;
                    Refresh();
                await Task.Run(() => ExecuteActivateDetailViewForEdit());
                   
                    SelectedAuthorizationSequenceGraphQLModel = null;
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
        public async Task NewAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteActivateDetailViewForEdit());
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
            await LoadAuthorizationSequence();
        }

        private bool CanExecuteChangeIndex(object parameter)
        {
            return true;
        }
        public bool CanDeleteAuthorizationSecuence
        {
            get
            {
                if (SelectedAuthorizationSequenceGraphQLModel is null) return false;
                return true;
            }
        }
        private ICommand _deleteAuthorizationSecuenceCommand;
        public ICommand DeleteAuthorizationSecuenceCommand
        {
            get
            {
                if (_deleteAuthorizationSecuenceCommand is null) _deleteAuthorizationSecuenceCommand = new AsyncCommand(DeleteAuthorizationSecuence, CanDeleteAuthorizationSecuence);
                return _deleteAuthorizationSecuenceCommand;
            }
        }
        
        #endregion
        #region methods
        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            _ = Task.Run(() => InitializeAsync());
        }
        public async Task ExecuteActivateDetailViewForEdit()
        {
            await Context.ActivateDetailViewForEdit(SelectedAuthorizationSequenceGraphQLModel);
        }

        public async Task InitializeAsync()
        {
            await LoadAuthorizationSequence();
        }
        public async Task LoadAuthorizationSequence()
        {
            try
            {
                this.Refresh();

                // Iniciar cronometro
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                IsBusy = true;
                string query = @"
               query( $filter: AuthorizationSequenceFilterInput!){
                      PageResponse: authorizationSequencePage(filter: $filter){
                        count
                        rows {
                          id
                            description
                            number
                            costCenter  {
                             id
                             name
                           }
                           authorizationSequenceType {
                             id
                           }
                           startRange
                           endRange
                           endDate
                           startDate
                           endDate
                           isActive
                           prefix
                           currentInvoiceNumber
                           mode
                           technicalKey
                           reference
                        }
                      }
                    }
                ";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.Pagination = new ExpandoObject();
                variables.filter.Pagination.Page = PageIndex;
                variables.filter.Pagination.PageSize = PageSize;

                var result = await AuthorizationSequenceService.GetPage(query, variables);
                stopwatch.Stop();
                this.ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
                Authorizations = Context.AutoMapper.Map<ObservableCollection<AuthorizationSequenceGraphQLModel>>(result.PageResponse.Rows);
                TotalCount = result.PageResponse.Count;

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

        public async Task DeleteAuthorizationSecuence()
        {
            try
            {
                IsBusy = true;
                int id = SelectedAuthorizationSequenceGraphQLModel.Id;

                string query = @"
                query($id:Int!) {
                  CanDeleteModel: canDeleteAuthorizationSequence(id:$id) {
                    canDelete
                    message
                  }
                }";
                object variables = new { Id = id };

                var validation = await this.AuthorizationSequenceService.CanDelete(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Atención!", text: $"¿Confirma que desea eliminar el registro {SelectedAuthorizationSequenceGraphQLModel.Description}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "La autorizacion no puede ser eliminada" +
                        (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }


                Refresh();
                var deletedAuthorizationSequence = await ExecuteDeleteAuthorizationSequence(id);

                await Context.EventAggregator.PublishOnCurrentThreadAsync(new AuthorizationSequenceDeleteMessage() { DeletedAuthorizationSequence = deletedAuthorizationSequence });

                // Desactivar opcion de eliminar registros
                NotifyOfPropertyChange(nameof(CanDeleteAuthorizationSecuence));
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
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteAuthorizationSequence" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task<AuthorizationSequenceGraphQLModel> ExecuteDeleteAuthorizationSequence(int id)
        {
            try
            {
                string query = @"
                mutation ($id: Int!) {
                  DeleteResponse: deleteAuthorizationSequence(id: $id) {
                    id
                  }
                }";
                object variables = new { Id = id };
                var deletedAuthorization = await this.AuthorizationSequenceService.Delete(query, variables);
                this.SelectedAuthorizationSequenceGraphQLModel = null;
                return deletedAuthorization;
            }
           
            catch (Exception ex)
            {
                throw;
            }
            
        }

        public Task HandleAsync(AuthorizationSequenceCreateMessage message, CancellationToken cancellationToken)
        {
            return Task.FromResult(Authorizations = new ObservableCollection<AuthorizationSequenceGraphQLModel>(message.AuthorizationSequences));
        }


        public Task HandleAsync(AuthorizationSequenceUpdateMessage message, CancellationToken cancellationToken)
        {
            return Task.FromResult(Authorizations = new ObservableCollection<AuthorizationSequenceGraphQLModel>(message.AuthorizationSequences));
        }
        public Task HandleAsync(AuthorizationSequenceDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                AuthorizationSequenceGraphQLModel authorizationSequenceToDelete = Authorizations.First(c => c.Id == message.DeletedAuthorizationSequence.Id);
                if (authorizationSequenceToDelete != null) _ = Application.Current.Dispatcher.Invoke(() => Authorizations.Remove(authorizationSequenceToDelete));
                return LoadAuthorizationSequence();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Task HandleAsync(AccountingBookDeleteMessage message, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
