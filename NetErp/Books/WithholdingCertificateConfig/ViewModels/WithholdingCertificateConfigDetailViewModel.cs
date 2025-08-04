using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Books.AccountingAccountGroups.DTO;
using NetErp.Books.AccountingEntities.ViewModels;
using NetErp.Global.CostCenters.DTO;
using NetErp.Helpers;
using Ninject.Infrastructure.Language;
using Services.Books.DAL.PostgreSQL;
using Services.Global.DAL.PostgreSQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using Xceed.Wpf.Toolkit.Primitives;
using static Dictionaries.BooksDictionaries;

namespace NetErp.Books.WithholdingCertificateConfig.ViewModels
{
    public class WithholdingCertificateConfigDetailViewModel : Screen, INotifyDataErrorInfo
    {
        public readonly IGenericDataAccess<CostCenterGraphQLModel> CostCenterService = IoC.Get<IGenericDataAccess<CostCenterGraphQLModel>>();
        public readonly IGenericDataAccess<AccountingAccountGroupGraphQLModel> AccountingAccountGroupGraphQLModelService = IoC.Get<IGenericDataAccess<AccountingAccountGroupGraphQLModel>>();
        public IGenericDataAccess<WithholdingCertificateConfigGraphQLModel> WithholdingCertificateConfigService { get; set; } = IoC.Get<IGenericDataAccess<WithholdingCertificateConfigGraphQLModel>>();


        public WithholdingCertificateConfigDetailViewModel(WithholdingCertificateConfigViewModel context, WithholdingCertificateConfigGraphQLModel? entity)
        {
            _errors = new Dictionary<string, List<string>>();
            Context = context;

            if (entity != null)
            {
                Entity = entity;
                Name = entity.Name;
                Description = entity.Description;
                IsNewRecord = false;
            }
            else
            {
                IsNewRecord = true;
                
            }

            Context.EventAggregator.SubscribeOnUIThread(this);
            var joinable = new JoinableTaskFactory(new JoinableTaskContext());
            joinable.Run(async () => await Initialize());

        }
        #region Propiedades
        // Context
        private WithholdingCertificateConfigViewModel _context;
        public WithholdingCertificateConfigViewModel Context
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

        // Identity
        private WithholdingCertificateConfigGraphQLModel? _entity;
        public WithholdingCertificateConfigGraphQLModel? Entity
        {
            get { return _entity; }
            set {
                if (_entity != value)
                {
                    _entity = value;
                    NotifyOfPropertyChange(nameof(_entity));
                }
            }
        }
           
        private string _description;
        public string Description
        {
            get { return _description; }
            set
            {
                if (_description != value)
                {
                    _description = value;
                    NotifyOfPropertyChange(nameof(Description));
                    this.NotifyOfPropertyChange(nameof(this.CanSave));
                    ValidateProperty(nameof(Description), value);
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
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperty(nameof(Name), value);
                }
            }
        }

        Dictionary<string, List<string>> _errors;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        private bool _isNewRecord = false;

        public bool IsNewRecord
        {
            get { return _isNewRecord; }
            set
            {
                if (_isNewRecord != value)
                {
                    _isNewRecord = value;
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.SetFocus(() => Name);
            ValidateProperties();
        }
        
        public bool CanSave
        {
            get
            {
                if (string.IsNullOrEmpty(Name)) return false;

                // Debe haber ingresado una descripcion
                if (string.IsNullOrEmpty(Description)) return false;

                if (SelectedCostCenter == null || SelectedCostCenter.Id == 0) return false;

                if (!AccountingAccounts.Any(x => x.IsChecked == true)) return false;

                // Debe haber ingresado un nombre
                return true;
            }
        }
        public bool HasErrors => _errors.Count > 0;

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null;
            return _errors[propertyName];
        }
        private CaptureTypeEnum _selectedCaptureType;


        private CostCenterDTO _selectedCostCenter;
        public CostCenterDTO SelectedCostCenter
        {
            get { return _selectedCostCenter; }
            set
            {
                if (_selectedCostCenter != value)
                {
                    _selectedCostCenter = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenter));
                    this.NotifyOfPropertyChange(nameof(this.CanSave));
                }
            }
        }
        private ObservableCollection<CostCenterDTO> _costCenters;


        private ObservableCollection<AccountingAccountGroupDetailDTO> _accountingAccounts;

        public ObservableCollection<CostCenterDTO> CostCenters
        {
            get { return _costCenters; }
            set
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        }
        public bool WithholdingCertificateConfigComboBoxIsEnabled
        {
            get
            {
                return true;
            }
        }
        public ObservableCollection<AccountingAccountGroupDetailDTO> AccountingAccounts
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
        public bool CaptureInfoAsPN => true;
        #endregion
        #region command
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
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        public void GoBack(object p)
        {
            _ = Task.Run(() => Context.ActivateMasterViewModelAsync());
            CleanUpControls();
        }
        public void CleanUpControls()
        {
            Name = "";
            Description = "";
            SelectedCostCenter = CostCenters.First(f => f.Id == 0);
        }
        public bool CanGoBack(object p)
        {
            return !IsBusy;
        }
        #endregion

        public async Task Initialize()
        {

            await getCostCentersAsync();
            await getAccountingAccountsAsync();

        }

        public async Task SaveAsync()
        {

            try
            {
                IsBusy = true;
                Refresh();
                WithholdingCertificateConfigGraphQLModel result = await ExecuteSave();

                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnCurrentThreadAsync(new WithholdingCertificateConfigCreateMessage() { CreatedWithholdingCertificateConfig = result });
                }
                else
                {
                    await Context.EventAggregator.PublishOnCurrentThreadAsync(new WithholdingCertificateConfigUpdateMessage() { UpdatedWithholdingCertificateConfig = result });
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
            //////////////////
           
        }
        public async Task<WithholdingCertificateConfigGraphQLModel> ExecuteSave()
        {
            List<int> accountingAccountsIds = [.. AccountingAccounts.Where(f => f.IsChecked == true).Select(x => x.Id)];
            dynamic data = new ExpandoObject();
            data.name = Name;
            data.description = Description;
            data.costCenterId = SelectedCostCenter.Id;
            data.accountingAccountsIds = accountingAccountsIds;
            if (IsNewRecord)
            {
               return await CreateAsync(data);
            }
            else
            {
                return await UpdateAsync(data);
            }
        }
        public async Task<WithholdingCertificateConfigGraphQLModel> UpdateAsync(dynamic data)
        {
            try
            {
                IsBusy = true;
                string query = @"
                 mutation($data: UpdateWithholdingCertificateConfigInput!, $id: Int!){
                UpdateResponse: updateWithholdingCertificateConfig(data: $data, id: $id){
                        name
                        description,
                        accountingAccounts  {
                            name
                            id
                        },
                        costCenter{
                          id,
                          name,
                          department{ name},
                          city { name}
                        }
                    }
                 }";

                dynamic variables = new ExpandoObject();
                variables.data = data;
                variables.id = Entity.Id;
                WithholdingCertificateConfigGraphQLModel result = await WithholdingCertificateConfigService.Update(query, variables);
                return result;

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
        public async Task<WithholdingCertificateConfigGraphQLModel> CreateAsync(dynamic data)
        {
            try
            {
                IsBusy = true;
                string query = @"
                 mutation($data: CreateWithholdingCertificateConfigInput!){
                 CreateResponse : createWithholdingCertificateConfig(data: $data){
                    id
                    name
                  }
                }";

                dynamic variables = new ExpandoObject();
                variables.data = data;

                WithholdingCertificateConfigGraphQLModel result = await WithholdingCertificateConfigService.Create(query, variables);
                return result;
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
        private async Task getCostCentersAsync()
        {
            string query = @"
                            query(){
                               ListResponse:  costCenters(){
                                id
                                name
                                address
                                city  {
                                  id
                                  name
                                  department {
                                    id
                                    name
                                  }
                                }
  
                              }
                            }";
            dynamic variables = new ExpandoObject();
            var source = await CostCenterService.GetList(query, variables);
            CostCenters = Context.AutoMapper.Map<ObservableCollection<CostCenterDTO>>(source);

            CostCenters.Insert(0, new CostCenterDTO() { Id = 0, Name = "SELECCIONE CENTRO DE COSTO" });
            if (!IsNewRecord)
            {
                SelectedCostCenter = CostCenters.First(f => f.Id == Entity?.CostCenter?.Id);
            }
            else
            {
                SelectedCostCenter = CostCenters.First(f => f.Id == 0);
            }


        }
        private async Task getAccountingAccountsAsync()
        {
            string query = @"query($filter : AccountingAccountGroupFilterInput!){
                                   ListResponse : accountingAccountGroups(filter:  $filter){
                                    name
                                    key
                                    accountingAccounts  {
                                      name,
                                      code
                                      id
                                    }
   
  
                                  }
                                }";
            dynamic variables = new ExpandoObject();
            variables.filter = new ExpandoObject();
            variables.filter.key = "CTAS_RETS_VTAS";
            IEnumerable<AccountingAccountGroupGraphQLModel> source = await AccountingAccountGroupGraphQLModelService.GetList(query, variables);

            ObservableCollection<AccountingAccountGroupDetailDTO> acgd = Context.AutoMapper.Map<ObservableCollection<AccountingAccountGroupDetailDTO>>(source.First().AccountingAccounts);
            foreach (var accountingAccount in acgd)
            {
                accountingAccount.Context = this;
                accountingAccount.IsChecked = Entity?.AccountingAccounts?.FirstOrDefault(x => x.Id == accountingAccount.Id) != null ? true : false;
            }

            AccountingAccounts = [.. acgd];

        }
        private void ClearErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
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
                        if (string.IsNullOrEmpty(value)  ) AddError(propertyName, "El nombre no puede estar vacío");
                        break;
                    case nameof(Description):
                        if (string.IsNullOrEmpty(value)) AddError(propertyName, "La descripción no puede estar vacío");
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
            ValidateProperty(nameof(Description), Description);
           
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
    }
}
