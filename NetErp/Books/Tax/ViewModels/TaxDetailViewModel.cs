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
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;
using Extensions.Global;

namespace NetErp.Books.Tax.ViewModels
{
    public class TaxDetailViewModel : Screen, INotifyDataErrorInfo
    {
        private readonly IRepository<TaxGraphQLModel> _taxService;

        public TaxDetailViewModel(TaxViewModel context, TaxGraphQLModel? entity, IRepository<TaxGraphQLModel> taxService)
        {
            _taxService = taxService ?? throw new ArgumentNullException(nameof(taxService));

            Context = context;
            _errors = new Dictionary<string, List<string>>();


            if (entity != null)
            {
                _entity = entity;
                SelectedTaxCategoryGraphQLModel = entity.TaxCategory;

            }


            Context.EventAggregator.SubscribeOnUIThread(this);
            var joinable = new JoinableTaskFactory(new JoinableTaskContext());
            joinable.Run(async () => await InitializeAsync());




        }
        public async Task InitializeAsync()
        {
            await LoadListAsync();
            IsActive = true;
            if (Entity != null)
            {
                SetUpdateProperties(Entity);
            }
        }
        public void SetUpdateProperties(TaxGraphQLModel entity)
        {
            Name = entity.Name;
            Margin = entity.Margin;
            Formula = entity.Formula;
            AlternativeFormula = entity.AlternativeFormula;
            IsActive = entity.IsActive;
            SelectedTaxCategoryGraphQLModel = TaxCategories.FirstOrDefault(f => f.Id == entity.TaxCategory.Id);
            GeneratedTaxAccountId = entity.GeneratedTaxAccount != null ? entity.GeneratedTaxAccount.Id : null;
            GeneratedTaxRefundAccountId = entity.GeneratedTaxRefundAccount != null ? entity.GeneratedTaxRefundAccount.Id : null;
            DeductibleTaxAccountId = entity.DeductibleTaxAccount != null ? entity.DeductibleTaxAccount.Id : null;
            DeductibleTaxRefundAccountId = entity.DeductibleTaxRefundAccount != null ? entity.DeductibleTaxRefundAccount.Id : null;
            Id = entity.Id;
            this.AcceptChanges();
        }
        private TaxViewModel _context;
        public TaxViewModel Context
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
        private TaxGraphQLModel? _entity;
        public TaxGraphQLModel Entity
        {
            get { return _entity; }
            set
            {
                if (_entity != value)
                {
                    _entity = value;
                    NotifyOfPropertyChange(nameof(Entity));

                }
            }
        }
        private ObservableCollection<AccountingAccountGraphQLModel> _accountingAccountOperations;

        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccountOperations
        {
            get { return _accountingAccountOperations; }
            set
            {
                if (_accountingAccountOperations != value)
                {
                    _accountingAccountOperations = value;
                    NotifyOfPropertyChange(nameof(AccountingAccountOperations));
                }
            }
        }
        private ObservableCollection<AccountingAccountGraphQLModel> _accountingAccountDevolutions;

        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccountDevolutions
        {
            get { return _accountingAccountDevolutions; }
            set
            {
                if (_accountingAccountDevolutions != value)
                {
                    _accountingAccountDevolutions = value;
                    NotifyOfPropertyChange(nameof(AccountingAccountDevolutions));
                }
            }
        }
        private ObservableCollection<TaxCategoryGraphQLModel> _taxCategories;

        public ObservableCollection<TaxCategoryGraphQLModel> TaxCategories
        {
            get { return _taxCategories; }
            set
            {
                if (_taxCategories != value)
                {
                    _taxCategories = value;
                    NotifyOfPropertyChange(nameof(TaxCategories));
                }
            }
        }
        private TaxCategoryGraphQLModel? _selectedTaxCategoryGraphQLModel;
        public TaxCategoryGraphQLModel? SelectedTaxCategoryGraphQLModel
        {
            get { return _selectedTaxCategoryGraphQLModel; }
            set
            {
                if (_selectedTaxCategoryGraphQLModel != value)
                {
                    _selectedTaxCategoryGraphQLModel = value;
                    TaxCategoryId = value != null ? value.Id : null;
                    NotifyOfPropertyChange(nameof(SelectedTaxCategoryGraphQLModel));
                    NotifyOfPropertyChange(nameof(IsEnabledSelectedGeneratedTaxAccount));
                    NotifyOfPropertyChange(nameof(IsEnabledSelectedGeneratedTaxRefundAccount));
                    NotifyOfPropertyChange(nameof(IsEnabledSelectedDeductibleTaxAccount));
                    NotifyOfPropertyChange(nameof(IsEnabledSelectedDeductibleTaxRefundAccount));
                    ValidateProperties();
                    NotifyOfPropertyChange(nameof(CanSave));

                }
            }
        }
        private int? _taxCategoryId;
        public int? TaxCategoryId
        {
            get { return _taxCategoryId; }
            set
            {
                if (_taxCategoryId != value)
                {
                    _taxCategoryId = value;
                    this.TrackChange(nameof(TaxCategoryId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        public int Id { get; set; }


        private string _name;
        private decimal? _margin;
        private int? _generatedTaxAccountId;
        private int? _generatedTaxRefundAccountId;
        private int? _deductibleTaxAccountId;
        private int? _deductibleTaxRefundAccountId;
        private int _taxCategory;
        private bool _isActive;
        private string _formula;
        private string _alternativeFormula;
        
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    NotifyOfPropertyChange(nameof(Name));
                    ValidateProperty(nameof(Name), Name);
                    this.TrackChange(nameof(Name));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        public decimal? Margin
        {
            get { return _margin; }
            set
            {
                if (_margin != value)
                {
                    _margin = value;
                    ValidateProperty(nameof(Margin), Margin);
                    NotifyOfPropertyChange(nameof(Margin));
                    this.TrackChange(nameof(Margin));
                    NotifyOfPropertyChange(nameof(CanSave));

                }
            }
        }
        public int? GeneratedTaxAccountId
        {
            get { return _generatedTaxAccountId; }
            set
            {
                if (_generatedTaxAccountId != value)
                {
                    _generatedTaxAccountId = value;
                    ValidateProperty(nameof(GeneratedTaxAccountId), GeneratedTaxAccountId);
                    NotifyOfPropertyChange(nameof(GeneratedTaxAccountId));
                    this.TrackChange(nameof(GeneratedTaxAccountId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        public int? GeneratedTaxRefundAccountId
        {
            get { return _generatedTaxRefundAccountId; }
            set
            {
                if (_generatedTaxRefundAccountId != value)
                {
                    _generatedTaxRefundAccountId = value;
                    ValidateProperty(nameof(GeneratedTaxRefundAccountId), GeneratedTaxRefundAccountId);
                    NotifyOfPropertyChange(nameof(GeneratedTaxRefundAccountId));
                    this.TrackChange(nameof(GeneratedTaxRefundAccountId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        public int? DeductibleTaxAccountId
        {
            get { return _deductibleTaxAccountId; }
            set
            {
                if (_deductibleTaxAccountId != value)
                {
                    _deductibleTaxAccountId = value;
                    ValidateProperty(nameof(DeductibleTaxAccountId), DeductibleTaxAccountId);
                    NotifyOfPropertyChange(nameof(DeductibleTaxAccountId));
                    this.TrackChange(nameof(DeductibleTaxAccountId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        public int? DeductibleTaxRefundAccountId
        {
            get { return _deductibleTaxRefundAccountId; }
            set
            {
                if (_deductibleTaxRefundAccountId != value)
                {
                    _deductibleTaxRefundAccountId = value;
                    ValidateProperty(nameof(DeductibleTaxRefundAccountId), DeductibleTaxRefundAccountId);
                    NotifyOfPropertyChange(nameof(DeductibleTaxRefundAccountId));
                    this.TrackChange(nameof(DeductibleTaxRefundAccountId));
                    NotifyOfPropertyChange(nameof(CanSave));

                }
            }
        }

        private bool _isNewRecord => Entity?.Id > 0 ? false : true;
        public bool IsEnabledSelectedGeneratedTaxAccount => (SelectedTaxCategoryGraphQLModel != null && SelectedTaxCategoryGraphQLModel?.Id >  0 && SelectedTaxCategoryGraphQLModel.GeneratedTaxAccountIsRequired.Equals(true));
        public bool IsEnabledSelectedGeneratedTaxRefundAccount => (SelectedTaxCategoryGraphQLModel != null && SelectedTaxCategoryGraphQLModel?.Id > 0 && SelectedTaxCategoryGraphQLModel.GeneratedTaxRefundAccountIsRequired.Equals(true) && SelectedTaxCategoryGraphQLModel.GeneratedTaxAccountIsRequired.Equals(true));
        public bool IsEnabledSelectedDeductibleTaxAccount => (SelectedTaxCategoryGraphQLModel != null && SelectedTaxCategoryGraphQLModel?.Id > 0 && SelectedTaxCategoryGraphQLModel.DeductibleTaxAccountIsRequired.Equals(true));
        public bool IsEnabledSelectedDeductibleTaxRefundAccount => (SelectedTaxCategoryGraphQLModel != null && SelectedTaxCategoryGraphQLModel?.Id > 0 && SelectedTaxCategoryGraphQLModel.DeductibleTaxRefundAccountIsRequired.Equals(true) && SelectedTaxCategoryGraphQLModel.DeductibleTaxAccountIsRequired.Equals(true));


        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    NotifyOfPropertyChange(nameof(IsActive));
                    this.TrackChange(nameof(IsActive));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        public string Formula
        {
            get { return _formula; }
            set
            {
                if (_formula != value)
                {
                    _formula = value;

                    NotifyOfPropertyChange(nameof(Formula));
                    this.TrackChange(nameof(Formula));
                    NotifyOfPropertyChange(nameof(CanSave));

                }
            }
        }
        public string AlternativeFormula
        {
            get { return _alternativeFormula; }
            set
            {
                if (_alternativeFormula != value)
                {
                    _alternativeFormula = value;

                    NotifyOfPropertyChange(nameof(AlternativeFormula));

                }
            }
        }


        Dictionary<string, List<string>> _errors;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null;
            return _errors[propertyName];
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
        #region Commands
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
      
        public bool IsNewRecord
        {
            get { return _isNewRecord; }

        }
        #endregion

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                UpsertResponseType<TaxGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }
                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                       ? new TaxCreateMessage() { CreatedTax = result }
                        : new TaxUpdateMessage() { UpdatedTax = result }
                );
                await Context.ActivateMasterViewModelAsync();
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
        public async Task<UpsertResponseType<TaxGraphQLModel>> ExecuteSaveAsync()
        {

            if (IsNewRecord)
            {
                string query = GetCreateQuery();
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");

                UpsertResponseType<TaxGraphQLModel> taxCreated = await _taxService.CreateAsync<UpsertResponseType<TaxGraphQLModel>>(query, variables);
                return taxCreated;
            }
            else
            {
                string query = GetUpdateQuery();
               
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = Id;

        
                UpsertResponseType<TaxGraphQLModel> updatedTax = await _taxService.UpdateAsync<UpsertResponseType<TaxGraphQLModel>>(query, variables);
                return updatedTax;

            }

        }
        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<TaxGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "tax", nested: sq => sq
                   .Field(e => e.Id)

                    .Field(e => e.Name)
                    .Field(e => e.Margin)
                    .Field(e => e.IsActive)
                    .Field(e => e.Formula)
                    .Select(e => e.GeneratedTaxAccount, cat => cat
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                     )
                    .Select(e => e.GeneratedTaxRefundAccount, cat => cat
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                    )
                    .Select(e => e.DeductibleTaxRefundAccount, cat => cat
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                    )
                    .Select(e => e.DeductibleTaxAccount, cat => cat
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                    )

                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Field)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateTaxInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateTax", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<TaxGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "tax", nested: sq => sq
                   .Field(e => e.Id)

                    .Field(e => e.Name)
                    .Field(e => e.Margin)
                    .Field(e => e.IsActive)
                    .Field(e => e.Formula)
                    .Select(e => e.GeneratedTaxAccount, cat => cat
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                     )
                    .Select(e => e.GeneratedTaxRefundAccount, cat => cat
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                    )
                    .Select(e => e.DeductibleTaxRefundAccount, cat => cat
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                    )
                    .Select(e => e.DeductibleTaxAccount, cat => cat
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                    )
                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Field)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateTaxInput!");

            var fragment = new GraphQLQueryFragment("createTax", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
       
      
        public bool CanSave
        {
            get
            {
                if (_errors.Count > 0  || !this.HasChanges()) { return false; }
                return true;
            }
        }
        public bool HasErrors => _errors.Count > 0;

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.SetFocus(() => Name);
            ValidateProperties();
            this.AcceptChanges();
            Formula = "Formula por definir";
            AlternativeFormula = "AlternativeFormula por definir";
        }
        public void GoBack(object p)
        {
            CleanUpControls();
            _ =  Context.ActivateMasterViewModelAsync();

        }
        private void ValidateProperties()
        {
            ValidateProperty(nameof(Name), Name);
            ValidateProperty(nameof(Margin), Margin);
            ValidateProperty(nameof(SelectedTaxCategoryGraphQLModel), SelectedTaxCategoryGraphQLModel?.Id);
            ValidateProperty(nameof(GeneratedTaxAccountId), GeneratedTaxAccountId);
            ValidateProperty(nameof(GeneratedTaxRefundAccountId), GeneratedTaxRefundAccountId);
            ValidateProperty(nameof(DeductibleTaxAccountId), DeductibleTaxAccountId);
            ValidateProperty(nameof(DeductibleTaxRefundAccountId), DeductibleTaxRefundAccountId);
          
        }
        public void CleanUpControls()
        {
            
        }
      
        private void ValidateProperty(string propertyName, int? value)
        {
            try
            {

                ClearErrors(propertyName);
                switch (propertyName)
                {
                    case nameof(SelectedTaxCategoryGraphQLModel):
                        if (!value.HasValue || value == 0) AddError(propertyName, "Debe seleccionar un tipo de impuesto");
                        break;

                    case nameof(GeneratedTaxAccountId):
                        if ((SelectedTaxCategoryGraphQLModel != null && SelectedTaxCategoryGraphQLModel.GeneratedTaxAccountIsRequired) &&  ( !value.HasValue || value == 0)) AddError(propertyName, "Debe seleccionar un GeneratedTaxAccountId");
                        break;
                    case nameof(GeneratedTaxRefundAccountId):
                        if ((SelectedTaxCategoryGraphQLModel != null && SelectedTaxCategoryGraphQLModel.GeneratedTaxRefundAccountIsRequired) && (!value.HasValue || value == 0)) AddError(propertyName, "Debe seleccionar un GeneratedTaxAccountId");
                        break;
                    case nameof(DeductibleTaxAccountId):
                        if ((SelectedTaxCategoryGraphQLModel != null && SelectedTaxCategoryGraphQLModel.DeductibleTaxAccountIsRequired) && (!value.HasValue || value == 0)) AddError(propertyName, "Debe seleccionar un GeneratedTaxAccountId");
                        break;
                    case nameof(DeductibleTaxRefundAccountId):
                        if ((SelectedTaxCategoryGraphQLModel != null && SelectedTaxCategoryGraphQLModel.DeductibleTaxRefundAccountIsRequired) &&  (!value.HasValue || value == 0)) AddError(propertyName, "Debe seleccionar un GeneratedTaxAccountId");
                        break;
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }
        private void ValidateProperty(string propertyName, decimal? value)
        {
            try
            {
                ClearErrors(propertyName);
                switch (propertyName)
                {

                    case nameof(Margin):
                        if (!value.HasValue || value == 0) AddError(propertyName, "El margen es requerido");
                        break;
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
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
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }

        }
        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
        private void ClearErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                RaiseErrorsChanged(propertyName);
            }
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
        private async Task LoadListAsync()
        {
            try
            {

                IsBusy = true;

                dynamic variables = new ExpandoObject();
                variables.accountingAccountsFilters = new ExpandoObject();
                variables.accountingAccountsFilters.only_auxiliary_accounts = true;


                variables.taxCategoryFilter = new ExpandoObject();
               
                string query = GetQueries();
                TaxDataContext result = await _taxService.GetDataContextAsync<TaxDataContext>(query, variables);

                AccountingAccountOperations = [.. Context.AutoMapper.Map<ObservableCollection<AccountingAccountGraphQLModel>>(result.AccountingAccounts?.Entries)];
                AccountingAccountDevolutions = [.. Context.AutoMapper.Map<ObservableCollection<AccountingAccountGraphQLModel>>(result.AccountingAccounts?.Entries)];
                TaxCategories = [.. Context.AutoMapper.Map<ObservableCollection<TaxCategoryGraphQLModel>>(result.TaxCategories.Entries)];

                AccountingAccountOperations.Insert(0, new AccountingAccountGraphQLModel() { Id = 0, Name = "SELECCIONE CUENTA CONTABLE" });
                AccountingAccountDevolutions.Insert(0, new AccountingAccountGraphQLModel() { Id = 0, Name = "USAR LA CUENTA DE LA TRANSACCIÓN ORIGINAL" });

                GeneratedTaxAccountId = Entity?.GeneratedTaxAccount != null ? Entity.GeneratedTaxAccount.Id : null;
                DeductibleTaxAccountId = Entity?.DeductibleTaxAccount != null ? Entity.DeductibleTaxAccount.Id : null;

                GeneratedTaxRefundAccountId = Entity?.GeneratedTaxRefundAccount != null ?  Entity.GeneratedTaxRefundAccount.Id : null;
                DeductibleTaxRefundAccountId = Entity?.DeductibleTaxRefundAccount != null ? Entity.DeductibleTaxRefundAccount.Id : null;


            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
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

        public string GetQueries()
        {
            var taxCategoriesFields = FieldSpec<PageType<TaxCategoryGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)

                    .Field(e => e.Name)
                    .Field(e => e.Prefix)
                    .Field(e => e.GeneratedTaxRefundAccountIsRequired)
                    .Field(e => e.GeneratedTaxAccountIsRequired)
                    .Field(e => e.DeductibleTaxRefundAccountIsRequired)
                    .Field(e => e.DeductibleTaxAccountIsRequired)
                    )
                .Build();
            var accountingAccountFields = FieldSpec<PageType<AccountingAccountGraphQLModel>>
               .Create()
               .SelectList(it => it.Entries, entries => entries
                   .Field(e => e.Id)
                   .Field(e => e.Name)
                   .Field(e => e.Code)
                   )
               .Build();

            var taxCategoriesParameters = new GraphQLQueryParameter("filters", "TaxCategoryFilters");
            var accountingAccountParameters = new GraphQLQueryParameter("filters", "AccountingAccountFilters");

            var taxCategoriesFragment = new GraphQLQueryFragment("taxCategoriesPage", [taxCategoriesParameters], taxCategoriesFields, "TaxCategories");
            var AccountingAccountFragment = new GraphQLQueryFragment("accountingAccountsPage", [accountingAccountParameters], accountingAccountFields, "AccountingAccounts");

            var builder = new GraphQLQueryBuilder(
            [taxCategoriesFragment, AccountingAccountFragment]
        );
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
