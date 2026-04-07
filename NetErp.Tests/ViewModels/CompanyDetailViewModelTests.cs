using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Common.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Global.CostCenters.Validators;
using NetErp.Global.CostCenters.ViewModels;
using NetErp.Helpers;
using NSubstitute;
using Xunit;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.ViewModels;

public class CompanyDetailViewModelTests
{
    private readonly IRepository<CompanyGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly IDialogService _dialogService;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly CompanyValidator _validator;
    private readonly CompanyDetailViewModel _vm;

    public CompanyDetailViewModelTests()
    {
        _service = Substitute.For<IRepository<CompanyGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _dialogService = Substitute.For<IDialogService>();

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;

        _validator = new CompanyValidator();

        _vm = new CompanyDetailViewModel(
            _service,
            _eventAggregator,
            _dialogService,
            _joinableTaskFactory,
            _validator);
    }

    private static CompanyGraphQLModel CreateSampleCompany() => new()
    {
        Id = 3,
        CompanyEntity = new AccountingEntityGraphQLModel
        {
            Id = 42,
            SearchName = "ACME S.A.S."
        }
    };

    #region Construction

    [Fact]
    public void Constructor_NullService_Throws()
    {
        System.Action act = () => new CompanyDetailViewModel(
            null!, _eventAggregator, _dialogService, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("companyService");
    }

    [Fact]
    public void Constructor_NullValidator_Throws()
    {
        System.Action act = () => new CompanyDetailViewModel(
            _service, _eventAggregator, _dialogService, _joinableTaskFactory, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("validator");
    }

    [Fact]
    public void Constructor_SetsDialogDimensions()
    {
        _vm.DialogWidth.Should().Be(540);
        _vm.DialogHeight.Should().Be(280);
    }

    #endregion

    #region SetForEdit (Update-only — no SetForNew)

    [Fact]
    public void SetForEdit_PopulatesFromModel()
    {
        _vm.SetForEdit(CreateSampleCompany());

        _vm.Id.Should().Be(3);
        _vm.IsNewRecord.Should().BeFalse();
        _vm.AccountingEntityCompanyId.Should().Be(42);
        _vm.AccountingEntityCompanySearchName.Should().Be("ACME S.A.S.");
    }

    [Fact]
    public void SetForEdit_NullCompanyEntity_DefaultsToZero()
    {
        CompanyGraphQLModel entity = new() { Id = 1, CompanyEntity = null! };

        _vm.SetForEdit(entity);

        _vm.AccountingEntityCompanyId.Should().Be(0);
        _vm.AccountingEntityCompanySearchName.Should().BeEmpty();
    }

    [Fact]
    public void SetForEdit_NoInitialChanges()
    {
        _vm.SetForEdit(CreateSampleCompany());

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AfterChangingAccountingEntity_ReturnsTrue()
    {
        _vm.SetForEdit(CreateSampleCompany());

        _vm.AccountingEntityCompanyId = 99;

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_ZeroAccountingEntity_ReturnsFalse()
    {
        _vm.SetForEdit(CreateSampleCompany());

        _vm.AccountingEntityCompanyId = 0;

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_IsBusy_ReturnsFalse()
    {
        _vm.SetForEdit(CreateSampleCompany());
        _vm.AccountingEntityCompanyId = 99;

        _vm.IsBusy = true;

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region Validation

    [Fact]
    public void AccountingEntityCompanyId_SetZero_AddsValidationError()
    {
        _vm.SetForEdit(CreateSampleCompany());

        _vm.AccountingEntityCompanyId = 0;

        _vm.HasErrors.Should().BeTrue();
        // Validator reports error under display field name (see CompanyDetailViewModel.ValidateAccountingEntity)
        _vm.GetErrors(nameof(CompanyDetailViewModel.AccountingEntityCompanySearchName)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void AccountingEntityCompanyId_SetValid_ClearsError()
    {
        _vm.SetForEdit(CreateSampleCompany());
        _vm.AccountingEntityCompanyId = 0;

        _vm.AccountingEntityCompanyId = 50;

        _vm.GetErrors(nameof(CompanyDetailViewModel.AccountingEntityCompanySearchName)).Cast<string>()
            .Should().BeEmpty();
    }

    #endregion

    #region ExecuteSaveAsync

    [Fact]
    public async Task ExecuteSaveAsync_CallsUpdateAsync()
    {
        UpsertResponseType<CompanyGraphQLModel> expectedResult = new()
        {
            Entity = CreateSampleCompany(),
            Success = true,
            Message = "OK"
        };
        _service.UpdateAsync<UpsertResponseType<CompanyGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForEdit(CreateSampleCompany());
        _vm.AccountingEntityCompanyId = 99;

        UpsertResponseType<CompanyGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).UpdateAsync<UpsertResponseType<CompanyGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region PropertyChanged

    [Fact]
    public void AccountingEntityCompanyId_Set_RaisesPropertyChanged()
    {
        _vm.SetForEdit(CreateSampleCompany());
        List<string> changedProperties = [];
        _vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        _vm.AccountingEntityCompanyId = 100;

        changedProperties.Should().Contain(nameof(CompanyDetailViewModel.AccountingEntityCompanyId));
        changedProperties.Should().Contain(nameof(CompanyDetailViewModel.CanSave));
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
        _vm.Id = 3;
        _vm.IsNewRecord.Should().BeFalse();
    }

    #endregion
}
