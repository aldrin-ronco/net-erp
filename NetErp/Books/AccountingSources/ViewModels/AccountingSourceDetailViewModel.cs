using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DevExpress.XtraEditors.Filtering;
using Dictionaries;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
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
using static Amazon.S3.Util.S3EventNotification;
using static DevExpress.Drawing.Printing.Internal.DXPageSizeInfo;
using static Models.Global.GraphQLResponseTypes;
using Extensions.Global;

namespace NetErp.Books.AccountingSources.ViewModels
{
    //TODO revisión general de funcionamiento
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
        private string _shortCode;
        public string ShortCode
        {
            get { return _shortCode; }
            set
            {
                _shortCode = value;
                NotifyOfPropertyChange(nameof(ShortCode));
                NotifyOfPropertyChange(nameof(Code));
                this.TrackChange(nameof(Code));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        // Codigo Largo
        public string Code
        {
            get
            {
                return $"_{(this.IsSystemSource ? "S" : "U")}_{this.ShortCode}";
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
                    this.TrackChange(nameof(Name));

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
                    this.TrackChange(nameof(IsSystemSource));

                }
            }
        }

        // IsKardexTransaction
        private bool _isKardexTransaction = false;
        public bool IsKardexTransaction
        {
            get { return _isKardexTransaction; }
            set
            {
                if (_isKardexTransaction != value)
                {
                    _isKardexTransaction = value;
                    NotifyOfPropertyChange(nameof(IsKardexTransaction));
                    this.TrackChange(nameof(IsKardexTransaction));
                    this.TrackChange(nameof(KardexFlow));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        // is annulled with additional document
        public bool IsAnnulledWithAdditionalDocument { get { return this.AnnulmentCharacter == 'A'; } }

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
        private char _annulmentCharacter = 'X';
        public char AnnulmentCharacter
        {
            get { return _annulmentCharacter; }
            set
            {
                if (_annulmentCharacter != value)
                {
                    _annulmentCharacter = value;
                    NotifyOfPropertyChange(nameof(AnnulmentCharacter));
                    NotifyOfPropertyChange(nameof(IsAnnulledWithAdditionalDocument));
                    this.TrackChange(nameof(AnnulmentCharacter));

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
        private char? _kardexFlow = Dictionaries.InventoriesDictionaries.KardexFlowDictionary.FirstOrDefault().Key;
        public char? KardexFlow
        {
            get { return _kardexFlow; }
            set
            {
                if (_kardexFlow != value)
                {
                    _kardexFlow = value;
                    NotifyOfPropertyChange(nameof(KardexFlow));
                    this.TrackChange(nameof(KardexFlow));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        // Id Seleccionado en ComboBox Cuentas Contables
        private int _accountingAccountId = -1;
        public int AccountingAccountId
        {
            get { return _accountingAccountId; }
            set
            {
                if (_accountingAccountId != value)
                {
                    _accountingAccountId = value;
                    NotifyOfPropertyChange(nameof(AccountingAccountId));
                    this.TrackChange(nameof(AccountingAccountId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        // SelectedProcessTypeId combobox tipos de procesos
        private int _processTypeId = -1;
        public int ProcessTypeId
        {
            get { return _processTypeId; }
            set
            {
                if (_processTypeId != value)
                {
                    _processTypeId = value;
                    NotifyOfPropertyChange(nameof(ProcessTypeId));
                    this.TrackChange(nameof(ProcessTypeId));

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
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync, CanSave);
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

        public AccountingSourceDetailViewModel(AccountingSourceViewModel context, IRepository<AccountingSourceGraphQLModel> accountingSourceService, ObservableCollection<ProcessTypeGraphQLModel> processTypes, IEnumerable<AccountingAccountPOCO> auxiliaryAccounts)
        {
            // Contexto
            this.Context = context;
            this._accountingSourceService = accountingSourceService;
            this.ProcessTypes = processTypes;
            // Cargar cuentas contables
           
            this.AuxiliaryAccountingAccounts = new ObservableCollection<AccountingAccountPOCO>(auxiliaryAccounts);
            // Cargar tipos de procesos
            //this.ProcessTypes = new ObservableCollection<ProcessTypeGraphQLModel>(ProcessTypeService.GetList());
            var joinable = new JoinableTaskFactory(new JoinableTaskContext());
         
        }

        public Task InitializeAsync()
        {
           
            return Task.CompletedTask;
        }
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.SetFocus(() => ShortCode);
            this.AcceptChanges();
            if (IsNewRecord)
            {
                this.TrackChange(nameof(IsKardexTransaction));
                this.TrackChange(nameof(AnnulmentCharacter));

            }
            this.NotifyOfPropertyChange(nameof(CanSave));
        }

       

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                UpsertResponseType<AccountingSourceGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }
                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new AccountingSourceCreateMessage() { CreatedAccountingSource = result }
                        : new AccountingSourceUpdateMessage() { UpdatedAccountingSource = result }
                );
                await Context.ActivateMasterViewAsync();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"\r\n{graphQLError.Errors[0].Message}\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }


            
        }

        public void CleanUpControls()
        {
            Id = 0;
            ProcessTypeId = -1;
            AccountingAccountId = -1;
            AnnulmentCharacter = 'X';
            KardexFlow = 'I';
            ShortCode = "";
            Name = "";
            IsKardexTransaction = false;
            this.AcceptChanges();

        }
        public string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<AccountingSourceGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingSource", nested: sq => sq
                   .Field(e => e.Id)
                   .Field(e => e.Code)
                   .Field(e => e.Name)
                   .Field(e => e.AnnulmentCharacter)
                   .Field(e => e.AnnulmentCode)
                   .Field(e => e.IsKardexTransaction)
                   .Field(e => e.AnnulmentCharacter)
                   .Field(e => e.KardexFlow)
                  
             
                    .Select(e => e.AccountingAccount, cos => cos
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                     )


                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateAccountingSourceInput!");

            var fragment = new GraphQLQueryFragment("createAccountingSource", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<AccountingSourceGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingSource", nested: sq => sq
                   .Field(e => e.Id)
                   .Field(e => e.Code)
                   .Field(e => e.Name)
                   .Field(e => e.AnnulmentCharacter)
                   .Field(e => e.AnnulmentCode)
                   .Field(e => e.IsKardexTransaction)
                   .Field(e => e.KardexFlow)


                    .Select(e => e.AccountingAccount, cos => cos
                            .Field(c => c.Id)
                            .Field(c => c.Name)
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
                new("data", "UpdateAccountingSourceInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateAccountingSource", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public async Task<UpsertResponseType<AccountingSourceGraphQLModel>> ExecuteSaveAsync()
        {

            dynamic variables = new ExpandoObject();
         
           

            if (IsNewRecord)
            {

                variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                string query = GetCreateQuery();
                UpsertResponseType<AccountingSourceGraphQLModel> AccountingSourceCreated = await _accountingSourceService.CreateAsync<UpsertResponseType<AccountingSourceGraphQLModel>>(query, variables);
                return AccountingSourceCreated;
            }
            else
            {
                var a = CanSave;
                string query = GetUpdateQuery();
                variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = Id;
                UpsertResponseType<AccountingSourceGraphQLModel> updatedAccountingSource = await _accountingSourceService.UpdateAsync<UpsertResponseType<AccountingSourceGraphQLModel>>(query, variables);
                return updatedAccountingSource;
            }
           
        }

        public bool CanSave
        {
            get
            {
                if (this.IsBusy || !this.HasChanges()) return false;
                if (this.ProcessTypeId == -1) return false;
                if (string.IsNullOrEmpty(this.ShortCode) || string.IsNullOrEmpty(this.Name)) return false;
                if (this.ShortCode.Trim().Length != 3) return false;
                if (this.IsKardexTransaction)
                {
                    if (this.AccountingAccountId == -1) return false;
                }
                
                return true;
            }
        }
        public void GoBack(object p)
        {
            _ = Context.ActivateMasterViewAsync();
           
        }

        public bool CanGoBack(object p)
        {
            return !IsBusy;
        }
    }
}
