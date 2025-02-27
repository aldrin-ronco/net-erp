using Caliburn.Micro;
using Common.Extensions;
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

        private ZoneGraphQLModel? _SelectedItem = null;
        public ZoneGraphQLModel? SelectedItem
        {
            get { return _SelectedItem; }
            set
            {
                if (_SelectedItem != value)
                {
                    _SelectedItem = value;
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

        private ICommand? _createZoneCommand;
        public ICommand CreateZoneCommand
        {
            get
            {
                if (_createZoneCommand is null) _createZoneCommand = new AsyncCommand(CreateZoneAsync);
                return _createZoneCommand;
            }

        }

        private ICommand? _paginationCommand;
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
            await Context.ActivateDetailViewForNewAsync();
        }

        private ICommand? _deleteZoneCommand;
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
                // Si no hay item seleccionado, no coninuar
                if (SelectedItem is null) return;
                string query = @"mutation($id: Int!){
                DeleteResponse: deleteZone(id: $id){
                       id
                    }
                }";
                
                MessageBoxResult answer = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {SelectedItem.Name} ?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                if (answer != MessageBoxResult.Yes) return;

                dynamic variables = new ExpandoObject();
                variables.id = SelectedItem.Id;
                ZoneGraphQLModel result = await ZoneService.Delete(query, variables);
                if (result == null)
                {
                    MessageBox.Show($"No se pudo eliminar la zona: {SelectedItem.Name}, Intente de nuevo.");
                }
                SelectedItem = null;
                await LoadZonesAsync();
            }
            catch(Exception)
            {
                throw;
            }
        }

        public async Task EditZoneAsync()
        {
            await Context.ActivateDetailViewForEditAsync(SelectedItem ?? new());
        }
        public async Task LoadZonesAsync()
        {
            try
            {
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
            catch (Exception)
            {
                throw;
            }
        }
        public ZoneMasterViewModel(ZoneViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnPublishedThread(this);
            _ = Task.Run(() =>  LoadZonesAsync());

        }
        public Task HandleAsync(ZoneUpdateMessage message, CancellationToken cancellationToken)
        {
            return LoadZonesAsync();
        }
        public Task HandleAsync(ZoneCreateMessage message, CancellationToken cancellationToken)
        {
            return LoadZonesAsync();
        }
        public Task HandleAsync(ZoneDeleteMessage message, CancellationToken cancellationToken)
        {
            return LoadZonesAsync();
        }
    }
}
