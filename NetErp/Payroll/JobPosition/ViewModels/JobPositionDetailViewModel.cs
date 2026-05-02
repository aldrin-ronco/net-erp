using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Payroll;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Payroll.JobPosition.ViewModels
{
    public class JobPositionDetailViewModel(IRepository<JobPositionGraphQLModel> jobPositionService, IEventAggregator eventAggregator, JoinableTaskFactory joinableTaskFactory, StringLengthCache stringLengthCache)
        : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<JobPositionGraphQLModel> _jobPositionService = jobPositionService;
        private readonly IEventAggregator _eventAggregator = eventAggregator;
        private readonly JoinableTaskFactory _joinableTaskFactory = joinableTaskFactory;
        private readonly StringLengthCache _stringLengthCache = stringLengthCache;
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
        } = 400;

        #endregion
        #region StringLength Properties

        public int NameMaxLength => _stringLengthCache.GetMaxLength<JobPositionGraphQLModel>(nameof(JobPositionGraphQLModel.Name));
        public int DescriptionMaxLength => _stringLengthCache.GetMaxLength<JobPositionGraphQLModel>(nameof(JobPositionGraphQLModel.Description));
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

        public string Name
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Name));
                    ValidateProperty(nameof(Name), value);
                    this.TrackChange(nameof(Name));
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
                    ValidateProperty(nameof(Description), value);
                    this.TrackChange(nameof(Description));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;
       
        
        public new bool IsActive
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsActive));
                    this.TrackChange(nameof(IsActive), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
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
        #region Validation (INotifyDataErrorInfo)

        private readonly Dictionary<string, List<string>> _errors = [];

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.TryGetValue(propertyName, out List<string>? value))
                return Enumerable.Empty<string>();
            return value;
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
            if (_errors.ContainsKey(propertyName))
            {
                RaiseErrorsChanged(propertyName);
            }
            _errors.Remove(propertyName);
        }

        private void ValidateProperty(string propertyName, string value)
        {
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(Name):
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "El nombre del puesto de trabajo es requerido");
                    break;
                case nameof(Description):
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "La descripción del puesto de trabajo es requerida");
                    break;
                    
            }
        }

        private void ValidateProperties()
        {
            ValidateProperty(nameof(Name), Name);
        }

        #endregion
        #region Button States

        public bool CanSave => !HasErrors && this.HasChanges() && !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Description);

        #endregion
        #region SetForNew / SetForEdit

        public void SetForNew()
        {
            this.ClearSeeds();
            this.AcceptChanges();
        }

        public void SetForEdit(JobPositionGraphQLModel entity)
        {
            Id = entity.Id;
            Name = entity.Name;
            Description = entity.Description;
            IsActive = entity.IsActive;
            NotifyOfPropertyChange(nameof(IsNewRecord));

            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(Description), Description);
            this.SeedValue(nameof(IsActive), IsActive);
            this.AcceptChanges();
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<JobPositionGraphQLModel> result = await ExecuteSaveAsync();
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
                        ? new JobPositionCreateMessage { CreatedJobPosition = result }
                        : new JobPositionUpdateMessage { UpdatedJobPosition = result }
                );

                await TryCloseAsync(true);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(SaveAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<UpsertResponseType<JobPositionGraphQLModel>> ExecuteSaveAsync()
        {
            if (IsNewRecord)
            {
                string query = _createQuery.Value;
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");

                return await _jobPositionService.CreateAsync<UpsertResponseType<JobPositionGraphQLModel>>(query, variables);
            }
            else
            {
                string query = _updateQuery.Value;
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = Id;
                return await _jobPositionService.UpdateAsync<UpsertResponseType<JobPositionGraphQLModel>>(query, variables);
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<string> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<JobPositionGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "jobPosition", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateJobPositionInput!");
            var fragment = new GraphQLQueryFragment("createJobPosition", [parameter], fields, "CreateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        private static readonly Lazy<string> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<JobPositionGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "jobPosition", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            List<GraphQLQueryParameter> parameters =
            [
                new("data", "UpdateJobPositionInput!"),
                new("id", "ID!")
            ];
            var fragment = new GraphQLQueryFragment("updateJobPosition", parameters, fields, "UpdateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        #endregion
    }
}