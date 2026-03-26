using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
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
using static Models.Global.GraphQLResponseTypes;

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

                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponseFilters.matching = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                string query = GetLoadEmailQuery();

                PageType<EmailGraphQLModel> result = await _emailService.GetPageAsync(query, variables);
                this.Emails = [.. result.Entries];
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
        public string GetLoadEmailQuery()
        {
            var EmailFields = FieldSpec<PageType<EmailGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    .Field(e => e.Email)
                   .Select(e => e.Smtp, acc => acc 
                   .Field (x => x.Name)
                   .Field(x => x.Host)
                   .Field(x => x.Port)
                   .Field(x => x.Id)
                   )

                    )
                .Build();

            var EmailParameters = new GraphQLQueryParameter("filters", "EmailFilters");

            var EmailFragment = new GraphQLQueryFragment("emailsPage", [EmailParameters], EmailFields, "PageResponse");

            var builder = new GraphQLQueryBuilder([EmailFragment]);

            return builder.GetQuery();
        }
        public async Task EditEmail() 
        {
            try
            {
                IsBusy = true;
                if(SelectedItem != null)   await Context.ActivateDetailViewForEditAsync(SelectedItem.Id);
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

                this.IsBusy = true;
                this.Refresh();

                string query = GetCanDeleteEmailQuery();

                object variables = new { canDeleteResponseId = SelectedItem.Id };

                var validation = await _emailService.CanDeleteAsync(query, variables);

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
                DeleteResponseType deletedEmail = await Task.Run(() => this.ExecuteDeleteEmailAsync(SelectedItem.Id));

                if (!deletedEmail.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedEmail.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await Context.EventAggregator.PublishOnUIThreadAsync(new EmailDeleteMessage { DeletedEmail = deletedEmail });

                NotifyOfPropertyChange(nameof(CanDeleteEmail));
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
        public async Task CreateEmailAsync()
        {
            try
            {
                IsBusy = true;
                await Context.ActivateDetailViewForNewAsync();
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
        public async Task<DeleteResponseType> ExecuteDeleteEmailAsync(int id)
        {
            try
            {

                string query = GetDeleteEmailQuery();

                object variables = new
                {
                    deleteResponseId = id
                };

                // Eliminar registros
                DeleteResponseType deletedRecord = await _emailService.DeleteAsync<DeleteResponseType>(query, variables);
                return deletedRecord;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public string GetDeleteEmailQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("deleteEmail", [parameter], fields, alias: "DeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public string GetCanDeleteEmailQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("canDeleteEmail", [parameter], fields, alias: "CanDeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }

        public async Task HandleAsync(EmailUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadEmailsAsync();
                _notificationService.ShowSuccess(message.UpdatedEmail.Message);
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
                _notificationService.ShowSuccess(message.CreatedEmail.Message);
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
                _notificationService.ShowSuccess(message.DeletedEmail.Message);
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
