﻿using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using Models.Books;
using NetErp.Books.AccountingAccountGroups.DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetErp.Books.AccountingAccountGroups.ViewModels
{
    public class AccountingAccountGroupMasterViewModel: Screen
    {
        public IGenericDataAccess<AccountingAccountGroupGraphQLModel> AccountingAccountGroupService { get; set; } = IoC.Get<IGenericDataAccess<AccountingAccountGroupGraphQLModel>>();
        public AccountingAccountGroupViewModel Context { get; set; }
        public AccountingAccountGroupMasterViewModel(AccountingAccountGroupViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnUIThread(this);
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
                    SelectedGroupAccountingAccounts = [.. Context.AutoMapper.Map<ObservableCollection<AccountingAccountGroupDTO>>(SelectedGroup.AccountingAccounts)];
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
                var selectedGroupAccountIds = SelectedGroup is null ? [] : SelectedGroup.AccountingAccounts.Select(a => a.Id).ToList();
                var selectedGroupAccountingAccountIds = SelectedGroup is null ? [] : SelectedGroupAccountingAccounts.Select(a => a.Id).ToList();
                if(!selectedGroupAccountIds.SequenceEqual(selectedGroupAccountingAccountIds)) return false;
                return true;
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
            try
            {
                IsBusy = true;
                List<int> accountingAccountsIds = [.. _selectedGroupAccountingAccountsShadow.Select(x => x.Id)];
                string query = @"
                mutation($data: UpdateAccountingAccountGroupInput!, $id: Int!){
                  UpdateResponse: updateAccountingAccountGroup(data: $data, id: $id){
                    id
                    name
                    key
                    accountingAccounts{
                      id
                      code
                      name
                    }
                  }
                }";

                dynamic variables = new ExpandoObject();
                variables.data = new ExpandoObject();
                variables.data.name = SelectedGroup.Name;
                variables.data.key = SelectedGroup.Key;
                variables.data.accountingAccountsIds = accountingAccountsIds;
                variables.id = SelectedGroup.Id;
                AccountingAccountGroupGraphQLModel result = await AccountingAccountGroupService.Update(query, variables);
                SelectedGroup.AccountingAccounts = result.AccountingAccounts;
                NotifyOfPropertyChange(nameof(AccountingAccountGroupComboBoxIsEnabled));
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

        public void DeleteAccountingAccounts()
        {
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
            _ = Task.Run(() => InitializeAsync());
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

        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;
                string query = @"
                query ($accountingAccountFilter: AccountingAccountFilterInput) {
                  accountingAccountGroups {
                    id
                    name
                    key
                    accountingAccounts {
                      id
                      name
                      code
                    }
                  }
                  accountingAccounts(filter: $accountingAccountFilter) {
                    id
                    code
                    name
                  }
                }
                ";

                dynamic variables = new ExpandoObject();
                variables.accountingAccountFilter = new ExpandoObject();
                variables.accountingAccountFilter.code = new ExpandoObject();
                variables.accountingAccountFilter.code.@operator = new List<string>() { "length", ">=" };
                variables.accountingAccountFilter.code.value = 4;
                AccountingAccountGroupDataContext result = await AccountingAccountGroupService.GetDataContext<AccountingAccountGroupDataContext>(query, variables);
                Groups = [.. result.AccountingAccountGroups];
                SelectedGroup = Groups.First();
                AccountingAccounts = [.. Context.AutoMapper.Map<ObservableCollection<AccountingAccountGroupDTO>>(result.AccountingAccounts)];
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
    }
}
