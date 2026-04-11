using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Common.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using Models.Inventory;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.Services;
using NetErp.Inventory.MeasurementUnits.Validators;
using NetErp.Inventory.MeasurementUnits.ViewModels;
using NSubstitute;
using Xunit;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.ViewModels;

[Collection("Debounce tests")]
public class MeasurementUnitViewModelTests
{
    private const int TestDebounceDelayMs = 30;
    private const int BeforeDebounceWaitMs = 10;
    private const int DebounceWaitMs = 100;

    private readonly IEventAggregator _eventAggregator;
    private readonly IRepository<MeasurementUnitGraphQLModel> _service;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly StringLengthCache _stringLengthCache;
    private readonly PermissionCache _permissionCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly MeasurementUnitValidator _validator;
    private readonly DebouncedAction _searchDebounce;
    private readonly MeasurementUnitViewModel _vm;

    public MeasurementUnitViewModelTests()
    {
        _eventAggregator = Substitute.For<IEventAggregator>();
        _service = Substitute.For<IRepository<MeasurementUnitGraphQLModel>>();
        _notificationService = Substitute.For<INotificationService>();
        _dialogService = Substitute.For<IDialogService>();

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        IRepository<PermissionDefinitionGraphQLModel> permDefRepo = Substitute.For<IRepository<PermissionDefinitionGraphQLModel>>();
        PermissionDefinitionCache permissionDefinitionCache = new(permDefRepo, _eventAggregator);

        IRepository<CompanyPermissionDefaultGraphQLModel> companyPermRepo = Substitute.For<IRepository<CompanyPermissionDefaultGraphQLModel>>();
        CompanyPermissionDefaultCache companyPermissionDefaultCache = new(companyPermRepo, _eventAggregator);

        IRepository<UserPermissionGraphQLModel> userPermRepo = Substitute.For<IRepository<UserPermissionGraphQLModel>>();
        UserPermissionCache userPermissionCache = new(userPermRepo, _eventAggregator);

        IGraphQLClient graphQLClient = Substitute.For<IGraphQLClient>();
        _permissionCache = new PermissionCache(permissionDefinitionCache, companyPermissionDefaultCache, userPermissionCache, graphQLClient, _eventAggregator);

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;

        _validator = new MeasurementUnitValidator();
        _searchDebounce = new DebouncedAction(delayMs: TestDebounceDelayMs);

        _vm = new MeasurementUnitViewModel(
            _eventAggregator,
            _service,
            _notificationService,
            _dialogService,
            _stringLengthCache,
            _permissionCache,
            _joinableTaskFactory,
            _validator,
            _searchDebounce);
    }

    private void SetupServiceReturnsEmptyPage()
    {
        PageType<MeasurementUnitGraphQLModel> emptyPage = new()
        {
            TotalEntries = 0,
            Entries = new ObservableCollection<MeasurementUnitGraphQLModel>()
        };
        _service.GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(emptyPage);
    }

    private async Task WarmUpAsync()
    {
        SetupServiceReturnsEmptyPage();
        await _vm.LoadMeasurementUnitsAsync();
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
        System.Action act = () => new MeasurementUnitViewModel(
            _eventAggregator, _service, _notificationService, _dialogService,
            _stringLengthCache, _permissionCache, _joinableTaskFactory, _validator, null!);

        act.Should().Throw<System.ArgumentNullException>().WithParameterName("searchDebounce");
    }

    #endregion
}
