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
using Models.Inventory;
using NetErp.Helpers.Cache;
using NetErp.Inventory.CatalogItems.Validators;
using NetErp.Inventory.CatalogItems.ViewModels;
using NSubstitute;
using Xunit;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.ViewModels;

public class CatalogDetailViewModelTests
{
    private readonly IRepository<CatalogGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly StringLengthCache _stringLengthCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly CatalogValidator _validator;
    private readonly CatalogDetailViewModel _vm;

    public CatalogDetailViewModelTests()
    {
        _service = Substitute.For<IRepository<CatalogGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;

        _validator = new CatalogValidator();

        _vm = new CatalogDetailViewModel(
            _service,
            _eventAggregator,
            _stringLengthCache,
            _joinableTaskFactory,
            _validator);
    }

    #region Construction

    [Fact]
    public void Constructor_NullService_Throws()
    {
        System.Action act = () => new CatalogDetailViewModel(
            null!, _eventAggregator, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("catalogService");
    }

    [Fact]
    public void Constructor_NullStringLengthCache_Throws()
    {
        System.Action act = () => new CatalogDetailViewModel(
            _service, _eventAggregator, null!, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("stringLengthCache");
    }

    [Fact]
    public void Constructor_NullValidator_Throws()
    {
        System.Action act = () => new CatalogDetailViewModel(
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
        _vm.SetForNew();

        _vm.Id.Should().Be(0);
        _vm.Name.Should().BeEmpty();
        _vm.IsNewRecord.Should().BeTrue();
    }

    [Fact]
    public void SetForNew_NoInitialChanges()
    {
        _vm.SetForNew();

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region SetForEdit

    [Fact]
    public void SetForEdit_PopulatesFromModel()
    {
        CatalogGraphQLModel entity = new() { Id = 7, Name = "Catálogo Principal" };

        _vm.SetForEdit(entity);

        _vm.Id.Should().Be(7);
        _vm.Name.Should().Be("Catálogo Principal");
        _vm.IsNewRecord.Should().BeFalse();
    }

    [Fact]
    public void SetForEdit_NoInitialChanges()
    {
        CatalogGraphQLModel entity = new() { Id = 7, Name = "Catálogo Principal" };

        _vm.SetForEdit(entity);

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AfterChangingName_ReturnsTrue()
    {
        _vm.SetForEdit(new CatalogGraphQLModel { Id = 7, Name = "Original" });

        _vm.Name = "Modificado";

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_EmptyName_ReturnsFalse()
    {
        _vm.SetForEdit(new CatalogGraphQLModel { Id = 7, Name = "Original" });

        _vm.Name = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_IsBusy_ReturnsFalse()
    {
        _vm.SetForEdit(new CatalogGraphQLModel { Id = 7, Name = "Original" });
        _vm.Name = "Modificado";

        _vm.IsBusy = true;

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region Validation

    [Fact]
    public void Name_SetEmpty_AddsValidationError()
    {
        _vm.SetForNew();
        _vm.Name = "Valid";

        _vm.Name = "";

        _vm.HasErrors.Should().BeTrue();
        _vm.GetErrors(nameof(CatalogDetailViewModel.Name)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void Name_SetValid_ClearsError()
    {
        _vm.SetForNew();
        _vm.Name = "";
        _vm.Name = "";  // no-op — value already empty
        // Force error by triggering validation manually via entering then clearing
        _vm.Name = "X";
        _vm.Name = "";
        _vm.HasErrors.Should().BeTrue();

        _vm.Name = "Catálogo";

        _vm.GetErrors(nameof(CatalogDetailViewModel.Name)).Cast<string>()
            .Should().BeEmpty();
    }

    [Fact]
    public void ErrorsChanged_FiredOnValidation()
    {
        _vm.SetForNew();
        _vm.Name = "Valid";
        List<string> firedProperties = [];
        _vm.ErrorsChanged += (_, e) => firedProperties.Add(e.PropertyName!);

        _vm.Name = "";

        firedProperties.Should().Contain(nameof(CatalogDetailViewModel.Name));
    }

    #endregion

    #region ExecuteSaveAsync

    [Fact]
    public async Task ExecuteSaveAsync_NewRecord_CallsCreateAsync()
    {
        UpsertResponseType<CatalogGraphQLModel> expectedResult = new()
        {
            Entity = new CatalogGraphQLModel { Id = 1, Name = "Nuevo" },
            Success = true,
            Message = "OK"
        };
        _service.CreateAsync<UpsertResponseType<CatalogGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForNew();
        _vm.Name = "Nuevo";

        UpsertResponseType<CatalogGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).CreateAsync<UpsertResponseType<CatalogGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSaveAsync_ExistingRecord_CallsUpdateAsync()
    {
        UpsertResponseType<CatalogGraphQLModel> expectedResult = new()
        {
            Entity = new CatalogGraphQLModel { Id = 7, Name = "Modificado" },
            Success = true,
            Message = "OK"
        };
        _service.UpdateAsync<UpsertResponseType<CatalogGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForEdit(new CatalogGraphQLModel { Id = 7, Name = "Original" });
        _vm.Name = "Modificado";

        UpsertResponseType<CatalogGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).UpdateAsync<UpsertResponseType<CatalogGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region PropertyChanged

    [Fact]
    public void Name_Set_RaisesPropertyChanged()
    {
        _vm.SetForNew();
        List<string> changedProperties = [];
        _vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        _vm.Name = "Test";

        changedProperties.Should().Contain(nameof(CatalogDetailViewModel.Name));
        changedProperties.Should().Contain(nameof(CatalogDetailViewModel.CanSave));
    }

    #endregion
}
