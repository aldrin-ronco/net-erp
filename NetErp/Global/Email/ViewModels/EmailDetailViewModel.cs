using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Caliburn.Micro;
using DevExpress.Drawing.Internal.Fonts.Interop;
using System.Collections.ObjectModel;
using Services.Global.DAL.PostgreSQL;
using static Models.Global.EmailGraphQLModel;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using Models.Global;
using System.Dynamic;
using Common.Interfaces;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.Threading;
using System.Collections;
using System.ComponentModel;
using Common.Extensions;
using DevExpress.Pdf.Xmp;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Common.Helpers;
using NetErp.Helpers;
using System.Windows.Threading;

namespace NetErp.Global.Email.ViewModels
{
    public class EmailDetailViewModel: Screen, INotifyDataErrorInfo
    {
        public EmailDetailViewModel(EmailViewModel context)
        {
            Context = context;
            var joinable = new JoinableTaskFactory(new JoinableTaskContext());
            joinable.Run(async () => await LoadSmtps());
            _errors = new Dictionary<string, List<string>>();
        }

        public IGenericDataAccess<EmailGraphQLModel> EmailService = IoC.Get<IGenericDataAccess<EmailGraphQLModel>>(); 
        public IGenericDataAccess<SmtpGraphQLModel> SmtpService = IoC.Get<IGenericDataAccess<SmtpGraphQLModel>>();


        public ICommand _goBackCommand;
        public ICommand GoBackCommand
        {
            get
            {
                if (_goBackCommand is null) _goBackCommand = new AsyncCommand(GoBack);
                return _goBackCommand;
            }
        }        

        private ObservableCollection<SmtpGraphQLModel> _emailSmtp;
        public ObservableCollection<SmtpGraphQLModel> EmailSmtp
        {
            get { return _emailSmtp; }
            set
            {
                if (_emailSmtp != value)
                {
                    _emailSmtp = value;
                    NotifyOfPropertyChange(nameof(EmailSmtp));
                }
            }
        }

        private SmtpGraphQLModel _selectedSmtp;
        public SmtpGraphQLModel SelectedSmtp
        {
            get { return _selectedSmtp; }
            set
            {
                if (_selectedSmtp != value)
                {
                    _selectedSmtp = value;
                    NotifyOfPropertyChange(nameof(SelectedSmtp));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        public string EmailId { get; set; }     

        private string _emailDescription;
        public string EmailDescription
        {
            get { return _emailDescription; }
            set
            {
                if (_emailDescription != value)
                {
                    _emailDescription = value;
                    NotifyOfPropertyChange(nameof(EmailDescription));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperty(nameof(EmailDescription), value);
                }
            }
        }

        private string _emailEmail;
        public string EmailEmail
        {
            get { return _emailEmail; }
            set
            {
                if (_emailEmail != value)
                {
                    _emailEmail = value;
                    NotifyOfPropertyChange(nameof(EmailEmail));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperty(nameof(EmailEmail), value);
                }

            }
        }

        private string _emailPassword;
        public string EmailPassword
        {
            get { return _emailPassword; }
            set
            {
                if (_emailPassword != value)
                {
                    _emailPassword = value;
                    NotifyOfPropertyChange(nameof(EmailPassword));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperty(nameof(EmailPassword), value);
                }
            }
        }
        public bool IsNewRecord => EmailId == string.Empty;

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (IsBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }            
        }



        public EmailViewModel Context { get; set; }

        Dictionary<string, List<string>> _errors;


        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }
        public bool CanSave
        {
            get
            {
                if (string.IsNullOrEmpty(EmailEmail) || string.IsNullOrEmpty(EmailPassword) || string.IsNullOrEmpty(EmailDescription) || SelectedSmtp.Id == 0) return false;
                return true;

            }
        }


        public async Task LoadSmtps()
        {
            try
            {
                string query = @"query {                     
                        ListResponse: smtps{
                            id
                            name
                            host
                            port
                        }
                }";

                var result = await SmtpService.GetList(query, new { });
                EmailSmtp = new ObservableCollection<SmtpGraphQLModel>(result);
                EmailSmtp.Insert(0, new() { Id = 0, Name = "<< SELECCIONE UN SMTP >>"});
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener nombres de SMTP", ex);
            }
        }  

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                EmailGraphQLModel result = await ExecuteSaveAsync();
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new EmailCreateMessage() { CreatedEmail = result });
                }
                else
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new EmailUpdateMessage() { UpdatedEmail = result });
                }
                await Context.ActivateMasterView();

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{exGraphQL.Message}.\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            finally
            {
                IsBusy = false;
            }


        }
        public async Task<EmailGraphQLModel> ExecuteSaveAsync()
        {
            try
            {
                dynamic variables = new ExpandoObject();
                variables.data = new ExpandoObject();

                if (!IsNewRecord) variables.id = EmailId;           
                
                variables.data.isCorporate = true;
                variables.data.sendElectronicInvoice = true;
                variables.data.accountingEntityId = 0;
                variables.data.description = EmailDescription; 
                variables.data.password = EmailPassword;
                variables.data.email = EmailEmail;
                variables.data.smtpId = SelectedSmtp!.Id;

                string query = IsNewRecord ? @"
                mutation ($data: CreateEmailInput!) {
                  CreateResponse: createEmail(data: $data) {
                    id
                    email
                    password
                    sendElectronicInvoice
                    isCorporate
                    accountingEntityId
                    smtp {
                      id
                      name
                    }
                  }
                }" :
                @"
                mutation ($data: UpdateEmailInput!, $id: String!) {
                  UpdateResponse: updateEmail(data: $data, id: $id) {
                    id
                    email
                    password
                    sendElectronicInvoice
                    isCorporate
                    accountingEntityId
                    smtp {
                      id
                      name
                    }
                  }
                }";

                var result = IsNewRecord ? await EmailService.Create(query, variables) : await EmailService.Update(query, variables);                
                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show("error:", ex.Message);
                return new();
            }
        }
        public async Task GoBack()
        {
            await Context.ActivateMasterView();
        }
        public void CleanUpControls()
        {
            try
            {
                EmailId = string.Empty;
                EmailDescription = string.Empty;
                EmailPassword = string.Empty;
                EmailEmail = string.Empty;
                SelectedSmtp = EmailSmtp.First(x => x.Id == 0);
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
        } 

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            ValidateProperties();
            _ = Application.Current.Dispatcher.BeginInvoke(() =>
            {
                this.SetFocus(nameof(EmailDescription));
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }


        public bool HasErrors => _errors.Count > 0;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;


        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null;
            return _errors[propertyName];
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ValidateProperty(string propertyName, string value)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty.Trim();
            try
            {
                ClearErrors(propertyName);
                switch (propertyName)
                {
                    case nameof(EmailEmail):
                        if (string.IsNullOrEmpty(EmailEmail)) AddError(propertyName, "El campo 'Correo electrónico' no puede estar vacío.");
                        break;
                    case nameof(EmailPassword):
                        if (string.IsNullOrEmpty(EmailPassword)) AddError(propertyName, "El campo 'Contraseña' no puede estar vacío.");
                        break;
                    case nameof(EmailDescription):
                        if (string.IsNullOrEmpty(EmailDescription)) AddError(propertyName, "El campo 'Descripción' no puede estar vacío.");
                        break;
                    default: 
                        break;
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        } 

        private void ValidateProperties()
        {
            ValidateProperty(nameof(EmailEmail), EmailEmail);
            ValidateProperty(nameof(EmailPassword), EmailPassword);
            ValidateProperty(nameof(EmailDescription), EmailDescription);
        }

    }
}
