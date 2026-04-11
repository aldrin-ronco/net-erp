using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using Models.Inventory;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Inventory.CatalogItems.DTO;
using NetErp.Inventory.CatalogItems.Validators;
using NetErp.Inventory.CatalogItems.ViewModels;
using NSubstitute;
using Xunit;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.ViewModels;

/// <summary>
/// Core tests for ItemDetailViewModel — construction, SetForNew/SetForEdit, CanSave, validation,
/// ExecuteSaveAsync. Does NOT cover EAN codes, components, images, S3 integration, or tab state.
/// </summary>
public class ItemDetailViewModelTests
{
    private readonly IRepository<ItemGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly IDialogService _dialogService;
    private readonly StringLengthCache _stringLengthCache;
    private readonly MeasurementUnitCache _measurementUnitCache;
    private readonly ItemBrandCache _itemBrandCache;
    private readonly AccountingGroupCache _accountingGroupCache;
    private readonly ItemSizeCategoryCache _itemSizeCategoryCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly ItemValidator _validator;
    private readonly IMapper _mapper;
    private readonly ItemDetailViewModel _vm;

    public ItemDetailViewModelTests()
    {
        _service = Substitute.For<IRepository<ItemGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _dialogService = Substitute.For<IDialogService>();

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;

        _validator = new ItemValidator();
        _mapper = Substitute.For<IMapper>();
        // Program the mapper with a realistic ItemGraphQLModel → ItemDTO projection so that
        // AddComponent (which calls _mapper.Map<ItemDTO>(DraftComponentItem)) and SetForEdit
        // (which maps each entity.Components[i].Component) produce usable ItemDTOs instead
        // of null, matching the real AutoMapper profile's field-by-field copy.
        _mapper.Map<ItemDTO>(Arg.Any<ItemGraphQLModel>())
            .Returns(call =>
            {
                ItemGraphQLModel? src = call.Arg<ItemGraphQLModel>();
                if (src is null) return null!;
                return new ItemDTO
                {
                    Id = src.Id,
                    Name = src.Name ?? string.Empty,
                    Reference = src.Reference ?? string.Empty,
                    Code = src.Code ?? string.Empty,
                    AllowFraction = src.AllowFraction
                };
            });

        _measurementUnitCache = BuildMeasurementUnitCache();
        _itemBrandCache = BuildItemBrandCache();
        _accountingGroupCache = BuildAccountingGroupCache();
        _itemSizeCategoryCache = BuildItemSizeCategoryCache();

        _vm = new ItemDetailViewModel(
            _service,
            _eventAggregator,
            _dialogService,
            _stringLengthCache,
            _measurementUnitCache,
            _itemBrandCache,
            _accountingGroupCache,
            _itemSizeCategoryCache,
            _joinableTaskFactory,
            _validator,
            _mapper,
            s3Helper: null,
            localImageCachePath: string.Empty);
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

    private static ItemBrandCache BuildItemBrandCache()
    {
        IRepository<ItemBrandGraphQLModel> repo = Substitute.For<IRepository<ItemBrandGraphQLModel>>();
        repo.GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(new PageType<ItemBrandGraphQLModel>
            {
                Entries =
                [
                    new ItemBrandGraphQLModel { Id = 1, Name = "Marca A" }
                ]
            });
        ItemBrandCache cache = new(repo, Substitute.For<IEventAggregator>());
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

    private static ItemSizeCategoryCache BuildItemSizeCategoryCache()
    {
        IRepository<ItemSizeCategoryGraphQLModel> repo = Substitute.For<IRepository<ItemSizeCategoryGraphQLModel>>();
        repo.GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(new PageType<ItemSizeCategoryGraphQLModel>
            {
                Entries =
                [
                    new ItemSizeCategoryGraphQLModel { Id = 1, Name = "Chico" }
                ]
            });
        ItemSizeCategoryCache cache = new(repo, Substitute.For<IEventAggregator>());
        cache.EnsureLoadedAsync().GetAwaiter().GetResult();
        return cache;
    }

    #endregion

    #region Construction

    [Fact]
    public void Constructor_NullService_Throws()
    {
        System.Action act = () => new ItemDetailViewModel(
            null!, _eventAggregator, _dialogService, _stringLengthCache,
            _measurementUnitCache, _itemBrandCache, _accountingGroupCache, _itemSizeCategoryCache,
            _joinableTaskFactory, _validator, _mapper, null, string.Empty);
        act.Should().Throw<ArgumentNullException>().WithParameterName("itemService");
    }

    [Fact]
    public void Constructor_NullDialogService_Throws()
    {
        System.Action act = () => new ItemDetailViewModel(
            _service, _eventAggregator, null!, _stringLengthCache,
            _measurementUnitCache, _itemBrandCache, _accountingGroupCache, _itemSizeCategoryCache,
            _joinableTaskFactory, _validator, _mapper, null, string.Empty);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dialogService");
    }

    [Fact]
    public void Constructor_NullValidator_Throws()
    {
        System.Action act = () => new ItemDetailViewModel(
            _service, _eventAggregator, _dialogService, _stringLengthCache,
            _measurementUnitCache, _itemBrandCache, _accountingGroupCache, _itemSizeCategoryCache,
            _joinableTaskFactory, null!, _mapper, null, string.Empty);
        act.Should().Throw<ArgumentNullException>().WithParameterName("validator");
    }

    [Fact]
    public void Constructor_NullMapper_Throws()
    {
        System.Action act = () => new ItemDetailViewModel(
            _service, _eventAggregator, _dialogService, _stringLengthCache,
            _measurementUnitCache, _itemBrandCache, _accountingGroupCache, _itemSizeCategoryCache,
            _joinableTaskFactory, _validator, null!, null, string.Empty);
        act.Should().Throw<ArgumentNullException>().WithParameterName("mapper");
    }

    [Fact]
    public void Constructor_NullS3Helper_IsS3Available_False()
    {
        _vm.IsS3Available.Should().BeFalse();
    }

    [Fact]
    public void Constructor_SetsDialogDimensions()
    {
        _vm.DialogWidth.Should().Be(900);
        _vm.DialogHeight.Should().Be(650);
    }

    #endregion

    #region SetForNew

    [Fact]
    public void SetForNew_SetsDefaults()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true,
            defaultMeasurementUnitId: null, defaultAccountingGroupId: null);

        _vm.Id.Should().Be(0);
        _vm.IsNewRecord.Should().BeTrue();
        _vm.Name.Should().BeEmpty();
        _vm.Reference.Should().BeEmpty();
        _vm.SubCategoryId.Should().Be(5);
        _vm.HasComponents.Should().BeFalse();
        _vm.ControlsStock.Should().BeTrue();
        _vm.IsModal.Should().BeTrue();
        _vm.IsEditing.Should().BeTrue();
        _vm.HasLoadedItem.Should().BeTrue();
        _vm.IsActive.Should().BeTrue();
        _vm.Billable.Should().BeTrue();
        _vm.AllowFraction.Should().BeFalse();
        _vm.AmountBasedOnWeight.Should().BeFalse();
    }

    [Fact]
    public void SetForNew_LoadsComboSources()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true,
            defaultMeasurementUnitId: null, defaultAccountingGroupId: null);

        _vm.MeasurementUnits.Should().HaveCount(2);
        _vm.AccountingGroups.Should().HaveCount(2);
        _vm.ItemBrands.Should().HaveCount(1);
        _vm.Sizes.Should().HaveCount(1);
    }

    [Fact]
    public void SetForNew_WithDefaults_PreselectsComboValues()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true,
            defaultMeasurementUnitId: 2, defaultAccountingGroupId: 11);

        _vm.SelectedMeasurementUnit.Should().NotBeNull();
        _vm.SelectedMeasurementUnit!.Id.Should().Be(2);
        _vm.SelectedAccountingGroup.Should().NotBeNull();
        _vm.SelectedAccountingGroup!.Id.Should().Be(11);
    }

    [Fact]
    public void SetForNew_WithoutDefaults_LeavesComboSelectionNull()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true,
            defaultMeasurementUnitId: null, defaultAccountingGroupId: null);

        _vm.SelectedMeasurementUnit.Should().BeNull();
        _vm.SelectedAccountingGroup.Should().BeNull();
    }

    [Fact]
    public void SetForNew_NoInitialChanges()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true,
            defaultMeasurementUnitId: 2, defaultAccountingGroupId: 11);

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region SetForEdit

    [Fact]
    public void SetForEdit_PopulatesFromModel()
    {
        ItemGraphQLModel entity = CreateSampleEntity();

        _vm.SetForEdit(entity, hasComponents: false, stockControl: true);

        _vm.Id.Should().Be(500);
        _vm.IsNewRecord.Should().BeFalse();
        _vm.Code.Should().Be("00001");
        _vm.Name.Should().Be("Coca Cola 350ml");
        _vm.Reference.Should().Be("REF-001");
        _vm.SubCategoryId.Should().Be(5);
        _vm.IsActive.Should().BeTrue();
        _vm.Billable.Should().BeTrue();
        _vm.AllowFraction.Should().BeFalse();
        _vm.HasComponents.Should().BeFalse();
        _vm.ControlsStock.Should().BeTrue();
        _vm.IsModal.Should().BeTrue();
        _vm.IsEditing.Should().BeTrue();
        _vm.SelectedMeasurementUnit.Should().NotBeNull();
        _vm.SelectedMeasurementUnit!.Id.Should().Be(2);
        _vm.SelectedAccountingGroup.Should().NotBeNull();
        _vm.SelectedAccountingGroup!.Id.Should().Be(10);
    }

    [Fact]
    public void SetForEdit_NoInitialChanges()
    {
        _vm.SetForEdit(CreateSampleEntity(), hasComponents: false, stockControl: true);

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AfterChangingName_ReturnsTrue()
    {
        _vm.SetForEdit(CreateSampleEntity(), hasComponents: false, stockControl: true);

        _vm.Name = "Coca Cola 500ml";

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_EmptyName_ReturnsFalse()
    {
        _vm.SetForEdit(CreateSampleEntity(), hasComponents: false, stockControl: true);

        _vm.Name = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyReference_ReturnsFalse()
    {
        _vm.SetForEdit(CreateSampleEntity(), hasComponents: false, stockControl: true);
        _vm.Name = "Modified";

        _vm.Reference = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoMeasurementUnit_ReturnsFalse()
    {
        _vm.SetForEdit(CreateSampleEntity(), hasComponents: false, stockControl: true);
        _vm.Name = "Modified";

        _vm.SelectedMeasurementUnit = null;

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoAccountingGroup_ReturnsFalse()
    {
        _vm.SetForEdit(CreateSampleEntity(), hasComponents: false, stockControl: true);
        _vm.Name = "Modified";

        _vm.SelectedAccountingGroup = null;

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_IsBusy_ReturnsFalse()
    {
        _vm.SetForEdit(CreateSampleEntity(), hasComponents: false, stockControl: true);
        _vm.Name = "Modified";

        _vm.IsBusy = true;

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_NotEditing_ReturnsFalse()
    {
        _vm.SetForEdit(CreateSampleEntity(), hasComponents: false, stockControl: true);
        _vm.Name = "Modified";

        _vm.IsEditing = false;

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region Validation

    [Fact]
    public void Name_SetEmpty_AddsValidationError()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true, null, null);
        _vm.Name = "Valid";

        _vm.Name = "";

        _vm.HasErrors.Should().BeTrue();
        _vm.GetErrors(nameof(ItemDetailViewModel.Name)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void Reference_SetEmpty_AddsValidationError()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true, null, null);
        _vm.Reference = "REF-001";

        _vm.Reference = "";

        _vm.HasErrors.Should().BeTrue();
        _vm.GetErrors(nameof(ItemDetailViewModel.Reference)).Cast<string>()
            .Should().NotBeEmpty();
    }

    [Fact]
    public void Name_SetValid_ClearsError()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true, null, null);
        _vm.Name = "X";
        _vm.Name = "";
        _vm.HasErrors.Should().BeTrue();

        _vm.Name = "Producto";

        _vm.GetErrors(nameof(ItemDetailViewModel.Name)).Cast<string>()
            .Should().BeEmpty();
    }

    #endregion

    #region ClearPanel

    [Fact]
    public void ClearPanel_ResetsState()
    {
        _vm.SetForEdit(CreateSampleEntity(), hasComponents: false, stockControl: true);

        _vm.ClearPanel();

        _vm.Id.Should().Be(0);
        _vm.Name.Should().BeEmpty();
        _vm.Reference.Should().BeEmpty();
        _vm.HasLoadedItem.Should().BeFalse();
        _vm.IsEditing.Should().BeFalse();
        _vm.IsModal.Should().BeFalse();
        _vm.SelectedMeasurementUnit.Should().BeNull();
        _vm.SelectedAccountingGroup.Should().BeNull();
    }

    #endregion

    #region LoadForPanel

    [Fact]
    public void LoadForPanel_SetsReadOnlyMode()
    {
        _vm.LoadForPanel(CreateSampleEntity(), hasComponents: false, stockControl: true);

        _vm.IsModal.Should().BeFalse();
        _vm.IsEditing.Should().BeFalse();
        _vm.HasLoadedItem.Should().BeTrue();
        _vm.Name.Should().Be("Coca Cola 350ml");
    }

    [Fact]
    public void EnterEditMode_AfterLoadForPanel_EnablesEditing()
    {
        _vm.LoadForPanel(CreateSampleEntity(), hasComponents: false, stockControl: true);

        _vm.EnterEditMode();

        _vm.IsEditing.Should().BeTrue();
    }

    #endregion

    #region ExecuteSaveAsync

    [Fact]
    public async Task ExecuteSaveAsync_NewRecord_CallsCreateAsync()
    {
        UpsertResponseType<ItemGraphQLModel> expectedResult = new()
        {
            Entity = new ItemGraphQLModel { Id = 1, Name = "Nuevo", Reference = "REF-X" },
            Success = true,
            Message = "OK"
        };
        _service.CreateAsync<UpsertResponseType<ItemGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true,
            defaultMeasurementUnitId: 2, defaultAccountingGroupId: 11);
        _vm.Name = "Nuevo";
        _vm.Reference = "REF-X";

        UpsertResponseType<ItemGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).CreateAsync<UpsertResponseType<ItemGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSaveAsync_ExistingRecord_CallsUpdateAsync()
    {
        UpsertResponseType<ItemGraphQLModel> expectedResult = new()
        {
            Entity = new ItemGraphQLModel { Id = 500, Name = "Modificado", Reference = "REF-001" },
            Success = true,
            Message = "OK"
        };
        _service.UpdateAsync<UpsertResponseType<ItemGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForEdit(CreateSampleEntity(), hasComponents: false, stockControl: true);
        _vm.Name = "Coca Cola Modificada";

        UpsertResponseType<ItemGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).UpdateAsync<UpsertResponseType<ItemGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region EAN — state initialization

    [Fact]
    public void SetForNew_ClearsEanCodes()
    {
        // First load an entity with EAN codes, then call SetForNew — the collection must reset
        _vm.SetForEdit(CreateEntityWithEanCodes(), hasComponents: false, stockControl: true);
        _vm.EanCodes.Should().NotBeEmpty();

        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true, null, null);

        _vm.EanCodes.Should().BeEmpty();
        _vm.EanCodeDraft.Should().BeEmpty();
    }

    [Fact]
    public void SetForEdit_PopulatesEanCodes_PreservingIsInternal()
    {
        _vm.SetForEdit(CreateEntityWithEanCodes(), hasComponents: false, stockControl: true);

        _vm.EanCodes.Should().HaveCount(3);
        _vm.EanCodes.Should().ContainSingle(e => e.EanCode == "INT-001" && e.IsInternal);
        _vm.EanCodes.Should().ContainSingle(e => e.EanCode == "EXT-100" && !e.IsInternal);
        _vm.EanCodes.Should().ContainSingle(e => e.EanCode == "EXT-200" && !e.IsInternal);
    }

    [Fact]
    public void ClearPanel_ClearsEanCodes()
    {
        _vm.SetForEdit(CreateEntityWithEanCodes(), hasComponents: false, stockControl: true);
        _vm.EanCodes.Should().NotBeEmpty();

        _vm.ClearPanel();

        _vm.EanCodes.Should().BeEmpty();
        _vm.EanCodeDraft.Should().BeEmpty();
    }

    #endregion

    #region EAN — CanAddEanCode

    [Fact]
    public void CanAddEanCode_EmptyDraft_False()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true, null, null);
        _vm.EanCodeDraft = "";

        _vm.CanAddEanCode.Should().BeFalse();
    }

    [Fact]
    public void CanAddEanCode_WhitespaceDraft_False()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true, null, null);
        _vm.EanCodeDraft = "   ";

        _vm.CanAddEanCode.Should().BeFalse();
    }

    [Fact]
    public void CanAddEanCode_ValidDraft_True()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true, null, null);
        _vm.EanCodeDraft = "7701234567890";

        _vm.CanAddEanCode.Should().BeTrue();
    }

    #endregion

    #region EAN — AddEanCodeCommand

    [Fact]
    public void AddEanCodeCommand_AddsExternalCode()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true, null, null);
        _vm.EanCodeDraft = "7701234567890";

        _vm.AddEanCodeCommand.Execute(null);

        _vm.EanCodes.Should().HaveCount(1);
        _vm.EanCodes[0].EanCode.Should().Be("7701234567890");
        _vm.EanCodes[0].IsInternal.Should().BeFalse();
    }

    [Fact]
    public void AddEanCodeCommand_TrimsDraft()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true, null, null);
        _vm.EanCodeDraft = "   7701234567890   ";

        _vm.AddEanCodeCommand.Execute(null);

        _vm.EanCodes[0].EanCode.Should().Be("7701234567890");
    }

    [Fact]
    public void AddEanCodeCommand_ClearsDraftAfterAdd()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true, null, null);
        _vm.EanCodeDraft = "7701234567890";

        _vm.AddEanCodeCommand.Execute(null);

        _vm.EanCodeDraft.Should().BeEmpty();
        _vm.CanAddEanCode.Should().BeFalse();
    }

    [Fact]
    public void AddEanCodeCommand_EmptyDraft_NoOp()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true, null, null);
        _vm.EanCodeDraft = "";

        _vm.AddEanCodeCommand.Execute(null);

        _vm.EanCodes.Should().BeEmpty();
    }

    [Fact]
    public void AddEanCodeCommand_MultipleCalls_AppendsEachCode()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true, null, null);

        _vm.EanCodeDraft = "111";
        _vm.AddEanCodeCommand.Execute(null);
        _vm.EanCodeDraft = "222";
        _vm.AddEanCodeCommand.Execute(null);
        _vm.EanCodeDraft = "333";
        _vm.AddEanCodeCommand.Execute(null);

        _vm.EanCodes.Select(e => e.EanCode).Should().Equal("111", "222", "333");
        _vm.EanCodes.Should().OnlyContain(e => !e.IsInternal);
    }

    #endregion

    #region EAN — CanDeleteEanCode

    [Fact]
    public void CanDeleteEanCode_NullSelected_False()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true, null, null);
        _vm.SelectedEanCode = null;

        _vm.CanDeleteEanCode.Should().BeFalse();
    }

    [Fact]
    public void CanDeleteEanCode_InternalSelected_False()
    {
        _vm.SetForEdit(CreateEntityWithEanCodes(), hasComponents: false, stockControl: true);

        _vm.SelectedEanCode = _vm.EanCodes.First(e => e.IsInternal);

        _vm.CanDeleteEanCode.Should().BeFalse();
    }

    [Fact]
    public void CanDeleteEanCode_ExternalSelected_True()
    {
        _vm.SetForEdit(CreateEntityWithEanCodes(), hasComponents: false, stockControl: true);

        _vm.SelectedEanCode = _vm.EanCodes.First(e => !e.IsInternal);

        _vm.CanDeleteEanCode.Should().BeTrue();
    }

    #endregion

    #region EAN — Change tracking drives CanSave

    [Fact]
    public void AddingEanCode_MarksVmAsDirty_EnablesCanSave()
    {
        _vm.SetForEdit(CreateEntityWithEanCodes(), hasComponents: false, stockControl: true);
        _vm.CanSave.Should().BeFalse();  // seeded, no changes yet

        _vm.EanCodeDraft = "NEW-999";
        _vm.AddEanCodeCommand.Execute(null);

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void RemovingEanCode_MarksVmAsDirty_EnablesCanSave()
    {
        _vm.SetForEdit(CreateEntityWithEanCodes(), hasComponents: false, stockControl: true);
        _vm.CanSave.Should().BeFalse();

        // Remove directly (simulating what DeleteEanCode does after the confirmation dialog,
        // which we can't invoke in unit tests because it calls ThemedMessageBox.Show).
        EanCodeByItemDTO toRemove = _vm.EanCodes.First(e => !e.IsInternal);
        _vm.EanCodes.Remove(toRemove);

        _vm.CanSave.Should().BeTrue();
    }

    #endregion

    #region EAN — ExecuteSaveAsync payload filtering

    [Fact]
    public async Task ExecuteSaveAsync_Update_SendsOnlyExternalEanCodes()
    {
        // Entity has 1 internal + 2 external codes. Only the 2 external should be sent.
        object? captured = null;
        _service.UpdateAsync<UpsertResponseType<ItemGraphQLModel>>(
            Arg.Any<string>(),
            Arg.Do<object>(v => captured = v),
            Arg.Any<CancellationToken>())
            .Returns(new UpsertResponseType<ItemGraphQLModel>
            {
                Entity = new ItemGraphQLModel { Id = 500 },
                Success = true,
                Message = "OK"
            });

        _vm.SetForEdit(CreateEntityWithEanCodes(), hasComponents: false, stockControl: true);
        _vm.Name = "Modified";  // force a change so the payload builds

        await _vm.ExecuteSaveAsync();

        List<string> sentEanCodes = ExtractEanCodes(captured, prefix: "updateResponseData");
        sentEanCodes.Should().BeEquivalentTo(["EXT-100", "EXT-200"]);
        sentEanCodes.Should().NotContain("INT-001");
    }

    [Fact]
    public async Task ExecuteSaveAsync_Create_SendsOnlyExternalEanCodes()
    {
        object? captured = null;
        _service.CreateAsync<UpsertResponseType<ItemGraphQLModel>>(
            Arg.Any<string>(),
            Arg.Do<object>(v => captured = v),
            Arg.Any<CancellationToken>())
            .Returns(new UpsertResponseType<ItemGraphQLModel>
            {
                Entity = new ItemGraphQLModel { Id = 1 },
                Success = true,
                Message = "OK"
            });

        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true,
            defaultMeasurementUnitId: 2, defaultAccountingGroupId: 11);
        _vm.Name = "Nuevo";
        _vm.Reference = "REF-X";
        // Add two external codes and one internal by direct construction
        _vm.EanCodeDraft = "EXT-A";
        _vm.AddEanCodeCommand.Execute(null);
        _vm.EanCodeDraft = "EXT-B";
        _vm.AddEanCodeCommand.Execute(null);
        _vm.EanCodes.Add(new EanCodeByItemDTO { EanCode = "INT-Z", IsInternal = true });

        await _vm.ExecuteSaveAsync();

        List<string> sentEanCodes = ExtractEanCodes(captured, prefix: "createResponseInput");
        sentEanCodes.Should().BeEquivalentTo(["EXT-A", "EXT-B"]);
        sentEanCodes.Should().NotContain("INT-Z");
    }

    [Fact]
    public async Task ExecuteSaveAsync_Update_NoEanCodes_SendsEmptyArray()
    {
        object? captured = null;
        _service.UpdateAsync<UpsertResponseType<ItemGraphQLModel>>(
            Arg.Any<string>(),
            Arg.Do<object>(v => captured = v),
            Arg.Any<CancellationToken>())
            .Returns(new UpsertResponseType<ItemGraphQLModel>
            {
                Entity = new ItemGraphQLModel { Id = 500 },
                Success = true,
                Message = "OK"
            });

        _vm.SetForEdit(CreateSampleEntity(), hasComponents: false, stockControl: true);  // no EAN codes
        _vm.Name = "Modified";

        await _vm.ExecuteSaveAsync();

        List<string> sentEanCodes = ExtractEanCodes(captured, prefix: "updateResponseData");
        sentEanCodes.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteSaveAsync_Update_OnlyInternalCodes_SendsEmptyArray()
    {
        object? captured = null;
        _service.UpdateAsync<UpsertResponseType<ItemGraphQLModel>>(
            Arg.Any<string>(),
            Arg.Do<object>(v => captured = v),
            Arg.Any<CancellationToken>())
            .Returns(new UpsertResponseType<ItemGraphQLModel>
            {
                Entity = new ItemGraphQLModel { Id = 500 },
                Success = true,
                Message = "OK"
            });

        ItemGraphQLModel entity = CreateSampleEntity();
        entity.EanCodes =
        [
            new EanCodeByItemGraphQLModel { EanCode = "INT-1", IsInternal = true },
            new EanCodeByItemGraphQLModel { EanCode = "INT-2", IsInternal = true }
        ];

        _vm.SetForEdit(entity, hasComponents: false, stockControl: true);
        _vm.Name = "Modified";

        await _vm.ExecuteSaveAsync();

        List<string> sentEanCodes = ExtractEanCodes(captured, prefix: "updateResponseData");
        sentEanCodes.Should().BeEmpty();
    }

    #endregion

    #region Components — state initialization

    [Fact]
    public void SetForNew_ClearsComponents()
    {
        _vm.SetForEdit(CreateEntityWithComponents(), hasComponents: true, stockControl: true);
        _vm.Components.Should().NotBeEmpty();

        _vm.SetForNew(subCategoryId: 5, hasComponents: true, stockControl: true, null, null);

        _vm.Components.Should().BeEmpty();
        _vm.DraftComponentItem.Should().BeNull();
        _vm.DraftComponentQuantity.Should().Be(0);
    }

    [Fact]
    public void SetForEdit_PopulatesComponents_MappingEachItem()
    {
        _vm.SetForEdit(CreateEntityWithComponents(), hasComponents: true, stockControl: true);

        _vm.Components.Should().HaveCount(2);
        _vm.Components[0].Component.Should().NotBeNull();
        _vm.Components[0].Component!.Id.Should().Be(900);
        _vm.Components[0].Component!.Name.Should().Be("Azúcar 1kg");
        _vm.Components[0].Quantity.Should().Be(2m);
        _vm.Components[1].Component!.Id.Should().Be(901);
        _vm.Components[1].Quantity.Should().Be(0.5m);
    }

    [Fact]
    public void ClearPanel_ClearsComponents()
    {
        _vm.SetForEdit(CreateEntityWithComponents(), hasComponents: true, stockControl: true);
        _vm.Components.Should().NotBeEmpty();

        _vm.ClearPanel();

        _vm.Components.Should().BeEmpty();
        _vm.DraftComponentItem.Should().BeNull();
        _vm.DraftComponentQuantity.Should().Be(0);
    }

    #endregion

    #region Components — DraftComponentItem derived properties

    [Fact]
    public void DraftComponentItem_Null_DerivedPropertiesReturnDefaults()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: true, stockControl: true, null, null);
        _vm.DraftComponentItem = null;

        _vm.DraftComponentName.Should().BeEmpty();
        _vm.DraftComponentReference.Should().BeEmpty();
        _vm.DraftComponentCode.Should().BeEmpty();
        _vm.DraftComponentAllowFraction.Should().BeFalse();
        _vm.DraftComponentQuantityEnabled.Should().BeFalse();
    }

    [Fact]
    public void DraftComponentItem_Set_UpdatesDerivedProperties()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: true, stockControl: true, null, null);

        _vm.DraftComponentItem = new ItemGraphQLModel
        {
            Id = 900,
            Name = "Azúcar 1kg",
            Reference = "AZU-001",
            Code = "00900",
            AllowFraction = true
        };

        _vm.DraftComponentName.Should().Be("Azúcar 1kg");
        _vm.DraftComponentReference.Should().Be("AZU-001");
        _vm.DraftComponentCode.Should().Be("00900");
        _vm.DraftComponentAllowFraction.Should().BeTrue();
        _vm.DraftComponentQuantityEnabled.Should().BeTrue();
    }

    [Fact]
    public void DraftComponentItem_SetThenNull_ResetsDerivedProperties()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: true, stockControl: true, null, null);
        _vm.DraftComponentItem = new ItemGraphQLModel
        {
            Id = 900,
            Name = "Azúcar 1kg",
            Reference = "AZU-001",
            AllowFraction = true
        };

        _vm.DraftComponentItem = null;

        _vm.DraftComponentName.Should().BeEmpty();
        _vm.DraftComponentReference.Should().BeEmpty();
        _vm.DraftComponentAllowFraction.Should().BeFalse();
        _vm.DraftComponentQuantityEnabled.Should().BeFalse();
    }

    #endregion

    #region Components — CanAddComponent

    [Fact]
    public void CanAddComponent_NullDraftItem_False()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: true, stockControl: true, null, null);
        _vm.DraftComponentItem = null;
        _vm.DraftComponentQuantity = 5m;

        _vm.CanAddComponent.Should().BeFalse();
    }

    [Fact]
    public void CanAddComponent_ZeroQuantity_False()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: true, stockControl: true, null, null);
        _vm.DraftComponentItem = new ItemGraphQLModel { Id = 900, Name = "Azúcar" };
        _vm.DraftComponentQuantity = 0m;

        _vm.CanAddComponent.Should().BeFalse();
    }

    [Fact]
    public void CanAddComponent_NegativeQuantity_False()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: true, stockControl: true, null, null);
        _vm.DraftComponentItem = new ItemGraphQLModel { Id = 900, Name = "Azúcar" };
        _vm.DraftComponentQuantity = -1m;

        _vm.CanAddComponent.Should().BeFalse();
    }

    [Fact]
    public void CanAddComponent_ValidItemAndQuantity_True()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: true, stockControl: true, null, null);
        _vm.DraftComponentItem = new ItemGraphQLModel { Id = 900, Name = "Azúcar" };
        _vm.DraftComponentQuantity = 2m;

        _vm.CanAddComponent.Should().BeTrue();
    }

    #endregion

    #region Components — AddComponentCommand

    [Fact]
    public void AddComponentCommand_AddsComponent_WithMappedItem()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: true, stockControl: true, null, null);
        _vm.DraftComponentItem = new ItemGraphQLModel
        {
            Id = 900,
            Name = "Azúcar 1kg",
            Reference = "AZU-001",
            Code = "00900"
        };
        _vm.DraftComponentQuantity = 2m;

        _vm.AddComponentCommand.Execute(null);

        _vm.Components.Should().HaveCount(1);
        _vm.Components[0].Component.Should().NotBeNull();
        _vm.Components[0].Component!.Id.Should().Be(900);
        _vm.Components[0].Component!.Name.Should().Be("Azúcar 1kg");
        _vm.Components[0].Component!.Reference.Should().Be("AZU-001");
        _vm.Components[0].Quantity.Should().Be(2m);
    }

    [Fact]
    public void AddComponentCommand_ResetsDraftAfterAdd()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: true, stockControl: true, null, null);
        _vm.DraftComponentItem = new ItemGraphQLModel { Id = 900, Name = "Azúcar" };
        _vm.DraftComponentQuantity = 2m;

        _vm.AddComponentCommand.Execute(null);

        _vm.DraftComponentItem.Should().BeNull();
        _vm.DraftComponentQuantity.Should().Be(0);
        _vm.CanAddComponent.Should().BeFalse();
    }

    [Fact]
    public void AddComponentCommand_NullDraftItem_NoOp()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: true, stockControl: true, null, null);
        _vm.DraftComponentItem = null;
        _vm.DraftComponentQuantity = 5m;

        _vm.AddComponentCommand.Execute(null);

        _vm.Components.Should().BeEmpty();
    }

    [Fact]
    public void AddComponentCommand_ZeroQuantity_NoOp()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: true, stockControl: true, null, null);
        _vm.DraftComponentItem = new ItemGraphQLModel { Id = 900, Name = "Azúcar" };
        _vm.DraftComponentQuantity = 0m;

        _vm.AddComponentCommand.Execute(null);

        _vm.Components.Should().BeEmpty();
    }

    [Fact]
    public void AddComponentCommand_MultipleCalls_AppendsEachComponent()
    {
        _vm.SetForNew(subCategoryId: 5, hasComponents: true, stockControl: true, null, null);

        _vm.DraftComponentItem = new ItemGraphQLModel { Id = 900, Name = "Azúcar" };
        _vm.DraftComponentQuantity = 2m;
        _vm.AddComponentCommand.Execute(null);

        _vm.DraftComponentItem = new ItemGraphQLModel { Id = 901, Name = "Harina" };
        _vm.DraftComponentQuantity = 0.5m;
        _vm.AddComponentCommand.Execute(null);

        _vm.DraftComponentItem = new ItemGraphQLModel { Id = 902, Name = "Sal" };
        _vm.DraftComponentQuantity = 0.01m;
        _vm.AddComponentCommand.Execute(null);

        _vm.Components.Should().HaveCount(3);
        _vm.Components.Select(c => c.Component!.Id).Should().Equal(900, 901, 902);
        _vm.Components.Select(c => c.Quantity).Should().Equal(2m, 0.5m, 0.01m);
    }

    #endregion

    #region Components — Change tracking drives CanSave

    [Fact]
    public void AddingComponent_MarksVmAsDirty_EnablesCanSave()
    {
        _vm.SetForEdit(CreateEntityWithComponents(), hasComponents: true, stockControl: true);
        _vm.CanSave.Should().BeFalse();

        _vm.DraftComponentItem = new ItemGraphQLModel { Id = 999, Name = "Nuevo" };
        _vm.DraftComponentQuantity = 3m;
        _vm.AddComponentCommand.Execute(null);

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void RemovingComponent_MarksVmAsDirty_EnablesCanSave()
    {
        _vm.SetForEdit(CreateEntityWithComponents(), hasComponents: true, stockControl: true);
        _vm.CanSave.Should().BeFalse();

        // Remove directly (simulating what DeleteComponent does after the confirmation dialog,
        // which we can't invoke in unit tests because it calls ThemedMessageBox.Show).
        ComponentsByItemDTO toRemove = _vm.Components.First();
        _vm.Components.Remove(toRemove);

        _vm.CanSave.Should().BeTrue();
    }

    #endregion

    #region Components — ExecuteSaveAsync payload transformation

    [Fact]
    public async Task ExecuteSaveAsync_Update_SendsComponentsAsItemIdQuantityPairs()
    {
        object? captured = null;
        _service.UpdateAsync<UpsertResponseType<ItemGraphQLModel>>(
            Arg.Any<string>(),
            Arg.Do<object>(v => captured = v),
            Arg.Any<CancellationToken>())
            .Returns(new UpsertResponseType<ItemGraphQLModel>
            {
                Entity = new ItemGraphQLModel { Id = 500 },
                Success = true,
                Message = "OK"
            });

        _vm.SetForEdit(CreateEntityWithComponents(), hasComponents: true, stockControl: true);
        // Add a new component to force a change on the Components collection specifically
        _vm.DraftComponentItem = new ItemGraphQLModel { Id = 902, Name = "Sal" };
        _vm.DraftComponentQuantity = 0.01m;
        _vm.AddComponentCommand.Execute(null);

        await _vm.ExecuteSaveAsync();

        List<(int itemId, decimal quantity)> sent = ExtractComponents(captured, prefix: "updateResponseData");
        sent.Should().HaveCount(3);
        sent.Should().Contain((900, 2m));
        sent.Should().Contain((901, 0.5m));
        sent.Should().Contain((902, 0.01m));
    }

    [Fact]
    public async Task ExecuteSaveAsync_Create_SendsComponentsAsItemIdQuantityPairs()
    {
        object? captured = null;
        _service.CreateAsync<UpsertResponseType<ItemGraphQLModel>>(
            Arg.Any<string>(),
            Arg.Do<object>(v => captured = v),
            Arg.Any<CancellationToken>())
            .Returns(new UpsertResponseType<ItemGraphQLModel>
            {
                Entity = new ItemGraphQLModel { Id = 1 },
                Success = true,
                Message = "OK"
            });

        _vm.SetForNew(subCategoryId: 5, hasComponents: true, stockControl: true,
            defaultMeasurementUnitId: 2, defaultAccountingGroupId: 11);
        _vm.Name = "Combo Promocional";
        _vm.Reference = "COMBO-001";

        _vm.DraftComponentItem = new ItemGraphQLModel { Id = 900, Name = "Azúcar" };
        _vm.DraftComponentQuantity = 2m;
        _vm.AddComponentCommand.Execute(null);

        _vm.DraftComponentItem = new ItemGraphQLModel { Id = 901, Name = "Harina" };
        _vm.DraftComponentQuantity = 1.5m;
        _vm.AddComponentCommand.Execute(null);

        await _vm.ExecuteSaveAsync();

        List<(int itemId, decimal quantity)> sent = ExtractComponents(captured, prefix: "createResponseInput");
        sent.Should().BeEquivalentTo(new[]
        {
            (itemId: 900, quantity: 2m),
            (itemId: 901, quantity: 1.5m)
        });
    }

    [Fact]
    public async Task ExecuteSaveAsync_Update_NoComponents_SendsNoComponentsKey()
    {
        // When Components hasn't been modified (not in change set) and the payload is Update,
        // the transformer shouldn't run. The serialized payload should not contain a "components" key.
        object? captured = null;
        _service.UpdateAsync<UpsertResponseType<ItemGraphQLModel>>(
            Arg.Any<string>(),
            Arg.Do<object>(v => captured = v),
            Arg.Any<CancellationToken>())
            .Returns(new UpsertResponseType<ItemGraphQLModel>
            {
                Entity = new ItemGraphQLModel { Id = 500 },
                Success = true,
                Message = "OK"
            });

        _vm.SetForEdit(CreateSampleEntity(), hasComponents: false, stockControl: true);
        _vm.Name = "Modified";

        await _vm.ExecuteSaveAsync();

        captured.Should().NotBeNull();
        IDictionary<string, object?> root = (IDictionary<string, object?>)captured!;
        IDictionary<string, object?> input = (IDictionary<string, object?>)root["updateResponseData"]!;
        input.Should().NotContainKey("Components");
        input.Should().NotContainKey("components");
    }

    #endregion

    #region Images — state initialization

    [Fact]
    public void SetForNew_ClearsImages()
    {
        _vm.SetForEdit(CreateEntityWithImages(), hasComponents: false, stockControl: true);
        _vm.Images.Should().NotBeEmpty();

        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true, null, null);

        _vm.Images.Should().BeEmpty();
    }

    [Fact]
    public void SetForEdit_PopulatesImages_OrderedByDisplayOrder()
    {
        // Deliberately feed images in non-sorted order; the VM must sort by DisplayOrder
        ItemGraphQLModel entity = CreateSampleEntity();
        entity.Images =
        [
            new ImageByItemGraphQLModel { DisplayOrder = 2, S3Bucket = "b", S3BucketDirectory = "d", S3FileName = "third.jpg" },
            new ImageByItemGraphQLModel { DisplayOrder = 0, S3Bucket = "b", S3BucketDirectory = "d", S3FileName = "first.jpg" },
            new ImageByItemGraphQLModel { DisplayOrder = 1, S3Bucket = "b", S3BucketDirectory = "d", S3FileName = "second.jpg" }
        ];

        _vm.SetForEdit(entity, hasComponents: false, stockControl: true);

        _vm.Images.Should().HaveCount(3);
        _vm.Images[0].S3FileName.Should().Be("first.jpg");
        _vm.Images[1].S3FileName.Should().Be("second.jpg");
        _vm.Images[2].S3FileName.Should().Be("third.jpg");
    }

    [Fact]
    public void SetForEdit_PopulatesImages_CopiesS3Fields()
    {
        _vm.SetForEdit(CreateEntityWithImages(), hasComponents: false, stockControl: true);

        _vm.Images[0].S3Bucket.Should().Be("product-images");
        _vm.Images[0].S3BucketDirectory.Should().Be("items");
        _vm.Images[0].S3FileName.Should().Be("abc.jpg");
        _vm.Images[1].S3FileName.Should().Be("xyz.png");
    }

    [Fact]
    public void ClearPanel_ClearsImages()
    {
        _vm.SetForEdit(CreateEntityWithImages(), hasComponents: false, stockControl: true);
        _vm.Images.Should().NotBeEmpty();

        _vm.ClearPanel();

        _vm.Images.Should().BeEmpty();
    }

    #endregion

    #region Images — CanAddImage

    [Fact]
    public void CanAddImage_NoS3Helper_False()
    {
        // _vm is built with s3Helper: null in the constructor — IsS3Available is false
        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true, null, null);
        _vm.HasAddImagePermission = true;

        _vm.IsS3Available.Should().BeFalse();
        _vm.CanAddImage.Should().BeFalse();
    }

    [Fact]
    public void CanAddImage_NoPermission_False()
    {
        ItemDetailViewModel vm = CreateVmWithS3();
        vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true, null, null);

        vm.HasAddImagePermission = false;

        vm.CanAddImage.Should().BeFalse();
    }

    [Fact]
    public void CanAddImage_AtMaxCapacity_False()
    {
        ItemDetailViewModel vm = CreateVmWithS3();
        vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true, null, null);
        vm.HasAddImagePermission = true;

        for (int i = 0; i < 4; i++)
            vm.Images.Add(new ImageByItemDTO { S3FileName = $"img{i}.jpg", S3Bucket = "b", S3BucketDirectory = "d" });

        vm.CanAddImage.Should().BeFalse();
    }

    [Fact]
    public void CanAddImage_ThreeImages_StillTrue()
    {
        ItemDetailViewModel vm = CreateVmWithS3();
        vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true, null, null);
        vm.HasAddImagePermission = true;

        for (int i = 0; i < 3; i++)
            vm.Images.Add(new ImageByItemDTO { S3FileName = $"img{i}.jpg", S3Bucket = "b", S3BucketDirectory = "d" });

        vm.CanAddImage.Should().BeTrue();
    }

    [Fact]
    public void CanAddImage_WithS3AndPermissionAndEmptyImages_True()
    {
        ItemDetailViewModel vm = CreateVmWithS3();
        vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true, null, null);
        vm.HasAddImagePermission = true;

        vm.IsS3Available.Should().BeTrue();
        vm.Images.Should().BeEmpty();
        vm.CanAddImage.Should().BeTrue();
    }

    #endregion

    #region Images — DeleteImageCommand

    [Fact]
    public void DeleteImageCommand_RemovesImageFromCollection()
    {
        _vm.SetForEdit(CreateEntityWithImages(), hasComponents: false, stockControl: true);
        ImageByItemDTO toRemove = _vm.Images[0];
        int initialCount = _vm.Images.Count;

        _vm.DeleteImageCommand.Execute(toRemove);

        _vm.Images.Should().HaveCount(initialCount - 1);
        _vm.Images.Should().NotContain(toRemove);
    }

    [Fact]
    public void DeleteImageCommand_NonImageParameter_NoOp()
    {
        _vm.SetForEdit(CreateEntityWithImages(), hasComponents: false, stockControl: true);
        int initialCount = _vm.Images.Count;

        _vm.DeleteImageCommand.Execute("not-an-image");

        _vm.Images.Should().HaveCount(initialCount);
    }

    [Fact]
    public void DeleteImageCommand_NullParameter_NoOp()
    {
        _vm.SetForEdit(CreateEntityWithImages(), hasComponents: false, stockControl: true);
        int initialCount = _vm.Images.Count;

        _vm.DeleteImageCommand.Execute(null);

        _vm.Images.Should().HaveCount(initialCount);
    }

    #endregion

    #region Images — Change tracking drives CanSave

    [Fact]
    public void AddingImage_MarksVmAsDirty_EnablesCanSave()
    {
        _vm.SetForEdit(CreateEntityWithImages(), hasComponents: false, stockControl: true);
        _vm.CanSave.Should().BeFalse();

        _vm.Images.Add(new ImageByItemDTO
        {
            S3FileName = "new-image.jpg",
            S3Bucket = "product-images",
            S3BucketDirectory = "items"
        });

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void RemovingImage_MarksVmAsDirty_EnablesCanSave()
    {
        _vm.SetForEdit(CreateEntityWithImages(), hasComponents: false, stockControl: true);
        _vm.CanSave.Should().BeFalse();

        _vm.Images.RemoveAt(0);

        _vm.CanSave.Should().BeTrue();
    }

    #endregion

    #region Images — ExecuteSaveAsync payload transformation

    [Fact]
    public async Task ExecuteSaveAsync_Update_SendsImagesWithS3FieldsAndDisplayOrder()
    {
        object? captured = null;
        _service.UpdateAsync<UpsertResponseType<ItemGraphQLModel>>(
            Arg.Any<string>(),
            Arg.Do<object>(v => captured = v),
            Arg.Any<CancellationToken>())
            .Returns(new UpsertResponseType<ItemGraphQLModel>
            {
                Entity = new ItemGraphQLModel { Id = 500 },
                Success = true,
                Message = "OK"
            });

        _vm.SetForEdit(CreateEntityWithImages(), hasComponents: false, stockControl: true);
        // Force a change to the Images collection so the payload emits it
        _vm.Images.Add(new ImageByItemDTO
        {
            S3FileName = "new.jpg",
            S3Bucket = "product-images",
            S3BucketDirectory = "items"
        });

        await _vm.ExecuteSaveAsync();

        List<ImagePayload> sent = ExtractImages(captured, prefix: "updateResponseData");
        sent.Should().HaveCount(3);
        sent[0].s3FileName.Should().Be("abc.jpg");
        sent[0].displayOrder.Should().Be(0);
        sent[1].s3FileName.Should().Be("xyz.png");
        sent[1].displayOrder.Should().Be(1);
        sent[2].s3FileName.Should().Be("new.jpg");
        sent[2].displayOrder.Should().Be(2);
        sent.Should().OnlyContain(i => i.s3Bucket == "product-images" && i.s3BucketDirectory == "items");
    }

    [Fact]
    public async Task ExecuteSaveAsync_Create_SendsImagesWithIndexAsDisplayOrder()
    {
        object? captured = null;
        _service.CreateAsync<UpsertResponseType<ItemGraphQLModel>>(
            Arg.Any<string>(),
            Arg.Do<object>(v => captured = v),
            Arg.Any<CancellationToken>())
            .Returns(new UpsertResponseType<ItemGraphQLModel>
            {
                Entity = new ItemGraphQLModel { Id = 1 },
                Success = true,
                Message = "OK"
            });

        _vm.SetForNew(subCategoryId: 5, hasComponents: false, stockControl: true,
            defaultMeasurementUnitId: 2, defaultAccountingGroupId: 11);
        _vm.Name = "Producto con imágenes";
        _vm.Reference = "REF-IMG";

        _vm.Images.Add(new ImageByItemDTO { S3FileName = "a.jpg", S3Bucket = "bucket", S3BucketDirectory = "dir" });
        _vm.Images.Add(new ImageByItemDTO { S3FileName = "b.jpg", S3Bucket = "bucket", S3BucketDirectory = "dir" });

        await _vm.ExecuteSaveAsync();

        List<ImagePayload> sent = ExtractImages(captured, prefix: "createResponseInput");
        sent.Should().HaveCount(2);
        sent[0].s3FileName.Should().Be("a.jpg");
        sent[0].displayOrder.Should().Be(0);
        sent[1].s3FileName.Should().Be("b.jpg");
        sent[1].displayOrder.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteSaveAsync_Update_NoImageChanges_DoesNotSendImagesKey()
    {
        // Components and Images are excluded from payload when not in change set.
        object? captured = null;
        _service.UpdateAsync<UpsertResponseType<ItemGraphQLModel>>(
            Arg.Any<string>(),
            Arg.Do<object>(v => captured = v),
            Arg.Any<CancellationToken>())
            .Returns(new UpsertResponseType<ItemGraphQLModel>
            {
                Entity = new ItemGraphQLModel { Id = 500 },
                Success = true,
                Message = "OK"
            });

        _vm.SetForEdit(CreateSampleEntity(), hasComponents: false, stockControl: true);  // no images
        _vm.Name = "Modified";

        await _vm.ExecuteSaveAsync();

        captured.Should().NotBeNull();
        IDictionary<string, object?> root = (IDictionary<string, object?>)captured!;
        IDictionary<string, object?> input = (IDictionary<string, object?>)root["updateResponseData"]!;
        input.Should().NotContainKey("images");
        input.Should().NotContainKey("Images");
    }

    #endregion

    #region Helpers

    private static ItemGraphQLModel CreateSampleEntity() => new()
    {
        Id = 500,
        Code = "00001",
        Name = "Coca Cola 350ml",
        Reference = "REF-001",
        IsActive = true,
        Billable = true,
        AllowFraction = false,
        AmountBasedOnWeight = false,
        HasExtendedInformation = false,
        AiuBasedService = false,
        SubCategory = new ItemSubCategoryGraphQLModel { Id = 5 },
        MeasurementUnit = new MeasurementUnitGraphQLModel { Id = 2 },
        AccountingGroup = new AccountingGroupGraphQLModel { Id = 10 }
    };

    private static ItemGraphQLModel CreateEntityWithEanCodes()
    {
        ItemGraphQLModel entity = CreateSampleEntity();
        entity.EanCodes =
        [
            new EanCodeByItemGraphQLModel { EanCode = "INT-001", IsInternal = true },
            new EanCodeByItemGraphQLModel { EanCode = "EXT-100", IsInternal = false },
            new EanCodeByItemGraphQLModel { EanCode = "EXT-200", IsInternal = false }
        ];
        return entity;
    }

    private static ItemGraphQLModel CreateEntityWithComponents()
    {
        ItemGraphQLModel entity = CreateSampleEntity();
        entity.Components =
        [
            new ComponentsByItemGraphQLModel
            {
                Quantity = 2m,
                Component = new ItemGraphQLModel { Id = 900, Name = "Azúcar 1kg", Reference = "AZU-001", Code = "00900" }
            },
            new ComponentsByItemGraphQLModel
            {
                Quantity = 0.5m,
                Component = new ItemGraphQLModel { Id = 901, Name = "Harina 500g", Reference = "HAR-001", Code = "00901" }
            }
        ];
        return entity;
    }

    private static ItemGraphQLModel CreateEntityWithImages()
    {
        ItemGraphQLModel entity = CreateSampleEntity();
        entity.Images =
        [
            new ImageByItemGraphQLModel
            {
                DisplayOrder = 0,
                S3Bucket = "product-images",
                S3BucketDirectory = "items",
                S3FileName = "abc.jpg"
            },
            new ImageByItemGraphQLModel
            {
                DisplayOrder = 1,
                S3Bucket = "product-images",
                S3BucketDirectory = "items",
                S3FileName = "xyz.png"
            }
        ];
        return entity;
    }

    /// <summary>
    /// Builds a VM instance with a real S3Helper holding fake credentials. The AWS SDK does not
    /// validate credentials at construction time, so as long as no S3 API is actually called the
    /// instance is safe to use in unit tests. Used for tests that need IsS3Available == true.
    /// </summary>
    private ItemDetailViewModel CreateVmWithS3()
    {
        Common.Helpers.S3Helper s3 = new(
            accessKey: "fake-access",
            secretKey: "fake-secret",
            region: Amazon.RegionEndpoint.USEast1,
            bucket: "fake-bucket",
            directory: "fake-dir");

        return new ItemDetailViewModel(
            _service,
            _eventAggregator,
            _dialogService,
            _stringLengthCache,
            _measurementUnitCache,
            _itemBrandCache,
            _accountingGroupCache,
            _itemSizeCategoryCache,
            _joinableTaskFactory,
            _validator,
            _mapper,
            s3Helper: s3,
            localImageCachePath: string.Empty);
    }

    /// <summary>
    /// Payload row for a single image. The ChangeCollector transformer maps each
    /// ImageByItemDTO → anonymous { s3Bucket, s3BucketDirectory, s3FileName, displayOrder }.
    /// </summary>
    private record ImagePayload(string s3Bucket, string s3BucketDirectory, string s3FileName, int displayOrder);

    /// <summary>
    /// Extracts the images collection from the captured GraphQL variables ExpandoObject.
    /// Uses reflection because the anonymous types emitted by ExecuteSaveAsync are internal
    /// to the NetErp assembly and can't be accessed via dynamic from NetErp.Tests.
    /// </summary>
    private static List<ImagePayload> ExtractImages(object? variables, string prefix)
    {
        variables.Should().NotBeNull("ExecuteSaveAsync should have invoked the service with a variables object");

        IDictionary<string, object?> root = (IDictionary<string, object?>)variables!;
        root.Should().ContainKey(prefix);

        IDictionary<string, object?> input = (IDictionary<string, object?>)root[prefix]!;
        input.Should().ContainKey("images");

        List<ImagePayload> result = [];
        foreach (object? i in (System.Collections.IEnumerable)input["images"]!)
        {
            i.Should().NotBeNull();
            Type t = i!.GetType();
            string s3Bucket = (string)t.GetProperty("s3Bucket")!.GetValue(i)!;
            string s3BucketDirectory = (string)t.GetProperty("s3BucketDirectory")!.GetValue(i)!;
            string s3FileName = (string)t.GetProperty("s3FileName")!.GetValue(i)!;
            int displayOrder = (int)t.GetProperty("displayOrder")!.GetValue(i)!;
            result.Add(new ImagePayload(s3Bucket, s3BucketDirectory, s3FileName, displayOrder));
        }
        return result;
    }

    /// <summary>
    /// Extracts the components collection from the captured GraphQL variables ExpandoObject.
    /// The ChangeCollector transformer maps each ComponentsByItemDTO → anonymous
    /// { itemId, quantity } pair. Anonymous types declared in another assembly (NetErp)
    /// are internal, so we can't use dynamic here — use reflection instead.
    /// </summary>
    private static List<(int itemId, decimal quantity)> ExtractComponents(object? variables, string prefix)
    {
        variables.Should().NotBeNull("ExecuteSaveAsync should have invoked the service with a variables object");

        IDictionary<string, object?> root = (IDictionary<string, object?>)variables!;
        root.Should().ContainKey(prefix);

        IDictionary<string, object?> input = (IDictionary<string, object?>)root[prefix]!;
        input.Should().ContainKey("components");

        List<(int itemId, decimal quantity)> result = [];
        foreach (object? c in (System.Collections.IEnumerable)input["components"]!)
        {
            c.Should().NotBeNull();
            Type t = c!.GetType();
            int itemId = (int)t.GetProperty("itemId")!.GetValue(c)!;
            decimal quantity = (decimal)t.GetProperty("quantity")!.GetValue(c)!;
            result.Add((itemId, quantity));
        }
        return result;
    }

    /// <summary>
    /// Extracts the eanCodes list from the captured GraphQL variables ExpandoObject.
    /// Navigates variables[prefix].eanCodes and materializes as List&lt;string&gt;.
    /// </summary>
    private static List<string> ExtractEanCodes(object? variables, string prefix)
    {
        variables.Should().NotBeNull("ExecuteSaveAsync should have invoked the service with a variables object");

        IDictionary<string, object?> root = (IDictionary<string, object?>)variables!;
        root.Should().ContainKey(prefix);

        IDictionary<string, object?> input = (IDictionary<string, object?>)root[prefix]!;
        input.Should().ContainKey("eanCodes");

        IEnumerable<string> eanCodes = (IEnumerable<string>)input["eanCodes"]!;
        return [.. eanCodes];
    }

    #endregion
}
