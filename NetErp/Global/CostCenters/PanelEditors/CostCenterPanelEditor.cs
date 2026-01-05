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
        [ExpandoPath("Data.name")]
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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _tradeName = string.Empty;
        [ExpandoPath("Data.tradeName")]
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
                    ValidateTradeName();
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _shortName = string.Empty;
        [ExpandoPath("Data.shortName")]
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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _state = "A";
        [ExpandoPath("Data.state")]
        public string State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    NotifyOfPropertyChange(nameof(State));
                    this.TrackChange(nameof(State));
                }
            }
        }

        private string _address = string.Empty;
        [ExpandoPath("Data.address")]
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
                    ValidateAddress();
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _primaryPhone = string.Empty;
        [ExpandoPath("Data.primaryPhone")]
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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _secondaryPhone = string.Empty;
        [ExpandoPath("Data.secondaryPhone")]
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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _primaryCellPhone = string.Empty;
        [ExpandoPath("Data.primaryCellPhone")]
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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _secondaryCellPhone = string.Empty;
        [ExpandoPath("Data.secondaryCellPhone")]
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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _dateControlType = "FA";
        [ExpandoPath("Data.dateControlType")]
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
                }
            }
        }

        private bool _showChangeWindowOnCash;
        [ExpandoPath("Data.showChangeWindowOnCash")]
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
                }
            }
        }

        private bool _allowBuy;
        [ExpandoPath("Data.allowBuy")]
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
                }
            }
        }

        private bool _allowSell;
        [ExpandoPath("Data.allowSell")]
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
                }
            }
        }

        private bool _isTaxable;
        [ExpandoPath("Data.isTaxable")]
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
                }
            }
        }

        private bool _priceListIncludeTax;
        [ExpandoPath("Data.priceListIncludeTax")]
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
                }
            }
        }

        private bool _invoicePriceIncludeTax;
        [ExpandoPath("Data.invoicePriceIncludeTax")]
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
                }
            }
        }

        private int _invoiceCopiesToPrint;
        [ExpandoPath("Data.invoiceCopiesToPrint")]
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
                }
            }
        }

        public bool RequiresConfirmationToPrintCopiesIsEnabled => InvoiceCopiesToPrint > 0;

        private bool _requiresConfirmationToPrintCopies;
        [ExpandoPath("Data.requiresConfirmationToPrintCopies")]
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
                }
            }
        }

        private bool _allowRepeatItemsOnSales;
        [ExpandoPath("Data.allowRepeatItemsOnSales")]
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
                }
            }
        }

        private bool _taxToCost;
        [ExpandoPath("Data.taxToCost")]
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
                }
            }
        }

        private int _companyLocationId;
        [ExpandoPath("Data.companyLocationId")]
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

        [ExpandoPath("Data.countryId")]
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

        [ExpandoPath("Data.departmentId")]
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

        [ExpandoPath("Data.cityId")]
        public int SelectedCityId => SelectedCity?.Id ?? 0;

        // Properties required by API but with fixed values
        [ExpandoPath("Data.defaultInvoiceObservation")]
        public string DefaultInvoiceObservation => string.Empty;

        [ExpandoPath("Data.invoiceFooter")]
        public string InvoiceFooter => string.Empty;

        [ExpandoPath("Data.remissionFooter")]
        public string RemissionFooter => string.Empty;

        [ExpandoPath("Data.relatedAccountingEntityId")]
        public int RelatedAccountingEntityId => 0;

        public Dictionary<string, string> DateControlTypeDictionary => GlobalDictionaries.DateControlTypeDictionary;

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

        private void ValidateTradeName()
        {
            ClearErrors(nameof(TradeName));
            if (string.IsNullOrWhiteSpace(TradeName))
            {
                AddError(nameof(TradeName), "El nombre comercial no puede estar vacío");
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

        private void ValidateAddress()
        {
            ClearErrors(nameof(Address));
            if (string.IsNullOrWhiteSpace(Address))
            {
                AddError(nameof(Address), "La dirección no puede estar vacía");
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
            ValidateTradeName();
            ValidateShortName();
            ValidateAddress();
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
            State = "A";
            Address = string.Empty;
            PrimaryPhone = string.Empty;
            SecondaryPhone = string.Empty;
            PrimaryCellPhone = string.Empty;
            SecondaryCellPhone = string.Empty;
            DateControlType = "FA";
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
            State = costCenterDTO.Status;
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

            this.AcceptChanges();
            ClearAllErrors();
            ValidateAll();

            IsEditing = false;
        }

        private void SeedDefaultValues()
        {
            this.SeedValue(nameof(State), State);
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
            this.SeedValue(nameof(RelatedAccountingEntityId), RelatedAccountingEntityId);
            this.AcceptChanges();
        }

        #endregion

        #region Abstract Methods Implementation

        protected override int GetId() => Id;

        protected override string GetCreateQuery()
        {
            var fields = FieldSpec<CostCenterGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Field(f => f.Name)
                .Field(f => f.TradeName)
                .Field(f => f.ShortName)
                .Field(f => f.Status)
                .Field(f => f.Address)
                .Field(f => f.PrimaryPhone)
                .Field(f => f.SecondaryPhone)
                .Field(f => f.PrimaryCellPhone)
                .Field(f => f.SecondaryCellPhone)
                .Field(f => f.DateControlType)
                .Field(f => f.ShowChangeWindowOnCash)
                .Field(f => f.AllowBuy)
                .Field(f => f.AllowSell)
                .Field(f => f.IsTaxable)
                .Field(f => f.PriceListIncludeTax)
                .Field(f => f.InvoicePriceIncludeTax)
                .Field(f => f.AllowRepeatItemsOnSales)
                .Field(f => f.InvoiceCopiesToPrint)
                .Field(f => f.RequiresConfirmationToPrintCopies)
                .Field(f => f.TaxToCost)
                .Field(f => f.DefaultInvoiceObservation)
                .Field(f => f.InvoiceFooter)
                .Field(f => f.RemissionFooter)
                .Select(f => f.Country, nested => nested
                    .Field(n => n.Id)
                    .Field(n => n.Code)
                    .Field(n => n.Name))
                .Select(f => f.Department, nested => nested
                    .Field(n => n.Id)
                    .Field(n => n.Code)
                    .Field(n => n.Name))
                .Select(f => f.City, nested => nested
                    .Field(n => n.Id)
                    .Field(n => n.Code)
                    .Field(n => n.Name))
                .Select(f => f.CompanyLocation, nested => nested
                    .Field(n => n.Id)
                    .Select(n => n.Company, company => company
                        .Field(c => c.Id)))
                .Build();

            var parameter = new GraphQLQueryParameter("data", "CreateCostCenterInput!");
            var fragment = new GraphQLQueryFragment("createCostCenter", [parameter], fields, "CreateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override string GetUpdateQuery()
        {
            var fields = FieldSpec<CostCenterGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Field(f => f.Name)
                .Field(f => f.TradeName)
                .Field(f => f.ShortName)
                .Field(f => f.Status)
                .Field(f => f.Address)
                .Field(f => f.PrimaryPhone)
                .Field(f => f.SecondaryPhone)
                .Field(f => f.PrimaryCellPhone)
                .Field(f => f.SecondaryCellPhone)
                .Field(f => f.DateControlType)
                .Field(f => f.ShowChangeWindowOnCash)
                .Field(f => f.AllowBuy)
                .Field(f => f.AllowSell)
                .Field(f => f.IsTaxable)
                .Field(f => f.PriceListIncludeTax)
                .Field(f => f.InvoicePriceIncludeTax)
                .Field(f => f.AllowRepeatItemsOnSales)
                .Field(f => f.InvoiceCopiesToPrint)
                .Field(f => f.RequiresConfirmationToPrintCopies)
                .Field(f => f.TaxToCost)
                .Field(f => f.DefaultInvoiceObservation)
                .Field(f => f.InvoiceFooter)
                .Field(f => f.RemissionFooter)
                .Select(f => f.Country, nested => nested
                    .Field(n => n.Id)
                    .Field(n => n.Code)
                    .Field(n => n.Name))
                .Select(f => f.Department, nested => nested
                    .Field(n => n.Id)
                    .Field(n => n.Code)
                    .Field(n => n.Name))
                .Select(f => f.City, nested => nested
                    .Field(n => n.Id)
                    .Field(n => n.Code)
                    .Field(n => n.Name))
                .Select(f => f.CompanyLocation, nested => nested
                    .Field(n => n.Id)
                    .Select(n => n.Company, company => company
                        .Field(c => c.Id)))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new GraphQLQueryParameter("data", "UpdateCostCenterInput!"),
                new GraphQLQueryParameter("id", "Int!")
            };
            var fragment = new GraphQLQueryFragment("updateCostCenter", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override async Task<CostCenterGraphQLModel> ExecuteSaveAsync()
        {
            string query;
            dynamic variables;

            if (IsNewRecord)
            {
                // For new records, set CompanyLocationId before collecting changes
                CompanyLocationId = CompanyLocationIdBeforeNew;

                query = GetCreateQuery();
                variables = ChangeCollector.CollectChanges(this, prefix: "data");
            }
            else
            {
                query = GetUpdateQuery();
                variables = ChangeCollector.CollectChanges(this, prefix: "data");
                variables.id = Id;
            }

            return IsNewRecord
                ? await _costCenterService.CreateAsync(query, variables)
                : await _costCenterService.UpdateAsync(query, variables);
        }

        protected override async Task PublishMessageAsync(CostCenterGraphQLModel result)
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
