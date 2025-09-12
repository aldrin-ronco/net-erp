using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Global;
using NetErp.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.EmailGraphQLModel;

namespace NetErp.Global.Email.ViewModels
{
    public class EmailMasterViewModel : Screen,
        IHandle<EmailDeleteMessage>,
        IHandle<EmailUpdateMessage>,
        IHandle<EmailCreateMessage>
    {
        private readonly IRepository<EmailGraphQLModel> _emailService;
        private readonly Helpers.Services.INotificationService _notificationService;
        
        public EmailViewModel Context { get; set; }
        
        public EmailMasterViewModel(
            EmailViewModel context,
            IRepository<EmailGraphQLModel> emailService,
            Helpers.Services.INotificationService notificationService)
        {
            Context = context;
            _emailService = emailService;
            _notificationService = notificationService;
            Context.EventAggregator.SubscribeOnPublishedThread(this);
        }        


        private ObservableCollection<EmailGraphQLModel> _emails;
        public ObservableCollection<EmailGraphQLModel> Emails
        {
            get { return _emails; }
            set
            {
                if (_emails != value)
                {
                    _emails = value;
                    NotifyOfPropertyChange(nameof(Emails));
                }
            }
        }

        private EmailGraphQLModel? _selectedItem = null;
        public EmailGraphQLModel? SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(CanDeleteEmail));
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
                    if(string.IsNullOrEmpty(value) || value.Length >=3) _ = LoadEmailsAsync();
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
        public bool CanDeleteEmail
        {
            get
            {
                if (SelectedItem is null) return false;
                return true;
            }
        }
        private ICommand _deleteEmailCommand;
        public ICommand DeleteEmailCommand
        {
            get
            {
                if (_deleteEmailCommand is null) _deleteEmailCommand = new AsyncCommand(DeleteEmail);
                return _deleteEmailCommand;
            }
        }
        private ICommand _createEmailCommand;
        public ICommand CreateEmailCommand
        {
            get
            {
                if (_createEmailCommand is null) _createEmailCommand = new AsyncCommand(CreateEmailAsync);
                return _createEmailCommand;
            }
        }


        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            await LoadEmailsAsync();
            this.SetFocus(() => FilterSearch);
        }
        public async Task LoadEmailsAsync()
        {
            try
            {
                IsBusy = true;
                string query;
                query = @"
                query($filter: EmailFilterInput!){
                  ListResponse: emails(filter: $filter){
                    id
                    description
                    password
                    email
                    smtp{
                      id
                      name
                      host
                      port
                    }
                   }                  
                }";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();

                variables.filter.and = new ExpandoObject[]
                {
                    new(),
                    new()
                };

                variables.filter.and[0].isCorporate = new ExpandoObject();
                variables.filter.and[0].isCorporate.@operator = "=";
                variables.filter.and[0].isCorporate.value = true;


                variables.filter.and[1].or = new ExpandoObject[]
                {
                    new(),
                    new()
                };

                variables.filter.and[1].or[0].email = new ExpandoObject();
                variables.filter.and[1].or[0].email.@operator = "like";
                variables.filter.and[1].or[0].email.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                variables.filter.and[1].or[1].description = new ExpandoObject();
                variables.filter.and[1].or[1].description.@operator = "like";
                variables.filter.and[1].or[1].description.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                var result = await _emailService.GetListAsync(query, variables);
                Emails = new ObservableCollection<EmailGraphQLModel>(result);       
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task EditEmail() 
        {
            try
            {
                IsBusy = true;
                await Context.ActivateDetailViewForEdit(SelectedItem ?? new());
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }


        }
        public async Task DeleteEmail()
        {
            try
            {
                IsBusy = true;
                string id = SelectedItem!.Id;

                string query = @"query($id:String!){
                  CanDeleteModel: canDeleteEmail(id: $id){
                    canDelete
                    message
                  }
                }";
                var variables = new
                {
                    Id = id
                };
                var validation = await _emailService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {SelectedItem.Description}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }
                this.IsBusy = true;

                Refresh();

                EmailGraphQLModel deletedEmail = await ExecuteDeleteEmailAsync(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new EmailDeleteMessage() { DeleteEmail = deletedEmail });

                NotifyOfPropertyChange(nameof(CanDeleteEmail));
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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteCustomer" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        } 
        public async Task CreateEmailAsync()
        {
            try
            {
                IsBusy = true;
                await Context.ActivateDetailViewForNew();
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod(); 
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{this.GetType().Name}.{(currentMethod is null ? "EditSeller" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task<EmailGraphQLModel> ExecuteDeleteEmailAsync(string id)
        {
            try
            {
                string query = @"mutation($id:String!){
                  DeleteResponse: deleteEmail(id: $id){
                    id
                    description
                    email
                  }
                }";

                object variables = new { Id = id };
                var result = await _emailService.DeleteAsync(query, variables);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }


        public async Task HandleAsync(EmailUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadEmailsAsync();
                _notificationService.ShowSuccess("Correo corporativo actualizado correctamente.");
                return;
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task HandleAsync(EmailCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadEmailsAsync();
                _notificationService.ShowSuccess("Correo corporativo creado correctamente.");
                return;
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task HandleAsync(EmailDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadEmailsAsync();
                _notificationService.ShowSuccess("Correo corporativo eliminado correctamente.");
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
