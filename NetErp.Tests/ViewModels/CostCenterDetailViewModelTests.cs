using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Common.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Global.CostCenters.Validators;
using NetErp.Global.CostCenters.ViewModels;
using NetErp.Helpers.Cache;
using NSubstitute;
using Xunit;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.ViewModels;

public class CostCenterDetailViewModelTests
{
    private readonly IRepository<CostCenterGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly StringLengthCache _stringLengthCache;
    private readonly AuthorizationSequenceCache _authorizationSequenceCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly CostCenterValidator _validator;
    private readonly CostCenterDetailViewModel _vm;

    public CostCenterDetailViewModelTests()
    {
        _service = Substitute.For<IRepository<CostCenterGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        IRepository<AuthorizationSequenceGraphQLModel> authSeqRepo = Substitute.For<IRepository<AuthorizationSequenceGraphQLModel>>();
        _authorizationSequenceCache = new AuthorizationSequenceCache(authSeqRepo, _eventAggregator);

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;

        _validator = new CostCenterValidator();

        _vm = new CostCenterDetailViewModel(
            _service,
            _eventAggregator,
            _stringLengthCache,
            _authorizationSequenceCache,
            _joinableTaskFactory,
            _validator);
    }

    private static List<CountryGraphQLModel> SampleCountries() =>
    [
        new CountryGraphQLModel
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
        }
    ];

    private static CostCenterGraphQLModel CreateSampleCostCenter() => new()
    {
        Id = 20,
        Name = "Centro Norte",
        TradeName = "CN Trade",
        ShortName = "CN",
        Status = "ACTIVE",
        Address = "Calle 1",
        PrimaryPhone = "1234567",
        SecondaryPhone = "",
        PrimaryCellPhone = "3001234567",
        SecondaryCellPhone = "",
        DateControlType = "OPEN_DATE",
        ShowChangeWindowOnCash = true,
        AllowBuy = true,
        AllowSell = true,
        IsTaxable = true,
        PriceListIncludeTax = false,
        InvoicePriceIncludeTax = false,
        AllowRepeatItemsOnSales = false,
        InvoiceCopiesToPrint = 2,
        RequiresConfirmationToPrintCopies = true,
        TaxToCost = false,
        DefaultInvoiceObservation = "obs",
        InvoiceFooter = "footer",
        RemissionFooter = "rem",
        CompanyLocation = new CompanyLocationGraphQLModel { Id = 5 },
        Country = new CountryGraphQLModel { Id = 1 },
        Department = new DepartmentGraphQLModel { Id = 1 },
        City = new CityGraphQLModel { Id = 1 },
        FeCreditDefaultAuthorizationSequence = new AuthorizationSequenceGraphQLModel { Id = 100, Description = "FE Cred" },
        FeCashDefaultAuthorizationSequence = null,
        PeDefaultAuthorizationSequence = null,
        DsDefaultAuthorizationSequence = null
    };

    #region Construction

    [Fact]
    public void Constructor_NullService_Throws()
    {
        System.Action act = () => new CostCenterDetailViewModel(
            null!, _eventAggregator, _stringLengthCache, _authorizationSequenceCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("costCenterService");
    }

    [Fact]
    public void Constructor_NullValidator_Throws()
    {
        System.Action act = () => new CostCenterDetailViewModel(
            _service, _eventAggregator, _stringLengthCache, _authorizationSequenceCache, _joinableTaskFactory, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("validator");
    }

    [Fact]
    public void Constructor_NullAuthSeqCache_Throws()
    {
        System.Action act = () => new CostCenterDetailViewModel(
            _service, _eventAggregator, _stringLengthCache, null!, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("authorizationSequenceCache");
    }

    [Fact]
    public void Constructor_SetsDialogDimensions()
    {
        _vm.DialogWidth.Should().Be(720);
        _vm.DialogHeight.Should().Be(620);
    }

    #endregion

    #region SetForNew

    [Fact]
    public void SetForNew_SetsDefaults()
    {
        _vm.SetForNew(parentCompanyLocationId: 5, SampleCountries());

        _vm.Id.Should().Be(0);
        _vm.IsNewRecord.Should().BeTrue();
        _vm.Name.Should().BeEmpty();
        _vm.TradeName.Should().BeEmpty();
        _vm.ShortName.Should().BeEmpty();
        _vm.Status.Should().Be("ACTIVE");
        _vm.Address.Should().BeEmpty();
        _vm.PrimaryPhone.Should().BeEmpty();
        _vm.PrimaryCellPhone.Should().BeEmpty();
        _vm.DateControlType.Should().Be("OPEN_DATE");
        _vm.ShowChangeWindowOnCash.Should().BeFalse();
        _vm.AllowBuy.Should().BeFalse();
        _vm.AllowSell.Should().BeFalse();
        _vm.IsTaxable.Should().BeFalse();
        _vm.InvoiceCopiesToPrint.Should().Be(0);
        _vm.RequiresConfirmationToPrintCopies.Should().BeFalse();
        _vm.CompanyLocationId.Should().Be(5);
        _vm.FeCreditDefaultAuthorizationSequenceId.Should().BeNull();
        _vm.FeCashDefaultAuthorizationSequenceId.Should().BeNull();
        _vm.PeDefaultAuthorizationSequenceId.Should().BeNull();
        _vm.DsDefaultAuthorizationSequenceId.Should().BeNull();
    }

    [Fact]
    public void SetForNew_PicksDefaultColombia()
    {
        _vm.SetForNew(parentCompanyLocationId: 5, SampleCountries());

        _vm.SelectedCountry.Should().NotBeNull();
        _vm.SelectedCountry!.Code.Should().Be("169");
        _vm.SelectedDepartment.Should().NotBeNull();
        _vm.SelectedCity.Should().NotBeNull();
    }

    [Fact]
    public void SetForNew_NoInitialChanges()
    {
        _vm.SetForNew(parentCompanyLocationId: 5, SampleCountries());

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region SetForEdit

    [Fact]
    public void SetForEdit_PopulatesFromModel()
    {
        _vm.SetForEdit(CreateSampleCostCenter(), SampleCountries());

        _vm.Id.Should().Be(20);
        _vm.IsNewRecord.Should().BeFalse();
        _vm.Name.Should().Be("Centro Norte");
        _vm.ShortName.Should().Be("CN");
        _vm.PrimaryPhone.Should().Be("1234567");
        _vm.PrimaryCellPhone.Should().Be("3001234567");
        _vm.IsTaxable.Should().BeTrue();
        _vm.InvoiceCopiesToPrint.Should().Be(2);
        _vm.CompanyLocationId.Should().Be(5);
        _vm.SelectedCity!.Id.Should().Be(1);
    }

    [Fact]
    public void SetForEdit_LoadsAuthorizationSequencesFromEntity()
    {
        _vm.SetForEdit(CreateSampleCostCenter(), SampleCountries());

        _vm.FeCreditDefaultAuthorizationSequenceId.Should().Be(100);
        _vm.FeCashDefaultAuthorizationSequenceId.Should().BeNull();
        _vm.PeDefaultAuthorizationSequenceId.Should().BeNull();
        _vm.DsDefaultAuthorizationSequenceId.Should().BeNull();
    }

    [Fact]
    public void SetForEdit_NoInitialChanges()
    {
        _vm.SetForEdit(CreateSampleCostCenter(), SampleCountries());

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AfterChangingName_ReturnsTrue()
    {
        _vm.SetForEdit(CreateSampleCostCenter(), SampleCountries());

        _vm.Name = "Centro Modificado";

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_EmptyName_ReturnsFalse()
    {
        _vm.SetForEdit(CreateSampleCostCenter(), SampleCountries());

        _vm.Name = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyShortName_ReturnsFalse()
    {
        _vm.SetForEdit(CreateSampleCostCenter(), SampleCountries());

        _vm.ShortName = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_IsBusy_ReturnsFalse()
    {
        _vm.SetForEdit(CreateSampleCostCenter(), SampleCountries());
        _vm.Name = "Modified";

        _vm.IsBusy = true;

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region Validation

    [Fact]
    public void Name_SetEmpty_AddsValidationError()
    {
        _vm.SetForNew(parentCompanyLocationId: 1, SampleCountries());
        _vm.Name = "Valid";

        _vm.Name = "";

        _vm.HasErrors.Should().BeTrue();
        _vm.GetErrors(nameof(CostCenterDetailViewModel.Name)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void ShortName_SetEmpty_AddsValidationError()
    {
        _vm.SetForNew(parentCompanyLocationId: 1, SampleCountries());
        _vm.ShortName = "CN";

        _vm.ShortName = "";

        _vm.GetErrors(nameof(CostCenterDetailViewModel.ShortName)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void PrimaryPhone_InvalidLength_AddsError()
    {
        _vm.SetForNew(parentCompanyLocationId: 1, SampleCountries());

        _vm.PrimaryPhone = "12345";

        _vm.GetErrors(nameof(CostCenterDetailViewModel.PrimaryPhone)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void PrimaryPhone_Valid7Digits_NoError()
    {
        _vm.SetForNew(parentCompanyLocationId: 1, SampleCountries());

        _vm.PrimaryPhone = "1234567";

        _vm.GetErrors(nameof(CostCenterDetailViewModel.PrimaryPhone)).Cast<string>()
            .Should().BeEmpty();
    }

    [Fact]
    public void PrimaryCellPhone_InvalidLength_AddsError()
    {
        _vm.SetForNew(parentCompanyLocationId: 1, SampleCountries());

        _vm.PrimaryCellPhone = "300";

        _vm.GetErrors(nameof(CostCenterDetailViewModel.PrimaryCellPhone)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void PrimaryCellPhone_Valid10Digits_NoError()
    {
        _vm.SetForNew(parentCompanyLocationId: 1, SampleCountries());

        _vm.PrimaryCellPhone = "3001234567";

        _vm.GetErrors(nameof(CostCenterDetailViewModel.PrimaryCellPhone)).Cast<string>()
            .Should().BeEmpty();
    }

    [Fact]
    public void HasBasicDataErrors_TrueWhenNameInvalid()
    {
        _vm.SetForNew(parentCompanyLocationId: 1, SampleCountries());
        _vm.Name = "Valid";
        _vm.Name = "";

        _vm.HasBasicDataErrors.Should().BeTrue();
    }

    [Fact]
    public void HasPhoneErrors_TrueWhenPhoneInvalid()
    {
        _vm.SetForNew(parentCompanyLocationId: 1, SampleCountries());

        _vm.PrimaryPhone = "12345";

        _vm.HasPhoneErrors.Should().BeTrue();
    }

    [Fact]
    public void HasPhoneErrors_FalseForBasicErrors()
    {
        _vm.SetForNew(parentCompanyLocationId: 1, SampleCountries());
        _vm.Name = "Valid";
        _vm.Name = "";

        _vm.HasPhoneErrors.Should().BeFalse();
    }

    #endregion

    #region IsTaxable Cascade

    [Fact]
    public void IsTaxable_SetFalse_ClearsPriceListIncludeTax()
    {
        _vm.SetForEdit(CreateSampleCostCenter(), SampleCountries());
        _vm.PriceListIncludeTax = true;
        _vm.InvoicePriceIncludeTax = true;

        _vm.IsTaxable = false;

        _vm.PriceListIncludeTax.Should().BeFalse();
        _vm.InvoicePriceIncludeTax.Should().BeFalse();
    }

    #endregion

    #region InvoiceCopiesToPrint Cascade

    [Fact]
    public void InvoiceCopiesToPrint_SetZero_ClearsRequiresConfirmation()
    {
        _vm.SetForEdit(CreateSampleCostCenter(), SampleCountries());
        _vm.RequiresConfirmationToPrintCopies = true;

        _vm.InvoiceCopiesToPrint = 0;

        _vm.RequiresConfirmationToPrintCopies.Should().BeFalse();
    }

    [Fact]
    public void RequiresConfirmationToPrintCopiesIsEnabled_TrueWhenCopiesGreaterThanZero()
    {
        _vm.SetForEdit(CreateSampleCostCenter(), SampleCountries());

        _vm.InvoiceCopiesToPrint = 3;

        _vm.RequiresConfirmationToPrintCopiesIsEnabled.Should().BeTrue();
    }

    [Fact]
    public void RequiresConfirmationToPrintCopiesIsEnabled_FalseWhenCopiesZero()
    {
        _vm.SetForEdit(CreateSampleCostCenter(), SampleCountries());

        _vm.InvoiceCopiesToPrint = 0;

        _vm.RequiresConfirmationToPrintCopiesIsEnabled.Should().BeFalse();
    }

    #endregion

    #region Authorization Sequences

    [Fact]
    public void FeCreditDefaultAuthorizationSequenceId_SetValue_TracksAsChange()
    {
        _vm.SetForEdit(CreateSampleCostCenter(), SampleCountries());

        _vm.FeCreditDefaultAuthorizationSequenceId = 200;

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void FeCashDefaultAuthorizationSequenceId_FromNullToValue_TracksAsChange()
    {
        _vm.SetForEdit(CreateSampleCostCenter(), SampleCountries());

        _vm.FeCashDefaultAuthorizationSequenceId = 50;

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void FeCreditDefaultAuthorizationSequenceId_BackToSeed_UnmarksAsChange()
    {
        _vm.SetForEdit(CreateSampleCostCenter(), SampleCountries());
        _vm.FeCreditDefaultAuthorizationSequenceId = 200;
        _vm.CanSave.Should().BeTrue();

        _vm.FeCreditDefaultAuthorizationSequenceId = 100; // back to seed value

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region Status (tri-state)

    [Fact]
    public void Status_DefaultIsActive()
    {
        _vm.SetForNew(1, SampleCountries());
        _vm.IsStatusActive.Should().BeTrue();
    }

    [Fact]
    public void Status_SetReadOnly_PropagatesCorrectly()
    {
        _vm.SetForEdit(CreateSampleCostCenter(), SampleCountries());

        _vm.IsStatusReadOnly = true;

        _vm.Status.Should().Be("READ_ONLY");
    }

    [Fact]
    public void Status_SetInactive_PropagatesCorrectly()
    {
        _vm.SetForEdit(CreateSampleCostCenter(), SampleCountries());

        _vm.IsStatusInactive = true;

        _vm.Status.Should().Be("INACTIVE");
    }

    #endregion

    #region ExecuteSaveAsync

    [Fact]
    public async Task ExecuteSaveAsync_NewRecord_CallsCreateAsync()
    {
        UpsertResponseType<CostCenterGraphQLModel> expectedResult = new()
        {
            Entity = CreateSampleCostCenter(),
            Success = true,
            Message = "OK"
        };
        _service.CreateAsync<UpsertResponseType<CostCenterGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForNew(parentCompanyLocationId: 5, SampleCountries());
        _vm.Name = "Test";
        _vm.ShortName = "T";

        UpsertResponseType<CostCenterGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).CreateAsync<UpsertResponseType<CostCenterGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSaveAsync_ExistingRecord_CallsUpdateAsync()
    {
        UpsertResponseType<CostCenterGraphQLModel> expectedResult = new()
        {
            Entity = CreateSampleCostCenter(),
            Success = true,
            Message = "OK"
        };
        _service.UpdateAsync<UpsertResponseType<CostCenterGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForEdit(CreateSampleCostCenter(), SampleCountries());
        _vm.Name = "Modified";

        UpsertResponseType<CostCenterGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).UpdateAsync<UpsertResponseType<CostCenterGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region PropertyChanged

    [Fact]
    public void Name_Set_RaisesPropertyChanged()
    {
        _vm.SetForNew(1, SampleCountries());
        List<string> changedProperties = [];
        _vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        _vm.Name = "Test";

        changedProperties.Should().Contain(nameof(CostCenterDetailViewModel.Name));
        changedProperties.Should().Contain(nameof(CostCenterDetailViewModel.CanSave));
    }

    #endregion
}
