using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using Common.Validators;
using FluentAssertions;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using NetErp.Billing.CreditLimit.DTO;
using NetErp.Billing.CreditLimit.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.Messages;
using NetErp.Helpers.Services;
using NSubstitute;
using NSubstitute.Core;
using Xunit;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.ViewModels;

/// <summary>
/// Tests del master de CreditLimit: debounce, carga paginada, validación inline + autosave por fila,
/// handler de <see cref="OperationCompletedMessage"/>, y ciclo de vida.
/// </summary>
[Collection("Debounce tests")]
public class CreditLimitMasterViewModelTests
{
    private const int TestDebounceDelayMs = 30;
    private const int BeforeDebounceWaitMs = 10;
    private const int DebounceWaitMs = 100;

    private readonly IMapper _mapper;
    private readonly IEventAggregator _eventAggregator;
    private readonly INotificationService _notificationService;
    private readonly ICreditLimitValidator _validator;
    private readonly IRepository<CreditLimitGraphQLModel> _service;
    private readonly IBackgroundQueueService _backgroundQueueService;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly DebouncedAction _searchDebounce;
    private readonly CreditLimitViewModel _conductor;
    private readonly CreditLimitMasterViewModel _vm;

    public CreditLimitMasterViewModelTests()
    {
        _mapper = Substitute.For<IMapper>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _notificationService = Substitute.For<INotificationService>();
        _validator = Substitute.For<ICreditLimitValidator>();
        _service = Substitute.For<IRepository<CreditLimitGraphQLModel>>();
        _backgroundQueueService = Substitute.For<IBackgroundQueueService>();

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;

        _searchDebounce = new DebouncedAction(delayMs: TestDebounceDelayMs);

        // Validator por defecto: siempre válido (los tests específicos sobreescriben).
        _validator.ValidateLimit(Arg.Any<decimal>(), Arg.Any<decimal>(), Arg.Any<decimal>())
            .Returns(ValidationResult.Success());

        // Mapper: proyecta el GraphQLModel al DTO preservando los campos relevantes.
        _mapper.Map<CreditLimitDTO>(Arg.Any<CreditLimitGraphQLModel>())
            .Returns(ci =>
            {
                CreditLimitGraphQLModel src = ci.Arg<CreditLimitGraphQLModel>();
                return new CreditLimitDTO
                {
                    Id = src.Id,
                    Customer = src.Customer,
                    CreditLimit = src.CreditLimit,
                    Used = src.Used,
                    OriginalLimit = src.OriginalLimit
                };
            });

        _conductor = new CreditLimitViewModel(
            _mapper,
            _eventAggregator,
            _notificationService,
            _validator,
            _service,
            _backgroundQueueService,
            _joinableTaskFactory,
            _searchDebounce);

        _vm = _conductor.CreditLimitMasterViewModel;
    }

    #region Helpers

    private void SetupServiceReturnsEmptyPage()
    {
        PageType<CreditLimitGraphQLModel> emptyPage = new()
        {
            TotalEntries = 0,
            Entries = new ObservableCollection<CreditLimitGraphQLModel>()
        };
        _service.GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(emptyPage);
    }

    private void SetupServiceReturnsEntries(params CreditLimitGraphQLModel[] entries)
    {
        PageType<CreditLimitGraphQLModel> page = new()
        {
            TotalEntries = entries.Length,
            Entries = new ObservableCollection<CreditLimitGraphQLModel>(entries)
        };
        _service.GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(page);
    }

    private static CreditLimitGraphQLModel BuildEntry(int customerId, decimal limit, decimal used, decimal original)
    {
        return new CreditLimitGraphQLModel
        {
            Id = customerId,
            CreditLimit = limit,
            Used = used,
            OriginalLimit = original,
            Customer = new CustomerGraphQLModel { Id = customerId }
        };
    }

    /// <summary>
    /// Ejerce una carga directa antes de los tests de debounce para pre-calentar
    /// Lazy estáticos y JIT.
    /// </summary>
    private async Task WarmUpAsync()
    {
        SetupServiceReturnsEmptyPage();
        await _vm.LoadCreditLimitsAsync();
        _service.ClearReceivedCalls();
    }

    #endregion

    #region Constructor null guards

    [Fact]
    public void Constructor_NullContext_Throws()
    {
        System.Action act = () => new CreditLimitMasterViewModel(
            null!, _notificationService, _validator, _backgroundQueueService, _service, _joinableTaskFactory, _searchDebounce);

        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void Constructor_NullNotificationService_Throws()
    {
        System.Action act = () => new CreditLimitMasterViewModel(
            _conductor, null!, _validator, _backgroundQueueService, _service, _joinableTaskFactory, _searchDebounce);

        act.Should().Throw<ArgumentNullException>().WithParameterName("notificationService");
    }

    [Fact]
    public void Constructor_NullValidator_Throws()
    {
        System.Action act = () => new CreditLimitMasterViewModel(
            _conductor, _notificationService, null!, _backgroundQueueService, _service, _joinableTaskFactory, _searchDebounce);

        act.Should().Throw<ArgumentNullException>().WithParameterName("validator");
    }

    [Fact]
    public void Constructor_NullBackgroundQueue_Throws()
    {
        System.Action act = () => new CreditLimitMasterViewModel(
            _conductor, _notificationService, _validator, null!, _service, _joinableTaskFactory, _searchDebounce);

        act.Should().Throw<ArgumentNullException>().WithParameterName("backgroundQueueService");
    }

    [Fact]
    public void Constructor_NullService_Throws()
    {
        System.Action act = () => new CreditLimitMasterViewModel(
            _conductor, _notificationService, _validator, _backgroundQueueService, null!, _joinableTaskFactory, _searchDebounce);

        act.Should().Throw<ArgumentNullException>().WithParameterName("creditLimitService");
    }

    [Fact]
    public void Constructor_NullSearchDebounce_Throws()
    {
        System.Action act = () => new CreditLimitMasterViewModel(
            _conductor, _notificationService, _validator, _backgroundQueueService, _service, _joinableTaskFactory, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("searchDebounce");
    }

    #endregion

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
    public async Task FilterSearch_ResetsPageIndexToOne()
    {
        await WarmUpAsync();
        _vm.PageIndex = 7;

        _vm.FilterSearch = "abc";

        _vm.PageIndex.Should().Be(1);
    }

    #endregion

    #region Load

    [Fact]
    public async Task LoadCreditLimitsAsync_PopulatesCollectionAndTotalCount()
    {
        SetupServiceReturnsEntries(
            BuildEntry(1, 100_000m, 0m, 100_000m),
            BuildEntry(2, 500_000m, 100_000m, 500_000m));

        await _vm.LoadCreditLimitsAsync();

        _vm.CreditLimits.Should().HaveCount(2);
        _vm.TotalCount.Should().Be(2);
        _vm.ResponseTime.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoadCreditLimitsAsync_TogglesIsBusy()
    {
        bool busyDuringLoad = false;
        _service.GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                busyDuringLoad = _vm.IsBusy;
                return new PageType<CreditLimitGraphQLModel> { TotalEntries = 0, Entries = [] };
            });

        await _vm.LoadCreditLimitsAsync();

        busyDuringLoad.Should().BeTrue();
        _vm.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task OnlyCustomersWithCreditLimit_Toggle_TriggersLoad()
    {
        await WarmUpAsync();

        _vm.OnlyCustomersWithCreditLimit = false;

        // Cambio discreto — sin debounce, carga inmediata. Pequeño yield para el _ = LoadAsync().
        await Task.Delay(BeforeDebounceWaitMs);

        await _service.Received(1).GetPageAsync(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region OnCreditLimitChanged — validación + autosave

    [Fact]
    public async Task OnCreditLimitChanged_InvalidLimit_RevertsAndShowsError()
    {
        _validator.ValidateLimit(Arg.Any<decimal>(), Arg.Any<decimal>(), Arg.Any<decimal>())
            .Returns(ValidationResult.Error("Límite inválido"));
        SetupServiceReturnsEntries(BuildEntry(1, 100_000m, 50_000m, 100_000m));
        await _vm.LoadCreditLimitsAsync();

        CreditLimitDTO dto = _vm.CreditLimits[0];
        dto.CreditLimit = 10_000m; // inválido → debe revertir

        dto.CreditLimit.Should().Be(100_000m);
        _notificationService.Received(1).ShowError("Límite inválido", "Error de Validación");
        await _backgroundQueueService.DidNotReceive().EnqueueOperationAsync(Arg.Any<IDataOperation>());
    }

    [Fact]
    public async Task OnCreditLimitChanged_ValidLimit_EnqueuesAndMarksPending()
    {
        SetupServiceReturnsEntries(BuildEntry(42, 100_000m, 0m, 100_000m));
        await _vm.LoadCreditLimitsAsync();

        CreditLimitDTO dto = _vm.CreditLimits[0];
        dto.CreditLimit = 150_000m;

        await Task.Delay(BeforeDebounceWaitMs); // deja correr EnqueueUpdateAsync

        dto.Status.Should().Be(OperationStatus.Pending);
        await _backgroundQueueService.Received(1).EnqueueOperationAsync(Arg.Any<IDataOperation>());
    }

    [Fact]
    public async Task OnCreditLimitChanged_Warning_StillEnqueuesAndNotifies()
    {
        _validator.ValidateLimit(Arg.Any<decimal>(), Arg.Any<decimal>(), Arg.Any<decimal>())
            .Returns(ValidationResult.Warning("Cambio superior al 50%"));
        SetupServiceReturnsEntries(BuildEntry(1, 100_000m, 0m, 100_000m));
        await _vm.LoadCreditLimitsAsync();

        CreditLimitDTO dto = _vm.CreditLimits[0];
        dto.CreditLimit = 200_000m;

        await Task.Delay(BeforeDebounceWaitMs);

        _notificationService.Received(1).ShowWarning("Cambio superior al 50%", "Advertencia");
        await _backgroundQueueService.Received(1).EnqueueOperationAsync(Arg.Any<IDataOperation>());
        dto.CreditLimit.Should().Be(200_000m);
    }

    [Fact]
    public async Task OnCreditLimitChanged_BackgroundQueueFails_MarksFailedAndNotifies()
    {
        _backgroundQueueService.EnqueueOperationAsync(Arg.Any<IDataOperation>())
            .Returns<Task<Guid>>(_ => throw new InvalidOperationException("servicio caído"));
        _backgroundQueueService.GetCriticalErrorMessage().Returns("El servicio no está disponible");
        SetupServiceReturnsEntries(BuildEntry(1, 100_000m, 0m, 100_000m));
        await _vm.LoadCreditLimitsAsync();

        CreditLimitDTO dto = _vm.CreditLimits[0];
        dto.CreditLimit = 150_000m;

        await Task.Delay(BeforeDebounceWaitMs);

        dto.Status.Should().Be(OperationStatus.Failed);
        _notificationService.Received(1).ShowError("El servicio no está disponible");
    }

    #endregion

    #region HandleAsync(OperationCompletedMessage)

    [Fact]
    public async Task HandleAsync_Success_SetsSavedAndUpdatesOriginalLimit()
    {
        SetupServiceReturnsEntries(BuildEntry(1, 100_000m, 0m, 100_000m));
        await _vm.LoadCreditLimitsAsync();

        CreditLimitDTO dto = _vm.CreditLimits[0];
        dto.CreditLimit = 200_000m; // dispara enqueue
        await Task.Delay(BeforeDebounceWaitMs);

        Guid opId = CaptureLastOperationId();

        await _vm.HandleAsync(new OperationCompletedMessage(opId, success: true, "Customer #1"), CancellationToken.None);

        dto.Status.Should().Be(OperationStatus.Saved);
        dto.OriginalLimit.Should().Be(200_000m);
    }

    [Fact]
    public async Task HandleAsync_Retrying_SetsRetryingAndTooltip()
    {
        SetupServiceReturnsEntries(BuildEntry(1, 100_000m, 0m, 100_000m));
        await _vm.LoadCreditLimitsAsync();

        CreditLimitDTO dto = _vm.CreditLimits[0];
        dto.CreditLimit = 150_000m;
        await Task.Delay(BeforeDebounceWaitMs);

        Guid opId = CaptureLastOperationId();

        await _vm.HandleAsync(
            new OperationCompletedMessage(opId, success: false, "Customer #1", exception: null, isRetrying: true, errorDetail: "Reintentando..."),
            CancellationToken.None);

        dto.Status.Should().Be(OperationStatus.Retrying);
        dto.StatusTooltip.Should().Be("Reintentando...");
    }

    [Fact]
    public async Task HandleAsync_Failed_SetsFailedTooltipAndNotifies()
    {
        SetupServiceReturnsEntries(BuildEntry(1, 100_000m, 0m, 100_000m));
        await _vm.LoadCreditLimitsAsync();

        CreditLimitDTO dto = _vm.CreditLimits[0];
        dto.CreditLimit = 150_000m;
        await Task.Delay(BeforeDebounceWaitMs);

        Guid opId = CaptureLastOperationId();

        await _vm.HandleAsync(
            new OperationCompletedMessage(opId, success: false, "Customer #1", exception: new Exception("boom"), isRetrying: false, errorDetail: "API rota"),
            CancellationToken.None);

        dto.Status.Should().Be(OperationStatus.Failed);
        dto.StatusTooltip.Should().Be("API rota");
        _notificationService.Received(1).ShowError(Arg.Is<string>(s => s.Contains("API rota")), durationMs: 6000);
    }

    [Fact]
    public async Task HandleAsync_UnknownOperationId_DoesNothing()
    {
        SetupServiceReturnsEntries(BuildEntry(1, 100_000m, 0m, 100_000m));
        await _vm.LoadCreditLimitsAsync();

        CreditLimitDTO dto = _vm.CreditLimits[0];
        OperationStatus initialStatus = dto.Status;

        await _vm.HandleAsync(new OperationCompletedMessage(Guid.NewGuid(), success: true, "no-existe"), CancellationToken.None);

        dto.Status.Should().Be(initialStatus);
    }

    #endregion

    #region OnDeactivateAsync

    [Fact]
    public async Task OnDeactivateAsync_Close_ClearsCollectionsAndUnsubscribes()
    {
        SetupServiceReturnsEntries(
            BuildEntry(1, 100_000m, 0m, 100_000m),
            BuildEntry(2, 200_000m, 0m, 200_000m));
        await ((IActivate)_vm).ActivateAsync(CancellationToken.None);

        await ((IDeactivate)_vm).DeactivateAsync(close: true, CancellationToken.None);

        _vm.CreditLimits.Should().BeEmpty();
        _eventAggregator.Received(1).Unsubscribe(_vm);
    }

    [Fact]
    public async Task OnDeactivateAsync_NoClose_PreservesState()
    {
        SetupServiceReturnsEntries(BuildEntry(1, 100_000m, 0m, 100_000m));
        await ((IActivate)_vm).ActivateAsync(CancellationToken.None);

        await ((IDeactivate)_vm).DeactivateAsync(close: false, CancellationToken.None);

        _vm.CreditLimits.Should().HaveCount(1);
        _eventAggregator.DidNotReceive().Unsubscribe(_vm);
    }

    #endregion

    #region Helpers auxiliares

    /// <summary>
    /// Captura el OperationId asignado por el master al último EnqueueOperationAsync recibido.
    /// </summary>
    private Guid CaptureLastOperationId()
    {
        foreach (ICall call in _backgroundQueueService.ReceivedCalls())
        {
            if (call.GetMethodInfo().Name != nameof(IBackgroundQueueService.EnqueueOperationAsync)) continue;
            if (call.GetArguments() is { Length: > 0 } args && args[0] is IDataOperation op)
            {
                return op.OperationId;
            }
        }
        throw new InvalidOperationException("No se encontró ninguna operación encolada.");
    }

    #endregion
}
