using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using Dictionaries;
using Models.Global;
using NetErp.Global.CostCenters.DTO;
using NetErp.Global.CostCenters.ViewModels;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.CostCenters.PanelEditors
{
    /// <summary>
    /// Panel Editor para la entidad CostCenter.
    /// Maneja la lógica de edición, validación y persistencia de centros de costo.
    /// </summary>
    public class CostCenterPanelEditor : CostCentersBasePanelEditor<CostCenterDTO, CostCenterGraphQLModel>
    {
        #region Fields

        private readonly IRepository<CostCenterGraphQLModel> _costCenterService;

        #endregion

        #region Constructor

        public CostCenterPanelEditor(
            CostCenterMasterViewModel masterContext,
            IRepository<CostCenterGraphQLModel> costCenterService)
            : base(masterContext)
        {
            _costCenterService = costCenterService ?? throw new ArgumentNullException(nameof(costCenterService));
        }

        #endregion

        #region Properties

        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    NotifyOfPropertyChange(nameof(Id));
                }
            }
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    NotifyOfPropertyChange(nameof(Name));
                    this.TrackChange(nameof(Name));
                    ValidateName();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _tradeName = string.Empty;
        public string TradeName
        {
            get => _tradeName;
            set
            {
                if (_tradeName != value)
                {
                    _tradeName = value;
                    NotifyOfPropertyChange(nameof(TradeName));
                    this.TrackChange(nameof(TradeName));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _shortName = string.Empty;
        public string ShortName
        {
            get => _shortName;
            set
            {
                if (_shortName != value)
                {
                    _shortName = value;
                    NotifyOfPropertyChange(nameof(ShortName));
                    this.TrackChange(nameof(ShortName));
                    ValidateShortName();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _status = "ACTIVE";
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    NotifyOfPropertyChange(nameof(Status));
                    this.TrackChange(nameof(Status));
                    NotifyOfPropertyChange(nameof(IsStatusActive));
                    NotifyOfPropertyChange(nameof(IsStatusReadOnly));
                    NotifyOfPropertyChange(nameof(IsStatusInactive));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        public bool IsStatusActive
        {
            get => Status == "ACTIVE";
            set
            {
                if (value) Status = "ACTIVE";
            }
        }

        public bool IsStatusReadOnly
        {
            get => Status == "READ_ONLY";
            set
            {
                if (value) Status = "READ_ONLY";
            }
        }

        public bool IsStatusInactive
        {
            get => Status == "INACTIVE";
            set
            {
                if (value) Status = "INACTIVE";
            }
        }

        private string _address = string.Empty;
        public string Address
        {
            get => _address;
            set
            {
                if (_address != value)
                {
                    _address = value;
                    NotifyOfPropertyChange(nameof(Address));
                    this.TrackChange(nameof(Address));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _primaryPhone = string.Empty;
        public string PrimaryPhone
        {
            get => _primaryPhone;
            set
            {
                if (_primaryPhone != value)
                {
                    _primaryPhone = value;
                    NotifyOfPropertyChange(nameof(PrimaryPhone));
                    this.TrackChange(nameof(PrimaryPhone));
                    ValidatePrimaryPhone();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _secondaryPhone = string.Empty;
        public string SecondaryPhone
        {
            get => _secondaryPhone;
            set
            {
                if (_secondaryPhone != value)
                {
                    _secondaryPhone = value;
                    NotifyOfPropertyChange(nameof(SecondaryPhone));
                    this.TrackChange(nameof(SecondaryPhone));
                    ValidateSecondaryPhone();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _primaryCellPhone = string.Empty;
        public string PrimaryCellPhone
        {
            get => _primaryCellPhone;
            set
            {
                if (_primaryCellPhone != value)
                {
                    _primaryCellPhone = value;
                    NotifyOfPropertyChange(nameof(PrimaryCellPhone));
                    this.TrackChange(nameof(PrimaryCellPhone));
                    ValidatePrimaryCellPhone();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _secondaryCellPhone = string.Empty;
        public string SecondaryCellPhone
        {
            get => _secondaryCellPhone;
            set
            {
                if (_secondaryCellPhone != value)
                {
                    _secondaryCellPhone = value;
                    NotifyOfPropertyChange(nameof(SecondaryCellPhone));
                    this.TrackChange(nameof(SecondaryCellPhone));
                    ValidateSecondaryCellPhone();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _dateControlType = "OPEN_DATE";
        public string DateControlType
        {
            get => _dateControlType;
            set
            {
                if (_dateControlType != value)
                {
                    _dateControlType = value;
                    NotifyOfPropertyChange(nameof(DateControlType));
                    this.TrackChange(nameof(DateControlType));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _showChangeWindowOnCash;
        public bool ShowChangeWindowOnCash
        {
            get => _showChangeWindowOnCash;
            set
            {
                if (_showChangeWindowOnCash != value)
                {
                    _showChangeWindowOnCash = value;
                    NotifyOfPropertyChange(nameof(ShowChangeWindowOnCash));
                    this.TrackChange(nameof(ShowChangeWindowOnCash));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _allowBuy;
        public bool AllowBuy
        {
            get => _allowBuy;
            set
            {
                if (_allowBuy != value)
                {
                    _allowBuy = value;
                    NotifyOfPropertyChange(nameof(AllowBuy));
                    this.TrackChange(nameof(AllowBuy));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _allowSell;
        public bool AllowSell
        {
            get => _allowSell;
            set
            {
                if (_allowSell != value)
                {
                    _allowSell = value;
                    NotifyOfPropertyChange(nameof(AllowSell));
                    this.TrackChange(nameof(AllowSell));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _isTaxable;
        public bool IsTaxable
        {
            get => _isTaxable;
            set
            {
                if (_isTaxable != value)
                {
                    _isTaxable = value;
                    NotifyOfPropertyChange(nameof(IsTaxable));
                    this.TrackChange(nameof(IsTaxable));

                    if (!_isTaxable)
                    {
                        InvoicePriceIncludeTax = false;
                        PriceListIncludeTax = false;
                    }
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _priceListIncludeTax;
        public bool PriceListIncludeTax
        {
            get => _priceListIncludeTax;
            set
            {
                if (_priceListIncludeTax != value)
                {
                    _priceListIncludeTax = value;
                    NotifyOfPropertyChange(nameof(PriceListIncludeTax));
                    this.TrackChange(nameof(PriceListIncludeTax));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _invoicePriceIncludeTax;
        public bool InvoicePriceIncludeTax
        {
            get => _invoicePriceIncludeTax;
            set
            {
                if (_invoicePriceIncludeTax != value)
                {
                    _invoicePriceIncludeTax = value;
                    NotifyOfPropertyChange(nameof(InvoicePriceIncludeTax));
                    this.TrackChange(nameof(InvoicePriceIncludeTax));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private int _invoiceCopiesToPrint;
        public int InvoiceCopiesToPrint
        {
            get => _invoiceCopiesToPrint;
            set
            {
                if (_invoiceCopiesToPrint != value)
                {
                    _invoiceCopiesToPrint = value;
                    NotifyOfPropertyChange(nameof(InvoiceCopiesToPrint));
                    this.TrackChange(nameof(InvoiceCopiesToPrint));

                    if (_invoiceCopiesToPrint == 0)
                    {
                        RequiresConfirmationToPrintCopies = false;
                    }
                    NotifyOfPropertyChange(nameof(RequiresConfirmationToPrintCopiesIsEnabled));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        public bool RequiresConfirmationToPrintCopiesIsEnabled => InvoiceCopiesToPrint > 0;

        private bool _requiresConfirmationToPrintCopies;
        public bool RequiresConfirmationToPrintCopies
        {
            get => _requiresConfirmationToPrintCopies;
            set
            {
                if (_requiresConfirmationToPrintCopies != value)
                {
                    _requiresConfirmationToPrintCopies = value;
                    NotifyOfPropertyChange(nameof(RequiresConfirmationToPrintCopies));
                    this.TrackChange(nameof(RequiresConfirmationToPrintCopies));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _allowRepeatItemsOnSales;
        public bool AllowRepeatItemsOnSales
        {
            get => _allowRepeatItemsOnSales;
            set
            {
                if (_allowRepeatItemsOnSales != value)
                {
                    _allowRepeatItemsOnSales = value;
                    NotifyOfPropertyChange(nameof(AllowRepeatItemsOnSales));
                    this.TrackChange(nameof(AllowRepeatItemsOnSales));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _taxToCost;
        public bool TaxToCost
        {
            get => _taxToCost;
            set
            {
                if (_taxToCost != value)
                {
                    _taxToCost = value;
                    NotifyOfPropertyChange(nameof(TaxToCost));
                    this.TrackChange(nameof(TaxToCost));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private int _companyLocationId;
        public int CompanyLocationId
        {
            get => _companyLocationId;
            set
            {
                if (_companyLocationId != value)
                {
                    _companyLocationId = value;
                    NotifyOfPropertyChange(nameof(CompanyLocationId));
                    this.TrackChange(nameof(CompanyLocationId));
                }
            }
        }

        private CountryGraphQLModel? _selectedCountry;
        public CountryGraphQLModel? SelectedCountry
        {
            get => _selectedCountry;
            set
            {
                if (_selectedCountry != value)
                {
                    _selectedCountry = value;
                    NotifyOfPropertyChange(nameof(SelectedCountry));
                    NotifyOfPropertyChange(nameof(SelectedCountryId));
                    this.TrackChange(nameof(SelectedCountryId));
                }
            }
        }

        [ExpandoPath("countryId")]
        public int SelectedCountryId => SelectedCountry?.Id ?? 0;

        private DepartmentGraphQLModel? _selectedDepartment;
        public DepartmentGraphQLModel? SelectedDepartment
        {
            get => _selectedDepartment;
            set
            {
                if (_selectedDepartment != value)
                {
                    _selectedDepartment = value;
                    NotifyOfPropertyChange(nameof(SelectedDepartment));
                    NotifyOfPropertyChange(nameof(SelectedDepartmentId));
                    this.TrackChange(nameof(SelectedDepartmentId));
                }
            }
        }

        [ExpandoPath("departmentId")]
        public int SelectedDepartmentId => SelectedDepartment?.Id ?? 0;

        private CityGraphQLModel? _selectedCity;
        public CityGraphQLModel? SelectedCity
        {
            get => _selectedCity;
            set
            {
                if (_selectedCity != value)
                {
                    _selectedCity = value;
                    NotifyOfPropertyChange(nameof(SelectedCity));
                    NotifyOfPropertyChange(nameof(SelectedCityId));
                    this.TrackChange(nameof(SelectedCityId));
                }
            }
        }

        [ExpandoPath("cityId")]
        public int SelectedCityId => SelectedCity?.Id ?? 0;

        private string _defaultInvoiceObservation = string.Empty;
        public string DefaultInvoiceObservation
        {
            get => _defaultInvoiceObservation;
            set
            {
                if (_defaultInvoiceObservation != value)
                {
                    _defaultInvoiceObservation = value;
                    NotifyOfPropertyChange(nameof(DefaultInvoiceObservation));
                    this.TrackChange(nameof(DefaultInvoiceObservation));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _invoiceFooter = string.Empty;
        public string InvoiceFooter
        {
            get => _invoiceFooter;
            set
            {
                if (_invoiceFooter != value)
                {
                    _invoiceFooter = value;
                    NotifyOfPropertyChange(nameof(InvoiceFooter));
                    this.TrackChange(nameof(InvoiceFooter));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _remissionFooter = string.Empty;
        public string RemissionFooter
        {
            get => _remissionFooter;
            set
            {
                if (_remissionFooter != value)
                {
                    _remissionFooter = value;
                    NotifyOfPropertyChange(nameof(RemissionFooter));
                    this.TrackChange(nameof(RemissionFooter));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        public Dictionary<string, string> DateControlTypeDictionary => GlobalDictionaries.DateControlTypeDictionary;

        public Dictionary<string, string> CostCenterStatusDictionary => GlobalDictionaries.CostCenterStatusDictionary;

        public int CompanyLocationIdBeforeNew { get; set; }

        #endregion

        #region CanSave

        public override bool CanSave
        {
            get
            {
                if (!IsEditing) return false;
                if (HasErrors) return false;
                if (!this.HasChanges()) return false;
                return true;
            }
        }

        #endregion

        #region Validation Methods

        private void ValidateName()
        {
            ClearErrors(nameof(Name));
            if (string.IsNullOrWhiteSpace(Name))
            {
                AddError(nameof(Name), "El nombre no puede estar vacío");
            }
        }

        private void ValidateShortName()
        {
            ClearErrors(nameof(ShortName));
            if (string.IsNullOrWhiteSpace(ShortName))
            {
                AddError(nameof(ShortName), "El nombre corto no puede estar vacío");
            }
        }

        private void ValidatePrimaryPhone()
        {
            ClearErrors(nameof(PrimaryPhone));
            string cleanPhone = CleanPhoneForValidation(PrimaryPhone);
            if (!string.IsNullOrEmpty(cleanPhone) && cleanPhone.Length != 7)
            {
                AddError(nameof(PrimaryPhone), "El número de teléfono debe contener 7 dígitos");
            }
        }

        private void ValidateSecondaryPhone()
        {
            ClearErrors(nameof(SecondaryPhone));
            string cleanPhone = CleanPhoneForValidation(SecondaryPhone);
            if (!string.IsNullOrEmpty(cleanPhone) && cleanPhone.Length != 7)
            {
                AddError(nameof(SecondaryPhone), "El número de teléfono debe contener 7 dígitos");
            }
        }

        private void ValidatePrimaryCellPhone()
        {
            ClearErrors(nameof(PrimaryCellPhone));
            string cleanPhone = CleanPhoneForValidation(PrimaryCellPhone);
            if (!string.IsNullOrEmpty(cleanPhone) && cleanPhone.Length != 10)
            {
                AddError(nameof(PrimaryCellPhone), "El número de celular debe contener 10 dígitos");
            }
        }

        private void ValidateSecondaryCellPhone()
        {
            ClearErrors(nameof(SecondaryCellPhone));
            string cleanPhone = CleanPhoneForValidation(SecondaryCellPhone);
            if (!string.IsNullOrEmpty(cleanPhone) && cleanPhone.Length != 10)
            {
                AddError(nameof(SecondaryCellPhone), "El número de celular debe contener 10 dígitos");
            }
        }

        public override void ValidateAll()
        {
            ValidateName();
            ValidateShortName();
            ValidatePrimaryPhone();
            ValidateSecondaryPhone();
            ValidatePrimaryCellPhone();
            ValidateSecondaryCellPhone();
        }

        #endregion

        #region SetForNew / SetForEdit

        public override void SetForNew(object context)
        {
            if (context is int companyLocationId)
            {
                CompanyLocationIdBeforeNew = companyLocationId;
            }

            OriginalDto = null;
            Id = 0;
            Name = string.Empty;
            TradeName = string.Empty;
            ShortName = string.Empty;
            Status = "ACTIVE";
            Address = string.Empty;
            PrimaryPhone = string.Empty;
            SecondaryPhone = string.Empty;
            PrimaryCellPhone = string.Empty;
            SecondaryCellPhone = string.Empty;
            DateControlType = "OPEN_DATE";
            ShowChangeWindowOnCash = false;
            AllowBuy = false;
            AllowSell = false;
            IsTaxable = false;
            PriceListIncludeTax = false;
            InvoicePriceIncludeTax = false;
            RequiresConfirmationToPrintCopies = false;
            InvoiceCopiesToPrint = 0;
            AllowRepeatItemsOnSales = false;
            TaxToCost = false;
            DefaultInvoiceObservation = string.Empty;
            InvoiceFooter = string.Empty;
            RemissionFooter = string.Empty;

            // Set default country (Colombia = 169)
            SelectedCountry = MasterContext.Countries?.FirstOrDefault(c => c.Code == "169");
            if (SelectedCountry != null)
            {
                SelectedDepartment = SelectedCountry.Departments?.FirstOrDefault();
                if (SelectedDepartment != null)
                {
                    SelectedCity = SelectedDepartment.Cities?.FirstOrDefault();
                }
            }

            SeedDefaultValues();
            ClearAllErrors();
            ValidateAll();

            IsEditing = true;
        }

        public override void SetForEdit(object dto)
        {
            if (dto is not CostCenterDTO costCenterDTO) return;

            OriginalDto = costCenterDTO;
            Id = costCenterDTO.Id;
            Name = costCenterDTO.Name;
            TradeName = costCenterDTO.TradeName;
            ShortName = costCenterDTO.ShortName;
            Status = costCenterDTO.Status;
            Address = costCenterDTO.Address;
            PrimaryPhone = costCenterDTO.PrimaryPhone;
            SecondaryPhone = costCenterDTO.SecondaryPhone;
            PrimaryCellPhone = costCenterDTO.PrimaryCellPhone;
            SecondaryCellPhone = costCenterDTO.SecondaryCellPhone;
            DateControlType = costCenterDTO.DateControlType;
            ShowChangeWindowOnCash = costCenterDTO.ShowChangeWindowOnCash;
            AllowBuy = costCenterDTO.AllowBuy;
            AllowSell = costCenterDTO.AllowSell;
            IsTaxable = costCenterDTO.IsTaxable;
            PriceListIncludeTax = costCenterDTO.PriceListIncludeTax;
            InvoicePriceIncludeTax = costCenterDTO.InvoicePriceIncludeTax;
            RequiresConfirmationToPrintCopies = costCenterDTO.RequiresConfirmationToPrintCopies;
            InvoiceCopiesToPrint = costCenterDTO.InvoiceCopiesToPrint;
            AllowRepeatItemsOnSales = costCenterDTO.AllowRepeatItemsOnSales;
            TaxToCost = costCenterDTO.TaxToCost;
            CompanyLocationId = costCenterDTO.CompanyLocation?.Id ?? 0;
            DefaultInvoiceObservation = costCenterDTO.DefaultInvoiceObservation ?? string.Empty;
            InvoiceFooter = costCenterDTO.InvoiceFooter ?? string.Empty;
            RemissionFooter = costCenterDTO.RemissionFooter ?? string.Empty;

            // Set country/department/city from DTO
            SelectedCountry = MasterContext.Countries?.FirstOrDefault(c => c.Id == costCenterDTO.Country?.Id);
            if (SelectedCountry != null)
            {
                SelectedDepartment = SelectedCountry.Departments?.FirstOrDefault(d => d.Id == costCenterDTO.Department?.Id);
                if (SelectedDepartment != null)
                {
                    SelectedCity = SelectedDepartment.Cities?.FirstOrDefault(c => c.Id == costCenterDTO.City?.Id);
                }
            }

            SeedCurrentValues();
            ClearAllErrors();
            ValidateAll();

            IsEditing = false;
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(TradeName), TradeName);
            this.SeedValue(nameof(ShortName), ShortName);
            this.SeedValue(nameof(Status), Status);
            this.SeedValue(nameof(Address), Address);
            this.SeedValue(nameof(PrimaryPhone), PrimaryPhone);
            this.SeedValue(nameof(SecondaryPhone), SecondaryPhone);
            this.SeedValue(nameof(PrimaryCellPhone), PrimaryCellPhone);
            this.SeedValue(nameof(SecondaryCellPhone), SecondaryCellPhone);
            this.SeedValue(nameof(DateControlType), DateControlType);
            this.SeedValue(nameof(ShowChangeWindowOnCash), ShowChangeWindowOnCash);
            this.SeedValue(nameof(AllowBuy), AllowBuy);
            this.SeedValue(nameof(AllowSell), AllowSell);
            this.SeedValue(nameof(IsTaxable), IsTaxable);
            this.SeedValue(nameof(PriceListIncludeTax), PriceListIncludeTax);
            this.SeedValue(nameof(InvoicePriceIncludeTax), InvoicePriceIncludeTax);
            this.SeedValue(nameof(RequiresConfirmationToPrintCopies), RequiresConfirmationToPrintCopies);
            this.SeedValue(nameof(InvoiceCopiesToPrint), InvoiceCopiesToPrint);
            this.SeedValue(nameof(AllowRepeatItemsOnSales), AllowRepeatItemsOnSales);
            this.SeedValue(nameof(TaxToCost), TaxToCost);
            this.SeedValue(nameof(CompanyLocationId), CompanyLocationId);
            this.SeedValue(nameof(SelectedCountryId), SelectedCountryId);
            this.SeedValue(nameof(SelectedDepartmentId), SelectedDepartmentId);
            this.SeedValue(nameof(SelectedCityId), SelectedCityId);
            this.SeedValue(nameof(DefaultInvoiceObservation), DefaultInvoiceObservation);
            this.SeedValue(nameof(InvoiceFooter), InvoiceFooter);
            this.SeedValue(nameof(RemissionFooter), RemissionFooter);
            this.AcceptChanges();
        }

        private void SeedDefaultValues()
        {
            this.SeedValue(nameof(Status), Status);
            this.SeedValue(nameof(DateControlType), DateControlType);
            this.SeedValue(nameof(ShowChangeWindowOnCash), ShowChangeWindowOnCash);
            this.SeedValue(nameof(AllowBuy), AllowBuy);
            this.SeedValue(nameof(AllowSell), AllowSell);
            this.SeedValue(nameof(IsTaxable), IsTaxable);
            this.SeedValue(nameof(PriceListIncludeTax), PriceListIncludeTax);
            this.SeedValue(nameof(InvoicePriceIncludeTax), InvoicePriceIncludeTax);
            this.SeedValue(nameof(RequiresConfirmationToPrintCopies), RequiresConfirmationToPrintCopies);
            this.SeedValue(nameof(InvoiceCopiesToPrint), InvoiceCopiesToPrint);
            this.SeedValue(nameof(AllowRepeatItemsOnSales), AllowRepeatItemsOnSales);
            this.SeedValue(nameof(TaxToCost), TaxToCost);
            this.SeedValue(nameof(SelectedCountryId), SelectedCountryId);
            this.SeedValue(nameof(SelectedDepartmentId), SelectedDepartmentId);
            this.SeedValue(nameof(SelectedCityId), SelectedCityId);
            this.SeedValue(nameof(DefaultInvoiceObservation), DefaultInvoiceObservation);
            this.SeedValue(nameof(InvoiceFooter), InvoiceFooter);
            this.SeedValue(nameof(RemissionFooter), RemissionFooter);
            this.AcceptChanges();
        }

        #endregion

        #region Abstract Methods Implementation

        protected override int GetId() => Id;

        protected override string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<CostCenterGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "costCenter", nested: entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.TradeName)
                    .Field(e => e.ShortName)
                    .Field(e => e.Status)
                    .Field(e => e.Address)
                    .Field(e => e.PrimaryPhone)
                    .Field(e => e.SecondaryPhone)
                    .Field(e => e.PrimaryCellPhone)
                    .Field(e => e.SecondaryCellPhone)
                    .Field(e => e.DateControlType)
                    .Field(e => e.ShowChangeWindowOnCash)
                    .Field(e => e.AllowBuy)
                    .Field(e => e.AllowSell)
                    .Field(e => e.IsTaxable)
                    .Field(e => e.PriceListIncludeTax)
                    .Field(e => e.InvoicePriceIncludeTax)
                    .Field(e => e.AllowRepeatItemsOnSales)
                    .Field(e => e.InvoiceCopiesToPrint)
                    .Field(e => e.RequiresConfirmationToPrintCopies)
                    .Field(e => e.TaxToCost)
                    .Field(e => e.DefaultInvoiceObservation)
                    .Field(e => e.InvoiceFooter)
                    .Field(e => e.RemissionFooter)
                    .Select(e => e.Country, country => country
                        .Field(c => c.Id)
                        .Field(c => c.Code)
                        .Field(c => c.Name))
                    .Select(e => e.Department, dept => dept
                        .Field(d => d.Id)
                        .Field(d => d.Code)
                        .Field(d => d.Name))
                    .Select(e => e.City, city => city
                        .Field(c => c.Id)
                        .Field(c => c.Code)
                        .Field(c => c.Name))
                    .Select(e => e.CompanyLocation, loc => loc
                        .Field(l => l.Id)
                        .Select(l => l.Company, company => company
                            .Field(c => c.Id))))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateCostCenterInput!");
            var fragment = new GraphQLQueryFragment("createCostCenter", [parameter], fields, "CreateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<CostCenterGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "costCenter", nested: entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.TradeName)
                    .Field(e => e.ShortName)
                    .Field(e => e.Status)
                    .Field(e => e.Address)
                    .Field(e => e.PrimaryPhone)
                    .Field(e => e.SecondaryPhone)
                    .Field(e => e.PrimaryCellPhone)
                    .Field(e => e.SecondaryCellPhone)
                    .Field(e => e.DateControlType)
                    .Field(e => e.ShowChangeWindowOnCash)
                    .Field(e => e.AllowBuy)
                    .Field(e => e.AllowSell)
                    .Field(e => e.IsTaxable)
                    .Field(e => e.PriceListIncludeTax)
                    .Field(e => e.InvoicePriceIncludeTax)
                    .Field(e => e.AllowRepeatItemsOnSales)
                    .Field(e => e.InvoiceCopiesToPrint)
                    .Field(e => e.RequiresConfirmationToPrintCopies)
                    .Field(e => e.TaxToCost)
                    .Field(e => e.DefaultInvoiceObservation)
                    .Field(e => e.InvoiceFooter)
                    .Field(e => e.RemissionFooter)
                    .Select(e => e.Country, country => country
                        .Field(c => c.Id)
                        .Field(c => c.Code)
                        .Field(c => c.Name))
                    .Select(e => e.Department, dept => dept
                        .Field(d => d.Id)
                        .Field(d => d.Code)
                        .Field(d => d.Name))
                    .Select(e => e.City, city => city
                        .Field(c => c.Id)
                        .Field(c => c.Code)
                        .Field(c => c.Name))
                    .Select(e => e.CompanyLocation, loc => loc
                        .Field(l => l.Id)
                        .Select(l => l.Company, company => company
                            .Field(c => c.Id))))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateCostCenterInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateCostCenter", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override async Task<UpsertResponseType<CostCenterGraphQLModel>> ExecuteSaveAsync()
        {
            string query;
            dynamic variables;

            if (IsNewRecord)
            {
                // For new records, set CompanyLocationId before collecting changes
                CompanyLocationId = CompanyLocationIdBeforeNew;

                query = GetCreateQuery();
                variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
            }
            else
            {
                query = GetUpdateQuery();
                variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = Id;
            }

            return IsNewRecord
                ? await _costCenterService.CreateAsync<UpsertResponseType<CostCenterGraphQLModel>>(query, variables)
                : await _costCenterService.UpdateAsync<UpsertResponseType<CostCenterGraphQLModel>>(query, variables);
        }

        protected override async Task PublishMessageAsync(UpsertResponseType<CostCenterGraphQLModel> result)
        {
            if (IsNewRecord)
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new CostCenterCreateMessage { CreatedCostCenter = result });
            }
            else
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new CostCenterUpdateMessage { UpdatedCostCenter = result });
            }
        }

        #endregion
    }
}
