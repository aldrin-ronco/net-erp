using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Billing;
using NetErp.Helpers;
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


namespace NetErp.Billing.Zones.ViewModels
{
    public class ZoneMasterViewModel : Screen, IHandle<ZoneCreateMessage>,
        IHandle<ZoneDeleteMessage>,
        IHandle<ZoneUpdateMessage>
    {
        public IGenericDataAccess<ZoneGraphQLModel> ZoneService { get; set; } = IoC.Get<IGenericDataAccess<ZoneGraphQLModel>>();
        private readonly Helpers.Services.INotificationService _notificationService = IoC.Get<Helpers.Services.INotificationService>();

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
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = Task.Run(() => LoadZonesAsync());
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
                    _ = Task.Run(() => LoadZonesAsync());
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
                await Task.Run(() => Context.ActivateDetailViewForNewAsync());
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
                // Si no hay item seleccionado, no continuar
                if (SelectedItem is null) return;
                int id = SelectedItem.Id;
                dynamic variables = new ExpandoObject();
                variables.id = id;


                string query = @"query($id:Int!){
                  CanDeleteModel: canDeleteZone(id: $id){
                    canDelete
                    message
                  }
                }";


                var validation = await this.ZoneService.CanDelete(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {SelectedItem.Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }

                ZoneGraphQLModel deletedZone = await ExecuteDeleteAsync(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new ZoneDeleteMessage() { DeletedZone = deletedZone });

                NotifyOfPropertyChange(nameof(CanDeleteZone));
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
        }

        public async Task<ZoneGraphQLModel> ExecuteDeleteAsync(int id)
        {
            try
            {
                string query = @"mutation($id: Int!){
                    DeleteResponse: deleteZone(id: $id){
                        id
                        name
                        isActive
                    }
                }";
                dynamic variables = new ExpandoObject();
                variables.id = id;
                var result = await ZoneService.Delete(query, variables);
                return result;
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public async Task EditZoneAsync()
        {
            try
            {
                await Task.Run(() => Context.ActivateDetailViewForEditAsync(SelectedItem ?? new()));
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
                string query = @"
                query($filter: ZoneFilterInput){
                     PageResponse :zonePage(filter: $filter) {
                        count
                        rows{
                            id
                            name
                            isActive
                        }
                    }
                }";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.name = new ExpandoObject();
                variables.filter.name.@operator = "like";
                variables.filter.name.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                if (ShowActiveZonesOnly) 
                {
                    variables.filter.isActive = new ExpandoObject();
                    variables.filter.isActive.@operator = "=";
                    variables.filter.isActive.value = ShowActiveZonesOnly;
                } 

                //Pagination
                variables.filter.pagination = new ExpandoObject();
                variables.filter.pagination.page = PageIndex;
                variables.filter.pagination.pageSize = PageSize;
                Stopwatch stopwatch= new();
                stopwatch.Start();
                var result = await ZoneService.GetPage(query, variables);
                TotalCount = result.PageResponse.Count;
                Zones = new ObservableCollection<ZoneGraphQLModel>(result.PageResponse.Rows);
                stopwatch.Stop();
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
        public ZoneMasterViewModel(ZoneViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnPublishedThread(this);
            _ = Task.Run(() =>  LoadZonesAsync());

        }
        public async Task HandleAsync(ZoneUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadZonesAsync();
                _notificationService.ShowSuccess("Zona eliminada correctamente");
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
                _notificationService.ShowSuccess("Zona creada correctamente");
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
                _notificationService.ShowSuccess("Zona eliminada correctamente");
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
