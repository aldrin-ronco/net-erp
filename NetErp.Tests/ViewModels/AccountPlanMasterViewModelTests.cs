using System.Collections.Generic;
using System.Threading.Tasks;
using Caliburn.Micro;
using Common.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Books.AccountingAccounts.Validators;
using NetErp.Books.AccountingAccounts.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.Services;
using NSubstitute;
using Xunit;

namespace NetErp.Tests.ViewModels;

/// <summary>
/// Tests de debounce para el master de AccountPlan. A diferencia de los otros
/// masters, la búsqueda de AccountPlan no llama al servicio GraphQL — filtra
/// localmente la lista <c>Accounts</c> y actualiza <c>SearchResults</c>. Los
/// asserts verifican ese efecto observable.
/// </summary>
[Collection("Debounce tests")]
public class AccountPlanMasterViewModelTests
{
    private const int TestDebounceDelayMs = 30;
    private const int BeforeDebounceWaitMs = 10;
    private const int DebounceWaitMs = 100;

    private readonly IRepository<AccountingAccountGraphQLModel> _service;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly IEventAggregator _eventAggregator;
    private readonly StringLengthCache _stringLengthCache;
    private readonly PermissionCache _permissionCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly AccountPlanValidator _validator;
    private readonly DebouncedAction _searchDebounce;
    private readonly AccountPlanMasterViewModel _vm;

    public AccountPlanMasterViewModelTests()
    {
        _service = Substitute.For<IRepository<AccountingAccountGraphQLModel>>();
        _notificationService = Substitute.For<INotificationService>();
        _dialogService = Substitute.For<IDialogService>();
        _eventAggregator = Substitute.For<IEventAggregator>();

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

        _validator = new AccountPlanValidator();
        _searchDebounce = new DebouncedAction(delayMs: TestDebounceDelayMs);

        _vm = new AccountPlanMasterViewModel(
            _service,
            _notificationService,
            _dialogService,
            _eventAggregator,
            _stringLengthCache,
            _permissionCache,
            _joinableTaskFactory,
            _validator,
            _searchDebounce);

        // Pre-popular la lista Accounts con datos de prueba — la búsqueda
        // filtra esta lista en memoria, no llama al servicio.
        _vm.Accounts = new List<AccountingAccountGraphQLModel>
        {
            new() { Id = 1, Code = "1105", Name = "Caja General" },
            new() { Id = 2, Code = "1110", Name = "Bancos" },
            new() { Id = 3, Code = "2105", Name = "Proveedores Nacionales" },
            new() { Id = 4, Code = "4135", Name = "Comercio al por Mayor" },
            new() { Id = 5, Code = "5105", Name = "Gastos de Personal" }
        };
    }

    #region Debounce en SearchText

    [Fact]
    public async Task SearchText_SingleAssignment_DoesNotFilterImmediately()
    {
        _vm.SearchText = "caja";

        await Task.Delay(BeforeDebounceWaitMs);

        _vm.SearchResults.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchText_SingleAssignment_FiltersAfterDebounce()
    {
        _vm.SearchText = "caja";

        await Task.Delay(DebounceWaitMs);

        _vm.SearchResults.Should().ContainSingle()
            .Which.Code.Should().Be("1105");
    }

    [Fact]
    public async Task SearchText_RapidSuccessiveChanges_AppliesOnlyLastFilter()
    {
        // Simular tipeo rápido: cada set cancela el filter anterior.
        _vm.SearchText = "ca";
        await Task.Delay(5);
        _vm.SearchText = "caj";
        await Task.Delay(5);
        _vm.SearchText = "caja";
        await Task.Delay(5);
        _vm.SearchText = "cajaa"; // Esta última no debe matchear.

        await Task.Delay(DebounceWaitMs);

        // El filtro final es "cajaa" → no match → SearchResults vacío.
        _vm.SearchResults.Should().BeEmpty();
        _vm.ShowSearchResults.Should().BeFalse();
    }

    [Fact]
    public async Task SearchText_ShorterThanMinimum_DoesNotFilter()
    {
        // SearchText tiene length gate de >= 2 (no >= 3 como otros masters).
        _vm.SearchText = "c";

        await Task.Delay(DebounceWaitMs);

        _vm.SearchResults.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchText_ClearedToEmpty_ClearsResultsImmediately()
    {
        // Primero hacer una búsqueda que produzca resultados.
        _vm.SearchText = "bancos";
        await Task.Delay(DebounceWaitMs);
        _vm.SearchResults.Should().NotBeEmpty();

        // Vaciar → el setter limpia SearchResults síncronamente (no pasa por
        // debounce en esta ruta; el setter tiene la rama string.IsNullOrEmpty
        // que resetea directamente).
        _vm.SearchText = "";

        _vm.SearchResults.Should().BeEmpty();
        _vm.ShowSearchResults.Should().BeFalse();
    }

    [Fact]
    public async Task SearchText_TwoSeparateSearches_UpdatesResultsTwice()
    {
        _vm.SearchText = "caja";
        await Task.Delay(DebounceWaitMs);
        _vm.SearchResults.Should().HaveCount(1);
        _vm.SearchResults[0].Code.Should().Be("1105");

        _vm.SearchText = "bancos";
        await Task.Delay(DebounceWaitMs);

        _vm.SearchResults.Should().HaveCount(1);
        _vm.SearchResults[0].Code.Should().Be("1110");
    }

    [Fact]
    public async Task SearchText_BoundaryTiming_MatchesConfiguredDelay()
    {
        _vm.SearchText = "caja";

        // Bien antes del delay → aún no se ha aplicado el filtro.
        await Task.Delay(TestDebounceDelayMs / 3);
        _vm.SearchResults.Should().BeEmpty();

        // Bien después del delay → filtro aplicado.
        await Task.Delay(DebounceWaitMs);
        _vm.SearchResults.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_NullSearchDebounce_Throws()
    {
        System.Action act = () => new AccountPlanMasterViewModel(
            _service, _notificationService, _dialogService, _eventAggregator,
            _stringLengthCache, _permissionCache, _joinableTaskFactory, _validator,
            null!);

        act.Should().Throw<System.ArgumentNullException>().WithParameterName("searchDebounce");
    }

    #endregion
}
