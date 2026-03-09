using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using GraphQL.Client.Http;
using Models.Books;
using NetErp.Books.AccountingAccountGroups.DTO;
using NetErp.Global.CostCenters.DTO;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.WithholdingCertificateConfig.ViewModels
{
    public class WithholdingCertificateConfigDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<WithholdingCertificateConfigGraphQLModel> _withholdingCertificateConfigService;
        private readonly IRepository<AccountingAccountGroupGraphQLModel> _accountingAccountGroupService;
        private readonly AccountingAccountGroupCache _accountingAccountGroupCache;
        private readonly CostCenterCache _costCenterCache;

        public WithholdingCertificateConfigViewModel Context { get; set; }

        #endregion

        #region State

        public bool IsNewRecord => Entity == null || Entity.Id == 0;

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

        private WithholdingCertificateConfigGraphQLModel? _entity;
        public WithholdingCertificateConfigGraphQLModel? Entity
        {
            get => _entity;
            set
            {
                if (_entity != value)
                {
                    _entity = value;
                    NotifyOfPropertyChange(nameof(Entity));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        #endregion

        #region Form Properties

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

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    NotifyOfPropertyChange(nameof(Description));
                    ValidateProperty(nameof(Description), value);
                    this.TrackChange(nameof(Description));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int _costCenterId;
        public int CostCenterId
        {
            get => _costCenterId;
            set
            {
                if (_costCenterId != value)
                {
                    _costCenterId = value;
                    NotifyOfPropertyChange(nameof(CostCenterId));
                    this.TrackChange(nameof(CostCenterId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private ObservableCollection<CostCenterDTO> _costCenters = [];
        public ObservableCollection<CostCenterDTO> CostCenters
        {
            get => _costCenters;
            set
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        }

        private ObservableCollection<AccountingAccountGroupGraphQLModel> _accountingAccountGroups = [];
        public ObservableCollection<AccountingAccountGroupGraphQLModel> AccountingAccountGroups
        {
            get => _accountingAccountGroups;
            set
            {
                if (_accountingAccountGroups != value)
                {
                    _accountingAccountGroups = value;
                    NotifyOfPropertyChange(nameof(AccountingAccountGroups));
                }
            }
        }

        private int? _selectedAccountingAccountGroupId;
        public int? SelectedAccountingAccountGroupId
        {
            get => _selectedAccountingAccountGroupId;
            set
            {
                if (_selectedAccountingAccountGroupId != value)
                {
                    // Si hay cuentas marcadas, confirmar antes de cambiar
                    if (AccountingAccounts.Any(a => a.IsChecked == true))
                    {
                        var result = ThemedMessageBox.Show("Atención !",
                            "Al cambiar el grupo se perderá la selección de cuentas actual. ¿Desea continuar?",
                            MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.No)
                        {
                            // Forzar re-notificación para revertir el combo al valor anterior
                            NotifyOfPropertyChange(nameof(SelectedAccountingAccountGroupId));
                            return;
                        }
                    }

                    _selectedAccountingAccountGroupId = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingAccountGroupId));
                    NotifyOfPropertyChange(nameof(AccountingAccountGroupId));
                    this.TrackChange(nameof(AccountingAccountGroupId));
                    if (value is > 0) _ = LoadGroupAccountsAsync(value.Value, restoreSelection: false);
                    else
                    {
                        AccountingAccounts = [];
                        _isAllChecked = false;
                        NotifyOfPropertyChange(nameof(IsAllChecked));
                        NotifyOfPropertyChange(nameof(AccountingAccountIds));
                        NotifyOfPropertyChange(nameof(CanSave));
                    }
                }
            }
        }

        [ExpandoPath("AccountingAccountGroupId")]
        public int? AccountingAccountGroupId => SelectedAccountingAccountGroupId;

        [ExpandoPath("AccountingAccountIds")]
        public List<int> AccountingAccountIds => AccountingAccounts
            .Where(f => f.IsChecked == true)
            .Select(x => x.Id)
            .ToList();

        private ObservableCollection<AccountingAccountGroupDetailDTO> _accountingAccounts = [];
        public ObservableCollection<AccountingAccountGroupDetailDTO> AccountingAccounts
        {
            get => _accountingAccounts;
            set
            {
                if (_accountingAccounts != value)
                {
                    UnsubscribeAccountingAccountEvents();
                    _accountingAccounts = value;
                    NotifyOfPropertyChange(nameof(AccountingAccounts));
                    SubscribeAccountingAccountEvents();
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
                case nameof(Description):
                    if (string.IsNullOrEmpty(Description)) AddError(propertyName, "La descripción no puede estar vacía");
                    break;
            }
        }

        private void ValidateProperties()
        {
            ValidateProperty(nameof(Name), Name);
            ValidateProperty(nameof(Description), Description);
        }

        #endregion

        #region Focus

        private bool _nameIsFocused;
        public bool NameIsFocused
        {
            get => _nameIsFocused;
            set
            {
                if (_nameIsFocused != value)
                {
                    _nameIsFocused = value;
                    NotifyOfPropertyChange(nameof(NameIsFocused));
                }
            }
        }

        private void SetFocusOnName()
        {
            NameIsFocused = false;
            NameIsFocused = true;
        }

        #endregion

        #region Button States

        private bool _isAllChecked;
        public bool IsAllChecked
        {
            get => _isAllChecked;
            set
            {
                if (_isAllChecked != value)
                {
                    _isAllChecked = value;
                    NotifyOfPropertyChange(nameof(IsAllChecked));
                    foreach (var account in AccountingAccounts) account.IsChecked = value;
                }
            }
        }

        public bool CanSave => !HasErrors && this.HasChanges()
                               && !string.IsNullOrEmpty(Name)
                               && !string.IsNullOrEmpty(Description)
                               && CostCenterId != 0
                               && SelectedAccountingAccountGroupId is > 0
                               && AccountingAccounts.Any(x => x.IsChecked == true);

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

        private ICommand? _goBackCommand;
        public ICommand GoBackCommand
        {
            get
            {
                _goBackCommand ??= new AsyncCommand(GoBackAsync);
                return _goBackCommand;
            }
        }

        #endregion

        #region Constructor

        private int? _editId;

        public WithholdingCertificateConfigDetailViewModel(
            WithholdingCertificateConfigViewModel context,
            IRepository<WithholdingCertificateConfigGraphQLModel> withholdingCertificateConfigService,
            AccountingAccountGroupCache accountingAccountGroupCache,
            CostCenterCache costCenterCache,
            int? editId = null)
        {
            Context = context;
            _withholdingCertificateConfigService = withholdingCertificateConfigService;
            _accountingAccountGroupCache = accountingAccountGroupCache;
            _accountingAccountGroupService = IoC.Get<IRepository<AccountingAccountGroupGraphQLModel>>();
            _costCenterCache = costCenterCache;
            _editId = editId;
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                IsBusy = true;
                if (_editId.HasValue)
                    await LoadDataForEditAsync(_editId.Value);
                await InitializeAsync();
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.OnViewReady \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
            ValidateProperties();
            this.AcceptChanges();
            System.Windows.Application.Current.Dispatcher.BeginInvoke(
                new System.Action(SetFocusOnName),
                System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            UnsubscribeAccountingAccountEvents();
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region Initialize

        public async Task InitializeAsync()
        {
            await _costCenterCache.EnsureLoadedAsync();

            CostCenters = Context.AutoMapper.Map<ObservableCollection<CostCenterDTO>>(_costCenterCache.Items);
            CostCenters.Insert(0, new CostCenterDTO() { Id = 0, Name = "SELECCIONE CENTRO DE COSTO" });
            CostCenterId = Entity?.CostCenter?.Id ?? 0;

            await LoadAccountingAccountGroupsAsync();

            // Cargar grupo desde la entidad (el campo accountingAccountGroup viene directamente del API)
            if (Entity?.AccountingAccountGroup is { Id: > 0 } group)
            {
                _selectedAccountingAccountGroupId = group.Id;
                NotifyOfPropertyChange(nameof(SelectedAccountingAccountGroupId));
                await LoadGroupAccountsAsync(group.Id);
            }
        }

        private async Task LoadAccountingAccountGroupsAsync()
        {
            try
            {
                await _accountingAccountGroupCache.EnsureLoadedAsync();
                AccountingAccountGroups = new ObservableCollection<AccountingAccountGroupGraphQLModel>(_accountingAccountGroupCache.Items);
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private async Task LoadGroupAccountsAsync(int groupId, bool restoreSelection = true)
        {
            try
            {
                IsBusy = true;

                string query = _loadGroupByIdQuery.Value;
                dynamic variables = new ExpandoObject();
                variables.singleItemResponseId = groupId;

                var group = await _accountingAccountGroupService.FindByIdAsync(query, variables);

                ObservableCollection<AccountingAccountGroupDetailDTO> acgd =
                    Context.AutoMapper.Map<ObservableCollection<AccountingAccountGroupDetailDTO>>(group.Accounts);

                foreach (var account in acgd)
                {
                    account.Context = this;
                    account.IsChecked = restoreSelection && Entity?.AccountingAccounts?.Any(x => x.Id == account.Id) == true;
                }

                AccountingAccounts = [.. acgd];
                _isAllChecked = AccountingAccounts.Count > 0 && AccountingAccounts.All(a => a.IsChecked == true);
                NotifyOfPropertyChange(nameof(IsAllChecked));
                NotifyOfPropertyChange(nameof(AccountingAccountIds));
                NotifyOfPropertyChange(nameof(CanSave));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                UpsertResponseType<WithholdingCertificateConfigGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(
                        text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo",
                        title: $"{result.Message}!",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return;
                }

                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new WithholdingCertificateConfigCreateMessage { CreatedWithholdingCertificateConfig = result }
                        : new WithholdingCertificateConfigUpdateMessage { UpdatedWithholdingCertificateConfig = result }
                );

                await Context.ActivateMasterViewModelAsync();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content!.ToString()!)!;
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

        public async Task<UpsertResponseType<WithholdingCertificateConfigGraphQLModel>> ExecuteSaveAsync()
        {
            if (IsNewRecord)
            {
                string query = _createQuery.Value;
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                return await _withholdingCertificateConfigService.CreateAsync<UpsertResponseType<WithholdingCertificateConfigGraphQLModel>>(query, variables);
            }
            else
            {
                string query = _updateQuery.Value;
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = Entity!.Id;
                return await _withholdingCertificateConfigService.UpdateAsync<UpsertResponseType<WithholdingCertificateConfigGraphQLModel>>(query, variables);
            }
        }

        public async Task GoBackAsync()
        {
            await Context.ActivateMasterViewModelAsync();
        }

        #endregion

        #region Load for Edit

        public async Task<WithholdingCertificateConfigGraphQLModel> LoadDataForEditAsync(int id)
        {
            string query = _loadByIdQuery.Value;
            dynamic variables = new ExpandoObject();
            variables.singleItemResponseId = id;

            var certificate = await _withholdingCertificateConfigService.FindByIdAsync(query, variables);
            PopulateFromEntity(certificate);
            return certificate;
        }

        public void PopulateFromEntity(WithholdingCertificateConfigGraphQLModel entity)
        {
            Name = entity.Name;
            Description = entity.Description;
            CostCenterId = entity.CostCenter?.Id ?? 0;
            Entity = entity;
            this.AcceptChanges();
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<string> _loadGroupByIdQuery = new(() =>
        {
            var fields = FieldSpec<AccountingAccountGroupGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.Name)
                .Field(e => e.Key)
                .SelectList(e => e.Accounts, accounts => accounts
                    .Field(a => a.Id)
                    .Field(a => a.Code)
                    .Field(a => a.Name)
                    .Field(a => a.Nature))
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("accountingAccountGroup", [parameter], fields, "SingleItemResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        });

        private static readonly Lazy<string> _loadByIdQuery = new(() =>
        {
            var fields = FieldSpec<WithholdingCertificateConfigGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.Name)
                .Field(e => e.Description)
                .Select(e => e.AccountingAccountGroup, group => group
                    .Field(g => g.Id))
                .SelectList(e => e.AccountingAccounts, accounts => accounts
                    .Field(a => a.Id)
                    .Field(a => a.Name))
                .Select(e => e.CostCenter, cost => cost
                    .Field(c => c.Id))
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("withholdingCertificate", [parameter], fields, "SingleItemResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        });

        private static readonly Lazy<string> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<WithholdingCertificateConfigGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "withholdingCertificate", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Description)
                    .Select(e => e.CostCenter, cost => cost
                        .Field(c => c.Id)
                        .Field(c => c.Name)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateWithholdingCertificateInput!");
            var fragment = new GraphQLQueryFragment("createWithholdingCertificate", [parameter], fields, "CreateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        private static readonly Lazy<string> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<WithholdingCertificateConfigGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "withholdingCertificate", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Description)
                    .Select(e => e.CostCenter, cost => cost
                        .Field(c => c.Id)
                        .Field(c => c.Name)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateWithholdingCertificateInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateWithholdingCertificate", parameters, fields, "UpdateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        #endregion

        #region Accounting Account Event Subscriptions

        private void SubscribeAccountingAccountEvents()
        {
            foreach (var account in _accountingAccounts)
            {
                account.PropertyChanged += AccountingAccount_PropertyChanged;
            }
            _accountingAccounts.CollectionChanged += AccountingAccounts_CollectionChanged;
        }

        private void UnsubscribeAccountingAccountEvents()
        {
            if (_accountingAccounts == null) return;
            foreach (var account in _accountingAccounts)
            {
                account.PropertyChanged -= AccountingAccount_PropertyChanged;
            }
            _accountingAccounts.CollectionChanged -= AccountingAccounts_CollectionChanged;
        }

        private void AccountingAccount_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AccountingAccountGroupDetailDTO.IsChecked))
            {
                NotifyOfPropertyChange(nameof(AccountingAccountIds));
                this.TrackChange(nameof(AccountingAccountIds));
                NotifyOfPropertyChange(nameof(CanSave));
                _isAllChecked = AccountingAccounts.Count > 0 && AccountingAccounts.All(a => a.IsChecked == true);
                NotifyOfPropertyChange(nameof(IsAllChecked));
            }
        }

        private void AccountingAccounts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (AccountingAccountGroupDetailDTO item in e.NewItems)
                    item.PropertyChanged += AccountingAccount_PropertyChanged;
            }
            if (e.OldItems != null)
            {
                foreach (AccountingAccountGroupDetailDTO item in e.OldItems)
                    item.PropertyChanged -= AccountingAccount_PropertyChanged;
            }
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion
    }
}
