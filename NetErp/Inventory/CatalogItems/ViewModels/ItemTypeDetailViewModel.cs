using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Billing;
using Models.Books;
using Models.Inventory;
using NetErp.Helpers;
using NetErp.Inventory.CatalogItems.DTO;
using NetErp.IoContainer;
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

namespace NetErp.Inventory.CatalogItems.ViewModels
{
    public class ItemTypeDetailViewModel : Screen, INotifyDataErrorInfo
    {
        public readonly IGenericDataAccess<ItemTypeGraphQLModel> ItemTypeService = IoC.Get<IGenericDataAccess<ItemTypeGraphQLModel>>();

        private CatalogViewModel _context;

        public CatalogViewModel Context
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

        private ObservableCollection<MeasurementUnitDTO> _measurementUnits;

        public ObservableCollection<MeasurementUnitDTO> MeasurementUnits
        {
            get { return _measurementUnits; }
            set 
            {
                if (_measurementUnits != value) 
                {
                    _measurementUnits = value;
                    NotifyOfPropertyChange(nameof(MeasurementUnits));
                }
            }
        }

        private ObservableCollection<AccountingGroupDTO> _accountingGroups;

        public ObservableCollection<AccountingGroupDTO> AccountingGroups
        {
            get { return _accountingGroups; }
            set 
            {
                if (_accountingGroups != value) 
                {
                    _accountingGroups = value;
                    NotifyOfPropertyChange(nameof(AccountingGroups));
                }
            }
        }

        private MeasurementUnitDTO _selectedMeasurementUnitByDefault;

        public MeasurementUnitDTO SelectedMeasurementUnitByDefault
        {
            get { return _selectedMeasurementUnitByDefault; }
            set 
            {
                if (_selectedMeasurementUnitByDefault != value)
                {
                    _selectedMeasurementUnitByDefault = value;
                    NotifyOfPropertyChange(nameof(SelectedMeasurementUnitByDefault));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private AccountingGroupDTO _selectedAccountingGroupByDefault;

        public AccountingGroupDTO SelectedAccountingGroupByDefault
        {
            get { return _selectedAccountingGroupByDefault; }
            set
            {
                if (_selectedAccountingGroupByDefault != value)
                {
                    _selectedAccountingGroupByDefault = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingGroupByDefault));
                    NotifyOfPropertyChange(nameof(CanSave));
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

        private bool _stockControlEnable;

        public bool StockControlEnable
        {
            get { return _stockControlEnable; }
            set 
            {
                if (_stockControlEnable != value)
                {
                    _stockControlEnable = value;
                    NotifyOfPropertyChange(nameof(StockControlEnable));
                }
            }
        }


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
                    ValidateProperty(nameof(Name), value);
                    NotifyOfPropertyChange(nameof(Name));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _prefixChar;

        public string PrefixChar
        {
            get { return _prefixChar; }
            set
            {
                if (_prefixChar != value)
                {
                    _prefixChar = value;
                    ValidateProperty(nameof(PrefixChar), value);
                    NotifyOfPropertyChange(nameof(PrefixChar));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _stockControl;

        public bool StockControl
        {
            get { return _stockControl; }
            set
            {
                if (_stockControl != value)
                {
                    _stockControl = value;
                    NotifyOfPropertyChange(nameof(StockControl));
                }
            }
        }

        private int _catalogId;

        public int CatalogId
        {
            get { return _catalogId; }
            set
            {
                if (_catalogId != value)
                {
                    _catalogId = value;
                    NotifyOfPropertyChange(nameof(CatalogId));
                }
            }
        }


        private int _measurementUnitIdByDefault;

        public int MeasurementUnitIdByDefault
        {
            get { return _measurementUnitIdByDefault; }
            set 
            {
                if (_measurementUnitIdByDefault != value)
                {
                    _measurementUnitIdByDefault = value;
                    NotifyOfPropertyChange(nameof(MeasurementUnitIdByDefault));
                }
            }
        }

        private int _accountingGroupIdByDefault;

        public int AccountingGroupIdByDefault
        {
            get { return _accountingGroupIdByDefault; }
            set
            {
                if (_accountingGroupIdByDefault != value)
                {
                    _accountingGroupIdByDefault = value;
                    NotifyOfPropertyChange(nameof(AccountingGroupIdByDefault));
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


        public bool IsNewRecord => Id == 0;

        public async Task Save()
        {
            try
            {
                IsBusy = true;
                Refresh();
                ItemTypeGraphQLModel result = await ExecuteSave();
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new ItemTypeCreateMessage() { CreatedItemType = result});
                }
                else
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new ItemTypeUpdateMessage() { UpdatedItemType = result});
                }
                Context.EnableOnActivateAsync = false;
                await Context.ActivateMasterView();
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

        public async Task<ItemTypeGraphQLModel> ExecuteSave()
        {
            string query = string.Empty;

            try
            {
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                
                if(!IsNewRecord) variables.Id = Id;

                variables.Data.Name = Name;
                variables.Data.PrefixChar = PrefixChar;
                variables.Data.StockControl = StockControl;
                variables.Data.CatalogId = CatalogId;
                variables.Data.MeasurementUnitIdByDefault = SelectedMeasurementUnitByDefault.Id;
                variables.Data.AccountingGroupIdByDefault = SelectedAccountingGroupByDefault.Id;

                query = IsNewRecord
                    ? @"
                    mutation($data: CreateItemTypeInput!){
                        CreateResponse: createItemType(data: $data){
                            id
                            name
                            prefixChar
                            stockControl
                            measurementUnitByDefault{
                                id
                                }
                            accountingGroupByDefault{
                                id
                                }
                            catalog{
                                id
                                }
                            }
                        }" : @"
                    mutation($data: UpdateItemTypeInput!, $id: Int!){
                        UpdateResponse: updateItemType(data: $data, id: $id){
                            id
                            name
                            prefixChar
                            stockControl
                            measurementUnitByDefault{
                                id
                                }
                            accountingGroupByDefault{
                                id
                                }
                            }
                        }";
                var result = IsNewRecord ? await ItemTypeService.Create(query, variables) : await ItemTypeService.Update(query, variables);
                return result;
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
                return SelectedMeasurementUnitByDefault != null && SelectedAccountingGroupByDefault != null && !string.IsNullOrEmpty(Name) && PrefixChar.Length == 1 && SelectedMeasurementUnitByDefault.Id > 0 && SelectedAccountingGroupByDefault.Id > 0;
            }
        }

        public void GoBack(object p)
        {
            Context.EnableOnActivateAsync = false;
            MeasurementUnits = [];
            AccountingGroups = [];
            _ = Task.Run(() => Context.ActivateMasterView());
        }

        public bool CanGoBack(object p)
        {
            return true;
        }

        public void CleanUpControls()
        {
            Id = 0;
            Name = string.Empty;
            PrefixChar = string.Empty;
            StockControl = false;
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperties();
            Initialize();
            this.SetFocus(nameof(Name));
        }
        public void Initialize()
        {
            if (IsNewRecord)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MeasurementUnits.Insert(0, new MeasurementUnitDTO() { Id = 0, Name = "<< SELECCIONE UNA UNIDAD DE MEDIDA >> " });
                    AccountingGroups.Insert(0, new AccountingGroupDTO() { Id = 0, Name = "<< SELECCIONE UN GRUPO CONTABLE >>" });
                });

                SelectedMeasurementUnitByDefault = MeasurementUnits.FirstOrDefault(x => x.Id == 0);
                SelectedAccountingGroupByDefault = AccountingGroups.FirstOrDefault(x => x.Id == 0);
                return;
            }
            SelectedMeasurementUnitByDefault = MeasurementUnits.FirstOrDefault(x => x.Id == MeasurementUnitIdByDefault);
            SelectedAccountingGroupByDefault = AccountingGroups.FirstOrDefault(x => x.Id == AccountingGroupIdByDefault);
            return;
        }

        public ItemTypeDetailViewModel(CatalogViewModel context, ObservableCollection<MeasurementUnitDTO> measurementUnits, ObservableCollection<AccountingGroupDTO> accountingGroups)
        {
            Context = context;
            MeasurementUnits = new ObservableCollection<MeasurementUnitDTO>(measurementUnits);
            AccountingGroups = new ObservableCollection<AccountingGroupDTO>(accountingGroups);
            _errors = new Dictionary<string, List<string>>();
        }

        #region "Errors"


        Dictionary<string, List<string>> _errors;

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
                        if (string.IsNullOrEmpty(Name)) AddError(propertyName, "El nombre del tipo de item no puede estar vacío");
                        break;
                    case nameof(PrefixChar):
                        if(string.IsNullOrEmpty(PrefixChar)) AddError(propertyName, "El nombre corto del tipo de item no puede estar vacío");
                        break;
                    default:
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
            ValidateProperty(nameof(PrefixChar), PrefixChar);
        }
        #endregion
    }
}
