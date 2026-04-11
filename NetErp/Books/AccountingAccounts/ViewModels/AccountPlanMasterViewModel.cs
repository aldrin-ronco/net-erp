using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Books.AccountingAccounts.DTO;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.AccountingAccounts.ViewModels
{
    public class AccountPlanMasterViewModel : Screen,
        IHandle<AccountingAccountCreateListMessage>,
        IHandle<AccountingAccountUpdateMessage>,
        IHandle<AccountingAccountDeleteMessage>,
        IHandle<PermissionsCacheRefreshedMessage>
    {
        #region Dependencies

        private readonly IRepository<AccountingAccountGraphQLModel> _accountingAccountService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly StringLengthCache _stringLengthCache;
        private readonly PermissionCache _permissionCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        #endregion

        #region Constructor

        public AccountPlanMasterViewModel(
            IRepository<AccountingAccountGraphQLModel> accountingAccountService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            IEventAggregator eventAggregator,
            StringLengthCache stringLengthCache,
            PermissionCache permissionCache,
            JoinableTaskFactory joinableTaskFactory)
        {
            _accountingAccountService = accountingAccountService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _stringLengthCache = stringLengthCache;
            _permissionCache = permissionCache;
            _joinableTaskFactory = joinableTaskFactory;
            _eventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Properties

        public List<AccountingAccountGraphQLModel> Accounts { get; set; } = [];

        public ObservableCollection<AccountingAccountDTO> AccountingAccounts
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountingAccounts));
                }
            }
        } = [];

        public AccountingAccountDTO? SelectedItem
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(CanEditAccount));
                    NotifyOfPropertyChange(nameof(CanDeleteAccount));
                }
            }
        }

        #region Search

        private readonly DebouncedAction _searchDebounce = new();

        public string SearchText
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SearchText));
                    if (string.IsNullOrEmpty(value))
                    {
                        SearchResults = [];
                        ShowSearchResults = false;
                    }
                    else if (value.Length >= 2)
                    {
                        _ = _searchDebounce.RunAsync(() => { FilterSearchResults(); return Task.CompletedTask; });
                    }
                }
            }
        } = string.Empty;

        public ObservableCollection<AccountingAccountGraphQLModel> SearchResults
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SearchResults));
                }
            }
        } = [];

        public AccountingAccountGraphQLModel? SelectedSearchResult
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedSearchResult));
                    if (value != null)
                    {
                        SearchAccount(value.Code);
                        SearchText = string.Empty;
                        ShowSearchResults = false;
                    }
                }
            }
        }

        public bool ShowSearchResults
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ShowSearchResults));
                }
            }
        }

        private void FilterSearchResults()
        {
            string search = SearchText.Trim().ToUpperInvariant();
            List<AccountingAccountGraphQLModel> results = [.. Accounts
                .Where(a => a.Code.Contains(search) ||
                            a.Name.Contains(search, StringComparison.InvariantCultureIgnoreCase))
                .OrderBy(a => a.Code)
                .Take(15)];

            SearchResults = new ObservableCollection<AccountingAccountGraphQLModel>(results);
            ShowSearchResults = results.Count > 0;
        }

        #endregion

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

        #endregion

        #region Permissions

        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.AccountingAccount.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.AccountingAccount.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.AccountingAccount.Delete);
        public bool HasReportPermission => _permissionCache.IsAllowed(PermissionCodes.AccountingAccount.Report);

        public bool CanCreateAccount => HasCreatePermission;
        public bool CanEditAccount => HasEditPermission && SelectedItem != null;
        public bool CanDeleteAccount => HasDeletePermission && SelectedItem != null;
        public bool CanReport => HasReportPermission;

        #endregion

        #region Commands

        private ICommand? _createCommand;
        public ICommand CreateCommand
        {
            get
            {
                _createCommand ??= new AsyncCommand(CreateAsync);
                return _createCommand;
            }
        }

        private ICommand? _editCommand;
        public ICommand EditCommand
        {
            get
            {
                _editCommand ??= new AsyncCommand(EditAsync);
                return _editCommand;
            }
        }

        private ICommand? _deleteCommand;
        public ICommand DeleteCommand
        {
            get
            {
                _deleteCommand ??= new AsyncCommand(DeleteAsync);
                return _deleteCommand;
            }
        }

        private ICommand? _reportCommand;
        public ICommand ReportCommand
        {
            get
            {
                _reportCommand ??= new AsyncCommand(ReportAsync);
                return _reportCommand;
            }
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                IsBusy = true;

                NotifyOfPropertyChange(nameof(HasCreatePermission));
                NotifyOfPropertyChange(nameof(HasEditPermission));
                NotifyOfPropertyChange(nameof(HasDeletePermission));
                NotifyOfPropertyChange(nameof(HasReportPermission));
                NotifyOfPropertyChange(nameof(CanCreateAccount));
                NotifyOfPropertyChange(nameof(CanEditAccount));
                NotifyOfPropertyChange(nameof(CanDeleteAccount));
                NotifyOfPropertyChange(nameof(CanReport));

                await LoadAccountsAsync();
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al inicializar el módulo.\r\n{GetType().Name}.{nameof(OnViewReady)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
                await TryCloseAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                _eventAggregator.Unsubscribe(this);
                AccountingAccounts.Clear();
                Accounts.Clear();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region Data Loading

        private async Task LoadAccountsAsync()
        {
            (GraphQLQueryFragment fragment, string query) = _loadQuery.Value;

            dynamic variables = new GraphQLVariables()
                .For(fragment, "pagination", new { PageSize = -1 })
                .Build();

            PageType<AccountingAccountGraphQLModel> result = await _accountingAccountService.GetPageAsync(query, variables);
            Accounts = [.. result.Entries];
            AccountingAccounts = PopulateAccountingAccountDTO(Accounts);
        }

        #endregion

        #region Tree Operations

        public void LoadChildren(AccountingAccountDTO parent, List<AccountingAccountGraphQLModel> accounts)
        {
            string parentCode = parent.Code.Trim();
            if (!parent.Childrens[0].IsDummyChild) return;

            int nLevel = parent.Code.Trim().Length switch
            {
                1 => 2,
                2 => 4,
                4 => 6,
                _ => 8,
            };

            parent.Childrens.RemoveAt(0);

            IEnumerable<AccountingAccountGraphQLModel> sorted = accounts
                .Where(a => a.Code.Trim().Length == nLevel && a.Code.Trim().StartsWith(parentCode.Trim()))
                .OrderBy(a => a.Code);

            foreach (AccountingAccountGraphQLModel account in sorted)
            {
                AccountingAccountDTO child = new()
                {
                    Id = account.Id,
                    Code = account.Code.Trim(),
                    Name = account.Name.Trim(),
                    Context = this,
                    Childrens = parent.Code.Trim().Length == 6
                        ? []
                        : [new AccountingAccountDTO { IsDummyChild = true, Code = "000", Name = "DummyChild" }]
                };
                parent.Childrens.Add(child);
            }
        }

        public void SearchAccount(string content)
        {
            IEnumerable<AccountingAccountDTO>? lv1 = null;
            IEnumerable<AccountingAccountDTO>? lv2 = null;
            IEnumerable<AccountingAccountDTO>? lv3 = null;
            IEnumerable<AccountingAccountDTO>? lv4 = null;
            IEnumerable<AccountingAccountDTO>? lv5 = null;
            string lv1Code = content.Trim().Length >= 1 ? content[..1] : "";
            string lv2Code = content.Trim().Length >= 2 ? content[..2] : "";
            string lv3Code = content.Trim().Length >= 4 ? content[..4] : "";
            string lv4Code = content.Trim().Length >= 6 ? content[..6] : "";
            string lv5Code = content.Trim().Length >= 8 ? content : "";

            lv1 = AccountingAccounts.Where(a => a.Code.Trim() == lv1Code);
            if (!lv1.Any()) return;

            if (!string.IsNullOrEmpty(lv2Code))
            {
                if (lv1.First().Childrens[0].IsDummyChild) LoadChildren(lv1.First(), Accounts);
                lv1.First().IsExpanded = true;
                lv2 = lv1.First().Childrens.Where(a => a.Code.Trim() == lv2Code);
            }
            else
            {
                lv1.First().IsSelected = true;
                SelectedItem = lv1.First();
                return;
            }

            if (!string.IsNullOrEmpty(lv3Code))
            {
                if (lv2.First().Childrens[0].IsDummyChild) LoadChildren(lv2.First(), Accounts);
                lv2.First().IsExpanded = true;
                lv3 = lv2.First().Childrens.Where(a => a.Code.Trim() == lv3Code);
            }
            else
            {
                lv2.First().IsSelected = true;
                SelectedItem = lv2.First();
                return;
            }

            if (!string.IsNullOrEmpty(lv4Code))
            {
                if (lv3.First().Childrens[0].IsDummyChild) LoadChildren(lv3.First(), Accounts);
                lv3.First().IsExpanded = true;
                lv4 = lv3.First().Childrens.Where(a => a.Code.Trim() == lv4Code);
            }
            else
            {
                lv3.First().IsSelected = true;
                SelectedItem = lv3.First();
                return;
            }

            if (!string.IsNullOrEmpty(lv5Code))
            {
                if (lv4.First().Childrens[0].IsDummyChild) LoadChildren(lv4.First(), Accounts);
                lv4.First().IsExpanded = true;
                lv5 = lv4.First().Childrens.Where(a => a.Code.Trim() == lv5Code);
            }
            else
            {
                lv4.First().IsSelected = true;
                SelectedItem = lv4.First();
                return;
            }

            if (lv5 != null && lv5.Any())
            {
                lv5.First().IsSelected = true;
                SelectedItem = lv5.First();
            }
        }

        public ObservableCollection<AccountingAccountDTO> PopulateAccountingAccountDTO(List<AccountingAccountGraphQLModel> accounts)
        {
            ObservableCollection<AccountingAccountDTO> accountsDTO = [];

            IEnumerable<AccountingAccountGraphQLModel> sorted = accounts
                .Where(a => a.Code.Trim().Length == 1)
                .OrderBy(a => a.Code);

            foreach (AccountingAccountGraphQLModel account in sorted)
            {
                AccountingAccountDTO dto = new()
                {
                    Id = account.Id,
                    Code = account.Code.Trim(),
                    Name = account.Name.Trim(),
                    Context = this,
                    Childrens = [new AccountingAccountDTO { IsDummyChild = true, Code = "000", Name = "DummyChild" }]
                };
                accountsDTO.Add(dto);
            }

            return accountsDTO;
        }

        private void DeleteAccountFromTree(string code)
        {
            AccountingAccountDTO? lv1 = code.Length >= 1 ? AccountingAccounts.FirstOrDefault(x => x.Code == code[..1]) : null;
            AccountingAccountDTO? lv2 = code.Length >= 2 && lv1 != null ? lv1.Childrens.FirstOrDefault(x => x.Code == code[..2]) : null;
            AccountingAccountDTO? lv3 = code.Length >= 4 && lv2 != null ? lv2.Childrens.FirstOrDefault(x => x.Code == code[..4]) : null;
            AccountingAccountDTO? lv4 = code.Length >= 6 && lv3 != null ? lv3.Childrens.FirstOrDefault(x => x.Code == code[..6]) : null;
            AccountingAccountDTO? lv5 = code.Length >= 8 && lv4 != null ? lv4.Childrens.FirstOrDefault(x => x.Code == code[..8]) : null;

            if (code.Length == 1 && lv1 != null) AccountingAccounts.Remove(lv1);
            else if (code.Length == 2 && lv1 != null && lv2 != null) lv1.Childrens.Remove(lv2);
            else if (code.Length == 4 && lv2 != null && lv3 != null) lv2.Childrens.Remove(lv3);
            else if (code.Length == 6 && lv3 != null && lv4 != null) lv3.Childrens.Remove(lv4);
            else if (code.Length >= 8 && lv4 != null && lv5 != null) lv4.Childrens.Remove(lv5);
        }

        private void RemoveAccountInMemory(int id)
        {
            AccountingAccountGraphQLModel? account = Accounts.FirstOrDefault(x => x.Id == id);
            if (account != null) Accounts.Remove(account);
        }

        #endregion

        #region CRUD Operations

        public async Task CreateAsync()
        {
            try
            {
                IsBusy = true;
                AccountPlanDetailViewModel detail = new(_accountingAccountService, _eventAggregator, _stringLengthCache, _joinableTaskFactory, Accounts);
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.60;
                    detail.DialogHeight = parentView.ActualHeight * 0.70;
                }

                await _dialogService.ShowDialogAsync(detail, "Nueva cuenta contable");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(CreateAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditAsync()
        {
            if (SelectedItem == null) return;
            try
            {
                IsBusy = true;
                AccountPlanDetailViewModel detail = new(_accountingAccountService, _eventAggregator, _stringLengthCache, _joinableTaskFactory, Accounts, selectedItemId: SelectedItem.Id);
                detail.Code = SelectedItem.Code;
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.60;
                    detail.DialogHeight = parentView.ActualHeight * 0.70;
                }

                await _dialogService.ShowDialogAsync(detail, "Modificar cuenta contable");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(EditAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteAsync()
        {
            if (SelectedItem == null) return;
            try
            {
                IsBusy = true;

                (GraphQLQueryFragment canDeleteFragment, string canDeleteQuery) = _canDeleteQuery.Value;
                dynamic canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedItem.Id)
                    .Build();
                CanDeleteType validation = await _accountingAccountService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    if (ThemedMessageBox.Show("Atención!",
                        "¿Confirma que desea eliminar la cuenta contable?",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    ThemedMessageBox.Show("Atención!",
                        $"Esta cuenta contable no puede ser eliminada\r\n\r\n{validation.Message}",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                (GraphQLQueryFragment deleteFragment, string deleteQuery) = _deleteQuery.Value;
                dynamic deleteVars = new GraphQLVariables()
                    .For(deleteFragment, "id", SelectedItem.Id)
                    .Build();
                DeleteResponseType deleteResponse = await _accountingAccountService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!deleteResponse.Success)
                {
                    ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"No fue posible eliminar la cuenta contable\r\n\r\n{deleteResponse.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error);
                    return;
                }

                RemoveAccountInMemory(SelectedItem.Id);

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new AccountingAccountDeleteMessage
                    {
                        DeletedAccountingAccount = new() { Id = SelectedItem.Id, Code = SelectedItem.Code },
                        DeletedResponseType = deleteResponse
                    },
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(DeleteAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ReportAsync()
        {
            if (!DirectoryHelper.Exists(ApplicationPaths.Reports.Books))
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Información",
                    text: $"No fue posible encontrar la ruta {DirectoryHelper.GetFullPath(ApplicationPaths.Reports.Books)}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Information);
                return;
            }

            try
            {
                IsBusy = true;
                (GraphQLQueryFragment fragment, string query) = _reportQuery.Value;
                dynamic variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { PageSize = -1 })
                    .Build();

                PageType<AccountingAccountGraphQLModel> result = await _accountingAccountService.GetPageAsync(query, variables);

                Stimulsoft.Report.StiReport report = new();
                report.Load(ApplicationPaths.Reports.Templates.AccountingAccountReport);

                var company = new { Name = SessionInfo.CurrentCompany!.CompanyEntity.SearchName };

                await report.RegBusinessObjectAsync("Company", "Company", "Company", company);
                await report.RegBusinessObjectAsync("AccountingAccounts", "AccountingAccounts", "AccountingAccounts", result.Entries);
                report.ShowWithWpf();
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(ReportAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Event Handlers

        public async Task HandleAsync(AccountingAccountCreateListMessage message, CancellationToken cancellationToken)
        {
            if (Accounts.Count == 0) return;
            Accounts.AddRange(message.UpsertList.Entity);
            AccountingAccounts = PopulateAccountingAccountDTO(Accounts);
            SearchAccount(message.UpsertList.Entity[^1].Code);
            _notificationService.ShowSuccess(message.UpsertList.Message);
            await Task.CompletedTask;
        }

        public async Task HandleAsync(AccountingAccountUpdateMessage message, CancellationToken cancellationToken)
        {
            if (Accounts.Count == 0) return;
            AccountingAccountGraphQLModel? existing = Accounts.FirstOrDefault(a => a.Id == message.UpsertAccount.Entity.Id);
            if (existing != null)
            {
                int index = Accounts.IndexOf(existing);
                Accounts[index] = message.UpsertAccount.Entity;
            }
            AccountingAccounts = PopulateAccountingAccountDTO(Accounts);
            SearchAccount(message.UpsertAccount.Entity.Code);
            _notificationService.ShowSuccess(message.UpsertAccount.Message);
            await Task.CompletedTask;
        }

        public async Task HandleAsync(AccountingAccountDeleteMessage message, CancellationToken cancellationToken)
        {
            DeleteAccountFromTree(message.DeletedAccountingAccount.Code);
            _notificationService.ShowSuccess(message.DeletedResponseType.Message);
            await Task.CompletedTask;
        }

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(HasReportPermission));
            NotifyOfPropertyChange(nameof(CanCreateAccount));
            NotifyOfPropertyChange(nameof(CanEditAccount));
            NotifyOfPropertyChange(nameof(CanDeleteAccount));
            NotifyOfPropertyChange(nameof(CanReport));
            return Task.CompletedTask;
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<AccountingAccountGraphQLModel>>
                .Create()
                .SelectList(list => list.Entries, entries => entries
                    .Field(f => f.Id)
                    .Field(f => f.Code)
                    .Field(f => f.Name)
                    .Field(f => f.Margin)
                    .Field(f => f.MarginBasis)
                    .Field(f => f.Nature)
                    .Field(f => f.InsertedAt)
                    .Field(f => f.UpdatedAt)
                    .Select(f => f.Company, company => company
                        .Field(f => f.Id)))
                .Build();

            var fragment = new GraphQLQueryFragment("accountingAccountsPage",
                [new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteAccountingAccount",
                [new("id", "ID!")],
                fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteAccountingAccount",
                [new("id", "ID!")],
                fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _reportQuery = new(() =>
        {
            var fields = FieldSpec<PageType<AccountingAccountGraphQLModel>>
                .Create()
                .SelectList(list => list.Entries, entries => entries
                    .Field(f => f.Code)
                    .Field(f => f.Name)
                    .Field(f => f.Nature))
                .Build();

            var fragment = new GraphQLQueryFragment("accountingAccountsPage",
                [new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion
    }
}
