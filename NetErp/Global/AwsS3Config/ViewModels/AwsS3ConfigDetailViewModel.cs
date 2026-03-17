using Amazon;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Dictionaries;
using Extensions.Global;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using static Models.Global.GraphQLResponseTypes;


namespace NetErp.Global.AwsS3Config.ViewModels
{
    public class AwsS3ConfigDetailViewModel : Screen, INotifyDataErrorInfo
    {
        public AwsS3ConfigViewModel Context { get; set; }
        private readonly IRepository<AwsS3ConfigGraphQLModel> _awsS3ConfigService;

        public AwsS3ConfigDetailViewModel(AwsS3ConfigViewModel context, IRepository<AwsS3ConfigGraphQLModel> awsS3ConfigService) {
            _errors = new Dictionary<string, List<string>>();
            Context = context;
            _awsS3ConfigService = awsS3ConfigService;


        }
        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            ValidateProperties();
            this.AcceptChanges();

        }

        #region Properties

        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                NotifyOfPropertyChange(nameof(Id));
                NotifyOfPropertyChange(nameof(IsNewRecord));
            }
        }
        private string _description;

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                NotifyOfPropertyChange(nameof(Description));
                this.TrackChange(nameof(Description));
                ValidateProperty(nameof(Description), value);

                NotifyOfPropertyChange(nameof(CanSave));


            }
        }
        private string _region;

        public string Region
        {
            get => _region;
            set
            {
                _region = value;
                NotifyOfPropertyChange(nameof(Region));
                this.TrackChange(nameof(Region));
                ValidateProperty(nameof(Region), value);
               
                NotifyOfPropertyChange(nameof(CanSave));


            }
        }
        private string _accessKey;

        public string AccessKey
        {
            get => _accessKey;
            set
            {
                _accessKey = value;
                NotifyOfPropertyChange(nameof(AccessKey));
                this.TrackChange(nameof(AccessKey));
                ValidateProperty(nameof(AccessKey), value);

                NotifyOfPropertyChange(nameof(CanSave));


            }
        }
        private string _secretKey;
        public string SecretKey
        {
            get => _secretKey;
            set
            {
                _secretKey = value;
                NotifyOfPropertyChange(nameof(SecretKey));
                this.TrackChange(nameof(SecretKey));
                ValidateProperty(nameof(SecretKey), value);

                NotifyOfPropertyChange(nameof(CanSave));


            }
        }
        #endregion
        #region PropertiesAndCommands
       // public static Dictionary<string, RegionEndpoint> AwsSesRegionDictionary = GlobalDictionaries.AwsSesRegionDictionary;
        public Dictionary<string, RegionEndpoint> AwsSesRegionDictionary { get { return GlobalDictionaries.AwsSesRegionDictionary; } }
        public bool IsNewRecord => Id == 0;

        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync, CanSave);
                return _saveCommand;
            }
        }
        private ICommand _goBackCommand;
        public ICommand GoBackCommand
        {
            get
            {
                if (_goBackCommand is null) _goBackCommand = new RelayCommand(CanGoBack, GoBack);
                return _goBackCommand;
            }
        }
        public void GoBack(object p)
        {
            CleanUpControls();
            _ = Task.Run(() => Context.ActivateMasterViewAsync());

        }
        public bool CanGoBack(object p)
        {
            return !IsBusy;
        }

        public bool CanSave
        {
            get
            {
                if (_errors.Count > 0 || !this.HasChanges()) { return false; }
                return true;
            }
        }

        #endregion
        #region validaciones

        public void CleanUpControls()
        {
            Description = "";

        }
        private bool _isBusy = false;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }
       
        Dictionary<string, List<string>> _errors;
        public bool HasErrors => _errors.Count > 0;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return new List<object>();
            return _errors[propertyName];
        }
        private void ValidateProperty(string propertyName, string value)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty.Trim();
            try
            {
                ClearErrors(propertyName);
                switch (propertyName)
                {
                    case nameof(Description):
                        if (string.IsNullOrEmpty(value)) AddError(propertyName, "La descripción  no puede estar vacía");
                        break;
                    case nameof(AccessKey):
                        if (string.IsNullOrEmpty(value)) AddError(propertyName, "La clave de acceso  no puede estar vacía");
                        break;
                    case nameof(SecretKey):
                        if (string.IsNullOrEmpty(value)) AddError(propertyName, "La clave secreta  no puede estar vacía");
                        break;
                    case nameof(Region):
                        if (string.IsNullOrEmpty(value)) AddError(propertyName, "Debe seleccionar una Región");
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


            ValidateProperty(nameof(Description), Description);
            ValidateProperty(nameof(Region), Region);
            ValidateProperty(nameof(SecretKey), SecretKey);
            ValidateProperty(nameof(AccessKey), AccessKey);
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

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
        #endregion

        #region ApiMethods
        public async Task<AwsS3ConfigGraphQLModel> LoadDataForEditAsync(int id)
        {
            try
            {
                string query = GetLoadAwsS3ConfigByIdQuery();

                dynamic variables = new ExpandoObject();


                variables.singleItemResponseId = id;

                var entity = await _awsS3ConfigService.FindByIdAsync(query, variables);

                // Poblar el ViewModel con los datos del entity (sin bloquear UI thread)
                PopulateFromAwsS3Config(entity);

                return entity;
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
        public void PopulateFromAwsS3Config(AwsS3ConfigGraphQLModel entity)
        {

          
            Id = entity.Id;
            Description = entity.Description;
            SecretKey = entity.SecretKey;
            AccessKey = entity.AccessKey;
            Region = entity.Region;
            this.SeedValue(nameof(Description), Description);
            this.SeedValue(nameof(SecretKey), SecretKey);
            this.SeedValue(nameof(AccessKey), AccessKey);
            this.SeedValue(nameof(Region), Region);

            this.AcceptChanges();



        }
        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                UpsertResponseType<AwsS3ConfigGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }
                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new AwsS3ConfigCreateMessage() { CreatedAwsS3Config = result }
                        : new AwsS3ConfigUpdateMessage() { UpdatedAwsS3Config = result }
                );

                // Context.EnableOnViewReady = false;
                await Context.ActivateMasterViewAsync();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{graphQLError.Errors[0].Extensions.Message} {graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "Save" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }


        }

        public async Task<UpsertResponseType<AwsS3ConfigGraphQLModel>> ExecuteSaveAsync()
        {

            dynamic variables = new ExpandoObject();


            if (IsNewRecord)
            {

                variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                string query = GetCreateQuery();
                UpsertResponseType<AwsS3ConfigGraphQLModel> groupCreated = await _awsS3ConfigService.CreateAsync<UpsertResponseType<AwsS3ConfigGraphQLModel>>(query, variables);
                return groupCreated;
            }
            else
            {

                string query = GetUpdateQuery();
                variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = Id;
                UpsertResponseType<AwsS3ConfigGraphQLModel> updatedGroup = await _awsS3ConfigService.UpdateAsync<UpsertResponseType<AwsS3ConfigGraphQLModel>>(query, variables);
                return updatedGroup;
            }

        }
        public string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<AwsS3ConfigGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "AwsS3Config", nested: sq => sq
                   .Field(e => e.Id)
                  .Field(e => e.Description)

                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateAwsS3ConfigInput!");

            var fragment = new GraphQLQueryFragment("createAwsS3Config", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<AwsS3ConfigGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "AwsS3Config", nested: sq => sq
                    .Field(e => e.Id)
                   .Field(e => e.Description)

                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();


            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateAwsS3ConfigInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateAwsS3Config", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public string GetLoadAwsS3ConfigByIdQuery()
        {
            var AwsS3ConfigFields = FieldSpec<AwsS3ConfigGraphQLModel>
             .Create()

                 .Field(e => e.Id)

                 .Field(e => e.Description)
                 .Field(e => e.SecretKey)
                 .Field(e => e.AccessKey)
                 .Field(e => e.Region)



                 .Build();
            var AwsS3ConfigIdParameter = new GraphQLQueryParameter("id", "ID!");

            var AwsS3ConfigFragment = new GraphQLQueryFragment("AwsS3Config", [AwsS3ConfigIdParameter], AwsS3ConfigFields, "SingleItemResponse");

            var builder = new GraphQLQueryBuilder([AwsS3ConfigFragment]);

            return builder.GetQuery();

        }
        #endregion
    

    }
}
