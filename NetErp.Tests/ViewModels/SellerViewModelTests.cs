using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using Models.Books;
using Models.Global;
using NetErp.Billing.Sellers.Validators;
using NetErp.Billing.Sellers.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.Services;
using NSubstitute;
using Xunit;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.ViewModels;

/// <summary>
/// Tests de debounce para el master de Seller. Espeja el patrón de
/// AccountingEntityViewModelTests: inyecta un <see cref="DebouncedAction"/>
/// con delay corto (30ms) para determinismo y velocidad.
/// </summary>
[Collection("Debounce tests")]
public class SellerViewModelTests
{
    private const int TestDebounceDelayMs = 30;
    private const int BeforeDebounceWaitMs = 10;
    private const int DebounceWaitMs = 100;

    private readonly IMapper _mapper;
    private readonly IEventAggregator _eventAggregator;
    private readonly INotificationService _notificationService;
    private readonly IRepository<SellerGraphQLModel> _service;
    private readonly IDialogService _dialogService;
    private readonly CostCenterCache _costCenterCache;
    private readonly IdentificationTypeCache _identificationTypeCache;
    private readonly CountryCache _countryCache;
    private readonly ZoneCache _zoneCache;
    private readonly StringLengthCache _stringLengthCache;
    private readonly PermissionCache _permissionCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly IGraphQLClient _graphQLClient;
    private readonly SellerValidator _validator;
    private readonly DebouncedAction _searchDebounce;
    private readonly SellerViewModel _vm;

    public SellerViewModelTests()
    {
        _mapper = Substitute.For<IMapper>();
        _mapper.Map<ObservableCollection<SellerDTO>>(Arg.Any<object>())
            .Returns(new ObservableCollection<SellerDTO>());
        _eventAggregator = Substitute.For<IEventAggregator>();
        _notificationService = Substitute.For<INotificationService>();
        _service = Substitute.For<IRepository<SellerGraphQLModel>>();
        _dialogService = Substitute.For<IDialogService>();

        IRepository<CostCenterGraphQLModel> costCenterRepo = Substitute.For<IRepository<CostCenterGraphQLModel>>();
        _costCenterCache = new CostCenterCache(costCenterRepo, _eventAggregator);

        IRepository<IdentificationTypeGraphQLModel> idTypeRepo = Substitute.For<IRepository<IdentificationTypeGraphQLModel>>();
        _identificationTypeCache = new IdentificationTypeCache(idTypeRepo, _eventAggregator);

        IRepository<CountryGraphQLModel> countryRepo = Substitute.For<IRepository<CountryGraphQLModel>>();
        _countryCache = new CountryCache(countryRepo, _eventAggregator);

        IRepository<ZoneGraphQLModel> zoneRepo = Substitute.For<IRepository<ZoneGraphQLModel>>();
        _zoneCache = new ZoneCache(zoneRepo, _eventAggregator);

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        IRepository<PermissionDefinitionGraphQLModel> permDefRepo = Substitute.For<IRepository<PermissionDefinitionGraphQLModel>>();
        PermissionDefinitionCache permissionDefinitionCache = new(permDefRepo, _eventAggregator);

        IRepository<CompanyPermissionDefaultGraphQLModel> companyPermRepo = Substitute.For<IRepository<CompanyPermissionDefaultGraphQLModel>>();
        CompanyPermissionDefaultCache companyPermissionDefaultCache = new(companyPermRepo, _eventAggregator);

        IRepository<UserPermissionGraphQLModel> userPermRepo = Substitute.For<IRepository<UserPermissionGraphQLModel>>();
        UserPermissionCache userPermissionCache = new(userPermRepo, _eventAggregator);

        _graphQLClient = Substitute.For<IGraphQLClient>();
        _permissionCache = new PermissionCache(permissionDefinitionCache, companyPermissionDefaultCache, userPermissionCache, _graphQLClient, _eventAggregator);

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;

        _validator = new SellerValidator();
        _searchDebounce = new DebouncedAction(delayMs: TestDebounceDelayMs);

        _vm = new SellerViewModel(
            _mapper,
            _eventAggregator,
            _service,
            _notificationService,
            _costCenterCache,
            _identificationTypeCache,
            _countryCache,
            _zoneCache,
            _stringLengthCache,
            _dialogService,
            _joinableTaskFactory,
            _graphQLClient,
            _validator,
            _permissionCache,
            _searchDebounce);
    }

    private void SetupServiceReturnsEmptyPage()
    {
        PageType<SellerGraphQLModel> emptyPage = new()
        {
            TotalEntries = 0,
            Entries = new ObservableCollection<SellerGraphQLModel>()
        };
        _service.GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(emptyPage);
    }

    private async Task WarmUpAsync()
    {
        SetupServiceReturnsEmptyPage();
        await _vm.LoadSellersAsync();
        _service.ClearReceivedCalls();
    }

    #region Debounce en FilterSearch

    [Fact]
    public async Task FilterSearch_SingleAssignment_DoesNotCallServiceImmediately()
    {
        await WarmUpAsync();

        _vm.FilterSearch = "test";

        await Task.Delay(BeforeDebounceWaitMs);

        await _service.DidNotReceive().GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterSearch_SingleAssignment_CallsServiceOnceAfterDebounce()
    {
        await WarmUpAsync();

        _vm.FilterSearch = "test";

        await Task.Delay(DebounceWaitMs);

        await _service.Received(1).GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterSearch_RapidSuccessiveChanges_CallsServiceOnlyOnce()
    {
        await WarmUpAsync();

        _vm.FilterSearch = "tes";
        await Task.Delay(5);
        _vm.FilterSearch = "test";
        await Task.Delay(5);
        _vm.FilterSearch = "testi";
        await Task.Delay(5);
        _vm.FilterSearch = "testin";

        await Task.Delay(DebounceWaitMs);

        await _service.Received(1).GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterSearch_ShorterThanMinimum_DoesNotCallService()
    {
        await WarmUpAsync();

        _vm.FilterSearch = "te";

        await Task.Delay(DebounceWaitMs);

        await _service.DidNotReceive().GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterSearch_ClearedToEmpty_TriggersDebouncedLoad()
    {
        await WarmUpAsync();

        _vm.FilterSearch = "test";
        await Task.Delay(DebounceWaitMs);

        _vm.FilterSearch = "";
        await Task.Delay(DebounceWaitMs);

        await _service.Received(2).GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterSearch_TwoSeparateSearches_CallsServiceTwice()
    {
        await WarmUpAsync();

        _vm.FilterSearch = "uno";
        await Task.Delay(DebounceWaitMs);

        _vm.FilterSearch = "dos";
        await Task.Delay(DebounceWaitMs);

        await _service.Received(2).GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterSearch_BoundaryTiming_MatchesConfiguredDelay()
    {
        await WarmUpAsync();

        _vm.FilterSearch = "test";

        await Task.Delay(TestDebounceDelayMs / 3);
        await _service.DidNotReceive().GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());

        await Task.Delay(DebounceWaitMs);
        await _service.Received(1).GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Constructor_NullSearchDebounce_Throws()
    {
        System.Action act = () => new SellerViewModel(
            _mapper, _eventAggregator, _service, _notificationService,
            _costCenterCache, _identificationTypeCache, _countryCache, _zoneCache,
            _stringLengthCache, _dialogService, _joinableTaskFactory, _graphQLClient,
            _validator, _permissionCache, null!);

        act.Should().Throw<System.ArgumentNullException>().WithParameterName("searchDebounce");
    }

    #endregion
}
