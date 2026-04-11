using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Common.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using Models.Inventory;
using NetErp.Helpers.Cache;
using NetErp.Inventory.CatalogItems.Validators;
using NetErp.Inventory.CatalogItems.ViewModels;
using NSubstitute;
using Xunit;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.ViewModels;

public class ItemTypeDetailViewModelTests
{
    private readonly IRepository<ItemTypeGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly StringLengthCache _stringLengthCache;
    private readonly MeasurementUnitCache _measurementUnitCache;
    private readonly AccountingGroupCache _accountingGroupCache;
    private readonly CatalogCache _catalogCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly ItemTypeValidator _validator;
    private readonly ItemTypeDetailViewModel _vm;

    public ItemTypeDetailViewModelTests()
    {
        _service = Substitute.For<IRepository<ItemTypeGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;

        _validator = new ItemTypeValidator();

        // Preload caches with sample data so combos have items and the prefix pool has context.
        _measurementUnitCache = BuildMeasurementUnitCache();
        _accountingGroupCache = BuildAccountingGroupCache();
        _catalogCache = BuildCatalogCache();

        _vm = new ItemTypeDetailViewModel(
            _service,
            _eventAggregator,
            _stringLengthCache,
            _measurementUnitCache,
            _accountingGroupCache,
            _catalogCache,
            _joinableTaskFactory,
            _validator);
    }

    #region Cache builders

    private static MeasurementUnitCache BuildMeasurementUnitCache()
    {
        IRepository<MeasurementUnitGraphQLModel> repo = Substitute.For<IRepository<MeasurementUnitGraphQLModel>>();
        repo.GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(new PageType<MeasurementUnitGraphQLModel>
            {
                Entries =
                [
                    new MeasurementUnitGraphQLModel { Id = 1, Name = "Unidad", Abbreviation = "Un" },
                    new MeasurementUnitGraphQLModel { Id = 2, Name = "Kilogramo", Abbreviation = "Kg" }
                ]
            });

        MeasurementUnitCache cache = new(repo, Substitute.For<IEventAggregator>());
        cache.EnsureLoadedAsync().GetAwaiter().GetResult();
        return cache;
    }

    private static AccountingGroupCache BuildAccountingGroupCache()
    {
        IRepository<AccountingGroupGraphQLModel> repo = Substitute.For<IRepository<AccountingGroupGraphQLModel>>();
        repo.GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(new PageType<AccountingGroupGraphQLModel>
            {
                Entries =
                [
                    new AccountingGroupGraphQLModel { Id = 10, Name = "Mercancía" },
                    new AccountingGroupGraphQLModel { Id = 11, Name = "Servicios" }
                ]
            });

        AccountingGroupCache cache = new(repo, Substitute.For<IEventAggregator>());
        cache.EnsureLoadedAsync().GetAwaiter().GetResult();
        return cache;
    }

    private static CatalogCache BuildCatalogCache()
    {
        IRepository<CatalogGraphQLModel> repo = Substitute.For<IRepository<CatalogGraphQLModel>>();
        repo.GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(new PageType<CatalogGraphQLModel>
            {
                Entries =
                [
                    new CatalogGraphQLModel
                    {
                        Id = 1,
                        Name = "Principal",
                        ItemTypes =
                        [
                            new ItemTypeGraphQLModel { Id = 100, Name = "Producto", PrefixChar = "P" },
                            new ItemTypeGraphQLModel { Id = 101, Name = "Servicio", PrefixChar = "S" }
                        ]
                    }
                ]
            });

        CatalogCache cache = new(repo, Substitute.For<IEventAggregator>());
        cache.EnsureLoadedAsync().GetAwaiter().GetResult();
        return cache;
    }

    #endregion

    #region Construction

    [Fact]
    public void Constructor_NullService_Throws()
    {
        System.Action act = () => new ItemTypeDetailViewModel(
            null!, _eventAggregator, _stringLengthCache,
            _measurementUnitCache, _accountingGroupCache, _catalogCache,
            _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("itemTypeService");
    }

    [Fact]
    public void Constructor_NullMeasurementUnitCache_Throws()
    {
        System.Action act = () => new ItemTypeDetailViewModel(
            _service, _eventAggregator, _stringLengthCache,
            null!, _accountingGroupCache, _catalogCache,
            _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("measurementUnitCache");
    }

    [Fact]
    public void Constructor_NullValidator_Throws()
    {
        System.Action act = () => new ItemTypeDetailViewModel(
            _service, _eventAggregator, _stringLengthCache,
            _measurementUnitCache, _accountingGroupCache, _catalogCache,
            _joinableTaskFactory, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("validator");
    }

    [Fact]
    public void Constructor_SetsDialogDimensions()
    {
        _vm.DialogWidth.Should().Be(520);
        _vm.DialogHeight.Should().Be(380);
    }

    #endregion

    #region SetForNew

    [Fact]
    public void SetForNew_SetsDefaults()
    {
        _vm.SetForNew(parentCatalogId: 1);

        _vm.Id.Should().Be(0);
        _vm.Name.Should().BeEmpty();
        _vm.PrefixChar.Should().BeEmpty();
        _vm.StockControl.Should().BeFalse();
        _vm.StockControlEnable.Should().BeTrue();
        _vm.CatalogId.Should().Be(1);
        _vm.DefaultMeasurementUnitId.Should().Be(0);
        _vm.DefaultAccountingGroupId.Should().Be(0);
        _vm.IsNewRecord.Should().BeTrue();
    }

    [Fact]
    public void SetForNew_LoadsComboSources()
    {
        _vm.SetForNew(parentCatalogId: 1);

        _vm.MeasurementUnits.Should().HaveCount(2);
        _vm.AccountingGroups.Should().HaveCount(2);
    }

    [Fact]
    public void SetForNew_PrefixPoolExcludesUsedLetters()
    {
        // Catalog has two ItemTypes with prefixes P and S — the pool should exclude both
        _vm.SetForNew(parentCatalogId: 1);

        _vm.AvailablePrefixChars.Should().NotContain("P");
        _vm.AvailablePrefixChars.Should().NotContain("S");
        _vm.AvailablePrefixChars.Should().Contain("A");
        _vm.AvailablePrefixChars.Should().HaveCount(24);  // 26 - 2 used
    }

    [Fact]
    public void SetForNew_NoInitialChanges()
    {
        _vm.SetForNew(parentCatalogId: 1);

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region SetForEdit

    [Fact]
    public void SetForEdit_PopulatesFromModel()
    {
        ItemTypeGraphQLModel entity = new()
        {
            Id = 100,
            Name = "Producto",
            PrefixChar = "P",
            StockControl = true,
            Catalog = new CatalogGraphQLModel { Id = 1 },
            DefaultMeasurementUnit = new MeasurementUnitGraphQLModel { Id = 2 },
            DefaultAccountingGroup = new AccountingGroupGraphQLModel { Id = 10 }
        };

        _vm.SetForEdit(entity);

        _vm.Id.Should().Be(100);
        _vm.Name.Should().Be("Producto");
        _vm.PrefixChar.Should().Be("P");
        _vm.StockControl.Should().BeTrue();
        _vm.StockControlEnable.Should().BeFalse();  // locked in edit mode
        _vm.CatalogId.Should().Be(1);
        _vm.DefaultMeasurementUnitId.Should().Be(2);
        _vm.DefaultAccountingGroupId.Should().Be(10);
        _vm.SelectedMeasurementUnit.Should().NotBeNull();
        _vm.SelectedMeasurementUnit!.Id.Should().Be(2);
        _vm.SelectedAccountingGroup.Should().NotBeNull();
        _vm.SelectedAccountingGroup!.Id.Should().Be(10);
        _vm.IsNewRecord.Should().BeFalse();
    }

    [Fact]
    public void SetForEdit_KeepsCurrentPrefixInPool()
    {
        // In edit mode, the current prefix must stay in the pool so the user can still select it
        ItemTypeGraphQLModel entity = new()
        {
            Id = 100,
            Name = "Producto",
            PrefixChar = "P",
            Catalog = new CatalogGraphQLModel { Id = 1 },
            DefaultMeasurementUnit = new MeasurementUnitGraphQLModel { Id = 2 },
            DefaultAccountingGroup = new AccountingGroupGraphQLModel { Id = 10 }
        };

        _vm.SetForEdit(entity);

        _vm.AvailablePrefixChars.Should().Contain("P");       // kept because it's the current
        _vm.AvailablePrefixChars.Should().NotContain("S");    // still used by another ItemType
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

        _vm.Name = "Producto Modificado";

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
    public void CanSave_InvalidPrefixChar_ReturnsFalse()
    {
        _vm.SetForEdit(CreateSampleEntity());
        _vm.Name = "Modified";

        _vm.PrefixChar = "ab";  // two chars, invalid

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_MissingMeasurementUnit_ReturnsFalse()
    {
        _vm.SetForEdit(CreateSampleEntity());
        _vm.Name = "Modified";

        _vm.SelectedMeasurementUnit = null;

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_MissingAccountingGroup_ReturnsFalse()
    {
        _vm.SetForEdit(CreateSampleEntity());
        _vm.Name = "Modified";

        _vm.SelectedAccountingGroup = null;

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
        _vm.SetForNew(parentCatalogId: 1);
        _vm.Name = "Valid";

        _vm.Name = "";

        _vm.HasErrors.Should().BeTrue();
        _vm.GetErrors(nameof(ItemTypeDetailViewModel.Name)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void PrefixChar_SetLowercase_AddsValidationError()
    {
        _vm.SetForNew(parentCatalogId: 1);

        _vm.PrefixChar = "a";

        _vm.GetErrors(nameof(ItemTypeDetailViewModel.PrefixChar)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void PrefixChar_SetUppercase_ClearsError()
    {
        _vm.SetForNew(parentCatalogId: 1);
        _vm.PrefixChar = "a";  // invalid

        _vm.PrefixChar = "A";

        _vm.GetErrors(nameof(ItemTypeDetailViewModel.PrefixChar)).Cast<string>()
            .Should().BeEmpty();
    }

    #endregion

    #region SelectedMeasurementUnit / SelectedAccountingGroup propagation

    [Fact]
    public void SelectedMeasurementUnit_Set_UpdatesDefaultMeasurementUnitId()
    {
        _vm.SetForNew(parentCatalogId: 1);

        _vm.SelectedMeasurementUnit = _vm.MeasurementUnits.First(x => x.Id == 2);

        _vm.DefaultMeasurementUnitId.Should().Be(2);
    }

    [Fact]
    public void SelectedAccountingGroup_Set_UpdatesDefaultAccountingGroupId()
    {
        _vm.SetForNew(parentCatalogId: 1);

        _vm.SelectedAccountingGroup = _vm.AccountingGroups.First(x => x.Id == 10);

        _vm.DefaultAccountingGroupId.Should().Be(10);
    }

    [Fact]
    public void SelectedMeasurementUnit_SetNull_ResetsDefaultMeasurementUnitIdToZero()
    {
        _vm.SetForEdit(CreateSampleEntity());

        _vm.SelectedMeasurementUnit = null;

        _vm.DefaultMeasurementUnitId.Should().Be(0);
    }

    #endregion

    #region ExecuteSaveAsync

    [Fact]
    public async Task ExecuteSaveAsync_NewRecord_CallsCreateAsync()
    {
        UpsertResponseType<ItemTypeGraphQLModel> expectedResult = new()
        {
            Entity = new ItemTypeGraphQLModel { Id = 1, Name = "Nuevo", PrefixChar = "X" },
            Success = true,
            Message = "OK"
        };
        _service.CreateAsync<UpsertResponseType<ItemTypeGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForNew(parentCatalogId: 1);
        _vm.Name = "Nuevo";
        _vm.PrefixChar = "X";
        _vm.SelectedMeasurementUnit = _vm.MeasurementUnits.First();
        _vm.SelectedAccountingGroup = _vm.AccountingGroups.First();

        UpsertResponseType<ItemTypeGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).CreateAsync<UpsertResponseType<ItemTypeGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSaveAsync_ExistingRecord_CallsUpdateAsync()
    {
        UpsertResponseType<ItemTypeGraphQLModel> expectedResult = new()
        {
            Entity = new ItemTypeGraphQLModel { Id = 100, Name = "Modificado", PrefixChar = "P" },
            Success = true,
            Message = "OK"
        };
        _service.UpdateAsync<UpsertResponseType<ItemTypeGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForEdit(CreateSampleEntity());
        _vm.Name = "Modificado";

        UpsertResponseType<ItemTypeGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).UpdateAsync<UpsertResponseType<ItemTypeGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helpers

    private static ItemTypeGraphQLModel CreateSampleEntity() => new()
    {
        Id = 100,
        Name = "Producto",
        PrefixChar = "P",
        StockControl = true,
        Catalog = new CatalogGraphQLModel { Id = 1 },
        DefaultMeasurementUnit = new MeasurementUnitGraphQLModel { Id = 2 },
        DefaultAccountingGroup = new AccountingGroupGraphQLModel { Id = 10 }
    };

    #endregion
}
