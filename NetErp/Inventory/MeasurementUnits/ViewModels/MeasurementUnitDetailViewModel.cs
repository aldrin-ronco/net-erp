using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Dictionaries;
using Extensions.Global;
using GraphQL.Client.Http;
using Models.Inventory;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Inventory.MeasurementUnits.ViewModels
{
    public class MeasurementUnitDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<MeasurementUnitGraphQLModel> _measurementUnitService;
        private readonly IEventAggregator _eventAggregator;

        #endregion

        #region State

        public bool IsNewRecord => MeasurementUnitId == 0;

        private bool _isBusy;
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

        #endregion

        #region ComboBox Sources

        public ObservableCollection<KeyValuePair<string, string>> MeasurementUnitTypes { get; } =
            new(InventoriesDictionaries.MeasurementUnitTypeDictionary.ToList());

        #endregion

        #region Form Properties

        private int _measurementUnitId;
        public int MeasurementUnitId
        {
            get => _measurementUnitId;
            set
            {
                if (_measurementUnitId != value)
                {
                    _measurementUnitId = value;
                    NotifyOfPropertyChange(nameof(MeasurementUnitId));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
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

        private string _abbreviation = string.Empty;
        public string Abbreviation
        {
            get => _abbreviation;
            set
            {
                if (_abbreviation != value)
                {
                    _abbreviation = value;
                    NotifyOfPropertyChange(nameof(Abbreviation));
                    ValidateProperty(nameof(Abbreviation), value);
                    this.TrackChange(nameof(Abbreviation));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _type = string.Empty;
        public string Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    NotifyOfPropertyChange(nameof(Type));
                    ValidateProperty(nameof(Type), value);
                    this.TrackChange(nameof(Type));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _dianCode = string.Empty;
        public string DianCode
        {
            get => _dianCode;
            set
            {
                if (_dianCode != value)
                {
                    _dianCode = value;
                    NotifyOfPropertyChange(nameof(DianCode));
                    ValidateProperty(nameof(DianCode), value);
                    this.TrackChange(nameof(DianCode));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        #endregion

        #region Validation (INotifyDataErrorInfo)

        private readonly Dictionary<string, List<string>> _errors = new();

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null!;
            return _errors[propertyName];
        }

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
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
            if (string.IsNullOrEmpty(value)) value = string.Empty;
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(Name):
                    if (string.IsNullOrEmpty(Name)) AddError(propertyName, "El nombre no puede estar vacío");
                    break;
                case nameof(Abbreviation):
                    if (string.IsNullOrEmpty(Abbreviation)) AddError(propertyName, "La abreviación no puede estar vacía");
                    break;
                case nameof(Type):
                    if (string.IsNullOrEmpty(Type)) AddError(propertyName, "El tipo no puede estar vacío");
                    break;
                case nameof(DianCode):
                    if (string.IsNullOrEmpty(DianCode)) AddError(propertyName, "El código DIAN no puede estar vacío");
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

        #region Constructor

        public MeasurementUnitDetailViewModel(
            IRepository<MeasurementUnitGraphQLModel> measurementUnitService,
            IEventAggregator eventAggregator)
        {
            _measurementUnitService = measurementUnitService;
            _eventAggregator = eventAggregator;
        }

        #endregion

        #region Lifecycle

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.SetFocus(() => Name);
            ValidateProperties();
            this.AcceptChanges();
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                UpsertResponseType<MeasurementUnitGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(
                        text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo",
                        title: $"{result.Message}!",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new MeasurementUnitCreateMessage { CreatedMeasurementUnit = result }
                        : new MeasurementUnitUpdateMessage { UpdatedMeasurementUnit = result }
                );

                await TryCloseAsync(true);
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content!.ToString()!);
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"\r\n{graphQLError.Errors[0].Message}\r\n{graphQLError.Errors[0].Extensions.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{currentMethod!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<UpsertResponseType<MeasurementUnitGraphQLModel>> ExecuteSaveAsync()
        {
            if (IsNewRecord)
            {
                string query = GetCreateQuery();
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                return await _measurementUnitService.CreateAsync<UpsertResponseType<MeasurementUnitGraphQLModel>>(query, variables);
            }
            else
            {
                string query = GetUpdateQuery();
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = MeasurementUnitId;
                return await _measurementUnitService.UpdateAsync<UpsertResponseType<MeasurementUnitGraphQLModel>>(query, variables);
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        #endregion

        #region GraphQL Queries

        public string GetCreateQuery()
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

            var parameter = new GraphQLQueryParameter("input", "CreateMeasurementUnitInput!");
            var fragment = new GraphQLQueryFragment("createMeasurementUnit", [parameter], fields, "CreateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetUpdateQuery()
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

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateMeasurementUnitInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateMeasurementUnit", parameters, fields, "UpdateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        #endregion

        #region Helper

        public new void AcceptChanges()
        {
            ViewModelExtensions.AcceptChanges(this);
        }

        #endregion
    }
}
