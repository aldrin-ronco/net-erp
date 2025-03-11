using Common.Extensions;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using GraphQL.Client.Http;
using Models.Books;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Extensions.Books;
using DevExpress.Xpf.Core;
using System.Dynamic;
using System.Threading;
using Caliburn.Micro;
using NetErp.Books.AccountingAccounts.DTO;
using Common.Helpers;

namespace NetErp.Books.AccountingAccounts.ViewModels
{
    public class AccountPlanMasterViewModel : ViewModelBase
    {

        #region "Propiedades"

        public List<AccountingAccountGraphQLModel> accounts = [];
        public readonly IGenericDataAccess<AccountingAccountGraphQLModel> AccountingAccountService = IoC.Get<IGenericDataAccess<AccountingAccountGraphQLModel>>();

        private AccountPlanViewModel _context;

        public AccountPlanViewModel Context
        {
            get { return _context; }
            set 
            {
                SetValue(ref _context, value); 
            }
        }



        private ObservableCollection<AccountingAccountDTO> _accountingAccounts = [];
        public ObservableCollection<AccountingAccountDTO> AccountingAccounts
        {
            get { return _accountingAccounts; }
            set
            {
                SetValue(ref _accountingAccounts, value);
            }
        }

        private AccountingAccountDTO? _selectedItem = null;
        public AccountingAccountDTO? SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                SetValue(ref _selectedItem, value);
            }
        }    

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

        #region "Constructores"
        public AccountPlanMasterViewModel(AccountPlanViewModel context)
        {
            try
            {
                Messenger.Default.Register<AccountingAccountCreateListMessage>(this, OnAccountingAccountCreateListMessage);
                Messenger.Default.Register<AccountingAccountUpdateMessage>(this, OnAccountingAccountUpdateMessage);
                Messenger.Default.Register<AccountingAccountDeleteMessage>(this, OnAccountingAccountDeleteMessage);
                this.Context = context;
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "AccountPlanMasterViewModel" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }
        #endregion

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
                throw new Exception("test");
                string parentCode = parent.Code.Trim();
                if (parent.Childrens[0].IsDummyChild)
                {
                    // Calcular la longitud del código de los hijos en función de la longitud del código del padre
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
                        AccountingAccountDTO _account = new()
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

                throw new AsyncException(innerException: ex);
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
                    SelectedItem = lv_1.First();
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
                    SelectedItem = lv_2.First();
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
                    SelectedItem = lv_3.First();
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
                    SelectedItem = lv_4.First();
                    return;
                }

                if (lv_5 != null)
                {
                    lv_5.First().IsSelected = true;
                    SelectedItem = lv_5.First();
                }
            }
            catch (AsyncException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "PopulateAccountingAccountDTO" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
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

                if (code.Length >= 1) 
                {
                    Lv1 = this.AccountingAccounts.Where(x => x.Code == code.Substring(0, 1)).FirstOrDefault();
                };
                if (code.Length >= 2) 
                {
                    if (Lv1 == null) throw new Exception("LV1 no puede ser null");
                    Lv2 = Lv1.Childrens.Where(x => x.Code == code.Substring(0, 2)).FirstOrDefault();
                };
                if (code.Length >= 4) 
                {
                    if (Lv2 == null) throw new Exception("LV2 no puede ser null");
                    Lv3 = Lv2.Childrens.Where(x => x.Code == code.Substring(0, 4)).FirstOrDefault();
                };
                if (code.Length >= 6) 
                { 
                    if (Lv3 == null) throw new Exception("LV3 no puede ser null");
                    Lv4 = Lv3.Childrens.Where(x => x.Code == code.Substring(0, 6)).FirstOrDefault(); 
                };
                if (code.Length >= 8) 
                {
                    if (Lv4 == null) throw new Exception("LV4 no puede ser null");
                    Lv5 = Lv4.Childrens.Where(x => x.Code == code.Substring(0, 8)).FirstOrDefault(); 
                    if (Lv5 == null) throw new Exception("LV5 no puede ser null");
                };

                if (code.Length == 1)
                {
                    await App.Current.Dispatcher.Invoke(async () =>
                    {
                        string query = @"
                    mutation ($id: Int!) {
                      DeleteResponse: deleteAccountingAccount (id: $id) {
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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteAccountFromAccountsDTO" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }
        public async Task<AccountingAccountGraphQLModel> ExecuteDelete(int id)
        {
            try
            {
                string query = @"
                mutation ($id: Int!) {
                  DeleteResponse: deleteAccountingAccount (id: $id) {
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

                return deletedAccountingAccount;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

        #region "Commands"

        [Command]
        public async void Initialize()
        {
            if (AccountingAccounts.Count > 0) return; // Execute initialize just once
            try
            {
                this.IsBusy = true;
                string query = @"
                query{
                  ListResponse : accountingAccounts{
                    id
                    code
                    name
                    nature
                    margin
                    marginBasis
                  }
                }"
;
                // Loading Data 
                var result = await this.AccountingAccountService.GetList(query, new object());
                accounts = new List<AccountingAccountGraphQLModel>(result);
                this.AccountingAccounts = PopulateAccountingAccountDTO(accounts);

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "Initialize" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                //await Task.Delay(1000);
                this.IsBusy = false;
            }
        }

        public bool CanInitialize()
        {
            return true;
        }

        // TODO parameter type revision
        [Command]
        public async Task Create(object parameter)
        {
            await Context.ActivateDetailViewModel("");
        }

        public bool CanCreate(object parameter)
        {
            return true;
        }

        [Command]
        public async Task Edit(object code)
        {
            //TODO edit wait indicator
            this.IsBusy = true;
            await Task.Run(()=> Context.ActivateDetailViewModel((string)code));
            this.IsBusy = false;
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
                string query = @"
                query($id: Int!){
                  CanDeleteModel: canDeleteAccountingAccount(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new {  Id = (int)id };

                var validation = await this.AccountingAccountService.CanDelete(query, variables);

                this.IsBusy = false;

                if (validation.CanDelete)
                {
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Atención!", text: "¿Confirma que desea eliminar la cuenta contable?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "Esta cuenta contable no puede ser eliminada" +
                        (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }

                this.IsBusy = true;
                var deletedAccountingAccount = await Task.Run(() => this.ExecuteDelete((int)id));
                Messenger.Default.Send(new AccountingAccountDeleteMessage() { DeletedAccountingAccount = deletedAccountingAccount });    
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "Delete" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
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

        #endregion

        #region "Messages"
        /// <summary>
        /// Recibe el resultado de la creacion de una cuenta contable, teniendo en cuenta que la creacion de un auxiliar implica la creacion de sus padres
        /// </summary>
        /// <param name="message">Instancia de List<BooksAccountingAccountModel> con la lista de cuentas creadas, auxiliar y padres</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// 

        void OnAccountingAccountCreateListMessage(AccountingAccountCreateListMessage message)
        {
            try
            {
                if(this.accounts.Count == 0) { return; }
                _ = Task.Run(() => accounts.AddRange(message.CreatedAccountingAccountList))
                  .ContinueWith(antecedent => AccountingAccounts = PopulateAccountingAccountDTO(accounts))
                  .ContinueWith(antecedent => SearchAccount(message.CreatedAccountingAccountList[message.CreatedAccountingAccountList.Count - 1].Code));
            }
            catch (AsyncException ex)
            {
                Execute.OnUIThread(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message ?? ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                });
            }
            catch (Exception ex)
            {
                Execute.OnUIThread(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                });
            }
        }

        /// <summary>
        /// Recibe el resultado de la actualizacion de una cuenta contable existente
        /// </summary>
        /// <param name="message">Instancia de BooksAccountingAccountModel con los datos de la cuenta actualizados</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// 

        void OnAccountingAccountUpdateMessage(AccountingAccountUpdateMessage message) 
        {
            IsBusy = true;
            if (this.accounts.Count == 0) { return; }
            Task.Run(() => accounts.Replace(message.UpdatedAccountingAccount))
                .ContinueWith(antecedent => AccountingAccounts = PopulateAccountingAccountDTO(accounts))
                .ContinueWith(antecedent => SearchAccount(message.UpdatedAccountingAccount.Code));
            IsBusy = false;
        }

        async void OnAccountingAccountDeleteMessage(AccountingAccountDeleteMessage message)
        {
            await DeleteAccountFromAccountsDTO(message.DeletedAccountingAccount.Code);
        }

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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "RemoveAccountInMemory" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }
        #endregion
    }
}
