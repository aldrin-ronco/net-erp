using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using Models.Inventory;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Helpers.Services;
using NetErp.Inventory.StockMovementsIn.DTO;
using NetErp.Inventory.StockMovementsIn.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Inventory.StockMovementsIn.ViewModels
{
    /// <summary>
    /// Conductor + Master del módulo Stock Movements In. Lista paginada de movimientos
    /// (entradas por concepto: kardex_flow=I), filtros, y disparo de modales
    /// Nuevo / Editar / Anular.
    /// </summary>
    public class StockMovementInViewModel : Screen,
        IHandle<StockMovementCreateMessage>,
        IHandle<StockMovementUpdateMessage>,
        IHandle<StockMovementDeleteMessage>,
        IHandle<StockMovementPostMessage>,
        IHandle<StockMovementCancelMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly INotificationService _notificationService;
        private readonly IDialogService _dialogService;
        private readonly IRepository<StockMovementGraphQLModel> _service;
        private readonly IRepository<StockMovementLineGraphQLModel> _lineService;
        private readonly IRepository<AccountingSourceGraphQLModel> _accountingSourceService;
        private readonly IRepository<StorageGraphQLModel> _storageService;
        private readonly IRepository<ItemGraphQLModel> _itemService;
        private readonly CostCenterCache _costCenterCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly DebouncedAction _searchDebounce;
        private readonly IBackgroundQueueService _backgroundQueueService;

        #endregion

        public StockMovementInViewModel(
            IEventAggregator eventAggregator,
            INotificationService notificationService,
            IDialogService dialogService,
            IRepository<StockMovementGraphQLModel> service,
            IRepository<StockMovementLineGraphQLModel> lineService,
            IRepository<AccountingSourceGraphQLModel> accountingSourceService,
            IRepository<StorageGraphQLModel> storageService,
            IRepository<ItemGraphQLModel> itemService,
            CostCenterCache costCenterCache,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            DebouncedAction searchDebounce,
            IBackgroundQueueService backgroundQueueService)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _lineService = lineService ?? throw new ArgumentNullException(nameof(lineService));
            _accountingSourceService = accountingSourceService ?? throw new ArgumentNullException(nameof(accountingSourceService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
            _costCenterCache = costCenterCache ?? throw new ArgumentNullException(nameof(costCenterCache));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _joinableTaskFactory = joinableTaskFactory ?? throw new ArgumentNullException(nameof(joinableTaskFactory));
            _searchDebounce = searchDebounce ?? throw new ArgumentNullException(nameof(searchDebounce));
            _backgroundQueueService = backgroundQueueService ?? throw new ArgumentNullException(nameof(backgroundQueueService));

            _eventAggregator.SubscribeOnUIThread(this);
            DisplayName = "Entradas de inventario por concepto";
        }

        #region Grid Properties

        public bool IsBusy { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(IsBusy)); } } }

        public ObservableCollection<StockMovementGraphQLModel> StockMovements
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(StockMovements));
                }
            }
        } = [];

        public StockMovementGraphQLModel? SelectedStockMovement
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedStockMovement));
                    NotifyOfPropertyChange(nameof(CanEdit));
                    NotifyOfPropertyChange(nameof(CanPost));
                    NotifyOfPropertyChange(nameof(CanCancel));
                    NotifyOfPropertyChange(nameof(CanDelete));
                }
            }
        }

        public ObservableCollection<AccountingSourceGraphQLModel> AccountingSources { get; } = [];
        public ObservableCollection<CostCenterGraphQLModel> CostCenters { get; } = [];

        public AccountingSourceGraphQLModel? FilterAccountingSource
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FilterAccountingSource));
                    PageIndex = 1;
                    _ = LoadAsync();
                }
            }
        }

        public CostCenterGraphQLModel? FilterCostCenter
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FilterCostCenter));
                    PageIndex = 1;
                    _ = LoadAsync();
                }
            }
        }

        public string FilterDocumentNumber
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FilterDocumentNumber));
                    if (string.IsNullOrEmpty(value) || value.Length >= 2)
                    {
                        PageIndex = 1;
                        _ = _searchDebounce.RunAsync(LoadAsync);
                    }
                }
            }
        } = string.Empty;

        public ObservableCollection<PeriodItem> PeriodOptions { get; } =
        [
            new() { Value = PeriodOption.Today, Label = "Hoy" },
            new() { Value = PeriodOption.Yesterday, Label = "Ayer" },
            new() { Value = PeriodOption.ThisWeek, Label = "Esta semana" },
            new() { Value = PeriodOption.Last7Days, Label = "Últimos 7 días" },
            new() { Value = PeriodOption.LastWeek, Label = "La semana pasada" },
            new() { Value = PeriodOption.Last14Days, Label = "Últimos 14 días" },
            new() { Value = PeriodOption.ThisMonth, Label = "Este mes" },
            new() { Value = PeriodOption.Last30Days, Label = "Últimos 30 días" },
            new() { Value = PeriodOption.LastMonth, Label = "El mes pasado" },
            new() { Value = PeriodOption.Custom, Label = "Rango personalizado" },
        ];

        private bool _suppressPeriodReload = true;

        public PeriodItem? SelectedPeriod
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedPeriod));
                    NotifyOfPropertyChange(nameof(IsCustomPeriod));
                    if (!_suppressPeriodReload)
                    {
                        PageIndex = 1;
                        _ = LoadAsync();
                    }
                }
            }
        }

        public DateTime? CustomDateFrom
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CustomDateFrom));
                    if (IsCustomPeriod && !_suppressPeriodReload)
                    {
                        PageIndex = 1;
                        _ = LoadAsync();
                    }
                }
            }
        }

        public DateTime? CustomDateTo
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CustomDateTo));
                    if (IsCustomPeriod && !_suppressPeriodReload)
                    {
                        PageIndex = 1;
                        _ = LoadAsync();
                    }
                }
            }
        }

        public bool IsCustomPeriod => SelectedPeriod?.Value == PeriodOption.Custom;

        public string[] StatusOptions { get; } = ["DRAFT", "POSTED", "ALL"];

        /// <summary>Estado mostrado: DRAFT (default), POSTED, ALL.</summary>
        public string FilterStatus
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FilterStatus));
                    PageIndex = 1;
                    _ = LoadAsync();
                }
            }
        } = "DRAFT";

        public int PageIndex { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(PageIndex)); } } } = 1;
        public int PageSize { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(PageSize)); } } } = 50;
        public int TotalCount { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(TotalCount)); } } }
        public string ResponseTime { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(ResponseTime)); } } } = string.Empty;

        /// <summary>Detail activo (no modal). null = master visible.</summary>
        public StockMovementInDetailViewModel? CurrentDetail
        {
            get;
            private set { if (field != value) { field = value; NotifyOfPropertyChange(); NotifyOfPropertyChange(nameof(IsMasterVisible)); NotifyOfPropertyChange(nameof(IsDetailVisible)); } }
        }

        public bool IsMasterVisible => CurrentDetail == null;
        public bool IsDetailVisible => CurrentDetail != null;

        public bool CanEdit => SelectedStockMovement != null && SelectedStockMovement.Status == "DRAFT";
        public bool CanPost => SelectedStockMovement != null && SelectedStockMovement.Status == "DRAFT";
        public bool CanCancel => SelectedStockMovement != null && SelectedStockMovement.Status == "POSTED" && string.IsNullOrEmpty(SelectedStockMovement.CancelledWith);
        public bool CanDelete => SelectedStockMovement != null && SelectedStockMovement.Status == "DRAFT";

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                await Task.WhenAll(
                    _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.StockMovement),
                    _costCenterCache.EnsureLoadedAsync(),
                    LoadAccountingSourcesAsync());
                CostCenters.Clear();
                foreach (CostCenterGraphQLModel cc in _costCenterCache.Items) CostCenters.Add(cc);
                SelectedPeriod = PeriodOptions.First(p => p.Value == PeriodOption.ThisMonth);
                _suppressPeriodReload = false;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al inicializar el módulo.\r\n{GetType().Name}.{nameof(OnViewReady)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
                await TryCloseAsync();
            }
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                _eventAggregator.Unsubscribe(this);
                StockMovements.Clear();
                AccountingSources.Clear();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region Loading

        private async Task LoadAccountingSourcesAsync()
        {
            var (fragment, query) = StockMovementInQueries.InboundAccountingSources.Value;
            dynamic filters = new ExpandoObject();
            filters.kardexFlow = "I";
            filters.annulment = false;
            object variables = new GraphQLVariables()
                .For(fragment, "pagination", new { Page = 1, PageSize = -1 })
                .For(fragment, "filters", filters)
                .Build();
            PageType<AccountingSourceGraphQLModel> page = await _accountingSourceService.GetPageAsync(query, variables);
            AccountingSources.Clear();
            foreach (AccountingSourceGraphQLModel s in page.Entries.OrderBy(a => a.Code)) AccountingSources.Add(s);
        }

        public async Task LoadAsync()
        {
            try
            {
                IsBusy = true;
                Stopwatch stopwatch = Stopwatch.StartNew();
                var (fragment, query) = StockMovementInQueries.StockMovementsPage.Value;

                dynamic filters = new ExpandoObject();
                if (!string.IsNullOrWhiteSpace(FilterDocumentNumber))
                    filters.documentNumber = FilterDocumentNumber.Trim();
                if (FilterAccountingSource != null && FilterAccountingSource.Id > 0)
                    filters.accountingSourceId = FilterAccountingSource.Id;
                if (FilterCostCenter != null && FilterCostCenter.Id > 0)
                    filters.costCenterId = FilterCostCenter.Id;
                if (FilterStatus is "DRAFT" or "POSTED")
                    filters.status = FilterStatus;
                filters.onlyVigentes = true;

                (DateTime? from, DateTime? to) = ResolveDateRange();
                if (from.HasValue) filters.insertedAtFrom = from.Value.ToIsoDatetime();
                if (to.HasValue) filters.insertedAtTo = to.Value.ToIsoDatetime();

                object variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<StockMovementGraphQLModel> result = await _service.GetPageAsync(query, variables);

                StockMovements = new ObservableCollection<StockMovementGraphQLModel>(result.Entries);
                TotalCount = result.TotalEntries;
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(LoadAsync)} \r\n{ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsBusy = false; }
        }

        private (DateTime? From, DateTime? To) ResolveDateRange()
        {
            if (SelectedPeriod == null) return (null, null);

            DateTime today = DateTime.Today;
            DateTime endOfToday = today.AddDays(1).AddTicks(-1);

            return SelectedPeriod.Value switch
            {
                PeriodOption.Today => (today, endOfToday),
                PeriodOption.Yesterday => (today.AddDays(-1), today.AddTicks(-1)),
                PeriodOption.ThisWeek => (StartOfWeek(today), endOfToday),
                PeriodOption.Last7Days => (today.AddDays(-6), endOfToday),
                PeriodOption.LastWeek => (StartOfWeek(today).AddDays(-7), StartOfWeek(today).AddTicks(-1)),
                PeriodOption.Last14Days => (today.AddDays(-13), endOfToday),
                PeriodOption.ThisMonth => (new DateTime(today.Year, today.Month, 1), endOfToday),
                PeriodOption.Last30Days => (today.AddDays(-29), endOfToday),
                PeriodOption.LastMonth => GetLastMonthRange(today),
                PeriodOption.Custom => (
                    CustomDateFrom?.Date,
                    CustomDateTo.HasValue ? CustomDateTo.Value.Date.AddDays(1).AddTicks(-1) : (DateTime?)null),
                _ => (null, null)
            };
        }

        private static DateTime StartOfWeek(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }

        private static (DateTime From, DateTime To) GetLastMonthRange(DateTime today)
        {
            DateTime firstOfThisMonth = new(today.Year, today.Month, 1);
            DateTime firstOfLastMonth = firstOfThisMonth.AddMonths(-1);
            DateTime lastOfLastMonth = firstOfThisMonth.AddTicks(-1);
            return (firstOfLastMonth, lastOfLastMonth);
        }

        #endregion

        #region Actions

        public async Task NewAsync()
        {
            try
            {
                IsBusy = true;
                StockMovementInNewDialogViewModel modal = new(
                    _service, _accountingSourceService, _storageService,
                    _costCenterCache, AccountingSources, _eventAggregator);

                if (this.GetView() is FrameworkElement parentView)
                {
                    modal.DialogWidth = parentView.ActualWidth * 0.45;
                }

                IsBusy = false;
                bool? result = await _dialogService.ShowDialogAsync(modal, "Nueva entrada de inventario");

                if (result == true && modal.CreatedId > 0)
                {
                    // Modal Nuevo guardó draft; ahora abre Detail con ese id
                    await OpenDetailAsync(modal.CreatedId);
                }
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(NewAsync)} \r\n{ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsBusy = false; }
        }

        public async Task EditAsync()
        {
            if (SelectedStockMovement == null || SelectedStockMovement.Status != "DRAFT") return;
            await OpenDetailAsync(SelectedStockMovement.Id);
        }

        private async Task OpenDetailAsync(int id)
        {
            try
            {
                IsBusy = true;
                StockMovementInDetailViewModel detail = new(
                    _eventAggregator, _notificationService, _dialogService,
                    _service, _lineService, _itemService, _stringLengthCache,
                    _joinableTaskFactory, _backgroundQueueService);

                await detail.LoadDataForEditAsync(id);
                detail.RequestClose += OnDetailRequestClose;
                CurrentDetail = detail;
                IsBusy = false;
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(OpenDetailAsync)} \r\n{ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsBusy = false; }
        }

        private async void OnDetailRequestClose(object? sender, EventArgs e)
        {
            if (CurrentDetail != null) CurrentDetail.RequestClose -= OnDetailRequestClose;
            CurrentDetail = null;
            await LoadAsync();
        }

        public async Task PostAsync()
        {
            if (SelectedStockMovement == null || SelectedStockMovement.Status != "DRAFT") return;
            if (ThemedMessageBox.Show("Confirmar postear",
                "¿Confirma postear este movimiento? Esta acción es irreversible (solo se podrá anular).",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                IsBusy = true;
                var (fragment, query) = StockMovementInQueries.PostMovement.Value;
                object variables = new GraphQLVariables().For(fragment, "id", SelectedStockMovement.Id).Build();
                StockMovementPostResponse? payload = await _service.MutationContextAsync<StockMovementPostResponse>(query, variables);
                StockMovementMutationPayload? result = payload?.UpdateResponse;
                if (result == null || !result.Success)
                {
                    _notificationService.ShowError(
                        StockMovementErrorFormatter.Format(result?.Message, result?.Errors, "No se pudo postear el movimiento."),
                        durationMs: 8000);
                    return;
                }
                _notificationService.ShowSuccess(result.Message);
                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new StockMovementPostMessage { PostedStockMovement = result }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(PostAsync)} \r\n{ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsBusy = false; }
        }

        public async Task CancelAsync()
        {
            if (SelectedStockMovement == null || SelectedStockMovement.Status != "POSTED" ||
                !string.IsNullOrEmpty(SelectedStockMovement.CancelledWith)) return;

            try
            {
                IsBusy = true;
                StockMovementInCancelDialogViewModel dlg = new(_service, SelectedStockMovement.Id, SelectedStockMovement.DocumentNumber ?? string.Empty);
                IsBusy = false;
                bool? ok = await _dialogService.ShowDialogAsync(dlg, "Anular movimiento");
                if (ok == true && dlg.Result?.Success == true)
                {
                    _notificationService.ShowSuccess(dlg.Result.Message);
                    await _eventAggregator.PublishOnCurrentThreadAsync(
                        new StockMovementCancelMessage { CancelledStockMovement = dlg.Result }, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(CancelAsync)} \r\n{ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsBusy = false; }
        }

        public async Task DeleteDraftAsync()
        {
            if (SelectedStockMovement == null || SelectedStockMovement.Status != "DRAFT") return;
            if (ThemedMessageBox.Show("Confirmar eliminación",
                $"¿Eliminar definitivamente el borrador {SelectedStockMovement.DocumentDisplay}? Esta acción no se puede deshacer.",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                IsBusy = true;
                var (fragment, query) = StockMovementInQueries.DeleteDraft.Value;
                object variables = new GraphQLVariables().For(fragment, "id", SelectedStockMovement.Id).Build();
                StockMovementDeleteResponse? payload = await _service.MutationContextAsync<StockMovementDeleteResponse>(query, variables);
                StockMovementMutationPayload? result = payload?.DeleteResponse;
                if (result == null || !result.Success)
                {
                    _notificationService.ShowError(
                        StockMovementErrorFormatter.Format(result?.Message, result?.Errors, "No se pudo eliminar el draft."),
                        durationMs: 8000);
                    return;
                }
                _notificationService.ShowSuccess(result.Message);
                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new StockMovementDeleteMessage { DeletedStockMovement = result }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(DeleteDraftAsync)} \r\n{ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsBusy = false; }
        }

        #endregion

        #region IHandle

        public Task HandleAsync(StockMovementCreateMessage message, CancellationToken cancellationToken) => LoadAsync();
        public Task HandleAsync(StockMovementUpdateMessage message, CancellationToken cancellationToken) => LoadAsync();
        public Task HandleAsync(StockMovementDeleteMessage message, CancellationToken cancellationToken) => LoadAsync();
        public Task HandleAsync(StockMovementPostMessage message, CancellationToken cancellationToken) => LoadAsync();
        public Task HandleAsync(StockMovementCancelMessage message, CancellationToken cancellationToken) => LoadAsync();

        #endregion
    }

    /// <summary>Wrapper response para post (alias updateResponse).</summary>
    internal class StockMovementPostResponse
    {
        public StockMovementMutationPayload UpdateResponse { get; set; } = new();
    }

    /// <summary>Wrapper response para delete (alias deleteResponse).</summary>
    internal class StockMovementDeleteResponse
    {
        public StockMovementMutationPayload DeleteResponse { get; set; } = new();
    }
}
