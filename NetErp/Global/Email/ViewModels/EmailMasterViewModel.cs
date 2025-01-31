using Amazon.S3.Model;
using Caliburn.Micro;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;
using Models.Global;
using NetErp.Global.Smtp.ViewModels;
using NetErp.Helpers;
using Services.Global.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using static DevExpress.Drawing.Printing.Internal.DXPageSizeInfo;
using static Models.Global.EmailGraphQLModel;


namespace NetErp.Global.Email.ViewModels
{
    public class EmailMasterViewModel : Screen,
        IHandle<EmailDeleteMessage>
    {
        public IGenericDataAccess<EmailGraphQLModel> EmailService { get; set; } = IoC.Get<IGenericDataAccess<EmailGraphQLModel>>();
        public EmailViewModel Context { get; set; }

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

        public EmailMasterViewModel(EmailViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnPublishedThread(this);

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
                    if(string.IsNullOrEmpty(value) || value.Length >=3) _= Task.Run(() => LoadEmailsAsync());
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

        public async Task DeleteEmail()
        {
            try
            {
                string id = SelectedItem.Id;

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
                var validation = await this.EmailService.CanDelete(query, variables);

                if (validation.CanDelete)
                {
                    //IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {SelectedItem.Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    //IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }
                //this.IsBusy = true;

                Refresh();

                EmailGraphQLModel deletedEmail = await ExecuteDeleteEmailAsync(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new EmailDeleteMessage() { DeleteEmail = deletedEmail });

                NotifyOfPropertyChange(nameof(CanDeleteEmail));
            }
            catch
            {
                throw;
            }
        }

        public async Task<EmailGraphQLModel>ExecuteDeleteEmailAsync(string id)
        {
            try
            {
                string query = @"mutation($id:String!){
                  DeleteResponse: deleteEmail(id: $id){
                    id
                    name
                    email
                  }
                }";

                object variables = new { Id = id };
                var result = await EmailService.Delete(query, variables);
                return result;
            }
            catch(Exception)
            {
                throw;
            }
        }

        public async Task LoadEmailsAsync()
        {
            try
            {                
                string query;
                query = @"
                query($filter: EmailFilterInput!){
                  ListResponse: emails(filter: $filter){
                    id
                    name
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

                object variables = new
                {
                    filter = new
                    {
                        email = FilterSearch,
                        isCorporate = true
                    }
                };

                var result = await EmailService.GetList(query, variables);
                Emails = new ObservableCollection<EmailGraphQLModel>(result);
            }
            catch (Exception)
            {

                throw;
            }
        }
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = Task.Run(() => LoadEmailsAsync());
            this.SetFocus(() => FilterSearch);
        }

        public Task HandleAsync(SmtpCreateMessage message, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task HandleAsync(EmailDeleteMessage message, CancellationToken cancellationToken)
        {
            EmailGraphQLModel emailToDelete = Emails.FirstOrDefault(x => x.Id == message.DeleteEmail.Id) ?? new EmailGraphQLModel();
            Emails.Remove(emailToDelete);
            SelectedItem = null;
            return Task.CompletedTask;
        }

        public async Task EditEmail()
        {
            await Context.ActivateDetailView(SelectedItem ?? new ());
        }

    }
}
