using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Dictionaries;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Inventory;
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

namespace NetErp.Inventory.MeasurementUnits.ViewModels
{
    public class MeasurementUnitDetailViewModel(
        IRepository<MeasurementUnitGraphQLModel> measurementUnitService,
        IEventAggregator eventAggregator,
        StringLengthCache stringLengthCache,
        JoinableTaskFactory joinableTaskFactory) : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<MeasurementUnitGraphQLModel> _measurementUnitService = measurementUnitService ?? throw new ArgumentNullException(nameof(measurementUnitService));
        private readonly IEventAggregator _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        private readonly StringLengthCache _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
        private readonly JoinableTaskFactory _joinableTaskFactory = joinableTaskFactory;

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
        } = 400;

        #endregion

        #region MaxLength Properties

        public int NameMaxLength => _stringLengthCache.GetMaxLength<MeasurementUnitGraphQLModel>(nameof(MeasurementUnitGraphQLModel.Name));
        public int AbbreviationMaxLength => _stringLengthCache.GetMaxLength<MeasurementUnitGraphQLModel>(nameof(MeasurementUnitGraphQLModel.Abbreviation));
        public int TypeMaxLength => _stringLengthCache.GetMaxLength<MeasurementUnitGraphQLModel>(nameof(MeasurementUnitGraphQLModel.Type));
        public int DianCodeMaxLength => _stringLengthCache.GetMaxLength<MeasurementUnitGraphQLModel>(nameof(MeasurementUnitGraphQLModel.DianCode));

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

        #region ComboBox Sources

        public ObservableCollection<KeyValuePair<string, string>> MeasurementUnitTypes { get; } =
            new(InventoriesDictionaries.MeasurementUnitTypeDictionary.ToList());

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

        public string Abbreviation
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Abbreviation));
                    ValidateProperty(nameof(Abbreviation), value);
                    this.TrackChange(nameof(Abbreviation));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public string Type
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Type));
                    ValidateProperty(nameof(Type), value);
                    this.TrackChange(nameof(Type));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public string DianCode
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DianCode));
                    ValidateProperty(nameof(DianCode), value);
                    this.TrackChange(nameof(DianCode));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        #endregion

        #region Button States

        public bool CanSave => !HasErrors && this.HasChanges()
                               && !string.IsNullOrEmpty(Name)
                               && !string.IsNullOrEmpty(Abbreviation)
                               && !string.IsNullOrEmpty(Type)
                               && !string.IsNullOrEmpty(DianCode);

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

        #region Lifecycle

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperties();
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion

        #region SetForNew / SetForEdit

        public void SetForNew()
        {
            Id = 0;
            Name = string.Empty;
            Abbreviation = string.Empty;
            Type = string.Empty;
            DianCode = string.Empty;
            SeedDefaultValues();
        }

        public void SetForEdit(MeasurementUnitGraphQLModel entity)
        {
            Id = entity.Id;
            Name = entity.Name;
            Abbreviation = entity.Abbreviation;
            Type = entity.Type;
            DianCode = entity.DianCode;
            SeedCurrentValues();
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.AcceptChanges();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(Abbreviation), Abbreviation);
            this.SeedValue(nameof(Type), Type);
            this.SeedValue(nameof(DianCode), DianCode);
            this.AcceptChanges();
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<MeasurementUnitGraphQLModel> result = await ExecuteSaveAsync();

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
                        ? new MeasurementUnitCreateMessage { CreatedMeasurementUnit = result }
                        : new MeasurementUnitUpdateMessage { UpdatedMeasurementUnit = result },
                    CancellationToken.None);

                await TryCloseAsync(true);
            }
            catch (AsyncException ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"Error al realizar operación.\r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(SaveAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<UpsertResponseType<MeasurementUnitGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                if (IsNewRecord)
                {
                    (GraphQLQueryFragment _, string query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    return await _measurementUnitService.CreateAsync<UpsertResponseType<MeasurementUnitGraphQLModel>>(query, variables);
                }
                else
                {
                    (GraphQLQueryFragment _, string query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;
                    return await _measurementUnitService.UpdateAsync<UpsertResponseType<MeasurementUnitGraphQLModel>>(query, variables);
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
            var fields = FieldSpec<UpsertResponseType<MeasurementUnitGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "measurementUnit", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.Abbreviation)
                    .Field(f => f.Type)
                    .Field(f => f.DianCode))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createMeasurementUnit",
                [new("input", "CreateMeasurementUnitInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<MeasurementUnitGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "measurementUnit", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.Abbreviation)
                    .Field(f => f.Type)
                    .Field(f => f.DianCode))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("updateMeasurementUnit",
                [new("data", "UpdateMeasurementUnitInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion

        #region Validation (INotifyDataErrorInfo)

        private readonly Dictionary<string, List<string>> _errors = [];

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.TryGetValue(propertyName, out List<string>? value)) return Enumerable.Empty<string>();
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
                _errors.Remove(propertyName);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ValidateProperty(string propertyName, string value)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty;
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(Name):
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "El nombre no puede estar vacío");
                    break;
                case nameof(Abbreviation):
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "La abreviación no puede estar vacía");
                    break;
                case nameof(Type):
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "El tipo no puede estar vacío");
                    break;
                case nameof(DianCode):
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "El código DIAN no puede estar vacío");
                    break;
            }
        }

        private void ValidateProperties()
        {
            ValidateProperty(nameof(Name), Name);
            ValidateProperty(nameof(Abbreviation), Abbreviation);
            ValidateProperty(nameof(Type), Type);
            ValidateProperty(nameof(DianCode), DianCode);
        }

        #endregion
    }
}
