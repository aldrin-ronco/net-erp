using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using Extensions.Global;
using GraphQL.Client.Http;
using Models.Billing;
using Models.Books;
using NetErp.Books.AccountingAccountGroups.DTO;
using NetErp.Global.CostCenters.DTO;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using Xceed.Wpf.Toolkit.Primitives;
using static Amazon.S3.Util.S3EventNotification;
using static Models.Global.GraphQLResponseTypes;


namespace NetErp.Books.AccountingPresentations.ViewModels
{
    class AccountingPresentationDetailViewModel : Screen, INotifyDataErrorInfo
    {
        public AccountingPresentationViewModel Context { get; set; }
        private readonly AccountingBookCache _accountingBookCache;
        Dictionary<string, List<string>> _errors;

       
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AccountingPresentationGraphQLModel> _accountingPresentationService;

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
                }
            }
        }

        public bool IsNewRecord => Id == 0;

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
                }
            }
        }

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
                    ValidateProperty(nameof(Name), _name);
                    this.TrackChange(nameof(Name));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        private List<int> _accountingBookIds = [];
        public List<int> AccountingBookIds
        {
            get { return _accountingBookIds; }
            set
            {
                if (_accountingBookIds != value)
                {
                    _accountingBookIds = value;
                    NotifyOfPropertyChange(nameof(AccountingBookIds));
                    this.TrackChange(nameof(AccountingBookIds));
                    
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        private bool _allowsClosure;
        public bool AllowsClosure
        {
            get { return _allowsClosure; }
            set
            {
                if (_allowsClosure != value)
                {
                    _allowsClosure = value;
                   
                        ClosureAccountingBookId = null ; 
                    
                    NotifyOfPropertyChange(nameof(AllowsClosure));
                    this.TrackChange(nameof(AllowsClosure));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int? _closureAccountingBookId;
        public int? ClosureAccountingBookId
        {
            get { return _closureAccountingBookId; }
            set
            {
                if(_closureAccountingBookId != value)
                {
                    _closureAccountingBookId = value;
                    NotifyOfPropertyChange(nameof(ClosureAccountingBookId));
                    this.TrackChange(nameof(ClosureAccountingBookId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private ObservableCollection<AccountingBookGraphQLModel> _accountingPresentationAccountingBooks;
        public ObservableCollection<AccountingBookGraphQLModel> AccountingPresentationAccountingBooks
        {
            get { return _accountingPresentationAccountingBooks; }
            set
            {
                if(_accountingPresentationAccountingBooks != value)
                {
                    _accountingPresentationAccountingBooks = value;
                    NotifyOfPropertyChange(nameof(AccountingPresentationAccountingBooks));
                }
            }
        }

        private ObservableCollection<AccountingBookDTO> _accountingBooks;
        public ObservableCollection<AccountingBookDTO> AccountingBooks
        {
            get { return _accountingBooks; }
            set
            {
                if (_accountingBooks != value)
                {
                    _accountingBooks = value;
                    NotifyOfPropertyChange(nameof(AccountingBooks));
                    

                }
            }
        }
        private ObservableCollection<AccountingBookDTO> _accountingBooksToClosure;
        public ObservableCollection<AccountingBookDTO> AccountingBooksToClosure
        {
            get { return _accountingBooksToClosure; }
            set
            {
                if (_accountingBooksToClosure != value)
                {
                    _accountingBooksToClosure = value;
                    NotifyOfPropertyChange(nameof(AccountingBooksToClosure));


                }
            }
        }
      
        private ICommand _goBackCommand;
        public ICommand? GoBackCommand
        {
            get
            {
                if (_goBackCommand is null) _goBackCommand = new AsyncCommand(GoBackAsync);
                return _goBackCommand;
            }
        }
        
        public bool CanSave
        {
            get
            {
                return !string.IsNullOrEmpty(Name) &&
                    ((!AllowsClosure || ClosureAccountingBookId > 0) && _errors.Count <= 0 && AccountingBookIds.Count > 0 && this.HasChanges());
            }
        }
        public void ToggleActive(RoutedEventArgs args)
        {
            List<int> selectedIds = AccountingBooks.Where(accountingBook => accountingBook.IsChecked == true).Select(x => x.Id.Value).ToList();
            if (!selectedIds.SequenceEqual(AccountingBookIds))
            {
                AccountingBookIds = [.. selectedIds];
            }
        }
        private ICommand _saveCommand;

        public ICommand? SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }
        public async Task InitializeAsync()
        {
            await Task.WhenAll(
                  _accountingBookCache.EnsureLoadedAsync()
                  );
            ObservableCollection<AccountingBookDTO> accountingBooks = Context.AutoMapper.Map<ObservableCollection<AccountingBookDTO>>(_accountingBookCache.Items);
            ObservableCollection<AccountingBookDTO> accountingBooksToClosure = [.. accountingBooks];
            accountingBooksToClosure.Insert(0, new AccountingBookDTO() { Id = null, Name = "SELECCIONAR UN LIBRO" });
            AccountingBooks = [.. accountingBooks]; 
            AccountingBooksToClosure = accountingBooksToClosure;
           
            this.AcceptChanges();

        }
        public async Task<AccountingPresentationGraphQLModel> LoadDataForEditAsync(int id)
        {
            try
            {
                string query = GetLoadSByIdQuery();

                dynamic variables = new ExpandoObject();


                variables.singleItemResponseId = id;

                var certificate = await _accountingPresentationService.FindByIdAsync(query, variables);

                // Poblar el ViewModel con los datos del seller (sin bloquear UI thread)
                PopulateFromTax(certificate);

                return certificate;
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
        public void PopulateFromTax(AccountingPresentationGraphQLModel entity)
        {
            var checkedIds = entity.AccountingBooks.Select(p => p.Id).ToHashSet();
            ObservableCollection<AccountingBookDTO> accountingBooks = Context.AutoMapper.Map<ObservableCollection<AccountingBookDTO>>(_accountingBookCache.Items);

                foreach (var accountingBook in accountingBooks)
                {
                    accountingBook.IsChecked = checkedIds.Contains(accountingBook.Id.Value);
                }
                ObservableCollection<AccountingBookDTO> AccountingBooksToClosure = [.. accountingBooks];
                AccountingBooksToClosure.Insert(0, new AccountingBookDTO() { Id = null, Name = "SELECCIONAR lIBRO" });
        
               
            Id = entity.Id;
            Name = entity.Name;
            AllowsClosure = entity.AllowsClosure;
            AccountingBooks = [..accountingBooks ];
            AccountingBooksToClosure = AccountingBooksToClosure;
            ClosureAccountingBookId = entity.ClosureAccountingBook is null ? 0 : entity.ClosureAccountingBook.Id;
            AccountingPresentationAccountingBooks = entity.AccountingBooks;
            AccountingBookIds = checkedIds.ToList();
            this.AcceptChanges();



        }
        public string GetLoadSByIdQuery()
        {
            var singleIdFields = FieldSpec<AccountingPresentationGraphQLModel>
             .Create()

                 .Field(e => e.Id)
                

                  .Field(e => e.AllowsClosure)
                  .Field(e => e.Name)
                  .SelectList(e => e.AccountingBooks, acc => acc
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                            )
                  .Select(e => e.ClosureAccountingBook, acc => acc
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                            )

             .Build();
            var singleIdParameter = new GraphQLQueryParameter("id", "ID!");

            var singleIdFragment = new GraphQLQueryFragment("accountingPresentation", [singleIdParameter], singleIdFields, "SingleItemResponse");

            var builder = new GraphQLQueryBuilder([singleIdFragment]);

            return builder.GetQuery();

        }
        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                UpsertResponseType<AccountingPresentationGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }
                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new AccountingPresentationCreateMessage() { CreatedAccountingPresentation = result }
                        : new AccountingPresentationUpdateMessage() { UpdatedAccountingPresentation = result }
                );


                await GoBackAsync();

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
        public async Task<UpsertResponseType<AccountingPresentationGraphQLModel>> ExecuteSaveAsync()
        {


            if (IsNewRecord)
            {

                string query = GetCreateQuery();
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");

                UpsertResponseType<AccountingPresentationGraphQLModel> taxCategoryCreated = await _accountingPresentationService.CreateAsync<UpsertResponseType<AccountingPresentationGraphQLModel>>(query, variables);
                return taxCategoryCreated;
            }
            else
            {
                string query = GetUpdateQuery();

                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = Id;

                UpsertResponseType<AccountingPresentationGraphQLModel> updatedTaxCategory = await _accountingPresentationService.UpdateAsync<UpsertResponseType<AccountingPresentationGraphQLModel>>(query, variables);
                return updatedTaxCategory;

            }

        }
        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<AccountingPresentationGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingPresentation", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                 
                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();
            
            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateAccountingPresentationInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateAccountingPresentation", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<AccountingPresentationGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingPresentation", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                   
                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateAccountingPresentationInput!");

            var fragment = new GraphQLQueryFragment("createAccountingPresentation", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperty(nameof(Name), Name);
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));

        }

        public async Task GoBackAsync()
        {
            
            await Context.ActivateMasterViewAsync();
            this.Name = "";
            this.AllowsClosure = false;
            this.AccountingPresentationAccountingBooks = [];
            this.ClosureAccountingBookId = null;
            this.AccountingBooks = [];
        }

        public AccountingPresentationDetailViewModel(AccountingPresentationViewModel context, Helpers.Services.INotificationService notificationService,
            IRepository<AccountingPresentationGraphQLModel> accountingPresentationService, AccountingBookCache accountingBookCache)
        {
            Context = context;
            _notificationService = notificationService;
            _accountingPresentationService = accountingPresentationService;
            _errors = [];
            AccountingBooks = [];
            AccountingPresentationAccountingBooks = [];
            _accountingBookCache = accountingBookCache;
        }


        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null;
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
       
        private void ValidateProperty(string propertyName, string value)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty.Trim();
            try
            {
                ClearErrors(propertyName);
                switch (propertyName)
                {
                    case nameof(Name):
                        if (string.IsNullOrEmpty(value)) AddError(propertyName, "El nombre no puede estar vacío");
                        break;

                }
            }
            catch (Exception ex)
            {
                Execute.OnUIThread(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                });
            }
        }
    }
}
