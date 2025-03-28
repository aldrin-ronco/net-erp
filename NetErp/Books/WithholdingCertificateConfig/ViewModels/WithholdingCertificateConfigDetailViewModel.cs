using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Books.AccountingEntities.ViewModels;
using NetErp.Global.CostCenters.DTO;
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
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using Xceed.Wpf.Toolkit.Primitives;

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
                IsNewRecord = false;
            }
            else
            {
                IsNewRecord = true;
                Entity = new WithholdingCertificateConfigGraphQLModel();
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
            set
            {
                if (_entity != value)
                {
                    _entity = value;                  
                    NotifyOfPropertyChange(nameof(_entity));
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


        public bool HasErrors => _errors.Count > 0;

        public IEnumerable GetErrors(string? propertyName)
        {
            throw new NotImplementedException();
        }
        
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
                   
                }
            }
        }
        private ObservableCollection<CostCenterDTO> _costCenters;

      
        private ObservableCollection<AccountingAccountGroupDetailGraphQLModel> _accountingAccounts;

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
        public ObservableCollection<AccountingAccountGroupDetailGraphQLModel> AccountingAccounts
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
            Entity = null;
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
            if (IsNewRecord)
            {
                await CreateAsync();
            }
            else
            {
                await UpdateAsync();
            }
        }
        public async Task UpdateAsync()
        {
            try
            {
                IsBusy = true;
                List<int> accountingAccountsIds = [.. AccountingAccounts.Where(f => f.IsChecked == true).Select(x => x.Id)];
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
                variables.data = new ExpandoObject();
                variables.data.name = Entity.Name;
                variables.data.description = Entity.Description;
                variables.data.costCenterId = SelectedCostCenter.Id;
                variables.data.accountingAccountsIds = accountingAccountsIds;
                variables.id = Entity.Id;
                WithholdingCertificateConfigGraphQLModel result = await WithholdingCertificateConfigService.Update(query, variables);
                Entity.AccountingAccounts = result.AccountingAccounts;
                GoBack(null);
               
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task CreateAsync()
        {
            try
            {
                IsBusy = true;
                List<int> accountingAccountsIds = [.. AccountingAccounts.Where(f => f.IsChecked == true).Select(x => x.Id)];
                string query = @"
                 mutation($data: CreateWithholdingCertificateConfigInput!){
                 CreateResponse : createWithholdingCertificateConfig(data: $data){
                    id
                    name
                  }
                }";

                dynamic variables = new ExpandoObject();
                variables.data = new ExpandoObject();
                variables.data.name = Entity.Name;
                variables.data.description = Entity.Description;
                variables.data.costCenterId = SelectedCostCenter.Id;
                variables.data.accountingAccountsIds = accountingAccountsIds;
                
                WithholdingCertificateConfigGraphQLModel result = await WithholdingCertificateConfigService.Create(query, variables);
                Entity.AccountingAccounts = result.AccountingAccounts;
                GoBack(null);
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
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
            if(!IsNewRecord)
            {
                SelectedCostCenter = CostCenters.First(f => f.Id == Entity?.CostCenter?.Id);
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
            var acgd = new ObservableCollection<AccountingAccountGroupDetailGraphQLModel>(source.First().AccountingAccounts);
             acgd.ForEach(f =>   f.IsChecked = Entity?.AccountingAccounts?.FirstOrDefault(x => x.Id == f.Id) != null ? true : false  );
            AccountingAccounts = acgd;

        }
    }
}
