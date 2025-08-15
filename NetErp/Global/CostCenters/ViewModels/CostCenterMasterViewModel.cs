using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core;
using DevExpress.Xpo.DB.Helpers;
using Dictionaries;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using Models.Inventory;
using NetErp.Global.CostCenters.DTO;
using NetErp.Global.Modals.ViewModels;
using NetErp.Helpers;
using NetErp.Inventory.CatalogItems.DTO;
using NetErp.Inventory.CatalogItems.ViewModels;
using Services.Inventory.DAL.PostgreSQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetErp.Global.CostCenters.ViewModels
{
    public class CostCenterMasterViewModel : Screen, INotifyDataErrorInfo, 
        IHandle<CostCenterCreateMessage>,
        IHandle<CostCenterUpdateMessage>,
        IHandle<CostCenterDeleteMessage>,
        IHandle<StorageCreateMessage>,
        IHandle<StorageUpdateMessage>,
        IHandle<StorageDeleteMessage>,
        IHandle<CompanyLocationCreateMessage>,
        IHandle<CompanyLocationUpdateMessage>,
        IHandle<CompanyLocationDeleteMessage>,
        IHandle<CompanyUpdateMessage>
    {
        public CostCenterViewModel Context { get; set; }

        private readonly IRepository<CompanyGraphQLModel> _companyService;
        private readonly IRepository<CompanyLocationGraphQLModel> _companyLocationService;
        private readonly IRepository<CostCenterGraphQLModel> _costCenterService;
        private readonly IRepository<StorageGraphQLModel> _storageService;
        private readonly IRepository<CountryGraphQLModel> _countryService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly Helpers.Services.INotificationService _notificationService;

        Dictionary<string, List<string>> _errors;

        #region "TabControls"

        #region "Company"

        private int _companyId;

        public int CompanyId
        {
            get { return _companyId; }
            set 
            {
                if (_companyId != value)
                {
                    _companyId = value;
                    NotifyOfPropertyChange(nameof(CompanyId));
                }
            }
        }


        private string _companyAccountingEntityCompanySearchName;

        public string CompanyAccountingEntityCompanySearchName
        {
            get { return _companyAccountingEntityCompanySearchName; }
            set 
            {
                if (_companyAccountingEntityCompanySearchName != value)
                {
                    _companyAccountingEntityCompanySearchName = value;
                    NotifyOfPropertyChange(nameof(CompanyAccountingEntityCompanySearchName));
                }
            }
        }

        private int _companyAccountingEntityCompanyId;

        public int CompanyAccountingEntityCompanyId
        {
            get { return _companyAccountingEntityCompanyId; }
            set 
            {
                if (_companyAccountingEntityCompanyId != value)
                {
                    _companyAccountingEntityCompanyId = value;
                    NotifyOfPropertyChange(nameof(CompanyAccountingEntityCompanyId));
                }
            }
        }


        private ICommand _searchCompanyAccountingEntityCompanyCommand;
        public ICommand SearchCompanyAccountingEntityCompanyCommand
        {
            get
            {
                if (_searchCompanyAccountingEntityCompanyCommand is null) _searchCompanyAccountingEntityCompanyCommand = new RelayCommand(CanSearchCompanyAccountingEntityCompany, SearchCompanyAccountingEntityCompany);
                return _searchCompanyAccountingEntityCompanyCommand;
            }
        }

        public async void SearchCompanyAccountingEntityCompany(object p)
        {
            string query = @"query($filter: AccountingEntityFilterInput!){
                PageResponse: accountingEntityPage(filter: $filter){
                count
                rows{
                    id
                    searchName
                    identificationNumber
                    verificationDigit
                }
                }
            }";

            string fieldHeader1 = "NIT";
            string fieldHeader2 = "Nombre o razón social";
            string fieldData1 = "IdentificationNumberWithVerificationDigit";
            string fieldData2 = "SearchName";
            var viewModel = new SearchWithTwoColumnsGridViewModel<AccountingEntityGraphQLModel>(query, fieldHeader1, fieldHeader2, fieldData1, fieldData2, null, SearchWithTwoColumnsGridMessageToken.CompanyAccountingEntity, _dialogService);

            await _dialogService.ShowDialogAsync(viewModel, "Búsqueda de terceros");
        }

        public bool CanSearchCompanyAccountingEntityCompany(object p) => true;

        #endregion

        #region "Location"

        private int _companyLocationId;

        public int CompanyLocationId
        {
            get { return _companyLocationId; }
            set 
            {
                if (_companyLocationId != value)
                {
                    _companyLocationId = value;
                    NotifyOfPropertyChange(nameof(CompanyLocationId));
                }
            }
        }

        private string _companyLocationName;

        public string CompanyLocationName
        {
            get { return _companyLocationName; }
            set 
            {
                if (_companyLocationName != value)
                {
                    _companyLocationName = value;
                    NotifyOfPropertyChange(nameof(CompanyLocationName));
                    ValidateProperty(nameof(CompanyLocationName), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int _companyIdBeforeNewCompanyLocation;

        public int CompanyIdBeforeNewCompanyLocation
        {
            get { return _companyIdBeforeNewCompanyLocation; }
            set 
            {
                if(_companyIdBeforeNewCompanyLocation != value)
                {
                    _companyIdBeforeNewCompanyLocation = value;
                    NotifyOfPropertyChange(nameof(CompanyIdBeforeNewCompanyLocation));
                }
            }
        }

        private int _companyLocationCompanyId;

        public int CompanyLocationCompanyId
        {
            get { return _companyLocationCompanyId; }
            set 
            {
                if (_companyLocationCompanyId != value)
                {
                    _companyLocationCompanyId = value;
                    NotifyOfPropertyChange(nameof(CompanyLocationCompanyId));
                }
            }
        }


        #endregion

        #region "CostCenter"

        private int _costCenterId;

        public int CostCenterId
        {
            get { return _costCenterId; }
            set 
            {
                if (_costCenterId != value)
                {
                    _costCenterId = value;
                    NotifyOfPropertyChange(nameof(CostCenterId));
                }
            }
        }

        private string _costCenterName;

        public string CostCenterName
        {
            get { return _costCenterName; }
            set 
            {
                if (_costCenterName != value)
                {
                    _costCenterName = value;
                    NotifyOfPropertyChange(nameof(CostCenterName));
                    ValidateProperty(nameof(CostCenterName), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _costCenterTradeName;

        public string CostCenterTradeName
        {
            get { return _costCenterTradeName; }
            set 
            {
                if (_costCenterTradeName != value)
                {
                    _costCenterTradeName = value;
                    NotifyOfPropertyChange(nameof(CostCenterTradeName));
                    ValidateProperty(nameof(CostCenterTradeName), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _costCenterShortName;

        public string CostCenterShortName
        {
            get { return _costCenterShortName; }
            set 
            {
                if (_costCenterShortName != value)
                {
                    _costCenterShortName = value;
                    NotifyOfPropertyChange(nameof(CostCenterShortName));
                    ValidateProperty(nameof(CostCenterShortName), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _costCenterState;

        public string CostCenterState
        {
            get { return _costCenterState; }
            set 
            {
                if (_costCenterState != value)
                {
                    _costCenterState = value;
                    NotifyOfPropertyChange(nameof(CostCenterState));
                }
            }
        }

        private string _costCenterAddress;

        public string CostCenterAddress
        {
            get { return _costCenterAddress; }
            set 
            {
                if (_costCenterAddress != value)
                {
                    _costCenterAddress = value;
                    NotifyOfPropertyChange(nameof(CostCenterAddress));
                    ValidateProperty(nameof(CostCenterAddress), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _costCenterPhone1;

        public string CostCenterPhone1
        {
            get { return _costCenterPhone1; }
            set 
            {
                if (_costCenterPhone1 != value)
                {
                    _costCenterPhone1 = value;
                    NotifyOfPropertyChange(nameof(CostCenterPhone1));
                    ValidateProperty(nameof(CostCenterPhone1), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _costCenterPhone2;

        public string CostCenterPhone2
        {
            get { return _costCenterPhone2; }
            set 
            {
                if (_costCenterPhone2 != value)
                {
                    _costCenterPhone2 = value;
                    NotifyOfPropertyChange(nameof(CostCenterPhone2));
                    ValidateProperty(nameof(CostCenterPhone2), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _costCenterCellPhone1;

        public string CostCenterCellPhone1
        {
            get { return _costCenterCellPhone1; }
            set 
            {
                if (_costCenterCellPhone1 != value)
                {
                    _costCenterCellPhone1 = value;
                    NotifyOfPropertyChange(nameof(CostCenterCellPhone1));
                    ValidateProperty(nameof(CostCenterCellPhone1), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _costCenterCellPhone2;
        public string CostCenterCellPhone2
        {
            get { return _costCenterCellPhone2; }
            set
            {
                if (_costCenterCellPhone2 != value)
                {
                    _costCenterCellPhone2 = value;
                    NotifyOfPropertyChange(nameof(CostCenterCellPhone2));
                    ValidateProperty(nameof(CostCenterCellPhone2), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private ObservableCollection<CountryGraphQLModel> _countries;

        public ObservableCollection<CountryGraphQLModel> Countries
        {
            get { return _countries; }
            set 
            {
                if (_countries != value)
                {
                    _countries = value;
                    NotifyOfPropertyChange(nameof(Countries));
                }
            }
        }

        private CountryGraphQLModel _costCenterSelectedCountry;

        public CountryGraphQLModel CostCenterSelectedCountry
        {
            get { return _costCenterSelectedCountry; }
            set 
            {
                if (_costCenterSelectedCountry != value)
                {
                    _costCenterSelectedCountry = value;
                    NotifyOfPropertyChange(nameof(CostCenterSelectedCountry));
                }
            }
        }

        private ObservableCollection<DepartmentGraphQLModel> _departments;

        public ObservableCollection<DepartmentGraphQLModel> Departments
        {
            get { return _departments; }
            set 
            {
                if (_departments != value)
                {
                    _departments = value;
                    NotifyOfPropertyChange(nameof(Departments));
                }
            }
        }

        private DepartmentGraphQLModel _costCenterSelectedDepartment;

        public DepartmentGraphQLModel CostCenterSelectedDepartment
        {
            get { return _costCenterSelectedDepartment; }
            set 
            {
                if (_costCenterSelectedDepartment != value)
                {
                    _costCenterSelectedDepartment = value;
                    NotifyOfPropertyChange(nameof(CostCenterSelectedDepartment));
                }
            }
        }

        private ObservableCollection<CityGraphQLModel> _cities;

        public ObservableCollection<CityGraphQLModel> Cities
        {
            get { return _cities; }
            set 
            {
                if (_cities != value)
                {
                    _cities = value;
                    NotifyOfPropertyChange(nameof(Cities));
                }
            }
        }

        private CityGraphQLModel _costCenterSelectedCity;

        public CityGraphQLModel CostCenterSelectedCity
        {
            get { return _costCenterSelectedCity; }
            set 
            {
                if (_costCenterSelectedCity != value)
                {
                    _costCenterSelectedCity = value;
                    NotifyOfPropertyChange(nameof(CostCenterSelectedCity));
                }
            }
        }

        public Dictionary<string, string> CostCenterDateControlTypeDictionary
        {
            get { return GlobalDictionaries.DateControlTypeDictionary; }
        }

        private string _selectedCostCenterDateControlType;

        public string SelectedCostCenterDateControlType
        {
            get { return _selectedCostCenterDateControlType; }
            set
            {
                if (_selectedCostCenterDateControlType != value)
                {
                    _selectedCostCenterDateControlType = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenterDateControlType));
                }
            }
        }

        private bool _costCenterShowChangeWindowOnCash;

        public bool CostCenterShowChangeWindowOnCash
        {
            get { return _costCenterShowChangeWindowOnCash; }
            set 
            {
                if (_costCenterShowChangeWindowOnCash != value)
                {
                    _costCenterShowChangeWindowOnCash = value;
                    NotifyOfPropertyChange(nameof(CostCenterShowChangeWindowOnCash));
                }
            }
        }

        private bool _costCenterAllowBuy;

        public bool CostCenterAllowBuy
        {
            get { return _costCenterAllowBuy; }
            set 
            {
                if (_costCenterAllowBuy != value) 
                {
                    _costCenterAllowBuy = value;
                    NotifyOfPropertyChange(nameof(CostCenterAllowBuy));
                }
            }
        }

        private bool _costCenterAllowSell;

        public bool CostCenterAllowSell
        {
            get { return _costCenterAllowSell; }
            set 
            {
                if (_costCenterAllowSell != value)
                {
                    _costCenterAllowSell = value;
                    NotifyOfPropertyChange(nameof(CostCenterAllowSell));
                }
            }
        }


        private bool _costCenterIsTaxable;

        public bool CostCenterIsTaxable
        {
            get { return _costCenterIsTaxable; }
            set 
            {
                if (_costCenterIsTaxable != value)
                {
                    _costCenterIsTaxable = value;
                    NotifyOfPropertyChange(nameof(CostCenterIsTaxable));
                    if(CostCenterIsTaxable is false)
                    {
                        CostCenterInvoicePriceIncludeTax = false;
                        CostCenterPriceListIncludeTax = false;
                    }
                }
            }
        }

        private bool _costCenterPriceListIncludeTax;

        public bool CostCenterPriceListIncludeTax
        {
            get { return _costCenterPriceListIncludeTax; }
            set 
            {
                if (_costCenterPriceListIncludeTax != value)
                {
                    _costCenterPriceListIncludeTax = value;
                    NotifyOfPropertyChange(nameof(CostCenterPriceListIncludeTax));
                }
            }
        }

        private bool _costCenterInvoicePriceIncludeTax;

        public bool CostCenterInvoicePriceIncludeTax
        {
            get { return _costCenterInvoicePriceIncludeTax; }
            set 
            {
                if (_costCenterInvoicePriceIncludeTax != value)
                {
                    _costCenterInvoicePriceIncludeTax = value;
                    NotifyOfPropertyChange(nameof(CostCenterInvoicePriceIncludeTax));
                } 
            }
        }

        private int _costCenterInvoiceCopiesToPrint;

        public int CostCenterInvoiceCopiesToPrint
        {
            get { return _costCenterInvoiceCopiesToPrint; }
            set 
            {
                if (_costCenterInvoiceCopiesToPrint != value)
                {
                    _costCenterInvoiceCopiesToPrint = value;
                    NotifyOfPropertyChange(nameof(CostCenterInvoiceCopiesToPrint));
                    if(CostCenterInvoiceCopiesToPrint == 0) CostCenterRequiresConfirmationToPrintCopies = false;
                    NotifyOfPropertyChange(nameof(CostCenterRequiresConfirmationToPrintCopiesIsEnable));
                }
            }
        }

        public bool CostCenterRequiresConfirmationToPrintCopiesIsEnable => CostCenterInvoiceCopiesToPrint > 0;

        private bool _costCenterRequiresConfirmationToPrintCopies;

        public bool CostCenterRequiresConfirmationToPrintCopies
        {
            get { return _costCenterRequiresConfirmationToPrintCopies; }
            set 
            {
                if (_costCenterRequiresConfirmationToPrintCopies != value)
                {
                    _costCenterRequiresConfirmationToPrintCopies = value;
                    NotifyOfPropertyChange(nameof(CostCenterRequiresConfirmationToPrintCopies));
                }
            }
        }

        private int _costCenterCompanyLocationId;

        public int CostCenterCompanyLocationId
        {
            get { return _costCenterCompanyLocationId; }
            set 
            {
                if (_costCenterCompanyLocationId != value)
                {
                    _costCenterCompanyLocationId = value;
                    NotifyOfPropertyChange(nameof(CostCenterCompanyLocationId));
                }
            }
        }

        private bool _costCenterAllowRepeatItemsOnSales;

        public bool CostCenterAllowRepeatItemsOnSales
        {
            get { return _costCenterAllowRepeatItemsOnSales; }
            set 
            {
                if (_costCenterAllowRepeatItemsOnSales != value)
                {
                    _costCenterAllowRepeatItemsOnSales = value;
                    NotifyOfPropertyChange(nameof(CostCenterAllowRepeatItemsOnSales));
                }
            }
        }

        private bool _costCenterTaxToCost;

        public bool CostCenterTaxToCost
        {
            get { return _costCenterTaxToCost; }
            set 
            {
                if (_costCenterTaxToCost != value)
                {
                    _costCenterTaxToCost = value;
                    NotifyOfPropertyChange(nameof(CostCenterTaxToCost));
                }
            }
        }


        public int CompanyLocationIdBeforeNewCostCenter { get; set; }

        #endregion

        #region "Storage"

        private int _storageId;

        public int StorageId
        {
            get { return _storageId; }
            set 
            {
                if (_storageId != value)
                {
                    _storageId = value;
                    NotifyOfPropertyChange(nameof(StorageId));
                }
            }
        }

        private string _storageName;

        public string StorageName
        {
            get { return _storageName; }
            set 
            {
                if (_storageName != value)
                {
                    _storageName = value;
                    NotifyOfPropertyChange(nameof(StorageName));
                    ValidateProperty(nameof(StorageName), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _storageAddress;

        public string StorageAddress
        {
            get { return _storageAddress; }
            set 
            {
                if (_storageAddress != value)
                {
                    _storageAddress = value;
                    NotifyOfPropertyChange(nameof(StorageAddress));
                    ValidateProperty(nameof(StorageAddress), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _storageState;

        public string StorageState
        {
            get { return _storageState; }
            set 
            {
                if (_storageState != value)
                {
                    _storageState = value;
                    NotifyOfPropertyChange(nameof(StorageState));
                }
            }
        }


        private CityGraphQLModel _storageSelectedCity;

        public CityGraphQLModel StorageSelectedCity
        {
            get { return _storageSelectedCity; }
            set 
            {
                if (_storageSelectedCity != value)
                {
                    _storageSelectedCity = value;
                    NotifyOfPropertyChange(nameof(StorageSelectedCity));
                }
            }
        }

        private DepartmentGraphQLModel _storageSelectedDepartment;

        public DepartmentGraphQLModel StorageSelectedDepartment
        {
            get { return _storageSelectedDepartment; }
            set 
            {
                if (_storageSelectedDepartment != value)
                {
                    _storageSelectedDepartment = value;
                    NotifyOfPropertyChange(nameof(StorageSelectedDepartment));
                }
            }
        }


        private CountryGraphQLModel _storageSelectedCountry;

        public CountryGraphQLModel StorageSelectedCountry
        {
            get { return _storageSelectedCountry; }
            set 
            {
                if (_storageSelectedCountry != value)
                {
                    _storageSelectedCountry = value;
                    NotifyOfPropertyChange(nameof(StorageSelectedCountry));
                } 
            }
        }

        private int _companyLocationIdBeforeNewStorage;

        public int CompanyLocationIdBeforeNewStorage
        {
            get { return _companyLocationIdBeforeNewStorage; }
            set 
            {
                if(_companyLocationIdBeforeNewStorage != value)
                {
                    _companyLocationIdBeforeNewStorage = value;
                    NotifyOfPropertyChange(nameof(CompanyLocationIdBeforeNewStorage));
                }
            }
        }

        private int _storageCompanyLocationId;

        public int StorageCompanyLocationId
        {
            get { return _storageCompanyLocationId; }
            set 
            {
                if (_storageCompanyLocationId != value)
                {
                    _storageCompanyLocationId = value;
                    NotifyOfPropertyChange(nameof(StorageCompanyLocationId));
                }
            }
        }


        #endregion



        #endregion


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

        private ICostCentersItems? _selectedItem;

        public ICostCentersItems? SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(TabControlVisibility));
                    _ = HandleSelectedItemChangedAsync();
                }
            }
        }

        public async Task HandleSelectedItemChangedAsync()
        {
            if (_selectedItem != null)
            {
                if (!IsNewRecord)
                {
                    IsEditing = false;
                    CanEdit = true;
                    CanUndo = false;
                    if (_selectedItem is CostCenterDTO costCenterDTO)
                    {
                        await SetCostCenterForEdit(costCenterDTO);
                        ClearAllErrors();
                        ValidateCostCenterProperties();
                        return;
                    }
                    if(_selectedItem is StorageDTO storageDTO)
                    {
                        await SetStorageForEdit(storageDTO);
                        ClearAllErrors();
                        ValidateStorageProperties();
                        return;
                    }
                    if(_selectedItem is CompanyLocationDTO companyLocationDTO)
                    {
                        await SetCompanyLocationForEdit(companyLocationDTO);
                        ClearAllErrors();
                        ValidateProperty(nameof(CompanyLocationName), CompanyLocationName);
                        return;
                    }
                    if(_selectedItem is CompanyDTO companyDTO)
                    {
                        await SetCompanyForEdit(companyDTO);
                        ClearAllErrors();
                        ValidateProperty(nameof(CompanyAccountingEntityCompanySearchName), CompanyAccountingEntityCompanySearchName);
                        return;
                    }
                }
                else
                {
                    IsEditing = true;
                    CanUndo = true;
                    CanEdit = false;

                    if(_selectedItem is CostCenterDTO costCenterDTO)
                    {
                        await SetCostCenterForNew();
                        ClearAllErrors();
                        ValidateCostCenterProperties();
                        return;
                    }
                    if (_selectedItem is StorageDTO storageDTO)
                    {
                        await SetStorageForNew();
                        ClearAllErrors();
                        ValidateStorageProperties();
                        return;
                    }
                    if(_selectedItem is CompanyLocationDTO companyLocationDTO)
                    {
                        await SetCompanyLocationForNew();
                        ClearAllErrors();
                        ValidateProperty(nameof(CompanyLocationName), CompanyLocationName);
                        return;
                    }
                }
            }
        }
        public async Task SetCompanyForEdit(CompanyDTO companyDTO)
        {
            CompanyId = companyDTO.Id;
            CompanyAccountingEntityCompanyId = companyDTO.AccountingEntityCompany.Id;
            CompanyAccountingEntityCompanySearchName = companyDTO.AccountingEntityCompany.SearchName;
        }
        public async Task SetCompanyLocationForNew()
        {
            CompanyLocationId = 0;
            CompanyLocationName = string.Empty;
        }

        public async Task SetCompanyLocationForEdit(CompanyLocationDTO companyLocationDTO)
        {
            CompanyLocationId = companyLocationDTO.Id;
            CompanyLocationName = companyLocationDTO.Name;
            CompanyLocationCompanyId = companyLocationDTO.Company.Id;
        }

        public async Task SetStorageForNew()
        {
            StorageId = 0;
            StorageName = string.Empty;
            StorageAddress = string.Empty;
            StorageState = "A";
            StorageSelectedCountry = Countries.FirstOrDefault(country => country.Code == "169") ?? throw new Exception(""); //Codigo de Colombia
            StorageSelectedDepartment = StorageSelectedCountry.Departments.FirstOrDefault(department => department.Country.Id == StorageSelectedCountry.Id) ?? throw new Exception("");
            StorageSelectedCity = StorageSelectedDepartment.Cities.FirstOrDefault(city => city.Department.Id == StorageSelectedDepartment.Id) ?? throw new Exception("");
        }

        public async Task SetStorageForEdit(StorageDTO storageDTO)
        {
            StorageId = storageDTO.Id;
            StorageName = storageDTO.Name;
            StorageAddress = storageDTO.Address;
            StorageState = storageDTO.State;
            StorageSelectedCountry = Countries.FirstOrDefault(country => country.Id == storageDTO.City.Department.Country.Id) ?? throw new Exception("");
            StorageSelectedDepartment = StorageSelectedCountry.Departments.FirstOrDefault(department => department.Id == storageDTO.City.Department.Id) ?? throw new Exception("");
            StorageSelectedCity = StorageSelectedDepartment.Cities.FirstOrDefault(city => city.Id == storageDTO.City.Id) ?? throw new Exception("");
            StorageCompanyLocationId = storageDTO.Location.Id;
        }

        public async Task SetCostCenterForNew()
        {
            CostCenterId = 0;
            CostCenterName = string.Empty;
            CostCenterTradeName = string.Empty;
            CostCenterShortName = string.Empty;
            CostCenterState = "A";
            CostCenterAddress = string.Empty;
            CostCenterPhone1 = string.Empty;
            CostCenterPhone2 = string.Empty;
            CostCenterCellPhone1 = string.Empty;
            CostCenterCellPhone2 = string.Empty;
            CostCenterSelectedCountry = Countries.FirstOrDefault(country => country.Code == "169") ?? throw new Exception(""); //Codigo de Colombia
            CostCenterSelectedDepartment = CostCenterSelectedCountry.Departments.FirstOrDefault(department => department.Country.Id == CostCenterSelectedCountry.Id) ?? throw new Exception("");
            CostCenterSelectedCity = CostCenterSelectedDepartment.Cities.FirstOrDefault(city => city.Department.Id == CostCenterSelectedDepartment.Id) ?? throw new Exception("");
            SelectedCostCenterDateControlType = "FA";
            CostCenterShowChangeWindowOnCash = false;
            CostCenterAllowBuy = false;
            CostCenterAllowSell = false;
            CostCenterIsTaxable = false;
            CostCenterPriceListIncludeTax = false;
            CostCenterInvoicePriceIncludeTax = false;
            CostCenterRequiresConfirmationToPrintCopies = false;
            CostCenterInvoiceCopiesToPrint = 0;
            CostCenterAllowRepeatItemsOnSales = false;
            CostCenterTaxToCost = false;
        }
        public async Task SetCostCenterForEdit(CostCenterDTO costCenterDTO)
        {
            CostCenterId = costCenterDTO.Id;
            CostCenterName = costCenterDTO.Name;
            CostCenterTradeName = costCenterDTO.TradeName;
            CostCenterShortName = costCenterDTO.ShortName;
            CostCenterState = costCenterDTO.State;
            CostCenterAddress = costCenterDTO.Address;
            CostCenterPhone1 = costCenterDTO.Phone1;
            CostCenterPhone2 = costCenterDTO.Phone2;
            CostCenterCellPhone1 = costCenterDTO.CellPhone1;
            CostCenterCellPhone2 = costCenterDTO.CellPhone2;
            CostCenterSelectedCountry = Countries.FirstOrDefault(country => country.Id == costCenterDTO.Country.Id) ?? throw new Exception("");
            CostCenterSelectedDepartment = CostCenterSelectedCountry.Departments.FirstOrDefault(department => department.Id == costCenterDTO.Department.Id) ?? throw new Exception("");
            CostCenterSelectedCity = CostCenterSelectedDepartment.Cities.FirstOrDefault(city => city.Id == costCenterDTO.City.Id) ?? throw new Exception("");
            SelectedCostCenterDateControlType = costCenterDTO.DateControlType;
            CostCenterShowChangeWindowOnCash = costCenterDTO.ShowChangeWindowOnCash;
            CostCenterAllowBuy = costCenterDTO.AllowBuy;
            CostCenterAllowSell = costCenterDTO.AllowSell;
            CostCenterIsTaxable = costCenterDTO.IsTaxable;
            CostCenterPriceListIncludeTax = costCenterDTO.PriceListIncludeTax;
            CostCenterInvoicePriceIncludeTax = costCenterDTO.InvoicePriceIncludeTax;
            CostCenterRequiresConfirmationToPrintCopies = costCenterDTO.RequiresConfirmationToPrintCopies;
            CostCenterInvoiceCopiesToPrint = costCenterDTO.InvoiceCopiesToPrint;
            CostCenterCompanyLocationId = costCenterDTO.Location.Id;
            CostCenterAllowRepeatItemsOnSales = costCenterDTO.AllowRepeatItemsOnSales;
            CostCenterTaxToCost = costCenterDTO.TaxToCost;
        }

        private int _selectedIndex = 0;

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set 
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    NotifyOfPropertyChange(nameof(SelectedIndex));
                }
            }
        }

        public bool TabControlVisibility
        {
            get 
            {
                if(_selectedItem != null && _selectedItem is not CostCenterDummyDTO && _selectedItem is not StorageDummyDTO)
                {
                    if (_selectedItem is CompanyDTO companyDTO) CompanyIdBeforeNewCompanyLocation = companyDTO.Id;
                    return true;
                }
                if (_selectedItem is CostCenterDummyDTO costCenterDummyDTO) CompanyLocationIdBeforeNewCostCenter = costCenterDummyDTO.Location.Id;
                if (_selectedItem is StorageDummyDTO storageDummyDTO) CompanyLocationIdBeforeNewStorage = storageDummyDTO.Location.Id;
                SelectedItem = null;
                return false; 
            }
        }

        private ObservableCollection<CompanyDTO> _companies;

        public ObservableCollection<CompanyDTO> Companies
        {
            get { return _companies; }
            set
            {
                if (_companies != value)
                {
                    _companies = value;
                    NotifyOfPropertyChange(nameof(Companies));
                }
            }
        }

        private ObservableCollection<CompanyLocationDTO> _locations;

        public ObservableCollection<CompanyLocationDTO> Locations
        {
            get { return _locations; }
            set
            {
                if (_locations != value)
                {
                    _locations = value;
                    NotifyOfPropertyChange(nameof(Locations));
                }
            }
        }

        private ObservableCollection<CostCenterDTO> _costCenters;

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

        private ObservableCollection<StorageDTO> _storages;

        public ObservableCollection<StorageDTO> Storages
        {
            get { return _storages; }
            set 
            {
                if (_storages != value)
                {
                    _storages = value;
                    NotifyOfPropertyChange(nameof(Storages));
                }
            }
        }

        public bool TreeViewIsEnable => !IsEditing;

        private bool _isEditing = false;

        public bool IsEditing
        {
            get { return _isEditing; }
            set 
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    NotifyOfPropertyChange(nameof(IsEditing));
                    NotifyOfPropertyChange(nameof(TreeViewIsEnable));
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


        private ICommand _deleteCompanyLocationCommand;
        public ICommand DeleteCompanyLocationCommand
        {
            get
            {
                if (_deleteCompanyLocationCommand is null) _deleteCompanyLocationCommand = new AsyncCommand(DeleteCompanyLocation, CanDeleteCompanyLocation);
                return _deleteCompanyLocationCommand;
            }
        }

        public async Task DeleteCompanyLocation()
        {
            try
            {
                IsBusy = true;
                int id = ((CompanyLocationDTO)SelectedItem).Id;

                string query = @"query($id:Int!){
                  CanDeleteModel: canDeleteCompanyLocation(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await _companyLocationService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {((CompanyLocationDTO)SelectedItem).Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }

                IsBusy = true;
                Refresh();

                CompanyLocationGraphQLModel deletedCompanyLocation = await ExecuteDeleteCompanyLocation(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new CompanyLocationDeleteMessage() { DeletedCompanyLocation = deletedCompanyLocation });

                NotifyOfPropertyChange(nameof(CanDeleteCompanyLocation));

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteCompanyLocation" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }

        }

        public async Task<CompanyLocationGraphQLModel> ExecuteDeleteCompanyLocation(int id)
        {
            try
            {
                string query = @"
                    mutation($id: Int!){
                      DeleteResponse: deleteCompanyLocation(id: $id){
                        id
                        name
                        company{
                          id
                        }
                      }
                    }";
                object variables = new { Id = id };
                CompanyLocationGraphQLModel deletedCompanyLocation = await _companyLocationService.DeleteAsync(query, variables);
                this.SelectedItem = null;
                return deletedCompanyLocation;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool CanDeleteCompanyLocation => true;

        private ICommand _deleteStorageCommand;
        public ICommand DeleteStorageCommand
        {
            get
            {
                if (_deleteStorageCommand is null) _deleteStorageCommand = new AsyncCommand(DeleteStorage, CanDeleteStorage);
                return _deleteStorageCommand;
            }
        }

        public async Task DeleteStorage()
        {
            try
            {
                IsBusy = true;
                int id = ((StorageDTO)SelectedItem).Id;

                string query = @"query($id:Int!){
                  CanDeleteModel: canDeleteStorage(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await _storageService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {((StorageDTO)SelectedItem).Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }

                IsBusy = true;
                Refresh();

                StorageGraphQLModel deletedStorage = await ExecuteDeleteStorage(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new StorageDeleteMessage() { DeletedStorage = deletedStorage });

                NotifyOfPropertyChange(nameof(CanDeleteStorage));
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteStorage" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<StorageGraphQLModel> ExecuteDeleteStorage(int id)
        {
            try
            {
                string query = @"
                    mutation ($id: Int!) {
                      DeleteResponse: deleteStorage(id: $id) {
                        id
                        name
                        address
                        state
                        city {
                          id
                          code
                          name
                          department {
                            id
                            country {
                              id
                            }
                          }
                        }
                        location {
                          id
                          company {
                            id
                          }
                        }
                      }
                    }";
                object variables = new { Id = id };
                StorageGraphQLModel deletedStorage = await _storageService.DeleteAsync(query, variables);
                this.SelectedItem = null;
                return deletedStorage;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool CanDeleteStorage => true;

        private ICommand _deleteCostCenterCommand;
        public ICommand DeleteCostCenterCommand
        {
            get
            {
                if (_deleteCostCenterCommand is null) _deleteCostCenterCommand = new AsyncCommand(DeleteCostCenter, CanDeleteCostCenter);
                return _deleteCostCenterCommand;
            }
        }

        public async Task DeleteCostCenter()
        {
            try
            {
                IsBusy = true;
                int id = ((CostCenterDTO)SelectedItem).Id;

                string query = @"query($id:Int!){
                  CanDeleteModel: canDeleteCostCenter(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await _costCenterService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {((CostCenterDTO)SelectedItem).Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }

                IsBusy = true;
                Refresh();

                CostCenterGraphQLModel deletedCostCenter = await ExecuteDeleteCostCenter(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new CostCenterDeleteMessage() { DeletedCostCenter = deletedCostCenter });

                NotifyOfPropertyChange(nameof(CanDeleteCostCenter));
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteCostCenter" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<CostCenterGraphQLModel> ExecuteDeleteCostCenter(int id)
        {
            try
            {
                string query = @"
                mutation ($id: Int!) {
                  DeleteResponse: deleteCostCenter(id: $id) {
                    id
                    name
                    tradeName
                    shortName
                    state
                    address
                    phone1
                    phone2
                    cellPhone1
                    cellPhone2
                    dateControlType
                    showChangeWindowOnCash
                    allowBuy
                    allowSell
                    isTaxable
                    priceListIncludeTax
                    invoicePriceIncludeTax
                    allowRepeatItemsOnSales
                    invoiceCopiesToPrint
                    requiresConfirmationToPrintCopies
                    taxToCost
                    defaultInvoiceObservation
                    invoiceFooter
                    remissionFooter
                    relatedAccountingEntity{
                        id
                    }
                    country {
                        id
                        code
                        name
                    }
                    department {
                        id
                        code
                        name
                    }
                    city {
                        id
                        code
                        name
                    }
                    location{
                        id
                        company{
                        id
                        }
                    }
                    }
                }";
                object variables = new { Id = id };
                CostCenterGraphQLModel deletedCostCenter = await _costCenterService.DeleteAsync(query, variables);
                this.SelectedItem = null;
                return deletedCostCenter;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool CanDeleteCostCenter()
        {
            return true;
        }

        private ICommand _createCompanyLocationCommand;
        public ICommand CreateCompanyLocationCommand
        {
            get
            {
                if (_createCompanyLocationCommand is null) _createCompanyLocationCommand = new AsyncCommand(CreateCompanyLocation, CanCreateCompanyLocation);
                return _createCompanyLocationCommand;
            }
        }

        public async Task CreateCompanyLocation()
        {
            IsNewRecord = true;
            SelectedItem = new CompanyLocationDTO();
            ValidateProperty(nameof(CompanyLocationName), CompanyLocationName);
            NotifyOfPropertyChange(nameof(CanSave));
            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                this.SetFocus(nameof(CompanyLocationName));
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public bool CanCreateCompanyLocation => true;

        private ICommand _createStorageCommand;
        public ICommand CreateStorageCommand
        {
            get
            {
                if (_createStorageCommand is null) _createStorageCommand = new AsyncCommand(CreateStorage, CanCreateStorage);
                return _createStorageCommand;
            }
        }
        public async Task CreateStorage()
        {
            IsNewRecord = true;
            SelectedItem = new StorageDTO();

            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                this.SetFocus(nameof(StorageName));
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public bool CanCreateStorage => true;

        private ICommand _createCostCenterCommand;
        public ICommand CreateCostCenterCommand
        {
            get
            {
                if (_createCostCenterCommand is null) _createCostCenterCommand = new AsyncCommand(CreateCostCenter, CanCreateCostCenter);
                return _createCostCenterCommand;
            }
        }

        public async Task CreateCostCenter()
        {
            IsNewRecord = true;
            SelectedItem = new CostCenterDTO();

            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                this.SetFocus(nameof(CostCenterName));
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public bool CanCreateCostCenter()
        {
            return true;
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

        public async Task Save()
        {
            try
            {
                IsBusy = true;
                Refresh();
                if(SelectedItem is CostCenterDTO costCenterDTO)
                {
                    CostCenterGraphQLModel result = await ExecuteSaveCostCenter();
                    if (IsNewRecord)
                    {
                        await Context.EventAggregator.PublishOnUIThreadAsync(new CostCenterCreateMessage() { CreatedCostCenter = result });
                    }
                    else
                    {
                        await Context.EventAggregator.PublishOnUIThreadAsync(new CostCenterUpdateMessage() { UpdatedCostCenter = result });

                    }
                }
                if (SelectedItem is StorageDTO storageDTO)
                {
                    StorageGraphQLModel result = await ExecuteSaveStorage();
                    if (IsNewRecord)
                    {
                        await Context.EventAggregator.PublishOnUIThreadAsync(new StorageCreateMessage() { CreatedStorage = result });
                    }
                    else
                    {
                        await Context.EventAggregator.PublishOnUIThreadAsync(new StorageUpdateMessage() { UpdatedStorage = result });
                    }
                }
                if (SelectedItem is CompanyLocationDTO companyLocationDTO)
                {
                    CompanyLocationGraphQLModel result = await ExecuteSaveCompanyLocation();
                    if (IsNewRecord)
                    {
                        await Context.EventAggregator.PublishOnCurrentThreadAsync(new CompanyLocationCreateMessage() { CreatedCompanyLocation = result });
                    }
                    else
                    {
                        await Context.EventAggregator.PublishOnCurrentThreadAsync(new CompanyLocationUpdateMessage() { UpdatedCompanyLocation = result });
                    }
                }
                if(SelectedItem is CompanyDTO companyDTO)
                {
                    CompanyGraphQLModel result = await ExecuteSaveCompany();
                    if (!IsNewRecord)
                    {
                        await Context.EventAggregator.PublishOnCurrentThreadAsync(new CompanyUpdateMessage() { UpdatedCompany = result });
                    }
                }
                IsEditing = false;
                CanUndo = false;
                CanEdit = true;
                SelectedIndex = 0;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
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

        public bool CanSave => IsEditing == true && _errors.Count <= 0;

        public async Task<CompanyGraphQLModel> ExecuteSaveCompany()
        {
            try
            {
                string query;
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                variables.Id = CompanyId;
                variables.Data.accountingEntityCompanyId = CompanyAccountingEntityCompanyId;
                query = @"
                    mutation ($data: UpdateCompanyInput!, $id: Int!) {
                      UpdateResponse: updateCompany(data: $data, id: $id) {
                        id
                        accountingEntityCompany {
                          id
                          searchName
                        }
                      }
                    }";
                var result = await _companyService.UpdateAsync(query, variables);
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<CompanyLocationGraphQLModel> ExecuteSaveCompanyLocation()
        {
            try
            {
                string query;
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                if (!IsNewRecord) variables.Id = CompanyLocationId;
                variables.Data.name = CompanyLocationName.Trim().RemoveExtraSpaces();
                variables.Data.companyId = IsNewRecord ? CompanyIdBeforeNewCompanyLocation : CompanyLocationCompanyId;
                if (IsNewRecord)
                {
                    query = @"
                        mutation ($data: CreateCompanyLocationInput!) {
                          CreateResponse: createCompanyLocation(data: $data) {
                            id
                            name
                            company {
                              id
                            }
                          }
                        }
                        ";
                }
                else
                {
                    query = @"
                        mutation ($data: UpdateCompanyLocationInput!, $id: Int!) {
                          UpdateResponse: updateCompanyLocation(data: $data, id: $id) {
                            id
                            name
                            company {
                              id
                            }
                          }
                        }";
                }
                var result = IsNewRecord ? await _companyLocationService.CreateAsync(query, variables) : await _companyLocationService.UpdateAsync(query, variables);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<StorageGraphQLModel> ExecuteSaveStorage()
        {
            try
            {
                string query;
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                if (!IsNewRecord) variables.Id = StorageId;
                variables.Data.name = StorageName.Trim().RemoveExtraSpaces();
                variables.Data.address = StorageAddress.Trim().RemoveExtraSpaces();
                variables.Data.state = StorageState;
                variables.Data.cityId = StorageSelectedCity.Id;
                variables.Data.companyLocationId = IsNewRecord ? CompanyLocationIdBeforeNewStorage : StorageCompanyLocationId;
                if (IsNewRecord)
                {
                    query = @"
                        mutation ($data: CreateStorageInput!) {
                          CreateResponse: createStorage(data: $data) {
                            id
                            name
                            address
                            state
                            city {
                              id
                              code
                              name
                              department{
                                id
                                country{
                                  id
                                }
                              }
                            }
                            location{
                              id
                              company{
                                id
                              }
                            }
                          }
                        }
                        ";
                }
                else
                {
                    query = @"
                        mutation ($data: UpdateStorageInput!, $id: Int!) {
                          UpdateResponse: updateStorage(data: $data, id: $id) {
                            id
                            name
                            address
                            state
                            city {
                              id
                              code
                              name
                              department{
                                id
                                country{
                                  id
                                }
                              }
                            }
                            location{
                              id
                              company{
                                id
                              }
                            }
                          }
                        }";
                }
                var result = IsNewRecord ? await _storageService.CreateAsync(query, variables) : await _storageService.UpdateAsync(query, variables);
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<CostCenterGraphQLModel> ExecuteSaveCostCenter()
        {
            try
            {
                string query;
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                if (!IsNewRecord) variables.Id = CostCenterId;
                variables.Data.name = CostCenterName.Trim().RemoveExtraSpaces();
                variables.Data.tradeName = CostCenterTradeName.Trim().RemoveExtraSpaces();
                variables.Data.shortName = CostCenterShortName.Trim().RemoveExtraSpaces();
                variables.Data.state = CostCenterState;
                variables.Data.address = CostCenterAddress.Trim().RemoveExtraSpaces();
                variables.Data.phone1 = CostCenterPhone1;
                variables.Data.phone2 = CostCenterPhone2;
                variables.Data.cellPhone1 = CostCenterCellPhone1;
                variables.Data.cellPhone2 = CostCenterCellPhone2;
                variables.Data.dateControlType = SelectedCostCenterDateControlType;
                variables.Data.showChangeWindowOnCash = CostCenterShowChangeWindowOnCash;
                variables.Data.allowBuy = CostCenterAllowBuy;
                variables.Data.allowSell = CostCenterAllowSell;
                variables.Data.isTaxable = CostCenterIsTaxable;
                variables.Data.priceListIncludeTax = CostCenterPriceListIncludeTax;
                variables.Data.invoicePriceIncludeTax = CostCenterInvoicePriceIncludeTax;
                variables.Data.countryId = CostCenterSelectedCountry.Id;
                variables.Data.departmentId = CostCenterSelectedDepartment.Id;
                variables.Data.cityId = CostCenterSelectedCity.Id;
                variables.Data.companyLocationId = IsNewRecord ? CompanyLocationIdBeforeNewCostCenter : CostCenterCompanyLocationId;
                variables.Data.allowRepeatItemsOnSales = CostCenterAllowRepeatItemsOnSales;
                variables.Data.invoiceCopiesToPrint = CostCenterInvoiceCopiesToPrint;
                variables.Data.requiresConfirmationToPrintCopies = CostCenterRequiresConfirmationToPrintCopies;
                variables.Data.taxToCost = CostCenterTaxToCost;
                variables.Data.defaultInvoiceObservation = string.Empty;
                variables.Data.invoiceFooter = string.Empty;
                variables.Data.remissionFooter = string.Empty;
                variables.Data.relatedAccountingEntityId = 0;
                if (IsNewRecord)
                {
                    query = @"
                        mutation ($data: CreateCostCenterInput!) {
                          CreateResponse: createCostCenter(data: $data) {
                            id
                            name
                            tradeName
                            shortName
                            state
                            address
                            phone1
                            phone2
                            cellPhone1
                            cellPhone2
                            dateControlType
                            showChangeWindowOnCash
                            allowBuy
                            allowSell
                            isTaxable
                            priceListIncludeTax
                            invoicePriceIncludeTax
                            allowRepeatItemsOnSales
                            invoiceCopiesToPrint
                            requiresConfirmationToPrintCopies
                            taxToCost
                            defaultInvoiceObservation
                            invoiceFooter
                            remissionFooter
                            relatedAccountingEntity{
                                id
                            }
                            country {
                              id
                              code
                              name
                            }
                            department {
                              id
                              code
                              name
                            }
                            city {
                              id
                              code
                              name
                            }
                            location{
                              id
                              company{
                                id
                              }
                            }
                          }
                        }
                        ";
                }
                else
                {
                    query = @"
                        mutation ($data: UpdateCostCenterInput!, $id: Int!) {
                          UpdateResponse: updateCostCenter(data: $data, id: $id) {
                            id
                            name
                            tradeName
                            shortName
                            state
                            address
                            phone1
                            phone2
                            cellPhone1
                            cellPhone2
                            dateControlType
                            showChangeWindowOnCash
                            allowBuy
                            allowSell
                            isTaxable
                            priceListIncludeTax
                            invoicePriceIncludeTax
                            allowRepeatItemsOnSales
                            invoiceCopiesToPrint
                            requiresConfirmationToPrintCopies
                            taxToCost
                            defaultInvoiceObservation
                            invoiceFooter
                            remissionFooter
                            relatedAccountingEntity{
                                id
                            }
                            country {
                              id
                              code
                              name
                            }
                            department {
                              id
                              code
                              name
                            }
                            city {
                              id
                              code
                              name
                            }
                            location{
                              id
                              company{
                                id
                              }
                            }
                          }
                        }";
                }
                var result = IsNewRecord ? await _costCenterService.CreateAsync(query, variables) : await _costCenterService.UpdateAsync(query, variables);
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }

        private ICommand _editCommand;
        public ICommand EditCommand
        {
            get
            {
                if (_editCommand is null) _editCommand = new AsyncCommand(Edit, CanEdit);
                return _editCommand;
            }
        }

        public async Task Edit()
        {
            IsEditing = true;
            CanUndo = true;
            CanEdit = false;

            if (SelectedItem is CostCenterDTO) this.SetFocus(nameof(CostCenterName));
            if (SelectedItem is StorageDTO) this.SetFocus(nameof(StorageName));
            if (SelectedItem is CompanyLocationDTO) this.SetFocus(nameof(CompanyLocationName));
        }

        private bool _canEdit = true;

        public bool CanEdit
        {
            get { return _canEdit; }
            set 
            {
                if (_canEdit != value)
                {
                    _canEdit = value;
                    NotifyOfPropertyChange(nameof(CanEdit));
                }
            }
        }

        private ICommand _undoCommand;

        public ICommand UndoCommand
        {
            get
            {
                if (_undoCommand is null) _undoCommand = new AsyncCommand(Undo, CanUndo);
                return _undoCommand;
            }
        }

        public async Task Undo()
        {
            if (IsNewRecord)
            {
                SelectedItem = null;
            }
            IsEditing = false;
            CanUndo = false;
            CanEdit = true;
            IsNewRecord = false;
            SelectedIndex = 0;
            if(SelectedItem is CostCenterDTO costCenterDTO) await SetCostCenterForEdit(costCenterDTO);
            if (SelectedItem is StorageDTO storageDTO) await SetStorageForEdit(storageDTO);
            if (SelectedItem is CompanyLocationDTO companyLocationDTO) await SetCompanyLocationForEdit(companyLocationDTO);
            if (SelectedItem is CompanyDTO companyDTO) await SetCompanyForEdit(companyDTO);
        }

        private bool _canUndo = false;
        public bool CanUndo
        {
            get { return _canUndo; }
            set
            {
                if (_canUndo != value)
                {
                    _canUndo = value;
                    NotifyOfPropertyChange(nameof(CanUndo));
                }
            }
        }


        public async Task LoadStorages(CompanyLocationDTO location, StorageDummyDTO storageDummyDTO)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    storageDummyDTO.Storages.Remove(storageDummyDTO.Storages[0]);
                });

                List<int> ids = [location.Id];
                string query = @"
                    query ($ids: [Int!]!) {
                      ListResponse: storagesByCompaniesLocationsIds(ids: $ids) {
                        id
                        name
                        address
                        state
                        city {
                          id
                          name
                          department{
                            id
                            code
                            name
                            country{
                              id
                              code
                              name
                            }
                          }
                        }
                        location{
                          id
                        }
                      }
                    }
                    ";
                dynamic variables = new ExpandoObject();
                variables.ids = ids;

                var source = await _storageService.GetListAsync(query, variables);
                Storages = Context.AutoMapper.Map<ObservableCollection<StorageDTO>>(source);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (StorageDTO storage in Storages)
                    {
                        storageDummyDTO.Storages.Add(storage);
                    }
                });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadStorages" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task LoadCostCenters(CompanyLocationDTO location, CostCenterDummyDTO costCenterDummyDTO)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    costCenterDummyDTO.CostCenters.Remove(costCenterDummyDTO.CostCenters[0]);
                });

                List<int> ids = [location.Id];
                string query = @"
                    query($ids: [Int!]!){
                      ListResponse: costCentersByCompaniesLocationsIds(ids: $ids){
                        id
                        name
                        tradeName
                        state
                        shortName
                        address
                        phone1
                        phone2
                        cellPhone1
                        cellPhone2
                        dateControlType
                        showChangeWindowOnCash
                        allowBuy
                        allowSell
                        isTaxable
                        priceListIncludeTax
                        invoicePriceIncludeTax
                        invoiceCopiesToPrint
                        requiresConfirmationToPrintCopies
                        allowRepeatItemsOnSales
                        taxToCost
                        relatedAccountingEntity{
                            id
                        }
                        location{
                            id
                        }
                        country {
                          id
                          code
                          name
                        }
                        department {
                          id
                          code
                          name
                        }
                        city {
                          id
                          code
                          name
                        }
                      }
                    }";
                dynamic variables = new ExpandoObject();
                variables.ids = ids;

                var source = await _costCenterService.GetListAsync(query, variables);
                CostCenters = Context.AutoMapper.Map<ObservableCollection<CostCenterDTO>>(source);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (CostCenterDTO costCenter in CostCenters)
                    {
                        costCenterDummyDTO.CostCenters.Add(costCenter);
                    }
                });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCostCenters" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }
        public async Task LoadCompaniesLocations(CompanyDTO company)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    company.Locations.Remove(company.Locations[0]);
                });

                List<int> ids = [company.Id];
                string query = @"
                    query($ids: [Int!]!){
                      ListResponse: companiesLocationsByCompaniesIds(ids: $ids){
                        id
                        name
                        company{
                            id
                        }
                      }
                    }";
                dynamic variables = new ExpandoObject();
                variables.ids = ids;

                var source = await _companyLocationService.GetListAsync(query, variables);
                Locations = Context.AutoMapper.Map<ObservableCollection<CompanyLocationDTO>>(source);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (CompanyLocationDTO location in Locations)
                    {
                        location.Context = this;
                        location.DummyItems.Add(new CostCenterDummyDTO(this, location));
                        location.DummyItems.Add(new StorageDummyDTO(this, location));
                        company.Locations.Add(location);
                    }
                });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCompaniesLocations" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task Initialize()
        {
            await LoadCompany();
            await LoadComboBoxes();
        }

        public async Task LoadComboBoxes()
        {
            try
            {
                string query = @"
                    query {
                      ListResponse: countries {
                        id
                        code
                        name
                        departments {
                          id
                          code
                          name
                          country {
                            id
                          }
                          cities {
                            id
                            code
                            name
                            department {
                              id
                              country {
                                id
                              }
                            }
                          }
                        }
                      }
                    }";

                var source = await _countryService.GetListAsync(query, new {  });
                Countries = new ObservableCollection<CountryGraphQLModel>(source);
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadComboBoxes" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task LoadCompany()
        {
            try
            {
                Refresh();
                string query = @"
                query($ids: [Int!]!){
                  ListResponse: companiesByIds(ids: $ids){
                    id
                    accountingEntityCompany{
                      searchName
                    }
                  }
                }";

                //TODO: corregir a usar el id de la compañia y no un id quemado
                dynamic variables = new ExpandoObject();
                variables.Ids = new List<int>() { 1 };

                IEnumerable<CompanyGraphQLModel> source = await _companyService.GetListAsync(query, variables);
                Companies = Context.AutoMapper.Map<ObservableCollection<CompanyDTO>>(source);
                if (Companies.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (CompanyDTO company in Companies)
                        {
                            company.Context = this;
                            company.Locations.Add(new CompanyLocationDTO() { IsDummyChild = true, Name = "Fucking Dummy" });
                        }
                    });
                }
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCompany" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public CostCenterMasterViewModel(
            CostCenterViewModel context,
            IRepository<CompanyGraphQLModel> companyService,
            IRepository<CompanyLocationGraphQLModel> companyLocationService,
            IRepository<CostCenterGraphQLModel> costCenterService,
            IRepository<StorageGraphQLModel> storageService,
            IRepository<CountryGraphQLModel> countryService,
            Helpers.IDialogService dialogService,
            Helpers.Services.INotificationService notificationService) 
        {
            Context = context;
            _companyService = companyService;
            _companyLocationService = companyLocationService;
            _costCenterService = costCenterService;
            _storageService = storageService;
            _countryService = countryService;
            _dialogService = dialogService;
            _notificationService = notificationService;
            
            Messenger.Default.Register<ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel>>(this, SearchWithTwoColumnsGridMessageToken.CompanyAccountingEntity, false, OnFindCompanyAccountingEntityMessage);
            _errors = new Dictionary<string, List<string>>();
            Context.EventAggregator.SubscribeOnUIThread(this);
        }

        public void OnFindCompanyAccountingEntityMessage(ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel> message)
        {
            if (message.ReturnedData is null) return;
            CompanyAccountingEntityCompanyId = message.ReturnedData.Id;
            CompanyAccountingEntityCompanySearchName = message.ReturnedData.SearchName;
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await base.OnActivateAsync(cancellationToken);
            await Initialize();
        }

        public async Task HandleAsync(CostCenterCreateMessage message, CancellationToken cancellationToken)
        {
            IsNewRecord = false;
            CostCenterDTO costCenterDTO = Context.AutoMapper.Map<CostCenterDTO>(message.CreatedCostCenter);
            CompanyDTO companyDTO = Companies.FirstOrDefault(company => company.Id == costCenterDTO.Location.Company.Id) ?? throw new Exception("");
            if (companyDTO == null) return;
            CompanyLocationDTO companyLocationDTO = companyDTO.Locations.FirstOrDefault(location => location.Id == costCenterDTO.Location.Id) ?? throw new Exception("");
            if (companyLocationDTO == null) return;
            CostCenterDummyDTO costCenterDummyDTO = companyLocationDTO.DummyItems.FirstOrDefault(dummy => dummy is CostCenterDummyDTO) as CostCenterDummyDTO ?? throw new Exception("");
            if (costCenterDummyDTO == null) return;
            if(!costCenterDummyDTO.IsExpanded && costCenterDummyDTO.CostCenters[0].IsDummyChild)
            {
                await LoadCostCenters(companyLocationDTO, costCenterDummyDTO);
                costCenterDummyDTO.IsExpanded = true;
                CostCenterDTO? costCenter = costCenterDummyDTO.CostCenters.FirstOrDefault(x => x.Id == costCenterDTO.Id);
                if (costCenter is null) return;
                _notificationService.ShowSuccess("Centro de costo creado correctamente.");
                SelectedItem = costCenter;
                return;
            }
            if (!costCenterDummyDTO.IsExpanded)
            {
                costCenterDummyDTO.IsExpanded = true;
                costCenterDummyDTO.CostCenters.Add(costCenterDTO);
                SelectedItem = costCenterDTO;
                _notificationService.ShowSuccess("Centro de costo creado correctamente.");
                return;
            }
            costCenterDummyDTO.CostCenters.Add(costCenterDTO);
            SelectedItem = costCenterDTO;
            _notificationService.ShowSuccess("Centro de costo creado correctamente.");
            return;
        }

        public Task HandleAsync(CostCenterUpdateMessage message, CancellationToken cancellationToken)
        {
            CostCenterDTO costCenterDTO = Context.AutoMapper.Map<CostCenterDTO>(message.UpdatedCostCenter);
            CompanyDTO? companyDTO = Companies.FirstOrDefault(company => company.Id == costCenterDTO.Location.Company.Id);
            if (companyDTO is null) return Task.CompletedTask;
            CompanyLocationDTO? companyLocationDTO = companyDTO.Locations.FirstOrDefault(location => location.Id == costCenterDTO.Location.Id);
            if (companyLocationDTO is null) return Task.CompletedTask;
            CostCenterDummyDTO? costCenterDummyDTO = companyLocationDTO.DummyItems.FirstOrDefault(dummy => dummy is CostCenterDummyDTO) as CostCenterDummyDTO;
            if (costCenterDummyDTO is null) return Task.CompletedTask;
            CostCenterDTO? costCenterToUpdate = costCenterDummyDTO.CostCenters.FirstOrDefault(costCenter => costCenter.Id == costCenterDTO.Id);
            if (costCenterToUpdate is null) return Task.CompletedTask;
            costCenterToUpdate.Id = costCenterDTO.Id;
            costCenterToUpdate.Name = costCenterDTO.Name;
            costCenterToUpdate.TradeName = costCenterDTO.TradeName;
            costCenterToUpdate.ShortName = costCenterDTO.ShortName;
            costCenterToUpdate.State = costCenterDTO.State;
            costCenterToUpdate.Address = costCenterDTO.Address;
            costCenterToUpdate.Phone1 = costCenterDTO.Phone1;
            costCenterToUpdate.Phone2 = costCenterDTO.Phone2;
            costCenterToUpdate.CellPhone1 = costCenterDTO.CellPhone1;
            costCenterToUpdate.CellPhone2 = costCenterDTO.CellPhone2;
            costCenterToUpdate.DateControlType = costCenterDTO.DateControlType;
            costCenterToUpdate.ShowChangeWindowOnCash = costCenterDTO.ShowChangeWindowOnCash;
            costCenterToUpdate.AllowBuy = costCenterDTO.AllowBuy;
            costCenterToUpdate.AllowSell = costCenterDTO.AllowSell;
            costCenterToUpdate.IsTaxable = costCenterDTO.IsTaxable;
            costCenterToUpdate.PriceListIncludeTax = costCenterDTO.PriceListIncludeTax;
            costCenterToUpdate.InvoicePriceIncludeTax = costCenterDTO.InvoicePriceIncludeTax;
            costCenterToUpdate.AllowRepeatItemsOnSales = costCenterDTO.AllowRepeatItemsOnSales;
            costCenterToUpdate.InvoiceCopiesToPrint = costCenterDTO.InvoiceCopiesToPrint;
            costCenterToUpdate.RequiresConfirmationToPrintCopies = costCenterDTO.RequiresConfirmationToPrintCopies;
            costCenterToUpdate.TaxToCost = costCenterDTO.TaxToCost;
            costCenterToUpdate.Country = costCenterDTO.Country;
            costCenterToUpdate.Department = costCenterDTO.Department;
            costCenterToUpdate.City = costCenterDTO.City;
            costCenterToUpdate.Location = costCenterDTO.Location;
            _notificationService.ShowSuccess("Centro de costo actualizado correctamente.");
            return Task.CompletedTask;
        }

        public Task HandleAsync(CostCenterDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CompanyDTO companyDTO = Companies.FirstOrDefault(company => company.Id == message.DeletedCostCenter.Location.Company.Id) ?? throw new Exception("");
                if (companyDTO is null) return;
                CompanyLocationDTO companyLocationDTO = companyDTO.Locations.FirstOrDefault(location => location.Id == message.DeletedCostCenter.Location.Id) ?? throw new Exception("");
                if (companyLocationDTO is null) return;
                CostCenterDummyDTO costCenterDummyDTO = companyLocationDTO.DummyItems.FirstOrDefault(dummy => dummy is CostCenterDummyDTO) as CostCenterDummyDTO ?? throw new Exception("");
                if (costCenterDummyDTO is null) return;
                costCenterDummyDTO.CostCenters.Remove(costCenterDummyDTO.CostCenters.Where(costCenter => costCenter.Id == message.DeletedCostCenter.Id).First());
                SelectedItem = null;
            });
            _notificationService.ShowSuccess("Centro de costo eliminado correctamente.");
            return Task.CompletedTask;
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
                if (propertyName.Contains("Phone"))
                {
                    // Remover espacios a la cadena
                    value = value.Replace(" ", "").Replace(Convert.ToChar(9).ToString(), "");
                    // Remover , = 44 y ; = 59
                    value = value.Replace(Convert.ToChar(44).ToString(), "").Replace(Convert.ToChar(59).ToString(), "");
                    // Remover - = 45 y _ = 95
                    value = value.Replace(Convert.ToChar(45).ToString(), "").Replace(Convert.ToChar(95).ToString(), "");
                }
                switch (propertyName)
                {
                    case nameof(CostCenterName):
                        if (string.IsNullOrEmpty(value.Trim())) AddError(propertyName, "El nombre no puede estar vacío");
                        break;
                    case nameof(CostCenterShortName):
                        if (string.IsNullOrEmpty(value.Trim())) AddError(propertyName, "El nombre corto no puede estar vacío");
                        break;
                    case nameof(CostCenterTradeName):
                        if (string.IsNullOrEmpty(value.Trim())) AddError(propertyName, "El nombre comercial no puede estar vacía");
                        break;
                    case nameof(CostCenterPhone1):
                        if (value.Length != 7 && !string.IsNullOrEmpty(value)) AddError(propertyName, "El número de teléfono debe contener 7 digitos");
                        break;
                    case nameof(CostCenterPhone2):
                        if (value.Length != 7 && !string.IsNullOrEmpty(value)) AddError(propertyName, "El número de teléfono debe contener 7 digitos");
                        break;
                    case nameof(CostCenterCellPhone1):
                        if (value.Length != 10 && !string.IsNullOrEmpty(value)) AddError(propertyName, "El número de teléfono celular debe contener 10 digitos");
                        break;
                    case nameof(CostCenterCellPhone2):
                        if (value.Length != 10 && !string.IsNullOrEmpty(value)) AddError(propertyName, "El número de teléfono celular debe contener 10 digitos");
                        break;
                    case nameof(CostCenterAddress):
                        if (string.IsNullOrEmpty(value.Trim())) AddError(propertyName, "La dirección no puede ser vacía");
                        break;
                    case nameof(StorageName):
                        if (string.IsNullOrEmpty(value.Trim())) AddError(propertyName, "El nombre no puede estar vacío");
                        break;
                    case nameof(StorageAddress):
                        if (string.IsNullOrEmpty(value.Trim())) AddError(propertyName, "La dirección no puede estar vacía");
                        break;
                    case nameof(CompanyLocationName):
                        if (string.IsNullOrEmpty(value.Trim())) AddError(propertyName, "El nombre no puede estar vacío");
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

        private void ValidateStorageProperties()
        {
            ValidateProperty(nameof(StorageName), StorageName);
            ValidateProperty(nameof(StorageAddress), StorageAddress);
        }

        private void ClearAllErrors()
        {
            _errors.Clear();
        }

        private void ValidateCostCenterProperties()
        {
            ValidateProperty(nameof(CostCenterName), CostCenterName);
            ValidateProperty(nameof(CostCenterShortName), CostCenterShortName);
            ValidateProperty(nameof(CostCenterTradeName), CostCenterTradeName);
            ValidateProperty(nameof(CostCenterAddress), CostCenterAddress);
        }

        public async Task HandleAsync(StorageCreateMessage message, CancellationToken cancellationToken)
        {
            IsNewRecord = false;
            StorageDTO storageDTO = Context.AutoMapper.Map<StorageDTO>(message.CreatedStorage);
            CompanyDTO companyDTO = Companies.FirstOrDefault(company => company.Id == storageDTO.Location.Company.Id) ?? throw new Exception("");
            if (companyDTO == null) return;
            CompanyLocationDTO companyLocationDTO = companyDTO.Locations.FirstOrDefault(location => location.Id == storageDTO.Location.Id) ?? throw new Exception("");
            if (companyLocationDTO == null) return;
            StorageDummyDTO storageDummyDTO = companyLocationDTO.DummyItems.FirstOrDefault(dummy => dummy is StorageDummyDTO) as StorageDummyDTO ?? throw new Exception("");
            if (storageDummyDTO == null) return;
            if (!storageDummyDTO.IsExpanded && storageDummyDTO.Storages[0].IsDummyChild)
            {
                await LoadStorages(companyLocationDTO, storageDummyDTO);
                storageDummyDTO.IsExpanded = true;
                StorageDTO? storage = storageDummyDTO.Storages.FirstOrDefault(x => x.Id == storageDTO.Id);
                if (storage is null) return;
                SelectedItem = storage;
                _notificationService.ShowSuccess("Almacén creado correctamente.");
                return;
            }
            if (!storageDummyDTO.IsExpanded)
            {
                storageDummyDTO.IsExpanded = true;
                storageDummyDTO.Storages.Add(storageDTO);
                SelectedItem = storageDTO;
                _notificationService.ShowSuccess("Almacén creado correctamente.");
                return;
            }
            storageDummyDTO.Storages.Add(storageDTO);
            SelectedItem = storageDTO;
            _notificationService.ShowSuccess("Almacén creado correctamente.");
            return;
        }

        public Task HandleAsync(StorageUpdateMessage message, CancellationToken cancellationToken)
        {
            StorageDTO storageDTO = Context.AutoMapper.Map<StorageDTO>(message.UpdatedStorage);
            CompanyDTO? companyDTO = Companies.FirstOrDefault(company => company.Id == storageDTO.Location.Company.Id);
            if (companyDTO is null) return Task.CompletedTask;
            CompanyLocationDTO? companyLocationDTO = companyDTO.Locations.FirstOrDefault(location => location.Id == storageDTO.Location.Id);
            if (companyLocationDTO is null) return Task.CompletedTask;
            StorageDummyDTO? storageDummyDTO = companyLocationDTO.DummyItems.FirstOrDefault(dummy => dummy is StorageDummyDTO) as StorageDummyDTO;
            if (storageDummyDTO is null) return Task.CompletedTask;
            StorageDTO? storageToUpdate = storageDummyDTO.Storages.FirstOrDefault(costCenter => costCenter.Id == storageDTO.Id);
            if (storageToUpdate is null) return Task.CompletedTask;
            storageToUpdate.Id = storageDTO.Id;
            storageToUpdate.Name = storageDTO.Name;
            storageToUpdate.Address = storageDTO.Address;
            storageToUpdate.State = storageDTO.State;
            storageToUpdate.City = storageDTO.City;
            storageToUpdate.Location = storageDTO.Location;
            _notificationService.ShowSuccess("Almacén actualizado correctamente.");
            return Task.CompletedTask;
        }

        public Task HandleAsync(StorageDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CompanyDTO companyDTO = Companies.FirstOrDefault(company => company.Id == message.DeletedStorage.Location.Company.Id) ?? throw new Exception("");
                if (companyDTO is null) return;
                CompanyLocationDTO companyLocationDTO = companyDTO.Locations.FirstOrDefault(location => location.Id == message.DeletedStorage.Location.Id) ?? throw new Exception("");
                if (companyLocationDTO is null) return;
                StorageDummyDTO storageDummyDTO = companyLocationDTO.DummyItems.FirstOrDefault(dummy => dummy is StorageDummyDTO) as StorageDummyDTO ?? throw new Exception("");
                if (storageDummyDTO is null) return;
                storageDummyDTO.Storages.Remove(storageDummyDTO.Storages.Where(storage => storage.Id == message.DeletedStorage.Id).First());
                SelectedItem = null;
            });
            _notificationService.ShowSuccess("Almacén eliminado correctamente.");
            return Task.CompletedTask;
        }

        public async Task HandleAsync(CompanyLocationCreateMessage message, CancellationToken cancellationToken)
        {
            IsNewRecord = false;
            CompanyLocationDTO companyLocationDTO = Context.AutoMapper.Map<CompanyLocationDTO>(message.CreatedCompanyLocation);
            CompanyDTO companyDTO = Companies.FirstOrDefault(company => company.Id == companyLocationDTO.Company.Id) ?? throw new Exception("");
            if (companyDTO is null) return;
            if (!companyDTO.IsExpanded && companyDTO.Locations[0].IsDummyChild)
            {
                await LoadCompaniesLocations(companyDTO);
                companyDTO.IsExpanded = true;
                CompanyLocationDTO? companyLocation = companyDTO.Locations.FirstOrDefault(x => x.Id == companyLocationDTO.Id);
                if (companyLocation is null) return;
                _notificationService.ShowSuccess("Ubicación de la compañía creada correctamente.");
                SelectedItem = companyLocation;
                return;
            }
            if (!companyDTO.IsExpanded)
            {
                companyDTO.IsExpanded = true;
                companyLocationDTO.DummyItems.Add(new CostCenterDummyDTO(this, companyLocationDTO));
                companyLocationDTO.DummyItems.Add(new StorageDummyDTO(this, companyLocationDTO));
                companyDTO.Locations.Add(companyLocationDTO);
                SelectedItem = companyLocationDTO;
                _notificationService.ShowSuccess("Ubicación de la compañía creada correctamente.");
                return;
            }
            companyLocationDTO.DummyItems.Add(new CostCenterDummyDTO(this, companyLocationDTO));
            companyLocationDTO.DummyItems.Add(new StorageDummyDTO(this, companyLocationDTO));
            companyDTO.Locations.Add(companyLocationDTO);
            SelectedItem = companyLocationDTO;
            _notificationService.ShowSuccess("Ubicación de la compañía creada correctamente.");
            return;
        }

        public Task HandleAsync(CompanyLocationUpdateMessage message, CancellationToken cancellationToken)
        {
            CompanyLocationDTO companyLocationDTO = Context.AutoMapper.Map<CompanyLocationDTO>(message.UpdatedCompanyLocation);
            CompanyDTO? companyDTO = Companies.FirstOrDefault(company => company.Id == companyLocationDTO.Company.Id);
            if (companyDTO is null) return Task.CompletedTask;
            CompanyLocationDTO? companyLocationToUpdate = companyDTO.Locations.FirstOrDefault(location => location.Id == companyLocationDTO.Id);
            if (companyLocationToUpdate is null) return Task.CompletedTask;
            companyLocationToUpdate.Id = companyLocationDTO.Id;
            companyLocationToUpdate.Name = companyLocationDTO.Name;
            companyLocationToUpdate.Company = companyLocationDTO.Company;
            _notificationService.ShowSuccess("Ubicación de la compañía actualizada correctamente.");
            return Task.CompletedTask;
        }

        public Task HandleAsync(CompanyLocationDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CompanyDTO companyDTO = Companies.FirstOrDefault(company => company.Id == message.DeletedCompanyLocation.Company.Id) ?? throw new Exception("");
                if (companyDTO is null) return;
                companyDTO.Locations.Remove(companyDTO.Locations.Where(location => location.Id == message.DeletedCompanyLocation.Id).First());
                SelectedItem = null;
            });
            _notificationService.ShowSuccess("Ubicación de la compañía eliminada correctamente.");
            return Task.CompletedTask;
        }

        public Task HandleAsync(CompanyUpdateMessage message, CancellationToken cancellationToken)
        {
            CompanyDTO companyDTO = Context.AutoMapper.Map<CompanyDTO>(message.UpdatedCompany);
            CompanyDTO? companyToUpdate = Companies.FirstOrDefault(company => company.Id == companyDTO.Id);
            if (companyToUpdate is null) return Task.CompletedTask;
            companyToUpdate.Id = companyDTO.Id;
            companyToUpdate.AccountingEntityCompany = companyDTO.AccountingEntityCompany;
            _notificationService.ShowSuccess("Compañía actualizada correctamente.");
            return Task.CompletedTask;
        }

        protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                Context.EventAggregator.Unsubscribe(this);
            }
            await base.OnDeactivateAsync(close, cancellationToken);
        }
    }
}
