using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Books;
using NetErp.Books.AccountingAccountGroups.DTO;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.AccountingAccountGroups.ViewModels
{
    public class AccountingAccountGroupViewModel : Screen
    {
        #region Dependencies

        private readonly IMapper _autoMapper;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AccountingAccountGroupGraphQLModel> _accountingAccountGroupService;
        private readonly IRepository<AccountingAccountGraphQLModel> _accountingAccountService;

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

        private AccountingAccountGroupGraphQLModel? _selectedGroup;
        public AccountingAccountGroupGraphQLModel? SelectedGroup
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
                    }
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

        #endregion

        #region Constructor

        public AccountingAccountGroupViewModel(
            IMapper mapper,
            Helpers.Services.INotificationService notificationService,
            IRepository<AccountingAccountGroupGraphQLModel> accountingAccountGroupService,
            IRepository<AccountingAccountGraphQLModel> accountingAccountService)
        {
            _autoMapper = mapper;
            _notificationService = notificationService;
            _accountingAccountGroupService = accountingAccountGroupService;
            _accountingAccountService = accountingAccountService;
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            await InitializeAsync();
        }

        #endregion

        #region Initialize

        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;

                string accountsQuery = GetLoadAccountingAccountsQuery();
                dynamic accountsVariables = new ExpandoObject();
                accountsVariables.AccountingAccountsPagePagination = new ExpandoObject();
                accountsVariables.AccountingAccountsPagePagination.pageSize = -1;
                accountsVariables.AccountingAccountsPageFilters = new ExpandoObject();
                accountsVariables.AccountingAccountsPageFilters.minCodeLength = 4;
                accountsVariables.AccountingAccountsPageSort = new[] { new { field = "CODE", direction = "ASC" } };

                PageType<AccountingAccountGraphQLModel> accountsResult = await _accountingAccountService.GetPageAsync(accountsQuery, accountsVariables);
                AccountingAccounts = _autoMapper.Map<ObservableCollection<AccountingAccountGroupDTO>>(accountsResult.Entries);
                foreach (var account in AccountingAccounts) account.Context = this;

                string query = GetLoadAccountingAccountGroupQuery();
                dynamic variables = new ExpandoObject();
                variables.AccountingAccountGroupsFilters = new ExpandoObject();

                PageType<AccountingAccountGroupGraphQLModel> result = await _accountingAccountGroupService.GetPageAsync(query, variables);
                Groups = [.. result.Entries];
                SelectedGroup = Groups.FirstOrDefault();
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"{GetType().Name}.InitializeAsync \r\n{ex.Message}",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            finally
            {
                IsBusy = false;
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
            foreach (var accountingAccount in AccountingAccounts)
            {
                if (accountingAccount.Code.StartsWith(SelectedAccountingAccountCode)
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
            NotifyOfPropertyChange(nameof(HasPendingChanges));
            NotifyOfPropertyChange(nameof(AccountingAccountGroupComboBoxIsEnabled));
            NotifyOfPropertyChange(nameof(CanSave));
            NotifyOfPropertyChange(nameof(CanUndo));
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
            NotifyOfPropertyChange(nameof(HasPendingChanges));
            NotifyOfPropertyChange(nameof(AccountingAccountGroupComboBoxIsEnabled));
            NotifyOfPropertyChange(nameof(CanSave));
            NotifyOfPropertyChange(nameof(CanUndo));
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
            NotifyOfPropertyChange(nameof(HasPendingChanges));
            NotifyOfPropertyChange(nameof(AccountingAccountGroupComboBoxIsEnabled));
            NotifyOfPropertyChange(nameof(CanSave));
            NotifyOfPropertyChange(nameof(CanUndo));
        }

        #endregion

        #region Save

        public async Task SaveAsync()
        {
            List<int> accountingAccountsIds = [.. _selectedGroupAccountingAccountsShadow.Select(x => x.Id)];
            await ExecuteSaveAsync(accountingAccountsIds);
        }

        public async Task ExecuteSaveAsync(List<int> accountingAccountsIds)
        {
            try
            {
                IsBusy = true;

                string query = GetUpdateQuery();
                dynamic variables = new ExpandoObject();
                variables.updateResponseData = new ExpandoObject();
                variables.updateResponseData.name = SelectedGroup!.Name;
                variables.updateResponseData.key = SelectedGroup.Key;
                variables.updateResponseData.accounts = accountingAccountsIds;
                variables.updateResponseId = SelectedGroup.Id;

                await _accountingAccountGroupService.UpdateAsync(query, variables);
                SelectedGroup.Accounts = _selectedGroupAccountingAccountsShadow
                    .Select(a => new AccountingAccountGroupDetailGraphQLModel { Id = a.Id, Code = a.Code, Name = a.Name, Nature = a.Nature })
                    .ToList();
                NotifyOfPropertyChange(nameof(HasPendingChanges));
                NotifyOfPropertyChange(nameof(AccountingAccountGroupComboBoxIsEnabled));
                NotifyOfPropertyChange(nameof(CanSave));
                NotifyOfPropertyChange(nameof(CanUndo));
                _notificationService.ShowSuccess("La configuración se ha guardado correctamente");
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"{GetType().Name}.ExecuteSaveAsync \r\n{ex.Message}",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        public string GetLoadAccountingAccountGroupQuery()
        {
            var fields = FieldSpec<PageType<AccountingAccountGroupGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Key)
                    .SelectList(e => e.Accounts, acc => acc
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                        .Field(c => c.Code)
                        .Field(c => c.Nature)))
                .Field(o => o.PageNumber)
                .Field(o => o.PageSize)
                .Field(o => o.TotalPages)
                .Field(o => o.TotalEntries)
                .Build();

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var filtersParam = new GraphQLQueryParameter("filters", "AccountingAccountGroupFilters");
            var fragment = new GraphQLQueryFragment("accountingAccountGroupsPage", [paginationParam, filtersParam], fields, "PageResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        public string GetUpdateQuery()
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
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetLoadAccountingAccountsQuery()
        {
            var fields = FieldSpec<PageType<AccountingAccountGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Code)
                    .Field(e => e.Name)
                    .Field(e => e.Nature))
                .Field(o => o.PageNumber)
                .Field(o => o.PageSize)
                .Field(o => o.TotalPages)
                .Field(o => o.TotalEntries)
                .Build();

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var filtersParam = new GraphQLQueryParameter("filters", "AccountingAccountFilters");
            var sortParam = new GraphQLQueryParameter("sort", "[AccountingAccountSortInput]");
            var fragment = new GraphQLQueryFragment("AccountingAccountsPage", [paginationParam, filtersParam, sortParam], fields, "PageResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        #endregion
    }
}
