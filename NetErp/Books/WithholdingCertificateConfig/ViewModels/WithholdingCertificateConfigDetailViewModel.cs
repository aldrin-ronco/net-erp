using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Models.Books;
using NetErp.Books.AccountingAccountGroups.DTO;
using NetErp.Global.CostCenters.DTO;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.VisualStudio.Threading;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.WithholdingCertificateConfig.ViewModels
{
    public class WithholdingCertificateConfigDetailViewModel(
        WithholdingCertificateConfigViewModel context,
        IRepository<WithholdingCertificateConfigGraphQLModel> withholdingCertificateConfigService,
        IRepository<AccountingAccountGroupGraphQLModel> accountingAccountGroupService,
        AccountingAccountGroupCache accountingAccountGroupCache,
        CostCenterCache costCenterCache,
        StringLengthCache stringLengthCache,
        JoinableTaskFactory joinableTaskFactory) : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<WithholdingCertificateConfigGraphQLModel> _withholdingCertificateConfigService = withholdingCertificateConfigService;
        private readonly IRepository<AccountingAccountGroupGraphQLModel> _accountingAccountGroupService = accountingAccountGroupService;
        private readonly AccountingAccountGroupCache _accountingAccountGroupCache = accountingAccountGroupCache;
        private readonly CostCenterCache _costCenterCache = costCenterCache;
        private readonly StringLengthCache _stringLengthCache = stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory = joinableTaskFactory;

        public WithholdingCertificateConfigViewModel Context { get; set; } = context;

        #endregion

        #region State

        public bool IsNewRecord => Entity == null || Entity.Id == 0;

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

        public WithholdingCertificateConfigGraphQLModel? Entity
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Entity));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

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
        } = 600;

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
        } = 500;

        #endregion

        #region Form Properties

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

        public int? CostCenterId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CostCenterId));
                    this.TrackChange(nameof(CostCenterId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public ObservableCollection<CostCenterDTO> CostCenters
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        } = [];

        public ObservableCollection<AccountingAccountGroupGraphQLModel> AccountingAccountGroups
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountingAccountGroups));
                }
            }
        } = [];

        public int? SelectedAccountingAccountGroupId
        {
            get;
            set
            {
                if (field != value)
                {
                    if (AccountingAccounts.Any(a => a.IsChecked == true))
                    {
                        var result = ThemedMessageBox.Show("Atención!",
                            "Al cambiar el grupo se perderá la selección de cuentas actual. ¿Desea continuar?",
                            MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.No)
                        {
                            NotifyOfPropertyChange(nameof(SelectedAccountingAccountGroupId));
                            return;
                        }
                    }

                    field = value;
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
        public List<int> AccountingAccountIds => [.. AccountingAccounts
            .Where(f => f.IsChecked == true)
            .Select(x => x.Id)];

        public ObservableCollection<AccountingAccountGroupDetailDTO> AccountingAccounts
        {
            get;
            set
            {
                if (field != value)
                {
                    UnsubscribeAccountingAccountEvents();
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountingAccounts));
                    SubscribeAccountingAccountEvents();
                }
            }
        } = [];

        #endregion

        #region StringLength Properties

        public int NameMaxLength => _stringLengthCache.GetMaxLength<WithholdingCertificateConfigGraphQLModel>(nameof(WithholdingCertificateConfigGraphQLModel.Name));
        public int DescriptionMaxLength => _stringLengthCache.GetMaxLength<WithholdingCertificateConfigGraphQLModel>(nameof(WithholdingCertificateConfigGraphQLModel.Description));

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
                RaiseErrorsChanged(propertyName);
            }
            _errors.Remove(propertyName);
        }

        private void ValidateProperty(string propertyName, string? value)
        {
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(Name):
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "El nombre no puede estar vacío");
                    break;
                case nameof(Description):
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "La descripción no puede estar vacía");
                    break;
            }
        }

        private void ValidateProperties()
        {
            ValidateProperty(nameof(Name), Name);
            ValidateProperty(nameof(Description), Description);
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
                               && CostCenterId is > 0
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

        private ICommand? _closeCommand;
        public ICommand CloseCommand
        {
            get
            {
                _closeCommand ??= new AsyncCommand(CloseAsync);
                return _closeCommand;
            }
        }

        #endregion
        #region Constructor

        #endregion

        #region Lifecycle

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                UnsubscribeAccountingAccountEvents();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region SetForNew / SetForEdit

        public void SetForNew()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(Description), Description);
            this.AcceptChanges();
            ValidateProperties();
        }

        public void SetForEdit()
        {
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(Description), Description);
            this.SeedValue(nameof(CostCenterId), CostCenterId);
            this.SeedValue(nameof(AccountingAccountGroupId), AccountingAccountGroupId);
            this.SeedValue(nameof(AccountingAccountIds), AccountingAccountIds);
            this.AcceptChanges();
            ValidateProperties();
        }

        #endregion

        #region Initialize

        public async Task InitializeAsync()
        {
            await _costCenterCache.EnsureLoadedAsync();

            CostCenters = Context.AutoMapper.Map<ObservableCollection<CostCenterDTO>>(_costCenterCache.Items);
            CostCenterId = Entity?.CostCenter?.Id;

            await LoadAccountingAccountGroupsAsync();

            if (Entity?.AccountingAccountGroup is { Id: > 0 } group)
            {
                SelectedAccountingAccountGroupId = group.Id;
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
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al cargar grupos de cuentas.\r\n{GetType().Name}.{nameof(LoadAccountingAccountGroupsAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
        }

        private async Task LoadGroupAccountsAsync(int groupId, bool restoreSelection = true)
        {
            try
            {
                IsBusy = true;

                var (fragment, query) = _loadGroupByIdQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "id", groupId)
                    .Build();

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
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al cargar cuentas del grupo.\r\n{GetType().Name}.{nameof(LoadGroupAccountsAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Save / Close

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<WithholdingCertificateConfigGraphQLModel> result = await ExecuteSaveAsync();

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

                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new WithholdingCertificateConfigCreateMessage { CreatedWithholdingCertificateConfig = result }
                        : new WithholdingCertificateConfigUpdateMessage { UpdatedWithholdingCertificateConfig = result },
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

        public async Task<UpsertResponseType<WithholdingCertificateConfigGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                if (IsNewRecord)
                {
                    var (_, query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    return await _withholdingCertificateConfigService.CreateAsync<UpsertResponseType<WithholdingCertificateConfigGraphQLModel>>(query, variables);
                }
                else
                {
                    var (_, query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Entity!.Id;
                    return await _withholdingCertificateConfigService.UpdateAsync<UpsertResponseType<WithholdingCertificateConfigGraphQLModel>>(query, variables);
                }
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public async Task CloseAsync()
        {
            await TryCloseAsync(false);
        }

        #endregion

        #region Load for Edit

        public async Task<WithholdingCertificateConfigGraphQLModel> LoadDataForEditAsync(int id)
        {
            try
            {
                var (fragment, query) = _loadByIdQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "id", id)
                    .Build();

                var certificate = await _withholdingCertificateConfigService.FindByIdAsync(query, variables);
                PopulateFromEntity(certificate);
                return certificate;
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public void PopulateFromEntity(WithholdingCertificateConfigGraphQLModel entity)
        {
            Name = entity.Name;
            Description = entity.Description;
            CostCenterId = entity.CostCenter?.Id;
            Entity = entity;
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadGroupByIdQuery = new(() =>
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

            var fragment = new GraphQLQueryFragment("accountingAccountGroup",
                [new("id", "ID!")],
                fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadByIdQuery = new(() =>
        {
            var fields = FieldSpec<WithholdingCertificateConfigGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.Name)
                .Field(e => e.Description)
                .Select(e => e.AccountingAccountGroup, group => group
                    .Field(g => g!.Id))
                .SelectList(e => e.AccountingAccounts, accounts => accounts
                    .Field(a => a!.Id)
                    .Field(a => a!.Name))
                .Select(e => e.CostCenter, cost => cost
                    .Field(c => c!.Id))
                .Build();

            var fragment = new GraphQLQueryFragment("withholdingCertificate",
                [new("id", "ID!")],
                fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<WithholdingCertificateConfigGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "withholdingCertificate", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Description)
                    .Select(e => e.CostCenter, cost => cost
                        .Field(c => c!.Id)
                        .Field(c => c!.Name)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createWithholdingCertificate",
                [new("input", "CreateWithholdingCertificateInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<WithholdingCertificateConfigGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "withholdingCertificate", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Description)
                    .Select(e => e.CostCenter, cost => cost
                        .Field(c => c!.Id)
                        .Field(c => c!.Name)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("updateWithholdingCertificate",
                [new("data", "UpdateWithholdingCertificateInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion

        #region Accounting Account Event Subscriptions

        private void SubscribeAccountingAccountEvents()
        {
            foreach (var account in AccountingAccounts)
            {
                account.PropertyChanged += AccountingAccount_PropertyChanged;
            }
            AccountingAccounts.CollectionChanged += AccountingAccounts_CollectionChanged;
        }

        private void UnsubscribeAccountingAccountEvents()
        {
            if (AccountingAccounts == null) return;
            foreach (var account in AccountingAccounts)
            {
                account.PropertyChanged -= AccountingAccount_PropertyChanged;
            }
            AccountingAccounts.CollectionChanged -= AccountingAccounts_CollectionChanged;
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
