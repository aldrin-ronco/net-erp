using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Books;
using Models.Inventory;
using NetErp.Books.AccountingEntities.ViewModels;
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
using static DevExpress.Drawing.Printing.Internal.DXPageSizeInfo;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Inventory.MeasurementUnits.ViewModels
{
    public class MeasurementUnitMasterViewModel : Screen,
        IHandle<MeasurementUnitCreateMessage>,
        IHandle<MeasurementUnitUpdateMessage>,
        IHandle<MeasurementUnitDeleteMessage>
    {
        private readonly IRepository<MeasurementUnitGraphQLModel> _measurementUnitService;
        private readonly Helpers.Services.INotificationService _notificationService;

        private MeasurementUnitViewModel _context;
        public MeasurementUnitViewModel Context
        {
            get { return _context; }
            set
            {
                if (_context != value)
                {
                    _context = value;
                    NotifyOfPropertyChange(nameof(Context));
                }
            }
        }

        /// <summary>
        /// Establece cuando la aplicacion esta ocupada
        /// </summary>
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

        private string _filterSearch = "";
        public string FilterSearch
        {
            get
            {
                if (_filterSearch is null) return string.Empty;
                return _filterSearch;
            }
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(FilterSearch) || FilterSearch.Length >= 3)
                    {
                        IsBusy = true;
                        _ = Task.Run(() => LoadMeasurementUnits());
                        SelectedMeasurementUnit = null;
                        IsBusy = false;
                    };
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

        private MeasurementUnitGraphQLModel? _selectedMeasurementUnit;
        public MeasurementUnitGraphQLModel? SelectedMeasurementUnit
        {
            get { return _selectedMeasurementUnit; }
            set
            {
                if (_selectedMeasurementUnit != value)
                {
                    _selectedMeasurementUnit = value;
                    NotifyOfPropertyChange(nameof(SelectedMeasurementUnit));
                    NotifyOfPropertyChange(nameof(CanDeleteMeasurementUnit));
                }
            }
        }

        private ObservableCollection<MeasurementUnitDTO> _measurementUnits = [];
        public ObservableCollection<MeasurementUnitDTO> MeasurementUnits
        {
            get { return this._measurementUnits; }
            set
            {
                if (this._measurementUnits != value)
                {
                    this._measurementUnits = value;
                    NotifyOfPropertyChange(nameof(MeasurementUnits));
                    NotifyOfPropertyChange(nameof(CanDeleteMeasurementUnit));
                }
            }
        }
        public bool CanDeleteMeasurementUnit
        {
            get
            {
                if (SelectedMeasurementUnit is null) return false;
                return true;
            }
        }

        private ICommand _createMeasurementUnitCommand;

        public ICommand CreateMeasurementUnitCommand
        {
            get
            {
                if (_createMeasurementUnitCommand is null) _createMeasurementUnitCommand = new AsyncCommand(CreateMeasurementUnitAsync, CanCreateMeasurementUnit);
                return _createMeasurementUnitCommand;
            }
        }

        private ICommand _deleteMeasurementUnitCommand;

        public ICommand DeleteMeasurementUnitCommand
        {
            get
            {
                if (_deleteMeasurementUnitCommand is null) _deleteMeasurementUnitCommand = new AsyncCommand(DeleteMeasurementUnitAsync, CanDeleteMeasurementUnit);
                return _deleteMeasurementUnitCommand;
            }
        }

        public bool CanCreateMeasurementUnit() => !IsBusy;


        public MeasurementUnitMasterViewModel(MeasurementUnitViewModel context,
            IRepository<MeasurementUnitGraphQLModel> measurementUnitService,
            Helpers.Services.INotificationService notificationService)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            _measurementUnitService = measurementUnitService ?? throw new ArgumentNullException(nameof(measurementUnitService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            Context.EventAggregator.SubscribeOnUIThread(this);
        }

        protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            //Context.EventAggregator.Unsubscribe(this);
            await base.OnDeactivateAsync(close, cancellationToken);
        }
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = Task.Run(() => LoadMeasurementUnits());
            _ = this.SetFocus(nameof(FilterSearch));
        }

        public async Task LoadMeasurementUnits()
        {
            try
            {

                IsBusy = true;

                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
                //variables.pageResponseFilters.name = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();
              
                string query = GetLoadMeasurementUnitQuery();

                PageType<MeasurementUnitGraphQLModel> result = await _measurementUnitService.GetPageAsync(query, variables);
                this.MeasurementUnits = this.Context.AutoMapper.Map<ObservableCollection<MeasurementUnitDTO>>(result.Entries);
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
        public string GetLoadMeasurementUnitQuery()
        {
            var measurementUnitFields = FieldSpec<PageType<MeasurementUnitGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Abbreviation)
                    .Field(e => e.Name)
                    
                    )
                .Build();

            var measurementUnitParameters = new GraphQLQueryParameter("filters", "MeasurementUnitFilters");

            var measurementUnitFragment = new GraphQLQueryFragment("measurementUnitsPage", [measurementUnitParameters], measurementUnitFields, "PageResponse");

            var builder = new GraphQLQueryBuilder([measurementUnitFragment]);

            return builder.GetQuery();
        }
        public async Task EditMeasurementUnit()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteEditMeasurementUnitAsync());
                SelectedMeasurementUnit = null;
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "EditAccountingEntity" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ExecuteEditMeasurementUnitAsync()
        {
            await Context.ActivateDetailViewForEdit(SelectedMeasurementUnit);
        }

        public async Task CreateMeasurementUnitAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteCreateMeasurementUnitAsync());
                SelectedMeasurementUnit = null;
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "EditCustomer" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ExecuteCreateMeasurementUnitAsync()
        {
            await Context.ActivateDetailViewForNew(); // Mostrar la Vista
        }

        public async Task DeleteMeasurementUnitAsync()
        {
            try
            {

                this.IsBusy = true;
                this.Refresh();

                string query = GetCanDeleteMeasurementUnitQuery();

                object variables = new { canDeleteResponseId = SelectedMeasurementUnit.Id };

                var validation = await _measurementUnitService.CanDeleteAsync(query, variables);

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
                DeleteResponseType deletedMeasurementUnit = await Task.Run(() => this.ExecuteDeleteMeasurementUnit(SelectedMeasurementUnit.Id));

                if (!deletedMeasurementUnit.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedMeasurementUnit.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await Context.EventAggregator.PublishOnUIThreadAsync(new MeasurementUnitDeleteMessage { DeletedMeasurementUnit = deletedMeasurementUnit });

                NotifyOfPropertyChange(nameof(CanDeleteMeasurementUnit));
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
        public string GetCanDeleteMeasurementUnitQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("canDeleteMeasurementUnit", [parameter], fields, alias: "CanDeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }
        public async Task<DeleteResponseType> ExecuteDeleteMeasurementUnit(int id)
        {
            try
            {

                string query = GetDeleteMeasurementUnitQuery();

                object variables = new
                {
                    deleteResponseId = id
                };

                // Eliminar registros
                DeleteResponseType deletedRecord = await _measurementUnitService.DeleteAsync<DeleteResponseType>(query, variables);
                return deletedRecord;
            }
            catch (Exception)
            {
                throw;
            }
           
           
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

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public async Task HandleAsync(MeasurementUnitCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                _notificationService.ShowSuccess(message.CreatedMeasurementUnit.Message);
                await LoadMeasurementUnits();
               
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task HandleAsync(MeasurementUnitUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                _notificationService.ShowSuccess(message.UpdatedMeasurementUnit.Message);
                await LoadMeasurementUnits();
               
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task HandleAsync(MeasurementUnitDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadMeasurementUnits();
                _notificationService.ShowSuccess("Unidad de medida eliminada correctamente");
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
