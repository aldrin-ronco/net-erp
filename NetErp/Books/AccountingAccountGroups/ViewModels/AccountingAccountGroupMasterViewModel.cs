using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.XtraEditors.Filtering;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using Models.Books;
using Models.Global;
using NetErp.Books.AccountingAccountGroups.DTO;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using Services.Global.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using static Amazon.S3.Util.S3EventNotification;
using static DevExpress.Data.Utils.SafeProcess;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.AccountingAccountGroups.ViewModels
{
    public class AccountingAccountGroupMasterViewModel: Screen
    {
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AccountingAccountGroupGraphQLModel> _accountingAccountGroupService;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;

        public AccountingAccountGroupViewModel Context { get; set; }
        public AccountingAccountGroupMasterViewModel(AccountingAccountGroupViewModel context,
            Helpers.Services.INotificationService notificationService,
            IRepository<AccountingAccountGroupGraphQLModel> accountingAccountGroupService,
            AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache
)
        {
            _notificationService = notificationService;
            _accountingAccountGroupService = accountingAccountGroupService;
            _auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;
            Context = context;
            Context.EventAggregator.SubscribeOnUIThread(this);
            _ = InitializeAsync();
        }

        private ObservableCollection<AccountingAccountGroupGraphQLModel> _groups;

        public ObservableCollection<AccountingAccountGroupGraphQLModel> Groups
        {
            get { return _groups; }
            set 
            {
                if(_groups != value)
                {
                    _groups = value;
                    NotifyOfPropertyChange(nameof(Groups));
                    NotifyOfPropertyChange(nameof(ShowAllControls));
                    
                }
            }
        }

        private ObservableCollection<AccountingAccountGroupDTO> _selectedGroupAccountingAccounts = [];

        public ObservableCollection<AccountingAccountGroupDTO> SelectedGroupAccountingAccounts
        {
            get { return _selectedGroupAccountingAccounts; }
            set 
            {
                if (_selectedGroupAccountingAccounts != value)
                {
                    _selectedGroupAccountingAccounts = value;
                    NotifyOfPropertyChange(nameof(SelectedGroupAccountingAccounts));
                }
            }
        }
        private AccountingAccountGroupDTO _selectedGroupAccountingAccountToDeleted;
        public AccountingAccountGroupDTO SelectedGroupAccountingAccountToDeleted
        {
            get { return _selectedGroupAccountingAccountToDeleted; }
            set
            {
                if (_selectedGroupAccountingAccountToDeleted != value)
                {
                    _selectedGroupAccountingAccountToDeleted = value;
                    NotifyOfPropertyChange(nameof(SelectedGroupAccountingAccountToDeleted));
                }
            }
        }

        private ObservableCollection<AccountingAccountGroupDTO> _selectedGroupAccountingAccountsShadow;

        private ObservableCollection<AccountingAccountGroupDTO> _accountingAccounts;

        public ObservableCollection<AccountingAccountGroupDTO> AccountingAccounts
        {
            get { return _accountingAccounts; }
            set 
            {
                if (_accountingAccounts != value)
                {
                    _accountingAccounts = value;
                    NotifyOfPropertyChange(nameof(AccountingAccounts));
                }
            }
        }

        private string _filterSearch = "";
        public string FilterSearch
        {
            get => _filterSearch;
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    // Solo ejecutamos la busqueda si esta vacio el filtro o si hay por lo menos 3 caracteres digitados
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) ApplyFilter();
                }
            }
        }

        private string _selectedAccountingAccountCode;

        public string SelectedAccountingAccountCode
        {
            get { return _selectedAccountingAccountCode; }
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


        private AccountingAccountGroupGraphQLModel _selectedGroup;

        public AccountingAccountGroupGraphQLModel SelectedGroup
        {
            get { return _selectedGroup; }
            set
            {
                if (_selectedGroup != value)
                {
                    _selectedGroup = value;
                    NotifyOfPropertyChange(nameof(SelectedGroup));
                    SelectedAccountingAccountCode = "";
                    FilterSearch = "";
                    SelectedGroupAccountingAccounts = [.. Context.AutoMapper.Map<ObservableCollection<AccountingAccountGroupDTO>>(SelectedGroup.Accounts)];
                    foreach (var accountingAccount in SelectedGroupAccountingAccounts)
                    {
                        accountingAccount.Context = this;
                    }
                    _selectedGroupAccountingAccountsShadow = [.. SelectedGroupAccountingAccounts];
                }
            }
        }

        private bool _isBusy;

        public bool IsBusy
        {
            get { return _isBusy; }
            set 
            {
                if (_isBusy != value) 
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }


        public bool CanAddAccountingAccount 
        {
            get
            {
                if(string.IsNullOrEmpty(SelectedAccountingAccountCode)) return false; 
                return true;
            }
        }

        public bool AccountingAccountGroupComboBoxIsEnabled
        {
            get
            {
                var selectedGroupAccountIds = SelectedGroup is null ? [] : SelectedGroup.Accounts.Select(a => a.Id).ToList();
                var selectedGroupAccountingAccountIds = SelectedGroup is null ? [] : SelectedGroupAccountingAccounts.Select(a => a.Id).ToList();
                if(!selectedGroupAccountIds.SequenceEqual(selectedGroupAccountingAccountIds)) return false;
                return true;
            }
        }
        public bool ShowAllControls
        {
            get
            {
                return Groups != null && Groups.Count > 0;
            }
        }
        private ICommand _addAccountingAccountCommand;

        public ICommand AddAccountingAccountCommand
        {
            get 
            {
                if(_addAccountingAccountCommand is null) _addAccountingAccountCommand = new DelegateCommand(AddAccountingAccounts);
                return _addAccountingAccountCommand; 
            }
        }

        private ICommand _deleteAccountingAccountCommand;

        public ICommand DeleteAccountingAccountCommand
        {
            get
            {
                if (_deleteAccountingAccountCommand is null) _deleteAccountingAccountCommand = new DelegateCommand(DeleteAccountingAccounts);
                return _deleteAccountingAccountCommand;
            }
        }

        private ICommand _saveCommand;

        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        public void ApplyFilter()
        {
            if (string.IsNullOrEmpty(FilterSearch))
            {
                UpdateMainList();
            }
            else
            {
                SelectedGroupAccountingAccounts = [.. _selectedGroupAccountingAccountsShadow.Where(x => x.Name.Contains(FilterSearch, StringComparison.CurrentCultureIgnoreCase) || x.Code.Contains(FilterSearch, StringComparison.CurrentCultureIgnoreCase))];
            }
        }

        public bool CanDeleteAccountingAccount 
        {
            get
            {
                if(SelectedGroupAccountingAccounts.Any(x => x.IsChecked == true)) return true;
                return false;
            }
        }

        public async Task SaveAsync()
        {
           List<int> accountingAccountsIds = [.. _selectedGroupAccountingAccountsShadow.Select(x => x.Id)];
           await ExcecuteSaveAsync(accountingAccountsIds);
        }
       
        public async Task ExcecuteSaveAsync(List<int> accountingAccountsIds)
        {
            dynamic variables = new ExpandoObject();


             

            try
            {
                IsBusy = true;

               
                string query = GetUpdateQuery();
                variables.updateResponseData = new ExpandoObject();
                variables.updateResponseData.name = SelectedGroup.Name;
                variables.updateResponseData.key = SelectedGroup.Key;
                variables.updateResponseData.accounts = accountingAccountsIds;
                variables.updateResponseId = SelectedGroup.Id;
                AccountingAccountGroupGraphQLModel result = await _accountingAccountGroupService.UpdateAsync(query, variables);
                SelectedGroup.Accounts = result.Accounts;
                NotifyOfPropertyChange(nameof(AccountingAccountGroupComboBoxIsEnabled));
                _notificationService.ShowSuccess("La configuración se ha guardado correctamente");
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task DeleteAccountingAccountAsync()
        {
           
                MessageBoxResult result = ThemedMessageBox.Show(title: "Atención!", text: $"¿Confirma que desea eliminar el registro {SelectedGroupAccountingAccountToDeleted.Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
            
           
            var Accounts = SelectedGroupAccountingAccounts.Where(f => f.Id != SelectedGroupAccountingAccountToDeleted.Id);
            List<int> accountingAccountsIds = [.. Accounts.Select(x => x.Id)];
            await ExcecuteSaveAsync(accountingAccountsIds);
            _selectedGroupAccountingAccountsShadow.Remove(_selectedGroupAccountingAccountsShadow.First(f => f.Id == SelectedGroupAccountingAccountToDeleted.Id));
            SelectedGroupAccountingAccounts = [.. Accounts];

        }
        public void DeleteAccountingAccounts()
        {
            MessageBoxResult result = ThemedMessageBox.Show(title: "Atención!", text: $"¿Confirma que desea eliminar los registros seleccionados?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
            foreach (var accountingAccount in SelectedGroupAccountingAccounts)
            {
                if (accountingAccount.IsChecked is true) 
                {
                    _selectedGroupAccountingAccountsShadow.Remove(_selectedGroupAccountingAccountsShadow.First(f => f.Id == accountingAccount.Id));
                    accountingAccount.IsChecked = false;
                } 
            }
            FilterSearch = "";
            UpdateMainList();
            NotifyOfPropertyChange(nameof(AccountingAccountGroupComboBoxIsEnabled));
        }


        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
        }

        public void UpdateMainList()
        {
            SelectedGroupAccountingAccounts = [.. _selectedGroupAccountingAccountsShadow];
        }

        public void AddAccountingAccounts()
        {
            foreach(var accountingAccount in AccountingAccounts)
            {
                if (accountingAccount.Code.StartsWith(SelectedAccountingAccountCode) && accountingAccount.Code.Length >= 8 && _selectedGroupAccountingAccountsShadow.FirstOrDefault(x => x.Id == accountingAccount.Id) == null) 
                {
                    accountingAccount.Context = this;
                    _selectedGroupAccountingAccountsShadow.Add(accountingAccount);
                }
            }
            FilterSearch = "";
            SelectedAccountingAccountCode = "";
            UpdateMainList();
            NotifyOfPropertyChange(nameof(AccountingAccountGroupComboBoxIsEnabled));
        }
        public string GetUpdateQuery() { 
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
                     )
                    

                    )
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
        var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public async Task InitializeAsync()
        {
            await Task.WhenAll(
              _auxiliaryAccountingAccountCache.EnsureLoadedAsync()
             
              );
            AccountingAccounts = [.. Context.AutoMapper.Map<ObservableCollection<AccountingAccountGroupDTO>>(_auxiliaryAccountingAccountCache.Items)];

            try
            {
                IsBusy = true;
                string query = GetLoadAccountingAccountGroupQuery();

                dynamic variables = new ExpandoObject();
                variables.AccountingAccountGroupsFilters = new ExpandoObject();
                variables.accountingAccountsFilters = new ExpandoObject();
                variables.accountingAccountsFilters.only_auxiliary_accounts = true;

                PageType<AccountingAccountGroupGraphQLModel> result = await _accountingAccountGroupService.GetPageAsync(query, variables);
                Groups = [.. result.Entries];
                SelectedGroup = Groups.FirstOrDefault();
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            finally
            {
                //Initialized = true;
                IsBusy = false;
            }
        }
        public string GetLoadAccountingAccountGroupQuery()
        {
           
          
            var accountingAccountGroupFields = FieldSpec<PageType<AccountingAccountGroupGraphQLModel>>
              .Create()
              .SelectList(it => it.Entries, entries => entries
                  .Field(e => e.Id)
                  .Field(e => e.Name)
                  .Field(e => e.Key)
                  .SelectList(e => e.Accounts, cat => cat
                      .Field(c => c.Id)
                      .Field(c => c.Name)
                      .Field(c => c.Code)
                  )
              )
              .Field(o => o.PageNumber)
              .Field(o => o.PageSize)
              .Field(o => o.TotalPages)
              .Field(o => o.TotalEntries)
              .Build();
            
            var accountingAccountGroupParameters = new GraphQLQueryParameter("pagination", "Pagination");
            var accountingAccountGroupFilterParameters = new GraphQLQueryParameter("filters", "AccountingAccountGroupFilters");
            var accountingAccountGroupFragment = new GraphQLQueryFragment("accountingAccountGroupsPage", [accountingAccountGroupParameters, accountingAccountGroupFilterParameters], accountingAccountGroupFields, "PageResponse");

            

            var builder =  new GraphQLQueryBuilder([accountingAccountGroupFragment]);
            return builder.GetQuery();
        }
        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {

            // Desconectar eventos para evitar memory leaks
            Context.EventAggregator.Unsubscribe(this);
            return base.OnDeactivateAsync(close, cancellationToken);
        }
    }
}
