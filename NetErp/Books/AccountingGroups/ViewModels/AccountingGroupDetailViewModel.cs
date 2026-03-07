using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Billing;
using Models.Books;
using Models.DTO.Global;
using Models.Global;
using NetErp.Global.CostCenters.DTO;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using Services.Billing.DAL.PostgreSQL;
using Services.Global.DAL.PostgreSQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using Extensions.Global;

using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static Amazon.S3.Util.S3EventNotification;
using static Dictionaries.BooksDictionaries;
using static Models.Global.GraphQLResponseTypes;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.AccountingGroups.ViewModels
{
    public class AccountingGroupDetailViewModel : Screen, INotifyDataErrorInfo
    {
        private readonly IRepository<AccountingGroupGraphQLModel> _accountingGroupService;
        public AccountingGroupViewModel Context { get; set; }
        public AccountingGroupDetailViewModel(AccountingGroupViewModel context, IRepository<AccountingGroupGraphQLModel> accountingGroupService)
        {
            Context = context;
            _errors = new Dictionary<string, List<string>>();
            _accountingGroupService = accountingGroupService;
        }
        public async Task InitializeAsync()
        {
            await Task.WhenAll(
                
                );
        }
        #region ModelProperties

        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                NotifyOfPropertyChange(nameof(Id));
                NotifyOfPropertyChange(nameof(IsNewRecord));
            }
        }

        private AccountingAccountGraphQLModel _selectedAccountAiuAdministration;
        public AccountingAccountGraphQLModel SelectedAccountAiuAdministration
        {
            get => _selectedAccountAiuAdministration;
            set
            {
                _selectedAccountAiuAdministration = value;
                NotifyOfPropertyChange(nameof(SelectedAccountAiuAdministration));
            }
        }

        private AccountingAccountGraphQLModel _selectedAccountAiuUnforeseen;
        public AccountingAccountGraphQLModel SelectedAccountAiuUnforeseen
        {
            get => _selectedAccountAiuUnforeseen;
            set
            {
                _selectedAccountAiuUnforeseen = value;
                NotifyOfPropertyChange(nameof(SelectedAccountAiuUnforeseen));
            }
        }

        private AccountingAccountGraphQLModel _selectedAccountAiuUtility;
        public AccountingAccountGraphQLModel SelectedAccountAiuUtility
        {
            get => _selectedAccountAiuUtility;
            set
            {
                _selectedAccountAiuUtility = value;
                NotifyOfPropertyChange(nameof(SelectedAccountAiuUtility));
            }
        }

        private AccountingAccountGraphQLModel _selectedAccountCost;
        public AccountingAccountGraphQLModel SelectedAccountCost
        {
            get => _selectedAccountCost;
            set
            {
                _selectedAccountCost = value;
                NotifyOfPropertyChange(nameof(SelectedAccountCost));
            }
        }

        private AccountingAccountGraphQLModel _selectedAccountIncome;
        public AccountingAccountGraphQLModel SelectedAccountIncome
        {
            get => _selectedAccountIncome;
            set
            {
                _selectedAccountIncome = value;
                NotifyOfPropertyChange(nameof(SelectedAccountIncome));
            }
        }

        private AccountingAccountGraphQLModel _selectedAccountIncomeReverse;
        public AccountingAccountGraphQLModel SelectedAccountIncomeReverse
        {
            get => _selectedAccountIncomeReverse;
            set
            {
                _selectedAccountIncomeReverse = value;
                NotifyOfPropertyChange(nameof(SelectedAccountIncomeReverse));
            }
        }

        private AccountingAccountGraphQLModel _selectedAccountInventory;
        public AccountingAccountGraphQLModel SelectedAccountInventory
        {
            get => _selectedAccountInventory;
            set
            {
                _selectedAccountInventory = value;
                NotifyOfPropertyChange(nameof(SelectedAccountInventory));
            }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                NotifyOfPropertyChange(nameof(Name));
            }
        }

        private TaxGraphQLModel _purchasePrimaryTax;
        public TaxGraphQLModel PurchasePrimaryTax
        {
            get => _purchasePrimaryTax;
            set
            {
                _purchasePrimaryTax = value;
                NotifyOfPropertyChange(nameof(PurchasePrimaryTax));
            }
        }

        private TaxGraphQLModel _purchaseSecondaryTax;
        public TaxGraphQLModel PurchaseSecondaryTax
        {
            get => _purchaseSecondaryTax;
            set
            {
                _purchaseSecondaryTax = value;
                NotifyOfPropertyChange(nameof(PurchaseSecondaryTax));
            }
        }

        private TaxGraphQLModel _salesPrimaryTax;
        public TaxGraphQLModel SalesPrimaryTax
        {
            get => _salesPrimaryTax;
            set
            {
                _salesPrimaryTax = value;
                NotifyOfPropertyChange(nameof(SalesPrimaryTax));
            }
        }

        private TaxGraphQLModel _salesSecondaryTax;
        public TaxGraphQLModel SalesSecondaryTax
        {
            get => _salesSecondaryTax;
            set
            {
                _salesSecondaryTax = value;
                NotifyOfPropertyChange(nameof(SalesSecondaryTax));
            }
        }
        private bool _allowAiu;
        public bool AllowAiu
        {
            get => _allowAiu;
            set
            {
                _allowAiu = value;
                NotifyOfPropertyChange(nameof(AllowAiu));
            }
        }

        

        public bool IsNewRecord => Id == 0;
        #endregion

        #region PropertiesAndCommands
        
        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync, CanSave);
                return _saveCommand;
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
        public bool CanGoBack(object p)
        {
            return !IsBusy;
        }

        public bool CanSave
        {
            get
            {
                if (_errors.Count > 0 ||  !this.HasChanges()) { return false; }
                return true;
            }
        }
       
        private bool _isBusy = false;
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

        public void GoBack(object p)
        {
            CleanUpControls();
            _ = Task.Run(() => Context.ActivateMasterViewAsync());

        }
        public void CleanUpControls()
        {
            Name = "";
           
        }
        #endregion
        Dictionary<string, List<string>> _errors;


        
        public bool HasErrors => _errors.Count > 0;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;


        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return new List<object>();
            return _errors[propertyName];
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                RaiseErrorsChanged(propertyName);
            }
        }
        #region ApiMethods
        public async Task<AccountingGroupGraphQLModel> LoadDataForEditAsync(int id)
        {
            try
            {
                string query = GetLoadAccountingGroupByIdQuery();

                dynamic variables = new ExpandoObject();


                variables.singleItemResponseId = id;

                var entity = await _accountingGroupService.FindByIdAsync(query, variables);

                // Poblar el ViewModel con los datos del entity (sin bloquear UI thread)
                PopulateFromAccountingGroup(entity);

                return entity;
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
        public void PopulateFromAccountingGroup(AccountingGroupGraphQLModel entity)
        {
            // Propiedades básicas del entity
           /* Id = entity.Id;

            IdentificationNumber = entity.AccountingEntity.IdentificationNumber;
           
            ZoneId = entity.Zone?.Id;*/


      
        }
        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                UpsertResponseType<AccountingGroupGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }
                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new AccountingGroupCreateMessage() { CreatedAccountingGroup = result }
                        : new AccountingGroupUpdateMessage() { UpdatedAccountingGroup = result }
                );

                // Context.EnableOnViewReady = false;
                await Context.ActivateMasterViewAsync();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{graphQLError.Errors[0].Extensions.Message} {graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "Save" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }


        }

        public async Task<UpsertResponseType<AccountingGroupGraphQLModel>> ExecuteSaveAsync()
        {

            dynamic variables = new ExpandoObject();


            if (IsNewRecord)
            {

                variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                string query = GetCreateQuery();
                UpsertResponseType<AccountingGroupGraphQLModel>groupCreated = await _accountingGroupService.CreateAsync<UpsertResponseType<AccountingGroupGraphQLModel>>(query, variables);
                return groupCreated;
            }
            else
            {

                string query = GetUpdateQuery();
                variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = Id;
                UpsertResponseType<AccountingGroupGraphQLModel> updatedGroup = await _accountingGroupService.UpdateAsync<UpsertResponseType<AccountingGroupGraphQLModel>>(query, variables);
                return updatedGroup;
            }

        }
        public string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<AccountingGroupGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingGroup", nested: sq => sq
                   .Field(e => e.Id)
                  .Field(e => e.Name)

                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateAccountingGroupInput!");

            var fragment = new GraphQLQueryFragment("createAccountingGroup", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<AccountingGroupGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingGroup", nested: sq => sq
                    .Field(e => e.Id)
                   .Field(e => e.Name)
                   
                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();


            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateAccountingGroupInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateAccountingGroup", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public string GetLoadAccountingGroupByIdQuery()
        {
            var sellersFields = FieldSpec<AccountingGroupGraphQLModel>
             .Create()

                 .Field(e => e.Id)

                 .Field(e => e.Name)
                 /*  .Select(e => e.AccountingEntity, acc => acc
                             .Field(c => c.Id)
                             .Field(c => c.VerificationDigit)
                             .Field(c => c.IdentificationNumber)
                             .Field(c => c.FirstName)
                             .Field(c => c.MiddleName)
                             .Field(c => c.FirstLastName)
                             .Field(c => c.MiddleLastName)
                             .Field(c => c.SearchName)
                             .Field(c => c.PrimaryPhone)
                             .Field(c => c.SecondaryPhone)
                             .Field(c => c.PrimaryCellPhone)
                             .Field(c => c.SecondaryCellPhone)
                             .Field(c => c.Address)
                             .Field(c => c.TelephonicInformation)
                             .Select(e => e.IdentificationType, co => co
                                     .Field(x => x.Id)
                                     .Field(x => x.Code)
                                 )
                             .Select(e => e.Country, co => co
                                     .Field(x => x.Id)
                                 )
                             .Select(e => e.City, co => co
                                     .Field(x => x.Id)
                                 )
                             .Select(e => e.Department, co => co
                                     .Field(x => x.Id)
                                     )
                             .SelectList(e => e.Emails, co => co
                                     .Field(x => x.Id)
                                     .Field(x => x.Description)
                                     .Field(x => x.Email)
                                     .Field(x => x.isElectronicInvoiceRecipient)
                                     )
                             )
                    .SelectList(e => e.CostCenters, acc => acc
                         .Field(c => c.Id)
                         .Field(c => c.Name)
                    )
                    .Select(e => e.Zone, acc => acc
                         .Field(c => c.Id)
                         .Field(c => c.Name)



               )*/
                 .Build();
            var sellerIdParameter = new GraphQLQueryParameter("id", "ID!");

            var sellerFragment = new GraphQLQueryFragment("seller", [sellerIdParameter], sellersFields, "SingleItemResponse");

            var builder = new GraphQLQueryBuilder([sellerFragment]);

            return builder.GetQuery();

        }
        #endregion
    }


}
