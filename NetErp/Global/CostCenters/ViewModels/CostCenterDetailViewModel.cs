using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Dictionaries;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Global.CostCenters.Shared;
using NetErp.Global.CostCenters.Validators;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.CostCenters.ViewModels
{
    /// <summary>
    /// Detail dialog ViewModel para CostCenter (centro de costo).
    /// El mÃ¡s complejo del mÃ³dulo: 27 campos, cascada Countryâ†’Deptâ†’City,
    /// 4 phone validators, status tri-valor, cascadas IsTaxable e InvoiceCopiesToPrint.
    /// </summary>
    public class CostCenterDetailViewModel : CostCentersDetailViewModelBase
    {
        #region Constants

        private const string DefaultCountryCode = "169"; // Colombia

        #endregion

        #region Dependencies

        private readonly IRepository<CostCenterGraphQLModel> _costCenterService;
        private readonly StringLengthCache _stringLengthCache;
        private readonly AuthorizationSequenceCache _authorizationSequenceCache;
        private readonly CostCenterValidator _validator;

        #endregion

        #region Constructor

        public CostCenterDetailViewModel(
            IRepository<CostCenterGraphQLModel> costCenterService,
            IEventAggregator eventAggregator,
            StringLengthCache stringLengthCache,
            AuthorizationSequenceCache authorizationSequenceCache,
            JoinableTaskFactory joinableTaskFactory,
            CostCenterValidator validator)
            : base(joinableTaskFactory, eventAggregator)
        {
            _costCenterService = costCenterService ?? throw new ArgumentNullException(nameof(costCenterService));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _authorizationSequenceCache = authorizationSequenceCache ?? throw new ArgumentNullException(nameof(authorizationSequenceCache));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));

            DialogWidth = 720;
            DialogHeight = 620;
        }

        #endregion

        #region Sources

        public ObservableCollection<CountryGraphQLModel> Countries { get; set; } = [];

        public ReadOnlyObservableCollection<AuthorizationSequenceGraphQLModel> AuthorizationSequences => _authorizationSequenceCache.Items;

        public Dictionary<string, string> DateControlTypeDictionary => GlobalDictionaries.DateControlTypeDictionary;
        public Dictionary<string, string> CostCenterStatusDictionary => GlobalDictionaries.CostCenterStatusDictionary;

        #endregion

        #region MaxLength

        public int NameMaxLength => _stringLengthCache.GetMaxLength<CostCenterGraphQLModel>(nameof(CostCenterGraphQLModel.Name));
        public int TradeNameMaxLength => _stringLengthCache.GetMaxLength<CostCenterGraphQLModel>(nameof(CostCenterGraphQLModel.TradeName));
        public int ShortNameMaxLength => _stringLengthCache.GetMaxLength<CostCenterGraphQLModel>(nameof(CostCenterGraphQLModel.ShortName));
        public int AddressMaxLength => _stringLengthCache.GetMaxLength<CostCenterGraphQLModel>(nameof(CostCenterGraphQLModel.Address));
        public int PrimaryPhoneMaxLength => _stringLengthCache.GetMaxLength<CostCenterGraphQLModel>(nameof(CostCenterGraphQLModel.PrimaryPhone));
        public int SecondaryPhoneMaxLength => _stringLengthCache.GetMaxLength<CostCenterGraphQLModel>(nameof(CostCenterGraphQLModel.SecondaryPhone));
        public int PrimaryCellPhoneMaxLength => _stringLengthCache.GetMaxLength<CostCenterGraphQLModel>(nameof(CostCenterGraphQLModel.PrimaryCellPhone));
        public int SecondaryCellPhoneMaxLength => _stringLengthCache.GetMaxLength<CostCenterGraphQLModel>(nameof(CostCenterGraphQLModel.SecondaryCellPhone));
        public int DefaultInvoiceObservationMaxLength => _stringLengthCache.GetMaxLength<CostCenterGraphQLModel>(nameof(CostCenterGraphQLModel.DefaultInvoiceObservation));
        public int InvoiceFooterMaxLength => _stringLengthCache.GetMaxLength<CostCenterGraphQLModel>(nameof(CostCenterGraphQLModel.InvoiceFooter));
        public int RemissionFooterMaxLength => _stringLengthCache.GetMaxLength<CostCenterGraphQLModel>(nameof(CostCenterGraphQLModel.RemissionFooter));

        #endregion

        #region Form Properties â€” Basic

        [ExpandoPath("name")]
        public string Name
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Name));
                    ValidateProperty(nameof(Name), value);
                    this.TrackChange(nameof(Name), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("tradeName")]
        public string TradeName
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(TradeName));
                    this.TrackChange(nameof(TradeName), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("shortName")]
        public string ShortName
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ShortName));
                    ValidateProperty(nameof(ShortName), value);
                    this.TrackChange(nameof(ShortName), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("status")]
        public string Status
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Status));
                    NotifyOfPropertyChange(nameof(IsStatusActive));
                    NotifyOfPropertyChange(nameof(IsStatusReadOnly));
                    NotifyOfPropertyChange(nameof(IsStatusInactive));
                    this.TrackChange(nameof(Status), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = CostCentersStatus.Active;

        public bool IsStatusActive
        {
            get => Status == CostCentersStatus.Active;
            set { if (value) Status = CostCentersStatus.Active; }
        }

        public bool IsStatusReadOnly
        {
            get => Status == CostCentersStatus.ReadOnly;
            set { if (value) Status = CostCentersStatus.ReadOnly; }
        }

        public bool IsStatusInactive
        {
            get => Status == CostCentersStatus.Inactive;
            set { if (value) Status = CostCentersStatus.Inactive; }
        }

        [ExpandoPath("address")]
        public string Address
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Address));
                    this.TrackChange(nameof(Address), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        #endregion

        #region Form Properties â€” Phones

        [ExpandoPath("primaryPhone")]
        public string PrimaryPhone
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PrimaryPhone));
                    ValidateProperty(nameof(PrimaryPhone), value);
                    this.TrackChange(nameof(PrimaryPhone), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("secondaryPhone")]
        public string SecondaryPhone
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SecondaryPhone));
                    ValidateProperty(nameof(SecondaryPhone), value);
                    this.TrackChange(nameof(SecondaryPhone), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("primaryCellPhone")]
        public string PrimaryCellPhone
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PrimaryCellPhone));
                    ValidateProperty(nameof(PrimaryCellPhone), value);
                    this.TrackChange(nameof(PrimaryCellPhone), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("secondaryCellPhone")]
        public string SecondaryCellPhone
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SecondaryCellPhone));
                    ValidateProperty(nameof(SecondaryCellPhone), value);
                    this.TrackChange(nameof(SecondaryCellPhone), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        #endregion

        #region Form Properties â€” Sales config

        [ExpandoPath("dateControlType")]
        public string DateControlType
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DateControlType));
                    this.TrackChange(nameof(DateControlType), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = "OPEN_DATE";

        [ExpandoPath("showChangeWindowOnCash")]
        public bool ShowChangeWindowOnCash
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ShowChangeWindowOnCash));
                    this.TrackChange(nameof(ShowChangeWindowOnCash), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("allowBuy")]
        public bool AllowBuy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AllowBuy));
                    this.TrackChange(nameof(AllowBuy), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("allowSell")]
        public bool AllowSell
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AllowSell));
                    this.TrackChange(nameof(AllowSell), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("allowRepeatItemsOnSales")]
        public bool AllowRepeatItemsOnSales
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AllowRepeatItemsOnSales));
                    this.TrackChange(nameof(AllowRepeatItemsOnSales), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        #endregion

        #region Form Properties â€” Tax / Invoice

        [ExpandoPath("isTaxable")]
        public bool IsTaxable
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsTaxable));
                    this.TrackChange(nameof(IsTaxable), value);

                    // Cascada: si IsTaxable=false, limpiar dependientes
                    if (!field)
                    {
                        PriceListIncludeTax = false;
                        InvoicePriceIncludeTax = false;
                    }

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("priceListIncludeTax")]
        public bool PriceListIncludeTax
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PriceListIncludeTax));
                    this.TrackChange(nameof(PriceListIncludeTax), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("invoicePriceIncludeTax")]
        public bool InvoicePriceIncludeTax
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(InvoicePriceIncludeTax));
                    this.TrackChange(nameof(InvoicePriceIncludeTax), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("taxToCost")]
        public bool TaxToCost
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(TaxToCost));
                    this.TrackChange(nameof(TaxToCost), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("invoiceCopiesToPrint")]
        public int InvoiceCopiesToPrint
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(InvoiceCopiesToPrint));
                    this.TrackChange(nameof(InvoiceCopiesToPrint), value);

                    // Cascada: si copies=0, limpiar confirmation
                    if (field == 0)
                    {
                        RequiresConfirmationToPrintCopies = false;
                    }

                    NotifyOfPropertyChange(nameof(RequiresConfirmationToPrintCopiesIsEnabled));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool RequiresConfirmationToPrintCopiesIsEnabled => InvoiceCopiesToPrint > 0;

        [ExpandoPath("requiresConfirmationToPrintCopies")]
        public bool RequiresConfirmationToPrintCopies
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(RequiresConfirmationToPrintCopies));
                    this.TrackChange(nameof(RequiresConfirmationToPrintCopies), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        #endregion

        #region Form Properties â€” Invoice texts

        [ExpandoPath("defaultInvoiceObservation")]
        public string DefaultInvoiceObservation
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DefaultInvoiceObservation));
                    this.TrackChange(nameof(DefaultInvoiceObservation), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("invoiceFooter")]
        public string InvoiceFooter
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(InvoiceFooter));
                    this.TrackChange(nameof(InvoiceFooter), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("remissionFooter")]
        public string RemissionFooter
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(RemissionFooter));
                    this.TrackChange(nameof(RemissionFooter), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        #endregion

        #region Form Properties â€” Geography & Parent

        [ExpandoPath("companyLocationId")]
        public int CompanyLocationId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CompanyLocationId));
                    this.TrackChange(nameof(CompanyLocationId), value);
                }
            }
        }

        public CountryGraphQLModel? SelectedCountry
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCountry));
                    NotifyOfPropertyChange(nameof(SelectedCountryId));
                    this.TrackChange(nameof(SelectedCountryId), value);
                    SelectedDepartment = GeographicCascadeHelper.FirstDepartment(value);
                }
            }
        }

        [ExpandoPath("countryId")]
        public int SelectedCountryId => SelectedCountry?.Id ?? 0;

        public DepartmentGraphQLModel? SelectedDepartment
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedDepartment));
                    NotifyOfPropertyChange(nameof(SelectedDepartmentId));
                    this.TrackChange(nameof(SelectedDepartmentId), value);
                    SelectedCity = value?.Cities?.FirstOrDefault();
                }
            }
        }

        [ExpandoPath("departmentId")]
        public int SelectedDepartmentId => SelectedDepartment?.Id ?? 0;

        public CityGraphQLModel? SelectedCity
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCity));
                    NotifyOfPropertyChange(nameof(SelectedCityId));
                    this.TrackChange(nameof(SelectedCityId), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("cityId")]
        public int SelectedCityId => SelectedCity?.Id ?? 0;

        #endregion

        #region Form Properties â€” Authorization Sequences (DIAN)

        [ExpandoPath("feCreditDefaultAuthorizationSequenceId")]
        public int? FeCreditDefaultAuthorizationSequenceId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FeCreditDefaultAuthorizationSequenceId));
                    this.TrackChange(nameof(FeCreditDefaultAuthorizationSequenceId), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("feCashDefaultAuthorizationSequenceId")]
        public int? FeCashDefaultAuthorizationSequenceId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FeCashDefaultAuthorizationSequenceId));
                    this.TrackChange(nameof(FeCashDefaultAuthorizationSequenceId), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("peDefaultAuthorizationSequenceId")]
        public int? PeDefaultAuthorizationSequenceId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PeDefaultAuthorizationSequenceId));
                    this.TrackChange(nameof(PeDefaultAuthorizationSequenceId), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("dsDefaultAuthorizationSequenceId")]
        public int? DsDefaultAuthorizationSequenceId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DsDefaultAuthorizationSequenceId));
                    this.TrackChange(nameof(DsDefaultAuthorizationSequenceId), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        #endregion

        #region CanSave + Tab Error Indicators

        public override bool CanSave => _validator.CanSave(new CostCenterCanSaveContext
        {
            IsBusy = IsBusy,
            Name = Name,
            ShortName = ShortName,
            HasChanges = this.HasChanges(),
            HasErrors = _errors.Count > 0
        });

        private static readonly string[] _basicDataFields = [nameof(Name), nameof(ShortName)];
        private static readonly string[] _phoneFields = [nameof(PrimaryPhone), nameof(SecondaryPhone), nameof(PrimaryCellPhone), nameof(SecondaryCellPhone)];

        public bool HasBasicDataErrors => _basicDataFields.Any(f => _errors.ContainsKey(f));
        public string? BasicDataTabTooltip => GetTabTooltip(_basicDataFields);

        public bool HasPhoneErrors => _phoneFields.Any(f => _errors.ContainsKey(f));
        public string? PhoneTabTooltip => GetTabTooltip(_phoneFields);

        protected override void RaiseErrorsChanged(string propertyName)
        {
            base.RaiseErrorsChanged(propertyName);
            if (_basicDataFields.Contains(propertyName))
            {
                NotifyOfPropertyChange(nameof(HasBasicDataErrors));
                NotifyOfPropertyChange(nameof(BasicDataTabTooltip));
            }
            if (_phoneFields.Contains(propertyName))
            {
                NotifyOfPropertyChange(nameof(HasPhoneErrors));
                NotifyOfPropertyChange(nameof(PhoneTabTooltip));
            }
        }

        #endregion

        #region Commands

        private ICommand? _saveCommand;
        public ICommand SaveCommand => _saveCommand ??= new AsyncCommand(SaveAsync);

        private ICommand? _cancelCommand;
        public ICommand CancelCommand => _cancelCommand ??= new AsyncCommand(CancelAsync);

        #endregion

        #region SetForNew / SetForEdit

        public void SetForNew(int parentCompanyLocationId, IEnumerable<CountryGraphQLModel> countries)
        {
            Countries = [.. countries];
            NotifyOfPropertyChange(nameof(Countries));

            Id = 0;
            Name = string.Empty;
            TradeName = string.Empty;
            ShortName = string.Empty;
            Status = CostCentersStatus.Active;
            Address = string.Empty;
            PrimaryPhone = string.Empty;
            SecondaryPhone = string.Empty;
            PrimaryCellPhone = string.Empty;
            SecondaryCellPhone = string.Empty;
            DateControlType = "OPEN_DATE";
            ShowChangeWindowOnCash = false;
            AllowBuy = false;
            AllowSell = false;
            IsTaxable = false; // cascada limpia PriceListIncludeTax + InvoicePriceIncludeTax
            RequiresConfirmationToPrintCopies = false;
            InvoiceCopiesToPrint = 0;
            AllowRepeatItemsOnSales = false;
            TaxToCost = false;
            DefaultInvoiceObservation = string.Empty;
            InvoiceFooter = string.Empty;
            RemissionFooter = string.Empty;
            CompanyLocationId = parentCompanyLocationId;
            FeCreditDefaultAuthorizationSequenceId = null;
            FeCashDefaultAuthorizationSequenceId = null;
            PeDefaultAuthorizationSequenceId = null;
            DsDefaultAuthorizationSequenceId = null;

            int defaultCountryId = Countries.FirstOrDefault(c => c.Code == DefaultCountryCode)?.Id ?? 0;
            (CountryGraphQLModel? country, DepartmentGraphQLModel? department, int cityId) =
                GeographicCascadeHelper.FindDefaults(Countries, defaultCountryId);
            SelectedCountry = country;
            SelectedDepartment = department;
            SelectedCity = department?.Cities?.FirstOrDefault(c => c.Id == cityId) ?? department?.Cities?.FirstOrDefault();

            SeedDefaultValues();
        }

        public void SetForEdit(CostCenterGraphQLModel entity, IEnumerable<CountryGraphQLModel> countries)
        {
            Countries = [.. countries];
            NotifyOfPropertyChange(nameof(Countries));

            Id = entity.Id;
            Name = entity.Name;
            TradeName = entity.TradeName;
            ShortName = entity.ShortName;
            Status = entity.Status;
            Address = entity.Address;
            PrimaryPhone = entity.PrimaryPhone;
            SecondaryPhone = entity.SecondaryPhone;
            PrimaryCellPhone = entity.PrimaryCellPhone;
            SecondaryCellPhone = entity.SecondaryCellPhone;
            DateControlType = entity.DateControlType;
            ShowChangeWindowOnCash = entity.ShowChangeWindowOnCash;
            AllowBuy = entity.AllowBuy;
            AllowSell = entity.AllowSell;
            IsTaxable = entity.IsTaxable;
            PriceListIncludeTax = entity.PriceListIncludeTax;
            InvoicePriceIncludeTax = entity.InvoicePriceIncludeTax;
            RequiresConfirmationToPrintCopies = entity.RequiresConfirmationToPrintCopies;
            InvoiceCopiesToPrint = entity.InvoiceCopiesToPrint;
            AllowRepeatItemsOnSales = entity.AllowRepeatItemsOnSales;
            TaxToCost = entity.TaxToCost;
            DefaultInvoiceObservation = entity.DefaultInvoiceObservation ?? string.Empty;
            InvoiceFooter = entity.InvoiceFooter ?? string.Empty;
            RemissionFooter = entity.RemissionFooter ?? string.Empty;
            CompanyLocationId = entity.CompanyLocation?.Id ?? 0;
            FeCreditDefaultAuthorizationSequenceId = entity.FeCreditDefaultAuthorizationSequence?.Id;
            FeCashDefaultAuthorizationSequenceId = entity.FeCashDefaultAuthorizationSequence?.Id;
            PeDefaultAuthorizationSequenceId = entity.PeDefaultAuthorizationSequence?.Id;
            DsDefaultAuthorizationSequenceId = entity.DsDefaultAuthorizationSequence?.Id;

            SelectedCountry = Countries.FirstOrDefault(c => c.Id == entity.Country?.Id);
            SelectedDepartment = SelectedCountry?.Departments?.FirstOrDefault(d => d.Id == entity.Department?.Id);
            SelectedCity = SelectedDepartment?.Cities?.FirstOrDefault(c => c.Id == entity.City?.Id);

            SeedCurrentValues();
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
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
            this.SeedValue(nameof(CompanyLocationId), CompanyLocationId);
            this.SeedValue(nameof(SelectedCountryId), SelectedCountryId);
            this.SeedValue(nameof(SelectedDepartmentId), SelectedDepartmentId);
            this.SeedValue(nameof(SelectedCityId), SelectedCityId);
            this.SeedValue(nameof(FeCreditDefaultAuthorizationSequenceId), FeCreditDefaultAuthorizationSequenceId);
            this.SeedValue(nameof(FeCashDefaultAuthorizationSequenceId), FeCashDefaultAuthorizationSequenceId);
            this.SeedValue(nameof(PeDefaultAuthorizationSequenceId), PeDefaultAuthorizationSequenceId);
            this.SeedValue(nameof(DsDefaultAuthorizationSequenceId), DsDefaultAuthorizationSequenceId);
            this.AcceptChanges();
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
            this.SeedValue(nameof(FeCreditDefaultAuthorizationSequenceId), FeCreditDefaultAuthorizationSequenceId);
            this.SeedValue(nameof(FeCashDefaultAuthorizationSequenceId), FeCashDefaultAuthorizationSequenceId);
            this.SeedValue(nameof(PeDefaultAuthorizationSequenceId), PeDefaultAuthorizationSequenceId);
            this.SeedValue(nameof(DsDefaultAuthorizationSequenceId), DsDefaultAuthorizationSequenceId);
            this.AcceptChanges();
        }

        #endregion

        #region Lifecycle

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperties();
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<CostCenterGraphQLModel> result = await ExecuteSaveAsync();

                if (!result.Success)
                {
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    ThemedMessageBox.Show(
                        text: $"El guardado no ha sido exitoso\r\n\r\n{result.Errors.ToUserMessage()}\r\n\r\nVerifique los datos y vuelva a intentarlo",
                        title: $"{result.Message}!",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new CostCenterCreateMessage { CreatedCostCenter = result }
                        : new CostCenterUpdateMessage { UpdatedCostCenter = result },
                    CancellationToken.None);

                await TryCloseAsync(true);
            }
            catch (AsyncException ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("AtenciÃ³n!",
                    $"Error al realizar operaciÃ³n.\r\n{ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("AtenciÃ³n!",
                    $"{GetType().Name}.{nameof(SaveAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<UpsertResponseType<CostCenterGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                if (IsNewRecord)
                {
                    (GraphQLQueryFragment _, string query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    return await _costCenterService.CreateAsync<UpsertResponseType<CostCenterGraphQLModel>>(query, variables);
                }
                else
                {
                    (GraphQLQueryFragment _, string query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;
                    return await _costCenterService.UpdateAsync<UpsertResponseType<CostCenterGraphQLModel>>(query, variables);
                }
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        #endregion

        #region Validation

        private void ValidateProperty(string propertyName, string? value)
        {
            CostCenterValidationContext context = BuildValidationContext();
            IReadOnlyList<string> errors = _validator.Validate(propertyName, value, context);
            SetPropertyErrors(propertyName, errors);
        }

        private void ValidateProperties()
        {
            CostCenterValidationContext context = BuildValidationContext();
            Dictionary<string, IReadOnlyList<string>> allErrors = _validator.ValidateAll(context);
            SetPropertyErrors(nameof(Name), allErrors.TryGetValue(nameof(Name), out IReadOnlyList<string>? n) ? n : []);
            SetPropertyErrors(nameof(ShortName), allErrors.TryGetValue(nameof(ShortName), out IReadOnlyList<string>? sn) ? sn : []);
            SetPropertyErrors(nameof(PrimaryPhone), allErrors.TryGetValue(nameof(PrimaryPhone), out IReadOnlyList<string>? pp) ? pp : []);
            SetPropertyErrors(nameof(SecondaryPhone), allErrors.TryGetValue(nameof(SecondaryPhone), out IReadOnlyList<string>? sp) ? sp : []);
            SetPropertyErrors(nameof(PrimaryCellPhone), allErrors.TryGetValue(nameof(PrimaryCellPhone), out IReadOnlyList<string>? pcp) ? pcp : []);
            SetPropertyErrors(nameof(SecondaryCellPhone), allErrors.TryGetValue(nameof(SecondaryCellPhone), out IReadOnlyList<string>? scp) ? scp : []);
        }

        private CostCenterValidationContext BuildValidationContext() => new()
        {
            Name = Name,
            ShortName = ShortName,
            PrimaryPhone = PrimaryPhone,
            SecondaryPhone = SecondaryPhone,
            PrimaryCellPhone = PrimaryCellPhone,
            SecondaryCellPhone = SecondaryCellPhone
        };

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            Dictionary<string, object> fields = BuildEntityFields();
            GraphQLQueryFragment fragment = new("createCostCenter",
                [new("input", "CreateCostCenterInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            Dictionary<string, object> fields = BuildEntityFields();
            GraphQLQueryFragment fragment = new("updateCostCenter",
                [new("data", "UpdateCostCenterInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static Dictionary<string, object> BuildEntityFields()
        {
            return FieldSpec<UpsertResponseType<CostCenterGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "costCenter", nested: sq => sq
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
                    .Select(f => f.Country, country => country
                        .Field(c => c.Id)
                        .Field(c => c.Code)
                        .Field(c => c.Name))
                    .Select(f => f.Department, dept => dept
                        .Field(d => d.Id)
                        .Field(d => d.Code)
                        .Field(d => d.Name))
                    .Select(f => f.City, city => city
                        .Field(c => c.Id)
                        .Field(c => c.Code)
                        .Field(c => c.Name))
                    .Select(f => f.CompanyLocation, loc => loc
                        .Field(l => l.Id)
                        .Select(l => l.Company, company => company
                            .Field(c => c.Id)))
                    .Select(f => f.FeCreditDefaultAuthorizationSequence!, seq => seq
                        .Field(s => s.Id)
                        .Field(s => s.Description))
                    .Select(f => f.FeCashDefaultAuthorizationSequence!, seq => seq
                        .Field(s => s.Id)
                        .Field(s => s.Description))
                    .Select(f => f.PeDefaultAuthorizationSequence!, seq => seq
                        .Field(s => s.Id)
                        .Field(s => s.Description))
                    .Select(f => f.DsDefaultAuthorizationSequence!, seq => seq
                        .Field(s => s.Id)
                        .Field(s => s.Description)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();
        }

        #endregion
    }
}
