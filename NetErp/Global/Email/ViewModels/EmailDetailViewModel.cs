using Amazon;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.EmailGraphQLModel;
using static Models.Global.GraphQLResponseTypes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NetErp.Global.Email.ViewModels
{
    public class EmailDetailViewModel: Screen, INotifyDataErrorInfo
    {
        private readonly IRepository<EmailGraphQLModel> _emailService;
        private readonly SmtpCache _smtpCache;
        private readonly IEventAggregator _eventAggregator;
        private readonly Microsoft.VisualStudio.Threading.JoinableTaskFactory _joinableTaskFactory;

        public EmailDetailViewModel(
            EmailViewModel context,
            IRepository<EmailGraphQLModel> emailService,
            IEventAggregator eventAggregator,
            Microsoft.VisualStudio.Threading.JoinableTaskFactory joinableTaskFactory,
            SmtpCache smtpCache)
        {
            Context = context;
            _emailService = emailService;
            _eventAggregator = eventAggregator;
            _joinableTaskFactory = joinableTaskFactory;

            _smtpCache = smtpCache;
            _errors = new Dictionary<string, List<string>>();
        }
        public async Task InitializeAsync()
        {
            await Task.WhenAll(
                _smtpCache.EnsureLoadedAsync()
            );

            EmailSmtp = [.. _smtpCache.Items];
            this.EmailSmtp.Insert(0, new SmtpGraphQLModel() { Id = 0, Name = "SELECCIONE SMTP" });
            this.IsCorporate = true;
            this.IsElectronicInvoiceRecipient = true;


        }

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
        [ExpandoPath("smtpId", SerializeAsId = true)]

        public SmtpGraphQLModel SelectedSmtp
        {
            get { return _selectedSmtp; }
            set
            {
                if (_selectedSmtp != value)
                {
                    _selectedSmtp = value;
                    NotifyOfPropertyChange(nameof(SelectedSmtp));
                    this.TrackChange(nameof(SelectedSmtp));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        public int Id { get; set; }     

        private string _description;
        public string Description
        {
            get { return _description; }
            set
            {
                if (_description != value)
                {
                    _description = value;
                    NotifyOfPropertyChange(nameof(Description));
                    this.TrackChange(nameof(Description));

                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperty(nameof(Description), value);
                }
            }
        }

        private string _email;
        public string Email
        {
            get { return _email; }
            set
            {
                if (_email != value)
                {
                    _email = value;
                    NotifyOfPropertyChange(nameof(Email));
                    this.TrackChange(nameof(Email));
                    ValidateProperty(nameof(Email), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }

            }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            set
            {
                if (_password != value)
                {
                    _password = value;
                    NotifyOfPropertyChange(nameof(Password));
                    this.TrackChange(nameof(Password));

                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperty(nameof(Password), value);
                }
            }
        }
        private bool _isCorporate;
        public bool IsCorporate
        {
            get { return _isCorporate; }
            set
            {
                if (_isCorporate != value)
                {
                    _isCorporate = value;
                    NotifyOfPropertyChange(nameof(IsCorporate));
                    this.TrackChange(nameof(IsCorporate));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        private bool _isElectronicInvoiceRecipient;
        public bool IsElectronicInvoiceRecipient
        {
            get { return _isElectronicInvoiceRecipient; }
            set
            {
                if (_isElectronicInvoiceRecipient != value)
                {
                    _isElectronicInvoiceRecipient = value;
                    NotifyOfPropertyChange(nameof(IsElectronicInvoiceRecipient));
                    this.TrackChange(nameof(IsElectronicInvoiceRecipient));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        
public bool IsNewRecord => Id == 0;
public string PasswordPlaceholder => !IsNewRecord
    ? "Deje vacío para continuar con la antigua contraseña"
    : string.Empty;

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
                if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(Description) || SelectedSmtp.Id == 0) return false;
                return true;

            }
        }


        

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<EmailGraphQLModel> result = await ExecuteSaveAsync();

                if (!result.Success)
                {
                    ThemedMessageBox.Show(
                        text: $"El guardado no ha sido exitoso\r\n\r\n{result.Errors.ToUserMessage()}\r\n\r\nVerifique los datos y vuelva a intentarlo",
                        title: $"{result.Message}!",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new EmailCreateMessage { CreatedEmail = result }
                        : new EmailUpdateMessage { UpdatedEmail = result },
                    CancellationToken.None);

                await TryCloseAsync(true);
            }
            catch (AsyncException ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al realizar operación.\r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al realizar operación.\r\n{GetType().Name}.{nameof(SaveAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }

         


        }
        public async Task<UpsertResponseType<EmailGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                if (IsNewRecord)
                {
                    var (_, query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    return await _emailService.CreateAsync<UpsertResponseType<EmailGraphQLModel>>(query, variables);
                }
                else
                {
                    var (_, query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;
                    return await _emailService.UpdateAsync<UpsertResponseType<EmailGraphQLModel>>(query, variables);
                }
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
      
        public async Task GoBack()
        {
            await Context.ActivateMasterViewAsync();
        }
        public void CleanUpControls()
        {
            try
            {
                Id = 0;
                Description = string.Empty;
                Password = string.Empty;
                Email = string.Empty;
                SelectedSmtp = EmailSmtp.FirstOrDefault(x => x.Id == 0);
                this.IsCorporate = true;
                this.IsElectronicInvoiceRecipient = true;
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
                this.SetFocus(nameof(Description));
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
                    case nameof(Email):
                        if (string.IsNullOrEmpty(Email)) AddError(propertyName, "El campo 'Correo electrónico' no puede estar vacío.");
                        break;
                    case nameof(Password):
                        if (string.IsNullOrEmpty(Password)) AddError(propertyName, "El campo 'Contraseña' no puede estar vacío.");
                        break;
                    case nameof(Description):
                        if (string.IsNullOrEmpty(Description)) AddError(propertyName, "El campo 'Descripción' no puede estar vacío.");
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
            ValidateProperty(nameof(Email), Email);
            ValidateProperty(nameof(Password), Password);
            ValidateProperty(nameof(Description), Description);
        }
        #region SetForNew / SetForEdit

        public void SetForNew()
        {
            Email = string.Empty;
            Description = string.Empty;
            Password =  string.Empty;
            SelectedSmtp = EmailSmtp.FirstOrDefault(x => x.Id == 0);

            this.ClearSeeds();
            this.SeedValue(nameof(Email), Email);
            this.SeedValue(nameof(Description), Description);
            this.SeedValue(nameof(Password), Password);
            this.SeedValue(nameof(SelectedSmtp), SelectedSmtp);
            this.AcceptChanges();
            ValidateProperties();
        }

        public void SetForEdit()
        {
            this.SeedValue(nameof(Email), Email);
            this.SeedValue(nameof(Description), Description);
            this.SeedValue(nameof(Password), Password);
            this.SeedValue(nameof(SelectedSmtp), SelectedSmtp);

            this.AcceptChanges();
            ValidateProperties();
        }

        #endregion

        #region Load for Edit
        public async Task<EmailGraphQLModel> LoadDataForEditAsync(int id)
        {
            try
            {
                string query = GetLoadEmailByIdQuery();

                dynamic variables = new ExpandoObject();


                variables.singleItemResponseId = id;

                var entity = await _emailService.FindByIdAsync(query, variables);

                PopulateFromEmail(entity);

                return entity;
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
        public void PopulateFromEmail(EmailGraphQLModel entity)
        {

            Id = entity.Id;
            Description = entity.Description;
            Email = entity.Email;
            Password = entity.Password;
            SelectedSmtp = entity.Smtp is null ? EmailSmtp.FirstOrDefault(c => c.Id == 0) : EmailSmtp.FirstOrDefault(c => c.Id == entity.Smtp.Id);

            this.SeedValue(nameof(Description), Description);
            this.SeedValue(nameof(Email), Email);
            this.SeedValue(nameof(Password), Password);
            this.SeedValue(nameof(SelectedSmtp), SelectedSmtp);


            this.AcceptChanges();
        }
        public string GetLoadEmailByIdQuery()
        {
            var emailFields = FieldSpec<EmailGraphQLModel>
             .Create()

                 .Field(e => e.Id)

                 .Field(e => e.Description)
                 .Field(e => e.Email)
       
                  .Select(e => e.Smtp, acc => acc
                    .Field(c => c!.Id)
                    .Field(c => c!.Name)
                    )



                 .Build();
            var EmailIdParameter = new GraphQLQueryParameter("id", "ID!");

            var EmailFragment = new GraphQLQueryFragment("Email", [EmailIdParameter], emailFields, "SingleItemResponse");

            var builder = new GraphQLQueryBuilder([EmailFragment]);

            return builder.GetQuery();

        }



        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<EmailGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "email", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    .Field(e => e.Email))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createEmail",
                [new("input", "CreateEmailInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<EmailGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "email", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    .Field(f => f.Email))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("updateEmail",
                [new("data", "UpdateEmailInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadByIdQuery = new(() =>
        {
            var fields = FieldSpec<EmailGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.Description)
                .Field(e => e.Email)
                
                .Select(e => e.Smtp, cat => cat
                    .Field(c => c.Id)
                    .Field(c => c.Name)
                    .Field(c => c.Host)
                    .Field(c => c.Port)
                    )
                
                .Build();

            var fragment = new GraphQLQueryFragment("email",
                [new("id", "ID!")],
                fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion

    }
}
