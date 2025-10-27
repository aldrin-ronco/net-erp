using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Billing;
using Models.Books;
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


namespace NetErp.Billing.Zones.ViewModels
{
    public class ZoneMasterViewModel : Screen, IHandle<ZoneCreateMessage>,
        IHandle<ZoneDeleteMessage>,
        IHandle<ZoneUpdateMessage>
    {
        private readonly IRepository<ZoneGraphQLModel> _zoneService;
        private readonly Helpers.Services.INotificationService _notificationService;

        public ZoneViewModel Context { get; set; }

        private ObservableCollection<ZoneGraphQLModel> _zones;
        public ObservableCollection<ZoneGraphQLModel> Zones
        {
            get { return _zones; }
            set
            {
                if (_zones != value)
                {
                    _zones = value;
                    NotifyOfPropertyChange(nameof(Zones));
                }
            }
        }

        private ZoneGraphQLModel? _selectedItem = null;
        public ZoneGraphQLModel? SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(CanDeleteZone));
                }
            }
        }

        public bool CanDeleteZone
        {
            get
            {
                return SelectedItem != null;
            }
        }

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
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = LoadZonesAsync();
                }
            }
        }

        private bool _showActiveZonesOnly = true;
        public bool ShowActiveZonesOnly
        {
            get { return _showActiveZonesOnly; }
            set 
            { 
                if(_showActiveZonesOnly != value)
                {
                    _showActiveZonesOnly = value;
                    NotifyOfPropertyChange(nameof(ShowActiveZonesOnly));
                    _ = LoadZonesAsync();
                }
            }
        }

        private bool _isBusy = true;

        public bool IsBusy
        {
            get { return _isBusy; }
            set 
            {
                if(_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    NotifyOfPropertyChange(nameof(CanDeleteZone));
                }
            }
        }



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

        private ICommand _createZoneCommand;
        public ICommand CreateZoneCommand
        {
            get
            {
                if (_createZoneCommand is null) _createZoneCommand = new AsyncCommand(CreateZoneAsync);
                return _createZoneCommand;
            }

        }

        private ICommand _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                if (_paginationCommand == null) _paginationCommand = new AsyncCommand(ExecuteChangeIndexAsync);
                return _paginationCommand;
            }
        }

        public async Task ExecuteChangeIndexAsync()
        {
            await LoadZonesAsync();
        }
        public async Task CreateZoneAsync()
        {
            try
            {
                await Context.ActivateDetailViewForNewAsync();
            }
            catch (AsyncException ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }

        private ICommand _deleteZoneCommand;
        public ICommand DeleteZoneCommand
        {
            get
            {
                if (_deleteZoneCommand is null) _deleteZoneCommand = new AsyncCommand(DeleteZoneAsync);
                return _deleteZoneCommand;
            }

        }

        public async Task DeleteZoneAsync()
        {
         

            try
            {
                if (SelectedItem is null) return;
                int id = SelectedItem.Id;
                this.IsBusy = true;
                this.Refresh();

                string query = GetCanDeleteZoneQuery();

                object variables = new { canDeleteResponseId = SelectedItem.Id };

                var validation = await _zoneService.CanDeleteAsync(query, variables);

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
                DeleteResponseType deletedZone = await Task.Run(() => this.ExecuteDeleteAsync(id));

                if (!deletedZone.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedZone.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await Context.EventAggregator.PublishOnUIThreadAsync(new ZoneDeleteMessage { DeletedZone = deletedZone });

                NotifyOfPropertyChange(nameof(CanDeleteZone));
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
        public string GetDeleteZoneQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("deleteZone", [parameter], fields, alias: "DeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public string GetCanDeleteZoneQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("canDeleteZone", [parameter], fields, alias: "CanDeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }
        public async Task<DeleteResponseType> ExecuteDeleteAsync(int id)
        {
            try
            {
                string query = GetDeleteZoneQuery();

                object variables = new
                {
                    deleteResponseId = id
                };

                // Eliminar registros
                DeleteResponseType deletedRecord = await _zoneService.DeleteAsync<DeleteResponseType>(query, variables);
                return deletedRecord;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task EditZoneAsync()
        {
            try
            {
                await Context.ActivateDetailViewForEditAsync(SelectedItem ?? new());
            }
            catch (AsyncException ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }
        public async Task LoadZonesAsync()
        {
         
            try
            {
                IsBusy = true;

                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponseFilters.name = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();
                string query = GetLoadZonesQuery();

                PageType<ZoneGraphQLModel> result = await _zoneService.GetPageAsync(query, variables);
                this.Zones = result.Entries ;
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
        public string GetLoadZonesQuery()
        {
            var zoneFields = FieldSpec<PageType<ZoneGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.IsActive))
                .Build();

            var zoneParameters = new GraphQLQueryParameter("filters", "ZoneFilters");

            var zoneFragment = new GraphQLQueryFragment("zonesPage", [zoneParameters], zoneFields, "PageResponse");

            var builder = new GraphQLQueryBuilder([zoneFragment]);

            return builder.GetQuery();
        }
        public ZoneMasterViewModel(
            ZoneViewModel context,
            IRepository<ZoneGraphQLModel> zoneService,
            Helpers.Services.INotificationService notificationService)
        {
            Context = context;
            _zoneService = zoneService;
            _notificationService = notificationService;
            Context.EventAggregator.SubscribeOnPublishedThread(this);
            _ = LoadZonesAsync();
        }
        public async Task HandleAsync(ZoneUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadZonesAsync();
                _notificationService.ShowSuccess(message.UpdatedZone.Message);
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task HandleAsync(ZoneCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadZonesAsync();
                _notificationService.ShowSuccess(message.CreatedZone.Message);
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task HandleAsync(ZoneDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadZonesAsync();
                _notificationService.ShowSuccess(message.DeletedZone.Message);
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
