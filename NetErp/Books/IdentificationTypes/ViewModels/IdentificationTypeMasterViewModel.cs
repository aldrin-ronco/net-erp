using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Books;
using NetErp;
using Extensions.Books;
using NetErp.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DevExpress.Mvvm;
using System.Windows.Input;
using System.Threading;
using Microsoft.Xaml.Behaviors.Core;
using System.Dynamic;

namespace NetErp.Books.IdentificationTypes.ViewModels
{
    public class IdentificationTypeMasterViewModel : Screen,
        IHandle<IdentificationTypeCreateMessage>,
        IHandle<IdentificationTypeUpdateMessage>,
        IHandle<IdentificationTypeDeleteMessage>
    {

        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<IdentificationTypeGraphQLModel> _identificationTypeService;
        // Context
        private IdentificationTypeViewModel _context;
        public IdentificationTypeViewModel Context
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

        #region Propiedades

        // Filtro de busqueda
        private string _filterSearch = "";
        public string FilterSearch
        {
            get { return _filterSearch; }
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 2)
                    {
                        _ = Task.Run(this.LoadIdentificationTypesAsync);
                    };
                }
            }
        }

        private bool _isBusy = false;
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

        private ICommand _createIdentificationTypeCommand;
        public ICommand CreateIdentificationTypeCommand
        {
            get
            {
                if (_createIdentificationTypeCommand is null) _createIdentificationTypeCommand = new AsyncCommand(CreateIdentificationTypeAsync, CanCreateIdentificationType);
                return _createIdentificationTypeCommand;
            }

        }


        public bool CanCreateIdentificationType() => !IsBusy;

        private ICommand _deleteIdentificationTypeCommand;
        public ICommand DeleteIdentificationTypeCommand
        {
            get
            {
                if (_deleteIdentificationTypeCommand is null) _deleteIdentificationTypeCommand = new AsyncCommand(DeleteIdentificationTypeAsync, CanDeleteIdentificationType);
                return _deleteIdentificationTypeCommand;
            }
        }

        public ICommand GotFocusCommand => new ActionCommand(ExecuteGotFocus);

        private void ExecuteGotFocus()
        {
            SelectedIdentificationType = null;
        }


        #endregion

        #region Colecciones

        private IdentificationTypeDTO _selectedIdentificationType;
        public IdentificationTypeDTO SelectedIdentificationType
        {
            get { return _selectedIdentificationType; }
            set
            {
                if (_selectedIdentificationType != value)
                {
                    _selectedIdentificationType = value;
                    NotifyOfPropertyChange(nameof(SelectedIdentificationType));
                    NotifyOfPropertyChange(nameof(CanDeleteIdentificationType));
                }
            }
        }

        private ObservableCollection<IdentificationTypeDTO> _identificationTypes;
        public ObservableCollection<IdentificationTypeDTO> IdentificationTypes
        {
            get { return this._identificationTypes; }
            set
            {
                if (this._identificationTypes != value)
                {
                    this._identificationTypes = value;
                    NotifyOfPropertyChange(nameof(IdentificationTypes));
                    NotifyOfPropertyChange(nameof(CanDeleteIdentificationType));
                }
            }
        }

        // Listado filtrado para permitir busqueda
        private ObservableCollection<IdentificationTypeDTO> _identificationTypesFiltered;
        public ObservableCollection<IdentificationTypeDTO> IdentificationTypesFiltered
        {
            get { return _identificationTypesFiltered; }
            set
            {
                if (_identificationTypesFiltered != value)
                {
                    _identificationTypesFiltered = value;
                    NotifyOfPropertyChange(nameof(IdentificationTypesFiltered));
                }
            }
        }

        #endregion

        public IdentificationTypeMasterViewModel(IdentificationTypeViewModel context, IRepository<IdentificationTypeGraphQLModel> identificationTypeService, Helpers.Services.INotificationService notificationService)
        {
            this.Context = context;
            this._identificationTypeService = identificationTypeService;
            this._notificationService = notificationService;
            Context.EventAggregator.SubscribeOnUIThread(this);
            _ = Task.Run(() => this.LoadIdentificationTypesAsync());
        }

        #region Metodos 

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.SetFocus(() => FilterSearch);
        }

        public async Task LoadIdentificationTypesAsync()
        {
            try
            {
                IsBusy = true;
                string query = @"
			    query($filter: IdentificationTypeFilterInput){
			        ListResponse: identificationTypes(filter: $filter){
			        id
			        code
			        name
			        hasVerificationDigit
			        minimumDocumentLength
			        }
			    }";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();

                variables.filter.or = new ExpandoObject[]
                {
                    new(),
                    new()
                };

                variables.filter.or[0].code = new ExpandoObject();
                variables.filter.or[0].code.@operator = "like";
                variables.filter.or[0].code.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                variables.filter.or[1].name = new ExpandoObject();
                variables.filter.or[1].name.@operator = "like";
                variables.filter.or[1].name.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                var result = await _identificationTypeService.GetListAsync(query, variables);
                ObservableCollection<IdentificationTypeGraphQLModel> source = new(result);
                this.IdentificationTypes = this.Context.AutoMapper.Map<ObservableCollection<IdentificationTypeDTO>>(source);
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

        public void OnChecked()
        {
            NotifyOfPropertyChange(nameof(CanDeleteIdentificationType));
        }

        public void OnUnchecked()
        {
            NotifyOfPropertyChange(nameof(CanDeleteIdentificationType));
        }

        public async Task EditIdentificationTypeAsync()
        {
            try
            {
                this.IsBusy = true;
                this.Refresh();
                await Task.Run(() => this.ExecuteEditIdentificationTypeAsync());
                SelectedIdentificationType = null;
                this.IsBusy = false;
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        public async Task ExecuteEditIdentificationTypeAsync()
        {
            try
            {
                await this.Context.ActivateDetailViewForEditAsync(this.SelectedIdentificationType);
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        public async Task CreateIdentificationTypeAsync()
        {
            await this.Context.ActivateDetailViewForNewAsync(); // Mostrar la Vista
        }

        public async Task DeleteIdentificationTypeAsync()
        {
            try
            {

                this.IsBusy = true;
                this.Refresh();

                string query = @"
                query($id:ID!) {
                  CanDeleteModel: canDeleteIdentificationType(id:$id) {
                    canDelete
                    message
                  }
                }";

                object variables = new { SelectedIdentificationType.Id };

                var validation = await _identificationTypeService.CanDeleteAsync(query, variables);

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
                var deletedIdentificationType = await Task.Run(() => this.ExecuteDeleteIdentificationTypeAsync(SelectedIdentificationType.Id));

                await Context.EventAggregator.PublishOnUIThreadAsync(new IdentificationTypeDeleteMessage { DeletedIdentificationType = deletedIdentificationType });

                NotifyOfPropertyChange(nameof(CanDeleteIdentificationType));
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

        public async Task<IdentificationTypeGraphQLModel> ExecuteDeleteIdentificationTypeAsync(int id)
        {
            try
            {

                string query = @"
                mutation($id:ID){
                  DeleteResponse: deleteIdentificationType(id:$id) {
                    id
                    code
                    name
                    hasVerificationDigit
                    minimumDocumentLength
                  }
                }";

                object variables = new
                {
                    id
                };

                // Eliminar registros
                var deletedRecord = await _identificationTypeService.DeleteAsync(query, variables);
                return deletedRecord;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task HandleAsync(IdentificationTypeCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadIdentificationTypesAsync();
                _notificationService.ShowSuccess("Tipo de identificación creado exitosamente");
                return;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task HandleAsync(IdentificationTypeUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadIdentificationTypesAsync();
                _notificationService.ShowSuccess("Tipo de identificación actualizado exitosamente");
                return;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task HandleAsync(IdentificationTypeDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadIdentificationTypesAsync();
                _notificationService.ShowSuccess("Tipo de identificación eliminado exitosamente");
                return;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool CanDeleteIdentificationType
        {
            get
            {
                if (SelectedIdentificationType is null) return false;
                return true;
            }
        }

        #endregion

    }
}
