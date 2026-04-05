using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using NetErp.Billing.Customers.Validators;
using NetErp.Billing.Customers.ViewModels;
using NetErp.Helpers.Cache;
using NSubstitute;
using Xunit;
using static Dictionaries.BooksDictionaries;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.ViewModels;

public class CustomerDetailViewModelTests
{
    private readonly IRepository<CustomerGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly IdentificationTypeCache _identificationTypeCache;
    private readonly CountryCache _countryCache;
    private readonly WithholdingTypeCache _withholdingTypeCache;
    private readonly ZoneCache _zoneCache;
    private readonly StringLengthCache _stringLengthCache;
    private readonly IMapper _mapper;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly IGraphQLClient _graphQLClient;
    private readonly CustomerValidator _validator;
    private readonly CustomerDetailViewModel _vm;

    public CustomerDetailViewModelTests()
    {
        _service = Substitute.For<IRepository<CustomerGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();

        // Caches con repos mock
        IRepository<IdentificationTypeGraphQLModel> idTypeRepo = Substitute.For<IRepository<IdentificationTypeGraphQLModel>>();
        _identificationTypeCache = new IdentificationTypeCache(idTypeRepo, _eventAggregator);

        IRepository<CountryGraphQLModel> countryRepo = Substitute.For<IRepository<CountryGraphQLModel>>();
        _countryCache = new CountryCache(countryRepo, _eventAggregator);

        IRepository<WithholdingTypeGraphQLModel> withholdingRepo = Substitute.For<IRepository<WithholdingTypeGraphQLModel>>();
        _withholdingTypeCache = new WithholdingTypeCache(withholdingRepo, _eventAggregator);

        IRepository<ZoneGraphQLModel> zoneRepo = Substitute.For<IRepository<ZoneGraphQLModel>>();
        _zoneCache = new ZoneCache(zoneRepo, _eventAggregator);

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        _mapper = Substitute.For<IMapper>();
        _mapper.Map<ObservableCollection<WithholdingTypeDTO>>(Arg.Any<object>())
            .Returns(new ObservableCollection<WithholdingTypeDTO>());

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;

        _graphQLClient = Substitute.For<IGraphQLClient>();
        _validator = new CustomerValidator();

        _vm = new CustomerDetailViewModel(
            _service, _eventAggregator,
            _identificationTypeCache, _countryCache, _withholdingTypeCache, _zoneCache,
            _stringLengthCache, _mapper, _joinableTaskFactory, _graphQLClient, _validator);
    }

    /// <summary>
    /// Prepara el ViewModel con datos minimos para que SetForNew funcione
    /// (requiere IdentificationTypes y Countries cargados).
    /// </summary>
    private void PrepareForNew()
    {
        // Simular cache de identification types con NIT (code "31" = default)
        IdentificationTypeGraphQLModel defaultIdType = new()
        {
            Id = 1, Code = "31", Name = "NIT", MinimumDocumentLength = 5,
            HasVerificationDigit = true, AllowsLetters = false
        };
        _identificationTypeCache.Add(defaultIdType);
        _vm.IdentificationTypes = _identificationTypeCache.Items;

        // Simular cache de paises con codes que coinciden con Constant defaults
        CountryGraphQLModel defaultCountry = new()
        {
            Id = 1, Code = "169", Name = "Colombia",
            Departments =
            [
                new DepartmentGraphQLModel
                {
                    Id = 1, Code = "08", Name = "Atlántico",
                    Cities = [new CityGraphQLModel { Id = 1, Code = "001", Name = "Barranquilla" }]
                }
            ]
        };
        _countryCache.Add(defaultCountry);
        _vm.Countries = _countryCache.Items;
        _vm.Zones = _zoneCache.Items;
        _vm.WithholdingTypes = [];
    }

    private CustomerGraphQLModel CreateSampleCustomer() => new()
    {
        Id = 10,
        CreditTerm = 30,
        IsTaxFree = false,
        IsActive = true,
        BlockingReason = string.Empty,
        RetainsAnyBasis = false,
        AccountingEntity = new AccountingEntityGraphQLModel
        {
            Regime = 'R',
            CaptureType = "PN",
            IdentificationType = new IdentificationTypeGraphQLModel { Id = 1, Code = "31", MinimumDocumentLength = 5 },
            FirstName = "Carlos",
            MiddleName = "Medrano",
            FirstLastName = "Antonio",
            MiddleLastName = "Garcia",
            PrimaryPhone = "1234567",
            SecondaryPhone = string.Empty,
            PrimaryCellPhone = "3001234567",
            SecondaryCellPhone = string.Empty,
            BusinessName = string.Empty,
            TradeName = string.Empty,
            Address = "Calle 1 # 2-3",
            IdentificationNumber = "12345678",
            VerificationDigit = string.Empty,
            Country = new CountryGraphQLModel { Id = 1 },
            Department = new DepartmentGraphQLModel { Id = 1 },
            City = new CityGraphQLModel { Id = 1 },
            Emails = []
        },
        WithholdingTypes = []
    };

    #region SetForNew

    [Fact]
    public void SetForNew_SetsDefaults()
    {
        PrepareForNew();

        _vm.SetForNew();

        _vm.Id.Should().Be(0);
        _vm.IsNewRecord.Should().BeTrue();
        _vm.IsActive.Should().BeTrue();
        _vm.SelectedCaptureType.Should().Be(CaptureTypeEnum.PN);
        _vm.CaptureInfoAsPN.Should().BeTrue();
        _vm.FirstName.Should().BeEmpty();
        _vm.BusinessName.Should().BeEmpty();
    }

    [Fact]
    public void SetForNew_NoInitialChanges()
    {
        PrepareForNew();

        _vm.SetForNew();

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region SetForEdit

    [Fact]
    public void SetForEdit_PopulatesFromModel()
    {
        PrepareForNew();
        CustomerGraphQLModel customer = CreateSampleCustomer();

        _vm.SetForEdit(customer);

        _vm.Id.Should().Be(10);
        _vm.IsNewRecord.Should().BeFalse();
        _vm.FirstName.Should().Be("Carlos");
        _vm.FirstLastName.Should().Be("Antonio");
        _vm.IdentificationNumber.Should().Be("12345678");
        _vm.CreditTerm.Should().Be(30);
        _vm.Address.Should().Be("Calle 1 # 2-3");
    }

    [Fact]
    public void SetForEdit_NoInitialChanges()
    {
        PrepareForNew();
        CustomerGraphQLModel customer = CreateSampleCustomer();

        _vm.SetForEdit(customer);

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AfterChangingName_ReturnsTrue()
    {
        // Add a CC type without verification digit for this test
        IdentificationTypeGraphQLModel ccType = new()
        {
            Id = 2, Code = "13", Name = "CC", MinimumDocumentLength = 5,
            HasVerificationDigit = false, AllowsLetters = false
        };
        _identificationTypeCache.Add(ccType);
        PrepareForNew();

        CustomerGraphQLModel customer = CreateSampleCustomer();
        customer.AccountingEntity.IdentificationType = new IdentificationTypeGraphQLModel { Id = 2, Code = "13", MinimumDocumentLength = 5 };
        _vm.SetForEdit(customer);

        _vm.FirstName = "Carlos Modificado";

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_EmptyFirstName_PN_ReturnsFalse()
    {
        PrepareForNew();
        CustomerGraphQLModel customer = CreateSampleCustomer();
        _vm.SetForEdit(customer);

        _vm.FirstName = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyIdentificationNumber_ReturnsFalse()
    {
        PrepareForNew();
        CustomerGraphQLModel customer = CreateSampleCustomer();
        _vm.SetForEdit(customer);

        _vm.IdentificationNumber = "";

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
        _vm.GetErrors(nameof(CustomerDetailViewModel.FirstName)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void FirstName_SetValid_ClearsError()
    {
        PrepareForNew();
        _vm.SetForNew();
        _vm.FirstName = "Valid";
        _vm.FirstName = ""; // trigger error

        _vm.FirstName = "Carlos"; // clear error

        _vm.GetErrors(nameof(CustomerDetailViewModel.FirstName)).Cast<string>()
            .Should().BeEmpty();
    }

    [Fact]
    public void BusinessName_SetEmpty_PJ_AddsValidationError()
    {
        PrepareForNew();
        _vm.SetForNew();
        _vm.SelectedCaptureType = CaptureTypeEnum.PJ;
        _vm.BusinessName = "Empresa";

        _vm.BusinessName = "";

        _vm.HasErrors.Should().BeTrue();
        _vm.GetErrors(nameof(CustomerDetailViewModel.BusinessName)).Cast<string>()
            .Should().NotBeEmpty();
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
    public void Phone_Invalid_Length_AddsError()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.PrimaryPhone = "12345";

        _vm.GetErrors(nameof(CustomerDetailViewModel.PrimaryPhone)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void Phone_Valid_7Digits_NoError()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.PrimaryPhone = "1234567";

        _vm.GetErrors(nameof(CustomerDetailViewModel.PrimaryPhone)).Cast<string>()
            .Should().BeEmpty();
    }

    #endregion

    #region ExecuteSaveAsync

    [Fact]
    public async Task ExecuteSaveAsync_NewRecord_CallsCreateAsync()
    {
        UpsertResponseType<CustomerGraphQLModel> expectedResult = new()
        {
            Entity = CreateSampleCustomer(),
            Success = true,
            Message = "OK"
        };
        _service.CreateAsync<UpsertResponseType<CustomerGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        PrepareForNew();
        _vm.SetForNew();
        _vm.IdentificationNumber = "99999999";
        _vm.FirstName = "Test";
        _vm.FirstLastName = "User";

        UpsertResponseType<CustomerGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).CreateAsync<UpsertResponseType<CustomerGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSaveAsync_ExistingRecord_CallsUpdateAsync()
    {
        UpsertResponseType<CustomerGraphQLModel> expectedResult = new()
        {
            Entity = CreateSampleCustomer(),
            Success = true,
            Message = "OK"
        };
        _service.UpdateAsync<UpsertResponseType<CustomerGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        PrepareForNew();
        _vm.SetForEdit(CreateSampleCustomer());
        _vm.FirstName = "Modified";

        UpsertResponseType<CustomerGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).UpdateAsync<UpsertResponseType<CustomerGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region PropertyChanged

    [Fact]
    public void Name_Set_RaisesPropertyChanged()
    {
        PrepareForNew();
        _vm.SetForNew();
        List<string> changedProperties = [];
        _vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        _vm.FirstName = "Test";

        changedProperties.Should().Contain(nameof(CustomerDetailViewModel.FirstName));
        changedProperties.Should().Contain(nameof(CustomerDetailViewModel.CanSave));
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
    public void CaptureType_SwitchToPJ_ClearsNameFields()
    {
        PrepareForNew();
        _vm.SetForNew();
        _vm.FirstName = "Carlos";
        _vm.FirstLastName = "Antonio";

        _vm.SelectedCaptureType = CaptureTypeEnum.PJ;

        _vm.FirstName.Should().BeEmpty();
        _vm.FirstLastName.Should().BeEmpty();
        _vm.CaptureInfoAsPJ.Should().BeTrue();
    }

    [Fact]
    public void CaptureType_SwitchToPN_ClearsBusinessName()
    {
        PrepareForNew();
        _vm.SetForNew();
        _vm.SelectedCaptureType = CaptureTypeEnum.PJ;
        _vm.BusinessName = "Mi Empresa";

        _vm.SelectedCaptureType = CaptureTypeEnum.PN;

        _vm.BusinessName.Should().BeEmpty();
        _vm.CaptureInfoAsPN.Should().BeTrue();
    }

    #endregion
}
