using Amazon;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Dictionaries;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.AwsS3Config.ViewModels
{
    public class AwsS3ConfigDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<AwsS3ConfigGraphQLModel> _awsS3ConfigService;
        private readonly IEventAggregator _eventAggregator;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        #endregion

        #region Dialog Size

        public double DialogWidth
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogWidth));
                }
            }
        } = 420;

        #endregion

        #region State

        public bool IsNewRecord => Id == 0;

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        #endregion

        #region Form Properties

        public int Id
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Id));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        public string Description
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Description));
                    ValidateProperty(nameof(Description), value);
                    this.TrackChange(nameof(Description));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public string Region
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Region));
                    ValidateProperty(nameof(Region), value);
                    this.TrackChange(nameof(Region));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = "us-east-1";

        public string AccessKey
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccessKey));
                    ValidateProperty(nameof(AccessKey), value);
                    this.TrackChange(nameof(AccessKey));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public string SecretKey
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SecretKey));
                    ValidateProperty(nameof(SecretKey), value);
                    this.TrackChange(nameof(SecretKey));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public Dictionary<string, RegionEndpoint> AwsSesRegionDictionary => GlobalDictionaries.AwsSesRegionDictionary;

        #endregion

        #region StringLength Properties

        public int DescriptionMaxLength => _stringLengthCache.GetMaxLength<AwsS3ConfigGraphQLModel>(nameof(AwsS3ConfigGraphQLModel.Description));
        public int AccessKeyMaxLength => _stringLengthCache.GetMaxLength<AwsS3ConfigGraphQLModel>(nameof(AwsS3ConfigGraphQLModel.AccessKey));
        public int SecretKeyMaxLength => _stringLengthCache.GetMaxLength<AwsS3ConfigGraphQLModel>(nameof(AwsS3ConfigGraphQLModel.SecretKey));

        #endregion

        #region Validation (INotifyDataErrorInfo)

        private readonly Dictionary<string, List<string>> _errors = [];

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return Enumerable.Empty<string>();
            return _errors[propertyName];
        }

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = [];

            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ClearErrors(string propertyName)
        {
            _errors.Remove(propertyName); // Existence verification not needed, remove do check 
            RaiseErrorsChanged(propertyName);
        }

        private void ValidateProperty(string propertyName, string value)
        {
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(Description):
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "La descripción no puede estar vacía");
                    break;
                case nameof(AccessKey):
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "La clave de acceso no puede estar vacía");
                    break;
                case nameof(SecretKey):
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "La clave secreta no puede estar vacía");
                    break;
                case nameof(Region):
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "Debe seleccionar una región");
                    break;
            }
        }

        private void ValidateProperties()
        {
            ValidateProperty(nameof(Description), Description);
            ValidateProperty(nameof(Region), Region);
            ValidateProperty(nameof(AccessKey), AccessKey);
            ValidateProperty(nameof(SecretKey), SecretKey);
        }

        #endregion

        #region Button States

        public bool CanSave => !HasErrors && this.HasChanges()
                               && !string.IsNullOrEmpty(Description)
                               && !string.IsNullOrEmpty(Region)
                               && !string.IsNullOrEmpty(AccessKey)
                               && !string.IsNullOrEmpty(SecretKey);

        #endregion

        #region Commands

        private ICommand? _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                _saveCommand ??= new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        private ICommand? _cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                _cancelCommand ??= new AsyncCommand(CancelAsync);
                return _cancelCommand;
            }
        }

        #endregion

        #region Constructor

        public AwsS3ConfigDetailViewModel(
            IRepository<AwsS3ConfigGraphQLModel> awsS3ConfigService,
            IEventAggregator eventAggregator,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory)
        {
            _awsS3ConfigService = awsS3ConfigService;
            _eventAggregator = eventAggregator;
            _stringLengthCache = stringLengthCache;
            _joinableTaskFactory = joinableTaskFactory;
        }

        #endregion

        #region SetForNew / SetForEdit

        public void SetForNew()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(Description), Description);
            this.SeedValue(nameof(AccessKey), AccessKey);
            this.SeedValue(nameof(SecretKey), SecretKey);
            this.SeedValue(nameof(Region), Region);
            this.AcceptChanges();
            ValidateProperties();
        }

        public void SetForEdit(AwsS3ConfigGraphQLModel entity)
        {
            Id = entity.Id;
            Description = entity.Description;
            AccessKey = entity.AccessKey;
            SecretKey = entity.SecretKey;
            Region = entity.Region;

            this.SeedValue(nameof(Description), Description);
            this.SeedValue(nameof(AccessKey), AccessKey);
            this.SeedValue(nameof(SecretKey), SecretKey);
            this.SeedValue(nameof(Region), Region);
            this.AcceptChanges();
            ValidateProperties();
        }

        #endregion

        #region Load for Edit

        public async Task LoadDataForEditAsync(int id)
        {
            var (fragment, query) = _loadByIdQuery.Value;
            System.Dynamic.ExpandoObject variables = new GraphQLVariables()
                .For(fragment, "id", id)
                .Build();

            AwsS3ConfigGraphQLModel entity = await _awsS3ConfigService.FindByIdAsync(query, variables);
            SetForEdit(entity);
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<AwsS3ConfigGraphQLModel> result = await ExecuteSaveAsync();

                if (!result.Success)
                {
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    ThemedMessageBox.Show(
                        text: $"El guardado no ha sido exitoso\r\n\r\n{result.Errors.ToUserMessage()}\r\n\r\nVerifique los datos y vuelva a intentarlo",
                        title: $"{result.Message}!",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new AwsS3ConfigCreateMessage { CreatedAwsS3Config = result }
                        : new AwsS3ConfigUpdateMessage { UpdatedAwsS3Config = result },
                    CancellationToken.None);

                await TryCloseAsync(true);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(SaveAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<UpsertResponseType<AwsS3ConfigGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                if (IsNewRecord)
                {
                    var (_, query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    return await _awsS3ConfigService.CreateAsync<UpsertResponseType<AwsS3ConfigGraphQLModel>>(query, variables);
                }
                else
                {
                    var (_, query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;
                    return await _awsS3ConfigService.UpdateAsync<UpsertResponseType<AwsS3ConfigGraphQLModel>>(query, variables);
                }
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<AwsS3ConfigGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "awsS3Config", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Description))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createAwsS3Config",
                [new("input", "CreateAwsS3ConfigInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<AwsS3ConfigGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "awsS3Config", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Description))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("updateAwsS3Config",
                [new("data", "UpdateAwsS3ConfigInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadByIdQuery = new(() =>
        {
            var fields = FieldSpec<AwsS3ConfigGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.Description)
                .Field(e => e.AccessKey)
                .Field(e => e.SecretKey)
                .Field(e => e.Region)
                .Build();

            var fragment = new GraphQLQueryFragment("AwsS3Config",
                [new("id", "ID!")],
                fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion
    }
}
