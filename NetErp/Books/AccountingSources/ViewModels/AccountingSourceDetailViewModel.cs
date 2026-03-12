using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Dictionaries;
using Extensions.Global;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
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

namespace NetErp.Books.AccountingSources.ViewModels
{
    public class AccountingSourceDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<AccountingSourceGraphQLModel> _accountingSourceService;
        private readonly IEventAggregator _eventAggregator;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;
        private readonly ProcessTypeCache _processTypeCache;

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

        public int Id { get; set; }

        private string _shortCode = string.Empty;
        public string ShortCode
        {
            get => _shortCode;
            set
            {
                if (_shortCode != value)
                {
                    _shortCode = value;
                    NotifyOfPropertyChange(nameof(ShortCode));
                    NotifyOfPropertyChange(nameof(Code));
                    ValidateProperty(nameof(ShortCode), value);
                    this.TrackChange(nameof(Code));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public string Code => $"_{(IsSystemSource ? "S" : "U")}_{ShortCode}";

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

        private bool _isSystemSource;
        public bool IsSystemSource
        {
            get => _isSystemSource;
            set
            {
                if (_isSystemSource != value)
                {
                    _isSystemSource = value;
                    NotifyOfPropertyChange(nameof(IsSystemSource));
                    NotifyOfPropertyChange(nameof(Code));
                    this.TrackChange(nameof(IsSystemSource));
                }
            }
        }

        private bool _isKardexTransaction;
        public bool IsKardexTransaction
        {
            get => _isKardexTransaction;
            set
            {
                if (_isKardexTransaction != value)
                {
                    _isKardexTransaction = value;
                    NotifyOfPropertyChange(nameof(IsKardexTransaction));
                    this.TrackChange(nameof(IsKardexTransaction));
                    this.TrackChange(nameof(KardexFlow));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool IsAnnulledWithAdditionalDocument => AnnulmentCharacter == 'A';

        public Dictionary<char, string> AnnulmentTypeDictionary => BooksDictionaries.AnnulmentTypeDictionary;

        private char _annulmentCharacter;
        public char AnnulmentCharacter
        {
            get => _annulmentCharacter;
            set
            {
                if (_annulmentCharacter != value)
                {
                    _annulmentCharacter = value;
                    NotifyOfPropertyChange(nameof(AnnulmentCharacter));
                    NotifyOfPropertyChange(nameof(IsAnnulledWithAdditionalDocument));
                    this.TrackChange(nameof(AnnulmentCharacter));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public Dictionary<char, string> KardexFlowDictionary => InventoriesDictionaries.KardexFlowDictionary;

        private char? _kardexFlow;
        public char? KardexFlow
        {
            get => _kardexFlow;
            set
            {
                if (_kardexFlow != value)
                {
                    _kardexFlow = value;
                    NotifyOfPropertyChange(nameof(KardexFlow));
                    this.TrackChange(nameof(KardexFlow));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int? _accountingAccountId;
        public int? AccountingAccountId
        {
            get => _accountingAccountId;
            set
            {
                if (_accountingAccountId != value)
                {
                    _accountingAccountId = value;
                    NotifyOfPropertyChange(nameof(AccountingAccountId));
                    this.TrackChange(nameof(AccountingAccountId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int? _processTypeId;
        public int? ProcessTypeId
        {
            get => _processTypeId;
            set
            {
                if (_processTypeId != value)
                {
                    _processTypeId = value;
                    NotifyOfPropertyChange(nameof(ProcessTypeId));
                    NotifyOfPropertyChange(nameof(SelectedProcessTypeName));
                    ValidateIntProperty(nameof(ProcessTypeId), value);
                    this.TrackChange(nameof(ProcessTypeId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public string SelectedProcessTypeName =>
            ProcessTypes.FirstOrDefault(p => p.Id == ProcessTypeId)?.Name ?? string.Empty;

        #endregion

        #region Collections

        private ObservableCollection<AccountingAccountGraphQLModel> _auxiliaryAccountingAccounts = [];
        public ObservableCollection<AccountingAccountGraphQLModel> AuxiliaryAccountingAccounts
        {
            get => _auxiliaryAccountingAccounts;
            set
            {
                if (_auxiliaryAccountingAccounts != value)
                {
                    _auxiliaryAccountingAccounts = value;
                    NotifyOfPropertyChange(nameof(AuxiliaryAccountingAccounts));
                }
            }
        }

        private ObservableCollection<ProcessTypeGraphQLModel> _processTypes = [];
        public ObservableCollection<ProcessTypeGraphQLModel> ProcessTypes
        {
            get => _processTypes;
            set
            {
                if (_processTypes != value)
                {
                    _processTypes = value;
                    NotifyOfPropertyChange(nameof(ProcessTypes));
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
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(ShortCode):
                    if (!IsNewRecord) break;
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "El código es requerido");
                    else if (value.Trim().Length != 3) AddError(propertyName, "El código debe tener 3 caracteres");
                    break;
                case nameof(Name):
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "El nombre es requerido");
                    break;
            }
        }

        private void ValidateIntProperty(string propertyName, int? value)
        {
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(ProcessTypeId):
                    if (!value.HasValue) AddError(propertyName, "Debe seleccionar un tipo de proceso");
                    break;
            }
        }

        private void ValidateProperties()
        {
            ValidateProperty(nameof(ShortCode), ShortCode);
            ValidateProperty(nameof(Name), Name);
            ValidateIntProperty(nameof(ProcessTypeId), ProcessTypeId);
        }

        #endregion

        #region Button States

        public bool CanSave => !HasErrors && this.HasChanges()
                               && !string.IsNullOrEmpty(Name)
                               && ShortCode.Trim().Length == 3
                               && ProcessTypeId.HasValue
                               && (!IsKardexTransaction || AccountingAccountId.HasValue);

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

        public AccountingSourceDetailViewModel(
            IRepository<AccountingSourceGraphQLModel> accountingSourceService,
            IEventAggregator eventAggregator,
            AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache,
            ProcessTypeCache processTypeCache)
        {
            _accountingSourceService = accountingSourceService;
            _eventAggregator = eventAggregator;
            _auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;
            _processTypeCache = processTypeCache;
        }

        #endregion

        #region Initialization

        public async Task InitializeAsync()
        {
            await Task.WhenAll(
                _auxiliaryAccountingAccountCache.EnsureLoadedAsync(),
                _processTypeCache.EnsureLoadedAsync()
            );

            AuxiliaryAccountingAccounts = [.. _auxiliaryAccountingAccountCache.Items];
            ProcessTypes = [.. _processTypeCache.Items];

            KardexFlow = InventoriesDictionaries.KardexFlowDictionary.FirstOrDefault().Key;
            AnnulmentCharacter = 'X';
        }

        #endregion

        #region Lifecycle

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperties();
            this.AcceptChanges();
            if (IsNewRecord)
            {
                this.SeedValue(nameof(IsKardexTransaction), false);
                this.SeedValue(nameof(IsSystemSource), false);
                this.SeedValue(nameof(AnnulmentCharacter), 'X');
            }
        }

        #endregion

        #region Load for Edit

        public async Task LoadDataForEditAsync(int id)
        {
            string query = _loadByIdQuery.Value;
            dynamic variables = new ExpandoObject();
            variables.singleItemResponseId = id;

            var entity = await _accountingSourceService.FindByIdAsync(query, variables);
            PopulateFromAccountingSource(entity);
        }

        private void PopulateFromAccountingSource(AccountingSourceGraphQLModel entity)
        {
            Name = entity.Name;
            Id = entity.Id;
            ProcessTypeId = entity.ProcessType.Id;
            ShortCode = entity.Code.Substring(entity.Code.Length - 3);
            KardexFlow = entity.KardexFlow;
            AnnulmentCharacter = entity.AnnulmentCharacter;
            IsKardexTransaction = entity.IsKardexTransaction;
            AccountingAccountId = entity.AccountingAccount?.Id;
            NotifyOfPropertyChange(nameof(IsNewRecord));
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
                UpsertResponseType<AccountingSourceGraphQLModel> result = await ExecuteSaveAsync();
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
                        ? new AccountingSourceCreateMessage { CreatedAccountingSource = result }
                        : new AccountingSourceUpdateMessage { UpdatedAccountingSource = result }
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

        public async Task<UpsertResponseType<AccountingSourceGraphQLModel>> ExecuteSaveAsync()
        {
            var excludes = !IsKardexTransaction ? new[] { nameof(KardexFlow), nameof(AccountingAccountId) } : null;

            if (IsNewRecord)
            {
                string query = _createQuery.Value;
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput", excludeProperties: excludes);
                return await _accountingSourceService.CreateAsync<UpsertResponseType<AccountingSourceGraphQLModel>>(query, variables);
            }
            else
            {
                string query = _updateQuery.Value;
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData", excludeProperties: excludes);
                variables.updateResponseId = Id;
                return await _accountingSourceService.UpdateAsync<UpsertResponseType<AccountingSourceGraphQLModel>>(query, variables);
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
            var fields = FieldSpec<UpsertResponseType<AccountingSourceGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingSource", nested: sq => sq
                   .Field(e => e.Id)
                   .Field(e => e.Code)
                   .Field(e => e.Name))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateAccountingSourceInput!");
            var fragment = new GraphQLQueryFragment("createAccountingSource", [parameter], fields, "CreateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        private static readonly Lazy<string> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<AccountingSourceGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingSource", nested: sq => sq
                   .Field(e => e.Id)
                   .Field(e => e.Code)
                   .Field(e => e.Name))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateAccountingSourceInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateAccountingSource", parameters, fields, "UpdateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        private static readonly Lazy<string> _loadByIdQuery = new(() =>
        {
            var fields = FieldSpec<AccountingSourceGraphQLModel>
             .Create()
                 .Field(e => e.Id)
                 .Field(e => e.AnnulmentCode)
                 .Field(e => e.Code)
                 .Field(e => e.Name)
                 .Field(e => e.IsSystemSource)
                 .Field(e => e.AnnulmentCharacter)
                 .Field(e => e.IsKardexTransaction)
                 .Field(e => e.KardexFlow)
                 .Select(e => e.AccountingAccount, acc => acc
                    .Field(c => c.Id)
                    .Field(c => c.Name))
                 .Select(e => e.ProcessType, cat => cat
                    .Field(c => c.Id)
                    .Field(c => c.Name)
                    .Select(c => c.MenuModule, dep => dep
                        .Field(d => d.Id)
                        .Field(d => d.Name)))
             .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("accountingSource", [parameter], fields, "SingleItemResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        });

        #endregion

    }
}
