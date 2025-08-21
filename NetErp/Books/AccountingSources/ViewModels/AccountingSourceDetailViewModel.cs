using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Dictionaries;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Helpers;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using static DevExpress.Drawing.Printing.Internal.DXPageSizeInfo;

namespace NetErp.Books.AccountingSources.ViewModels
{
    public class AccountingSourceDetailViewModel : Screen
    {
        #region Propiedades


        private readonly IRepository<AccountingSourceGraphQLModel> _accountingSourceService;

        // Context
        private AccountingSourceViewModel _context;
        public AccountingSourceViewModel Context
        {
            get { return _context; }
            set
            {
                if (_context != value)
                {
                    _context = value;
                    NotifyOfPropertyChange(nameof(Context));
                }
            }
        }

        // Id
        private int _id;
        public int Id
        {
            get { return _id; }
            set
            {
                if (_id != value)
                {
                    _id = value;
                    NotifyOfPropertyChange(nameof(Id));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        // Codigo corto
        private string _code;
        public string Code
        {
            get { return _code; }
            set
            {
                _code = value;
                NotifyOfPropertyChange(nameof(Code));
                NotifyOfPropertyChange(nameof(FullCode));
                NotifyOfPropertyChange(nameof(AnnulmentCode));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        // Codigo Largo
        public string FullCode
        {
            get
            {
                return $"_{(this.IsSystemSource ? "S" : "U")}_{this.Code}";
            }
        }

        // Codigo de Anulacion
        public string AnnulmentCode
        {
            get
            {
                return $"{this.SelectedAnnulmentType}{(this.IsSystemSource ? "S" : "U")}_{this.Code}";
            }
        }

        // Nombre de la fuente contable
        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    NotifyOfPropertyChange(nameof(Name));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        // IsSystemSource 
        private bool _isSystemSource;
        public bool IsSystemSource
        {
            get { return _isSystemSource; }
            set
            {
                if (_isSystemSource != value)
                {
                    _isSystemSource = value;
                    NotifyOfPropertyChange(nameof(IsSystemSource));
                }
            }
        }

        // IsKardexTransaction
        private bool _isKardexTransaction;
        public bool IsKardexTransaction
        {
            get { return _isKardexTransaction; }
            set
            {
                if (_isKardexTransaction != value)
                {
                    _isKardexTransaction = value;
                    NotifyOfPropertyChange(nameof(IsKardexTransaction));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        // is annulled with additional document
        public bool IsAnnulledWithAdditionalDocument { get { return this.SelectedAnnulmentType == 'A'; } }

        // Si es un nuevo registro
        public bool IsNewRecord
        {
            get { return (this.Id == 0); }
        }

        // Diccionario de tipos de anulaciones
        public Dictionary<char, string> AnnulmentTypeDictionary
        {
            get { return Dictionaries.BooksDictionaries.AnnulmentTypeDictionary; }
        }

        // SelectedAnnulmentType { Valor por defecto : X, Sin documento adicional }
        private char _selectedAnnulmentType = 'X';
        public char SelectedAnnulmentType
        {
            get { return _selectedAnnulmentType; }
            set
            {
                if (_selectedAnnulmentType != value)
                {
                    _selectedAnnulmentType = value;
                    NotifyOfPropertyChange(nameof(SelectedAnnulmentType));
                    NotifyOfPropertyChange(nameof(IsAnnulledWithAdditionalDocument));
                    NotifyOfPropertyChange(nameof(AnnulmentCode));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        // Diccionario de flujo de kardex : Entrada o Salida
        public Dictionary<char, string> KardexFlowDictionary
        {
            get { return InventoriesDictionaries.KardexFlowDictionary; }
        }

        // SelectedKardexFlow
        private char _selectedKardexFlow = Dictionaries.InventoriesDictionaries.KardexFlowDictionary.FirstOrDefault().Key;
        public char SelectedKardexFlow
        {
            get { return _selectedKardexFlow; }
            set
            {
                if (_selectedKardexFlow != value)
                {
                    _selectedKardexFlow = value;
                    NotifyOfPropertyChange(nameof(SelectedKardexFlow));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        // Id Seleccionado en ComboBox Cuentas Contables
        private int _selectedAccountingAccountId = -1;
        public int SelectedAccountingAccountId
        {
            get { return _selectedAccountingAccountId; }
            set
            {
                if (_selectedAccountingAccountId != value)
                {
                    _selectedAccountingAccountId = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingAccountId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        // SelectedProcessTypeId combobox tipos de procesos
        private int _selectedProcessTypeId = -1;
        public int SelectedProcessTypeId
        {
            get { return _selectedProcessTypeId; }
            set
            {
                if (_selectedProcessTypeId != value)
                {
                    _selectedProcessTypeId = value;
                    NotifyOfPropertyChange(nameof(SelectedProcessTypeId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
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
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private ICommand _goBackCommand;
        public ICommand GoBackCommand
        {
            get
            {
                if (_goBackCommand is null) _goBackCommand = new RelayCommand(CanGoBack, GoBack);
                return _goBackCommand;
            }
        }

        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(Save, CanSave);
                return _saveCommand;
            }
        }
        
        #endregion

        #region Colecciones

        // Cuentas contables
        private ObservableCollection<AccountingAccountPOCO> _auxiliaryAccountingAccounts;
        public ObservableCollection<AccountingAccountPOCO> AuxiliaryAccountingAccounts
        {
            get { return _auxiliaryAccountingAccounts; }
            set
            {
                if (_auxiliaryAccountingAccounts != value)
                {
                    _auxiliaryAccountingAccounts = value;
                    NotifyOfPropertyChange(nameof(AuxiliaryAccountingAccounts));
                }
            }
        }

        // Tipos de procesos
        private ObservableCollection<ProcessTypeGraphQLModel> _processTypes;
        public ObservableCollection<ProcessTypeGraphQLModel> ProcessTypes
        {
            get { return _processTypes; }
            set
            {
                if (_processTypes != value)
                {
                    _processTypes = value;
                    NotifyOfPropertyChange(nameof(ProcessTypes));

                }
            }
        }

        #endregion

        #region POCO's
        public class AccountingAccountPOCO
        {
            public int Id { get; set; } = 0;
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string FullName { get { return $"{Code} - {Name}"; } }
        }
        #endregion

        public AccountingSourceDetailViewModel(AccountingSourceViewModel context, IRepository<AccountingSourceGraphQLModel> accountingSourceService)
        {
            // Contexto
            this.Context = context;
            this._accountingSourceService = accountingSourceService;
            // Cargar cuentas contables
            var auxiliaryAccounts = from account in this.Context.AccountingSourceMasterViewModel.AccountingAccounts
                                    select new AccountingAccountPOCO { Id = account.Id, Code = account.Code, Name = account.Name };
            this.AuxiliaryAccountingAccounts = new ObservableCollection<AccountingAccountPOCO>(auxiliaryAccounts);
            // Cargar tipos de procesos
            //this.ProcessTypes = new ObservableCollection<ProcessTypeGraphQLModel>(ProcessTypeService.GetList());
            var joinable = new JoinableTaskFactory(new JoinableTaskContext());
            joinable.Run(async () => await Initialize());
        }

        public async Task Initialize()
        {
            ProcessTypes = Context.ProcessTypes;
        }
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.SetFocus(() => Code);
        }

        //public async Task<IGenericDataAccess<AccountingSourceGraphQLModel>.PageResponseType> LoadPage()
        //{
        //    try
        //    {
        //        string queryPage;
        //        queryPage = @"query($filter: AccountingSourceFilterInput) {
        //            PageResponse: accountingSourcePage(filter:$filter) {
        //                count
        //                rows {
        //                    id
        //                    code
        //                  fullCode
        //                  annulmentCode
        //                  name
        //                  isSystemSource
        //                  annulmentCharacter
        //                  isKardexTransaction
        //                  kardexFlow
        //                  accountingAccount {
        //                        id
        //                  }
        //                    processType {
        //                        id
        //                        name
        //                      module {
        //                            id
        //                            name
        //                    }
        //                    }
        //                }
        //            }
        //        }
        //        ";
        //        dynamic variables = new ExpandoObject();
        //        variables.filter = new ExpandoObject();
        //        variables.filter.Annulment = false;
        //        return await AccountingSourceService.GetPage(queryPage, variables);
        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
        //}

        public async Task Save()
        {
            try
            {
                this.IsBusy = true;
                this.Refresh();
                var result = await ExecuteSave();
                if (IsNewRecord)
                {
                    await this.Context.EventAggregator.PublishOnUIThreadAsync(new AccountingSourceCreateMessage() { CreatedAccountingSource = Context.AutoMapper.Map<AccountingSourceDTO>(result)});
                }
                else
                {
                    await this.Context.EventAggregator.PublishOnUIThreadAsync(new AccountingSourceUpdateMessage() { UpdatedAccountingSource = Context.AutoMapper.Map<AccountingSourceDTO>(result)});
                }
                Context.EnableOnViewReady = false;
                await this.Context.ActivateMasterViewAsync();
                
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{exGraphQL.Message}.\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void CleanUpControls()
        {
            Id = 0;
            SelectedProcessTypeId = -1;
            SelectedAccountingAccountId = -1;
            SelectedAnnulmentType = 'X';
            SelectedKardexFlow = 'E';
            Code = "";
            Name = "";
            IsKardexTransaction = false;
            
        }

        public async Task<AccountingSourceGraphQLModel> ExecuteSave()
        {
            // Guardar datos
            try
            {
                if (this.Id == 0)
                {
                    string query = @"
				mutation ($data: CreateAccountingSourceInput!) {
				  CreateResponse: createAccountingSource(data: $data) {
					id
					code
					fullCode
					annulmentCode
					name
					isSystemSource
					annulmentCharacter
					isKardexTransaction
					kardexFlow
					accountingAccount {
					  id
					}
					processType {
					  id
					  name
					  module {
						id
						name
					  }
					}
				  }
				}";

                    dynamic variables = new ExpandoObject();
                    variables.Data = new ExpandoObject();
                    variables.Data.Code = Code;
                    variables.Data.FullCode = FullCode;
                    variables.Data.Name = Name;
                    variables.Data.AnnulmentCode = AnnulmentCode;
                    variables.Data.IsSystemSource = IsSystemSource;
                    variables.Data.AnnulmentCharacter = SelectedAnnulmentType;
                    variables.Data.IsKardexTransaction = IsKardexTransaction;
                    variables.Data.KardexFlow = SelectedKardexFlow;
                    variables.Data.AccountingAccountId = SelectedAccountingAccountId;
                    variables.Data.ProcessTypeId = SelectedProcessTypeId;
                    variables.Data.CreatedBy = SessionInfo.UserEmail;
                    var result = await _accountingSourceService.CreateAsync(query, variables);
                    return result;
                }
                else
                {
                    string query = @"
				mutation ($data: UpdateAccountingSourceInput!, $id: Int!) {
				  UpdateResponse: updateAccountingSource(data: $data, id: $id) {
					id
					code
					fullCode
					annulmentCode
					name
					isSystemSource
					annulmentCharacter
					isKardexTransaction
					kardexFlow
					accountingAccount {
					  id
					  code
					}
					processType {
					   id
					   name
					   module {
						 id
						 name
					  }
					}
				  }
				}";
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                variables.Id = Id;
                variables.Data.Code = Code;
                variables.Data.AnnulmentCode = AnnulmentCode;
                variables.Data.Name = Name;
                variables.Data.FullCode = FullCode;
                variables.Data.AnnulmentCharacter = SelectedAnnulmentType;
                variables.Data.AccountingAccountId = SelectedAccountingAccountId;
                var result = await _accountingSourceService.UpdateAsync(query, variables);
                return result;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool CanSave
        {
            get
            {
                if (this.IsBusy) return false;
                if (this.SelectedProcessTypeId == -1) return false;
                if (string.IsNullOrEmpty(this.Code) || string.IsNullOrEmpty(this.Name)) return false;
                if (this.Code.Trim().Length != 3) return false;
                if (this.IsKardexTransaction)
                {
                    if (this.SelectedAccountingAccountId == -1) return false;
                }
                return true;
            }
        }
        public void GoBack(object p)
        {
            _ = Task.Run(() => Context.ActivateMasterViewAsync());
        }

        public bool CanGoBack(object p)
        {
            return !IsBusy;
        }
    }
}
