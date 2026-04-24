using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Treasury;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using INotificationService = NetErp.Helpers.Services.INotificationService;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;

namespace NetErp.Treasury.TreasuryConfig.ViewModels
{
    public class TreasuryConfigViewModel : Screen
    {
        private readonly IRepository<TreasuryConfigGraphQLModel> _treasuryConfigService;
        private readonly SubAccountingAccountCache _subAccountingAccountCache;
        private readonly IEventAggregator _eventAggregator;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly INotificationService _notificationService;
        private TreasuryConfigGraphQLModel? _loadedConfig;

        public TreasuryConfigViewModel(
            IRepository<TreasuryConfigGraphQLModel> treasuryConfigService,
            SubAccountingAccountCache subAccountingAccountCache,
            IEventAggregator eventAggregator,
            JoinableTaskFactory joinableTaskFactory,
            INotificationService notificationService)
        {
            _treasuryConfigService = treasuryConfigService;
            _subAccountingAccountCache = subAccountingAccountCache;
            _eventAggregator = eventAggregator;
            _joinableTaskFactory = joinableTaskFactory;
            _notificationService = notificationService;
        }

        #region Properties

        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                if (_id == value) return;
                _id = value;
                NotifyOfPropertyChange(nameof(Id));
                NotifyOfPropertyChange(nameof(IsNewRecord));
            }
        }

        public bool IsNewRecord => Id == 0;

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                NotifyOfPropertyChange(nameof(IsBusy));
                NotifyOfPropertyChange(nameof(CanEdit));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing == value) return;
                _isEditing = value;
                NotifyOfPropertyChange(nameof(IsEditing));
                NotifyOfPropertyChange(nameof(CanEdit));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        public bool CanEdit => !IsEditing && !IsBusy;

        private AccountingAccountGraphQLModel? _selectedCardGroupAccountingAccount;

        [ExpandoPath("cardGroupAccountingAccountId", SerializeAsId = true)]
        public AccountingAccountGraphQLModel? SelectedCardGroupAccountingAccount
        {
            get => _selectedCardGroupAccountingAccount;
            set
            {
                if (_selectedCardGroupAccountingAccount == value) return;
                _selectedCardGroupAccountingAccount = value;
                NotifyOfPropertyChange(nameof(SelectedCardGroupAccountingAccount));
                this.TrackChange(nameof(SelectedCardGroupAccountingAccount), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private AccountingAccountGraphQLModel? _selectedCashGroupAccountingAccount;

        [ExpandoPath("cashGroupAccountingAccountId", SerializeAsId = true)]
        public AccountingAccountGraphQLModel? SelectedCashGroupAccountingAccount
        {
            get => _selectedCashGroupAccountingAccount;
            set
            {
                if (_selectedCashGroupAccountingAccount == value) return;
                _selectedCashGroupAccountingAccount = value;
                NotifyOfPropertyChange(nameof(SelectedCashGroupAccountingAccount));
                this.TrackChange(nameof(SelectedCashGroupAccountingAccount), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private AccountingAccountGraphQLModel? _selectedCheckGroupAccountingAccount;

        [ExpandoPath("checkGroupAccountingAccountId", SerializeAsId = true)]
        public AccountingAccountGraphQLModel? SelectedCheckGroupAccountingAccount
        {
            get => _selectedCheckGroupAccountingAccount;
            set
            {
                if (_selectedCheckGroupAccountingAccount == value) return;
                _selectedCheckGroupAccountingAccount = value;
                NotifyOfPropertyChange(nameof(SelectedCheckGroupAccountingAccount));
                this.TrackChange(nameof(SelectedCheckGroupAccountingAccount), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private AccountingAccountGraphQLModel? _selectedCheckingGroupAccountingAccount;

        [ExpandoPath("checkingGroupAccountingAccountId", SerializeAsId = true)]
        public AccountingAccountGraphQLModel? SelectedCheckingGroupAccountingAccount
        {
            get => _selectedCheckingGroupAccountingAccount;
            set
            {
                if (_selectedCheckingGroupAccountingAccount == value) return;
                _selectedCheckingGroupAccountingAccount = value;
                NotifyOfPropertyChange(nameof(SelectedCheckingGroupAccountingAccount));
                this.TrackChange(nameof(SelectedCheckingGroupAccountingAccount), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private AccountingAccountGraphQLModel? _selectedSavingsGroupAccountingAccount;

        [ExpandoPath("savingsGroupAccountingAccountId", SerializeAsId = true)]
        public AccountingAccountGraphQLModel? SelectedSavingsGroupAccountingAccount
        {
            get => _selectedSavingsGroupAccountingAccount;
            set
            {
                if (_selectedSavingsGroupAccountingAccount == value) return;
                _selectedSavingsGroupAccountingAccount = value;
                NotifyOfPropertyChange(nameof(SelectedSavingsGroupAccountingAccount));
                this.TrackChange(nameof(SelectedSavingsGroupAccountingAccount), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        public ReadOnlyObservableCollection<AccountingAccountGraphQLModel> AccountingAccounts => _subAccountingAccountCache.Items;

        public bool CanSave => IsEditing && this.HasChanges() && !IsBusy;

        #endregion

        #region Commands

        private ICommand? _editCommand;
        public ICommand EditCommand
        {
            get
            {
                _editCommand ??= new DelegateCommand(() => IsEditing = true);
                return _editCommand;
            }
        }

        private ICommand? _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                _saveCommand ??= new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        private ICommand? _undoCommand;
        public ICommand UndoCommand
        {
            get
            {
                _undoCommand ??= new DelegateCommand(Undo);
                return _undoCommand;
            }
        }

        #endregion

        #region Lifecycle

        protected override async Task OnInitializedAsync(CancellationToken cancellationToken)
        {
            try
            {
                IsBusy = true;

                await _subAccountingAccountCache.EnsureLoadedAsync();

                await LoadTreasuryConfigAsync();
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(OnInitializedAsync)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }

            await base.OnInitializedAsync(cancellationToken);
        }

        #endregion

        #region Load

        private async Task LoadTreasuryConfigAsync()
        {
            var (_, query) = _loadQuery.Value;
            var config = await _treasuryConfigService.GetSingleItemAsync(query, new { });

            if (config is not null)
            {
                _loadedConfig = config;
                Id = config.Id;

                _selectedCardGroupAccountingAccount = AccountingAccounts.FirstOrDefault(x => x.Id == config.CardGroupAccountingAccount?.Id);
                NotifyOfPropertyChange(nameof(SelectedCardGroupAccountingAccount));

                _selectedCashGroupAccountingAccount = AccountingAccounts.FirstOrDefault(x => x.Id == config.CashGroupAccountingAccount?.Id);
                NotifyOfPropertyChange(nameof(SelectedCashGroupAccountingAccount));

                _selectedCheckGroupAccountingAccount = AccountingAccounts.FirstOrDefault(x => x.Id == config.CheckGroupAccountingAccount?.Id);
                NotifyOfPropertyChange(nameof(SelectedCheckGroupAccountingAccount));

                _selectedCheckingGroupAccountingAccount = AccountingAccounts.FirstOrDefault(x => x.Id == config.CheckingGroupAccountingAccount?.Id);
                NotifyOfPropertyChange(nameof(SelectedCheckingGroupAccountingAccount));

                _selectedSavingsGroupAccountingAccount = AccountingAccounts.FirstOrDefault(x => x.Id == config.SavingsGroupAccountingAccount?.Id);
                NotifyOfPropertyChange(nameof(SelectedSavingsGroupAccountingAccount));

                SeedCurrentValues();
            }
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(SelectedCardGroupAccountingAccount), SelectedCardGroupAccountingAccount);
            this.SeedValue(nameof(SelectedCashGroupAccountingAccount), SelectedCashGroupAccountingAccount);
            this.SeedValue(nameof(SelectedCheckGroupAccountingAccount), SelectedCheckGroupAccountingAccount);
            this.SeedValue(nameof(SelectedCheckingGroupAccountingAccount), SelectedCheckingGroupAccountingAccount);
            this.SeedValue(nameof(SelectedSavingsGroupAccountingAccount), SelectedSavingsGroupAccountingAccount);
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion

        #region Undo

        private void Undo()
        {
            _selectedCardGroupAccountingAccount = AccountingAccounts.FirstOrDefault(x => x.Id == _loadedConfig?.CardGroupAccountingAccount?.Id);
            NotifyOfPropertyChange(nameof(SelectedCardGroupAccountingAccount));

            _selectedCashGroupAccountingAccount = AccountingAccounts.FirstOrDefault(x => x.Id == _loadedConfig?.CashGroupAccountingAccount?.Id);
            NotifyOfPropertyChange(nameof(SelectedCashGroupAccountingAccount));

            _selectedCheckGroupAccountingAccount = AccountingAccounts.FirstOrDefault(x => x.Id == _loadedConfig?.CheckGroupAccountingAccount?.Id);
            NotifyOfPropertyChange(nameof(SelectedCheckGroupAccountingAccount));

            _selectedCheckingGroupAccountingAccount = AccountingAccounts.FirstOrDefault(x => x.Id == _loadedConfig?.CheckingGroupAccountingAccount?.Id);
            NotifyOfPropertyChange(nameof(SelectedCheckingGroupAccountingAccount));

            _selectedSavingsGroupAccountingAccount = AccountingAccounts.FirstOrDefault(x => x.Id == _loadedConfig?.SavingsGroupAccountingAccount?.Id);
            NotifyOfPropertyChange(nameof(SelectedSavingsGroupAccountingAccount));

            SeedCurrentValues();
            IsEditing = false;
        }

        #endregion

        #region Save

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;

                UpsertResponseType<TreasuryConfigGraphQLModel> result;

                if (IsNewRecord)
                {
                    var (_, query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    result = await _treasuryConfigService.CreateAsync<UpsertResponseType<TreasuryConfigGraphQLModel>>(query, variables);
                }
                else
                {
                    var (_, query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    result = await _treasuryConfigService.UpdateAsync<UpsertResponseType<TreasuryConfigGraphQLModel>>(query, variables);
                }

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

                if (result.Entity is not null)
                {
                    _loadedConfig = result.Entity;
                    Id = result.Entity.Id;
                }

                _notificationService.ShowSuccess(result.Message);
                SeedCurrentValues();
                IsEditing = false;
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(SaveAsync)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<TreasuryConfigGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Select(f => f.CardGroupAccountingAccount, nested: a => a.Field(a => a.Id))
                .Select(f => f.CashGroupAccountingAccount, nested: a => a.Field(a => a.Id))
                .Select(f => f.CheckGroupAccountingAccount, nested: a => a.Field(a => a.Id))
                .Select(f => f.CheckingGroupAccountingAccount, nested: a => a.Field(a => a.Id))
                .Select(f => f.SavingsGroupAccountingAccount, nested: a => a.Field(a => a.Id))
                .Build();

            var fragment = new GraphQLQueryFragment("treasuryConfig", [], fields, "SingleItemResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<TreasuryConfigGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "treasuryConfig", nested: sq => sq
                    .Field(f => f.Id)
                    .Select(f => f.CardGroupAccountingAccount, nested: a => a.Field(a => a.Id))
                    .Select(f => f.CashGroupAccountingAccount, nested: a => a.Field(a => a.Id))
                    .Select(f => f.CheckGroupAccountingAccount, nested: a => a.Field(a => a.Id))
                    .Select(f => f.CheckingGroupAccountingAccount, nested: a => a.Field(a => a.Id))
                    .Select(f => f.SavingsGroupAccountingAccount, nested: a => a.Field(a => a.Id)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createTreasuryConfig",
                [new("input", "CreateTreasuryConfigInput!")],
                fields, "CreateResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<TreasuryConfigGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "treasuryConfig", nested: sq => sq
                    .Field(f => f.Id)
                    .Select(f => f.CardGroupAccountingAccount, nested: a => a.Field(a => a.Id))
                    .Select(f => f.CashGroupAccountingAccount, nested: a => a.Field(a => a.Id))
                    .Select(f => f.CheckGroupAccountingAccount, nested: a => a.Field(a => a.Id))
                    .Select(f => f.CheckingGroupAccountingAccount, nested: a => a.Field(a => a.Id))
                    .Select(f => f.SavingsGroupAccountingAccount, nested: a => a.Field(a => a.Id)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("updateTreasuryConfig",
                [new("data", "UpdateTreasuryConfigInput!")],
                fields, "UpdateResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
