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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.Tax.ViewModels
{
    public class TaxDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<TaxGraphQLModel> _taxService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IGraphQLClient _graphQLClient;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;
        private readonly TaxCategoryCache _taxCategoryCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly Microsoft.VisualStudio.Threading.JoinableTaskFactory _joinableTaskFactory;

        #endregion

        #region State

        public bool IsNewRecord => Id == 0;

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

        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    NotifyOfPropertyChange(nameof(Id));
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

        private decimal? _rate;
        public decimal? Rate
        {
            get => _rate;
            set
            {
                if (_rate != value)
                {
                    _rate = value;
                    NotifyOfPropertyChange(nameof(Rate));
                    ValidateDecimalProperty(nameof(Rate), value);
                    this.TrackChange(nameof(Rate));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    NotifyOfPropertyChange(nameof(IsActive));
                    this.TrackChange(nameof(IsActive));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _formula = string.Empty;
        public string Formula
        {
            get => _formula;
            set
            {
                if (_formula != value)
                {
                    _formula = value;
                    NotifyOfPropertyChange(nameof(Formula));
                    this.TrackChange(nameof(Formula));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _alternativeFormula = string.Empty;
        public string AlternativeFormula
        {
            get => _alternativeFormula;
            set
            {
                if (_alternativeFormula != value)
                {
                    _alternativeFormula = value;
                    NotifyOfPropertyChange(nameof(AlternativeFormula));
                    this.TrackChange(nameof(AlternativeFormula));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        #endregion

        #region TaxCategory Selection

        private ObservableCollection<TaxCategoryGraphQLModel> _taxCategories = [];
        public ObservableCollection<TaxCategoryGraphQLModel> TaxCategories
        {
            get => _taxCategories;
            set
            {
                if (_taxCategories != value)
                {
                    _taxCategories = value;
                    NotifyOfPropertyChange(nameof(TaxCategories));
                }
            }
        }

        private TaxCategoryGraphQLModel? _selectedTaxCategoryGraphQLModel;
        public TaxCategoryGraphQLModel? SelectedTaxCategoryGraphQLModel
        {
            get => _selectedTaxCategoryGraphQLModel;
            set
            {
                if (_selectedTaxCategoryGraphQLModel != value)
                {
                    _selectedTaxCategoryGraphQLModel = value;
                    TaxCategoryId = value?.Id;
                    NotifyOfPropertyChange(nameof(SelectedTaxCategoryGraphQLModel));
                    NotifyOfPropertyChange(nameof(IsVisibleGeneratedTaxSection));
                    NotifyOfPropertyChange(nameof(IsVisibleGeneratedTaxRefundAccount));
                    NotifyOfPropertyChange(nameof(IsVisibleDeductibleTaxSection));
                    NotifyOfPropertyChange(nameof(IsVisibleDeductibleTaxRefundAccount));
                    NotifyOfPropertyChange(nameof(IsVisiblePercentage));
                    if (!IsVisiblePercentage) Rate = 0;
                    if (!IsVisibleGeneratedTaxSection) GeneratedTaxAccountId = null;
                    if (!IsVisibleGeneratedTaxRefundAccount) GeneratedTaxRefundAccountId = null;
                    if (!IsVisibleDeductibleTaxSection) DeductibleTaxAccountId = null;
                    if (!IsVisibleDeductibleTaxRefundAccount) DeductibleTaxRefundAccountId = null;
                    ValidateIntProperty(nameof(SelectedTaxCategoryGraphQLModel), value?.Id);
                    ValidateIntProperty(nameof(GeneratedTaxAccountId), GeneratedTaxAccountId);
                    ValidateIntProperty(nameof(GeneratedTaxRefundAccountId), GeneratedTaxRefundAccountId);
                    ValidateIntProperty(nameof(DeductibleTaxAccountId), DeductibleTaxAccountId);
                    ValidateIntProperty(nameof(DeductibleTaxRefundAccountId), DeductibleTaxRefundAccountId);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int? _taxCategoryId;
        public int? TaxCategoryId
        {
            get => _taxCategoryId;
            set
            {
                if (_taxCategoryId != value)
                {
                    _taxCategoryId = value;
                    this.TrackChange(nameof(TaxCategoryId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        #endregion

        #region Accounting Account Selections

        private ObservableCollection<AccountingAccountGraphQLModel> _accountingAccountOperations = [];
        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccountOperations
        {
            get => _accountingAccountOperations;
            set
            {
                if (_accountingAccountOperations != value)
                {
                    _accountingAccountOperations = value;
                    NotifyOfPropertyChange(nameof(AccountingAccountOperations));
                }
            }
        }

        private ObservableCollection<AccountingAccountGraphQLModel> _accountingAccountDevolutions = [];
        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccountDevolutions
        {
            get => _accountingAccountDevolutions;
            set
            {
                if (_accountingAccountDevolutions != value)
                {
                    _accountingAccountDevolutions = value;
                    NotifyOfPropertyChange(nameof(AccountingAccountDevolutions));
                }
            }
        }

        private int? _generatedTaxAccountId;
        public int? GeneratedTaxAccountId
        {
            get => _generatedTaxAccountId;
            set
            {
                if (_generatedTaxAccountId != value)
                {
                    _generatedTaxAccountId = value;
                    NotifyOfPropertyChange(nameof(GeneratedTaxAccountId));
                    ValidateIntProperty(nameof(GeneratedTaxAccountId), value);
                    this.TrackChange(nameof(GeneratedTaxAccountId));
                    if (value.HasValue && !GeneratedTaxRefundAccountId.HasValue)
                        GeneratedTaxRefundAccountId = value;
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int? _generatedTaxRefundAccountId;
        public int? GeneratedTaxRefundAccountId
        {
            get => _generatedTaxRefundAccountId;
            set
            {
                if (_generatedTaxRefundAccountId != value)
                {
                    _generatedTaxRefundAccountId = value;
                    NotifyOfPropertyChange(nameof(GeneratedTaxRefundAccountId));
                    ValidateIntProperty(nameof(GeneratedTaxRefundAccountId), value);
                    this.TrackChange(nameof(GeneratedTaxRefundAccountId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int? _deductibleTaxAccountId;
        public int? DeductibleTaxAccountId
        {
            get => _deductibleTaxAccountId;
            set
            {
                if (_deductibleTaxAccountId != value)
                {
                    _deductibleTaxAccountId = value;
                    NotifyOfPropertyChange(nameof(DeductibleTaxAccountId));
                    ValidateIntProperty(nameof(DeductibleTaxAccountId), value);
                    this.TrackChange(nameof(DeductibleTaxAccountId));
                    if (value.HasValue && !DeductibleTaxRefundAccountId.HasValue)
                        DeductibleTaxRefundAccountId = value;
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int? _deductibleTaxRefundAccountId;
        public int? DeductibleTaxRefundAccountId
        {
            get => _deductibleTaxRefundAccountId;
            set
            {
                if (_deductibleTaxRefundAccountId != value)
                {
                    _deductibleTaxRefundAccountId = value;
                    NotifyOfPropertyChange(nameof(DeductibleTaxRefundAccountId));
                    ValidateIntProperty(nameof(DeductibleTaxRefundAccountId), value);
                    this.TrackChange(nameof(DeductibleTaxRefundAccountId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool IsVisibleGeneratedTaxSection =>
            SelectedTaxCategoryGraphQLModel != null
            && SelectedTaxCategoryGraphQLModel.GeneratedTaxAccountIsRequired;

        public bool IsVisibleGeneratedTaxRefundAccount =>
            SelectedTaxCategoryGraphQLModel != null
            && SelectedTaxCategoryGraphQLModel.GeneratedTaxAccountIsRequired
            && SelectedTaxCategoryGraphQLModel.GeneratedTaxRefundAccountIsRequired;

        public bool IsVisibleDeductibleTaxSection =>
            SelectedTaxCategoryGraphQLModel != null
            && SelectedTaxCategoryGraphQLModel.DeductibleTaxAccountIsRequired;

        public bool IsVisibleDeductibleTaxRefundAccount =>
            SelectedTaxCategoryGraphQLModel != null
            && SelectedTaxCategoryGraphQLModel.DeductibleTaxAccountIsRequired
            && SelectedTaxCategoryGraphQLModel.DeductibleTaxRefundAccountIsRequired;

        public bool IsVisiblePercentage =>
            SelectedTaxCategoryGraphQLModel != null
            && SelectedTaxCategoryGraphQLModel.UsesPercentage;

        #endregion

        #region StringLength Properties

        public int NameMaxLength => _stringLengthCache.GetMaxLength<TaxGraphQLModel>(nameof(TaxGraphQLModel.Name));

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

        private void ValidateProperty(string propertyName, string value)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty;
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(Name):
                    if (string.IsNullOrEmpty(Name)) AddError(propertyName, "El nombre no puede estar vacío");
                    break;
            }
        }

        private void ValidateDecimalProperty(string propertyName, decimal? value)
        {
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(Rate):
                    if (IsVisiblePercentage && (!value.HasValue || value == 0)) AddError(propertyName, "El porcentaje es requerido");
                    break;
            }
        }

        private void ValidateIntProperty(string propertyName, int? value)
        {
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(SelectedTaxCategoryGraphQLModel):
                    if (!value.HasValue) AddError(propertyName, "Debe seleccionar un tipo de impuesto");
                    break;
                case nameof(GeneratedTaxAccountId):
                    if (SelectedTaxCategoryGraphQLModel != null && SelectedTaxCategoryGraphQLModel.GeneratedTaxAccountIsRequired
                        && !value.HasValue)
                        AddError(propertyName, "Debe seleccionar una cuenta de operación");
                    break;
                case nameof(GeneratedTaxRefundAccountId):
                    if (SelectedTaxCategoryGraphQLModel != null && SelectedTaxCategoryGraphQLModel.GeneratedTaxRefundAccountIsRequired
                        && !value.HasValue)
                        AddError(propertyName, "Debe seleccionar una cuenta de devolución");
                    break;
                case nameof(DeductibleTaxAccountId):
                    if (SelectedTaxCategoryGraphQLModel != null && SelectedTaxCategoryGraphQLModel.DeductibleTaxAccountIsRequired
                        && !value.HasValue)
                        AddError(propertyName, "Debe seleccionar una cuenta de operación");
                    break;
                case nameof(DeductibleTaxRefundAccountId):
                    if (SelectedTaxCategoryGraphQLModel != null && SelectedTaxCategoryGraphQLModel.DeductibleTaxRefundAccountIsRequired
                        && !value.HasValue)
                        AddError(propertyName, "Debe seleccionar una cuenta de devolución");
                    break;
            }
        }

        private void ValidateProperties()
        {
            ValidateProperty(nameof(Name), Name);
            ValidateDecimalProperty(nameof(Rate), Rate);
            ValidateIntProperty(nameof(SelectedTaxCategoryGraphQLModel), SelectedTaxCategoryGraphQLModel?.Id);
            ValidateIntProperty(nameof(GeneratedTaxAccountId), GeneratedTaxAccountId);
            ValidateIntProperty(nameof(GeneratedTaxRefundAccountId), GeneratedTaxRefundAccountId);
            ValidateIntProperty(nameof(DeductibleTaxAccountId), DeductibleTaxAccountId);
            ValidateIntProperty(nameof(DeductibleTaxRefundAccountId), DeductibleTaxRefundAccountId);
        }

        #endregion

        #region Button States

        public bool CanSave => !HasErrors && this.HasChanges()
                               && !string.IsNullOrEmpty(Name)
                               && (!IsVisiblePercentage || (Rate.HasValue && Rate > 0))
                               && TaxCategoryId.HasValue;

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

        public TaxDetailViewModel(
            IRepository<TaxGraphQLModel> taxService,
            IEventAggregator eventAggregator,
            AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache,
            TaxCategoryCache taxCategoryCache,
            StringLengthCache stringLengthCache,
            Microsoft.VisualStudio.Threading.JoinableTaskFactory joinableTaskFactory,
            IGraphQLClient graphQLClient)
        {
            _taxService = taxService;
            _eventAggregator = eventAggregator;
            _auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;
            _taxCategoryCache = taxCategoryCache;
            _stringLengthCache = stringLengthCache;
            _joinableTaskFactory = joinableTaskFactory;
            _graphQLClient = graphQLClient;
        }

        #endregion

        #region Initialization

        public async Task InitializeAsync()
        {
            await CacheBatchLoader.LoadAsync(
                _graphQLClient, default,
                _auxiliaryAccountingAccountCache, _taxCategoryCache);

            AccountingAccountOperations = [.. _auxiliaryAccountingAccountCache.Items];
            AccountingAccountDevolutions = [.. _auxiliaryAccountingAccountCache.Items];
            TaxCategories = [.. _taxCategoryCache.Items];
        }

        #endregion

        #region SetForNew / SetForEdit

        public void SetForNew()
        {
            IsActive = true;
            Rate = 0;
            Formula = "Formula por definir";
            AlternativeFormula = "AlternativeFormula por definir";

            this.ClearSeeds();
            this.SeedValue(nameof(IsActive), IsActive);
            this.SeedValue(nameof(Rate), Rate);
            this.SeedValue(nameof(Formula), Formula);
            this.SeedValue(nameof(AlternativeFormula), AlternativeFormula);
            this.AcceptChanges();
            ValidateProperties();
        }

        public void SetForEdit()
        {
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(Rate), Rate);
            this.SeedValue(nameof(IsActive), IsActive);
            this.SeedValue(nameof(Formula), Formula);
            this.SeedValue(nameof(AlternativeFormula), AlternativeFormula);
            this.SeedValue(nameof(TaxCategoryId), TaxCategoryId);
            this.SeedValue(nameof(GeneratedTaxAccountId), GeneratedTaxAccountId);
            this.SeedValue(nameof(GeneratedTaxRefundAccountId), GeneratedTaxRefundAccountId);
            this.SeedValue(nameof(DeductibleTaxAccountId), DeductibleTaxAccountId);
            this.SeedValue(nameof(DeductibleTaxRefundAccountId), DeductibleTaxRefundAccountId);
            this.AcceptChanges();
            ValidateProperties();
        }

        #endregion

        #region Load for Edit

        public async Task LoadDataForEditAsync(int id)
        {
            var (fragment, query) = _loadByIdQuery.Value;
            var variables = new GraphQLVariables()
                .For(fragment, "id", id)
                .Build();

            var tax = await _taxService.FindByIdAsync(query, variables);
            PopulateFromTax(tax);
        }

        private void PopulateFromTax(TaxGraphQLModel tax)
        {
            Name = tax.Name;
            Rate = tax.Rate;
            Formula = tax.Formula;
            AlternativeFormula = tax.AlternativeFormula;
            IsActive = tax.IsActive;
            SelectedTaxCategoryGraphQLModel = TaxCategories.FirstOrDefault(f => f.Id == tax.TaxCategory.Id);
            GeneratedTaxAccountId = tax.GeneratedTaxAccount?.Id;
            GeneratedTaxRefundAccountId = tax.GeneratedTaxRefundAccount?.Id;
            DeductibleTaxAccountId = tax.DeductibleTaxAccount?.Id;
            DeductibleTaxRefundAccountId = tax.DeductibleTaxRefundAccount?.Id;
            Id = tax.Id;
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<TaxGraphQLModel> result = await ExecuteSaveAsync();

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
                        ? new TaxCreateMessage { CreatedTax = result }
                        : new TaxUpdateMessage { UpdatedTax = result },
                    CancellationToken.None);

                await TryCloseAsync(true);
            }
            catch (AsyncException ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al realizar operación.\r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al realizar operación.\r\n{GetType().Name}.{nameof(SaveAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<UpsertResponseType<TaxGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                if (IsNewRecord)
                {
                    var (_, query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    return await _taxService.CreateAsync<UpsertResponseType<TaxGraphQLModel>>(query, variables);
                }
                else
                {
                    var (_, query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;
                    return await _taxService.UpdateAsync<UpsertResponseType<TaxGraphQLModel>>(query, variables);
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
            var fields = FieldSpec<UpsertResponseType<TaxGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "tax", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Name))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createTax",
                [new("input", "CreateTaxInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<TaxGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "tax", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Name))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("updateTax",
                [new("data", "UpdateTaxInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadByIdQuery = new(() =>
        {
            var fields = FieldSpec<TaxGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.Name)
                .Field(e => e.Rate)
                .Field(e => e.IsActive)
                .Field(e => e.Formula)
                .Field(e => e.AlternativeFormula)
                .Select(e => e.TaxCategory, cat => cat
                    .Field(c => c.Id)
                    .Field(c => c.Name)
                    .Field(c => c.GeneratedTaxAccountIsRequired)
                    .Field(c => c.GeneratedTaxRefundAccountIsRequired)
                    .Field(c => c.DeductibleTaxAccountIsRequired)
                    .Field(c => c.DeductibleTaxRefundAccountIsRequired))
                .Select(e => e.GeneratedTaxAccount, acc => acc
                    .Field(a => a.Id)
                    .Field(a => a.Name))
                .Select(e => e.GeneratedTaxRefundAccount, acc => acc
                    .Field(a => a.Id)
                    .Field(a => a.Name))
                .Select(e => e.DeductibleTaxAccount, acc => acc
                    .Field(a => a.Id)
                    .Field(a => a.Name))
                .Select(e => e.DeductibleTaxRefundAccount, acc => acc
                    .Field(a => a.Id)
                    .Field(a => a.Name))
                .Build();

            var fragment = new GraphQLQueryFragment("tax",
                [new("id", "ID!")],
                fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion
    }
}
