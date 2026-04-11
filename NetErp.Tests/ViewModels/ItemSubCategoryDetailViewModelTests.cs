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

public class ItemSubCategoryDetailViewModelTests
{
    private readonly IRepository<ItemSubCategoryGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly StringLengthCache _stringLengthCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly ItemSubCategoryValidator _validator;
    private readonly ItemSubCategoryDetailViewModel _vm;

    public ItemSubCategoryDetailViewModelTests()
    {
        _service = Substitute.For<IRepository<ItemSubCategoryGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;

        _validator = new ItemSubCategoryValidator();

        _vm = new ItemSubCategoryDetailViewModel(
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
        System.Action act = () => new ItemSubCategoryDetailViewModel(
            null!, _eventAggregator, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("itemSubCategoryService");
    }

    [Fact]
    public void Constructor_NullValidator_Throws()
    {
        System.Action act = () => new ItemSubCategoryDetailViewModel(
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
        _vm.SetForNew(parentItemCategoryId: 5);

        _vm.Id.Should().Be(0);
        _vm.Name.Should().BeEmpty();
        _vm.ItemCategoryId.Should().Be(5);
        _vm.IsNewRecord.Should().BeTrue();
    }

    [Fact]
    public void SetForNew_NoInitialChanges()
    {
        _vm.SetForNew(parentItemCategoryId: 5);

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region SetForEdit

    [Fact]
    public void SetForEdit_PopulatesFromModel()
    {
        ItemSubCategoryGraphQLModel entity = new()
        {
            Id = 9,
            Name = "Gaseosas",
            ItemCategory = new ItemCategoryGraphQLModel { Id = 5 }
        };

        _vm.SetForEdit(entity);

        _vm.Id.Should().Be(9);
        _vm.Name.Should().Be("Gaseosas");
        _vm.ItemCategoryId.Should().Be(5);
        _vm.IsNewRecord.Should().BeFalse();
    }

    [Fact]
    public void SetForEdit_NullItemCategory_DefaultsItemCategoryIdToZero()
    {
        ItemSubCategoryGraphQLModel entity = new()
        {
            Id = 9,
            Name = "Gaseosas",
            ItemCategory = null
        };

        _vm.SetForEdit(entity);

        _vm.ItemCategoryId.Should().Be(0);
    }

    [Fact]
    public void SetForEdit_NoInitialChanges()
    {
        ItemSubCategoryGraphQLModel entity = new()
        {
            Id = 9,
            Name = "Gaseosas",
            ItemCategory = new ItemCategoryGraphQLModel { Id = 5 }
        };

        _vm.SetForEdit(entity);

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AfterChangingName_ReturnsTrue()
    {
        _vm.SetForEdit(new ItemSubCategoryGraphQLModel
        {
            Id = 9,
            Name = "Gaseosas",
            ItemCategory = new ItemCategoryGraphQLModel { Id = 5 }
        });

        _vm.Name = "Gaseosas Light";

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_EmptyName_ReturnsFalse()
    {
        _vm.SetForEdit(new ItemSubCategoryGraphQLModel
        {
            Id = 9,
            Name = "Gaseosas",
            ItemCategory = new ItemCategoryGraphQLModel { Id = 5 }
        });

        _vm.Name = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_IsBusy_ReturnsFalse()
    {
        _vm.SetForEdit(new ItemSubCategoryGraphQLModel
        {
            Id = 9,
            Name = "Gaseosas",
            ItemCategory = new ItemCategoryGraphQLModel { Id = 5 }
        });
        _vm.Name = "Modified";

        _vm.IsBusy = true;

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region Validation

    [Fact]
    public void Name_SetEmpty_AddsValidationError()
    {
        _vm.SetForNew(parentItemCategoryId: 5);
        _vm.Name = "Valid";

        _vm.Name = "";

        _vm.HasErrors.Should().BeTrue();
        _vm.GetErrors(nameof(ItemSubCategoryDetailViewModel.Name)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void Name_SetValid_ClearsError()
    {
        _vm.SetForNew(parentItemCategoryId: 5);
        _vm.Name = "X";
        _vm.Name = "";
        _vm.HasErrors.Should().BeTrue();

        _vm.Name = "Gaseosas";

        _vm.GetErrors(nameof(ItemSubCategoryDetailViewModel.Name)).Cast<string>()
            .Should().BeEmpty();
    }

    #endregion

    #region ExecuteSaveAsync

    [Fact]
    public async Task ExecuteSaveAsync_NewRecord_CallsCreateAsync()
    {
        UpsertResponseType<ItemSubCategoryGraphQLModel> expectedResult = new()
        {
            Entity = new ItemSubCategoryGraphQLModel { Id = 1, Name = "Nueva" },
            Success = true,
            Message = "OK"
        };
        _service.CreateAsync<UpsertResponseType<ItemSubCategoryGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForNew(parentItemCategoryId: 5);
        _vm.Name = "Nueva";

        UpsertResponseType<ItemSubCategoryGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).CreateAsync<UpsertResponseType<ItemSubCategoryGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSaveAsync_ExistingRecord_CallsUpdateAsync()
    {
        UpsertResponseType<ItemSubCategoryGraphQLModel> expectedResult = new()
        {
            Entity = new ItemSubCategoryGraphQLModel { Id = 9, Name = "Modificada" },
            Success = true,
            Message = "OK"
        };
        _service.UpdateAsync<UpsertResponseType<ItemSubCategoryGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForEdit(new ItemSubCategoryGraphQLModel
        {
            Id = 9,
            Name = "Gaseosas",
            ItemCategory = new ItemCategoryGraphQLModel { Id = 5 }
        });
        _vm.Name = "Modificada";

        UpsertResponseType<ItemSubCategoryGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).UpdateAsync<UpsertResponseType<ItemSubCategoryGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    #endregion
}
