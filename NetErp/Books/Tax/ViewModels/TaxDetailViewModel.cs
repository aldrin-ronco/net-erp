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
using NetErp.Books.AccountingAccountGroups.DTO;
using NetErp.Books.Tax.ViewModels;
using NetErp.Helpers;
using Services.Books.DAL.PostgreSQL;
using Services.Global.DAL.PostgreSQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static Chilkat.Http;
using static Dictionaries.BooksDictionaries;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NetErp.Books.Tax.ViewModels
{
    public class TaxDetailViewModel : Screen, INotifyDataErrorInfo
    {
        public readonly IGenericDataAccess<TaxGraphQLModel> TaxService = IoC.Get<IGenericDataAccess<TaxGraphQLModel>>();


        public TaxDetailViewModel(TaxViewModel context, TaxGraphQLModel? entity)
        {


            Context = context;
            _errors = new Dictionary<string, List<string>>();


            if (entity != null)
            {
                _entity = entity;

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
            SelectedTaxTypeGraphQLModel = TaxTypes.FirstOrDefault(f => f.Id == entity.TaxType.Id);
            if (entity.GeneratedTaxAccount != null) { SelectedGeneratedTaxAccount = AccountingAccountOperations.FirstOrDefault(f => f.Id == entity.GeneratedTaxAccount.Id);  } 
            if (entity.GeneratedTaxRefundAccount != null) { SelectedGeneratedTaxRefundAccount = AccountingAccountDevolutions.FirstOrDefault(f => f.Id == entity.GeneratedTaxRefundAccount.Id); }
            if (entity.DeductibleTaxAccount != null) { SelectedDeductibleTaxAccount = AccountingAccountOperations.FirstOrDefault(f => f.Id == entity.DeductibleTaxAccount.Id); }
            if (entity.DeductibleTaxRefundAccount != null) { SelectedDeductibleTaxRefundAccount = AccountingAccountDevolutions.FirstOrDefault(f => f.Id == entity.DeductibleTaxRefundAccount.Id); }
            
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
        private ObservableCollection<TaxTypeGraphQLModel> _taxTypes;

        public ObservableCollection<TaxTypeGraphQLModel> TaxTypes
        {
            get { return _taxTypes; }
            set
            {
                if (_taxTypes != value)
                {
                    _taxTypes = value;
                    NotifyOfPropertyChange(nameof(TaxTypes));
                }
            }
        }
        private TaxTypeGraphQLModel? _selectedTaxTypeGraphQLModel;
        public TaxTypeGraphQLModel? SelectedTaxTypeGraphQLModel
        {
            get { return _selectedTaxTypeGraphQLModel; }
            set
            {
                if (_selectedTaxTypeGraphQLModel != value)
                {
                    _selectedTaxTypeGraphQLModel = value;
                    NotifyOfPropertyChange(nameof(SelectedTaxTypeGraphQLModel));
                    NotifyOfPropertyChange(nameof(IsEnabledSelectedGeneratedTaxAccount));
                    NotifyOfPropertyChange(nameof(IsEnabledSelectedGeneratedTaxRefundAccount));
                    NotifyOfPropertyChange(nameof(IsEnabledSelectedDeductibleTaxAccount));
                    NotifyOfPropertyChange(nameof(IsEnabledSelectedDeductibleTaxRefundAccount));
                    ValidateProperties();
                    NotifyOfPropertyChange(nameof(CanSave));

                }
            }
        }

        public int Id { get; set; }


        private string _name;
        private decimal? _margin;
        private AccountingAccountGraphQLModel _selectedGeneratedTaxAccount;
        private AccountingAccountGraphQLModel _selectedGeneratedTaxRefundAccount;
        private AccountingAccountGraphQLModel _selectedDeductibleTaxAccount;
        private AccountingAccountGraphQLModel _selectedDeductibleTaxRefundAccount;
        private TaxTypeGraphQLModel _taxType;
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
                    NotifyOfPropertyChange(nameof(CanSave));

                }
            }
        }
        public AccountingAccountGraphQLModel SelectedGeneratedTaxAccount
        {
            get { return _selectedGeneratedTaxAccount; }
            set
            {
                if (_selectedGeneratedTaxAccount != value)
                {
                    _selectedGeneratedTaxAccount = value;
                    ValidateProperty(nameof(SelectedGeneratedTaxAccount), SelectedGeneratedTaxAccount.Id);
                    NotifyOfPropertyChange(nameof(SelectedGeneratedTaxAccount));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        public AccountingAccountGraphQLModel SelectedGeneratedTaxRefundAccount
        {
            get { return _selectedGeneratedTaxRefundAccount; }
            set
            {
                if (_selectedGeneratedTaxRefundAccount != value)
                {
                    _selectedGeneratedTaxRefundAccount = value;
                    ValidateProperty(nameof(SelectedGeneratedTaxRefundAccount), SelectedGeneratedTaxRefundAccount.Id);
                    NotifyOfPropertyChange(nameof(SelectedGeneratedTaxRefundAccount));

                }
            }
        }
        public AccountingAccountGraphQLModel SelectedDeductibleTaxAccount
        {
            get { return _selectedDeductibleTaxAccount; }
            set
            {
                if (_selectedDeductibleTaxAccount != value)
                {
                    _selectedDeductibleTaxAccount = value;
                    ValidateProperty(nameof(SelectedDeductibleTaxAccount), SelectedDeductibleTaxAccount.Id);
                    NotifyOfPropertyChange(nameof(CanSave));
                    NotifyOfPropertyChange(nameof(SelectedDeductibleTaxAccount));

                }
            }
        }
        public AccountingAccountGraphQLModel SelectedDeductibleTaxRefundAccount
        {
            get { return _selectedDeductibleTaxRefundAccount; }
            set
            {
                if (_selectedDeductibleTaxRefundAccount != value)
                {
                    _selectedDeductibleTaxRefundAccount = value;
                    ValidateProperty(nameof(SelectedDeductibleTaxRefundAccount), SelectedDeductibleTaxRefundAccount.Id);
                    NotifyOfPropertyChange(nameof(CanSave));
                    NotifyOfPropertyChange(nameof(SelectedDeductibleTaxRefundAccount));

                }
            }
        }

        private bool _isNewRecord => Entity?.Id > 0 ? false : true;
        public bool IsEnabledSelectedGeneratedTaxAccount => (SelectedTaxTypeGraphQLModel != null && SelectedTaxTypeGraphQLModel?.Id >  0 &&  SelectedTaxTypeGraphQLModel.GeneratedTaxAccountIsRequired.Equals(true));
        public bool IsEnabledSelectedGeneratedTaxRefundAccount => (SelectedTaxTypeGraphQLModel != null && SelectedTaxTypeGraphQLModel?.Id > 0 && SelectedTaxTypeGraphQLModel.GeneratedTaxRefundAccountIsRequired.Equals(true) && SelectedTaxTypeGraphQLModel.GeneratedTaxAccountIsRequired.Equals(true));
        public bool IsEnabledSelectedDeductibleTaxAccount => (SelectedTaxTypeGraphQLModel != null && SelectedTaxTypeGraphQLModel?.Id > 0 && SelectedTaxTypeGraphQLModel.DeductibleTaxAccountIsRequired.Equals(true));
        public bool IsEnabledSelectedDeductibleTaxRefundAccount => (SelectedTaxTypeGraphQLModel != null && SelectedTaxTypeGraphQLModel?.Id > 0 && SelectedTaxTypeGraphQLModel.DeductibleTaxRefundAccountIsRequired.Equals(true) && SelectedTaxTypeGraphQLModel.DeductibleTaxAccountIsRequired.Equals(true));


        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    NotifyOfPropertyChange(nameof(IsActive));

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
                if (_saveCommand is null) _saveCommand = new AsyncCommand(Save, CanSave);
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

        public async Task Save()
        {
            try
            {
                IsBusy = true;
                Refresh();
                TaxGraphQLModel result = await ExecuteSave();
                var pageResult = await LoadPage();
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnCurrentThreadAsync(new TaxCreateMessage() { CreatedTax = result, Taxs = pageResult.PageResponse.Rows });
                }
                else
                {
                    await Context.EventAggregator.PublishOnCurrentThreadAsync(new TaxUpdateMessage() { UpdatedTax = result, Taxs = pageResult.PageResponse.Rows });
                }
                // Context.EnableOnViewReady = false;
                await Context.ActivateMasterViewModelAsync();
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

        public async Task<TaxGraphQLModel> ExecuteSave()
        {
            dynamic variables = new ExpandoObject();
            variables.Data = new ExpandoObject();
            variables.Data.name = Name;
            variables.Data.margin = Margin;
            variables.Data.formula = "Formula por definir";
            variables.Data.alternativeFormula = "AlternativeFormula por definir";
            variables.Data.taxTypeId = SelectedTaxTypeGraphQLModel?.Id;
            variables.Data.generatedTaxAccountId = SelectedGeneratedTaxAccount?.Id;
            variables.Data.generatedTaxRefundAccountId = SelectedGeneratedTaxRefundAccount?.Id;
            variables.Data.deductibleTaxAccountId = SelectedDeductibleTaxAccount?.Id;
            variables.Data.deductibleTaxRefundAccountId = SelectedDeductibleTaxRefundAccount?.Id;
            variables.Data.isActive = IsActive;

            if (IsNewRecord)
            {
                return await CreateAsync(variables);
            }
            else
            {
                return await UpdateAsync(variables);
            }

        }
        private async Task<TaxGraphQLModel> CreateAsync(dynamic variables)
        {
            try
            {
                IsBusy = true;
                var query = @"
                    mutation($data: CreateTaxInput!){
                     CreateResponse: createTax(data: $data){
                        id
                        name
                        margin  
                        generatedTaxAccount {
                          id
                          name
                        }
                        generatedTaxRefundAccount {
                        id
                        name
                        }
                        deductibleTaxAccount {
                          id 
                          name
                        }
                        deductibleTaxRefundAccount {
                          id
                          name
                        }
                      }
                    }";

                TaxGraphQLModel record = await TaxService.Create(query, variables);
                return record;
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                IsBusy = false;
            }

        }
        public async Task<TaxGraphQLModel> UpdateAsync(dynamic variables)
        {
            try
            {

                var query = @"
                    mutation($data: UpdateTaxInput!, $id : Int!){
                     UpdateResponse: updateTax(data: $data, id: $id){
                        id
                        name
                        margin  
                        generatedTaxAccount {
                          id
                          name
                        }
                        generatedTaxRefundAccount {
                        id
                        name
                        }
                        deductibleTaxAccount {
                          id 
                          name
                        }
                        deductibleTaxRefundAccount {
                          id
                          name
                        }
                      }
                    }";
                variables.id = Entity.Id;


                return await TaxService.Update(query, variables);

            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public bool CanSave
        {
            get
            {


                if (_errors.Count > 0) { return false; }

                return true;
            }
        }
        public bool HasErrors => _errors.Count > 0;

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.SetFocus(() => Name);

            ValidateProperties();
        }
        public void GoBack(object p)
        {
            CleanUpControls();
            _ = Task.Run(() => Context.ActivateMasterViewModelAsync());

        }
        private void ValidateProperties()
        {
            ValidateProperty(nameof(Name), Name);
            ValidateProperty(nameof(Margin), Margin);
            ValidateProperty(nameof(SelectedTaxTypeGraphQLModel), SelectedTaxTypeGraphQLModel?.Id);
            ValidateProperty(nameof(SelectedGeneratedTaxAccount), SelectedGeneratedTaxAccount?.Id);
            ValidateProperty(nameof(SelectedGeneratedTaxRefundAccount), SelectedGeneratedTaxRefundAccount?.Id);
            ValidateProperty(nameof(SelectedDeductibleTaxAccount), SelectedDeductibleTaxAccount?.Id);
            ValidateProperty(nameof(SelectedDeductibleTaxRefundAccount), SelectedDeductibleTaxRefundAccount?.Id);
          
        }
        public void CleanUpControls()
        {
            
        }
        public async Task<IGenericDataAccess<TaxGraphQLModel>.PageResponseType> LoadPage()
        {
            string query = Context.listquery;

            dynamic variables = new ExpandoObject();
            variables.filter = new ExpandoObject();
            variables.filter.Pagination = new ExpandoObject();
            variables.filter.Pagination.Page = 1;
            variables.filter.Pagination.PageSize = 50;
            variables.filter.and = new ExpandoObject[]
              {
                     new(),
                     new()
              };

            variables.filter.and[0].isActive = new ExpandoObject();
            variables.filter.and[0].isActive.@operator = "=";
            variables.filter.and[0].isActive.value = true;



            return await TaxService.GetPage(query, variables);
        }
        private void ValidateProperty(string propertyName, int? value)
        {
            try
            {

                ClearErrors(propertyName);
                switch (propertyName)
                {
                    case nameof(SelectedTaxTypeGraphQLModel):
                        if (!value.HasValue || value == 0) AddError(propertyName, "Debe seleccionar un tipo de impuesto");
                        break;

                    case nameof(SelectedGeneratedTaxAccount):
                        if ((SelectedTaxTypeGraphQLModel != null && SelectedTaxTypeGraphQLModel.GeneratedTaxAccountIsRequired) &&  ( !value.HasValue || value == 0)) AddError(propertyName, "Debe seleccionar un GeneratedTaxAccountId");
                        break;
                    case nameof(SelectedGeneratedTaxRefundAccount):
                        if ((SelectedTaxTypeGraphQLModel != null && SelectedTaxTypeGraphQLModel.GeneratedTaxRefundAccountIsRequired) && (!value.HasValue || value == 0)) AddError(propertyName, "Debe seleccionar un GeneratedTaxAccountId");
                        break;
                    case nameof(SelectedDeductibleTaxAccount):
                        if ((SelectedTaxTypeGraphQLModel != null && SelectedTaxTypeGraphQLModel.DeductibleTaxAccountIsRequired) && (!value.HasValue || value == 0)) AddError(propertyName, "Debe seleccionar un GeneratedTaxAccountId");
                        break;
                    case nameof(SelectedDeductibleTaxRefundAccount):
                        if ((SelectedTaxTypeGraphQLModel != null && SelectedTaxTypeGraphQLModel.DeductibleTaxRefundAccountIsRequired) &&  (!value.HasValue || value == 0)) AddError(propertyName, "Debe seleccionar un GeneratedTaxAccountId");
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
               
                string query = @"
                query ($accountingAccountFilter: AccountingAccountFilterInput, $taxTypeFilter : TaxTypeFilterInput) {
                  accountingAccounts(filter: $accountingAccountFilter) {
                    id
                    code
                    name
                  
                }
                TaxTypes : taxTypes(filter : $taxTypeFilter){
        
                       id
                       name
                      generatedTaxAccountIsRequired
                      generatedTaxRefundAccountIsRequired
                      deductibleTaxAccountIsRequired
                      deductibleTaxRefundAccountIsRequired
                      prefix
      
      
                    }
                }
                ";

                dynamic variables = new ExpandoObject();
                variables.accountingAccountFilter = new ExpandoObject();
                variables.accountingAccountFilter.code = new ExpandoObject();
                variables.accountingAccountFilter.code.@operator = new List<string>() { "length", ">=" };
                variables.accountingAccountFilter.code.value = 8;

                variables.taxTypeFilter = new ExpandoObject();
                TaxDataContext result = await TaxService.GetDataContext<TaxDataContext>(query, variables);
               
                AccountingAccountOperations = [.. Context.AutoMapper.Map<ObservableCollection<AccountingAccountGraphQLModel>>(result.AccountingAccounts)];
                AccountingAccountDevolutions = [.. Context.AutoMapper.Map<ObservableCollection<AccountingAccountGraphQLModel>>(result.AccountingAccounts)];
                TaxTypes = [.. Context.AutoMapper.Map<ObservableCollection<TaxTypeGraphQLModel>>(result.TaxTypes)];

                AccountingAccountOperations.Insert(0, new AccountingAccountGraphQLModel() { Id = 0, Name = "SELECCIONE CUENTA CONTABLE" });
                AccountingAccountDevolutions.Insert(0, new AccountingAccountGraphQLModel() { Id = 0, Name = "USAR LA CUENTA DE LA TRANSACCIÓN ORIGINAL" });
                
                SelectedGeneratedTaxAccount = Entity?.GeneratedTaxAccount !=null ? AccountingAccountOperations.First(f => f.Id == Entity?.GeneratedTaxAccount.Id) :   AccountingAccountOperations.First(f => f.Id == 0);
                SelectedDeductibleTaxAccount = Entity?.DeductibleTaxAccount != null ? AccountingAccountOperations.First(f => f.Id == Entity?.DeductibleTaxAccount.Id) : AccountingAccountOperations.First(f => f.Id == 0);

                SelectedGeneratedTaxRefundAccount = Entity?.GeneratedTaxRefundAccount != null ? AccountingAccountDevolutions.First(f => f.Id == Entity?.GeneratedTaxRefundAccount.Id) : AccountingAccountDevolutions.First(f => f.Id == 0);
                SelectedDeductibleTaxRefundAccount = Entity?.DeductibleTaxRefundAccount != null ? AccountingAccountDevolutions.First(f => f.Id == Entity?.DeductibleTaxRefundAccount.Id) : AccountingAccountDevolutions.First(f => f.Id == 0);


               
               
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
    }


    }
