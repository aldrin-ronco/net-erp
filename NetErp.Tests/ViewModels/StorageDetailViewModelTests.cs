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

public class StorageDetailViewModelTests
{
    private readonly IRepository<StorageGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly StringLengthCache _stringLengthCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly StorageValidator _validator;
    private readonly StorageDetailViewModel _vm;

    public StorageDetailViewModelTests()
    {
        _service = Substitute.For<IRepository<StorageGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;

        _validator = new StorageValidator();

        _vm = new StorageDetailViewModel(
            _service,
            _eventAggregator,
            _stringLengthCache,
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

    private static StorageGraphQLModel CreateSampleStorage() => new()
    {
        Id = 10,
        Name = "Bodega Principal",
        Address = "Calle 1 # 2-3",
        Status = "ACTIVE",
        CompanyLocation = new CompanyLocationGraphQLModel { Id = 5 },
        City = new CityGraphQLModel
        {
            Id = 1,
            Department = new DepartmentGraphQLModel
            {
                Id = 1,
                Country = new CountryGraphQLModel { Id = 1 }
            }
        }
    };

    #region Construction

    [Fact]
    public void Constructor_NullService_Throws()
    {
        System.Action act = () => new StorageDetailViewModel(
            null!, _eventAggregator, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("storageService");
    }

    [Fact]
    public void Constructor_NullValidator_Throws()
    {
        System.Action act = () => new StorageDetailViewModel(
            _service, _eventAggregator, _stringLengthCache, _joinableTaskFactory, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("validator");
    }

    [Fact]
    public void Constructor_SetsDialogDimensions()
    {
        _vm.DialogWidth.Should().Be(540);
        _vm.DialogHeight.Should().Be(552);
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
        _vm.Address.Should().BeEmpty();
        _vm.Status.Should().Be("ACTIVE");
        _vm.CompanyLocationId.Should().Be(5);
    }

    [Fact]
    public void SetForNew_LoadsCountriesAndPicksDefaultColombia()
    {
        _vm.SetForNew(parentCompanyLocationId: 5, SampleCountries());

        _vm.Countries.Should().HaveCount(1);
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
        _vm.SetForEdit(CreateSampleStorage(), SampleCountries());

        _vm.Id.Should().Be(10);
        _vm.IsNewRecord.Should().BeFalse();
        _vm.Name.Should().Be("Bodega Principal");
        _vm.Address.Should().Be("Calle 1 # 2-3");
        _vm.Status.Should().Be("ACTIVE");
        _vm.CompanyLocationId.Should().Be(5);
        _vm.SelectedCountry.Should().NotBeNull();
        _vm.SelectedDepartment.Should().NotBeNull();
        _vm.SelectedCity.Should().NotBeNull();
        _vm.SelectedCityId.Should().Be(1);
    }

    [Fact]
    public void SetForEdit_NoInitialChanges()
    {
        _vm.SetForEdit(CreateSampleStorage(), SampleCountries());

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AfterChangingName_ReturnsTrue()
    {
        _vm.SetForEdit(CreateSampleStorage(), SampleCountries());

        _vm.Name = "Bodega Modificada";

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_EmptyName_ReturnsFalse()
    {
        _vm.SetForEdit(CreateSampleStorage(), SampleCountries());

        _vm.Name = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoCity_ReturnsFalse()
    {
        _vm.SetForEdit(CreateSampleStorage(), SampleCountries());
        _vm.Name = "Modified";

        _vm.SelectedCity = null;

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_IsBusy_ReturnsFalse()
    {
        _vm.SetForEdit(CreateSampleStorage(), SampleCountries());
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
        _vm.GetErrors(nameof(StorageDetailViewModel.Name)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void Name_SetValid_ClearsError()
    {
        _vm.SetForNew(parentCompanyLocationId: 1, SampleCountries());
        _vm.Name = "";

        _vm.Name = "Bodega";

        _vm.GetErrors(nameof(StorageDetailViewModel.Name)).Cast<string>()
            .Should().BeEmpty();
    }

    #endregion

    #region Status (tri-state)

    [Fact]
    public void Status_DefaultIsActive()
    {
        _vm.SetForNew(1, SampleCountries());
        _vm.IsStatusActive.Should().BeTrue();
        _vm.IsStatusReadOnly.Should().BeFalse();
        _vm.IsStatusInactive.Should().BeFalse();
    }

    [Fact]
    public void Status_SetReadOnly_PropagatesCorrectly()
    {
        _vm.SetForEdit(CreateSampleStorage(), SampleCountries());

        _vm.IsStatusReadOnly = true;

        _vm.Status.Should().Be("READ_ONLY");
        _vm.IsStatusActive.Should().BeFalse();
        _vm.IsStatusReadOnly.Should().BeTrue();
        _vm.IsStatusInactive.Should().BeFalse();
    }

    [Fact]
    public void Status_SetInactive_PropagatesCorrectly()
    {
        _vm.SetForEdit(CreateSampleStorage(), SampleCountries());

        _vm.IsStatusInactive = true;

        _vm.Status.Should().Be("INACTIVE");
        _vm.IsStatusActive.Should().BeFalse();
        _vm.IsStatusInactive.Should().BeTrue();
    }

    #endregion

    #region Geography Cascade

    [Fact]
    public void SelectedCountry_Change_UpdatesDepartment()
    {
        _vm.SetForEdit(CreateSampleStorage(), SampleCountries());
        CountryGraphQLModel newCountry = new()
        {
            Id = 2,
            Code = "USA",
            Departments = [new DepartmentGraphQLModel { Id = 99, Cities = [new CityGraphQLModel { Id = 88 }] }]
        };

        _vm.SelectedCountry = newCountry;

        _vm.SelectedDepartment.Should().NotBeNull();
        _vm.SelectedDepartment!.Id.Should().Be(99);
    }

    #endregion

    #region ExecuteSaveAsync

    [Fact]
    public async Task ExecuteSaveAsync_NewRecord_CallsCreateAsync()
    {
        UpsertResponseType<StorageGraphQLModel> expectedResult = new()
        {
            Entity = CreateSampleStorage(),
            Success = true,
            Message = "OK"
        };
        _service.CreateAsync<UpsertResponseType<StorageGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForNew(parentCompanyLocationId: 5, SampleCountries());
        _vm.Name = "Test";

        UpsertResponseType<StorageGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).CreateAsync<UpsertResponseType<StorageGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSaveAsync_ExistingRecord_CallsUpdateAsync()
    {
        UpsertResponseType<StorageGraphQLModel> expectedResult = new()
        {
            Entity = CreateSampleStorage(),
            Success = true,
            Message = "OK"
        };
        _service.UpdateAsync<UpsertResponseType<StorageGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForEdit(CreateSampleStorage(), SampleCountries());
        _vm.Name = "Modified";

        UpsertResponseType<StorageGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).UpdateAsync<UpsertResponseType<StorageGraphQLModel>>(
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

        changedProperties.Should().Contain(nameof(StorageDetailViewModel.Name));
        changedProperties.Should().Contain(nameof(StorageDetailViewModel.CanSave));
    }

    #endregion
}
