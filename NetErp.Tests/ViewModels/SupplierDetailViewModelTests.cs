using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using Models.Books;
using Models.Global;
using Models.Suppliers;
using NetErp.Helpers.Cache;
using NetErp.Suppliers.Suppliers.Validators;
using NetErp.Suppliers.Suppliers.ViewModels;
using NSubstitute;
using Xunit;
using static Dictionaries.BooksDictionaries;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.ViewModels;

public class SupplierDetailViewModelTests
{
    private readonly IRepository<SupplierGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly IdentificationTypeCache _identificationTypeCache;
    private readonly CountryCache _countryCache;
    private readonly WithholdingTypeCache _withholdingTypeCache;
    private readonly StringLengthCache _stringLengthCache;
    private readonly IMapper _mapper;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly IGraphQLClient _graphQLClient;
    private readonly SupplierValidator _validator;
    private readonly ObservableCollection<AccountingAccountGraphQLModel> _accountingAccounts;
    private readonly SupplierDetailViewModel _vm;

    public SupplierDetailViewModelTests()
    {
        _service = Substitute.For<IRepository<SupplierGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();

        IRepository<IdentificationTypeGraphQLModel> idTypeRepo = Substitute.For<IRepository<IdentificationTypeGraphQLModel>>();
        _identificationTypeCache = new IdentificationTypeCache(idTypeRepo, _eventAggregator);

        IRepository<CountryGraphQLModel> countryRepo = Substitute.For<IRepository<CountryGraphQLModel>>();
        _countryCache = new CountryCache(countryRepo, _eventAggregator);

        IRepository<WithholdingTypeGraphQLModel> withholdingRepo = Substitute.For<IRepository<WithholdingTypeGraphQLModel>>();
        _withholdingTypeCache = new WithholdingTypeCache(withholdingRepo, _eventAggregator);

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        _mapper = Substitute.For<IMapper>();

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;

        _graphQLClient = Substitute.For<IGraphQLClient>();
        _validator = new SupplierValidator();

        _accountingAccounts = [];

        _vm = new SupplierDetailViewModel(
            _service,
            _eventAggregator,
            _accountingAccounts,
            _identificationTypeCache,
            _countryCache,
            _withholdingTypeCache,
            _stringLengthCache,
            _mapper,
            _graphQLClient,
            _joinableTaskFactory,
            _validator,
            Substitute.For<NetErp.Helpers.Services.INotificationService>(),
            Substitute.For<NetErp.Helpers.Services.IAccountingEntityLookupService>());
    }

    private void PrepareForNew()
    {
        IdentificationTypeGraphQLModel ccType = new()
        {
            Id = 1,
            Code = "13",
            Name = "CC",
            MinimumDocumentLength = 5,
            HasVerificationDigit = false,
            AllowsLetters = false
        };
        _identificationTypeCache.Add(ccType);

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
        _vm.SelectedIdentificationType = _identificationTypeCache.Items.First();
    }

    private SupplierGraphQLModel CreateSampleSupplier() => new()
    {
        Id = 10,
        IsTaxFree = false,
        WithholdingAppliesOnAnyAmount = false,
        IcaWithholdingRate = 0m,
        IcaAccountingAccount = new AccountingAccountGraphQLModel { Id = 0 },
        AccountingEntity = new AccountingEntityGraphQLModel
        {
            Regime = 'R',
            CaptureType = "PN",
            IdentificationType = new IdentificationTypeGraphQLModel { Id = 1, Code = "13", MinimumDocumentLength = 5 },
            FirstName = "Carlos",
            MiddleName = "",
            FirstLastName = "Medrano",
            MiddleLastName = "",
            BusinessName = "",
            TradeName = "",
            CommercialCode = "",
            PrimaryPhone = "1234567",
            SecondaryPhone = "",
            PrimaryCellPhone = "3001234567",
            SecondaryCellPhone = "",
            Address = "Calle 1 # 2-3",
            IdentificationNumber = "12345678",
            VerificationDigit = "",
            Country = new CountryGraphQLModel { Id = 1 },
            Department = new DepartmentGraphQLModel { Id = 1 },
            City = new CityGraphQLModel { Id = 1 },
            Emails = []
        },
        WithholdingTypes = []
    };

    #region Construction

    [Fact]
    public void Constructor_NullValidator_Throws()
    {
        System.Action act = () => new SupplierDetailViewModel(
            _service, _eventAggregator, _accountingAccounts,
            _identificationTypeCache, _countryCache, _withholdingTypeCache,
            _stringLengthCache, _mapper, _graphQLClient, _joinableTaskFactory, null!,
            Substitute.For<NetErp.Helpers.Services.INotificationService>(),
            Substitute.For<NetErp.Helpers.Services.IAccountingEntityLookupService>());

        act.Should().Throw<ArgumentNullException>().WithParameterName("validator");
    }

    #endregion

    #region SetForNew

    [Fact]
    public void SetForNew_SetsDefaults()
    {
        PrepareForNew();

        _vm.SetForNew();

        _vm.Id.Should().Be(0);
        _vm.IsNewRecord.Should().BeTrue();
        _vm.SelectedCaptureType.Should().Be(CaptureTypeEnum.PN);
        _vm.CaptureInfoAsPN.Should().BeTrue();
        _vm.FirstName.Should().BeEmpty();
        _vm.MiddleName.Should().BeEmpty();
        _vm.FirstLastName.Should().BeEmpty();
        _vm.MiddleLastName.Should().BeEmpty();
        _vm.BusinessName.Should().BeEmpty();
        _vm.Address.Should().BeEmpty();
        _vm.IdentificationNumber.Should().BeEmpty();
        _vm.PrimaryPhone.Should().BeEmpty();
        _vm.SecondaryPhone.Should().BeEmpty();
        _vm.PrimaryCellPhone.Should().BeEmpty();
        _vm.SecondaryCellPhone.Should().BeEmpty();
        _vm.IsTaxFree.Should().BeFalse();
        _vm.IcaWithholdingRate.Should().Be(0m);
        _vm.WithholdingAppliesOnAnyAmount.Should().BeFalse();
    }

    [Fact]
    public void SetForNew_NoInitialChanges()
    {
        PrepareForNew();

        _vm.SetForNew();

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void SetForNew_SetsDefaultCountry()
    {
        PrepareForNew();

        _vm.SetForNew();

        _vm.SelectedCountry.Should().NotBeNull();
        _vm.SelectedDepartment.Should().NotBeNull();
        _vm.SelectedCityId.Should().Be(1);
    }

    #endregion

    #region SetForEdit

    [Fact]
    public void SetForEdit_PopulatesFromModel()
    {
        PrepareForNew();
        SupplierGraphQLModel supplier = CreateSampleSupplier();

        _vm.SetForEdit(supplier);

        _vm.Id.Should().Be(10);
        _vm.IsNewRecord.Should().BeFalse();
        _vm.FirstName.Should().Be("Carlos");
        _vm.FirstLastName.Should().Be("Medrano");
        _vm.IdentificationNumber.Should().Be("12345678");
        _vm.PrimaryPhone.Should().Be("1234567");
        _vm.PrimaryCellPhone.Should().Be("3001234567");
        _vm.Address.Should().Be("Calle 1 # 2-3");
        _vm.SelectedCaptureType.Should().Be(CaptureTypeEnum.PN);
    }

    [Fact]
    public void SetForEdit_NoInitialChanges()
    {
        PrepareForNew();
        SupplierGraphQLModel supplier = CreateSampleSupplier();

        _vm.SetForEdit(supplier);

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AfterChangingName_ReturnsTrue()
    {
        PrepareForNew();
        SupplierGraphQLModel supplier = CreateSampleSupplier();
        _vm.SetForEdit(supplier);

        _vm.FirstName = "Carlos Modificado";

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_EmptyFirstName_PN_ReturnsFalse()
    {
        PrepareForNew();
        SupplierGraphQLModel supplier = CreateSampleSupplier();
        _vm.SetForEdit(supplier);

        _vm.FirstName = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyIdentificationNumber_ReturnsFalse()
    {
        PrepareForNew();
        SupplierGraphQLModel supplier = CreateSampleSupplier();
        _vm.SetForEdit(supplier);

        _vm.IdentificationNumber = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_IsBusy_ReturnsFalse()
    {
        PrepareForNew();
        SupplierGraphQLModel supplier = CreateSampleSupplier();
        _vm.SetForEdit(supplier);
        _vm.FirstName = "Modified";

        _vm.IsBusy = true;

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
        _vm.GetErrors(nameof(SupplierDetailViewModel.FirstName)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void FirstName_SetValid_ClearsError()
    {
        PrepareForNew();
        _vm.SetForNew();
        _vm.FirstName = "";

        _vm.FirstName = "Carlos";

        _vm.GetErrors(nameof(SupplierDetailViewModel.FirstName)).Cast<string>()
            .Should().BeEmpty();
    }

    [Fact]
    public void Phone_InvalidLength_AddsError()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.PrimaryPhone = "12345";

        _vm.GetErrors(nameof(SupplierDetailViewModel.PrimaryPhone)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void Phone_Valid7Digits_NoError()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.PrimaryPhone = "1234567";

        _vm.GetErrors(nameof(SupplierDetailViewModel.PrimaryPhone)).Cast<string>()
            .Should().BeEmpty();
    }

    [Fact]
    public void CellPhone_InvalidLength_AddsError()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.PrimaryCellPhone = "12345";

        _vm.GetErrors(nameof(SupplierDetailViewModel.PrimaryCellPhone)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void IcaWithholdingRate_OutOfRange_AddsError()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.IcaWithholdingRate = 150m;

        _vm.GetErrors(nameof(SupplierDetailViewModel.IcaWithholdingRate)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void IcaWithholdingRate_Valid_NoError()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.IcaWithholdingRate = 5m;

        _vm.GetErrors(nameof(SupplierDetailViewModel.IcaWithholdingRate)).Cast<string>()
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

        firedProperties.Should().Contain("FirstName");
    }

    [Fact]
    public void HasBasicDataErrors_TrueWhenBasicFieldHasError()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.FirstName = "Valid";
        _vm.FirstName = "";

        _vm.HasBasicDataErrors.Should().BeTrue();
    }

    [Fact]
    public void HasWithholdingErrors_TrueWhenIcaOutOfRange()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.IcaWithholdingRate = 150m;

        _vm.HasWithholdingErrors.Should().BeTrue();
    }

    [Fact]
    public void HasWithholdingErrors_FalseForBasicFieldErrors()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.FirstName = "Valid";
        _vm.FirstName = "";

        _vm.HasWithholdingErrors.Should().BeFalse();
    }

    #endregion

    #region ExecuteSaveAsync

    [Fact]
    public async Task ExecuteSaveAsync_NewRecord_CallsCreateAsync()
    {
        UpsertResponseType<SupplierGraphQLModel> expectedResult = new()
        {
            Entity = CreateSampleSupplier(),
            Success = true,
            Message = "OK"
        };
        _service.CreateAsync<UpsertResponseType<SupplierGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        PrepareForNew();
        _vm.SetForNew();
        _vm.IdentificationNumber = "99999999";
        _vm.FirstName = "Test";
        _vm.FirstLastName = "User";

        UpsertResponseType<SupplierGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).CreateAsync<UpsertResponseType<SupplierGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSaveAsync_ExistingRecord_CallsUpdateAsync()
    {
        UpsertResponseType<SupplierGraphQLModel> expectedResult = new()
        {
            Entity = CreateSampleSupplier(),
            Success = true,
            Message = "OK"
        };
        _service.UpdateAsync<UpsertResponseType<SupplierGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        PrepareForNew();
        _vm.SetForEdit(CreateSampleSupplier());
        _vm.FirstName = "Modified";

        UpsertResponseType<SupplierGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).UpdateAsync<UpsertResponseType<SupplierGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
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

        changedProperties.Should().Contain(nameof(SupplierDetailViewModel.FirstName));
        changedProperties.Should().Contain(nameof(SupplierDetailViewModel.CanSave));
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

    #region CaptureType

    [Fact]
    public void CaptureType_PN_CaptureInfoAsPN_IsTrue()
    {
        _vm.SelectedCaptureType = CaptureTypeEnum.PN;
        _vm.CaptureInfoAsPN.Should().BeTrue();
        _vm.CaptureInfoAsPJ.Should().BeFalse();
    }

    [Fact]
    public void CaptureType_PJ_CaptureInfoAsPJ_IsTrue()
    {
        _vm.SelectedCaptureType = CaptureTypeEnum.PJ;
        _vm.CaptureInfoAsPJ.Should().BeTrue();
        _vm.CaptureInfoAsPN.Should().BeFalse();
    }

    #endregion
}
