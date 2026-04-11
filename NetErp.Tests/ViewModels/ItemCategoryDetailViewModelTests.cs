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

public class ItemCategoryDetailViewModelTests
{
    private readonly IRepository<ItemCategoryGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly StringLengthCache _stringLengthCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly ItemCategoryValidator _validator;
    private readonly ItemCategoryDetailViewModel _vm;

    public ItemCategoryDetailViewModelTests()
    {
        _service = Substitute.For<IRepository<ItemCategoryGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;

        _validator = new ItemCategoryValidator();

        _vm = new ItemCategoryDetailViewModel(
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
        System.Action act = () => new ItemCategoryDetailViewModel(
            null!, _eventAggregator, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("itemCategoryService");
    }

    [Fact]
    public void Constructor_NullValidator_Throws()
    {
        System.Action act = () => new ItemCategoryDetailViewModel(
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
        _vm.SetForNew(parentItemTypeId: 3);

        _vm.Id.Should().Be(0);
        _vm.Name.Should().BeEmpty();
        _vm.ItemTypeId.Should().Be(3);
        _vm.IsNewRecord.Should().BeTrue();
    }

    [Fact]
    public void SetForNew_NoInitialChanges()
    {
        _vm.SetForNew(parentItemTypeId: 3);

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region SetForEdit

    [Fact]
    public void SetForEdit_PopulatesFromModel()
    {
        ItemCategoryGraphQLModel entity = new()
        {
            Id = 5,
            Name = "Bebidas",
            ItemType = new ItemTypeGraphQLModel { Id = 3 }
        };

        _vm.SetForEdit(entity);

        _vm.Id.Should().Be(5);
        _vm.Name.Should().Be("Bebidas");
        _vm.ItemTypeId.Should().Be(3);
        _vm.IsNewRecord.Should().BeFalse();
    }

    [Fact]
    public void SetForEdit_NullItemType_DefaultsItemTypeIdToZero()
    {
        ItemCategoryGraphQLModel entity = new()
        {
            Id = 5,
            Name = "Bebidas",
            ItemType = null
        };

        _vm.SetForEdit(entity);

        _vm.ItemTypeId.Should().Be(0);
    }

    [Fact]
    public void SetForEdit_NoInitialChanges()
    {
        ItemCategoryGraphQLModel entity = new()
        {
            Id = 5,
            Name = "Bebidas",
            ItemType = new ItemTypeGraphQLModel { Id = 3 }
        };

        _vm.SetForEdit(entity);

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AfterChangingName_ReturnsTrue()
    {
        _vm.SetForEdit(new ItemCategoryGraphQLModel
        {
            Id = 5,
            Name = "Bebidas",
            ItemType = new ItemTypeGraphQLModel { Id = 3 }
        });

        _vm.Name = "Bebidas Frías";

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_EmptyName_ReturnsFalse()
    {
        _vm.SetForEdit(new ItemCategoryGraphQLModel
        {
            Id = 5,
            Name = "Bebidas",
            ItemType = new ItemTypeGraphQLModel { Id = 3 }
        });

        _vm.Name = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_IsBusy_ReturnsFalse()
    {
        _vm.SetForEdit(new ItemCategoryGraphQLModel
        {
            Id = 5,
            Name = "Bebidas",
            ItemType = new ItemTypeGraphQLModel { Id = 3 }
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
        _vm.SetForNew(parentItemTypeId: 3);
        _vm.Name = "Valid";

        _vm.Name = "";

        _vm.HasErrors.Should().BeTrue();
        _vm.GetErrors(nameof(ItemCategoryDetailViewModel.Name)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void Name_SetValid_ClearsError()
    {
        _vm.SetForNew(parentItemTypeId: 3);
        _vm.Name = "X";
        _vm.Name = "";
        _vm.HasErrors.Should().BeTrue();

        _vm.Name = "Bebidas";

        _vm.GetErrors(nameof(ItemCategoryDetailViewModel.Name)).Cast<string>()
            .Should().BeEmpty();
    }

    #endregion

    #region ExecuteSaveAsync

    [Fact]
    public async Task ExecuteSaveAsync_NewRecord_CallsCreateAsync()
    {
        UpsertResponseType<ItemCategoryGraphQLModel> expectedResult = new()
        {
            Entity = new ItemCategoryGraphQLModel { Id = 1, Name = "Nueva" },
            Success = true,
            Message = "OK"
        };
        _service.CreateAsync<UpsertResponseType<ItemCategoryGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForNew(parentItemTypeId: 3);
        _vm.Name = "Nueva";

        UpsertResponseType<ItemCategoryGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).CreateAsync<UpsertResponseType<ItemCategoryGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSaveAsync_ExistingRecord_CallsUpdateAsync()
    {
        UpsertResponseType<ItemCategoryGraphQLModel> expectedResult = new()
        {
            Entity = new ItemCategoryGraphQLModel { Id = 5, Name = "Modificada" },
            Success = true,
            Message = "OK"
        };
        _service.UpdateAsync<UpsertResponseType<ItemCategoryGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForEdit(new ItemCategoryGraphQLModel
        {
            Id = 5,
            Name = "Bebidas",
            ItemType = new ItemTypeGraphQLModel { Id = 3 }
        });
        _vm.Name = "Modificada";

        UpsertResponseType<ItemCategoryGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).UpdateAsync<UpsertResponseType<ItemCategoryGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    #endregion
}
