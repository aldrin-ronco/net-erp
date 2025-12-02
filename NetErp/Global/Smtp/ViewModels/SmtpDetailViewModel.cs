using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using static Models.Global.GraphQLResponseTypes;
using Extensions.Global;

namespace NetErp.Global.Smtp.ViewModels
{
    public class SmtpDetailViewModel : Screen, INotifyDataErrorInfo
    {
        private readonly IRepository<SmtpGraphQLModel> _smtpService;
        
        public SmtpViewModel Context { get; set; }
        public bool IsNewRecord => SmtpId == 0;

        Dictionary<string, List<string>> _errors;
        
        public SmtpDetailViewModel(
            SmtpViewModel context,
            IRepository<SmtpGraphQLModel> smtpService)
        {
            Context = context;
            _smtpService = smtpService;
            _errors = new Dictionary<string, List<string>>();
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.SetFocus(() => Name);
            ValidateProperties();
            this.AcceptChanges();
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


        private int _smtpId;

        public int SmtpId
        {
            get { return _smtpId; }
            set 
            {
                if (_smtpId != value) 
                {
                    _smtpId = value;
                    NotifyOfPropertyChange(nameof(SmtpId));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }


        private string _name = string.Empty;

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    NotifyOfPropertyChange(nameof(Name));
                    ValidateProperty(nameof(Name), value);
                    this.TrackChange(nameof(Name));
                    NotifyOfPropertyChange(nameof(CanSave));

                }
            }
        }

        private string _host = string.Empty;

        public string Host
        {
            get { return _host; }
            set
            {
                if (_host != value)
                {
                    _host = value;
                    NotifyOfPropertyChange(nameof(Host));
                    ValidateProperty(nameof(Host), value);
                    this.TrackChange(nameof(Host));
                    NotifyOfPropertyChange(nameof(CanSave));


                }
            }
        }

        private int _port;

        public int Port
        {
            get { return _port; }
            set
            {
                if (_port != value)
                {
                    _port = value;
                    NotifyOfPropertyChange(nameof(Port));
                    this.TrackChange(nameof(Port));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }


        private ICommand? _goBackCommand;
        public ICommand? GoBackCommand
        {
            get
            {
                if (_goBackCommand is null) _goBackCommand = new AsyncCommand(GoBackAsync);
                return _goBackCommand;
            }
        }

        public async Task GoBackAsync()
        {
            await Context.ActivateMasterView();
        }

        public bool CanSave
        {
            get
            {
                if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Host) || Port == 0 || (!this.HasChanges())) return false;
                return true;
            }
        }

        private ICommand? _saveCommand;

        public ICommand? SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

       public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                UpsertResponseType<SmtpGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }
                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new SmtpCreateMessage() { CreatedSmtp = result }
                        : new SmtpUpdateMessage() { UpdatedSmtp = result }
                );
                await Context.ActivateMasterView();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"\r\n{graphQLError.Errors[0].Message}\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
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

        public string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<SmtpGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "Smtp", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.Host)
                    .Field(f => f.Port)
                   )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateSmtpInput!");

            var fragment = new GraphQLQueryFragment("createSmtp", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<SmtpGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "smtp", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.Host)
                    .Field(f => f.Port)
                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateSmtpInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateSmtp", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public async Task<UpsertResponseType<SmtpGraphQLModel>> ExecuteSaveAsync()
        {

            try
            {
                if (IsNewRecord)
                {
                    string query = GetCreateQuery();
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");

                    UpsertResponseType<SmtpGraphQLModel> smtpCreated = await _smtpService.CreateAsync<UpsertResponseType<SmtpGraphQLModel>>(query, variables);
                    return smtpCreated;
                }
                else
                {
                    string query = GetUpdateQuery();
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = SmtpId;

                    UpsertResponseType<SmtpGraphQLModel> smtpUpdate = await _smtpService.UpdateAsync<UpsertResponseType<SmtpGraphQLModel>>(query, variables);
                    return smtpUpdate;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public void CleanUpControls()
        {
            SmtpId = 0;
            Name = string.Empty;
            Host = string.Empty;
            Port = 0;
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
                    case nameof(Name):
                        if (string.IsNullOrEmpty(Name)) AddError(propertyName, "El nombre no puede estar vacío");
                        break;
                    case nameof(Host):
                        if (string.IsNullOrEmpty(Host)) AddError(propertyName, "El host no puede estar vacío");
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
            ValidateProperty(nameof(Name), Name);
            ValidateProperty(nameof(Host), Host);
        }
    }
}
