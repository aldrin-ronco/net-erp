using System.Collections.ObjectModel;
using System.Reflection;
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
using NetErp.Helpers.Services;
using NetErp.Inventory.CatalogItems.Validators;
using NetErp.Inventory.CatalogItems.ViewModels;
using NSubstitute;
using Xunit;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.ViewModels;

/// <summary>
/// Tests de debounce para el master de CatalogRoot. A diferencia de los
/// masters list-based, la búsqueda filtra un árbol en memoria a partir de
/// items obtenidos del servicio. El gate mínimo es 4 caracteres (no 3).
///
/// <para>Marcado como CollectionDefinition para forzar serialización —
/// SearchItemsAsync hace bastante trabajo y bajo contención del thread pool
/// (cuando el suite corre en paralelo) los timings del debounce se vuelven
/// inestables. Correr los tests de esta clase de forma serial elimina esa
/// contención.</para>
/// </summary>
[Collection("Debounce tests")]
public class CatalogRootMasterViewModelTests
{
    private const int TestDebounceDelayMs = 50;
    private const int BeforeDebounceWaitMs = 15;
    // Wait más generoso que otros masters para absorber contención del thread
    // pool cuando el suite corre en paralelo — SearchItemsAsync hace extra
    // trabajo (BuildTreeFromSearchResults, volatile reads, locks).
    private const int DebounceWaitMs = 300;

    private readonly IMapper _mapper;
    private readonly IEventAggregator _eventAggregator;
    private readonly IRepository<CatalogGraphQLModel> _catalogService;
    private readonly IRepository<ItemTypeGraphQLModel> _itemTypeService;
    private readonly IRepository<ItemCategoryGraphQLModel> _itemCategoryService;
    private readonly IRepository<ItemSubCategoryGraphQLModel> _itemSubCategoryService;
    private readonly IRepository<ItemGraphQLModel> _itemService;
    private readonly IRepository<S3StorageLocationGraphQLModel> _s3LocationService;
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly CatalogCache _catalogCache;
    private readonly MeasurementUnitCache _measurementUnitCache;
    private readonly ItemBrandCache _itemBrandCache;
    private readonly AccountingGroupCache _accountingGroupCache;
    private readonly ItemSizeCategoryCache _itemSizeCategoryCache;
    private readonly StringLengthCache _stringLengthCache;
    private readonly CatalogValidator _catalogValidator;
    private readonly ItemTypeValidator _itemTypeValidator;
    private readonly ItemCategoryValidator _itemCategoryValidator;
    private readonly ItemSubCategoryValidator _itemSubCategoryValidator;
    private readonly ItemValidator _itemValidator;
    private readonly IGraphQLClient _graphQLClient;
    private readonly PermissionCache _permissionCache;
    private readonly DebouncedAction _searchDebounce;
    private readonly CatalogViewModel _context;
    private readonly CatalogRootMasterViewModel _vm;

    public CatalogRootMasterViewModelTests()
    {
        _mapper = Substitute.For<IMapper>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _catalogService = Substitute.For<IRepository<CatalogGraphQLModel>>();
        _itemTypeService = Substitute.For<IRepository<ItemTypeGraphQLModel>>();
        _itemCategoryService = Substitute.For<IRepository<ItemCategoryGraphQLModel>>();
        _itemSubCategoryService = Substitute.For<IRepository<ItemSubCategoryGraphQLModel>>();
        _itemService = Substitute.For<IRepository<ItemGraphQLModel>>();
        _s3LocationService = Substitute.For<IRepository<S3StorageLocationGraphQLModel>>();
        _dialogService = Substitute.For<IDialogService>();
        _notificationService = Substitute.For<INotificationService>();

        _catalogCache = new CatalogCache(_catalogService, _eventAggregator);
        _measurementUnitCache = new MeasurementUnitCache(
            Substitute.For<IRepository<MeasurementUnitGraphQLModel>>(), _eventAggregator);
        _itemBrandCache = new ItemBrandCache(
            Substitute.For<IRepository<ItemBrandGraphQLModel>>(), _eventAggregator);
        _accountingGroupCache = new AccountingGroupCache(
            Substitute.For<IRepository<AccountingGroupGraphQLModel>>(), _eventAggregator);
        _itemSizeCategoryCache = new ItemSizeCategoryCache(
            Substitute.For<IRepository<ItemSizeCategoryGraphQLModel>>(), _eventAggregator);

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        _catalogValidator = new CatalogValidator();
        _itemTypeValidator = new ItemTypeValidator();
        _itemCategoryValidator = new ItemCategoryValidator();
        _itemSubCategoryValidator = new ItemSubCategoryValidator();
        _itemValidator = new ItemValidator();

        _graphQLClient = Substitute.For<IGraphQLClient>();

        IRepository<PermissionDefinitionGraphQLModel> permDefRepo = Substitute.For<IRepository<PermissionDefinitionGraphQLModel>>();
        PermissionDefinitionCache permissionDefinitionCache = new(permDefRepo, _eventAggregator);

        IRepository<CompanyPermissionDefaultGraphQLModel> companyPermRepo = Substitute.For<IRepository<CompanyPermissionDefaultGraphQLModel>>();
        CompanyPermissionDefaultCache companyPermissionDefaultCache = new(companyPermRepo, _eventAggregator);

        IRepository<UserPermissionGraphQLModel> userPermRepo = Substitute.For<IRepository<UserPermissionGraphQLModel>>();
        UserPermissionCache userPermissionCache = new(userPermRepo, _eventAggregator);

        _permissionCache = new PermissionCache(permissionDefinitionCache, companyPermissionDefaultCache, userPermissionCache, _graphQLClient, _eventAggregator);

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;

        _searchDebounce = new DebouncedAction(delayMs: TestDebounceDelayMs);

        // CatalogViewModel es el parent Context — construirlo con las mismas
        // deps. Su comportamiento no se ejercita en estos tests (solo se
        // referencia por el master).
        _context = new CatalogViewModel(
            _mapper, _eventAggregator, _catalogService, _itemTypeService,
            _itemCategoryService, _itemSubCategoryService, _itemService, _s3LocationService,
            _dialogService, _notificationService, _joinableTaskFactory,
            _catalogCache, _measurementUnitCache, _itemBrandCache, _accountingGroupCache,
            _itemSizeCategoryCache, _stringLengthCache,
            _catalogValidator, _itemTypeValidator, _itemCategoryValidator,
            _itemSubCategoryValidator, _itemValidator,
            _graphQLClient, _permissionCache, _searchDebounce);

        _vm = new CatalogRootMasterViewModel(
            _context, _catalogService, _itemTypeService, _itemCategoryService,
            _itemSubCategoryService, _itemService, _s3LocationService,
            _dialogService, _notificationService, _eventAggregator, _mapper,
            _joinableTaskFactory, _catalogCache, _measurementUnitCache,
            _itemBrandCache, _accountingGroupCache, _itemSizeCategoryCache,
            _stringLengthCache, _catalogValidator, _itemTypeValidator,
            _itemCategoryValidator, _itemSubCategoryValidator, _itemValidator,
            _graphQLClient, _permissionCache, _searchDebounce);
    }

    private void SetupItemServiceReturnsEmptyPage()
    {
        PageType<ItemGraphQLModel> emptyPage = new()
        {
            TotalEntries = 0,
            Entries = new ObservableCollection<ItemGraphQLModel>()
        };
        _itemService.GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(emptyPage);
    }

    /// <summary>
    /// Pre-calienta JIT del path SearchItemsAsync via reflection — invoca el
    /// método privado directamente con una búsqueda throwaway. Bypass del
    /// debounce evita dejar tareas en vuelo que ensucien el contador del
    /// siguiente test bajo contención del thread pool.
    /// </summary>
    private async Task WarmUpAsync()
    {
        SetupItemServiceReturnsEmptyPage();

        // Seed FilterSearch via reflection para que SearchItemsAsync no haga
        // early-return (gate de MinSearchLength). Evita pasar por el setter
        // que dispararía el debounce.
        PropertyInfo filterProp = typeof(CatalogRootMasterViewModel)
            .GetProperty(nameof(CatalogRootMasterViewModel.FilterSearch))!;
        // field-backed auto-property: el setter está disponible
        filterProp.SetValue(_vm, "warmup");

        // Invocar directamente SearchItemsAsync (privado)
        MethodInfo searchMethod = typeof(CatalogRootMasterViewModel)
            .GetMethod("SearchItemsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Task task = (Task)searchMethod.Invoke(_vm, null)!;
        await task;

        // Resetear estado: limpiar FilterSearch (via reflection para no
        // disparar ClearSearch del setter) y el contador del mock.
        filterProp.SetValue(_vm, string.Empty);
        _itemService.ClearReceivedCalls();
    }

    #region Debounce en FilterSearch

    [Fact]
    public async Task FilterSearch_SingleAssignment_DoesNotCallServiceImmediately()
    {
        await WarmUpAsync();

        _vm.FilterSearch = "test";

        await Task.Delay(BeforeDebounceWaitMs);

        await _itemService.DidNotReceive().GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterSearch_SingleAssignment_CallsServiceOnceAfterDebounce()
    {
        await WarmUpAsync();

        _vm.FilterSearch = "test";

        await Task.Delay(DebounceWaitMs);

        await _itemService.Received(1).GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterSearch_RapidSuccessiveChanges_CallsServiceOnlyOnce()
    {
        await WarmUpAsync();

        _vm.FilterSearch = "tese";
        await Task.Delay(5);
        _vm.FilterSearch = "tesex";
        await Task.Delay(5);
        _vm.FilterSearch = "tesexy";
        await Task.Delay(5);
        _vm.FilterSearch = "tesexyy";

        await Task.Delay(DebounceWaitMs);

        await _itemService.Received(1).GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterSearch_ShorterThanMinimum_DoesNotCallService()
    {
        await WarmUpAsync();

        // MinSearchLength = 4; "tes" (3 caracteres) NO debe disparar búsqueda.
        _vm.FilterSearch = "tes";

        await Task.Delay(DebounceWaitMs);

        await _itemService.DidNotReceive().GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterSearch_ClearedToEmpty_DoesNotCallService()
    {
        // En CatalogRootMasterViewModel, vaciar la búsqueda NO dispara una
        // nueva carga — solo restaura el árbol previo vía ClearSearch().
        await WarmUpAsync();

        _vm.FilterSearch = "test";
        await Task.Delay(DebounceWaitMs);
        _itemService.ClearReceivedCalls();

        _vm.FilterSearch = "";
        await Task.Delay(DebounceWaitMs);

        // El clear NO debe resultar en una llamada al servicio.
        await _itemService.DidNotReceive().GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
        _vm.IsSearching.Should().BeFalse();
    }

    [Fact]
    public async Task FilterSearch_TwoSeparateSearches_CallsServiceTwice()
    {
        await WarmUpAsync();

        _vm.FilterSearch = "uno1";
        await Task.Delay(DebounceWaitMs);

        _vm.FilterSearch = "dos2";
        await Task.Delay(DebounceWaitMs);

        await _itemService.Received(2).GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterSearch_BoundaryTiming_MatchesConfiguredDelay()
    {
        await WarmUpAsync();

        _vm.FilterSearch = "test";

        await Task.Delay(TestDebounceDelayMs / 3);
        await _itemService.DidNotReceive().GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());

        await Task.Delay(DebounceWaitMs);
        await _itemService.Received(1).GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Constructor_NullSearchDebounce_Throws()
    {
        System.Action act = () => new CatalogRootMasterViewModel(
            _context, _catalogService, _itemTypeService, _itemCategoryService,
            _itemSubCategoryService, _itemService, _s3LocationService,
            _dialogService, _notificationService, _eventAggregator, _mapper,
            _joinableTaskFactory, _catalogCache, _measurementUnitCache,
            _itemBrandCache, _accountingGroupCache, _itemSizeCategoryCache,
            _stringLengthCache, _catalogValidator, _itemTypeValidator,
            _itemCategoryValidator, _itemSubCategoryValidator, _itemValidator,
            _graphQLClient, _permissionCache, null!);

        act.Should().Throw<System.ArgumentNullException>().WithParameterName("searchDebounce");
    }

    #endregion
}
