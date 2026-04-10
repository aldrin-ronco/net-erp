using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Global.S3StorageLocation.Validators;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.S3StorageLocation.ViewModels
{
    public class S3StorageLocationDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<S3StorageLocationGraphQLModel> _service;
        private readonly IEventAggregator _eventAggregator;
        private readonly StringLengthCache _stringLengthCache;
        private readonly AwsS3ConfigCache _awsS3ConfigCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly S3StorageLocationValidator _validator;

        #endregion

        #region MaxLength Properties

        public int DescriptionMaxLength => _stringLengthCache.GetMaxLength<S3StorageLocationGraphQLModel>(nameof(S3StorageLocationGraphQLModel.Description));
        public int KeyMaxLength => _stringLengthCache.GetMaxLength<S3StorageLocationGraphQLModel>(nameof(S3StorageLocationGraphQLModel.Key));
        public int BucketMaxLength => _stringLengthCache.GetMaxLength<S3StorageLocationGraphQLModel>(nameof(S3StorageLocationGraphQLModel.Bucket));
        public int DirectoryMaxLength => _stringLengthCache.GetMaxLength<S3StorageLocationGraphQLModel>(nameof(S3StorageLocationGraphQLModel.Directory));

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
        } = 500;

        public double DialogHeight
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogHeight));
                }
            }
        } = 430;

        #endregion

        #region Properties

        private readonly Dictionary<string, List<string>> _errors = [];

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

        public bool IsNewRecord => Id == 0;

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
                    this.TrackChange(nameof(Description), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public string Key
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Key));
                    ValidateProperty(nameof(Key), value);
                    this.TrackChange(nameof(Key), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public string Bucket
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Bucket));
                    ValidateProperty(nameof(Bucket), value);
                    this.TrackChange(nameof(Bucket), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public string Directory
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Directory));
                    ValidateProperty(nameof(Directory), value);
                    this.TrackChange(nameof(Directory), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("awsS3ConfigId", SerializeAsId = true)]
        public AwsS3ConfigGraphQLModel? SelectedAwsS3Config
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedAwsS3Config));
                    this.TrackChange(nameof(SelectedAwsS3Config), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public ObservableCollection<AwsS3ConfigGraphQLModel> AwsS3Configs
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AwsS3Configs));
                }
            }
        } = [];

        public bool CanSave => _validator.CanSave(this.HasChanges(), HasErrors);

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

        public S3StorageLocationDetailViewModel(
            IRepository<S3StorageLocationGraphQLModel> service,
            IEventAggregator eventAggregator,
            StringLengthCache stringLengthCache,
            AwsS3ConfigCache awsS3ConfigCache,
            JoinableTaskFactory joinableTaskFactory,
            S3StorageLocationValidator validator)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _awsS3ConfigCache = awsS3ConfigCache ?? throw new ArgumentNullException(nameof(awsS3ConfigCache));
            _joinableTaskFactory = joinableTaskFactory;
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        #endregion

        #region Lifecycle

        protected override async Task OnInitializedAsync(CancellationToken cancellationToken)
        {
            await base.OnInitializedAsync(cancellationToken);
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperties();
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close) this.AcceptChanges();
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region SetForNew / SetForEdit

        public void SetForNew()
        {
            LoadComboSources();
            Id = 0;
            Description = string.Empty;
            Key = string.Empty;
            Bucket = string.Empty;
            Directory = string.Empty;
            SelectedAwsS3Config = null;
            SeedDefaultValues();
        }

        public void SetForEdit(S3StorageLocationGraphQLModel entity)
        {
            LoadComboSources();
            Id = entity.Id;
            Description = entity.Description ?? string.Empty;
            Key = entity.Key ?? string.Empty;
            Bucket = entity.Bucket ?? string.Empty;
            Directory = entity.Directory ?? string.Empty;
            SelectedAwsS3Config = entity.AwsS3Config is null
                ? null
                : AwsS3Configs.FirstOrDefault(c => c.Id == entity.AwsS3Config.Id);
            SeedCurrentValues();
        }

        private void LoadComboSources()
        {
            AwsS3Configs = [.. _awsS3ConfigCache.Items];
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(Description), Description);
            this.SeedValue(nameof(Key), Key);
            this.SeedValue(nameof(Bucket), Bucket);
            this.SeedValue(nameof(Directory), Directory);
            this.AcceptChanges();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Description), Description);
            this.SeedValue(nameof(Key), Key);
            this.SeedValue(nameof(Bucket), Bucket);
            this.SeedValue(nameof(Directory), Directory);
            this.SeedValue(nameof(SelectedAwsS3Config), SelectedAwsS3Config);
            this.AcceptChanges();
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<S3StorageLocationGraphQLModel> result = await ExecuteSaveAsync();

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
                        ? new S3StorageLocationCreateMessage { CreatedS3StorageLocation = result }
                        : new S3StorageLocationUpdateMessage { UpdatedS3StorageLocation = result },
                    CancellationToken.None);

                await TryCloseAsync(true);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(SaveAsync)} \r\n{ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<UpsertResponseType<S3StorageLocationGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                if (IsNewRecord)
                {
                    var (_, query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    return await _service.CreateAsync<UpsertResponseType<S3StorageLocationGraphQLModel>>(query, variables);
                }
                else
                {
                    var (_, query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;
                    return await _service.UpdateAsync<UpsertResponseType<S3StorageLocationGraphQLModel>>(query, variables);
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

        private static Dictionary<string, object> BuildResponseFields()
        {
            return FieldSpec<UpsertResponseType<S3StorageLocationGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "s3StorageLocation", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    .Field(e => e.Key)
                    .Field(e => e.Bucket)
                    .Field(e => e.Directory)
                    .Select(e => e.AwsS3Config, aws => aws
                        .Field(a => a.Id)
                        .Field(a => a.Description)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();
        }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            var fields = BuildResponseFields();
            var fragment = new GraphQLQueryFragment("createS3StorageLocation",
                [new("input", "CreateS3StorageLocationInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            var fields = BuildResponseFields();
            var fragment = new GraphQLQueryFragment("updateS3StorageLocation",
                [new("data", "UpdateS3StorageLocationInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion

        #region Validation (INotifyDataErrorInfo)

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.TryGetValue(propertyName, out List<string>? value)) return Enumerable.Empty<string>();
            return value;
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
            if (_errors.ContainsKey(propertyName))
            {
                RaiseErrorsChanged(propertyName);
            }
            _errors.Remove(propertyName);
        }

        private void ValidateProperty(string propertyName, string? value)
        {
            IReadOnlyList<string> errors = _validator.Validate(propertyName, value);
            SetPropertyErrors(propertyName, errors);
        }

        private void ValidateProperties()
        {
            Dictionary<string, IReadOnlyList<string>> all = _validator.ValidateAll(Description, Key, Bucket, Directory);
            SetPropertyErrors(nameof(Description), all.TryGetValue(nameof(Description), out var e1) ? e1 : []);
            SetPropertyErrors(nameof(Key), all.TryGetValue(nameof(Key), out var e2) ? e2 : []);
            SetPropertyErrors(nameof(Bucket), all.TryGetValue(nameof(Bucket), out var e3) ? e3 : []);
            SetPropertyErrors(nameof(Directory), all.TryGetValue(nameof(Directory), out var e4) ? e4 : []);
        }

        private void SetPropertyErrors(string propertyName, IReadOnlyList<string> errors)
        {
            bool hadErrors = _errors.ContainsKey(propertyName);

            if (errors.Count > 0)
                _errors[propertyName] = [.. errors];
            else if (hadErrors)
                _errors.Remove(propertyName);

            if (hadErrors || errors.Count > 0)
                RaiseErrorsChanged(propertyName);
        }

        #endregion
    }
}
