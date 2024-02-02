using Common.Extensions;
using Extensions;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using GraphQL.Client.Http;
using Models.Books;
using NetErp.Books.AccountingAccounts.DTO;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Extensions.Books;
using System.Windows.Navigation;
using DevExpress.Mvvm.POCO;

namespace NetErp.Books.AccountingAccounts.ViewModel
{
    public class AccountPlanMasterViewModel : ViewModelBase
    {
        #region "Variables Privadas"
        public List<AccountingAccountGraphQLModel> accounts = [];
        //private ICommand _editComand;
        //private ICommand _createCommand;
        //private ICommand _deleteCommand;
        //public /*IBooksAccountingAccount*/ BooksAccountingAccount;
        public IGenericDataAccess<AccountingAccountGraphQLModel> AccountingAccountService = null!;
        public DevExpress.Mvvm.INavigationService NavigationService { get; } = null!;

        #endregion

        #region "Propiedades"

        //private IEventAggregator _eventAggregator;

        //public ICommand NavigateToAccountingPlanDetailViewCommand { get; } = null!;

        //private AccountPlanViewModel _context;
        //public AccountPlanViewModel Context
        //{
        //    get { return _context; }
        //    set
        //    {
        //        SetValue(ref _context, value);
        //    }
        //}

        private ObservableCollection<AccountingAccountDTO> _accountingAccounts = [];
        public ObservableCollection<AccountingAccountDTO> AccountingAccounts
        {
            get { return _accountingAccounts; }
            set
            {
                SetValue(ref _accountingAccounts, value);
            }
        }

        private AccountingAccountDTO _selectedItem = new();
        public AccountingAccountDTO SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                SetValue(ref _selectedItem, value);
            }
        }

        //public ICommand EditCommand
        //{
        //    get
        //    {
        //        if (_editComand == null)
        //        {
        //            _editComand = new RelayCommand(CanEdit, Edit);
        //        }
        //        return _editComand;
        //    }
        //}

        //public ICommand CreateCommand
        //{
        //    get
        //    {
        //        if (_createCommand == null)
        //            _createCommand = new RelayCommand(CanCreate, Create);
        //        return _createCommand;
        //    }
        //}

        //public ICommand DeleteCommand
        //{
        //    get
        //    {
        //        if (_deleteCommand == null)
        //            _deleteCommand = new RelayCommand(CanDelete, Delete);
        //        return _deleteCommand;
        //    }
        //}

        // Is Busy
        private bool _isBusy = false;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (_isBusy != value)
                {
                    SetValue(ref _isBusy, value);
                }
            }
        }

        #endregion

        public ObservableCollection<AccountingAccountDTO> PopulateAccountingAccountDTO(List<AccountingAccountGraphQLModel> accounts)
        {
            Dictionary<String, AccountingAccountDTO> parents = new Dictionary<string, AccountingAccountDTO>();
            ObservableCollection<AccountingAccountDTO> accountsDTO = [];
            // Ordenamos la lista en funcion a la longitud del codigo
            // var sorted = from item in accounts orderby item.Code.Trim().Length ascending select item;
            try
            {
                var sorted = from item
                     in accounts
                             where item.Code.Trim().Length == 1
                             orderby item.Code ascending
                             select item;

                foreach (AccountingAccountGraphQLModel account in sorted)
                {
                    string parent_code = string.Empty;
                    parent_code = account.Code.Trim().Length switch
                    {
                        1 => account.Code.Trim(),
                        2 => account.Code.Trim().Substring(0, 1),
                        4 => account.Code.Trim().Substring(0, 2),
                        6 => account.Code.Trim().Substring(0, 4),
                        _ => account.Code.Trim().Substring(0, 6) // Default
                    };

                    if (!parents.ContainsKey(parent_code))
                    {
                        AccountingAccountDTO _account = new()
                        {
                            Id = account.Id,
                            Code = account.Code.Trim(),
                            Name = account.Name.Trim(),
                            Context = this,
                            Childrens = [new AccountingAccountDTO() { IsDummyChild = true, Childrens = [], Code = "000", Name = "Fucking Dummy" }]
                        };
                        accountsDTO.Add(_account);
                        parents.Add(parent_code, _account);
                    }

                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => Xceed.Wpf.Toolkit.MessageBox.Show($"{this.GetType().Name}.{(currentMethod is null ? "PopulateAccountingAccountDTO" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
 
            }
            return accountsDTO;
        }

        public async Task DeleteAccountFromAccountsDTO(string code)
        {
            try
            {
                AccountingAccountDTO? Lv1 = null;
                AccountingAccountDTO? Lv2 = null;
                AccountingAccountDTO? Lv3 = null;
                AccountingAccountDTO? Lv4 = null;
                AccountingAccountDTO? Lv5 = null;

                if (code.Length >= 1) Lv1 = this.AccountingAccounts.Where(x => x.Code == code.Substring(0, 1)).FirstOrDefault();
                if (Lv1 == null) throw new Exception("LV1 no puede ser null");
                if (code.Length >= 2) Lv2 = Lv1.Childrens.Where(x => x.Code == code.Substring(0, 2)).FirstOrDefault();
                if (Lv2 == null) throw new Exception("LV2 no puede ser null");
                if (code.Length >= 4) Lv3 = Lv2.Childrens.Where(x => x.Code == code.Substring(0, 4)).FirstOrDefault();
                if (Lv3 == null) throw new Exception("LV3 no puede ser null");
                if (code.Length >= 6) Lv4 = Lv3.Childrens.Where(x => x.Code == code.Substring(0, 6)).FirstOrDefault();
                if (Lv4 == null) throw new Exception("LV4 no puede ser null");
                if (code.Length >= 8) Lv5 = Lv4.Childrens.Where(x => x.Code == code.Substring(0, 8)).FirstOrDefault();
                if (Lv5 == null) throw new Exception("LV5 no puede ser null");

                if (code.Length == 1)
                {
                    await App.Current.Dispatcher.Invoke(async () =>
                    {
                        string query = @"
                    mutation ($id: ID!) {
                      deleteAccountingAccount (id: $id) {
                        id
                        code
                        name
                        margin
                        marginBasis
                        nature
                      }
                    }";
                       await this.AccountingAccountService.Delete(query, new { Lv1.Id });
                    });
                }
                else
                {
                    if (code.Length == 2)
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            Lv1.Childrens.Remove(Lv2);
                        });
                    }
                    else
                    {
                        if (code.Length == 4)
                        {
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                Lv2.Childrens.Remove(Lv3);
                            });
                        }
                        else
                        {
                            if (code.Length == 6)
                            {
                                App.Current.Dispatcher.Invoke(() =>
                                {
                                    Lv3.Childrens.Remove(Lv4);
                                });
                            }
                            else
                            {
                                App.Current.Dispatcher.Invoke(() =>
                                {
                                    Lv4.Childrens.Remove(Lv5);
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => Xceed.Wpf.Toolkit.MessageBox.Show($"{this.GetType().Name}.{(currentMethod is null ? "DeleteAccountFromAccountsDTO" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
               
            }
        }

        //public static async Task<AccountPlanMasterViewModel> Create(AccountPlanViewModel context)
        //{
        //    try
        //    {
        //        var _instance = new AccountPlanMasterViewModel(context);
        //        await _instance.Initialize();
        //        return _instance;

        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        public async Task Initialize()
        {
            try
            {
                string query = @"
                query ($where: AccountingAccountsWhereInput) {
                  accountingAccounts(where: $where) {
                    id
                    code
                    name
                    margin
                    marginBasis
                    nature
                  }
                }";

                // Loading Data 
                var result = await this.AccountingAccountService.GetList(query, new object());
                accounts = new List<AccountingAccountGraphQLModel>(result);
                this.AccountingAccounts = PopulateAccountingAccountDTO(accounts);
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null) {
                    App.Current.Dispatcher.Invoke(() => Xceed.Wpf.Toolkit.MessageBox.Show($"{this.GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));                    
                } else {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => Xceed.Wpf.Toolkit.MessageBox.Show($"{this.GetType().Name}.{(currentMethod is null ? "Initialize" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));

            }
        }

        [Command]
        public void NavigateToAccountingPlanDetailView() 
        { 
            //AccountPlanDetailViewModel.SetParentViewModel(this);
            NavigationService.Navigate("AccountPlanDetailView", this, this, true); 
        }

        public bool CanNavigateToAccountingPlanDetailView() => true;

        //public AccountPlanDetailViewModel AccountPlanDetailViewModel { get; set; }

        //AccountPlanViewModel context,
        public AccountPlanMasterViewModel(IGenericDataAccess<AccountingAccountGraphQLModel> accountingAccountService, INavigationService navigationService)
        {
            try
            {
                //DevExpress.Mvvm.INavigationService navigationService
                //this.Context = context;
                //this._eventAggregator = IoC.Get<IEventAggregator>();
                //this._eventAggregator.SubscribeOnUIThread(this);
                Messenger.Default.Register<AccountingAccountCreateListMessage>(this, OnAccountingAccountCreateListMessage);
                Messenger.Default.Register<AccountingAccountUpdateMessage>(this, OnAccountingAccountUpdateMessage);

                // Everithing else
                AccountingAccountService = accountingAccountService;
                NavigationService = navigationService;
                //AccountPlanDetailViewModel = new(accountingAccountService, navigationService);
                //NavigateToAccountingPlanDetailViewCommand = new DelegateCommand(NavigateToAccountingPlanDetailView);
                //this.BooksAccountingAccount = IoC.Get<IBooksAccountingAccount>();
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => Xceed.Wpf.Toolkit.MessageBox.Show($"{this.GetType().Name}.{(currentMethod is null ? "AccountPlanMasterViewModel" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        #region "Methods"

        /// <summary>
        /// Carga los hijos de una cuenta contable
        /// </summary>
        /// <param name="parent">Code del padre para carga de hijos</param>
        /// <param name="accounts">Instancia de List<BooksAccountingAccountModel> con las cuentas del PUC</param>
        public void LoadChildren(AccountingAccountDTO parent, List<AccountingAccountGraphQLModel> accounts)
        {
            try
            {
                string parentCode = parent.Code.Trim();
                if (parent.Childrens[0].IsDummyChild)
                {
                    // Calcular el nivel de codigos que queremos
                    int nLevel = 0;
                    nLevel = parent.Code.Trim().Length switch
                    {
                        1 => 2,
                        2 => 4,
                        4 => 6,
                        _ => 8,
                    };

                    // Remove DummyChild
                    App.Current.Dispatcher.Invoke((System.Action)delegate
                    {
                        parent.Childrens.Remove(parent.Childrens[0]);
                    });

                    // Obtenemos todos los hijos del padre en cuestion
                    var sorted = from account
                                 in accounts
                                 where account.Code.Trim().Length == nLevel && account.Code.Trim().Substring(0, parentCode.Trim().Length) == parentCode.Trim()
                                 orderby account.Code ascending
                                 select account;

                    foreach (AccountingAccountGraphQLModel account in sorted)
                    {
                        AccountingAccountDTO _account = new AccountingAccountDTO()
                        {
                            Id = account.Id,
                            Code = account.Code.Trim(),
                            Name = account.Name.Trim(),
                            Context = this,
                            Childrens = parent.Code.Trim().Length == 6 ? [] : [ new AccountingAccountDTO() { IsDummyChild = true, Childrens = [], Code = "000", Name = "Fucking Dummy" }]
                        };

                        App.Current.Dispatcher.Invoke((System.Action)delegate
                        {
                            parent.Childrens.Add(_account);
                        });
                    }

                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => Xceed.Wpf.Toolkit.MessageBox.Show($"{this.GetType().Name}.{(currentMethod is null ? "LoadChildren" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));    
            }
        }

        /// <summary>
        /// Busca una cuenta contable en el arbol
        /// </summary>
        /// <param name="content"></param>
        public void SearchAccount(string content)
        {

            try
            {
                IEnumerable<AccountingAccountDTO>? lv_1 = null;
                IEnumerable<AccountingAccountDTO>? lv_2 = null;
                IEnumerable<AccountingAccountDTO>? lv_3 = null;
                IEnumerable<AccountingAccountDTO>? lv_4 = null;
                IEnumerable<AccountingAccountDTO>? lv_5 = null;
                string lv1 = content.Trim().Length >= 1 ? content.Substring(0, 1) : "";
                string lv2 = content.Trim().Length >= 2 ? content.Substring(0, 2) : "";
                string lv3 = content.Trim().Length >= 4 ? content.Substring(0, 4) : "";
                string lv4 = content.Trim().Length >= 6 ? content.Substring(0, 6) : "";
                string lv5 = content.Trim().Length >= 8 ? content : "";

                // Leve1
                lv_1 = from account
                in _accountingAccounts
                       where account.Code.Trim() == lv1
                       select account;

                if (lv_1 == null)
                {
                    return;
                }

                // Level2
                if (!string.IsNullOrEmpty(lv2))
                {
                    if (lv_1.First().Childrens[0].IsDummyChild)
                    {
                        LoadChildren(lv_1.First(), accounts);
                    }
                    lv_1.First().IsExpanded = true;
                    lv_2 = from account
                            in lv_1.First().Childrens
                           where account.Code.Trim() == lv2
                           select account;
                }
                else
                {
                    lv_1.First().IsSelected = true;
                    return;
                }

                // Level3
                if (!string.IsNullOrEmpty(lv3))
                {
                    if (lv_2.First().Childrens[0].IsDummyChild)
                    {
                        LoadChildren(lv_2.First(), accounts);
                    }
                    lv_2.First().IsExpanded = true;
                    lv_3 = from account
                            in lv_2.First().Childrens
                           where account.Code.Trim() == lv3
                           select account;
                }
                else
                {
                    lv_2.First().IsSelected = true;
                    return;
                }

                // Level4
                if (!string.IsNullOrEmpty(lv4))
                {
                    if (lv_3.First().Childrens[0].IsDummyChild)
                    {
                        LoadChildren(lv_3.First(), accounts);
                    }
                    lv_3.First().IsExpanded = true;
                    lv_4 = from account
                           in lv_3.First().Childrens
                           where account.Code.Trim() == lv4
                           select account;
                }
                else
                {
                    lv_3.First().IsSelected = true;
                    return;
                }

                if (!string.IsNullOrEmpty(lv5))
                {
                    if (lv_4.First().Childrens[0].IsDummyChild)
                    {
                        LoadChildren(lv_4.First(), accounts);
                    }
                    lv_4.First().IsExpanded = true;
                    lv_5 = from account
                           in lv_4.First().Childrens
                           where account.Code.Trim() == lv5
                           select account;
                }
                else
                {
                    lv_4.First().IsSelected = true;
                    return;
                }

                if (lv_5 != null)
                {
                    lv_5.First().IsSelected = true;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => Xceed.Wpf.Toolkit.MessageBox.Show($"{this.GetType().Name}.{(currentMethod is null ? "SearchAccount" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }

        }

        #endregion

        #region "Commands"

        // TODO parameter type revision
        [Command]
        public void Create(object parameter)
        {
            //await this.Context.ActivateDetailViewModel("");
            NavigationService.Navigate("AccountPlanDetailView", "", this);

        }

        public bool CanCreate(object parameter)
        {
            return true;
        }

        [Command]
        public void Edit(object code)
        {
            //await this.Context.ActivateDetailViewModel((string)code);
            NavigationService.Navigate("AccountPlanDetailView", (string)code, this);
        }

        public bool CanEdit(object code)
        {
            return true;
        }

        [Command]
        public async void Delete(object id)
        {
            try
            {

                this.IsBusy = true;
                //TODO this.Refresh();

                string query = @"
                query($id:ID!){
                  canDeleteAccountingAccount(id:$id) {
                    canDelete
                    message
                  }
                }";

                object variables = new {  Id = (int)id };

                var validation = await this.AccountingAccountService.CanDelete(query, variables);

                this.IsBusy = false;

                if (validation.CanDelete)
                {
                    MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show("¿ Confirma que desea eliminar la cuenta contable ?", "Atención !", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    App.Current.Dispatcher.Invoke(() => Xceed.Wpf.Toolkit.MessageBox.Show("Esta cuenta contable no puede ser eliminada" +
                        (char)13 + (char)13 + validation.Message, "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
                    return;
                }

                this.IsBusy = true;
                //TODO this.Refresh();
                var deletedAccountingAccount = await Task.Run(() => this.ExecuteDelete((int)id));
                Messenger.Default.Send(new AccountingAccountDeleteMessage() { DeletedAccountingAccount = deletedAccountingAccount });    
                //await this._eventAggregator.PublishOnUIThreadAsync(new AccountingAccountDeleteMessage() { DeletedAccountingAccount = deletedAccountingAccount });
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => Xceed.Wpf.Toolkit.MessageBox.Show($"{this.GetType().Name}.{(currentMethod is null ? "Delete" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                this.IsBusy = false;
            }
        }
        public bool CanDelete(object id)
        {
            return true;
        }
        public async Task<AccountingAccountGraphQLModel> ExecuteDelete(int id)
        {
            try
            {
                string query = @"
                mutation ($id: ID!) {
                  deleteAccountingAccount (id: $id) {
                    id
                    code
                    name
                    margin
                    marginBasis
                    nature
                  }
                }";

                object variables = new
                {
                    Id = (int)id
                };

                var deletedAccountingAccount = await AccountingAccountService.Delete(query, variables);
                RemoveAccountInMemory(accounts, (int)id);
                await DeleteAccountFromAccountsDTO(deletedAccountingAccount.Code);

                return deletedAccountingAccount;
            }
            catch (Exception)
            {
                throw;
            }
        }



        /// <summary>
        /// Recibe el resultado de la creacion de una cuenta contable, teniendo en cuenta que la creacion de un auxiliar implica la creacion de sus padres
        /// </summary>
        /// <param name="message">Instancia de List<BooksAccountingAccountModel> con la lista de cuentas creadas, auxiliar y padres</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// 

        //.ContinueWith(antecedent => this.Context.ActivateMasterViewModel())
        void OnAccountingAccountCreateListMessage(AccountingAccountCreateListMessage message)
        {
             Task.Run(() => accounts.AddRange(message.CreatedAccountingAccountList))
                .ContinueWith(antecedent => AccountingAccounts = PopulateAccountingAccountDTO(accounts))
                .ContinueWith(antecedent => SearchAccount(message.CreatedAccountingAccountList[message.CreatedAccountingAccountList.Count - 1].Code));
        }

        //public Task HandleAsync(AccountingAccountCreateListMessage message, CancellationToken cancellationToken)
        //{
        //    return Task.Run(() => this.Context.AccountPlanMasterViewModel.accounts.AddRange(message.CreatedAccountingAccountList))
        //        .ContinueWith(antecedent => this.AccountingAccounts = this.PopulateAccountingAccountDTO(this.Context.AccountPlanMasterViewModel.accounts))
        //        .ContinueWith(antecedent => this.Context.ActivateMasterViewModel())
        //        .ContinueWith(antecedent => this.searchAccount(message.CreatedAccountingAccountList[message.CreatedAccountingAccountList.Count - 1].Code));
        //}

        /// <summary>
        /// Recibe el resultado de la actualizacion de una cuenta contable existente
        /// </summary>
        /// <param name="message">Instancia de BooksAccountingAccountModel con los datos de la cuenta actualizados</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// 

        //.ContinueWith(antecedent => this.Context.ActivateMasterViewModel())
        void OnAccountingAccountUpdateMessage(AccountingAccountUpdateMessage message) 
        {
            Task.Run(() => accounts.Replace(message.UpdatedAccountingAccount))
                .ContinueWith(antecedent => AccountingAccounts = PopulateAccountingAccountDTO(accounts))
                .ContinueWith(antecedent => SearchAccount(message.UpdatedAccountingAccount.Code));
        }
        //public Task HandleAsync(AccountingAccountUpdateMessage message, CancellationToken cancellationToken)
        //{
        //    return Task.Run(() => this.Context.AccountPlanMasterViewModel.accounts.Replace(message.UpdatedAccountingAccount))
        //        .ContinueWith(antecedent => this.AccountingAccounts = this.PopulateAccountingAccountDTO(this.Context.AccountPlanMasterViewModel.accounts))
        //        .ContinueWith(antecedent => this.Context.ActivateMasterViewModel())
        //        .ContinueWith(antecedent => this.searchAccount(message.UpdatedAccountingAccount.Code));
        //}

        private void RemoveAccountInMemory(List<AccountingAccountGraphQLModel> accounts, int id)
        {
            try
            {
                AccountingAccountGraphQLModel? account = accounts.FirstOrDefault(x => x.Id == id);
                if (account != null) accounts.Remove(account);
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => Xceed.Wpf.Toolkit.MessageBox.Show($"{this.GetType().Name}.{(currentMethod is null ? "RemoveAccountInMemory" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        #endregion

    }
}
