using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using GraphQL.Client.Http;
using Models.Books;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.TaxCategory.ViewModels
{
    public class TaxCategoryDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<TaxCategoryGraphQLModel> _taxCategoryService;
        private readonly IEventAggregator _eventAggregator;

        #endregion

        #region State

        public bool IsNewRecord => TaxCategoryId == 0;

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

        #region Form Properties

        private int _taxCategoryId;
        public int TaxCategoryId
        {
            get => _taxCategoryId;
            set
            {
                if (_taxCategoryId != value)
                {
                    _taxCategoryId = value;
                    NotifyOfPropertyChange(nameof(TaxCategoryId));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        private string _code = string.Empty;
        public string Code
        {
            get => _code;
            set
            {
                if (_code != value)
                {
                    _code = value;
                    NotifyOfPropertyChange(nameof(Code));
                    ValidateProperty(nameof(Code), value);
                    if (IsNewRecord) this.TrackChange(nameof(Code));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool IsCodeReadOnly => !IsNewRecord;

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

        private string _prefix = string.Empty;
        public string Prefix
        {
            get => _prefix;
            set
            {
                if (_prefix != value)
                {
                    _prefix = value;
                    NotifyOfPropertyChange(nameof(Prefix));
                    ValidateProperty(nameof(Prefix), value);
                    this.TrackChange(nameof(Prefix));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _usesPercentage;
        public bool UsesPercentage
        {
            get => _usesPercentage;
            set
            {
                if (_usesPercentage != value)
                {
                    _usesPercentage = value;
                    NotifyOfPropertyChange(nameof(UsesPercentage));
                    this.TrackChange(nameof(UsesPercentage));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _generatedTaxAccountIsRequired;
        public bool GeneratedTaxAccountIsRequired
        {
            get => _generatedTaxAccountIsRequired;
            set
            {
                if (_generatedTaxAccountIsRequired != value)
                {
                    _generatedTaxAccountIsRequired = value;
                    NotifyOfPropertyChange(nameof(GeneratedTaxAccountIsRequired));
                    NotifyOfPropertyChange(nameof(IsReadOnlyGeneratedTaxRefundAccountIsRequired));
                    this.TrackChange(nameof(GeneratedTaxAccountIsRequired));
                    NotifyOfPropertyChange(nameof(CanSave));
                    if (value == false) GeneratedTaxRefundAccountIsRequired = false;
                }
            }
        }

        private bool _generatedTaxRefundAccountIsRequired;
        public bool GeneratedTaxRefundAccountIsRequired
        {
            get => _generatedTaxRefundAccountIsRequired;
            set
            {
                if (_generatedTaxRefundAccountIsRequired != value)
                {
                    _generatedTaxRefundAccountIsRequired = value;
                    NotifyOfPropertyChange(nameof(GeneratedTaxRefundAccountIsRequired));
                    this.TrackChange(nameof(GeneratedTaxRefundAccountIsRequired));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _deductibleTaxAccountIsRequired;
        public bool DeductibleTaxAccountIsRequired
        {
            get => _deductibleTaxAccountIsRequired;
            set
            {
                if (_deductibleTaxAccountIsRequired != value)
                {
                    _deductibleTaxAccountIsRequired = value;
                    NotifyOfPropertyChange(nameof(DeductibleTaxAccountIsRequired));
                    NotifyOfPropertyChange(nameof(IsReadOnlyDeductibleTaxRefundAccountIsRequired));
                    this.TrackChange(nameof(DeductibleTaxAccountIsRequired));
                    NotifyOfPropertyChange(nameof(CanSave));
                    if (value == false) DeductibleTaxRefundAccountIsRequired = false;
                }
            }
        }

        private bool _deductibleTaxRefundAccountIsRequired;
        public bool DeductibleTaxRefundAccountIsRequired
        {
            get => _deductibleTaxRefundAccountIsRequired;
            set
            {
                if (_deductibleTaxRefundAccountIsRequired != value)
                {
                    _deductibleTaxRefundAccountIsRequired = value;
                    NotifyOfPropertyChange(nameof(DeductibleTaxRefundAccountIsRequired));
                    this.TrackChange(nameof(DeductibleTaxRefundAccountIsRequired));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool IsReadOnlyGeneratedTaxRefundAccountIsRequired => !GeneratedTaxAccountIsRequired;
        public bool IsReadOnlyDeductibleTaxRefundAccountIsRequired => !DeductibleTaxAccountIsRequired;

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
                case nameof(Code):
                    if (string.IsNullOrEmpty(Code)) AddError(propertyName, "El código no puede estar vacío");
                    break;
                case nameof(Name):
                    if (string.IsNullOrEmpty(Name)) AddError(propertyName, "El nombre no puede estar vacío");
                    break;
                case nameof(Prefix):
                    if (string.IsNullOrEmpty(Prefix)) AddError(propertyName, "El prefijo no puede estar vacío");
                    break;
            }
        }

        private void ValidateProperties()
        {
            ValidateProperty(nameof(Code), Code);
            ValidateProperty(nameof(Name), Name);
            ValidateProperty(nameof(Prefix), Prefix);
        }

        #endregion

        #region Button States

        public bool CanSave => !HasErrors && this.HasChanges()
                               && !string.IsNullOrEmpty(Code)
                               && !string.IsNullOrEmpty(Name)
                               && !string.IsNullOrEmpty(Prefix)
                               && (GeneratedTaxAccountIsRequired || DeductibleTaxAccountIsRequired);

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

        public TaxCategoryDetailViewModel(
            IRepository<TaxCategoryGraphQLModel> taxCategoryService,
            IEventAggregator eventAggregator)
        {
            _taxCategoryService = taxCategoryService;
            _eventAggregator = eventAggregator;
        }

        #endregion

        #region Lifecycle

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
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
                UpsertResponseType<TaxCategoryGraphQLModel> result = await ExecuteSaveAsync();
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
                        ? new TaxCategoryCreateMessage { CreatedTaxCategory = result }
                        : new TaxCategoryUpdateMessage { UpdatedTaxCategory = result }
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

        public async Task<UpsertResponseType<TaxCategoryGraphQLModel>> ExecuteSaveAsync()
        {
            if (IsNewRecord)
            {
                string query = GetCreateQuery();
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                return await _taxCategoryService.CreateAsync<UpsertResponseType<TaxCategoryGraphQLModel>>(query, variables);
            }
            else
            {
                string query = GetUpdateQuery();
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = TaxCategoryId;
                return await _taxCategoryService.UpdateAsync<UpsertResponseType<TaxCategoryGraphQLModel>>(query, variables);
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
            var fields = FieldSpec<UpsertResponseType<TaxCategoryGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "taxCategory", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Code)
                    .Field(f => f.Name)
                    .Field(f => f.Prefix)
                    .Field(f => f.UsesPercentage)
                    .Field(e => e.GeneratedTaxAccountIsRequired)
                    .Field(e => e.GeneratedTaxRefundAccountIsRequired)
                    .Field(e => e.DeductibleTaxAccountIsRequired)
                    .Field(e => e.DeductibleTaxRefundAccountIsRequired))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateTaxCategoryInput!");
            var fragment = new GraphQLQueryFragment("createTaxCategory", [parameter], fields, "CreateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<TaxCategoryGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "taxCategory", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Code)
                    .Field(f => f.Name)
                    .Field(f => f.Prefix)
                    .Field(f => f.UsesPercentage)
                    .Field(e => e.GeneratedTaxAccountIsRequired)
                    .Field(e => e.GeneratedTaxRefundAccountIsRequired)
                    .Field(e => e.DeductibleTaxAccountIsRequired)
                    .Field(e => e.DeductibleTaxRefundAccountIsRequired))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateTaxCategoryInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateTaxCategory", parameters, fields, "UpdateResponse");
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
