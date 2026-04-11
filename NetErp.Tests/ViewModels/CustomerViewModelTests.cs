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
using NetErp.Billing.Customers.Validators;
using NetErp.Billing.Customers.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.Services;
using NSubstitute;
using Xunit;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.ViewModels;

/// <summary>
/// Tests de debounce para el master de Customer. Espeja el patrón de
/// AccountingEntityViewModelTests: inyecta un <see cref="DebouncedAction"/>
/// con delay corto (30ms) para determinismo y velocidad.
/// </summary>
[Collection("Debounce tests")]
public class CustomerViewModelTests
{
    private const int TestDebounceDelayMs = 30;
    private const int BeforeDebounceWaitMs = 10;
    private const int DebounceWaitMs = 100;

    private readonly IMapper _mapper;
    private readonly IEventAggregator _eventAggregator;
    private readonly INotificationService _notificationService;
    private readonly IRepository<CustomerGraphQLModel> _service;
    private readonly IDialogService _dialogService;
    private readonly IdentificationTypeCache _identificationTypeCache;
    private readonly CountryCache _countryCache;
    private readonly WithholdingTypeCache _withholdingTypeCache;
    private readonly ZoneCache _zoneCache;
    private readonly StringLengthCache _stringLengthCache;
    private readonly PermissionCache _permissionCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly IGraphQLClient _graphQLClient;
    private readonly CustomerValidator _validator;
    private readonly DebouncedAction _searchDebounce;
    private readonly CustomerViewModel _vm;

    public CustomerViewModelTests()
    {
        _mapper = Substitute.For<IMapper>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _notificationService = Substitute.For<INotificationService>();
        _service = Substitute.For<IRepository<CustomerGraphQLModel>>();
        _dialogService = Substitute.For<IDialogService>();

        IRepository<IdentificationTypeGraphQLModel> idTypeRepo = Substitute.For<IRepository<IdentificationTypeGraphQLModel>>();
        _identificationTypeCache = new IdentificationTypeCache(idTypeRepo, _eventAggregator);

        IRepository<CountryGraphQLModel> countryRepo = Substitute.For<IRepository<CountryGraphQLModel>>();
        _countryCache = new CountryCache(countryRepo, _eventAggregator);

        IRepository<WithholdingTypeGraphQLModel> withholdingRepo = Substitute.For<IRepository<WithholdingTypeGraphQLModel>>();
        _withholdingTypeCache = new WithholdingTypeCache(withholdingRepo, _eventAggregator);

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

        _validator = new CustomerValidator();
        _searchDebounce = new DebouncedAction(delayMs: TestDebounceDelayMs);

        _vm = new CustomerViewModel(
            _mapper,
            _eventAggregator,
            _notificationService,
            _service,
            _dialogService,
            _identificationTypeCache,
            _countryCache,
            _withholdingTypeCache,
            _zoneCache,
            _stringLengthCache,
            _permissionCache,
            _joinableTaskFactory,
            _graphQLClient,
            _validator,
            _searchDebounce);
    }

    private void SetupServiceReturnsEmptyPage()
    {
        PageType<CustomerGraphQLModel> emptyPage = new()
        {
            TotalEntries = 0,
            Entries = new ObservableCollection<CustomerGraphQLModel>()
        };
        _service.GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(emptyPage);
    }

    /// <summary>
    /// Ejecuta una carga directa para pre-calentar Lazy estáticos y JIT
    /// antes de ejercer la ruta debounced en el test.
    /// </summary>
    private async Task WarmUpAsync()
    {
        SetupServiceReturnsEmptyPage();
        await _vm.LoadCustomersAsync();
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
        System.Action act = () => new CustomerViewModel(
            _mapper, _eventAggregator, _notificationService, _service, _dialogService,
            _identificationTypeCache, _countryCache, _withholdingTypeCache, _zoneCache,
            _stringLengthCache, _permissionCache, _joinableTaskFactory, _graphQLClient,
            _validator, null!);

        act.Should().Throw<System.ArgumentNullException>().WithParameterName("searchDebounce");
    }

    #endregion
}
