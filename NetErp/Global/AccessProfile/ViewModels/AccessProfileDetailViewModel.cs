using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.AccessProfileGraphQLModel;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.AccessProfile.ViewModels
{
    public class AccessProfileDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<AccessProfileGraphQLModel> _accessProfileService;
        private readonly IEventAggregator _eventAggregator;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        #endregion

        #region Constructor

        public AccessProfileDetailViewModel(
            IRepository<AccessProfileGraphQLModel> accessProfileService,
            IEventAggregator eventAggregator,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory)
        {
            _accessProfileService = accessProfileService;
            _eventAggregator = eventAggregator;
            _stringLengthCache = stringLengthCache;
            _joinableTaskFactory = joinableTaskFactory;
        }

        #endregion

        #region Properties

        public int Id { get; set; }
        public int SourceProfileId { get; set; }

        public bool IsNewRecord => Id == 0;
        public bool IsClone => SourceProfileId > 0;

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
        } = 450;

        public string Name
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Name));
                    this.TrackChange(nameof(Name));
                    ValidateProperty(nameof(Name), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public string Description
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Description));
                    this.TrackChange(nameof(Description));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("isActive")]
        public bool ProfileIsActive
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ProfileIsActive));
                    this.TrackChange(nameof(ProfileIsActive));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = true;

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

        #region StringLength Properties

        public int NameMaxLength => _stringLengthCache.GetMaxLength<AccessProfileGraphQLModel>(nameof(AccessProfileGraphQLModel.Name));
        public int DescriptionMaxLength => _stringLengthCache.GetMaxLength<AccessProfileGraphQLModel>(nameof(AccessProfileGraphQLModel.Description));

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

        public bool CanSave
        {
            get
            {
                if (string.IsNullOrEmpty(Name)) return false;
                if (!this.HasChanges()) return false;
                return _errors.Count <= 0;
            }
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;

                UpsertResponseType<AccessProfileGraphQLModel> result;

                if (IsClone)
                {
                    var (_, query) = _cloneQuery.Value;
                    dynamic variables = new ExpandoObject();
                    variables.createResponseInput = new
                    {
                        sourceAccessProfileId = SourceProfileId,
                        name = Name,
                        description = Description
                    };
                    result = await _accessProfileService.CreateAsync<UpsertResponseType<AccessProfileGraphQLModel>>(query, variables);
                }
                else if (IsNewRecord)
                {
                    var (_, query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    result = await _accessProfileService.CreateAsync<UpsertResponseType<AccessProfileGraphQLModel>>(query, variables);
                }
                else
                {
                    var (_, query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;
                    result = await _accessProfileService.UpdateAsync<UpsertResponseType<AccessProfileGraphQLModel>>(query, variables);
                }

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
                    IsNewRecord || IsClone
                        ? (object)new AccessProfileCreateMessage { CreatedAccessProfile = result }
                        : new AccessProfileUpdateMessage { UpdatedAccessProfile = result },
                    CancellationToken.None);

                await TryCloseAsync(true);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al realizar operación.\r\n{GetType().Name}.{nameof(SaveAsync)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        #endregion

        #region Lifecycle

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperties();
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion

        #region INotifyDataErrorInfo

        private readonly Dictionary<string, List<string>> _errors = [];

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
                _errors.Remove(propertyName);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ValidateProperty(string propertyName, string value)
        {
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(Name):
                    if (string.IsNullOrEmpty(Name)) AddError(propertyName, "El campo 'Nombre' no puede estar vacío.");
                    break;
            }
        }

        private void ValidateProperties()
        {
            ValidateProperty(nameof(Name), Name);
        }

        #endregion

        #region SetForNew

        public void SetForNew()
        {
            Id = 0;
            Name = string.Empty;
            Description = string.Empty;
            ProfileIsActive = true;

            this.ClearSeeds();
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(Description), Description);
            this.SeedValue(nameof(ProfileIsActive), ProfileIsActive);
            this.AcceptChanges();
        }

        public void SetForEdit(AccessProfileGraphQLModel profile)
        {
            Id = profile.Id;
            SourceProfileId = 0;
            Name = profile.Name;
            Description = profile.Description;
            ProfileIsActive = profile.IsActive;

            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(Description), Description);
            this.SeedValue(nameof(ProfileIsActive), ProfileIsActive);
            this.AcceptChanges();
        }

        public void SetForClone(int sourceProfileId, string sourceName)
        {
            Id = 0;
            SourceProfileId = sourceProfileId;
            Name = $"{sourceName} (COPIA)";
            Description = string.Empty;
            ProfileIsActive = true;

            this.ClearSeeds();
            this.SeedValue(nameof(Name), string.Empty);
            this.SeedValue(nameof(Description), Description);
            this.SeedValue(nameof(ProfileIsActive), ProfileIsActive);
            this.AcceptChanges();
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<AccessProfileGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accessProfile", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Description)
                    .Field(e => e.IsActive))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createAccessProfile",
                [new("input", "CreateAccessProfileInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<AccessProfileGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accessProfile", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Description)
                    .Field(e => e.IsActive))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("updateAccessProfile",
                [new("data", "UpdateAccessProfileInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _cloneQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<AccessProfileGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accessProfile", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Description)
                    .Field(e => e.IsActive))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("cloneAccessProfile",
                [new("input", "CloneAccessProfileInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
