using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Books.AccountingEntities.Validators;
using NetErp.Books.AccountingEntities.ViewModels;
using NetErp.Helpers.Cache;
using NSubstitute;
using Xunit;
using static Dictionaries.BooksDictionaries;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.ViewModels;

public class AccountingEntityDetailViewModelTests
{
    private readonly IRepository<AccountingEntityGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly IdentificationTypeCache _identificationTypeCache;
    private readonly CountryCache _countryCache;
    private readonly StringLengthCache _stringLengthCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly AccountingEntityValidator _validator;
    private readonly AccountingEntityDetailViewModel _vm;

    public AccountingEntityDetailViewModelTests()
    {
        _service = Substitute.For<IRepository<AccountingEntityGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();

        IRepository<IdentificationTypeGraphQLModel> idTypeRepo = Substitute.For<IRepository<IdentificationTypeGraphQLModel>>();
        _identificationTypeCache = new IdentificationTypeCache(idTypeRepo, _eventAggregator);

        IRepository<CountryGraphQLModel> countryRepo = Substitute.For<IRepository<CountryGraphQLModel>>();
        _countryCache = new CountryCache(countryRepo, _eventAggregator);

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;

        _validator = new AccountingEntityValidator();

        _vm = new AccountingEntityDetailViewModel(
            _service,
            _eventAggregator,
            _identificationTypeCache,
            _countryCache,
            _stringLengthCache,
            _joinableTaskFactory,
            _validator);
    }

    /// <summary>
    /// Prepara el ViewModel con los datos maestros mínimos para que SetForNew
    /// funcione: tipo de identificación con code "31" (NIT) y país con code "169"
    /// (Colombia) con departamento "08" y ciudad "001".
    /// </summary>
    private void PrepareForNew()
    {
        IdentificationTypeGraphQLModel defaultIdType = new()
        {
            Id = 1,
            Code = "31",
            Name = "NIT",
            MinimumDocumentLength = 5,
            HasVerificationDigit = true,
            AllowsLetters = false
        };
        _identificationTypeCache.Add(defaultIdType);

        CountryGraphQLModel defaultCountry = new()
        {
            Id = 1,
            Code = "169",
            Name = "Colombia",
            Departments =
            [
                new DepartmentGraphQLModel
                {
                    Id = 1, Code = "08", Name = "Atlántico", CountryId = 1,
                    Cities = [new CityGraphQLModel { Id = 1, Code = "001", Name = "Barranquilla" }]
                }
            ]
        };
        _countryCache.Add(defaultCountry);

        _vm.IdentificationTypes = _identificationTypeCache.Items;
        _vm.Countries = _countryCache.Items;
    }

    private AccountingEntityGraphQLModel CreateSampleEntity() => new()
    {
        Id = 10,
        Regime = 'R',
        CaptureType = "PN",
        IdentificationType = new IdentificationTypeGraphQLModel
        {
            Id = 1,
            Code = "31",
            Name = "NIT",
            MinimumDocumentLength = 5,
            HasVerificationDigit = true
        },
        FirstName = "Carlos",
        MiddleName = "",
        FirstLastName = "Medrano",
        MiddleLastName = "",
        BusinessName = "",
        TradeName = "",
        PrimaryPhone = "1234567",
        SecondaryPhone = "",
        PrimaryCellPhone = "3001234567",
        SecondaryCellPhone = "",
        Address = "Calle 1 # 2-3",
        IdentificationNumber = "12345678",
        VerificationDigit = "5",
        Country = new CountryGraphQLModel { Id = 1 },
        Department = new DepartmentGraphQLModel { Id = 1 },
        City = new CityGraphQLModel { Id = 1 },
        Emails = []
    };

    #region Construction

    [Fact]
    public void Constructor_NullService_Throws()
    {
        System.Action act = () => new AccountingEntityDetailViewModel(
            null!, _eventAggregator, _identificationTypeCache, _countryCache,
            _stringLengthCache, _joinableTaskFactory, _validator);

        act.Should().Throw<ArgumentNullException>().WithParameterName("accountingEntityService");
    }

    [Fact]
    public void Constructor_NullEventAggregator_Throws()
    {
        System.Action act = () => new AccountingEntityDetailViewModel(
            _service, null!, _identificationTypeCache, _countryCache,
            _stringLengthCache, _joinableTaskFactory, _validator);

        act.Should().Throw<ArgumentNullException>().WithParameterName("eventAggregator");
    }

    [Fact]
    public void Constructor_NullIdentificationTypeCache_Throws()
    {
        System.Action act = () => new AccountingEntityDetailViewModel(
            _service, _eventAggregator, null!, _countryCache,
            _stringLengthCache, _joinableTaskFactory, _validator);

        act.Should().Throw<ArgumentNullException>().WithParameterName("identificationTypeCache");
    }

    [Fact]
    public void Constructor_NullCountryCache_Throws()
    {
        System.Action act = () => new AccountingEntityDetailViewModel(
            _service, _eventAggregator, _identificationTypeCache, null!,
            _stringLengthCache, _joinableTaskFactory, _validator);

        act.Should().Throw<ArgumentNullException>().WithParameterName("countryCache");
    }

    [Fact]
    public void Constructor_NullStringLengthCache_Throws()
    {
        System.Action act = () => new AccountingEntityDetailViewModel(
            _service, _eventAggregator, _identificationTypeCache, _countryCache,
            null!, _joinableTaskFactory, _validator);

        act.Should().Throw<ArgumentNullException>().WithParameterName("stringLengthCache");
    }

    [Fact]
    public void Constructor_NullValidator_Throws()
    {
        System.Action act = () => new AccountingEntityDetailViewModel(
            _service, _eventAggregator, _identificationTypeCache, _countryCache,
            _stringLengthCache, _joinableTaskFactory, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("validator");
    }

    [Fact]
    public void Constructor_InitializesEmailsCollection()
    {
        _vm.Emails.Should().NotBeNull();
        _vm.Emails.Should().BeEmpty();
    }

    #endregion

    #region SetForNew

    [Fact]
    public void SetForNew_SetsIdentityDefaults()
    {
        PrepareForNew();

        _vm.SetForNew();

        _vm.Id.Should().Be(0);
        _vm.IsNewRecord.Should().BeTrue();
        _vm.SelectedRegime.Should().Be('R');
        _vm.SelectedCaptureType.Should().Be(CaptureTypeEnum.PN);
        _vm.CaptureInfoAsPN.Should().BeTrue();
        _vm.CaptureInfoAsPJ.Should().BeFalse();
    }

    [Fact]
    public void SetForNew_ClearsAllTextFields()
    {
        PrepareForNew();

        _vm.SetForNew();

        _vm.IdentificationNumber.Should().BeEmpty();
        _vm.BusinessName.Should().BeEmpty();
        _vm.TradeName.Should().BeEmpty();
        _vm.FirstName.Should().BeEmpty();
        _vm.MiddleName.Should().BeEmpty();
        _vm.FirstLastName.Should().BeEmpty();
        _vm.MiddleLastName.Should().BeEmpty();
        _vm.PrimaryPhone.Should().BeEmpty();
        _vm.SecondaryPhone.Should().BeEmpty();
        _vm.PrimaryCellPhone.Should().BeEmpty();
        _vm.SecondaryCellPhone.Should().BeEmpty();
        _vm.Address.Should().BeEmpty();
    }

    [Fact]
    public void SetForNew_SelectsDefaultIdentificationType()
    {
        PrepareForNew();

        _vm.SetForNew();

        _vm.SelectedIdentificationType.Should().NotBeNull();
        _vm.SelectedIdentificationType.Code.Should().Be("31");
    }

    [Fact]
    public void SetForNew_SelectsDefaultGeography()
    {
        PrepareForNew();

        _vm.SetForNew();

        _vm.SelectedCountry.Should().NotBeNull();
        _vm.SelectedCountry.Code.Should().Be("169");
        _vm.SelectedDepartment.Should().NotBeNull();
        _vm.SelectedDepartment.Code.Should().Be("08");
        _vm.SelectedCityId.Should().Be(1);
    }

    [Fact]
    public void SetForNew_InitializesEmptyEmailsCollection()
    {
        PrepareForNew();

        _vm.SetForNew();

        _vm.Emails.Should().NotBeNull();
        _vm.Emails.Should().BeEmpty();
    }

    [Fact]
    public void SetForNew_NoInitialChanges_CanSaveFalse()
    {
        PrepareForNew();

        _vm.SetForNew();

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void SetForNew_MissingDefaultIdentificationType_ThrowsAsyncException()
    {
        // Cache vacío → no se encuentra code "31"
        _vm.IdentificationTypes = _identificationTypeCache.Items;
        _vm.Countries = _countryCache.Items;

        System.Action act = () => _vm.SetForNew();

        act.Should().Throw<AsyncException>();
    }

    #endregion

    #region PopulateFromAccountingEntity

    [Fact]
    public void PopulateFromAccountingEntity_CopiesAllScalarFields()
    {
        PrepareForNew();
        AccountingEntityGraphQLModel entity = CreateSampleEntity();

        _vm.PopulateFromAccountingEntity(entity);

        _vm.Id.Should().Be(10);
        _vm.IsNewRecord.Should().BeFalse();
        _vm.SelectedRegime.Should().Be('R');
        _vm.SelectedCaptureType.Should().Be(CaptureTypeEnum.PN);
        _vm.IdentificationNumber.Should().Be("12345678");
        _vm.FirstName.Should().Be("Carlos");
        _vm.FirstLastName.Should().Be("Medrano");
        _vm.PrimaryPhone.Should().Be("1234567");
        _vm.PrimaryCellPhone.Should().Be("3001234567");
        _vm.Address.Should().Be("Calle 1 # 2-3");
    }

    [Fact]
    public void PopulateFromAccountingEntity_ResolvesGeographyFromCaches()
    {
        PrepareForNew();
        AccountingEntityGraphQLModel entity = CreateSampleEntity();

        _vm.PopulateFromAccountingEntity(entity);

        _vm.SelectedCountry.Should().NotBeNull();
        _vm.SelectedCountry.Id.Should().Be(1);
        _vm.SelectedDepartment.Should().NotBeNull();
        _vm.SelectedDepartment.Id.Should().Be(1);
        _vm.SelectedCityId.Should().Be(1);
    }

    [Fact]
    public void PopulateFromAccountingEntity_NullEmails_InitializesEmpty()
    {
        PrepareForNew();
        AccountingEntityGraphQLModel entity = CreateSampleEntity();
        entity.Emails = null!;

        _vm.PopulateFromAccountingEntity(entity);

        _vm.Emails.Should().NotBeNull();
        _vm.Emails.Should().BeEmpty();
    }

    [Fact]
    public void PopulateFromAccountingEntity_WithEmails_CopiesCollection()
    {
        PrepareForNew();
        AccountingEntityGraphQLModel entity = CreateSampleEntity();
        entity.Emails =
        [
            new EmailGraphQLModel { Id = 1, Description = "Principal", Email = "carlos@test.com" },
            new EmailGraphQLModel { Id = 2, Description = "Contacto", Email = "contacto@test.com" }
        ];

        _vm.PopulateFromAccountingEntity(entity);

        _vm.Emails.Should().HaveCount(2);
        _vm.Emails[0].Email.Should().Be("carlos@test.com");
        _vm.Emails[1].Email.Should().Be("contacto@test.com");
    }

    [Fact]
    public void PopulateFromAccountingEntity_NoInitialChanges_CanSaveFalse()
    {
        PrepareForNew();
        AccountingEntityGraphQLModel entity = CreateSampleEntity();

        _vm.PopulateFromAccountingEntity(entity);

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AfterChangingFirstName_ReturnsTrue()
    {
        PrepareForNew();
        _vm.PopulateFromAccountingEntity(CreateSampleEntity());

        _vm.FirstName = "Carlos Modificado";

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_EmptyFirstName_PN_ReturnsFalse()
    {
        PrepareForNew();
        _vm.PopulateFromAccountingEntity(CreateSampleEntity());

        _vm.FirstName = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyFirstLastName_PN_ReturnsFalse()
    {
        PrepareForNew();
        _vm.PopulateFromAccountingEntity(CreateSampleEntity());

        _vm.FirstLastName = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyBusinessName_PJ_ReturnsFalse()
    {
        PrepareForNew();
        _vm.SetForNew();
        _vm.SelectedCaptureType = CaptureTypeEnum.PJ;
        _vm.IdentificationNumber = "900123456";

        _vm.BusinessName = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyIdentificationNumber_ReturnsFalse()
    {
        PrepareForNew();
        _vm.PopulateFromAccountingEntity(CreateSampleEntity());

        _vm.IdentificationNumber = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_IdentificationNumberShorterThanMinimum_ReturnsFalse()
    {
        PrepareForNew();
        _vm.PopulateFromAccountingEntity(CreateSampleEntity());

        _vm.IdentificationNumber = "123";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_IsBusy_ReturnsFalse()
    {
        PrepareForNew();
        _vm.PopulateFromAccountingEntity(CreateSampleEntity());
        _vm.FirstName = "Modified";

        _vm.IsBusy = true;

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_NewRecordWithoutChanges_ReturnsFalse()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_NewRecordWithValidData_ReturnsTrue()
    {
        PrepareForNew();
        _vm.SetForNew();
        _vm.IdentificationNumber = "12345678";
        _vm.FirstName = "Juan";
        _vm.FirstLastName = "Pérez";

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_EditRecordWithoutChanges_ReturnsFalse()
    {
        PrepareForNew();
        _vm.PopulateFromAccountingEntity(CreateSampleEntity());

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region Validation

    [Fact]
    public void FirstName_SetEmpty_PN_AddsValidationError()
    {
        PrepareForNew();
        _vm.SetForNew();
        _vm.FirstName = "Valid";

        _vm.FirstName = "";

        _vm.HasErrors.Should().BeTrue();
        _vm.GetErrors(nameof(AccountingEntityDetailViewModel.FirstName)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void FirstName_SetValid_ClearsError()
    {
        PrepareForNew();
        _vm.SetForNew();
        _vm.FirstName = "";

        _vm.FirstName = "Carlos";

        _vm.GetErrors(nameof(AccountingEntityDetailViewModel.FirstName)).Cast<string>()
            .Should().BeEmpty();
    }

    [Fact]
    public void FirstLastName_SetEmpty_PN_AddsValidationError()
    {
        PrepareForNew();
        _vm.SetForNew();
        _vm.FirstLastName = "Valid";

        _vm.FirstLastName = "";

        _vm.GetErrors(nameof(AccountingEntityDetailViewModel.FirstLastName)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void BusinessName_SetEmpty_PJ_AddsValidationError()
    {
        PrepareForNew();
        _vm.SetForNew();
        _vm.SelectedCaptureType = CaptureTypeEnum.PJ;
        _vm.BusinessName = "Valid Business";

        _vm.BusinessName = "";

        _vm.GetErrors(nameof(AccountingEntityDetailViewModel.BusinessName)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void IdentificationNumber_TooShort_AddsError()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.IdentificationNumber = "12";

        _vm.GetErrors(nameof(AccountingEntityDetailViewModel.IdentificationNumber)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void IdentificationNumber_AtMinimumLength_NoError()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.IdentificationNumber = "12345";

        _vm.GetErrors(nameof(AccountingEntityDetailViewModel.IdentificationNumber)).Cast<string>()
            .Should().BeEmpty();
    }

    [Fact]
    public void PrimaryPhone_InvalidLength_AddsError()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.PrimaryPhone = "12345";

        _vm.GetErrors(nameof(AccountingEntityDetailViewModel.PrimaryPhone)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void PrimaryPhone_Valid7Digits_NoError()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.PrimaryPhone = "1234567";

        _vm.GetErrors(nameof(AccountingEntityDetailViewModel.PrimaryPhone)).Cast<string>()
            .Should().BeEmpty();
    }

    [Fact]
    public void PrimaryCellPhone_InvalidLength_AddsError()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.PrimaryCellPhone = "12345";

        _vm.GetErrors(nameof(AccountingEntityDetailViewModel.PrimaryCellPhone)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void PrimaryCellPhone_Valid10Digits_NoError()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.PrimaryCellPhone = "3001234567";

        _vm.GetErrors(nameof(AccountingEntityDetailViewModel.PrimaryCellPhone)).Cast<string>()
            .Should().BeEmpty();
    }

    [Fact]
    public void SecondaryPhone_EmptyValue_NoError()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.SecondaryPhone = "";

        _vm.GetErrors(nameof(AccountingEntityDetailViewModel.SecondaryPhone)).Cast<string>()
            .Should().BeEmpty();
    }

    [Fact]
    public void ErrorsChanged_FiredOnValidation()
    {
        PrepareForNew();
        _vm.SetForNew();
        _vm.FirstName = "Valid";
        List<string> firedProperties = [];
        _vm.ErrorsChanged += (_, e) => firedProperties.Add(e.PropertyName!);

        _vm.FirstName = "";

        firedProperties.Should().Contain(nameof(AccountingEntityDetailViewModel.FirstName));
    }

    [Fact]
    public void GetErrors_NullPropertyName_ReturnsEmpty()
    {
        PrepareForNew();
        _vm.SetForNew();

        IEnumerable<string> errors = _vm.GetErrors(null).Cast<string>();

        errors.Should().BeEmpty();
    }

    [Fact]
    public void GetErrors_UnknownProperty_ReturnsEmpty()
    {
        PrepareForNew();
        _vm.SetForNew();

        IEnumerable<string> errors = _vm.GetErrors("NonExistentProperty").Cast<string>();

        errors.Should().BeEmpty();
    }

    [Fact]
    public void HasBasicDataErrors_TrueWhenFirstNameError()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.FirstName = "Valid";
        _vm.FirstName = "";

        _vm.HasBasicDataErrors.Should().BeTrue();
    }

    [Fact]
    public void HasBasicDataErrors_TrueWhenPrimaryPhoneError()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.PrimaryPhone = "12345";

        _vm.HasBasicDataErrors.Should().BeTrue();
    }

    [Fact]
    public void BasicDataTabTooltip_NullWhenNoErrors()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.BasicDataTabTooltip.Should().BeNull();
    }

    [Fact]
    public void BasicDataTabTooltip_ContainsErrorWhenPresent()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.FirstName = "Valid";
        _vm.FirstName = "";

        _vm.BasicDataTabTooltip.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region CaptureType transitions

    [Fact]
    public void CaptureType_PN_ExposesPNPanelsOnly()
    {
        _vm.SelectedCaptureType = CaptureTypeEnum.PN;

        _vm.CaptureInfoAsPN.Should().BeTrue();
        _vm.CaptureInfoAsPJ.Should().BeFalse();
    }

    [Fact]
    public void CaptureType_PJ_ExposesPJPanelsOnly()
    {
        _vm.SelectedCaptureType = CaptureTypeEnum.PJ;

        _vm.CaptureInfoAsPJ.Should().BeTrue();
        _vm.CaptureInfoAsPN.Should().BeFalse();
    }

    [Fact]
    public void CaptureType_SwitchFromPNToPJ_ClearsNameFields()
    {
        PrepareForNew();
        _vm.SetForNew();
        _vm.FirstName = "Juan";
        _vm.MiddleName = "Carlos";
        _vm.FirstLastName = "Pérez";
        _vm.MiddleLastName = "García";
        _vm.TradeName = "Tienda Juan";

        _vm.SelectedCaptureType = CaptureTypeEnum.PJ;

        _vm.FirstName.Should().BeEmpty();
        _vm.MiddleName.Should().BeEmpty();
        _vm.FirstLastName.Should().BeEmpty();
        _vm.MiddleLastName.Should().BeEmpty();
        _vm.TradeName.Should().BeEmpty();
    }

    [Fact]
    public void CaptureType_SwitchFromPJToPN_ClearsBusinessName()
    {
        PrepareForNew();
        _vm.SetForNew();
        _vm.SelectedCaptureType = CaptureTypeEnum.PJ;
        _vm.BusinessName = "Some Corp";

        _vm.SelectedCaptureType = CaptureTypeEnum.PN;

        _vm.BusinessName.Should().BeEmpty();
    }

    [Fact]
    public void CaptureType_SwitchToPJ_ClearsNameErrors()
    {
        PrepareForNew();
        _vm.SetForNew();
        _vm.FirstName = "Valid";
        _vm.FirstName = ""; // Genera error
        _vm.HasErrors.Should().BeTrue();

        _vm.SelectedCaptureType = CaptureTypeEnum.PJ;

        _vm.GetErrors(nameof(AccountingEntityDetailViewModel.FirstName)).Cast<string>()
            .Should().BeEmpty();
        _vm.GetErrors(nameof(AccountingEntityDetailViewModel.FirstLastName)).Cast<string>()
            .Should().BeEmpty();
    }

    #endregion

    #region Emails

    [Fact]
    public void CanAddEmail_EmptyFields_ReturnsFalse()
    {
        _vm.Email = "";
        _vm.EmailDescription = "";

        _vm.CanAddEmail.Should().BeFalse();
    }

    [Fact]
    public void CanAddEmail_InvalidEmailFormat_ReturnsFalse()
    {
        _vm.EmailDescription = "Principal";
        _vm.Email = "not-an-email";

        _vm.CanAddEmail.Should().BeFalse();
    }

    [Fact]
    public void CanAddEmail_ValidData_ReturnsTrue()
    {
        _vm.EmailDescription = "Principal";
        _vm.Email = "test@example.com";

        _vm.CanAddEmail.Should().BeTrue();
    }

    [Fact]
    public void AddEmail_AppendsToCollectionAndClearsInputs()
    {
        PrepareForNew();
        _vm.SetForNew();
        _vm.EmailDescription = "Principal";
        _vm.Email = "test@example.com";

        _vm.AddEmail();

        _vm.Emails.Should().HaveCount(1);
        _vm.Emails[0].Email.Should().Be("test@example.com");
        _vm.Emails[0].Description.Should().Be("Principal");
        _vm.Email.Should().BeEmpty();
        _vm.EmailDescription.Should().BeEmpty();
    }

    [Fact]
    public void CanRemoveEmail_AlwaysReturnsTrue()
    {
        _vm.CanRemoveEmail(null!).Should().BeTrue();
    }

    #endregion

    #region Regime

    [Fact]
    public void SelectedRegime_SetDifferentValue_TracksChange()
    {
        PrepareForNew();
        _vm.PopulateFromAccountingEntity(CreateSampleEntity());

        _vm.SelectedRegime = 'S';

        _vm.CanSave.Should().BeTrue();
    }

    #endregion

    #region ExecuteSaveAsync

    [Fact]
    public async Task ExecuteSaveAsync_NewRecord_CallsCreateAsync()
    {
        UpsertResponseType<AccountingEntityGraphQLModel> expectedResult = new()
        {
            Entity = CreateSampleEntity(),
            Success = true,
            Message = "OK"
        };
        _service.CreateAsync<UpsertResponseType<AccountingEntityGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        PrepareForNew();
        _vm.SetForNew();
        _vm.IdentificationNumber = "99999999";
        _vm.FirstName = "Test";
        _vm.FirstLastName = "User";

        UpsertResponseType<AccountingEntityGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).CreateAsync<UpsertResponseType<AccountingEntityGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSaveAsync_ExistingRecord_CallsUpdateAsync()
    {
        UpsertResponseType<AccountingEntityGraphQLModel> expectedResult = new()
        {
            Entity = CreateSampleEntity(),
            Success = true,
            Message = "OK"
        };
        _service.UpdateAsync<UpsertResponseType<AccountingEntityGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        PrepareForNew();
        _vm.PopulateFromAccountingEntity(CreateSampleEntity());
        _vm.FirstName = "Modified";

        UpsertResponseType<AccountingEntityGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).UpdateAsync<UpsertResponseType<AccountingEntityGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
        await _service.DidNotReceive().CreateAsync<UpsertResponseType<AccountingEntityGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region PropertyChanged

    [Fact]
    public void FirstName_Set_RaisesPropertyChanged()
    {
        PrepareForNew();
        _vm.SetForNew();
        List<string> changedProperties = [];
        _vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        _vm.FirstName = "Test";

        changedProperties.Should().Contain(nameof(AccountingEntityDetailViewModel.FirstName));
        changedProperties.Should().Contain(nameof(AccountingEntityDetailViewModel.CanSave));
    }

    [Fact]
    public void IdentificationNumber_Set_RaisesVerificationDigitNotification()
    {
        PrepareForNew();
        _vm.SetForNew();
        List<string> changedProperties = [];
        _vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        _vm.IdentificationNumber = "900123456";

        changedProperties.Should().Contain(nameof(AccountingEntityDetailViewModel.VerificationDigit));
    }

    [Fact]
    public void IsNewRecord_IdZero_ReturnsTrue()
    {
        _vm.Id = 0;
        _vm.IsNewRecord.Should().BeTrue();
    }

    [Fact]
    public void IsNewRecord_IdNonZero_ReturnsFalse()
    {
        _vm.Id = 10;
        _vm.IsNewRecord.Should().BeFalse();
    }

    #endregion

    #region VerificationDigit

    [Fact]
    public void VerificationDigit_NewRecordWithNitAndIdentification_ReturnsComputedValue()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.IdentificationNumber = "900123456";

        _vm.VerificationDigit.Should().NotBeNull();
    }

    [Fact]
    public void VerificationDigit_NewRecordWithoutIdType_ReturnsEmpty()
    {
        // No PrepareForNew → SelectedIdentificationType es null
        _vm.Id = 0;

        _vm.VerificationDigit.Should().BeEmpty();
    }

    [Fact]
    public void VerificationDigit_EditRecord_ReturnsSetValue()
    {
        PrepareForNew();
        AccountingEntityGraphQLModel entity = CreateSampleEntity();
        entity.VerificationDigit = "9";
        _vm.PopulateFromAccountingEntity(entity);

        _vm.VerificationDigit.Should().Be("9");
    }

    #endregion

    #region IdentificationNumberMask

    [Fact]
    public void IdentificationNumberMask_AllowsLettersFalse_NumericPattern()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.IdentificationNumberMask.Should().StartWith("[0-9]");
    }

    [Fact]
    public void IdentificationNumberMask_AllowsLettersTrue_AlphanumericPattern()
    {
        IdentificationTypeGraphQLModel alphaType = new()
        {
            Id = 2,
            Code = "41",
            Name = "Pasaporte",
            MinimumDocumentLength = 6,
            HasVerificationDigit = false,
            AllowsLetters = true
        };
        _identificationTypeCache.Add(alphaType);
        _vm.IdentificationTypes = _identificationTypeCache.Items;
        _vm.SelectedIdentificationType = alphaType;

        _vm.IdentificationNumberMask.Should().StartWith("[a-zA-Z0-9]");
    }

    #endregion
}
