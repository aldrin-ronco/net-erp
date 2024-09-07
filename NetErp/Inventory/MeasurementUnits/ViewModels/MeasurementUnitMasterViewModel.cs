using Caliburn.Micro;
using Common.Extensions;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DTOLibrary.Books;
using GraphQL.Client.Http;
using Models.Books;
using Models.Inventory;
using NetErp.Books.AccountingEntities.ViewModels;
using NetErp.Helpers;
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

namespace NetErp.Inventory.MeasurementUnits.ViewModels
{
    public class MeasurementUnitMasterViewModel : Screen,
        IHandle<MeasurementUnitCreateMessage>,
        IHandle<MeasurementUnitUpdateMessage>,
        IHandle<MeasurementUnitDeleteMessage>
    {
        public readonly IGenericDataAccess<MeasurementUnitGraphQLModel> MeasurementUnitService = IoC.Get<IGenericDataAccess<MeasurementUnitGraphQLModel>>();

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
                if (_createMeasurementUnitCommand is null) _createMeasurementUnitCommand = new AsyncCommand(CreateMeasurementUnit, CanCreateMeasurementUnit);
                return _createMeasurementUnitCommand;
            }
        }

        private ICommand _deleteMeasurementUnitCommand;

        public ICommand DeleteMeasurementUnitCommand
        {
            get
            {
                if (_deleteMeasurementUnitCommand is null) _deleteMeasurementUnitCommand = new AsyncCommand(DeleteMeasurementUnit, CanDeleteMeasurementUnit);
                return _deleteMeasurementUnitCommand;
            }
        }

        public bool CanCreateMeasurementUnit() => !IsBusy;


        public MeasurementUnitMasterViewModel(MeasurementUnitViewModel context)
        {
            try
            {
                Context = context;
                Context.EventAggregator.SubscribeOnUIThread(this);
            }
            catch (Exception)
            {

                throw;
            }

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
                Refresh();
                string query = @"
                query($filter: MeasurementUnitFilterInput){
                    ListResponse: measurementUnits(filter: $filter){
                    id
                    abbreviation
                    name
                    }
                }";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.Name = FilterSearch == "" ? "" : FilterSearch;
                // Iniciar cronometro
                Stopwatch stopwatch = new();
                stopwatch.Start();

                var source = await MeasurementUnitService.GetList(query, variables);
                MeasurementUnits = Context.AutoMapper.Map<ObservableCollection<MeasurementUnitDTO>>(source);
                stopwatch.Stop();

                // Detener cronometro
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadAccountingEntities" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditMeasurementUnit()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteEditMeasurementUnit());
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

        public async Task ExecuteEditMeasurementUnit()
        {
            await Context.ActivateDetailViewForEdit(SelectedMeasurementUnit);
        }

        public async Task CreateMeasurementUnit()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteCreateMeasurementUnit());
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

        public async Task ExecuteCreateMeasurementUnit()
        {
            await Context.ActivateDetailViewForNew(); // Mostrar la Vista
        }

        public async Task DeleteMeasurementUnit()
        {
            try
            {
                IsBusy = true;
                int id = SelectedMeasurementUnit.Id;

                string query = @"
                query($id:ID) {
                  CanDeleteModel: canDeleteMeasurementUnit(id:$id) {
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await this.MeasurementUnitService.CanDelete(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Atención!", text: $"¿Confirma que desea eliminar el registro {SelectedMeasurementUnit.Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "La unidad de medida no puede ser eliminada" +
                        (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }


                Refresh();

                var deletedMeasurementUnit = await ExecuteDeleteMeasurementUnit(id);

                await Context.EventAggregator.PublishOnCurrentThreadAsync(new MeasurementUnitDeleteMessage() { DeletedMeasurementUnit = deletedMeasurementUnit });

                // Desactivar opcion de eliminar registros
                NotifyOfPropertyChange(nameof(CanDeleteMeasurementUnit));


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
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteAccountingEntity" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task<MeasurementUnitGraphQLModel> ExecuteDeleteMeasurementUnit(int id)
        {
            try
            {
                string query = @"
                mutation ($id: ID) {
                  DeleteResponse: deleteMeasurementUnit(id: $id) {
                    id
                  }
                }";
                object variables = new { Id = id };
                var deletedMeasurementUnit = await this.MeasurementUnitService.Delete(query, variables);
                this.SelectedMeasurementUnit = null;
                return deletedMeasurementUnit;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Task HandleAsync(MeasurementUnitCreateMessage message, CancellationToken cancellationToken)
        {
            return Task.FromResult(MeasurementUnits = new ObservableCollection<MeasurementUnitDTO>(Context.AutoMapper.Map<ObservableCollection<MeasurementUnitDTO>>(message.MeasurementUnits)));
        }

        public Task HandleAsync(MeasurementUnitUpdateMessage message, CancellationToken cancellationToken)
        {
            return Task.FromResult(MeasurementUnits = new ObservableCollection<MeasurementUnitDTO>(Context.AutoMapper.Map<ObservableCollection<MeasurementUnitDTO>>(message.MeasurementUnits)));
        }

        public Task HandleAsync(MeasurementUnitDeleteMessage message, CancellationToken cancellationToken)
        {
            _ = this.SetFocus(nameof(FilterSearch));
            return LoadMeasurementUnits();
        }
    }
}
