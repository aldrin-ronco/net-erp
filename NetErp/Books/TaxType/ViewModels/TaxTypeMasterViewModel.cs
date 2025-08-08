using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Data.Native;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using Ninject.Activation;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetErp.Books.TaxType.ViewModels
{
   public class TaxTypeMasterViewModel : Screen,
         IHandle<TaxTypeDeleteMessage>,
        IHandle<TaxTypeUpdateMessage>,
        IHandle<TaxTypeCreateMessage>
    {
        public TaxTypeViewModel Context { get; set; }

        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<TaxTypeGraphQLModel> _taxTypeService;

        public TaxTypeMasterViewModel(TaxTypeViewModel context, Helpers.Services.INotificationService notificationService, IRepository<TaxTypeGraphQLModel> taxTypeService)
        {
            Context = context;
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _taxTypeService = taxTypeService ?? throw new ArgumentNullException(nameof(taxTypeService));

            Context.EventAggregator.SubscribeOnUIThread(this);
            _ =InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            await this.LoadTaxTypesAsync();
           
        }
        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);

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
        // Filtro de busqueda
        private string _filterSearch = "";
        public string FilterSearch
        {
            get { return _filterSearch; }
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value == null ? "" : value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 2)
                    {
                        _ = this.LoadTaxTypesAsync();
                    }
                    ;
                }
            }
        }
        private ObservableCollection<TaxTypeGraphQLModel> _taxTypes;

        public ObservableCollection<TaxTypeGraphQLModel> TaxTypes
        {
            get { return _taxTypes; }
            set
            {
                if (_taxTypes != value)
                {
                    _taxTypes = value;
                    NotifyOfPropertyChange(nameof(TaxTypes));
                }
            }
        }
        private TaxTypeGraphQLModel? _selectedTaxTypeGraphQLModel;
        public TaxTypeGraphQLModel? SelectedTaxTypeGraphQLModel
        {
            get { return _selectedTaxTypeGraphQLModel; }
            set
            {
                if (_selectedTaxTypeGraphQLModel != value)
                {
                    _selectedTaxTypeGraphQLModel = value;
                    NotifyOfPropertyChange(nameof(CanDeleteTaxType));
                    NotifyOfPropertyChange(nameof(SelectedTaxTypeGraphQLModel));
                }
            }
        }

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
        private ICommand _deleteTaxTypeCommand;
        public ICommand DeleteTaxTypeCommand
        {
            get
            {
                if (_deleteTaxTypeCommand is null) _deleteTaxTypeCommand = new AsyncCommand(DeleteTaxTypeAsync, CanDeleteTaxType);
                return _deleteTaxTypeCommand;
            }
        }
      
        #endregion
        public bool CanDeleteTaxType { 
            get
            {
                if (SelectedTaxTypeGraphQLModel is null) return false;
                return true;
            }
        }
        public async Task NewAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                SelectedTaxTypeGraphQLModel = null;
                await  ExecuteActivateDetailViewForEditAsync();
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "NewtTaxTypeEntity" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
       



        public async Task DeleteTaxTypeAsync()
        {
            try
            {
                IsBusy = true;
                int? id = SelectedTaxTypeGraphQLModel?.Id;

                string query = @"
                query($id:Int!) {
                  CanDeleteModel: canDeleteTaxType(id:$id) {
                    canDelete
                    message
                  }
                }";
                object variables = new { Id = id };

                var validation = await this._taxTypeService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Atención!", text: $"¿Confirma que desea eliminar el registro {SelectedTaxTypeGraphQLModel.Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                        (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }


                Refresh();
                var deletedTaxType = await ExecuteDeleteTaxTypeAsync(id.Value);

                await Context.EventAggregator.PublishOnCurrentThreadAsync(new TaxTypeDeleteMessage() { DeletedTaxType = deletedTaxType });
                
                // Desactivar opcion de eliminar registros
                NotifyOfPropertyChange(nameof(CanDeleteTaxType));
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
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteTaxType" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<TaxTypeGraphQLModel> ExecuteDeleteTaxTypeAsync(int id)
        {
            try
            {
                string query = @"
                mutation ($id: Int!) {
                  DeleteResponse: deleteTaxType(id: $id) {
                    id
                  }
                }";
                object variables = new { Id = id };
                var deletedTaxType = await this._taxTypeService.DeleteAsync(query, variables);
                this.SelectedTaxTypeGraphQLModel = null;
                return deletedTaxType;
            }

            catch (Exception ex)
            {
                throw;
            }

        }
        public async Task LoadTaxTypesAsync()
        {
            try
            {
                IsBusy = true;
                string query = @"
			    query($filter : TaxTypeFilterInput!){
     
                   ListResponse : taxTypes(filter : $filter){
        
                       id
                       name
                      generatedTaxAccountIsRequired
                      generatedTaxRefundAccountIsRequired
                      deductibleTaxAccountIsRequired
                      deductibleTaxRefundAccountIsRequired
                      prefix
      
      
                    }
       
                }
                ";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();

                variables.filter.or = new ExpandoObject[]
                {
                    new(),
                    new()
                };

                variables.filter.or[0].name = new ExpandoObject();
                variables.filter.or[0].name.@operator = "like";
                variables.filter.or[0].name.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                variables.filter.or[1].prefix = new ExpandoObject();
                variables.filter.or[1].prefix.@operator = "like";
                variables.filter.or[1].prefix.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                var result = await _taxTypeService.GetListAsync(query, variables);
                ObservableCollection<TaxTypeGraphQLModel> source = new(result);
                this.TaxTypes = this.Context.AutoMapper.Map<ObservableCollection<TaxTypeGraphQLModel>>(source);
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
        public async Task EditTaxTypeAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await  ExecuteActivateDetailViewForEditAsync();

                SelectedTaxTypeGraphQLModel = null;
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "EditTaxType" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task ExecuteActivateDetailViewForEditAsync()
        {
            await Context.ActivateDetailViewForEdit(SelectedTaxTypeGraphQLModel);
        }

        public async Task HandleAsync(TaxTypeDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadTaxTypesAsync();
                _notificationService.ShowSuccess("El tipo de impuesto fue eliminado correctamente");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task HandleAsync(TaxTypeUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadTaxTypesAsync();
            _notificationService.ShowSuccess("El tipo de impuesto fue actualizado correctamente");
        }

        public async Task HandleAsync(TaxTypeCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadTaxTypesAsync();
                _notificationService.ShowSuccess("El tipo de impuesto fue creado correctamente");
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
