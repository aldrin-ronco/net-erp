using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Models.Books;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
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
        private readonly StringLengthCache _stringLengthCache;

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
                    NotifyOfPropertyChange(nameof(IsCodeReadOnly));
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
                    ValidateProperty(nameof(Code));
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
                    ValidateProperty(nameof(Name));
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
                    ValidateProperty(nameof(Prefix));
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

        #region StringLength Properties

        public int CodeMaxLength => _stringLengthCache.GetMaxLength<TaxCategoryGraphQLModel>(nameof(TaxCategoryGraphQLModel.Code));
        public int NameMaxLength => _stringLengthCache.GetMaxLength<TaxCategoryGraphQLModel>(nameof(TaxCategoryGraphQLModel.Name));
        public int PrefixMaxLength => _stringLengthCache.GetMaxLength<TaxCategoryGraphQLModel>(nameof(TaxCategoryGraphQLModel.Prefix));

        #endregion

        #region Validation (INotifyDataErrorInfo)

        private readonly Dictionary<string, List<string>> _errors = [];

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

        private void ValidateProperty(string propertyName)
        {
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
            ValidateProperty(nameof(Code));
            ValidateProperty(nameof(Name));
            ValidateProperty(nameof(Prefix));
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
            IEventAggregator eventAggregator,
            StringLengthCache stringLengthCache)
        {
            _taxCategoryService = taxCategoryService;
            _eventAggregator = eventAggregator;
            _stringLengthCache = stringLengthCache;
        }

        #endregion

        #region SetForNew / SetForEdit

        public void SetForNew()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(UsesPercentage), UsesPercentage);
            this.SeedValue(nameof(GeneratedTaxAccountIsRequired), GeneratedTaxAccountIsRequired);
            this.SeedValue(nameof(GeneratedTaxRefundAccountIsRequired), GeneratedTaxRefundAccountIsRequired);
            this.SeedValue(nameof(DeductibleTaxAccountIsRequired), DeductibleTaxAccountIsRequired);
            this.SeedValue(nameof(DeductibleTaxRefundAccountIsRequired), DeductibleTaxRefundAccountIsRequired);
            this.AcceptChanges();
            ValidateProperties();
        }

        public void SetForEdit(TaxCategoryGraphQLModel entity)
        {
            TaxCategoryId = entity.Id;
            Code = entity.Code;
            Name = entity.Name;
            Prefix = entity.Prefix;
            UsesPercentage = entity.UsesPercentage;
            GeneratedTaxAccountIsRequired = entity.GeneratedTaxAccountIsRequired;
            GeneratedTaxRefundAccountIsRequired = entity.GeneratedTaxRefundAccountIsRequired;
            DeductibleTaxAccountIsRequired = entity.DeductibleTaxAccountIsRequired;
            DeductibleTaxRefundAccountIsRequired = entity.DeductibleTaxRefundAccountIsRequired;

            this.SeedValue(nameof(Code), Code);
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(Prefix), Prefix);
            this.SeedValue(nameof(UsesPercentage), UsesPercentage);
            this.SeedValue(nameof(GeneratedTaxAccountIsRequired), GeneratedTaxAccountIsRequired);
            this.SeedValue(nameof(GeneratedTaxRefundAccountIsRequired), GeneratedTaxRefundAccountIsRequired);
            this.SeedValue(nameof(DeductibleTaxAccountIsRequired), DeductibleTaxAccountIsRequired);
            this.SeedValue(nameof(DeductibleTaxRefundAccountIsRequired), DeductibleTaxRefundAccountIsRequired);
            this.AcceptChanges();
            ValidateProperties();
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<TaxCategoryGraphQLModel> result = await ExecuteSaveAsync();

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
                        ? new TaxCategoryCreateMessage { CreatedTaxCategory = result }
                        : new TaxCategoryUpdateMessage { UpdatedTaxCategory = result },
                    CancellationToken.None);

                await TryCloseAsync(true);
            }
            catch (AsyncException ex)
            {
                await App.Current.Dispatcher.InvokeAsync(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al realizar operación.\r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                await App.Current.Dispatcher.InvokeAsync(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al realizar operación.\r\n{GetType().Name}.{nameof(SaveAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<UpsertResponseType<TaxCategoryGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                if (IsNewRecord)
                {
                    var (_, query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    return await _taxCategoryService.CreateAsync<UpsertResponseType<TaxCategoryGraphQLModel>>(query, variables);
                }
                else
                {
                    var (_, query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = TaxCategoryId;
                    return await _taxCategoryService.UpdateAsync<UpsertResponseType<TaxCategoryGraphQLModel>>(query, variables);
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

            var fragment = new GraphQLQueryFragment("createTaxCategory",
                [new("input", "CreateTaxCategoryInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
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

            var fragment = new GraphQLQueryFragment("updateTaxCategory",
                [new("data", "UpdateTaxCategoryInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
