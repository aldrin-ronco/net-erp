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

namespace NetErp.Books.IdentificationTypes.ViewModels
{
    public class IdentificationTypeDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<IdentificationTypeGraphQLModel> _identificationTypeService;
        private readonly IEventAggregator _eventAggregator;
        private readonly StringLengthCache _stringLengthCache;

        #endregion

        #region Properties

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
                    NotifyOfPropertyChange(nameof(IsReadOnlyCode));
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
                    this.TrackChange(nameof(Code));
                    NotifyOfPropertyChange(nameof(CanSave));
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
                    ValidateProperty(nameof(Name));
                    this.TrackChange(nameof(Name));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _hasVerificationDigit;
        public bool HasVerificationDigit
        {
            get => _hasVerificationDigit;
            set
            {
                if (_hasVerificationDigit != value)
                {
                    _hasVerificationDigit = value;
                    NotifyOfPropertyChange(nameof(HasVerificationDigit));
                    this.TrackChange(nameof(HasVerificationDigit));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _allowsLetters;
        public bool AllowsLetters
        {
            get => _allowsLetters;
            set
            {
                if (_allowsLetters != value)
                {
                    _allowsLetters = value;
                    NotifyOfPropertyChange(nameof(AllowsLetters));
                    this.TrackChange(nameof(AllowsLetters));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int _minimumDocumentLength = 7;
        public int MinimumDocumentLength
        {
            get => _minimumDocumentLength;
            set
            {
                if (_minimumDocumentLength != value)
                {
                    _minimumDocumentLength = value;
                    NotifyOfPropertyChange(nameof(MinimumDocumentLength));
                    ValidateProperty(nameof(MinimumDocumentLength));
                    this.TrackChange(nameof(MinimumDocumentLength));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool IsNewRecord => Id == 0;
        public bool IsReadOnlyCode => !IsNewRecord;

        public bool CanSave
        {
            get
            {
                if (HasErrors) return false;
                if (string.IsNullOrEmpty(Code) || Code.Length != 2) return false;
                if (string.IsNullOrEmpty(Name)) return false;
                if (MinimumDocumentLength < 5) return false;
                if (!this.HasChanges()) return false;
                return true;
            }
        }

        #endregion

        #region StringLength Properties

        public int CodeMaxLength => _stringLengthCache.GetMaxLength<IdentificationTypeGraphQLModel>(nameof(IdentificationTypeGraphQLModel.Code));
        public int NameMaxLength => _stringLengthCache.GetMaxLength<IdentificationTypeGraphQLModel>(nameof(IdentificationTypeGraphQLModel.Name));

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
                    if (string.IsNullOrEmpty(Code) || Code.Length != 2)
                        AddError(propertyName, "El código debe tener exactamente 2 dígitos");
                    break;
                case nameof(Name):
                    if (string.IsNullOrEmpty(Name))
                        AddError(propertyName, "El nombre no puede estar vacío");
                    break;
                case nameof(MinimumDocumentLength):
                    if (MinimumDocumentLength < 5)
                        AddError(propertyName, "La longitud mínima debe ser al menos 5");
                    break;
            }
        }

        private void ValidateProperties()
        {
            ValidateProperty(nameof(Code));
            ValidateProperty(nameof(Name));
            ValidateProperty(nameof(MinimumDocumentLength));
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

        #region Constructor

        public IdentificationTypeDetailViewModel(
            IRepository<IdentificationTypeGraphQLModel> identificationTypeService,
            IEventAggregator eventAggregator,
            StringLengthCache stringLengthCache)
        {
            _identificationTypeService = identificationTypeService;
            _eventAggregator = eventAggregator;
            _stringLengthCache = stringLengthCache;
        }

        #endregion

        #region SetForNew / SetForEdit

        public void SetForNew()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(HasVerificationDigit), HasVerificationDigit);
            this.SeedValue(nameof(AllowsLetters), AllowsLetters);
            this.SeedValue(nameof(MinimumDocumentLength), MinimumDocumentLength);
            this.AcceptChanges();
            ValidateProperties();
        }

        public void SetForEdit(IdentificationTypeGraphQLModel entity)
        {
            Id = entity.Id;
            Code = entity.Code;
            Name = entity.Name;
            HasVerificationDigit = entity.HasVerificationDigit;
            AllowsLetters = entity.AllowsLetters;
            MinimumDocumentLength = entity.MinimumDocumentLength;

            this.SeedValue(nameof(Code), Code);
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(HasVerificationDigit), HasVerificationDigit);
            this.SeedValue(nameof(AllowsLetters), AllowsLetters);
            this.SeedValue(nameof(MinimumDocumentLength), MinimumDocumentLength);
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
                UpsertResponseType<IdentificationTypeGraphQLModel> result = await ExecuteSaveAsync();

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
                        ? new IdentificationTypeCreateMessage { CreatedIdentificationType = result }
                        : new IdentificationTypeUpdateMessage { UpdatedIdentificationType = result },
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

        public async Task<UpsertResponseType<IdentificationTypeGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                string[] excludes = IsNewRecord ? [] : [nameof(Code)];

                if (IsNewRecord)
                {
                    var (_, query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput", excludeProperties: excludes);
                    return await _identificationTypeService.CreateAsync<UpsertResponseType<IdentificationTypeGraphQLModel>>(query, variables);
                }
                else
                {
                    var (_, query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData", excludeProperties: excludes);
                    variables.updateResponseId = Id;
                    return await _identificationTypeService.UpdateAsync<UpsertResponseType<IdentificationTypeGraphQLModel>>(query, variables);
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
            var fields = FieldSpec<UpsertResponseType<IdentificationTypeGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "identificationType", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.Code)
                    .Field(f => f.HasVerificationDigit)
                    .Field(f => f.AllowsLetters)
                    .Field(f => f.MinimumDocumentLength))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createIdentificationType",
                [new("input", "CreateIdentificationTypeInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<IdentificationTypeGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "identificationType", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.Code)
                    .Field(f => f.HasVerificationDigit)
                    .Field(f => f.AllowsLetters)
                    .Field(f => f.MinimumDocumentLength))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("updateIdentificationType",
                [new("data", "UpdateIdentificationTypeInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
