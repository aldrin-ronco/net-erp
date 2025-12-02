using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Billing;
using Models.Books;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using Services.Books.DAL.PostgreSQL;
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
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.Smtp.ViewModels
{
    public class SmtpMasterViewModel : Screen,
        IHandle<SmtpDeleteMessage>,
        IHandle<SmtpUpdateMessage>,
        IHandle<SmtpCreateMessage>
    {
        private readonly IRepository<SmtpGraphQLModel> _smtpService;
        private readonly Helpers.Services.INotificationService _notificationService;
        
        public SmtpViewModel Context { get; set; }

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


        private ObservableCollection<SmtpGraphQLModel> _smtps;

        public ObservableCollection<SmtpGraphQLModel> Smtps
        {
            get { return _smtps; }
            set 
            {
                if (_smtps != value) 
                {
                    _smtps = value;
                    NotifyOfPropertyChange(nameof(Smtps));
                }
            }
        }

        private SmtpGraphQLModel? _selectedItem = null;

        public SmtpGraphQLModel? SelectedItem
        {
            get { return _selectedItem; }
            set 
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(CanDeleteSmtp));
                }
            }
        }

        private string _filterSearch;

        public string FilterSearch
        {
            get { return _filterSearch; }
            set 
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if(string.IsNullOrEmpty(value) || value.Length >= 3) _ = LoadSmtpsAsync();
                }
            }
        }

        public bool CanDeleteSmtp
        {
            get 
            { 
                if(SelectedItem is null) return false;
                return true;
            }
        }

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
                if (_paginationCommand == null) this._paginationCommand = new AsyncCommand(ExecuteChangeIndex, CanExecuteChangeIndex);
                return _paginationCommand;
            }
        }

        private async Task ExecuteChangeIndex()
        {
            await LoadSmtpsAsync();
        }
        private bool CanExecuteChangeIndex()
        {
            return true;
        }

        private ICommand _deleteSmtpCommand;

        public ICommand DeleteSmtpCommand
        {
            get
            {
                if (_deleteSmtpCommand is null) _deleteSmtpCommand = new AsyncCommand(DeleteSmtpAsync);
                return _deleteSmtpCommand;
            }
        }
        public async Task DeleteSmtpAsync()
        {
            try
            {

                this.IsBusy = true;
                this.Refresh();

                string query = GetCanDeleteSmtpQuery();

                object variables = new { canDeleteResponseId = SelectedItem.Id };

                var validation = await _smtpService.CanDeleteAsync(query, variables);

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
                DeleteResponseType deletedSmtp = await Task.Run(() => this.ExecuteDeleteSmtpAsync(SelectedItem.Id));

                if (!deletedSmtp.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedSmtp.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await Context.EventAggregator.PublishOnUIThreadAsync(new SmtpDeleteMessage { DeletedSmtp = deletedSmtp });

                NotifyOfPropertyChange(nameof(CanDeleteSmtp));
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
        public string GetCanDeleteSmtpQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("canDeleteSmtp", [parameter], fields, alias: "CanDeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }
        
      
        public async Task<DeleteResponseType> ExecuteDeleteSmtpAsync(int id)
        {
            try
            {

                string query = GetDeleteSmtpQuery();

                object variables = new
                {
                    deleteResponseId = id
                };

                // Eliminar registros
                DeleteResponseType deletedRecord = await _smtpService.DeleteAsync<DeleteResponseType>(query, variables);
                return deletedRecord;
            }
            catch (Exception)
            {
                throw;
            }
        }
       
        public string GetDeleteSmtpQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("deleteSmtp", [parameter], fields, alias: "DeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public SmtpMasterViewModel(
            SmtpViewModel context,
            IRepository<SmtpGraphQLModel> smtpService,
            Helpers.Services.INotificationService notificationService)
        {
            Context = context;
            _smtpService = smtpService;
            _notificationService = notificationService;
            Context.EventAggregator.SubscribeOnPublishedThread(this);
        }

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            await LoadSmtpsAsync();
            this.SetFocus(() => FilterSearch);
        }

        public async Task EditSmtp()
        {
            await Context.ActivateDetailViewForEdit(SelectedItem ?? new());
        }
        public async Task LoadSmtpsAsync()
        {
            try
            {

                IsBusy = true;

                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponseFilters.name = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.Page = PageIndex;
                variables.pageResponsePagination.PageSize = PageSize;
                string query = GetLoadSmtpsQuery();

                PageType<SmtpGraphQLModel> result = await _smtpService.GetPageAsync(query, variables);
                this.Smtps = [.. result.Entries];
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
        public string GetLoadSmtpsQuery()
        {
            var smtpsFields = FieldSpec<PageType<SmtpGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Host)
                    .Field(e => e.Name)
                    .Field(e => e.Port)
                   )
                .Build();

            var smtpsParameters = new GraphQLQueryParameter("filters", "SmtpFilters");
            var smtpsPagParameters = new GraphQLQueryParameter("pagination", "Pagination");
            var smtpsFragment = new GraphQLQueryFragment("smtpsPage", [smtpsParameters, smtpsPagParameters], smtpsFields, "PageResponse");

            var builder = new GraphQLQueryBuilder([smtpsFragment]);

            return builder.GetQuery();
        }
       

        private ICommand _createSmtpCommand;

        public ICommand CreateSmtpCommand
        {
            get 
            {
                if (_createSmtpCommand is null) _createSmtpCommand = new AsyncCommand(CreateSmtpAsync);
                return _createSmtpCommand; 
            }

        }

        public async Task CreateSmtpAsync()
        {
            await Context.ActivateDetailViewForNew();
        }

        public async Task HandleAsync(SmtpDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadSmtpsAsync();
                _notificationService.ShowSuccess(message.DeletedSmtp.Message);
                return;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task HandleAsync(SmtpUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadSmtpsAsync();
                _notificationService.ShowSuccess(message.UpdatedSmtp.Message);
                return;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task HandleAsync(SmtpCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadSmtpsAsync();
                _notificationService.ShowSuccess(message.CreatedSmtp.Message);
                return;
            }
            catch (Exception)
            {

                throw;
            }
        }
        
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
