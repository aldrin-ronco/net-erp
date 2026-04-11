using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Common.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Books.AccountingEntities.Validators;
using NetErp.Books.AccountingEntities.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.Services;
using NSubstitute;
using Xunit;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.ViewModels;

/// <summary>
/// Tests para el master de AccountingEntity. Se enfocan en el comportamiento
/// de debounce de la búsqueda (FilterSearch): los caracteres mecanografiados
/// rápido NO deben disparar una llamada por cada set, sino una sola después
/// del delay del <see cref="DebouncedAction"/>.
///
/// <para>El <see cref="DebouncedAction"/> se inyecta por constructor, así que
/// los tests pasan uno con delay muy corto (30ms) para mantener el suite
/// rápido y determinista, sin depender de delays reales de 500ms.</para>
/// </summary>
[Collection("Debounce tests")]
public class AccountingEntityViewModelTests
{
    // Delay corto pero suficiente para que las cancelaciones de rapid-typing
    // se observen sin race conditions. El delay real en producción es 500ms;
    // inyectamos uno de 30ms en tests para que el suite no tarde 6s.
    private const int TestDebounceDelayMs = 30;

    // Cuánto esperar después de "tipear" para confirmar que NO se llamó al
    // servicio (menos que TestDebounceDelayMs).
    private const int BeforeDebounceWaitMs = 10;

    // Cuánto esperar después de "tipear" para confirmar que SÍ se llamó al
    // servicio (más que TestDebounceDelayMs con margen).
    private const int DebounceWaitMs = 100;

    private readonly IEventAggregator _eventAggregator;
    private readonly INotificationService _notificationService;
    private readonly IRepository<AccountingEntityGraphQLModel> _service;
    private readonly IDialogService _dialogService;
    private readonly IdentificationTypeCache _identificationTypeCache;
    private readonly CountryCache _countryCache;
    private readonly StringLengthCache _stringLengthCache;
    private readonly PermissionCache _permissionCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly AccountingEntityValidator _validator;
    private readonly DebouncedAction _searchDebounce;
    private readonly AccountingEntityViewModel _vm;

    public AccountingEntityViewModelTests()
    {
        _eventAggregator = Substitute.For<IEventAggregator>();
        _notificationService = Substitute.For<INotificationService>();
        _service = Substitute.For<IRepository<AccountingEntityGraphQLModel>>();
        _dialogService = Substitute.For<IDialogService>();

        IRepository<IdentificationTypeGraphQLModel> idTypeRepo = Substitute.For<IRepository<IdentificationTypeGraphQLModel>>();
        _identificationTypeCache = new IdentificationTypeCache(idTypeRepo, _eventAggregator);

        IRepository<CountryGraphQLModel> countryRepo = Substitute.For<IRepository<CountryGraphQLModel>>();
        _countryCache = new CountryCache(countryRepo, _eventAggregator);

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        // PermissionCache requiere sub-caches y un IGraphQLClient. No se dispara
        // carga en este set de tests (no llamamos OnViewReady), así que basta
        // con instanciarlo con dependencias mock.
        IRepository<Models.Global.PermissionDefinitionGraphQLModel> permDefRepo = Substitute.For<IRepository<Models.Global.PermissionDefinitionGraphQLModel>>();
        PermissionDefinitionCache permissionDefinitionCache = new(permDefRepo, _eventAggregator);

        IRepository<Models.Global.CompanyPermissionDefaultGraphQLModel> companyPermRepo = Substitute.For<IRepository<Models.Global.CompanyPermissionDefaultGraphQLModel>>();
        CompanyPermissionDefaultCache companyPermissionDefaultCache = new(companyPermRepo, _eventAggregator);

        IRepository<Models.Global.UserPermissionGraphQLModel> userPermRepo = Substitute.For<IRepository<Models.Global.UserPermissionGraphQLModel>>();
        UserPermissionCache userPermissionCache = new(userPermRepo, _eventAggregator);

        IGraphQLClient graphQLClient = Substitute.For<IGraphQLClient>();
        _permissionCache = new PermissionCache(permissionDefinitionCache, companyPermissionDefaultCache, userPermissionCache, graphQLClient, _eventAggregator);

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;

        _validator = new AccountingEntityValidator();
        _searchDebounce = new DebouncedAction(delayMs: TestDebounceDelayMs);

        _vm = new AccountingEntityViewModel(
            _eventAggregator,
            _notificationService,
            _service,
            _dialogService,
            _identificationTypeCache,
            _countryCache,
            _stringLengthCache,
            _permissionCache,
            _joinableTaskFactory,
            _validator,
            _searchDebounce);
    }

    private void SetupServiceReturnsEmptyPage()
    {
        PageType<AccountingEntityGraphQLModel> emptyPage = new()
        {
            TotalEntries = 0,
            Entries = new ObservableCollection<AccountingEntityGraphQLModel>()
        };
        _service.GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(emptyPage);
    }

    #region Debounce en FilterSearch

    /// <summary>
    /// Ejecuta una carga directa (sin debounce) para pre-calentar los Lazy
    /// estáticos y otros caches del primer acceso. Sin este warm-up, el
    /// primer debounced-load en un test aislado puede tomar tiempo extra
    /// inicializando estructuras y acabar fuera del wait window del test.
    /// </summary>
    private async Task WarmUpAsync()
    {
        SetupServiceReturnsEmptyPage();
        await _vm.LoadAccountingEntitiesAsync();
        _service.ClearReceivedCalls();
    }

    [Fact]
    public async Task FilterSearch_SingleAssignment_DoesNotCallServiceImmediately()
    {
        await WarmUpAsync();

        _vm.FilterSearch = "test";

        // Esperar un poco pero menos que el delay del debounce
        await Task.Delay(BeforeDebounceWaitMs);

        await _service.DidNotReceive().GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterSearch_SingleAssignment_CallsServiceOnceAfterDebounce()
    {
        await WarmUpAsync();

        _vm.FilterSearch = "test";

        // Esperar más que el delay del debounce
        await Task.Delay(DebounceWaitMs);

        await _service.Received(1).GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterSearch_RapidSuccessiveChanges_CallsServiceOnlyOnce()
    {
        await WarmUpAsync();

        // Simular al usuario escribiendo 4 caracteres rápidamente, todos por
        // debajo del delay del debounce (TestDebounceDelayMs = 30ms). Cada set
        // cancela el delay anterior. Los Task.Delay(5) entre sets dejan que
        // las continuations async del DebouncedAction avancen — replica el
        // comportamiento real de WPF donde cada keystroke pasa por el
        // dispatcher y no se amontona en un hot-loop síncrono, sin exceder
        // el delay total del debounce (5 × 4 = 20ms < 30ms).
        _vm.FilterSearch = "tes";
        await Task.Delay(5);
        _vm.FilterSearch = "test";
        await Task.Delay(5);
        _vm.FilterSearch = "testi";
        await Task.Delay(5);
        _vm.FilterSearch = "testin";

        // Esperar más que el delay del debounce desde la última tecla
        await Task.Delay(DebounceWaitMs);

        await _service.Received(1).GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterSearch_ShorterThanMinimum_DoesNotCallService()
    {
        await WarmUpAsync();

        // Menos de 3 caracteres y no-vacío: no debe disparar búsqueda.
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

        // Primera búsqueda completa — se espera al debounce antes de la segunda.
        _vm.FilterSearch = "uno";
        await Task.Delay(DebounceWaitMs);

        // Segunda búsqueda — también debe dispararse
        _vm.FilterSearch = "dos";
        await Task.Delay(DebounceWaitMs);

        await _service.Received(2).GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FilterSearch_BoundaryTiming_MatchesConfiguredDelay()
    {
        // Prueba que el delay real está en el orden del delay configurado
        // (TestDebounceDelayMs = 30ms), no uno arbitrariamente corto. Esto
        // blinda contra una regresión tipo "throttle con delay despreciable".
        await WarmUpAsync();

        _vm.FilterSearch = "test";

        // Bien antes del delay configurado → NO debe haber llamada.
        await Task.Delay(TestDebounceDelayMs / 3);
        await _service.DidNotReceive().GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());

        // Bien después del delay configurado → SÍ debe haber llamada.
        await Task.Delay(DebounceWaitMs);
        await _service.Received(1).GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Constructor_NullSearchDebounce_Throws()
    {
        System.Action act = () => new AccountingEntityViewModel(
            _eventAggregator, _notificationService, _service, _dialogService,
            _identificationTypeCache, _countryCache, _stringLengthCache,
            _permissionCache, _joinableTaskFactory, _validator, null!);

        act.Should().Throw<System.ArgumentNullException>().WithParameterName("searchDebounce");
    }

    [Fact]
    public async Task FilterSearch_ResetsPageIndexToOne()
    {
        await WarmUpAsync();
        _vm.PageIndex = 5;

        _vm.FilterSearch = "test";

        // PageIndex se resetea síncronamente en el setter antes del debounce.
        _vm.PageIndex.Should().Be(1);

        // Limpieza: esperar a que el debounce se complete para no dejar tareas
        // colgadas que ensucien otros tests del runner.
        await Task.Delay(DebounceWaitMs);
    }

    #endregion
}
