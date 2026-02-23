using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Inventory;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Inventory.MeasurementUnits.ViewModels
{
    public class MeasurementUnitViewModel : Screen,
        IHandle<MeasurementUnitCreateMessage>,
        IHandle<MeasurementUnitUpdateMessage>,
        IHandle<MeasurementUnitDeleteMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<MeasurementUnitGraphQLModel> _measurementUnitService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;

        #endregion

        #region Grid Properties

        private bool _isBusy;
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

        private ObservableCollection<MeasurementUnitGraphQLModel> _measurementUnits = [];
        public ObservableCollection<MeasurementUnitGraphQLModel> MeasurementUnits
        {
            get => _measurementUnits;
            set
            {
                if (_measurementUnits != value)
                {
                    _measurementUnits = value;
                    NotifyOfPropertyChange(nameof(MeasurementUnits));
                }
            }
        }

        private MeasurementUnitGraphQLModel? _selectedMeasurementUnit;
        public MeasurementUnitGraphQLModel? SelectedMeasurementUnit
        {
            get => _selectedMeasurementUnit;
            set
            {
                if (_selectedMeasurementUnit != value)
                {
                    _selectedMeasurementUnit = value;
                    NotifyOfPropertyChange(nameof(SelectedMeasurementUnit));
                    NotifyOfPropertyChange(nameof(CanEditMeasurementUnit));
                    NotifyOfPropertyChange(nameof(CanDeleteMeasurementUnit));
                }
            }
        }

        private string _filterSearch = string.Empty;
        public string FilterSearch
        {
            get => _filterSearch;
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = LoadMeasurementUnitsAsync();
                }
            }
        }

        private int _pageIndex = 1;
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

        private int _pageSize = 50;
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

        private int _totalCount;
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

        private string _responseTime = string.Empty;
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

        #endregion

        #region Button States

        public bool CanEditMeasurementUnit => SelectedMeasurementUnit != null;
        public bool CanDeleteMeasurementUnit => SelectedMeasurementUnit != null;

        #endregion

        #region Commands

        private ICommand? _createMeasurementUnitCommand;
        public ICommand CreateMeasurementUnitCommand
        {
            get
            {
                _createMeasurementUnitCommand ??= new AsyncCommand(CreateMeasurementUnitAsync);
                return _createMeasurementUnitCommand;
            }
        }

        private ICommand? _editMeasurementUnitCommand;
        public ICommand EditMeasurementUnitCommand
        {
            get
            {
                _editMeasurementUnitCommand ??= new AsyncCommand(EditMeasurementUnitAsync);
                return _editMeasurementUnitCommand;
            }
        }

        private ICommand? _deleteMeasurementUnitCommand;
        public ICommand DeleteMeasurementUnitCommand
        {
            get
            {
                _deleteMeasurementUnitCommand ??= new AsyncCommand(DeleteMeasurementUnitAsync);
                return _deleteMeasurementUnitCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadMeasurementUnitsAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public MeasurementUnitViewModel(
            IMapper mapper,
            IEventAggregator eventAggregator,
            IRepository<MeasurementUnitGraphQLModel> measurementUnitService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService)
        {
            _eventAggregator = eventAggregator;
            _measurementUnitService = measurementUnitService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _eventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            await LoadMeasurementUnitsAsync();
        }

        protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                _eventAggregator.Unsubscribe(this);
            }
            await base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region CRUD Operations

        public async Task CreateMeasurementUnitAsync()
        {
            var detail = new MeasurementUnitDetailViewModel(_measurementUnitService, _eventAggregator);
            await _dialogService.ShowDialogAsync(detail, "Nueva unidad de medida");
        }

        public async Task EditMeasurementUnitAsync()
        {
            if (SelectedMeasurementUnit == null) return;
            var detail = new MeasurementUnitDetailViewModel(_measurementUnitService, _eventAggregator);
            detail.MeasurementUnitId = SelectedMeasurementUnit.Id;
            detail.Name = SelectedMeasurementUnit.Name;
            detail.Abbreviation = SelectedMeasurementUnit.Abbreviation;
            detail.Type = SelectedMeasurementUnit.Type;
            detail.DianCode = SelectedMeasurementUnit.DianCode;
            detail.AcceptChanges();
            await _dialogService.ShowDialogAsync(detail, "Editar unidad de medida");
        }

        public async Task DeleteMeasurementUnitAsync()
        {
            if (SelectedMeasurementUnit == null) return;
            try
            {
                IsBusy = true;
                Refresh();

                string query = GetCanDeleteMeasurementUnitQuery();
                object variables = new { canDeleteResponseId = SelectedMeasurementUnit.Id };
                var validation = await _measurementUnitService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    if (ThemedMessageBox.Show("Atención !",
                        "¿Confirma que desea eliminar el registro seleccionado?",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                }
                else
                {
                    IsBusy = false;
                    ThemedMessageBox.Show("Atención !",
                        "El registro no puede ser eliminado" + (char)13 + (char)13 + validation.Message,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                DeleteResponseType deletedMeasurementUnit = await Task.Run(() => ExecuteDeleteMeasurementUnitAsync(SelectedMeasurementUnit.Id));

                if (!deletedMeasurementUnit.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro \n\n {deletedMeasurementUnit.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await _eventAggregator.PublishOnUIThreadAsync(new MeasurementUnitDeleteMessage { DeletedMeasurementUnit = deletedMeasurementUnit });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content!.ToString()!);
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()!.Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<DeleteResponseType> ExecuteDeleteMeasurementUnitAsync(int id)
        {
            string query = GetDeleteMeasurementUnitQuery();
            object variables = new { deleteResponseId = id };
            return await _measurementUnitService.DeleteAsync<DeleteResponseType>(query, variables);
        }

        #endregion

        #region Load

        public async Task LoadMeasurementUnitsAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = new();
                stopwatch.Start();

                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponseFilters.name = string.IsNullOrEmpty(FilterSearch)
                    ? ""
                    : FilterSearch.Trim().RemoveExtraSpaces();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.Page = PageIndex;
                variables.pageResponsePagination.PageSize = PageSize;

                string query = GetLoadMeasurementUnitsQuery();
                PageType<MeasurementUnitGraphQLModel> result = await _measurementUnitService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                MeasurementUnits = [.. result.Entries];
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content!.ToString()!);
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()!.Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        public string GetLoadMeasurementUnitsQuery()
        {
            var fields = FieldSpec<PageType<MeasurementUnitGraphQLModel>>
                .Create()
                .Field(it => it.TotalEntries)
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Abbreviation)
                    .Field(e => e.Name)
                    .Field(e => e.Type)
                    .Field(e => e.DianCode))
                .Build();

            var filtersParameter = new GraphQLQueryParameter("filters", "MeasurementUnitFilters");
            var paginationParameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("measurementUnitsPage", [filtersParameter, paginationParameter], fields, "PageResponse");

            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        public string GetDeleteMeasurementUnitQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("deleteMeasurementUnit", [parameter], fields, alias: "DeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetCanDeleteMeasurementUnitQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("canDeleteMeasurementUnit", [parameter], fields, alias: "CanDeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        #endregion

        #region Event Handlers

        public async Task HandleAsync(MeasurementUnitCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadMeasurementUnitsAsync();
            _notificationService.ShowSuccess(message.CreatedMeasurementUnit.Message);
        }

        public async Task HandleAsync(MeasurementUnitUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadMeasurementUnitsAsync();
            _notificationService.ShowSuccess(message.UpdatedMeasurementUnit.Message);
        }

        public async Task HandleAsync(MeasurementUnitDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadMeasurementUnitsAsync();
            SelectedMeasurementUnit = null;
            _notificationService.ShowSuccess(message.DeletedMeasurementUnit.Message);
        }

        #endregion
    }
}
