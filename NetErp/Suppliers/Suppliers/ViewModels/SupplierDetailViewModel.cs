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
using Models.Books;
using Models.DTO.Global;
using Models.Global;
using Models.Suppliers;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Suppliers.Suppliers.Validators;
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

namespace NetErp.Suppliers.Suppliers.ViewModels
{
    public class SupplierDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IGraphQLClient _graphQLClient;
        private readonly IdentificationTypeCache _identificationTypeCache;
        private readonly WithholdingTypeCache _withholdingTypeCache;
        private readonly CountryCache _countryCache;
        private readonly IRepository<SupplierGraphQLModel> _supplierService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IMapper _mapper;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly SupplierValidator _validator;

        private readonly Dictionary<string, List<string>> _errors = [];

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

        private ICommand? _cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                _cancelCommand ??= new AsyncCommand(CancelAsync);
                return _cancelCommand;
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

        #endregion

        #region Dialog Size

        public double DialogWidth
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogWidth));
                }
            }
        } = 600;

        public double DialogHeight
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogHeight));
                }
            }
        } = 500;

        #endregion

        #region StringLength Properties

        public int FirstNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.FirstName));
        public int MiddleNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.MiddleName));
        public int FirstLastNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.FirstLastName));
        public int MiddleLastNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.MiddleLastName));
        public int BusinessNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.BusinessName));
        public int TradeNameMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.TradeName));
        public int PrimaryPhoneMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.PrimaryPhone));
        public int SecondaryPhoneMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.SecondaryPhone));
        public int PrimaryCellPhoneMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.PrimaryCellPhone));
        public int SecondaryCellPhoneMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.SecondaryCellPhone));
        public int AddressMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.Address));
        public int IdentificationNumberMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.IdentificationNumber));
        public int CommercialCodeMaxLength => _stringLengthCache.GetMaxLength<AccountingEntityGraphQLModel>(nameof(AccountingEntityGraphQLModel.CommercialCode));

        public string IdentificationNumberMask
        {
            get
            {
                int max = IdentificationNumberMaxLength;
                bool allowsLetters = SelectedIdentificationType?.AllowsLetters ?? false;
                return allowsLetters ? $"[a-zA-Z0-9]{{0,{max}}}" : $"[0-9]{{0,{max}}}";
            }
        }

        #endregion

        #region Properties

        public Dictionary<char, string> RegimeDictionary => BooksDictionaries.RegimeDictionary;

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

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

        [ExpandoPath("accountingEntity.commercialCode")]
        public string CommercialCode
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CommercialCode));
                    this.TrackChange(nameof(CommercialCode));
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

        public decimal IcaWithholdingRate
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    ValidateProperty(nameof(IcaWithholdingRate), value);
                    NotifyOfPropertyChange(nameof(IcaWithholdingRate));
                    this.TrackChange(nameof(IcaWithholdingRate));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public ObservableCollection<WithholdingTypeDTO> WithholdingTypes
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(WithholdingTypes));
                }
            }
        } = [];

        public List<int> WithholdingTypeIds
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(WithholdingTypeIds));
                    this.TrackChange(nameof(WithholdingTypeIds));
                }
            }
        } = [];

        public ReadOnlyObservableCollection<IdentificationTypeGraphQLModel>? IdentificationTypes
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IdentificationTypes));
                    NotifyOfPropertyChange(nameof(CanSave));
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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

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
                    this.TrackChange(nameof(SelectedIdentificationType));
                    NotifyOfPropertyChange(nameof(IdentificationNumberMask));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperty(nameof(IdentificationNumber), IdentificationNumber);
                    if (IsNewRecord)
                    {
                        _ = this.SetFocus(nameof(IdentificationNumber));
                    }
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
                    NotifyOfPropertyChange(nameof(SelectedCountry));
                    this.TrackChange(nameof(SelectedCountry));
                    if (field is not null && field.Departments.Count > 0)
                    {
                        SelectedDepartment = field.Departments.FirstOrDefault(x => x.CountryId == field.Id);
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
                    NotifyOfPropertyChange(nameof(SelectedDepartment));
                    this.TrackChange(nameof(SelectedDepartment));
                    if (field is not null && field.Cities.Count > 0)
                    {
                        SelectedCityId = field.Cities.First().Id;
                        NotifyOfPropertyChange(nameof(SelectedCityId));
                    }
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("accountingEntity.cityId")]
        public int SelectedCityId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCityId));
                    this.TrackChange(nameof(SelectedCityId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public int? IcaAccountingAccountId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IcaAccountingAccountId));
                    this.TrackChange(nameof(IcaAccountingAccountId));
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
                    NotifyOfPropertyChange(nameof(VerificationDigit));
                    this.TrackChange(nameof(IdentificationNumber));
                    this.TrackChange(nameof(VerificationDigit));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        private string _verificationDigit = string.Empty;
        [ExpandoPath("accountingEntity.verificationDigit")]
        public string VerificationDigit
        {
            get => !IsNewRecord
                    ? _verificationDigit
                    : SelectedIdentificationType == null || !SelectedIdentificationType.HasVerificationDigit
                    ? string.Empty
                    : IdentificationNumber.Trim().Length >= SelectedIdentificationType.MinimumDocumentLength
                    ? IdentificationNumber.GetVerificationDigit()
                    : string.Empty;
            set
            {
                if (_verificationDigit != value)
                {
                    _verificationDigit = value;
                    NotifyOfPropertyChange(nameof(VerificationDigit));
                    this.TrackChange(nameof(VerificationDigit));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool WithholdingAppliesOnAnyAmount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(WithholdingAppliesOnAnyAmount));
                    this.TrackChange(nameof(WithholdingAppliesOnAnyAmount));
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

        private ObservableCollection<EmailDTO> _emails = [];

        [ExpandoPath("accountingEntity.emails")]
        public ObservableCollection<EmailDTO> Emails
        {
            get => _emails;
            set
            {
                if (_emails != value)
                {
                    if (_emails != null)
                    {
                        _emails.CollectionChanged -= Emails_CollectionChanged!;
                    }

                    _emails = value;

                    if (_emails != null)
                    {
                        _emails.CollectionChanged += Emails_CollectionChanged!;
                    }

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

        public void CheckBoxClicked(RoutedEventArgs eventArgs)
        {
            WithholdingTypeIds = [.. WithholdingTypes.Where(f => f.IsSelected).Select(s => s.Id)];
            this.TrackChange(nameof(WithholdingTypeIds));
            NotifyOfPropertyChange(nameof(WithholdingTypeIds));
            NotifyOfPropertyChange(nameof(CanSave));
        }

        public EmailDTO? SelectedEmail
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
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Id));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        [ExpandoPath("accountingEntity.captureType")]
        public CaptureTypeEnum SelectedCaptureType
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCaptureType));
                    NotifyOfPropertyChange(nameof(CaptureInfoAsPN));
                    NotifyOfPropertyChange(nameof(CaptureInfoAsPJ));
                    this.TrackChange(nameof(SelectedCaptureType));

                    if (CaptureInfoAsPN)
                    {
                        BusinessName = string.Empty;
                        this.TrackChange(nameof(BusinessName));
                        ValidateProperty(nameof(FirstName), FirstName);
                        ValidateProperty(nameof(FirstLastName), FirstLastName);
                    }
                    if (CaptureInfoAsPJ)
                    {
                        FirstName = string.Empty;
                        FirstLastName = string.Empty;
                        TradeName = string.Empty;
                        MiddleLastName = string.Empty;
                        MiddleName = string.Empty;
                        this.TrackChange(nameof(FirstName));
                        this.TrackChange(nameof(FirstLastName));
                        this.TrackChange(nameof(TradeName));
                        this.TrackChange(nameof(MiddleLastName));
                        this.TrackChange(nameof(MiddleName));
                        ValidateProperty(nameof(BusinessName), BusinessName);
                    }
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperties();
                    if (string.IsNullOrEmpty(IdentificationNumber))
                    {
                        _ = this.SetFocus(nameof(IdentificationNumber));
                    }
                    else
                    {
                        _ = this.SetFocus(CaptureInfoAsPN ? nameof(FirstName) : nameof(BusinessName));
                    }
                }
            }
        }

        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccounts
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountingAccounts));
                }
            }
        } = [];

        public bool CaptureInfoAsPN => SelectedCaptureType.Equals(CaptureTypeEnum.PN);
        public bool CaptureInfoAsPJ => SelectedCaptureType.Equals(CaptureTypeEnum.PJ);

        public bool IsNewRecord => Id == 0;

        // Tab field groupings for error indicators
        private static readonly string[] _basicDataFields = [nameof(IdentificationNumber), nameof(BusinessName), nameof(FirstName), nameof(FirstLastName), nameof(PrimaryPhone), nameof(SecondaryPhone), nameof(PrimaryCellPhone), nameof(SecondaryCellPhone)];
        private static readonly string[] _withholdingFields = [nameof(IcaWithholdingRate)];

        public bool HasBasicDataErrors => _basicDataFields.Any(f => _errors.ContainsKey(f));
        public string? BasicDataTabTooltip => GetTabTooltip(_basicDataFields);

        public bool HasWithholdingErrors => _withholdingFields.Any(f => _errors.ContainsKey(f));
        public string? WithholdingTabTooltip => GetTabTooltip(_withholdingFields);

        private string? GetTabTooltip(string[] fields)
        {
            List<string> errors = [.. fields
                .Where(f => _errors.ContainsKey(f))
                .SelectMany(f => _errors[f])];
            return errors.Count > 0 ? string.Join("\n", errors) : null;
        }

        public bool CanSave => _validator.CanSave(new SupplierCanSaveContext
        {
            IsBusy = IsBusy,
            IdentificationNumber = IdentificationNumber,
            MinimumDocumentLength = SelectedIdentificationType?.MinimumDocumentLength ?? 0,
            HasVerificationDigit = SelectedIdentificationType?.HasVerificationDigit ?? false,
            VerificationDigit = VerificationDigit,
            CaptureInfoAsPN = CaptureInfoAsPN,
            CaptureInfoAsPJ = CaptureInfoAsPJ,
            FirstName = FirstName,
            FirstLastName = FirstLastName,
            BusinessName = BusinessName,
            HasChanges = this.HasChanges(),
            HasErrors = _errors.Count > 0
        });

        #endregion

        #region Methods

        public void EndRowEditing()
        {
            try
            {
                NotifyOfPropertyChange(nameof(Emails));
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(EndRowEditing)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void AddEmail()
        {
            try
            {
                EmailDTO email = new() { Description = EmailDescription, Email = Email };
                Email = string.Empty;
                EmailDescription = string.Empty;
                Emails.Add(email);
                _ = this.SetFocus(nameof(EmailDescription));
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(AddEmail)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void PhoneInputLostFocus(FrameworkElement element)
        {
            switch (element.Name.ToLower())
            {
                case "primaryphone":
                    PrimaryPhone = PrimaryPhone.ToPhoneFormat("### ## ##");
                    break;
                case "secondaryphone":
                    SecondaryPhone = SecondaryPhone.ToPhoneFormat("### ## ##");
                    break;
                case "primarycellphone":
                    PrimaryCellPhone = PrimaryCellPhone.ToPhoneFormat("### ### ## ##");
                    break;
                case "secondarycellphone":
                    SecondaryCellPhone = SecondaryCellPhone.ToPhoneFormat("### ### ## ##");
                    break;
                default:
                    break;
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        public void SetForNew()
        {
            try
            {
                List<WithholdingTypeDTO> retentionList = [];
                Id = 0;
                SelectedRegime = 'R';
                IdentificationNumber = string.Empty;
                VerificationDigit = string.Empty;
                SelectedCaptureType = CaptureTypeEnum.PN;
                BusinessName = string.Empty;
                FirstName = string.Empty;
                MiddleName = string.Empty;
                FirstLastName = string.Empty;
                MiddleLastName = string.Empty;
                PrimaryPhone = string.Empty;
                SecondaryPhone = string.Empty;
                PrimaryCellPhone = string.Empty;
                SecondaryCellPhone = string.Empty;
                Address = string.Empty;
                TradeName = string.Empty;
                CommercialCode = string.Empty;
                IsTaxFree = false;
                IcaWithholdingRate = 0m;
                IcaAccountingAccountId = null;
                WithholdingAppliesOnAnyAmount = false;
                Emails = [];

                SelectedIdentificationType = IdentificationTypes?.FirstOrDefault(x => x.Code == Constant.DefaultIdentificationTypeCode);
                SelectedCountry = Countries?.FirstOrDefault(x => x.Code == Constant.DefaultCountryCode);
                if (SelectedCountry is not null)
                {
                    SelectedDepartment = SelectedCountry.Departments.FirstOrDefault(x => x.Code == Constant.DefaultDepartmentCode);
                }
                if (SelectedDepartment is not null && SelectedDepartment.Cities is not null)
                {
                    SelectedCityId = SelectedDepartment.Cities.FirstOrDefault(x => x.Code == Constant.DefaultCityCode)?.Id ?? 0;
                }

                foreach (WithholdingTypeDTO retention in WithholdingTypes)
                {
                    retentionList.Add(new WithholdingTypeDTO
                    {
                        Id = retention.Id,
                        Name = retention.Name,
                        IsSelected = false
                    });
                }
                WithholdingTypes = [.. retentionList];
                WithholdingTypeIds = [];

                SeedDefaultValues();
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(SelectedCaptureType), SelectedCaptureType);
            this.SeedValue(nameof(SelectedCountry), SelectedCountry);
            this.SeedValue(nameof(SelectedDepartment), SelectedDepartment);
            this.SeedValue(nameof(SelectedIdentificationType), SelectedIdentificationType);
            this.SeedValue(nameof(SelectedCityId), SelectedCityId);
            this.SeedValue(nameof(SelectedRegime), SelectedRegime);
            this.SeedValue(nameof(WithholdingAppliesOnAnyAmount), WithholdingAppliesOnAnyAmount);
            this.SeedValue(nameof(IsTaxFree), IsTaxFree);
            this.SeedValue(nameof(IcaWithholdingRate), IcaWithholdingRate);
            this.SeedValue(nameof(IcaAccountingAccountId), IcaAccountingAccountId);
            this.AcceptChanges();
        }

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<SupplierGraphQLModel> result = await ExecuteSaveAsync();
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
                        ? new SupplierCreateMessage { CreatedSupplier = result }
                        : new SupplierUpdateMessage { UpdatedSupplier = result },
                    CancellationToken.None);
                await TryCloseAsync(true);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(SaveAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<UpsertResponseType<SupplierGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                Dictionary<string, Func<object?, object?>> transformers = new()
                {
                    [nameof(Emails)] = item =>
                    {
                        EmailDTO email = (EmailDTO)item!;
                        return new { description = email.Description, email = email.Email };
                    }
                };

                if (IsNewRecord)
                {
                    (_, string query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput", transformers);
                    return await _supplierService.CreateAsync<UpsertResponseType<SupplierGraphQLModel>>(query, variables);
                }
                else
                {
                    (_, string query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData", transformers);
                    variables.updateResponseId = Id;
                    return await _supplierService.UpdateAsync<UpsertResponseType<SupplierGraphQLModel>>(query, variables);
                }
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public void RemoveEmail(object p)
        {
            try
            {
                if (ThemedMessageBox.Show("Confirme ...", $"¿ Confirma que desea eliminar el email : {SelectedEmail?.Email ?? ""} ?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                if (SelectedEmail != null)
                {
                    EmailDTO? emailToDelete = Emails.FirstOrDefault(email => email.Id == SelectedEmail.Id);
                    if (emailToDelete is null) return;
                    Emails.Remove(emailToDelete);
                }
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(RemoveEmail)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public SupplierDetailViewModel(
            IRepository<SupplierGraphQLModel> supplierService,
            IEventAggregator eventAggregator,
            ObservableCollection<AccountingAccountGraphQLModel> accountingAccounts,
            IdentificationTypeCache identificationTypeCache,
            CountryCache countryCache,
            WithholdingTypeCache withholdingTypeCache,
            StringLengthCache stringLengthCache,
            IMapper mapper,
            IGraphQLClient graphQLClient,
            JoinableTaskFactory joinableTaskFactory,
            SupplierValidator validator)
        {
            _eventAggregator = eventAggregator;
            _mapper = mapper;
            AccountingAccounts = accountingAccounts;
            _supplierService = supplierService;
            _identificationTypeCache = identificationTypeCache;
            _countryCache = countryCache;
            _withholdingTypeCache = withholdingTypeCache;
            _stringLengthCache = stringLengthCache;
            _graphQLClient = graphQLClient;
            _joinableTaskFactory = joinableTaskFactory;
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            Emails = [];
        }

        public async Task InitializeAsync()
        {
            try
            {
                await CacheBatchLoader.LoadAsync(
                    _graphQLClient, default,
                    _identificationTypeCache, _countryCache, _withholdingTypeCache);
                IdentificationTypes = _identificationTypeCache.Items;
                Countries = _countryCache.Items;
                WithholdingTypes = _mapper.Map<ObservableCollection<WithholdingTypeDTO>>(_withholdingTypeCache.Items);
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperties();
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        public async Task LoadDataForEditAsync(int id)
        {
            try
            {
                (GraphQLQueryFragment fragment, string query) = _loadByIdQuery.Value;
                object variables = new GraphQLVariables()
                    .For(fragment, "id", id)
                    .Build();

                SupplierGraphQLModel supplier = await _supplierService.FindByIdAsync(query, variables);
                SetForEdit(supplier);
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public void SetForEdit(SupplierGraphQLModel supplier)
        {
            Id = supplier.Id;
            VerificationDigit = supplier.AccountingEntity.VerificationDigit;
            SelectedCaptureType = (CaptureTypeEnum)Enum.Parse(typeof(CaptureTypeEnum), supplier.AccountingEntity.CaptureType);
            BusinessName = supplier.AccountingEntity.BusinessName ?? string.Empty;
            SelectedIdentificationType = IdentificationTypes?.FirstOrDefault(x => x.Code == supplier.AccountingEntity.IdentificationType.Code);
            IdentificationNumber = supplier.AccountingEntity.IdentificationNumber ?? string.Empty;
            FirstName = supplier.AccountingEntity.FirstName ?? string.Empty;
            MiddleName = supplier.AccountingEntity.MiddleName ?? string.Empty;
            FirstLastName = supplier.AccountingEntity.FirstLastName ?? string.Empty;
            MiddleLastName = supplier.AccountingEntity.MiddleLastName ?? string.Empty;
            PrimaryPhone = supplier.AccountingEntity.PrimaryPhone ?? string.Empty;
            SecondaryPhone = supplier.AccountingEntity.SecondaryPhone ?? string.Empty;
            PrimaryCellPhone = supplier.AccountingEntity.PrimaryCellPhone ?? string.Empty;
            SecondaryCellPhone = supplier.AccountingEntity.SecondaryCellPhone ?? string.Empty;
            Emails = supplier.AccountingEntity.Emails is null ? [] : _mapper.Map<ObservableCollection<EmailDTO>>(supplier.AccountingEntity.Emails);
            SelectedCountry = Countries?.FirstOrDefault(c => c.Id == supplier.AccountingEntity.Country.Id);
            SelectedDepartment = SelectedCountry?.Departments.FirstOrDefault(d => d.Id == supplier.AccountingEntity.Department.Id);
            SelectedCityId = supplier.AccountingEntity.City.Id;
            Address = supplier.AccountingEntity.Address ?? string.Empty;
            IsTaxFree = supplier.IsTaxFree;
            IcaWithholdingRate = supplier.IcaWithholdingRate;
            IcaAccountingAccountId = supplier.IcaAccountingAccount?.Id;
            TradeName = supplier.AccountingEntity.TradeName ?? string.Empty;
            WithholdingAppliesOnAnyAmount = supplier.WithholdingAppliesOnAnyAmount;

            List<WithholdingTypeDTO> withholdingTypes = [];
            foreach (WithholdingTypeDTO withholdingType in WithholdingTypes)
            {
                bool exist = supplier.WithholdingTypes is not null && supplier.WithholdingTypes.Any(x => x.Id == withholdingType.Id);
                withholdingTypes.Add(new WithholdingTypeDTO
                {
                    Id = withholdingType.Id,
                    Name = withholdingType.Name,
                    IsSelected = exist
                });
            }
            WithholdingTypes = [.. withholdingTypes];
            WithholdingTypeIds = [.. WithholdingTypes.Where(f => f.IsSelected).Select(s => s.Id)];

            SeedCurrentValues();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(SelectedIdentificationType), SelectedIdentificationType);
            this.SeedValue(nameof(IdentificationNumber), IdentificationNumber);
            this.SeedValue(nameof(SelectedCaptureType), SelectedCaptureType);
            this.SeedValue(nameof(BusinessName), BusinessName);
            this.SeedValue(nameof(FirstName), FirstName);
            this.SeedValue(nameof(MiddleName), MiddleName);
            this.SeedValue(nameof(FirstLastName), FirstLastName);
            this.SeedValue(nameof(MiddleLastName), MiddleLastName);
            this.SeedValue(nameof(TradeName), TradeName);
            this.SeedValue(nameof(CommercialCode), CommercialCode);
            this.SeedValue(nameof(PrimaryPhone), PrimaryPhone);
            this.SeedValue(nameof(SecondaryPhone), SecondaryPhone);
            this.SeedValue(nameof(PrimaryCellPhone), PrimaryCellPhone);
            this.SeedValue(nameof(SecondaryCellPhone), SecondaryCellPhone);
            this.SeedValue(nameof(Address), Address);
            this.SeedValue(nameof(SelectedCountry), SelectedCountry);
            this.SeedValue(nameof(SelectedDepartment), SelectedDepartment);
            this.SeedValue(nameof(SelectedCityId), SelectedCityId);
            this.SeedValue(nameof(SelectedRegime), SelectedRegime);
            this.SeedValue(nameof(IsTaxFree), IsTaxFree);
            this.SeedValue(nameof(IcaWithholdingRate), IcaWithholdingRate);
            this.SeedValue(nameof(IcaAccountingAccountId), IcaAccountingAccountId);
            this.SeedValue(nameof(WithholdingAppliesOnAnyAmount), WithholdingAppliesOnAnyAmount);
            this.SeedValue(nameof(WithholdingTypeIds), WithholdingTypeIds);
            this.AcceptChanges();
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<UpsertResponseType<SupplierGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "supplier", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.IsTaxFree))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            GraphQLQueryFragment fragment = new("createSupplier",
                [new("input", "CreateSupplierInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<UpsertResponseType<SupplierGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "supplier", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.IsTaxFree))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            GraphQLQueryFragment fragment = new("updateSupplier",
                [new("data", "UpdateSupplierInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadByIdQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<SupplierGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.IsTaxFree)
                .Field(e => e.IcaWithholdingRate)
                .Field(e => e.WithholdingAppliesOnAnyAmount)
                .Select(e => e.AccountingEntity, acc => acc
                    .Field(c => c!.Id)
                    .Field(c => c!.VerificationDigit)
                    .Field(c => c!.IdentificationNumber)
                    .Field(c => c!.FirstName)
                    .Field(c => c!.MiddleName)
                    .Field(c => c!.FirstLastName)
                    .Field(c => c!.MiddleLastName)
                    .Field(c => c!.SearchName)
                    .Field(c => c!.TradeName)
                    .Field(c => c!.BusinessName)
                    .Field(c => c!.PrimaryPhone)
                    .Field(c => c!.SecondaryPhone)
                    .Field(c => c!.PrimaryCellPhone)
                    .Field(c => c!.SecondaryCellPhone)
                    .Field(c => c!.Address)
                    .Field(c => c!.CaptureType)
                    .Field(c => c!.TelephonicInformation)
                    .Select(e => e!.IdentificationType, co => co
                        .Field(x => x.Id)
                        .Field(x => x.Code))
                    .Select(e => e!.Country, co => co
                        .Field(x => x.Id)
                        .Field(x => x.Code))
                    .Select(e => e!.City, co => co
                        .Field(x => x.Id)
                        .Field(x => x.Code))
                    .Select(e => e!.Department, co => co
                        .Field(x => x.Id)
                        .Field(x => x.Code))
                    .SelectList(e => e!.Emails, co => co
                        .Field(x => x.Id)
                        .Field(x => x.Description)
                        .Field(x => x.Email)
                        .Field(x => x.IsElectronicInvoiceRecipient)))
                .Select(e => e.IcaAccountingAccount, acc => acc
                    .Field(c => c!.Id)
                    .Field(c => c!.Name)
                    .Field(c => c!.Code))
                .SelectList(e => e.WithholdingTypes!, acc => acc
                    .Field(c => c.Id)
                    .Field(c => c.Name))
                .Build();

            GraphQLQueryFragment fragment = new("supplier",
                [new("id", "ID!")],
                fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion

        #region Validation

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            if (_basicDataFields.Contains(propertyName))
            {
                NotifyOfPropertyChange(nameof(BasicDataTabTooltip));
                NotifyOfPropertyChange(nameof(HasBasicDataErrors));
            }
            if (_withholdingFields.Contains(propertyName))
            {
                NotifyOfPropertyChange(nameof(WithholdingTabTooltip));
                NotifyOfPropertyChange(nameof(HasWithholdingErrors));
            }
        }

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.TryGetValue(propertyName, out List<string>? value)) return Enumerable.Empty<string>();
            return value;
        }

        private void ValidateProperty(string propertyName, string value)
        {
            SupplierValidationContext context = new()
            {
                CaptureInfoAsPN = CaptureInfoAsPN,
                CaptureInfoAsPJ = CaptureInfoAsPJ,
                MinimumDocumentLength = SelectedIdentificationType?.MinimumDocumentLength ?? 0,
                FirstName = FirstName,
                FirstLastName = FirstLastName,
                BusinessName = BusinessName,
                IdentificationNumber = IdentificationNumber,
                PrimaryPhone = PrimaryPhone,
                SecondaryPhone = SecondaryPhone,
                PrimaryCellPhone = PrimaryCellPhone,
                SecondaryCellPhone = SecondaryCellPhone,
                IcaWithholdingRate = IcaWithholdingRate
            };
            IReadOnlyList<string> errors = _validator.Validate(propertyName, value, context);
            SetPropertyErrors(propertyName, errors);
        }

        private void ValidateProperty(string propertyName, decimal value)
        {
            SupplierValidationContext context = new()
            {
                CaptureInfoAsPN = CaptureInfoAsPN,
                CaptureInfoAsPJ = CaptureInfoAsPJ,
                MinimumDocumentLength = SelectedIdentificationType?.MinimumDocumentLength ?? 0,
                FirstName = FirstName,
                FirstLastName = FirstLastName,
                BusinessName = BusinessName,
                IdentificationNumber = IdentificationNumber,
                IcaWithholdingRate = value
            };
            IReadOnlyList<string> errors = _validator.Validate(propertyName, value, context);
            SetPropertyErrors(propertyName, errors);
        }

        private void SetPropertyErrors(string propertyName, IReadOnlyList<string> errors)
        {
            bool hadErrors = _errors.ContainsKey(propertyName);

            if (errors.Count > 0)
                _errors[propertyName] = [.. errors];
            else if (hadErrors)
                _errors.Remove(propertyName);

            if (hadErrors || errors.Count > 0)
                RaiseErrorsChanged(propertyName);
        }

        private void ValidateProperties()
        {
            SupplierValidationContext context = new()
            {
                CaptureInfoAsPN = CaptureInfoAsPN,
                CaptureInfoAsPJ = CaptureInfoAsPJ,
                MinimumDocumentLength = SelectedIdentificationType?.MinimumDocumentLength ?? 0,
                FirstName = FirstName,
                FirstLastName = FirstLastName,
                BusinessName = BusinessName,
                IdentificationNumber = IdentificationNumber,
                PrimaryPhone = PrimaryPhone,
                SecondaryPhone = SecondaryPhone,
                PrimaryCellPhone = PrimaryCellPhone,
                SecondaryCellPhone = SecondaryCellPhone,
                IcaWithholdingRate = IcaWithholdingRate
            };

            Dictionary<string, IReadOnlyList<string>> allErrors = _validator.ValidateAll(context);

            SetPropertyErrors(nameof(IdentificationNumber), allErrors.TryGetValue(nameof(IdentificationNumber), out IReadOnlyList<string>? idErrors) ? idErrors : []);
            SetPropertyErrors(nameof(FirstName), allErrors.TryGetValue(nameof(FirstName), out IReadOnlyList<string>? fnErrors) ? fnErrors : []);
            SetPropertyErrors(nameof(FirstLastName), allErrors.TryGetValue(nameof(FirstLastName), out IReadOnlyList<string>? flnErrors) ? flnErrors : []);
            SetPropertyErrors(nameof(BusinessName), allErrors.TryGetValue(nameof(BusinessName), out IReadOnlyList<string>? bnErrors) ? bnErrors : []);
            SetPropertyErrors(nameof(PrimaryPhone), allErrors.TryGetValue(nameof(PrimaryPhone), out IReadOnlyList<string>? ppErrors) ? ppErrors : []);
            SetPropertyErrors(nameof(SecondaryPhone), allErrors.TryGetValue(nameof(SecondaryPhone), out IReadOnlyList<string>? spErrors) ? spErrors : []);
            SetPropertyErrors(nameof(PrimaryCellPhone), allErrors.TryGetValue(nameof(PrimaryCellPhone), out IReadOnlyList<string>? pcpErrors) ? pcpErrors : []);
            SetPropertyErrors(nameof(SecondaryCellPhone), allErrors.TryGetValue(nameof(SecondaryCellPhone), out IReadOnlyList<string>? scpErrors) ? scpErrors : []);
            SetPropertyErrors(nameof(IcaWithholdingRate), allErrors.TryGetValue(nameof(IcaWithholdingRate), out IReadOnlyList<string>? icaErrors) ? icaErrors : []);
        }

        #endregion

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                if (_emails != null)
                {
                    _emails.CollectionChanged -= Emails_CollectionChanged!;
                }
                IdentificationTypes = null;
                Countries = null;
                SelectedIdentificationType = null;
                SelectedCountry = null;
                Emails?.Clear();
                WithholdingTypes?.Clear();
            }

            return base.OnDeactivateAsync(close, cancellationToken);
        }
    }
}
