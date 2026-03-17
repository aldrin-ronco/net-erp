using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using NetErp.Books.AccountingAccountGroups.DTO;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.AccountingAccountGroups.ViewModels
{
    public class AccountingAccountGroupViewModel : Screen,
        IHandle<AccountingAccountGroupUpdateMessage>
    {
        #region Dependencies

        private readonly IMapper _autoMapper;
        private readonly IEventAggregator _eventAggregator;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly IRepository<AccountingAccountGroupGraphQLModel> _accountingAccountGroupService;
        private readonly IRepository<AccountingAccountGraphQLModel> _accountingAccountService;
        private readonly JoinableTaskFactory _joinableTaskFactory;

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

        private ObservableCollection<AccountingAccountGroupGraphQLModel> _groups = [];
        public ObservableCollection<AccountingAccountGroupGraphQLModel> Groups
        {
            get => _groups;
            set
            {
                if (_groups != value)
                {
                    _groups = value;
                    NotifyOfPropertyChange(nameof(Groups));
                    NotifyOfPropertyChange(nameof(ShowAllControls));
                }
            }
        }

        private AccountingAccountGroupGraphQLModel _selectedGroup;
        public AccountingAccountGroupGraphQLModel SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                if (_selectedGroup != value)
                {
                    _selectedGroup = value;
                    NotifyOfPropertyChange(nameof(SelectedGroup));
                    SelectedAccountingAccountCode = string.Empty;
                    FilterSearch = string.Empty;
                    _isAllChecked = false;
                    NotifyOfPropertyChange(nameof(IsAllChecked));
                    if (_selectedGroup != null)
                    {
                        SelectedGroupAccountingAccounts = _autoMapper.Map<ObservableCollection<AccountingAccountGroupDTO>>(_selectedGroup.Accounts);
                        foreach (var account in SelectedGroupAccountingAccounts) account.Context = this;
                        _selectedGroupAccountingAccountsShadow = [.. SelectedGroupAccountingAccounts];
                        GroupFilters = _autoMapper.Map<ObservableCollection<AccountingAccountGroupFilterDTO>>(_selectedGroup.Filters);
                        UpdateFilteredAccounts();
                    }
                    else
                    {
                        GroupFilters = [];
                        UpdateFilteredAccounts();
                    }
                    NotifyOfPropertyChange(nameof(CanOpenFilterDialog));
                }
            }
        }

        private ObservableCollection<AccountingAccountGroupDTO> _selectedGroupAccountingAccounts = [];
        public ObservableCollection<AccountingAccountGroupDTO> SelectedGroupAccountingAccounts
        {
            get => _selectedGroupAccountingAccounts;
            set
            {
                if (_selectedGroupAccountingAccounts != value)
                {
                    _selectedGroupAccountingAccounts = value;
                    NotifyOfPropertyChange(nameof(SelectedGroupAccountingAccounts));
                }
            }
        }

        private ObservableCollection<AccountingAccountGroupDTO> _selectedGroupAccountingAccountsShadow = [];

        // All accounts loaded from API (minCodeLength >= 4)
        private ObservableCollection<AccountingAccountGroupDTO> _accountingAccounts = [];
        public ObservableCollection<AccountingAccountGroupDTO> AccountingAccounts
        {
            get => _accountingAccounts;
            set
            {
                if (_accountingAccounts != value)
                {
                    _accountingAccounts = value;
                    NotifyOfPropertyChange(nameof(AccountingAccounts));
                }
            }
        }

        // Accounts filtered by group filter prefixes (bound to the account selector combo)
        private ObservableCollection<AccountingAccountGroupDTO> _filteredAccountingAccounts = [];
        public ObservableCollection<AccountingAccountGroupDTO> FilteredAccountingAccounts
        {
            get => _filteredAccountingAccounts;
            set
            {
                if (_filteredAccountingAccounts != value)
                {
                    _filteredAccountingAccounts = value;
                    NotifyOfPropertyChange(nameof(FilteredAccountingAccounts));
                }
            }
        }

        // Group filters (prefix accounts)
        private ObservableCollection<AccountingAccountGroupFilterDTO> _groupFilters = [];
        public ObservableCollection<AccountingAccountGroupFilterDTO> GroupFilters
        {
            get => _groupFilters;
            set
            {
                if (_groupFilters != value)
                {
                    _groupFilters = value;
                    NotifyOfPropertyChange(nameof(GroupFilters));
                    NotifyOfPropertyChange(nameof(HasGroupFilters));
                }
            }
        }

        public bool HasGroupFilters => GroupFilters != null && GroupFilters.Count > 0;

        private string _filterSearch = string.Empty;
        public string FilterSearch
        {
            get => _filterSearch;
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) ApplyFilter();
                }
            }
        }

        private string _selectedAccountingAccountCode = string.Empty;
        public string SelectedAccountingAccountCode
        {
            get => _selectedAccountingAccountCode;
            set
            {
                if (_selectedAccountingAccountCode != value)
                {
                    _selectedAccountingAccountCode = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingAccountCode));
                    NotifyOfPropertyChange(nameof(CanAddAccountingAccount));
                }
            }
        }

        public bool CanAddAccountingAccount => !string.IsNullOrEmpty(SelectedAccountingAccountCode);

        public bool HasPendingChanges
        {
            get
            {
                if (SelectedGroup is null) return false;
                var originalIds = SelectedGroup.Accounts.Select(a => a.Id).OrderBy(id => id).ToList();
                var currentIds = _selectedGroupAccountingAccountsShadow.Select(a => a.Id).OrderBy(id => id).ToList();
                return !originalIds.SequenceEqual(currentIds);
            }
        }

        public bool AccountingAccountGroupComboBoxIsEnabled => !HasPendingChanges;

        public bool CanSave => HasPendingChanges;

        public bool ShowAllControls => Groups != null && Groups.Count > 0;

        public bool CanDeleteAccountingAccount => SelectedGroupAccountingAccounts.Any(x => x.IsChecked);

        public bool CanUndo => HasPendingChanges;

        public bool CanOpenFilterDialog => !HasPendingChanges && SelectedGroup != null;

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
                    foreach (var account in SelectedGroupAccountingAccounts) account.IsChecked = value;
                }
            }
        }

        #endregion

        #region Helpers

        private void NotifyPendingChangesState()
        {
            NotifyOfPropertyChange(nameof(HasPendingChanges));
            NotifyOfPropertyChange(nameof(AccountingAccountGroupComboBoxIsEnabled));
            NotifyOfPropertyChange(nameof(CanSave));
            NotifyOfPropertyChange(nameof(CanUndo));
            NotifyOfPropertyChange(nameof(CanOpenFilterDialog));
        }

        #endregion

        #region Commands

        private ICommand? _addAccountingAccountCommand;
        public ICommand AddAccountingAccountCommand
        {
            get
            {
                _addAccountingAccountCommand ??= new DelegateCommand(AddAccountingAccounts);
                return _addAccountingAccountCommand;
            }
        }

        private ICommand? _deleteAccountingAccountCommand;
        public ICommand DeleteAccountingAccountCommand
        {
            get
            {
                _deleteAccountingAccountCommand ??= new DelegateCommand(DeleteAccountingAccounts);
                return _deleteAccountingAccountCommand;
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
                _undoCommand ??= new DelegateCommand(UndoChanges);
                return _undoCommand;
            }
        }

        private ICommand? _openFilterDialogCommand;
        public ICommand OpenFilterDialogCommand
        {
            get
            {
                _openFilterDialogCommand ??= new AsyncCommand(OpenFilterDialogAsync);
                return _openFilterDialogCommand;
            }
        }

        #endregion

        #region Constructor

        public AccountingAccountGroupViewModel(
            IMapper mapper,
            IEventAggregator eventAggregator,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            IRepository<AccountingAccountGroupGraphQLModel> accountingAccountGroupService,
            IRepository<AccountingAccountGraphQLModel> accountingAccountService,
            JoinableTaskFactory joinableTaskFactory)
        {
            _autoMapper = mapper;
            _eventAggregator = eventAggregator;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _accountingAccountGroupService = accountingAccountGroupService;
            _accountingAccountService = accountingAccountService;
            _joinableTaskFactory = joinableTaskFactory;
            _eventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            await InitializeAsync();
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                _eventAggregator.Unsubscribe(this);
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region Message Handlers

        public Task HandleAsync(AccountingAccountGroupUpdateMessage message, CancellationToken cancellationToken)
        {
            if (SelectedGroup is null) return Task.CompletedTask;
            var result = message.UpsertAccountingAccountGroup;
            SelectedGroup.Accounts = [.. _selectedGroupAccountingAccountsShadow.Select(a => new AccountingAccountGroupDetailGraphQLModel { Id = a.Id, Code = a.Code, Name = a.Name, Nature = a.Nature })];
            NotifyPendingChangesState();
            _notificationService.ShowSuccess(result.Message);
            return Task.CompletedTask;
        }

        #endregion

        #region Initialize

        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;

                var (accountsFragment, groupsFragment, query) = _initializeQuery.Value;
                var variables = new GraphQLVariables()
                    .For(accountsFragment, "pagination", new { pageSize = -1 })
                    .For(accountsFragment, "filters", new { minCodeLength = 4 })
                    .For(accountsFragment, "sort", new[] { new { field = "CODE", direction = "ASC" } })
                    .For(groupsFragment, "filters", new { })
                    .Build();

                var result = await _accountingAccountService.GetDataContextAsync<InitializeContextData>(query, variables);

                AccountingAccounts = _autoMapper.Map<ObservableCollection<AccountingAccountGroupDTO>>(result.AccountingAccountsPage.Entries);
                foreach (var account in AccountingAccounts) account.Context = this;

                Groups = new ObservableCollection<AccountingAccountGroupGraphQLModel>(result.AccountingAccountGroupsPage.Entries);
                if (Groups is not null && Groups.Count>0) SelectedGroup = Groups.First();
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al inicializar el módulo.\r\n{GetType().Name}.{nameof(InitializeAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Group Filters

        private void UpdateFilteredAccounts()
        {
            if (GroupFilters == null || GroupFilters.Count == 0)
            {
                FilteredAccountingAccounts = new ObservableCollection<AccountingAccountGroupDTO>(AccountingAccounts);
            }
            else
            {
                var prefixes = GroupFilters.Select(f => f.AccountingAccountCode.Trim()).ToList();
                FilteredAccountingAccounts = new ObservableCollection<AccountingAccountGroupDTO>(
                    AccountingAccounts.Where(a => prefixes.Any(p => a.Code.TrimEnd().StartsWith(p))));
            }
            SelectedAccountingAccountCode = string.Empty;
        }

        public async Task OpenFilterDialogAsync()
        {
            if (SelectedGroup is null) return;

            try
            {
                var dialog = new AccountingAccountGroupFilterDialogViewModel(
                    _autoMapper, _notificationService, _dialogService, _accountingAccountGroupService);
                dialog.Initialize(SelectedGroup, AccountingAccounts);

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    dialog.DialogWidth = parentView.ActualWidth * 0.7;

                bool? result = await _dialogService.ShowDialogAsync(dialog, $"Filtros del grupo: {SelectedGroup.Name}");

                if (dialog.FiltersChanged)
                {
                    GroupFilters = _autoMapper.Map<ObservableCollection<AccountingAccountGroupFilterDTO>>(SelectedGroup.Filters);
                    UpdateFilteredAccounts();
                }
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al abrir el diálogo de filtros.\r\n{GetType().Name}.{nameof(OpenFilterDialogAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
        }

        #endregion

        #region Filter

        public void ApplyFilter()
        {
            if (string.IsNullOrEmpty(FilterSearch))
            {
                UpdateMainList();
            }
            else
            {
                SelectedGroupAccountingAccounts = [.. _selectedGroupAccountingAccountsShadow
                    .Where(x => x.Name.Contains(FilterSearch, StringComparison.CurrentCultureIgnoreCase)
                             || x.Code.Contains(FilterSearch, StringComparison.CurrentCultureIgnoreCase))];
            }
        }

        public void UpdateMainList()
        {
            SelectedGroupAccountingAccounts = [.. _selectedGroupAccountingAccountsShadow];
        }

        #endregion

        #region Add / Delete Accounts

        public void AddAccountingAccounts()
        {
            string selectedCode = SelectedAccountingAccountCode.Trim();
            foreach (var accountingAccount in AccountingAccounts)
            {
                if (accountingAccount.Code.TrimEnd().StartsWith(selectedCode)
                    && accountingAccount.Code.Trim().Length >= 8
                    && _selectedGroupAccountingAccountsShadow.FirstOrDefault(x => x.Id == accountingAccount.Id) == null)
                {
                    var clone = _autoMapper.Map<AccountingAccountGroupDTO>(accountingAccount);
                    clone.Context = this;
                    _selectedGroupAccountingAccountsShadow.Add(clone);
                }
            }
            FilterSearch = string.Empty;
            SelectedAccountingAccountCode = string.Empty;
            UpdateMainList();
            NotifyPendingChangesState();
        }

        public void DeleteAccountingAccounts()
        {
            var toRemove = _selectedGroupAccountingAccountsShadow.Where(x => x.IsChecked).ToList();
            foreach (var item in toRemove)
            {
                _selectedGroupAccountingAccountsShadow.Remove(item);
                item.IsChecked = false;
            }
            _isAllChecked = false;
            NotifyOfPropertyChange(nameof(IsAllChecked));
            FilterSearch = string.Empty;
            UpdateMainList();
            NotifyPendingChangesState();
        }

        #endregion

        #region Undo

        public void UndoChanges()
        {
            if (SelectedGroup is null) return;
            _selectedGroupAccountingAccountsShadow = _autoMapper.Map<ObservableCollection<AccountingAccountGroupDTO>>(SelectedGroup.Accounts);
            foreach (var account in _selectedGroupAccountingAccountsShadow) account.Context = this;
            _isAllChecked = false;
            NotifyOfPropertyChange(nameof(IsAllChecked));
            FilterSearch = string.Empty;
            SelectedAccountingAccountCode = string.Empty;
            UpdateMainList();
            NotifyPendingChangesState();
        }

        #endregion

        #region Save

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                List<int> accountingAccountsIds = [.. _selectedGroupAccountingAccountsShadow.Select(x => x.Id)];
                UpsertResponseType<AccountingAccountGroupGraphQLModel> result  = await ExecuteSaveAsync(accountingAccountsIds);

                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(new AccountingAccountGroupUpdateMessage { UpsertAccountingAccountGroup = result }, CancellationToken.None);
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
            } finally
            {
                IsBusy = false;
            }
        }

        public async Task<UpsertResponseType<AccountingAccountGroupGraphQLModel>> ExecuteSaveAsync(List<int> accountingAccountsIds)
        {
            try
            {
                var (fragment, query) = _updateQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "data", new { name = SelectedGroup!.Name, key = SelectedGroup.Key, accounts = accountingAccountsIds })
                    .For(fragment, "id", SelectedGroup.Id)
                    .Build();
                UpsertResponseType<AccountingAccountGroupGraphQLModel> result = await _accountingAccountGroupService.UpdateAsync<UpsertResponseType<AccountingAccountGroupGraphQLModel>>(query, variables);
                return result;
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment AccountsFragment, GraphQLQueryFragment GroupsFragment, string Query)> _initializeQuery = new(() =>
        {
            var accountsFields = FieldSpec<PageType<AccountingAccountGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Code)
                    .Field(e => e.Name)
                    .Field(e => e.Nature))
                .Build();

            var accountsFragment = new GraphQLQueryFragment("AccountingAccountsPage",
                [new("pagination", "Pagination"), new("filters", "AccountingAccountFilters"), new("sort", "[AccountingAccountSortInput]")],
                accountsFields, "AccountingAccountsPage");

            var groupsFields = FieldSpec<PageType<AccountingAccountGroupGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Key)
                    .SelectList(e => e.Accounts, acc => acc
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                        .Field(c => c.Code)
                        .Field(c => c.Nature))
                    .SelectList(e => e.Filters, f => f
                        .Field(fi => fi.Id)
                        .Select(fi => fi.AccountingAccount, acc => acc
                            .Field(a => a.Id)
                            .Field(a => a.Code)
                            .Field(a => a.Name))))
                .Build();

            var groupsFragment = new GraphQLQueryFragment("accountingAccountGroupsPage",
                [new("filters", "AccountingAccountGroupFilters")],
                groupsFields, "AccountingAccountGroupsPage");

            var query = new GraphQLQueryBuilder([accountsFragment, groupsFragment]).GetQuery();
            return (accountsFragment, groupsFragment, query);
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<AccountingAccountGroupGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingAccountGroup", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Key)
                    .SelectList(e => e.Accounts, acc => acc
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                        .Field(c => c.Code)
                        .Field(c => c.Nature)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateAccountingAccountGroupWithAccountsInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("UpdateAccountingAccountGroupWithAccounts", parameters, fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion

        #region Response Types

        private class InitializeContextData
        {
            public PageType<AccountingAccountGraphQLModel> AccountingAccountsPage { get; set; } = new();
            public PageType<AccountingAccountGroupGraphQLModel> AccountingAccountGroupsPage { get; set; } = new();
        }

        #endregion
    }
}
