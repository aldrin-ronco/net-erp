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

public class CompanyLocationDetailViewModelTests
{
    private readonly IRepository<CompanyLocationGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly StringLengthCache _stringLengthCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly CompanyLocationValidator _validator;
    private readonly CompanyLocationDetailViewModel _vm;

    public CompanyLocationDetailViewModelTests()
    {
        _service = Substitute.For<IRepository<CompanyLocationGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;

        _validator = new CompanyLocationValidator();

        _vm = new CompanyLocationDetailViewModel(
            _service,
            _eventAggregator,
            _stringLengthCache,
            _joinableTaskFactory,
            _validator);
    }

    private static CompanyLocationGraphQLModel CreateSampleEntity() => new()
    {
        Id = 5,
        Name = "Sede Centro",
        Company = new CompanyGraphQLModel { Id = 1 }
    };

    #region Construction

    [Fact]
    public void Constructor_NullService_Throws()
    {
        System.Action act = () => new CompanyLocationDetailViewModel(
            null!, _eventAggregator, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("companyLocationService");
    }

    [Fact]
    public void Constructor_NullValidator_Throws()
    {
        System.Action act = () => new CompanyLocationDetailViewModel(
            _service, _eventAggregator, _stringLengthCache, _joinableTaskFactory, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("validator");
    }

    [Fact]
    public void Constructor_SetsDialogDimensions()
    {
        _vm.DialogWidth.Should().Be(460);
        _vm.DialogHeight.Should().Be(240);
    }

    #endregion

    #region SetForNew

    [Fact]
    public void SetForNew_SetsDefaults()
    {
        _vm.SetForNew(parentCompanyId: 7);

        _vm.Id.Should().Be(0);
        _vm.IsNewRecord.Should().BeTrue();
        _vm.Name.Should().BeEmpty();
        _vm.CompanyId.Should().Be(7);
    }

    [Fact]
    public void SetForNew_NoInitialChanges()
    {
        _vm.SetForNew(parentCompanyId: 7);

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region SetForEdit

    [Fact]
    public void SetForEdit_PopulatesFromModel()
    {
        _vm.SetForEdit(CreateSampleEntity());

        _vm.Id.Should().Be(5);
        _vm.IsNewRecord.Should().BeFalse();
        _vm.Name.Should().Be("Sede Centro");
        _vm.CompanyId.Should().Be(1);
    }

    [Fact]
    public void SetForEdit_NoInitialChanges()
    {
        _vm.SetForEdit(CreateSampleEntity());

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AfterChangingName_ReturnsTrue()
    {
        _vm.SetForEdit(CreateSampleEntity());

        _vm.Name = "Sede Norte";

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_EmptyName_ReturnsFalse()
    {
        _vm.SetForEdit(CreateSampleEntity());

        _vm.Name = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_IsBusy_ReturnsFalse()
    {
        _vm.SetForEdit(CreateSampleEntity());
        _vm.Name = "Modified";

        _vm.IsBusy = true;

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region Validation

    [Fact]
    public void Name_SetEmpty_AddsValidationError()
    {
        _vm.SetForNew(parentCompanyId: 1);
        _vm.Name = "Valid";

        _vm.Name = "";

        _vm.HasErrors.Should().BeTrue();
        _vm.GetErrors(nameof(CompanyLocationDetailViewModel.Name)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void Name_SetValid_ClearsError()
    {
        _vm.SetForNew(parentCompanyId: 1);
        _vm.Name = "";

        _vm.Name = "Sede Norte";

        _vm.GetErrors(nameof(CompanyLocationDetailViewModel.Name)).Cast<string>()
            .Should().BeEmpty();
    }

    [Fact]
    public void ErrorsChanged_FiredOnValidation()
    {
        _vm.SetForNew(parentCompanyId: 1);
        _vm.Name = "Valid";
        List<string> firedProperties = [];
        _vm.ErrorsChanged += (_, e) => firedProperties.Add(e.PropertyName!);

        _vm.Name = "";

        firedProperties.Should().Contain(nameof(CompanyLocationDetailViewModel.Name));
    }

    #endregion

    #region ExecuteSaveAsync

    [Fact]
    public async Task ExecuteSaveAsync_NewRecord_CallsCreateAsync()
    {
        UpsertResponseType<CompanyLocationGraphQLModel> expectedResult = new()
        {
            Entity = CreateSampleEntity(),
            Success = true,
            Message = "OK"
        };
        _service.CreateAsync<UpsertResponseType<CompanyLocationGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForNew(parentCompanyId: 1);
        _vm.Name = "Sede Test";

        UpsertResponseType<CompanyLocationGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).CreateAsync<UpsertResponseType<CompanyLocationGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSaveAsync_ExistingRecord_CallsUpdateAsync()
    {
        UpsertResponseType<CompanyLocationGraphQLModel> expectedResult = new()
        {
            Entity = CreateSampleEntity(),
            Success = true,
            Message = "OK"
        };
        _service.UpdateAsync<UpsertResponseType<CompanyLocationGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForEdit(CreateSampleEntity());
        _vm.Name = "Sede Modificada";

        UpsertResponseType<CompanyLocationGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).UpdateAsync<UpsertResponseType<CompanyLocationGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region PropertyChanged

    [Fact]
    public void Name_Set_RaisesPropertyChanged()
    {
        _vm.SetForNew(parentCompanyId: 1);
        List<string> changedProperties = [];
        _vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        _vm.Name = "Test";

        changedProperties.Should().Contain(nameof(CompanyLocationDetailViewModel.Name));
        changedProperties.Should().Contain(nameof(CompanyLocationDetailViewModel.CanSave));
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
        _vm.Id = 5;
        _vm.IsNewRecord.Should().BeFalse();
    }

    #endregion
}
