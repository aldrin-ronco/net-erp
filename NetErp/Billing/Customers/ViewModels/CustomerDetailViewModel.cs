using AutoMapper;
using Caliburn.Micro;
using Common.Constants;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Dictionaries;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using Models.Books;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Dictionaries.BooksDictionaries;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Billing.Customers.ViewModels
{
    public class CustomerDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<CustomerGraphQLModel> _customerService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IdentificationTypeCache _identificationTypeCache;
        private readonly CountryCache _countryCache;
        private readonly WithholdingTypeCache _withholdingTypeCache;
        private readonly ZoneCache _zoneCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly IMapper _autoMapper;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly IGraphQLClient _graphQLClient;

        #endregion

        #region Commands

        private ICommand? _deleteMailCommand;
        public ICommand DeleteMailCommand
        {
            get
            {
                _deleteMailCommand ??= new RelayCommand(CanRemoveEmail, RemoveEmail);
                return _deleteMailCommand;
            }
        }

        private ICommand? _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                _saveCommand ??= new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        private ICommand? _cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                _cancelCommand ??= new AsyncCommand(CancelAsync);
                return _cancelCommand;
            }
        }

        #endregion

        #region Properties

        private readonly Dictionary<string, List<string>> _errors = [];
        private List<string> _seedEmails = [];

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        public ReadOnlyObservableCollection<ZoneGraphQLModel> Zones
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Zones));
                }
            }
        } = new([]);

        [ExpandoPath("zoneId", SerializeAsId = true)]
        public ZoneGraphQLModel? SelectedZone
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedZone));
                    this.TrackChange(nameof(SelectedZone), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public int SelectedIndexPage
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedIndexPage));
                }
            }
        }

        public Dictionary<char, string> RegimeDictionary => BooksDictionaries.RegimeDictionary;

        [ExpandoPath("accountingEntity.regime")]
        public char SelectedRegime
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedRegime));
                    this.TrackChange(nameof(SelectedRegime));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = 'R';

        [ExpandoPath("accountingEntity.firstName")]
        public string FirstName
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    ValidateProperty(nameof(FirstName), value);
                    NotifyOfPropertyChange(nameof(FirstName));
                    this.TrackChange(nameof(FirstName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("accountingEntity.middleName")]
        public string MiddleName
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(MiddleName));
                    this.TrackChange(nameof(MiddleName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("accountingEntity.firstLastName")]
        public string FirstLastName
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    ValidateProperty(nameof(FirstLastName), value);
                    NotifyOfPropertyChange(nameof(FirstLastName));
                    this.TrackChange(nameof(FirstLastName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("accountingEntity.middleLastName")]
        public string MiddleLastName
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(MiddleLastName));
                    this.TrackChange(nameof(MiddleLastName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("accountingEntity.primaryPhone")]
        public string PrimaryPhone
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    ValidateProperty(nameof(PrimaryPhone), value);
                    NotifyOfPropertyChange(nameof(PrimaryPhone));
                    this.TrackChange(nameof(PrimaryPhone));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("accountingEntity.secondaryPhone")]
        public string SecondaryPhone
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    ValidateProperty(nameof(SecondaryPhone), value);
                    NotifyOfPropertyChange(nameof(SecondaryPhone));
                    this.TrackChange(nameof(SecondaryPhone));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("accountingEntity.primaryCellPhone")]
        public string PrimaryCellPhone
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    ValidateProperty(nameof(PrimaryCellPhone), value);
                    NotifyOfPropertyChange(nameof(PrimaryCellPhone));
                    this.TrackChange(nameof(PrimaryCellPhone));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("accountingEntity.secondaryCellPhone")]
        public string SecondaryCellPhone
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    ValidateProperty(nameof(SecondaryCellPhone), value);
                    NotifyOfPropertyChange(nameof(SecondaryCellPhone));
                    this.TrackChange(nameof(SecondaryCellPhone));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("accountingEntity.address")]
        public string Address
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Address));
                    this.TrackChange(nameof(Address));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("accountingEntity.businessName")]
        public string BusinessName
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    ValidateProperty(nameof(BusinessName), value);
                    NotifyOfPropertyChange(nameof(BusinessName));
                    this.TrackChange(nameof(BusinessName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("accountingEntity.tradeName")]
        public string TradeName
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(TradeName));
                    this.TrackChange(nameof(TradeName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public ObservableCollection<WithholdingTypeDTO> WithholdingTypes
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(WithholdingTypes));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = [];

        public ReadOnlyObservableCollection<IdentificationTypeGraphQLModel> IdentificationTypes
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IdentificationTypes));
                }
            }
        } = new([]);

        [ExpandoPath("accountingEntity.identificationTypeId", SerializeAsId = true)]
        public IdentificationTypeGraphQLModel? SelectedIdentificationType
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedIdentificationType));
                    NotifyOfPropertyChange(nameof(IdentificationNumberMask));
                    this.TrackChange(nameof(SelectedIdentificationType));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperty(nameof(IdentificationNumber), field);
                }
            }
        }

        public ReadOnlyObservableCollection<CountryGraphQLModel>? Countries
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Countries));
                }
            }
        }

        [ExpandoPath("accountingEntity.countryId", SerializeAsId = true)]
        public CountryGraphQLModel? SelectedCountry
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    ValidateProperty(nameof(SelectedCountry), field);
                    NotifyOfPropertyChange(nameof(SelectedCountry));
                    this.TrackChange(nameof(SelectedCountry));
                    if (field != null)
                    {
                        SelectedDepartment = SelectedCountry?.Departments.FirstOrDefault();
                        NotifyOfPropertyChange(nameof(SelectedDepartment));
                    }
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("accountingEntity.departmentId", SerializeAsId = true)]
        public DepartmentGraphQLModel? SelectedDepartment
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    ValidateProperty(nameof(SelectedDepartment), field);
                    NotifyOfPropertyChange(nameof(SelectedDepartment));
                    this.TrackChange(nameof(SelectedDepartment));
                    if (field != null)
                    {
                        if (SelectedDepartment?.Cities.Count > 0)
                        {
                            SelectedCityId = SelectedDepartment.Cities.First().Id;
                            NotifyOfPropertyChange(nameof(SelectedCityId));
                        }
                    }
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("accountingEntity.cityId")]
        public int? SelectedCityId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    ValidateProperty(nameof(SelectedCityId), field);
                    NotifyOfPropertyChange(nameof(SelectedCityId));
                    this.TrackChange(nameof(SelectedCityId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("accountingEntity.identificationNumber")]
        public string IdentificationNumber
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    ValidateProperty(nameof(IdentificationNumber), value);
                    NotifyOfPropertyChange(nameof(IdentificationNumber));
                    this.TrackChange(nameof(IdentificationNumber));
                    NotifyOfPropertyChange(nameof(VerificationDigit));
                    this.TrackChange(nameof(VerificationDigit));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("accountingEntity.verificationDigit")]
        public string? VerificationDigit
        {
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(VerificationDigit));
                }
            }
            get => !IsNewRecord
                    ? field
                    : SelectedIdentificationType == null || !SelectedIdentificationType.HasVerificationDigit
                    ? string.Empty
                    : IdentificationNumber.Trim().Length >= SelectedIdentificationType.MinimumDocumentLength
                    ? IdentificationNumber.GetVerificationDigit()
                    : string.Empty;
        }

        public bool CanRemoveEmail(object p) => true;
        public bool CanAddEmail => !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(EmailDescription) && Email.IsValidEmail();

        public string EmailDescription
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(EmailDescription));
                    NotifyOfPropertyChange(nameof(CanAddEmail));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public string Email
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Email));
                    NotifyOfPropertyChange(nameof(CanAddEmail));
                }
            }
        } = string.Empty;

        [ExpandoPath("accountingEntity.emails")]
        public ObservableCollection<EmailGraphQLModel> Emails
        {
            get;
            set
            {
                if (field != value)
                {
                    if (field != null)
                        field.CollectionChanged -= Emails_CollectionChanged!;

                    field = value;

                    if (field != null)
                        field.CollectionChanged += Emails_CollectionChanged!;

                    NotifyOfPropertyChange(nameof(Emails));
                    this.TrackChange(nameof(Emails));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private void Emails_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.TrackChange(nameof(Emails));
            NotifyOfPropertyChange(nameof(CanSave));
        }

        public EmailGraphQLModel? SelectedEmail
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedEmail));
                }
            }
        }

        public int Id
        {
            get;
            set
            {
                field = value;
                NotifyOfPropertyChange(nameof(Id));
                NotifyOfPropertyChange(nameof(IsNewRecord));
            }
        }

        [ExpandoPath(path: "accountingEntity.captureType")]
        public CaptureTypeEnum SelectedCaptureType
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCaptureType));
                    this.TrackChange(nameof(SelectedCaptureType));
                    NotifyOfPropertyChange(nameof(CaptureInfoAsPN));
                    NotifyOfPropertyChange(nameof(CaptureInfoAsPJ));
                    if (CaptureInfoAsPN)
                    {
                        BusinessName = string.Empty;
                        ClearErrors(nameof(BusinessName));
                        ValidateProperty(nameof(FirstName), FirstName);
                        ValidateProperty(nameof(FirstLastName), FirstLastName);
                    }
                    if (CaptureInfoAsPJ)
                    {
                        FirstName = string.Empty;
                        MiddleName = string.Empty;
                        FirstLastName = string.Empty;
                        MiddleLastName = string.Empty;
                        TradeName = string.Empty;
                        ClearErrors(nameof(FirstName));
                        ClearErrors(nameof(FirstLastName));
                        ValidateProperty(nameof(BusinessName), BusinessName);
                    }
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperties();
                }
            }
        }

        public bool CaptureInfoAsPN => SelectedCaptureType.Equals(CaptureTypeEnum.PN);
        public bool CaptureInfoAsPJ => SelectedCaptureType.Equals(CaptureTypeEnum.PJ);

        public bool IsNewRecord => Id == 0;

        public int CreditTerm
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CreditTerm));
                    this.TrackChange(nameof(CreditTerm));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public new bool IsActive
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsActive));
                    this.TrackChange(nameof(IsActive));

                    if (field)
                    {
                        BlockingReason = string.Empty;
                        ClearErrors(nameof(BlockingReason));
                    }
                    else
                    {
                        ValidateProperty(nameof(BlockingReason), BlockingReason);
                    }

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool IsTaxFree
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsTaxFree));
                    this.TrackChange(nameof(IsTaxFree));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public string? BlockingReason
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(BlockingReason));
                    this.TrackChange(nameof(BlockingReason));

                    if (!IsActive)
                    {
                        ValidateProperty(nameof(BlockingReason), value);
                    }

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool RetainsAnyBasis
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(RetainsAnyBasis));
                    this.TrackChange(nameof(RetainsAnyBasis));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        #endregion

        #region CanSave

        private bool HasEmailChanges()
        {
            if (IsNewRecord) return false;

            var currentEmails = Emails.Select(e => e.Email).ToList();
            if (currentEmails.Count != _seedEmails.Count) return true;

            var seedSet = new HashSet<string>(_seedEmails);
            return !currentEmails.All(email => seedSet.Contains(email));
        }

        public bool CanSave
        {
            get
            {
                if (SelectedIdentificationType == null) return false;
                if (string.IsNullOrEmpty(IdentificationNumber.Trim()) || IdentificationNumber.Length < SelectedIdentificationType.MinimumDocumentLength) return false;
                if (SelectedIdentificationType.HasVerificationDigit && string.IsNullOrEmpty(VerificationDigit)) return false;
                if (CaptureInfoAsPJ && string.IsNullOrEmpty(BusinessName)) return false;
                if (CaptureInfoAsPN && (string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(FirstLastName))) return false;
                if (SelectedCountry == null) return false;
                if (SelectedDepartment == null) return false;
                if (SelectedCityId == 0) return false;
                if (_errors.Count > 0) return false;
                if (!IsNewRecord && !this.HasChanges() && !HasEmailChanges()) return false;
                return true;
            }
        }

        #endregion

        #region MaxLength Properties

        public int BusinessNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.BusinessName));
        public int FirstNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.FirstName));
        public int MiddleNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.MiddleName));
        public int FirstLastNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.FirstLastName));
        public int MiddleLastNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.MiddleLastName));
        public int TradeNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.TradeName));
        public int AddressMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.Address));
        public int PrimaryPhoneMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.PrimaryPhone));
        public int SecondaryPhoneMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.SecondaryPhone));
        public int PrimaryCellPhoneMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.PrimaryCellPhone));
        public int SecondaryCellPhoneMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.SecondaryCellPhone));
        public int IdentificationNumberMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.IdentificationNumber));
        public string IdentificationNumberMask
        {
            get
            {
                int max = IdentificationNumberMaxLength;
                bool allowsLetters = SelectedIdentificationType?.AllowsLetters ?? false;
                return allowsLetters ? $"[a-zA-Z0-9]{{0,{max}}}" : $"[0-9]{{0,{max}}}";
            }
        }

        public int BlockingReasonMaxLength => _stringLengthCache.GetMaxLength<CustomerGraphQLModel>(nameof(CustomerGraphQLModel.BlockingReason));
        public int EmailDescriptionMaxLength => _stringLengthCache.GetMaxLength<EmailGraphQLModel>(nameof(EmailGraphQLModel.Description));
        public int EmailMaxLength => _stringLengthCache.GetMaxLength<EmailGraphQLModel>(nameof(EmailGraphQLModel.Email));

        #endregion

        #region Constructor

        public CustomerDetailViewModel(
            IRepository<CustomerGraphQLModel> customerService,
            IEventAggregator eventAggregator,
            IdentificationTypeCache identificationTypeCache,
            CountryCache countryCache,
            WithholdingTypeCache withholdingTypeCache,
            ZoneCache zoneCache,
            StringLengthCache stringLengthCache,
            IMapper autoMapper,
            JoinableTaskFactory joinableTaskFactory,
            IGraphQLClient graphQLClient)
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _identificationTypeCache = identificationTypeCache ?? throw new ArgumentNullException(nameof(identificationTypeCache));
            _countryCache = countryCache ?? throw new ArgumentNullException(nameof(countryCache));
            _withholdingTypeCache = withholdingTypeCache ?? throw new ArgumentNullException(nameof(withholdingTypeCache));
            _zoneCache = zoneCache ?? throw new ArgumentNullException(nameof(zoneCache));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _autoMapper = autoMapper ?? throw new ArgumentNullException(nameof(autoMapper));
            _joinableTaskFactory = joinableTaskFactory;
            _graphQLClient = graphQLClient;

            Emails = [];
        }

        #endregion

        #region Lifecycle

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            if (!IsNewRecord)
            {
                _seedEmails = Emails.Select(e => e.Email).ToList();
            }
            ValidateProperties();
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion

        #region Methods

        public async Task LoadCachesAsync()
        {
            await CacheBatchLoader.LoadAsync(
                _graphQLClient, default,
                _identificationTypeCache, _countryCache, _withholdingTypeCache, _zoneCache);

            IdentificationTypes = _identificationTypeCache.Items;
            Countries = _countryCache.Items;
            WithholdingTypes = new ObservableCollection<WithholdingTypeDTO>(_autoMapper.Map<ObservableCollection<WithholdingTypeDTO>>(_withholdingTypeCache.Items));
            Zones = _zoneCache.Items;
        }

        public async Task LoadDataForEditAsync(int id)
        {
            var (fragment, query) = _loadByIdQuery.Value;
            var variables = new GraphQLVariables()
                .For(fragment, "id", id)
                .Build();

            var customer = await _customerService.FindByIdAsync(query, variables);
            SetForEdit(customer);
        }

        public void SetForNew()
        {
            Id = 0;
            SelectedRegime = 'R';
            IdentificationNumber = string.Empty;
            VerificationDigit = string.Empty;
            SelectedIdentificationType = IdentificationTypes.First(x => x.Code == Constant.DefaultIdentificationTypeCode);
            SelectedCaptureType = CaptureTypeEnum.PN;
            BusinessName = string.Empty;
            TradeName = string.Empty;
            FirstName = string.Empty;
            MiddleName = string.Empty;
            FirstLastName = string.Empty;
            MiddleLastName = string.Empty;
            PrimaryPhone = string.Empty;
            SecondaryPhone = string.Empty;
            PrimaryCellPhone = string.Empty;
            SecondaryCellPhone = string.Empty;
            Address = string.Empty;
            Emails = [];
            SelectedCountry = Countries?.FirstOrDefault(x => x.Code == Constant.DefaultCountryCode);
            SelectedDepartment = SelectedCountry?.Departments.Find(x => x.Code == Constant.DefaultDepartmentCode);
            if (SelectedDepartment is not null && SelectedDepartment.Cities is not null)
            {
                var city = SelectedDepartment?.Cities?.Find(x => x.Code == Constant.DefaultCityCode);
                if (city is not null) SelectedCityId = city.Id;
            }
            CreditTerm = 0;
            IsActive = true;
            IsTaxFree = false;
            RetainsAnyBasis = false;
            BlockingReason = string.Empty;
            SelectedZone = null;

            List<WithholdingTypeDTO> withholdingTypes = [];
            foreach (WithholdingTypeDTO retention in WithholdingTypes)
            {
                withholdingTypes.Add(new WithholdingTypeDTO()
                {
                    Id = retention.Id,
                    Name = retention.Name,
                    IsSelected = false
                });
            }
            WithholdingTypes = new ObservableCollection<WithholdingTypeDTO>(withholdingTypes);

            SeedDefaultValues();
        }

        public void SetForEdit(CustomerGraphQLModel customer)
        {
            Id = customer.Id;
            CreditTerm = customer.CreditTerm;
            IsTaxFree = customer.IsTaxFree;
            IsActive = customer.IsActive;
            BlockingReason = customer.BlockingReason;
            RetainsAnyBasis = customer.RetainsAnyBasis;

            SelectedRegime = customer.AccountingEntity.Regime;
            SelectedCaptureType = Enum.Parse<CaptureTypeEnum>(customer.AccountingEntity.CaptureType);
            SelectedIdentificationType = IdentificationTypes.First(x => x.Id == customer.AccountingEntity.IdentificationType.Id);
            FirstName = customer.AccountingEntity.FirstName;
            MiddleName = customer.AccountingEntity.MiddleName;
            FirstLastName = customer.AccountingEntity.FirstLastName;
            MiddleLastName = customer.AccountingEntity.MiddleLastName;
            PrimaryPhone = customer.AccountingEntity.PrimaryPhone;
            SecondaryPhone = customer.AccountingEntity.SecondaryPhone;
            PrimaryCellPhone = customer.AccountingEntity.PrimaryCellPhone;
            SecondaryCellPhone = customer.AccountingEntity.SecondaryCellPhone;
            BusinessName = customer.AccountingEntity.BusinessName;
            TradeName = customer.AccountingEntity.TradeName;
            Address = customer.AccountingEntity.Address;
            IdentificationNumber = customer.AccountingEntity.IdentificationNumber;
            VerificationDigit = customer.AccountingEntity.VerificationDigit;

            Emails = customer.AccountingEntity.Emails is null ? [] : new ObservableCollection<EmailGraphQLModel>(customer.AccountingEntity.Emails);

            SelectedCountry = Countries?.FirstOrDefault(c => c.Id == customer.AccountingEntity.Country.Id);
            SelectedDepartment = SelectedCountry?.Departments.Find(d => d.Id == customer.AccountingEntity.Department.Id);
            SelectedCityId = customer.AccountingEntity.City.Id;

            List<WithholdingTypeDTO> withholdingTypes = [];
            foreach (WithholdingTypeDTO retention in WithholdingTypes)
            {
                bool exist = customer.WithholdingTypes is not null && customer.WithholdingTypes.Any(x => x.Id == retention.Id);
                withholdingTypes.Add(new WithholdingTypeDTO()
                {
                    Id = retention.Id,
                    Name = retention.Name,
                    IsSelected = exist
                });
            }
            WithholdingTypes = new ObservableCollection<WithholdingTypeDTO>(withholdingTypes);

            SelectedZone = customer.Zone is null ? null : Zones.FirstOrDefault(z => z.Id == customer.Zone.Id);

            SeedCurrentValues();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(SelectedRegime), SelectedRegime);
            this.SeedValue(nameof(SelectedCaptureType), SelectedCaptureType);
            this.SeedValue(nameof(SelectedIdentificationType), SelectedIdentificationType);
            this.SeedValue(nameof(CreditTerm), CreditTerm);
            this.SeedValue(nameof(IsTaxFree), IsTaxFree);
            this.SeedValue(nameof(RetainsAnyBasis), RetainsAnyBasis);
            this.SeedValue(nameof(IsActive), IsActive);
            this.SeedValue(nameof(SelectedCountry), SelectedCountry);
            this.SeedValue(nameof(SelectedDepartment), SelectedDepartment);
            this.SeedValue(nameof(SelectedCityId), SelectedCityId);
            this.SeedValue(nameof(BusinessName), BusinessName);
            this.SeedValue(nameof(TradeName), TradeName);
            this.SeedValue(nameof(FirstName), FirstName);
            this.SeedValue(nameof(MiddleName), MiddleName);
            this.SeedValue(nameof(FirstLastName), FirstLastName);
            this.SeedValue(nameof(MiddleLastName), MiddleLastName);
            this.SeedValue(nameof(PrimaryPhone), PrimaryPhone);
            this.SeedValue(nameof(SecondaryPhone), SecondaryPhone);
            this.SeedValue(nameof(PrimaryCellPhone), PrimaryCellPhone);
            this.SeedValue(nameof(SecondaryCellPhone), SecondaryCellPhone);
            this.SeedValue(nameof(Address), Address);
            this.SeedValue(nameof(BlockingReason), BlockingReason);
            this.SeedValue(nameof(VerificationDigit), VerificationDigit);
            this.SeedValue(nameof(SelectedZone), SelectedZone);
            this.AcceptChanges();
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(SelectedRegime), SelectedRegime);
            this.SeedValue(nameof(SelectedCaptureType), SelectedCaptureType);
            this.SeedValue(nameof(SelectedIdentificationType), SelectedIdentificationType);
            this.SeedValue(nameof(VerificationDigit), VerificationDigit);
            this.SeedValue(nameof(CreditTerm), CreditTerm);
            this.SeedValue(nameof(IsTaxFree), IsTaxFree);
            this.SeedValue(nameof(RetainsAnyBasis), RetainsAnyBasis);
            this.SeedValue(nameof(IsActive), IsActive);
            this.SeedValue(nameof(SelectedCountry), SelectedCountry);
            this.SeedValue(nameof(SelectedDepartment), SelectedDepartment);
            this.SeedValue(nameof(SelectedCityId), SelectedCityId);
            this.AcceptChanges();
        }

        public void AddEmail()
        {
            EmailGraphQLModel email = new() { Description = EmailDescription, Email = Email };
            Email = string.Empty;
            EmailDescription = string.Empty;
            Emails.Add(email);
        }

        public void RemoveEmail(object p)
        {
            if (SelectedEmail == null) return;
            if (ThemedMessageBox.Show("Confirme ...", $"¿ Confirma que desea eliminar el email : {SelectedEmail.Email} ?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
            EmailGraphQLModel? emailToDelete = Emails.FirstOrDefault(email => email.Id == SelectedEmail.Id);
            if (emailToDelete is null) return;
            Emails.Remove(emailToDelete);
        }

        public void EndRowEditing()
        {
            NotifyOfPropertyChange(nameof(Emails));
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<CustomerGraphQLModel> result = await ExecuteSaveAsync();

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
                        ? new CustomerCreateMessage { CreatedCustomer = result }
                        : new CustomerUpdateMessage { UpdatedCustomer = result },
                    CancellationToken.None);

                await TryCloseAsync(true);
            }
            catch (AsyncException ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"Error al realizar operación.\r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(SaveAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<UpsertResponseType<CustomerGraphQLModel>> ExecuteSaveAsync()
        {
            List<int> withholdingTypes = [];
            if (WithholdingTypes != null)
            {
                foreach (WithholdingTypeDTO withholdingType in WithholdingTypes)
                {
                    if (withholdingType.IsSelected)
                        withholdingTypes.Add(withholdingType.Id);
                }
            }

            var transformers = new Dictionary<string, Func<object?, object?>>
            {
                [nameof(Emails)] = item =>
                {
                    var email = (EmailGraphQLModel)item!;
                    return new
                    {
                        description = email.Description,
                        email = email.Email
                    };
                }
            };

            try
            {
                if (IsNewRecord)
                {
                    var (_, query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput", transformers);
                    return await _customerService.CreateAsync<UpsertResponseType<CustomerGraphQLModel>>(query, variables);
                }
                else
                {
                    var (_, query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData", transformers);
                    variables.updateResponseId = Id;
                    return await _customerService.UpdateAsync<UpsertResponseType<CustomerGraphQLModel>>(query, variables);
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

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadByIdQuery = new(() =>
        {
            var customerFields = FieldSpec<CustomerGraphQLModel>
                .Create()
                .Field(c => c.Id)
                .Field(c => c.CreditTerm)
                .Field(c => c.IsTaxFree)
                .Field(c => c.IsActive)
                .Field(c => c.BlockingReason)
                .Field(c => c.RetainsAnyBasis)
                .Select(c => c.AccountingEntity, entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.IdentificationNumber)
                    .Field(e => e.VerificationDigit)
                    .Field(e => e.CaptureType)
                    .Field(e => e.BusinessName)
                    .Field(e => e.FirstName)
                    .Field(e => e.MiddleName)
                    .Field(e => e.FirstLastName)
                    .Field(e => e.MiddleLastName)
                    .Field(e => e.PrimaryPhone)
                    .Field(e => e.SecondaryPhone)
                    .Field(e => e.PrimaryCellPhone)
                    .Field(e => e.SecondaryCellPhone)
                    .Field(e => e.Address)
                    .Field(e => e.Regime)
                    .Field(e => e.FullName)
                    .Field(e => e.TradeName)
                    .Field(e => e.SearchName)
                    .Field(e => e.TelephonicInformation)
                    .Field(e => e.CommercialCode)
                    .Select(e => e.IdentificationType, it => it
                        .Field(i => i.Id)
                        .Field(i => i.Name)
                        .Field(i => i.Code)
                        .Field(i => i.HasVerificationDigit)
                        .Field(i => i.MinimumDocumentLength))
                    .Select(e => e.Country, co => co
                        .Field(c => c.Id))
                    .Select(e => e.Department, dept => dept
                        .Field(d => d.Id))
                    .Select(e => e.City, city => city
                        .Field(ci => ci.Id))
                    .SelectList(e => e.Emails, email => email
                        .Field(em => em.Id)
                        .Field(em => em.Description)
                        .Field(em => em.Email)))
                .SelectList(c => c.WithholdingTypes, wt => wt
                    .Field(w => w.Id)
                    .Field(w => w.Name))
                .Select(c => c.Zone, zone => zone
                    .Field(z => z.Id))
                .Build();

            var fragment = new GraphQLQueryFragment("customer", [new("id", "ID!")], customerFields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<CustomerGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "customer", nested: sq => sq
                    .Field(c => c.Id)
                    .Field(c => c.CreditTerm)
                    .Field(c => c.IsTaxFree)
                    .Field(c => c.IsActive)
                    .Field(c => c.BlockingReason)
                    .Field(c => c.RetainsAnyBasis)
                    .Select(c => c.AccountingEntity, nested: entity => entity
                        .Field(e => e.Id)
                        .Field(e => e.IdentificationNumber)
                        .Field(e => e.VerificationDigit)
                        .Field(e => e.CaptureType)
                        .Field(e => e.BusinessName)
                        .Field(e => e.FirstName)
                        .Field(e => e.MiddleName)
                        .Field(e => e.FirstLastName)
                        .Field(e => e.MiddleLastName)
                        .Field(e => e.FullName)
                        .Field(e => e.SearchName)
                        .Field(e => e.TelephonicInformation)
                        .Field(e => e.Address)
                        .Field(e => e.TradeName)
                        .SelectList(e => e.Emails, em => em
                            .Field(email => email.Id)
                            .Field(email => email.Description)
                            .Field(email => email.Email))))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createCustomer",
                [new("input", "CreateCustomerInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<CustomerGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "customer", nested: sq => sq
                    .Field(c => c.Id)
                    .Field(c => c.CreditTerm)
                    .Field(c => c.IsTaxFree)
                    .Field(c => c.IsActive)
                    .Field(c => c.BlockingReason)
                    .Field(c => c.RetainsAnyBasis)
                    .Select(c => c.AccountingEntity, nested: entity => entity
                        .Field(e => e.Id)
                        .Field(e => e.IdentificationNumber)
                        .Field(e => e.VerificationDigit)
                        .Field(e => e.CaptureType)
                        .Field(e => e.BusinessName)
                        .Field(e => e.FirstName)
                        .Field(e => e.MiddleName)
                        .Field(e => e.FirstLastName)
                        .Field(e => e.MiddleLastName)
                        .Field(e => e.FullName)
                        .Field(e => e.SearchName)
                        .Field(e => e.TelephonicInformation)
                        .Field(e => e.Address)
                        .Field(e => e.TradeName)
                        .SelectList(e => e.Emails, em => em
                            .Field(email => email.Id)
                            .Field(email => email.Description)
                            .Field(email => email.Email)
                            .Field(email => email.isElectronicInvoiceRecipient)
                            .Field(email => email.IsCorporate))))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("updateCustomer",
                [new("data", "UpdateCustomerInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion

        #region Validation (INotifyDataErrorInfo)

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        // Tab error indicators
        private static readonly string[] _basicDataFields = [nameof(FirstName), nameof(FirstLastName), nameof(BusinessName), nameof(PrimaryPhone), nameof(SecondaryPhone), nameof(PrimaryCellPhone), nameof(SecondaryCellPhone), nameof(SelectedCountry), nameof(SelectedDepartment), nameof(SelectedCityId)];
        private static readonly string[] _otherDataFields = [nameof(BlockingReason)];

        public bool HasBasicDataErrors => _basicDataFields.Any(f => _errors.ContainsKey(f));
        public string? BasicDataTabTooltip => GetTabTooltip(_basicDataFields);

        public bool HasOtherDataErrors => _otherDataFields.Any(f => _errors.ContainsKey(f));
        public string? OtherDataTabTooltip => GetTabTooltip(_otherDataFields);

        private string? GetTabTooltip(string[] fields)
        {
            var errors = fields
                .Where(f => _errors.ContainsKey(f))
                .SelectMany(f => _errors[f])
                .ToList();
            return errors.Count > 0 ? string.Join("\n", errors) : null;
        }

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            if (_basicDataFields.Contains(propertyName))
            {
                NotifyOfPropertyChange(nameof(HasBasicDataErrors));
                NotifyOfPropertyChange(nameof(BasicDataTabTooltip));
            }
            if (_otherDataFields.Contains(propertyName))
            {
                NotifyOfPropertyChange(nameof(HasOtherDataErrors));
                NotifyOfPropertyChange(nameof(OtherDataTabTooltip));
            }
        }

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null!;
            return _errors[propertyName];
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = [];

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

        private void ValidateProperty(string propertyName, string? value)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty.Trim();
            ClearErrors(propertyName);
            if (propertyName.Contains("Phone"))
            {
                value = value.Replace(" ", "").Replace(Convert.ToChar(9).ToString(), "");
                value = value.Replace(Convert.ToChar(44).ToString(), "").Replace(Convert.ToChar(59).ToString(), "");
                value = value.Replace(Convert.ToChar(45).ToString(), "").Replace(Convert.ToChar(95).ToString(), "");
            }
            switch (propertyName)
            {
                case nameof(IdentificationNumber):
                    if (string.IsNullOrEmpty(value) || value.Trim().Length < SelectedIdentificationType?.MinimumDocumentLength) AddError(propertyName, "El número de identificación no puede estar vacío");
                    break;
                case nameof(FirstName):
                    if (string.IsNullOrEmpty(value.Trim()) && CaptureInfoAsPN) AddError(propertyName, "El primer nombre no puede estar vacío");
                    break;
                case nameof(FirstLastName):
                    if (string.IsNullOrEmpty(value.Trim()) && CaptureInfoAsPN) AddError(propertyName, "El primer apellido no puede estar vacío");
                    break;
                case nameof(BusinessName):
                    if (string.IsNullOrEmpty(value.Trim()) && CaptureInfoAsPJ) AddError(propertyName, "La razón social no puede estar vacía");
                    break;
                case nameof(BlockingReason):
                    if (string.IsNullOrEmpty(value.Trim()) && !IsActive) AddError(propertyName, "Debe especificar un motivo de bloqueo");
                    break;
                case nameof(PrimaryPhone):
                    if (value.Length != 7 && !string.IsNullOrEmpty(PrimaryPhone)) AddError(propertyName, "El número de teléfono debe contener 7 digitos");
                    break;
                case nameof(SecondaryPhone):
                    if (value.Length != 7 && !string.IsNullOrEmpty(SecondaryPhone)) AddError(propertyName, "El número de teléfono debe contener 7 digitos");
                    break;
                case nameof(PrimaryCellPhone):
                    if (value.Length != 10 && !string.IsNullOrEmpty(PrimaryCellPhone)) AddError(propertyName, "El número de teléfono celular debe contener 10 digitos");
                    break;
                case nameof(SecondaryCellPhone):
                    if (value.Length != 10 && !string.IsNullOrEmpty(SecondaryCellPhone)) AddError(propertyName, "El número de teléfono celular debe contener 10 digitos");
                    break;
            }
        }

        private void ValidateProperty(string propertyName, object? value)
        {
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(SelectedCountry):
                    if (value == null) AddError(propertyName, "Debe seleccionar un país");
                    break;
                case nameof(SelectedDepartment):
                    if (value == null) AddError(propertyName, "Debe seleccionar un departamento");
                    break;
                case nameof(SelectedCityId):
                    if (value is int cityId && cityId == 0) AddError(propertyName, "Debe seleccionar un municipio");
                    break;
            }
        }

        private void ValidateProperties()
        {
            if (CaptureInfoAsPJ) ValidateProperty(nameof(BusinessName), BusinessName);
            if (CaptureInfoAsPN)
            {
                ValidateProperty(nameof(FirstName), FirstName);
                ValidateProperty(nameof(FirstLastName), FirstLastName);
                ValidateProperty(nameof(IdentificationNumber), IdentificationNumber);
            }
        }

        #endregion

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                if (Emails != null)
                {
                    Emails.CollectionChanged -= Emails_CollectionChanged!;
                }

                IdentificationTypes = null!;
                Countries = null!;
                Zones = null!;
                SelectedIdentificationType = null!;
                SelectedCountry = null!;
                this.AcceptChanges();
                Emails?.Clear();
                WithholdingTypes?.Clear();
            }

            return base.OnDeactivateAsync(close, cancellationToken);
        }
    }
}
