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
using NetErp.Billing.Sellers.Validators;
using NetErp.Billing.Sellers.ViewModels;
using NetErp.Global.CostCenters.DTO;
using NetErp.Helpers.Cache;
using NSubstitute;
using Xunit;
using static Dictionaries.BooksDictionaries;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.ViewModels;

public class SellerDetailViewModelTests
{
    private readonly IRepository<SellerGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly IdentificationTypeCache _identificationTypeCache;
    private readonly CountryCache _countryCache;
    private readonly ZoneCache _zoneCache;
    private readonly CostCenterCache _costCenterCache;
    private readonly StringLengthCache _stringLengthCache;
    private readonly IMapper _mapper;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly IGraphQLClient _graphQLClient;
    private readonly SellerValidator _validator;
    private readonly SellerDetailViewModel _vm;

    public SellerDetailViewModelTests()
    {
        _service = Substitute.For<IRepository<SellerGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();

        IRepository<IdentificationTypeGraphQLModel> idTypeRepo = Substitute.For<IRepository<IdentificationTypeGraphQLModel>>();
        _identificationTypeCache = new IdentificationTypeCache(idTypeRepo, _eventAggregator);

        IRepository<CountryGraphQLModel> countryRepo = Substitute.For<IRepository<CountryGraphQLModel>>();
        _countryCache = new CountryCache(countryRepo, _eventAggregator);

        IRepository<ZoneGraphQLModel> zoneRepo = Substitute.For<IRepository<ZoneGraphQLModel>>();
        _zoneCache = new ZoneCache(zoneRepo, _eventAggregator);

        IRepository<CostCenterGraphQLModel> costCenterRepo = Substitute.For<IRepository<CostCenterGraphQLModel>>();
        _costCenterCache = new CostCenterCache(costCenterRepo, _eventAggregator);

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        _mapper = Substitute.For<IMapper>();
        _mapper.Map<ObservableCollection<CostCenterDTO>>(Arg.Any<object>())
            .Returns(callInfo =>
            {
                // Return CostCenterDTOs matching the cache items
                ObservableCollection<CostCenterDTO> result = [];
                foreach (CostCenterGraphQLModel cc in _costCenterCache.Items)
                {
                    result.Add(new CostCenterDTO { Id = cc.Id, Name = cc.Name });
                }
                return result;
            });

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;

        _graphQLClient = Substitute.For<IGraphQLClient>();
        _validator = new SellerValidator();

        _vm = new SellerDetailViewModel(
            _service, _eventAggregator,
            _identificationTypeCache, _countryCache, _zoneCache, _costCenterCache,
            _stringLengthCache, _mapper, _joinableTaskFactory, _graphQLClient, _validator,
            Substitute.For<NetErp.Helpers.Services.INotificationService>(),
            Substitute.For<NetErp.Helpers.Services.IAccountingEntityLookupService>());
    }

    private void PrepareForNew()
    {
        IdentificationTypeGraphQLModel ccType = new()
        {
            Id = 1, Code = "13", Name = "CC", MinimumDocumentLength = 5,
            HasVerificationDigit = false, AllowsLetters = false
        };
        _identificationTypeCache.Add(ccType);

        CountryGraphQLModel defaultCountry = new()
        {
            Id = 1, Code = "169", Name = "Colombia",
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

        CostCenterGraphQLModel costCenter = new() { Id = 1, Name = "Principal" };
        _costCenterCache.Add(costCenter);

        _vm.Countries = _countryCache.Items;
        _vm.Zones = _zoneCache.Items;
        _vm.SelectedIdentificationType = _identificationTypeCache.Items.First();
    }

    private SellerGraphQLModel CreateSampleSeller() => new()
    {
        Id = 10,
        IsActive = true,
        AccountingEntity = new AccountingEntityGraphQLModel
        {
            Regime = 'N',
            CaptureType = "PN",
            IdentificationType = new IdentificationTypeGraphQLModel { Id = 1, Code = "13", MinimumDocumentLength = 5 },
            FirstName = "Carlos",
            MiddleName = "",
            FirstLastName = "Medrano",
            MiddleLastName = "",
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
        CostCenters = [new CostCenterGraphQLModel { Id = 1, Name = "Principal" }]
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
        _vm.MiddleName.Should().BeEmpty();
        _vm.FirstLastName.Should().BeEmpty();
        _vm.MiddleLastName.Should().BeEmpty();
        _vm.Address.Should().BeEmpty();
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
        _vm.SelectedCityId.Should().NotBeNull();
    }

    #endregion

    #region SetForEdit

    [Fact]
    public void SetForEdit_PopulatesFromModel()
    {
        PrepareForNew();
        SellerGraphQLModel seller = CreateSampleSeller();

        _vm.SetForEdit(seller);

        _vm.Id.Should().Be(10);
        _vm.IsNewRecord.Should().BeFalse();
        _vm.IsActive.Should().BeTrue();
        _vm.FirstName.Should().Be("Carlos");
        _vm.FirstLastName.Should().Be("Medrano");
        _vm.IdentificationNumber.Should().Be("12345678");
        _vm.PrimaryPhone.Should().Be("1234567");
        _vm.PrimaryCellPhone.Should().Be("3001234567");
        _vm.Address.Should().Be("Calle 1 # 2-3");
    }

    [Fact]
    public void SetForEdit_NoInitialChanges()
    {
        PrepareForNew();
        SellerGraphQLModel seller = CreateSampleSeller();

        _vm.SetForEdit(seller);

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void SetForEdit_CostCentersMarkedCorrectly()
    {
        PrepareForNew();
        SellerGraphQLModel seller = CreateSampleSeller();

        _vm.SetForEdit(seller);

        _vm.CostCenters.Should().Contain(c => c.Id == 1 && c.IsSelected);
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AfterChangingName_ReturnsTrue()
    {
        PrepareForNew();
        SellerGraphQLModel seller = CreateSampleSeller();
        _vm.SetForEdit(seller);

        _vm.FirstName = "Carlos Modificado";

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_EmptyFirstName_PN_ReturnsFalse()
    {
        PrepareForNew();
        SellerGraphQLModel seller = CreateSampleSeller();
        _vm.SetForEdit(seller);

        _vm.FirstName = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyIdentificationNumber_ReturnsFalse()
    {
        PrepareForNew();
        SellerGraphQLModel seller = CreateSampleSeller();
        _vm.SetForEdit(seller);

        _vm.IdentificationNumber = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoCostCentersSelected_ReturnsFalse()
    {
        PrepareForNew();
        SellerGraphQLModel seller = CreateSampleSeller();
        _vm.SetForEdit(seller);

        foreach (CostCenterDTO cc in _vm.CostCenters)
            cc.IsSelected = false;

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
        _vm.GetErrors(nameof(SellerDetailViewModel.FirstName)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void FirstName_SetValid_ClearsError()
    {
        PrepareForNew();
        _vm.SetForNew();
        _vm.FirstName = "";

        _vm.FirstName = "Carlos";

        _vm.GetErrors(nameof(SellerDetailViewModel.FirstName)).Cast<string>()
            .Should().BeEmpty();
    }

    [Fact]
    public void Phone_InvalidLength_AddsError()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.PrimaryPhone = "12345";

        _vm.GetErrors(nameof(SellerDetailViewModel.PrimaryPhone)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void Phone_Valid7Digits_NoError()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.PrimaryPhone = "1234567";

        _vm.GetErrors(nameof(SellerDetailViewModel.PrimaryPhone)).Cast<string>()
            .Should().BeEmpty();
    }

    [Fact]
    public void CellPhone_InvalidLength_AddsError()
    {
        PrepareForNew();
        _vm.SetForNew();

        _vm.PrimaryCellPhone = "12345";

        _vm.GetErrors(nameof(SellerDetailViewModel.PrimaryCellPhone)).Cast<string>()
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
    public void ValidateProperties_ClearsErrorAfterValueCorrected()
    {
        // Reproduce bug: ValidateProperties re-called after fixing a field
        // must NOT leave stale errors from previous validation cycle
        PrepareForNew();
        _vm.SetForNew();

        // Force initial validation via CaptureType toggle (simulates OnViewReady)
        _vm.SelectedCaptureType = CaptureTypeEnum.PJ;
        _vm.SelectedCaptureType = CaptureTypeEnum.PN;

        // Precondition: FirstLastName="" → error exists
        _vm.GetErrors(nameof(SellerDetailViewModel.FirstLastName)).Cast<string>()
            .Should().NotBeEmpty("precondition: error exists on empty field");

        // User fills in FirstLastName → setter clears error
        _vm.FirstLastName = "Medrano";
        _vm.GetErrors(nameof(SellerDetailViewModel.FirstLastName)).Cast<string>()
            .Should().BeEmpty("setter should clear the error");

        // Trigger ValidateProperties again via CaptureType toggle
        _vm.SelectedCaptureType = CaptureTypeEnum.PJ;
        _vm.SelectedCaptureType = CaptureTypeEnum.PN;

        // FirstLastName="Medrano" is valid — error must NOT reappear
        _vm.GetErrors(nameof(SellerDetailViewModel.FirstLastName)).Cast<string>()
            .Should().BeEmpty("ValidateProperties must not leave stale errors for valid fields");
    }

    #endregion

    #region ExecuteSaveAsync

    [Fact]
    public async Task ExecuteSaveAsync_NewRecord_CallsCreateAsync()
    {
        UpsertResponseType<SellerGraphQLModel> expectedResult = new()
        {
            Entity = CreateSampleSeller(),
            Success = true,
            Message = "OK"
        };
        _service.CreateAsync<UpsertResponseType<SellerGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        PrepareForNew();
        _vm.SetForNew();
        _vm.IdentificationNumber = "99999999";
        _vm.FirstName = "Test";
        _vm.FirstLastName = "User";
        // Select at least one cost center
        foreach (CostCenterDTO cc in _vm.CostCenters)
            cc.IsSelected = true;

        UpsertResponseType<SellerGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).CreateAsync<UpsertResponseType<SellerGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSaveAsync_ExistingRecord_CallsUpdateAsync()
    {
        UpsertResponseType<SellerGraphQLModel> expectedResult = new()
        {
            Entity = CreateSampleSeller(),
            Success = true,
            Message = "OK"
        };
        _service.UpdateAsync<UpsertResponseType<SellerGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        PrepareForNew();
        _vm.SetForEdit(CreateSampleSeller());
        _vm.FirstName = "Modified";

        UpsertResponseType<SellerGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).UpdateAsync<UpsertResponseType<SellerGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
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

        changedProperties.Should().Contain(nameof(SellerDetailViewModel.FirstName));
        changedProperties.Should().Contain(nameof(SellerDetailViewModel.CanSave));
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
    public void CaptureType_SwitchToPJ_ClearsNameErrors()
    {
        PrepareForNew();
        _vm.SetForNew();
        // PN mode triggers FirstName/FirstLastName errors
        _vm.HasErrors.Should().BeTrue();

        _vm.SelectedCaptureType = CaptureTypeEnum.PJ;

        // PJ mode should clear PN-specific errors
        _vm.GetErrors(nameof(SellerDetailViewModel.FirstName)).Cast<string>()
            .Should().BeEmpty();
        _vm.GetErrors(nameof(SellerDetailViewModel.FirstLastName)).Cast<string>()
            .Should().BeEmpty();
    }

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
