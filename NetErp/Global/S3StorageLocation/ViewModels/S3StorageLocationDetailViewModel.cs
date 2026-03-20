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
using Models.Global;
using NetErp.Global.S3StorageLocation.ViewModels;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using Ninject.Activation;
using Services.Global.DAL.PostgreSQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.S3StorageLocation.ViewModels
{
    public class S3StorageLocationDetailViewModel : Screen, INotifyDataErrorInfo
    {
        public S3StorageLocationViewModel Context { get; set; }
        private readonly IRepository<S3StorageLocationGraphQLModel> _s3StorageLocationService;
        private readonly AwsS3ConfigCache _awsS3ConfigCache;

        
        public S3StorageLocationDetailViewModel(S3StorageLocationViewModel context, Helpers.Services.INotificationService notificationService,
            IRepository<S3StorageLocationGraphQLModel> s3StorageLocationService, AwsS3ConfigCache awsS3ConfigCache)
        {
            Context = context;
            _s3StorageLocationService = s3StorageLocationService;
            _errors = new Dictionary<string, List<string>>();
            _awsS3ConfigCache = awsS3ConfigCache;
            _ = Task.Run(() => InitializeAsync());
        }

        public async Task InitializeAsync()
        {
            await Task.WhenAll(
                _awsS3ConfigCache.EnsureLoadedAsync()
                );
            AwsS3Configs = new ObservableCollection<AwsS3ConfigGraphQLModel>(_awsS3ConfigCache.Items);
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
        private string _bucket;

        public string Bucket
        {
            get => _bucket;
            set
            {
                _bucket = value;
                NotifyOfPropertyChange(nameof(Bucket));
                this.TrackChange(nameof(Bucket));
                ValidateProperty(nameof(Bucket), value);

                NotifyOfPropertyChange(nameof(CanSave));


            }
        }
        private string _directory;

        public string Directory
        {
            get => _directory;
            set
            {
                _directory = value;
                NotifyOfPropertyChange(nameof(Directory));
                this.TrackChange(nameof(Directory));
                ValidateProperty(nameof(Directory), value);

                NotifyOfPropertyChange(nameof(CanSave));


            }
        }
        private string _key;
        public string Key
        {
            get => _key;
            set
            {
                _key = value;
                NotifyOfPropertyChange(nameof(Key));
                this.TrackChange(nameof(Key));
                ValidateProperty(nameof(Key), value);

                NotifyOfPropertyChange(nameof(CanSave));


            }
        }
        private AwsS3ConfigGraphQLModel _selectedAwsS3Config;
        [ExpandoPath("awsS3ConfigId", SerializeAsId = true)]

        public AwsS3ConfigGraphQLModel SelectedAwsS3Config
        {
            get => _selectedAwsS3Config;
            set
            {
                _selectedAwsS3Config = value;
                NotifyOfPropertyChange(nameof(SelectedAwsS3Config));
                this.TrackChange(nameof(SelectedAwsS3Config));
                ValidateProperty(nameof(SelectedAwsS3Config), value?.Id);

                NotifyOfPropertyChange(nameof(CanSave));


            }
        }
        #endregion
        #region PropertiesAndCommands

        private ObservableCollection<AwsS3ConfigGraphQLModel> _awsS3Configs = [];
        public ObservableCollection<AwsS3ConfigGraphQLModel> AwsS3Configs
        {
            get => _awsS3Configs;
            set
            {
                if (_awsS3Configs != value)
                {
                    _awsS3Configs = value;
                    NotifyOfPropertyChange(nameof(AwsS3Configs));
                }
            }
        }

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
        private void ValidateProperty(string propertyName, int? value)
        {
            try
            {
                ClearErrors(propertyName);
                switch (propertyName)
                {
                    case nameof(SelectedAwsS3Config):
                        if (value.HasValue && value.Value < 1) AddError(propertyName, "Debe seleccionar una configuración");
                        break;
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
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
                    case nameof(Description):
                        if (string.IsNullOrEmpty(value)) AddError(propertyName, "La descripción  no puede estar vacía");
                        break;
                    case nameof(Key):
                        if (string.IsNullOrEmpty(value)) AddError(propertyName, "La clave no puede estar vacía");
                        break;
                    case nameof(Bucket):
                        if (string.IsNullOrEmpty(value)) AddError(propertyName, "El  Bucket no puede estar vacío");
                        break;
                    case nameof(Directory):
                        if (string.IsNullOrEmpty(value)) AddError(propertyName, "Ek directorio no puede esta vacio");
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
            ValidateProperty(nameof(Key), Key);
            ValidateProperty(nameof(Bucket), Bucket);
            ValidateProperty(nameof(Directory), Directory);
            ValidateProperty(nameof(SelectedAwsS3Config), SelectedAwsS3Config?.Id);
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
        public async Task<S3StorageLocationGraphQLModel> LoadDataForEditAsync(int id)
        {
            try
            {
                string query = GetLoadS3StorageLocationByIdQuery();

                dynamic variables = new ExpandoObject();


                variables.singleItemResponseId = id;

                var entity = await _s3StorageLocationService.FindByIdAsync(query, variables);

                // Poblar el ViewModel con los datos del entity (sin bloquear UI thread)
                PopulateFromS3StorageLocation(entity);

                return entity;
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
        public void PopulateFromS3StorageLocation(S3StorageLocationGraphQLModel entity)
        {


            Id = entity.Id;
            Description = entity.Description;
            Key = entity.Key;
            Bucket = entity.Bucket;
            Directory = entity.Directory;
            SelectedAwsS3Config = entity.AwsS3Config is null ? null : AwsS3Configs.FirstOrDefault(c => c.Id == entity.AwsS3Config.Id);

            this.SeedValue(nameof(Description), Description);
            this.SeedValue(nameof(Key), Key);
            this.SeedValue(nameof(Bucket), Bucket);
            this.SeedValue(nameof(Directory), Directory);
            this.SeedValue(nameof(SelectedAwsS3Config), SelectedAwsS3Config);


            this.AcceptChanges();



        }
        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                UpsertResponseType<S3StorageLocationGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }
                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new S3StorageLocationCreateMessage() { CreatedS3StorageLocation = result }
                        : new S3StorageLocationUpdateMessage() { UpdatedS3StorageLocation = result }
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

        public async Task<UpsertResponseType<S3StorageLocationGraphQLModel>> ExecuteSaveAsync()
        {

            dynamic variables = new ExpandoObject();


            if (IsNewRecord)
            {

                variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                string query = GetCreateQuery();
                UpsertResponseType<S3StorageLocationGraphQLModel> groupCreated = await _s3StorageLocationService.CreateAsync<UpsertResponseType<S3StorageLocationGraphQLModel>>(query, variables);
                return groupCreated;
            }
            else
            {

                string query = GetUpdateQuery();
                variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = Id;
                UpsertResponseType<S3StorageLocationGraphQLModel> updatedGroup = await _s3StorageLocationService.UpdateAsync<UpsertResponseType<S3StorageLocationGraphQLModel>>(query, variables);
                return updatedGroup;
            }

        }
        public string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<S3StorageLocationGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "S3StorageLocation", nested: sq => sq
                   .Field(e => e.Id)
                  .Field(e => e.Description)

                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateS3StorageLocationInput!");

            var fragment = new GraphQLQueryFragment("createS3StorageLocation", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<S3StorageLocationGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "S3StorageLocation", nested: sq => sq
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
                new("data", "UpdateS3StorageLocationInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateS3StorageLocation", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public string GetLoadS3StorageLocationByIdQuery()
        {
            var S3StorageLocationFields = FieldSpec<S3StorageLocationGraphQLModel>
             .Create()

                 .Field(e => e.Id)

                 .Field(e => e.Description)
                 .Field(e => e.Bucket)
                 .Field(e => e.Directory)
                 .Field(e => e.Key)
                  .Select(e => e.AwsS3Config,  acc => acc
                    .Field(c => c!.Id)
                    .Field(c => c!.Description)
                    )



                 .Build();
            var S3StorageLocationIdParameter = new GraphQLQueryParameter("id", "ID!");

            var S3StorageLocationFragment = new GraphQLQueryFragment("S3StorageLocation", [S3StorageLocationIdParameter], S3StorageLocationFields, "SingleItemResponse");

            var builder = new GraphQLQueryBuilder([S3StorageLocationFragment]);

            return builder.GetQuery();

        }
        #endregion
    }
}
