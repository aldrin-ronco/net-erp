using Amazon.Util.Internal;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using Models.Treasury;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using Ninject.Activation;
using Services.Books.DAL.PostgreSQL;
using Services.Global.DAL.PostgreSQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xceed.Wpf.Toolkit.Primitives;
using static Models.Global.GraphQLResponseTypes;
using static Models.Treasury.TreasuryConceptGraphQLModel;
using static Models.Global.GraphQLResponseTypes;
using Extensions.Global;
using DevExpress.XtraEditors.Filtering;

namespace NetErp.Treasury.Concept.ViewModels
{
    public class ConceptDetailViewModel : Screen, INotifyDataErrorInfo
    {
        private readonly IRepository<TreasuryConceptGraphQLModel> _conceptService;
        private readonly IRepository<AccountingAccountGraphQLModel> _accountingAccountService;

        public ConceptViewModel Context { get; set; }
        public ConceptDetailViewModel(
            ConceptViewModel context,
            IRepository<TreasuryConceptGraphQLModel> conceptService,
            IRepository<AccountingAccountGraphQLModel> accountingAccountService)
        {
            Context = context;
            _conceptService = conceptService;
            _accountingAccountService = accountingAccountService;
            var joinable = new JoinableTaskFactory(new JoinableTaskContext());
            joinable.Run(async () => await LoadNamesAccountingAccountsAsync());
            _errors = new Dictionary<string, List<string>>();
        }

        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    {
                        _name = value;
                        NotifyOfPropertyChange(nameof(Name));
                        this.TrackChange(nameof(Name));
                        ValidateProperty(nameof(Name), value);
                        NotifyOfPropertyChange(nameof(CanSave));
                    }
                }
            }
        }
        private bool IsNewRecord => ConceptId == 0;
        private string _type;
        public string Type
        {
            get { return _type; }
            set
            {
                if (_type != value)
                {
                    _type = value;
                    NotifyOfPropertyChange(nameof(Type));
                    NotifyOfPropertyChange(nameof(IsPercentageSectionVisible));
                    NotifyOfPropertyChange(nameof(IsPercentageOptionsVisible));
                    NotifyOfPropertyChange(nameof(Margin));
                    this.TrackChange(nameof(Type));

                    NotifyOfPropertyChange(nameof(CanSave));

                    // Asegurar que al seleccionar "Ingreso", la casilla se oculta
                    if (_type == "I")
                    {
                        AllowMargin = false;
                        NotifyOfPropertyChange(nameof(AllowMargin));
                        NotifyOfPropertyChange(nameof(CanSave));
                    }
                }
            }
        }
        public bool IsTypeD => Type == "D";
        public bool IsTypeI => Type == "I";
        public bool IsTypeE => Type == "E";
        private bool _allowMargin;
        public bool AllowMargin
        {
            get => _allowMargin;
            set
            {
                if (_allowMargin != value)
                {
                    _allowMargin = value;
                    NotifyOfPropertyChange(nameof(AllowMargin));
                    NotifyOfPropertyChange(nameof(IsPercentageOptionsVisible));
                    this.TrackChange(nameof(AllowMargin));
                    NotifyOfPropertyChange(nameof(Margin));
                    this.TrackChange(nameof(Margin));
                    NotifyOfPropertyChange(nameof(IsBase100));
                    NotifyOfPropertyChange(nameof(IsBase1000));
                    NotifyOfPropertyChange(nameof(MarginBasis));
                    this.TrackChange(nameof(MarginBasis));
                    NotifyOfPropertyChange(nameof(CanSave));

                    if (_allowMargin)
                    {
                        PercentageValue = 0.000m;
                        IsBase100 = true;
                        
                    }
                }
            }
        }

        public bool CanSave
        {
            get
            {

                if (string.IsNullOrEmpty(Name) ||
                    string.IsNullOrEmpty(Type) ||
                    AccountingAccountId == null ||
                    AccountingAccountId == 0)
                {
                    return false;
                }

                if (Type == "D" || Type == "E")
                {
                    if (AllowMargin && PercentageValue <= 0)
                    {
                        return false;
                    }
                }
                if (!this.HasChanges()) return false;
                return true;
            }
        }

        private decimal _percentageValue = 0.000m;
        public decimal PercentageValue
        {
            get { return _percentageValue; }
            set
            {
                if (_percentageValue != value)
                {
                    _percentageValue = value;
                    NotifyOfPropertyChange(nameof(PercentageValue));
                    NotifyOfPropertyChange(nameof(Margin));
                    NotifyOfPropertyChange(nameof(IsBase1000));
                    NotifyOfPropertyChange(nameof(IsBase100));
                    this.TrackChange(nameof(Margin));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        public decimal Margin => AllowMargin ? PercentageValue : 0;
        public int MarginBasis => AllowMargin ? (IsBase100 ? 100 : 1000) : 0;

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
        private int _conceptId;
        public int ConceptId
        {
            get { return _conceptId; }
            set
            {
                if (_conceptId != value)
                {
                    _conceptId = value;
                    NotifyOfPropertyChange(nameof(ConceptId));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }
        private bool _isBase100;
        public bool IsBase100
        {
            get { return _isBase100; }
            set
            {
                if (_isBase100 != value)
                {
                    _isBase100 = value;
                    if (value) _isBase1000 = false;
                    NotifyOfPropertyChange(nameof(IsBase100));
                    NotifyOfPropertyChange(nameof(IsBase1000));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        private bool _isBase1000;
        public bool IsBase1000
        {
            get { return _isBase1000; }
            set
            {
                if (_isBase1000 != value)
                {
                    _isBase1000 = value;
                    if (value) _isBase100 = false;
                    NotifyOfPropertyChange(nameof(IsBase1000));
                    NotifyOfPropertyChange(nameof(IsBase100));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }



        private ICommand _changeTypeCommand;
        public ICommand ChangeTypeCommand
        {
            get
            {
                return _changeTypeCommand ??= new AsyncCommand<object>(async param =>
                {
                    if (param is string type)
                    {
                        Type = type;
                    }
                });
            }
        }
        private ICommand _goBackCommand;
        public ICommand GoBackCommand
        {
            get
            {
                if (_goBackCommand is null) _goBackCommand = new AsyncCommand(GoBackAsync);
                return _goBackCommand;
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
        public Visibility IsPercentageSectionVisible
        {
            get => (Type == "D" || Type == "E") ? Visibility.Visible : Visibility.Collapsed;
        }
        public Visibility IsPercentageOptionsVisible
        {
            get => (AllowMargin && (Type == "D" || Type == "E"))
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private int? _accountingAccountId;
        public int? AccountingAccountId
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
        private ObservableCollection<AccountingAccountGraphQLModel> _accountingAccount;
        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccount
        {
            get { return _accountingAccount; }
            set
            {
                if (_accountingAccount != value)
                {
                    _accountingAccount = value;
                    NotifyOfPropertyChange(nameof(AccountingAccount));
                }
            }
        }

        public async Task LoadNamesAccountingAccountsAsync()
        {
            try
            {
                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();

                variables.pageResponseFilters.only_auxiliary_accounts = true;
                string query = GetLoadAccountingAccountsQuery();
                PageType<AccountingAccountGraphQLModel> result = await _accountingAccountService.GetPageAsync(query, variables);




                AccountingAccount = result.Entries;
                AccountingAccount.Insert(0, new() { Id = 0, Name = "<< SELECCIONE UNA CUENTA >>" });
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public string GetLoadAccountingAccountsQuery()
        {
            var accountingAccountFields = FieldSpec<PageType<AccountingAccountGraphQLModel>>
             .Create()
             .SelectList(it => it.Entries, entries => entries
                 .Field(e => e.Id)
                 .Field(e => e.Margin)
                 .Field(e => e.Code)
                 .Field(e => e.Name)
                 .Field(e => e.MarginBasis)
             )
             .Field(o => o.PageNumber)
             .Field(o => o.PageSize)
             .Field(o => o.TotalPages)
             .Field(o => o.TotalEntries)
             .Build();
            var accountingAccountParameters = new GraphQLQueryParameter("filters", "AccountingAccountFilters");

            var accountingAccountFragment = new GraphQLQueryFragment("accountingAccountsPage", [accountingAccountParameters], accountingAccountFields, "pageResponse");
           
            var builder = new GraphQLQueryBuilder([ accountingAccountFragment]) ;
            return builder.GetQuery();
        }
        public async Task GoBackAsync()
        {
            await Context.ActivateMasterViewAsync();
        }
       
      
        public async Task SaveAsync()
        {
            
            try
            {
                IsBusy = true;
                Refresh();
                UpsertResponseType<TreasuryConceptGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }
                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new TreasuryConceptCreateMessage() { CreatedTreasuryConcept = result }
                        : new TreasuryConceptUpdateMessage() { UpdatedTreasuryConcept = result }
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

        public string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<TreasuryConceptGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "concept", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateTreasuryConceptInput!");

            var fragment = new GraphQLQueryFragment("createTreasuryConcept", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<TreasuryConceptGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "concept", nested: sq => sq
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
                new("data", "UpdateTreasuryConceptInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateTreasuryConcept", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public async Task<UpsertResponseType<TreasuryConceptGraphQLModel>> ExecuteSaveAsync()
        {

            try
            {
                if (IsNewRecord)
                {
                    string query = GetCreateQuery();
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");

                    UpsertResponseType<TreasuryConceptGraphQLModel> treasuryConceptGraphQLModelCreated = await _conceptService.CreateAsync<UpsertResponseType<TreasuryConceptGraphQLModel>>(query, variables);
                    return treasuryConceptGraphQLModelCreated;
                }
                else
                {
                    string query = GetUpdateQuery();

                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = ConceptId;

                    UpsertResponseType<TreasuryConceptGraphQLModel> updatedTreasuryConcept = await _conceptService.UpdateAsync<UpsertResponseType<TreasuryConceptGraphQLModel>>(query, variables);
                    return updatedTreasuryConcept;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void CleanUpControls()
        {
            ConceptId = 0;
            Name = string.Empty;
            AllowMargin = false; 
            PercentageValue = 0;
            IsBase100 = false;
            Type = string.Empty;
            AccountingAccountId = null;


        }
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.SetFocus(() => Name);
           
            this.SeedValue(nameof(Type), Type);
            ValidateProperties();
        }

        public bool HasErrors => _errors.Count > 0;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
        public IEnumerable GetErrors(string propertyName)
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
                        if (string.IsNullOrEmpty(Name)) AddError(propertyName, "El campo 'Nombre' no puede estar vacío.");
                        break;                   
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }
        private void ValidateProperties()
        {
            ValidateProperty(nameof(Name), Name);
        }
        Dictionary<string, List<string>> _errors;
    }
}

