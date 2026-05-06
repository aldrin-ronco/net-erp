using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Inventory;
using NetErp.Global.Modals.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Helpers.Messages;
using NetErp.Helpers.Services;
using NetErp.Helpers.Shortcuts;
using NetErp.Inventory.StockMovementsOut.DTO;
using NetErp.Inventory.StockMovementsOut.Helpers;
using NetErp.UserControls.ItemDimensionEditor.DTO;
using NetErp.UserControls.ItemDimensionEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Inventory.StockMovementsOut.ViewModels
{
    /// <summary>
    /// Detail modal del Stock Movement de salida (status DRAFT).
    /// Header readonly excepto note. Grid de líneas + UC <c>ItemDimensionEditor</c>
    /// para captura. Persistencia incremental por línea.
    /// </summary>
    public class StockMovementOutDetailViewModel : Screen, IHandle<OperationCompletedMessage>
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly INotificationService _notificationService;
        private readonly IDialogService _dialogService;
        private readonly IRepository<StockMovementGraphQLModel> _service;
        private readonly IRepository<StockMovementLineGraphQLModel> _lineService;
        private readonly IRepository<ItemGraphQLModel> _itemService;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly IBackgroundQueueService _backgroundQueueService;
        private readonly IItemImageProvider _imageProvider;
        private readonly Dictionary<Guid, int> _operationLineMapping = [];

        // Cache de stock disponible y costo por item, poblado en la misma búsqueda
        // que identifica al producto (stockTotalsPage tracking=BASE). Evita una llamada
        // adicional al servidor para obtener costo y disponibilidad.
        private readonly Dictionary<int, (decimal Available, decimal Cost)> _baseStockByItemId = [];

        // TODO: enlazar con permiso de usuario (PermissionCodes.StockMovementOut.ViewCost)
        // cuando esté disponible. Por ahora todos pueden ver el costo.
        public bool CanViewCost { get; } = true;

        public StockMovementOutDetailViewModel(
            IEventAggregator eventAggregator,
            INotificationService notificationService,
            IDialogService dialogService,
            IRepository<StockMovementGraphQLModel> service,
            IRepository<StockMovementLineGraphQLModel> lineService,
            IRepository<ItemGraphQLModel> itemService,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            IBackgroundQueueService backgroundQueueService,
            IItemImageProvider imageProvider)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _lineService = lineService ?? throw new ArgumentNullException(nameof(lineService));
            _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _joinableTaskFactory = joinableTaskFactory ?? throw new ArgumentNullException(nameof(joinableTaskFactory));
            _backgroundQueueService = backgroundQueueService ?? throw new ArgumentNullException(nameof(backgroundQueueService));
            _imageProvider = imageProvider ?? throw new ArgumentNullException(nameof(imageProvider));
            _eventAggregator.SubscribeOnUIThread(this);
            Lines.CollectionChanged += (_, __) =>
            {
                NotifyOfPropertyChange(nameof(CanPost));
                NotifyOfPropertyChange(nameof(CanRemoveLine));
            };
        }

        public int NoteMaxLength => _stringLengthCache.GetMaxLength<StockMovementGraphQLModel>(nameof(StockMovementGraphQLModel.Note));

        #region Estado del documento

        private StockMovementGraphQLModel _model = new();

        public int Id { get => _model.Id; }
        public string? DocumentNumber { get => _model.DocumentNumber; }
        public string Status { get => _model.Status; }
        public string AccountingSourceName { get => _model.AccountingSource?.Name ?? string.Empty; }
        public string AccountingSourceCode { get => _model.AccountingSource?.Code ?? string.Empty; }
        public string DocumentDisplay => _model.DocumentDisplay;
        public string CostCenterName { get => _model.CostCenter?.Name ?? string.Empty; }
        public string StorageName { get => _model.Storage?.Name ?? string.Empty; }
        public IEnumerable<int> HighlightedStorageIds
            => _model.Storage?.Id is int id && id > 0 ? [id] : [];
        public DateTime InsertedAt { get => _model.InsertedAt; }
        public string DocumentDate => _model.InsertedAt.ToString("dd/MM/yyyy");
        public string DocumentTime => _model.InsertedAt.ToString("HH:mm");

        public string Note
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value ?? string.Empty;
                    NotifyOfPropertyChange(nameof(Note));
                    NotifyOfPropertyChange(nameof(NoteHasChanges));
                    if (IsDraft && NoteHasChanges) TriggerNoteDebounce();
                }
            }
        } = string.Empty;

        private CancellationTokenSource? _noteDebounceCts;
        private const int NoteDebounceMs = 800;

        private void TriggerNoteDebounce()
        {
            _noteDebounceCts?.Cancel();
            _noteDebounceCts?.Dispose();
            _noteDebounceCts = new CancellationTokenSource();
            CancellationToken token = _noteDebounceCts.Token;
            NoteStatus = OperationStatus.Pending;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(NoteDebounceMs, token);
                    if (token.IsCancellationRequested) return;
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    await AutoSaveNoteAsync();
                }
                catch (TaskCanceledException) { /* superseded */ }
            });
        }

        private async Task FlushNoteAsync()
        {
            if (_noteDebounceCts is not null) await _noteDebounceCts!.CancelAsync();
            _noteDebounceCts?.Dispose();
            _noteDebounceCts = null;
            if (IsDraft && NoteHasChanges) await AutoSaveNoteAsync();
        }

        private string SeedNote { get; set; } = string.Empty;
        public bool NoteHasChanges => !string.Equals(SeedNote, Note, StringComparison.Ordinal);

        public OperationStatus NoteStatus
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(NoteStatusBrush));
                if (value == OperationStatus.Saved) _ = ResetNoteStatusAsync();
            }
        } = OperationStatus.Unchanged;

        public Brush NoteStatusBrush => NoteStatus switch
        {
            OperationStatus.Pending => Brushes.Orange,
            OperationStatus.Saved => Brushes.Green,
            OperationStatus.Failed => Brushes.Red,
            _ => Brushes.Transparent
        };

        private async Task ResetNoteStatusAsync()
        {
            await Task.Delay(2500);
            if (NoteStatus == OperationStatus.Saved) NoteStatus = OperationStatus.Unchanged;
        }

        public async Task AutoSaveNoteAsync()
        {
            if (!IsDraft || !NoteHasChanges) return;
            try
            {
                NoteStatus = OperationStatus.Pending;
                var (fragment, query) = StockMovementOutQueries.UpdateDraft.Value;
                object variables = new GraphQLVariables()
                    .For(fragment, "id", _model.Id)
                    .For(fragment, "data", new { note = Note?.Trim() ?? string.Empty })
                    .Build();
                UpdateDraftResponse? responseObj = await _service.MutationContextAsync<UpdateDraftResponse>(query, variables);
                StockMovementMutationPayload? payload = responseObj?.UpdateResponse;
                if (payload == null || !payload.Success)
                {
                    NoteStatus = OperationStatus.Failed;
                    _notificationService.ShowError(
                        StockMovementErrorFormatter.Format(payload?.Message, payload?.Errors, "No se pudo actualizar la nota."),
                        durationMs: 8000);
                    return;
                }
                SeedNote = Note ?? string.Empty;
                NotifyOfPropertyChange(nameof(NoteHasChanges));
                NoteStatus = OperationStatus.Saved;
            }
            catch (Exception ex)
            {
                NoteStatus = OperationStatus.Failed;
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{nameof(AutoSaveNoteAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public ObservableCollection<StockMovementLineDTO> Lines { get; } = [];

        public StockMovementLineDTO? SelectedLine
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedLine));
                    NotifyOfPropertyChange(nameof(CanRemoveLine));
                }
            }
        }

        public bool IsDraft => Status == "DRAFT";
        public bool CanPost => IsDraft && Lines.Count > 0 && !IsBusy;
        public bool CanRemoveLine => IsDraft && SelectedLine != null && !IsBusy;
        public bool CanSaveNote => IsDraft && NoteHasChanges && !IsBusy;

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    NotifyOfPropertyChange(nameof(CanPost));
                    NotifyOfPropertyChange(nameof(CanRemoveLine));
                    NotifyOfPropertyChange(nameof(CanSaveNote));
                    NotifyOfPropertyChange(nameof(CanCommitLine));
                    NotifyOfPropertyChange(nameof(CanTryCommitLine));
                }
            }
        }

        #endregion

        #region UC ItemDimensionEditor

        public ItemDimensionEditorViewModel? Editor
        {
            get;
            private set
            {
                field = value;
                NotifyOfPropertyChange(nameof(Editor));
            }
        }

        private void BuildEditor()
        {
            Editor = new ItemDimensionEditorViewModel(
                searchProvider: SearchItemsAsync,
                direction: DimensionDirection.Out,
                dialogService: _dialogService,
                lotProvider: LoadLotAvailabilityAsync,
                serialProvider: LoadSerialAvailabilityAsync,
                sizeStockProvider: LoadSizeAvailabilityAsync,
                imageProvider: _imageProvider);
            Editor.StorageId = _model.Storage?.Id ?? 0;
            Editor.ExcludeStockMovementId = _model.Id;
            Editor.LineCompleted += OnLineCompletedHandler;
            Editor.RequestUnitCostFocus += OnRequestUnitCostFocus;
            Editor.ItemPickerProvider = OpenItemSearchModalAsync;
            Editor.PropertyChanged += OnEditorPropertyChanged;
        }

        private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (Editor == null) return;
            if (e.PropertyName == nameof(ItemDimensionEditorViewModel.SelectedItem))
            {
                // Auto-populate LineUnitCost desde cache poblado por la búsqueda.
                int? itemId = Editor.SelectedItem?.Id;
                (decimal Available, decimal Cost) data = itemId.HasValue && _baseStockByItemId.TryGetValue(itemId.Value, out var d)
                    ? d
                    : (0m, 0m);
                LineUnitCost = data.Cost;

                // Inyectar tope de stock para validación INotifyDataErrorInfo del campo Cantidad.
                // Solo aplica a items BASE; dimensionados validan en sus modales.
                if (Editor.SelectedItem != null && Editor.DimensionType == DimensionType.Base)
                {
                    Editor.MaxBaseQuantityUnit = Editor.SelectedItem.MeasurementUnit?.Abbreviation ?? string.Empty;
                    Editor.MaxBaseQuantity = data.Available;
                }
                else
                {
                    Editor.MaxBaseQuantity = null;
                    Editor.MaxBaseQuantityUnit = string.Empty;
                }
            }
            if (e.PropertyName == nameof(ItemDimensionEditorViewModel.CanComplete)
                || e.PropertyName == nameof(ItemDimensionEditorViewModel.HasSelectedItem)
                || e.PropertyName == nameof(ItemDimensionEditorViewModel.BaseQuantity)
                || e.PropertyName == nameof(ItemDimensionEditorViewModel.IsBaseDimension))
            {
                NotifyOfPropertyChange(nameof(CanCommitLine));
                NotifyOfPropertyChange(nameof(CanTryCommitLine));
            }
        }

        private void OnRequestUnitCostFocus(object? sender, EventArgs e)
        {
            // Salidas: cost read-only. Foco al botón Agregar via BtnAddFocus (toggle
            // false→true para forzar refoco aunque el binding no detecte cambio).
            BtnAddFocus = false;
            BtnAddFocus = true;
        }

        public bool BtnAddFocus
        {
            get;
            set
            {
                field = value;
                NotifyOfPropertyChange();
            }
        }

        private async Task<ItemGraphQLModel?> OpenItemSearchModalAsync(string initialTerm)
        {
            try
            {
                ItemGraphQLModel? picked = null;
                (GraphQLQueryFragment fragment, string query) = StockMovementOutQueries.ItemsWithStockPage.Value;

                // stockTotalsPage: filtra por bodega del documento; solo devuelve items con qty > 0.
                // ExpandoObject (no anónimo): el modal agrega .matching y .pageResponsePagination
                // dinámicamente — un anónimo es inmutable y haría fallar la asignación.
                dynamic filters = new ExpandoObject();
                filters.storageId = _model.Storage?.Id ?? 0;
                object variables = new GraphQLVariables()
                    .For(fragment, "filters", filters)
                    .Build();

                Func<StockTotalGraphQLModel?, Task> onSelected = entry =>
                {
                    if (entry?.Item != null)
                    {
                        picked = entry.Item;
                        // Costo y disponibilidad provienen de la misma búsqueda — no hay query extra.
                        // BASE row carga ambos; tracking dimensional carga solo costo (availability
                        // efectiva la maneja el modal de dimensiones).
                        _baseStockByItemId[entry.Item.Id] = (entry.Quantity, entry.Cost);
                    }
                    return Task.CompletedTask;
                };
                SearchWithThreeColumnsGridViewModel<StockTotalGraphQLModel> modal =
                    new(
                        query: query,
                        fieldHeader1: "Código", fieldHeader2: "Producto", fieldHeader3: "Referencia",
                        fieldData1: "Code", fieldData2: "Name", fieldData3: "Reference",
                        variables: variables,
                        dialogService: _dialogService,
                        onSelectedAsync: onSelected);

                modal.FilterSearch = initialTerm ?? string.Empty;
                await _dialogService.ShowDialogAsync(modal, "Buscar producto");
                return picked;
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{nameof(OpenItemSearchModalAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void OnLineCompletedHandler(object? sender, LineCompletedEventArgs args)
            => _ = OnLineCompletedAfterUcAsync(args);

        private int _tempIdCounter = 0;
        private int NextTempId() => --_tempIdCounter;

        private Task OnLineCompletedAfterUcAsync(LineCompletedEventArgs args)
        {
            if (!IsDraft) return Task.CompletedTask;

            // Costo y disponibilidad provienen del cache poblado por la búsqueda
            // (stockTotalsPage en SearchItemsAsync / OpenItemSearchModalAsync).
            (decimal available, decimal cost) = _baseStockByItemId.TryGetValue(args.Item.Id, out var data)
                ? data
                : (0m, 0m);

            // Salidas: para items base (sin dimensiones) validar qty vs stock disponible.
            // Items dimensionados: el modal valida por dimensión.
            bool isBaseItem = args.Lots.Count == 0 && args.Serials.Count == 0 && args.Sizes.Count == 0;
            if (isBaseItem && args.TotalQuantity > available)
            {
                _notificationService.ShowError(
                    $"Stock insuficiente. Disponible: {available:0.##} {args.Item.MeasurementUnit?.Abbreviation ?? string.Empty}",
                    durationMs: 6000);
                FocusQuantityDeferred();
                return Task.CompletedTask;
            }

            LineUnitCost = 0m;
            Editor?.Reset();
            FocusSearchDeferred();

            // Optimistic add: DTO local con id temporal negativo + Status=Pending.
            StockMovementLineDTO optimistic = new()
            {
                Id = NextTempId(),
                StockMovementId = _model.Id,
                Item = args.Item,
                DisplayOrder = Lines.Count,
                Status = OperationStatus.Pending
            };
            optimistic.SetQuantitySilently(args.TotalQuantity);
            optimistic.SetUnitCostSilently(cost);
            optimistic.ApplyLocalDimensions(args.Lots, args.Serials, args.Sizes);
            // Snapshot stock para columna informativa — solo BASE (sin dimensiones).
            if (!optimistic.HasDimensions) optimistic.SetAvailableStockSnapshot(available);
            optimistic.LineChanged += OnLineChanged;
            Lines.Add(optimistic);

            // Persistencia en background. No marca IsBusy → UX optimista.
            _ = PersistNewLineAsync(optimistic, args, cost);
            return Task.CompletedTask;
        }

        private void FocusSearchDeferred()
        {
            #pragma warning disable VSTHRD001
            _ = (Application.Current?.Dispatcher.BeginInvoke(
                new System.Action(() => Editor?.FocusSearch()),
                System.Windows.Threading.DispatcherPriority.ContextIdle));
            #pragma warning restore
        }

        private void FocusQuantityDeferred()
        {
            #pragma warning disable VSTHRD001
            _ = (Application.Current?.Dispatcher.BeginInvoke(
                new System.Action(() => Editor?.FocusQuantity()),
                System.Windows.Threading.DispatcherPriority.ContextIdle));
            #pragma warning restore
        }

        /// <summary>Costo unitario para la próxima línea — capturado fuera del UC.</summary>
        public decimal LineUnitCost
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(CanCommitLine));
                NotifyOfPropertyChange(nameof(CanTryCommitLine));
            }
        }

        /// <summary>
        /// Foco al campo de costo. UC dispara esto cuando se selecciona ítem dimensionado.
        /// Toggle false→true para forzar refoco aunque el binding no detecte cambio.
        /// </summary>
        public bool LineUnitCostFocus
        {
            get;
            set
            {
                field = value;
                NotifyOfPropertyChange();
            }
        }

        public bool CanCommitLine => !IsBusy && IsDraft && Editor != null && Editor.CanComplete && LineUnitCost > 0;

        public void CommitLine()
        {
            if (!CanCommitLine) return;
            Editor!.RaiseCompleted();
        }

        /// <summary>
        /// Habilita botón Add. Para items base requiere qty>0; para dimensionados
        /// solo requiere item + costo (modal abre al presionar Add).
        /// </summary>
        public bool CanTryCommitLine => !IsBusy && IsDraft && Editor != null
            && Editor.HasSelectedItem
            && LineUnitCost > 0
            && !_isDimensionsDialogOpen;

        // Guard de re-entrada: previene abrir múltiples modales de dimensiones
        // simultáneos para el mismo item. Bloquea CanTryCommitLine mientras está abierto.
        private bool _isDimensionsDialogOpen;

        /// <summary>
        /// Acción del botón Add. Si el item es dimensionado y aún no tiene dimensiones
        /// completas, abre el modal correspondiente. Si el modal queda vacío
        /// (cancelado o sin datos), la línea NO se persiste.
        /// </summary>
        public async Task TryCommitLineAsync()
        {
            if (!CanTryCommitLine) return;

            // Salidas: tallas y lotes usan modales dedicados (XxxDimensionDialogOutView).
            // No reusar OpenDimensionsDialogAsync del Editor — UX diferente.
            if (Editor!.DimensionType == DimensionType.Size)
            {
                await OpenSizesOutDialogForNewLineAsync();
                return;
            }
            if (Editor.DimensionType == DimensionType.Lot)
            {
                await OpenLotsOutDialogForNewLineAsync();
                return;
            }

            if (Editor.IsDimensioned && !Editor.IsLineComplete)
            {
                await Editor.OpenDimensionsDialogAsync();
                if (!Editor.IsLineComplete) return;
            }
            if (!CanCommitLine) return;
            Editor.RaiseCompleted();
        }

        // Salidas: abre modal dedicado de tallas, construye args y dispara persistencia
        // sin pasar por Editor.RaiseCompleted (que usa el modal compartido del UC).
        private async Task OpenSizesOutDialogForNewLineAsync()
        {
            ItemGraphQLModel? selected = Editor?.SelectedItem;
            if (selected == null) return;
            _isDimensionsDialogOpen = true;
            NotifyOfPropertyChange(nameof(CanTryCommitLine));
            try
            {
                int storageId = _model.Storage?.Id ?? 0;
                SizesDimensionDialogOutViewModel dialog = new(selected, storageId, [], LoadSizeAvailabilityAsync);
                bool? ok = await _dialogService.ShowDialogAsync(dialog, "Asignar tallas (salida)");
                if (ok != true || dialog.Result.Count == 0) return;

                LineCompletedEventArgs args = new()
                {
                    Item = selected,
                    Direction = DimensionDirection.Out,
                    TotalQuantity = dialog.Result.Sum(s => s.Quantity),
                    Sizes = [.. dialog.Result]
                };
                await OnLineCompletedAfterUcAsync(args);
            }
            finally
            {
                _isDimensionsDialogOpen = false;
                NotifyOfPropertyChange(nameof(CanTryCommitLine));
            }
        }

        // Salidas: abre modal dedicado de lotes (FEFO).
        private async Task OpenLotsOutDialogForNewLineAsync()
        {
            ItemGraphQLModel? selected = Editor?.SelectedItem;
            if (selected == null) return;
            _isDimensionsDialogOpen = true;
            NotifyOfPropertyChange(nameof(CanTryCommitLine));
            try
            {
                int storageId = _model.Storage?.Id ?? 0;
                LotsDimensionDialogOutViewModel dialog = new(selected, storageId, [], LoadLotAvailabilityAsync);
                bool? ok = await _dialogService.ShowDialogAsync(dialog, "Asignar lotes (salida)");
                if (ok != true || dialog.Result.Count == 0) return;

                LineCompletedEventArgs args = new()
                {
                    Item = selected,
                    Direction = DimensionDirection.Out,
                    TotalQuantity = dialog.Result.Sum(l => l.Quantity),
                    Lots = [.. dialog.Result]
                };
                await OnLineCompletedAfterUcAsync(args);
            }
            finally
            {
                _isDimensionsDialogOpen = false;
                NotifyOfPropertyChange(nameof(CanTryCommitLine));
            }
        }

        // Salidas: autocomplete usa stockTotalsPage filtrado por bodega del documento.
        // Solo devuelve items con quantity > 0. Deduplica por item.id (un mismo item
        // puede aparecer múltiples veces con distintos trackings BASE/SIZE/LOT/SERIAL).
        private async Task<IReadOnlyList<ItemGraphQLModel>> SearchItemsAsync(ItemSearchFilters filters, CancellationToken token)
        {
            var (fragment, query) = StockMovementOutQueries.ItemsWithStockPage.Value;
            dynamic gqlFilters = new ExpandoObject();
            gqlFilters.storageId = _model.Storage?.Id ?? 0;
            if (!string.IsNullOrWhiteSpace(filters.Term))
                gqlFilters.matching = filters.Term.Trim();

            object variables = new GraphQLVariables()
                .For(fragment, "pagination", new { Page = 1, PageSize = filters.ExactMatchOnly ? 5 : 25 })
                .For(fragment, "filters", gqlFilters)
                .Build();

            PageResponseType<StockTotalGraphQLModel> response = await _service.GetDataContextAsync<PageResponseType<StockTotalGraphQLModel>>(query, variables, token);
            // Cachear cost + available por itemId — populado por la misma búsqueda.
            // BASE row: quantity = stock disponible. Otros trackings: quantity es agregado.
            // Tomamos la primera ocurrencia (BASE preferido si existe) para el costo a mostrar.
            foreach (StockTotalGraphQLModel entry in response.PageResponse.Entries)
            {
                if (entry.Item == null) continue;
                bool isBase = string.Equals(entry.Dimension, "BASE", StringComparison.OrdinalIgnoreCase);
                if (isBase || !_baseStockByItemId.ContainsKey(entry.Item.Id))
                    _baseStockByItemId[entry.Item.Id] = (entry.Quantity, entry.Cost);
            }
            return [.. response.PageResponse.Entries
                .Where(e => e.Item != null)
                .GroupBy(e => e.Item.Id)
                .Select(g => g.First().Item)];
        }

        // Providers de disponibilidad para outbound — consultados por ItemDimensionEditor.
        // Backend rechaza PageSize=-1 (fetch-all) para stock_by_*. Tope API: 100.
        private const int StockByPageSize = 100;

        // Report error → re-throw. Dialog VM (LoadAvailabilityAsync caller) captura
        // y muestra estado al usuario; sin re-throw el modal abre con datos vacíos
        // sin indicar fallo.
        private async Task ReportProviderErrorAsync(string source, Exception ex)
        {
            await _joinableTaskFactory.SwitchToMainThreadAsync();
            ThemedMessageBox.Show("Atención!",
                $"{source}: {ex.GetErrorMessage()}",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async Task<IReadOnlyList<LotAvailability>> LoadLotAvailabilityAsync(int itemId, int storageId, CancellationToken token)
        {
            try
            {
                var (fragment, query) = StockMovementOutQueries.StockByLotsPage.Value;
                object variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = 1, PageSize = StockByPageSize })
                    .For(fragment, "filters", new { itemId, storageId })
                    .Build();
                PageResponseType<StockByLotGraphQLModel> response = await _service.GetDataContextAsync<PageResponseType<StockByLotGraphQLModel>>(query, variables, token);
                return [.. response.PageResponse.Entries
                    .Where(e => e.Lot != null && e.Quantity > 0)
                    .Select(e => new LotAvailability(
                        LotId: e.Lot.Id,
                        LotNumber: e.Lot.LotNumber ?? string.Empty,
                        ExpirationDate: e.Lot.ExpirationDate,
                        AvailableQuantity: e.Quantity,
                        Cost: e.Cost))];
            }
            catch (Exception ex)
            {
                await ReportProviderErrorAsync(nameof(LoadLotAvailabilityAsync), ex);
                throw;
            }
        }

        private async Task<IReadOnlyList<SerialAvailability>> LoadSerialAvailabilityAsync(int itemId, int storageId, CancellationToken token)
        {
            try
            {
                var (fragment, query) = StockMovementOutQueries.StockBySerialsPage.Value;
                object variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = 1, PageSize = StockByPageSize })
                    .For(fragment, "filters", new { itemId, storageId })
                    .Build();
                PageResponseType<StockBySerialGraphQLModel> response = await _service.GetDataContextAsync<PageResponseType<StockBySerialGraphQLModel>>(query, variables, token);
                return [.. response.PageResponse.Entries
                    .Where(e => e.Serial != null)
                    .Select(e => new SerialAvailability(
                        SerialId: e.Serial.Id,
                        SerialNumber: e.Serial.SerialNumber ?? string.Empty,
                        Cost: e.Cost))];
            }
            catch (Exception ex)
            {
                await ReportProviderErrorAsync(nameof(LoadSerialAvailabilityAsync), ex);
                throw;
            }
        }

        private async Task<IReadOnlyList<SizeAvailability>> LoadSizeAvailabilityAsync(int itemId, int storageId, CancellationToken token)
        {
            try
            {
                var (fragment, query) = StockMovementOutQueries.StockBySizesPage.Value;
                object variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = 1, PageSize = StockByPageSize })
                    .For(fragment, "filters", new { itemId, storageId })
                    .Build();
                PageResponseType<StockBySizeGraphQLModel> response = await _service.GetDataContextAsync<PageResponseType<StockBySizeGraphQLModel>>(query, variables, token);
                return [.. response.PageResponse.Entries
                    .Where(e => e.Size != null && e.Quantity > 0)
                    .Select(e => new SizeAvailability(
                        SizeId: e.Size.Id,
                        SizeName: e.Size.Name ?? string.Empty,
                        AvailableQuantity: e.Quantity,
                        Cost: e.Cost))];
            }
            catch (Exception ex)
            {
                await ReportProviderErrorAsync(nameof(LoadSizeAvailabilityAsync), ex);
                throw;
            }
        }

        #endregion

        #region Lifecycle

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                if (Editor != null)
                {
                    Editor.LineCompleted -= OnLineCompletedHandler;
                    Editor.RequestUnitCostFocus -= OnRequestUnitCostFocus;
                    Editor.PropertyChanged -= OnEditorPropertyChanged;
                    Editor = null;
                }
                UnsubscribeLines();
                _operationLineMapping.Clear();
                Lines.Clear();
                _noteDebounceCts?.Cancel();
                _noteDebounceCts?.Dispose();
                _noteDebounceCts = null;
                _eventAggregator.Unsubscribe(this);
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        public async Task LoadDataForEditAsync(int id)
        {
            var (fragment, query) = StockMovementOutQueries.StockMovementById.Value;
            object variables = new GraphQLVariables().For(fragment, "id", id).Build();
            StockMovementGraphQLModel sm = await _service.FindByIdAsync(query, variables);
            if (sm == null) throw new InvalidOperationException("Stock movement no encontrado.");
            _model = sm;
            // SeedNote ANTES que Note: el setter de Note evalúa NoteHasChanges
            // contra SeedNote para decidir si dispara AutoSaveNoteAsync. Asignar
            // primero evita el save espurio en load.
            SeedNote = sm.Note ?? string.Empty;
            Note = SeedNote;
            UnsubscribeLines();
            Lines.Clear();
            foreach (StockMovementLineGraphQLModel l in sm.Lines.OrderBy(x => x.DisplayOrder))
                Lines.Add(StockMovementLineDTO.FromModel(l));
            SubscribeLines();
            // Pre-cargar cache de stock disponible por item desde la misma query del draft
            // (item.stocks viene anidado, sin round-trip extra). Habilita validación in-grid
            // de cantidad para items BASE en líneas existentes sin necesidad de buscar otra
            // vez. Filtra fila BASE de la bodega del documento.
            int currentStorageId = _model.Storage?.Id ?? 0;
            foreach (StockMovementLineGraphQLModel l in sm.Lines)
            {
                if (l.Item == null || l.Item.Stocks == null) continue;
                StockTotalGraphQLModel? baseRow = l.Item.Stocks
                    .FirstOrDefault(s => string.Equals(s.Dimension, "BASE", StringComparison.OrdinalIgnoreCase)
                                      && s.Storage?.Id == currentStorageId);
                if (baseRow != null)
                    _baseStockByItemId[l.Item.Id] = (baseRow.Quantity, baseRow.Cost);
            }
            // Snapshot informativo de stock por línea BASE — alimenta columna "Stock" en grid.
            // Solo se setea para items sin dimensiones; dimensionados quedan en null.
            foreach (StockMovementLineDTO dto in Lines)
            {
                if (dto.HasDimensions || dto.Item == null) continue;
                if (_baseStockByItemId.TryGetValue(dto.Item.Id, out (decimal Available, decimal Cost) data))
                    dto.SetAvailableStockSnapshot(data.Available);
            }
            NotifyAllHeader();
            // Solo construir Editor la primera vez. Reload solo refresca la grilla
            // y mantiene el UC vivo (preserva foco / bindings).
            if (Editor == null) BuildEditor();
            else Editor.StorageId = _model.Storage?.Id ?? 0;
        }

        private void NotifyAllHeader()
        {
            NotifyOfPropertyChange(nameof(Id));
            NotifyOfPropertyChange(nameof(DocumentNumber));
            NotifyOfPropertyChange(nameof(DocumentDisplay));
            NotifyOfPropertyChange(nameof(Status));
            NotifyOfPropertyChange(nameof(AccountingSourceName));
            NotifyOfPropertyChange(nameof(AccountingSourceCode));
            NotifyOfPropertyChange(nameof(CostCenterName));
            NotifyOfPropertyChange(nameof(StorageName));
            NotifyOfPropertyChange(nameof(InsertedAt));
            NotifyOfPropertyChange(nameof(DocumentDate));
            NotifyOfPropertyChange(nameof(DocumentTime));
            NotifyOfPropertyChange(nameof(IsDraft));
            NotifyOfPropertyChange(nameof(CanPost));
            NotifyOfPropertyChange(nameof(NoteHasChanges));
        }

        #endregion

        #region Save Note (incremental)

        public async Task SaveNoteAsync()
        {
            if (!CanSaveNote) return;
            try
            {
                IsBusy = true;
                var (fragment, query) = StockMovementOutQueries.UpdateDraft.Value;
                object variables = new GraphQLVariables()
                    .For(fragment, "id", _model.Id)
                    .For(fragment, "data", new { note = Note.Trim() })
                    .Build();
                UpdateDraftResponse? responseObj = await _service.MutationContextAsync<UpdateDraftResponse>(query, variables);
                StockMovementMutationPayload? payload = responseObj?.UpdateResponse;
                if (payload == null || !payload.Success)
                {
                    _notificationService.ShowError(
                        StockMovementErrorFormatter.Format(payload?.Message, payload?.Errors, "No se pudo actualizar la nota."),
                        durationMs: 8000);
                    return;
                }
                SeedNote = Note;
                NotifyOfPropertyChange(nameof(NoteHasChanges));
                NotifyOfPropertyChange(nameof(CanSaveNote));
                _notificationService.ShowSuccess(payload.Message);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{nameof(SaveNoteAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsBusy = false; }
        }

        #endregion

        #region Add line

        /// <summary>
        /// Persistencia background del optimistic add. No marca IsBusy. En caso de éxito,
        /// asigna real Id + Status=Saved. En error, remueve la fila y notifica.
        /// </summary>
        private async Task PersistNewLineAsync(StockMovementLineDTO dto, LineCompletedEventArgs args, decimal unitCost)
        {
            try
            {
                var (addFragment, addQuery) = StockMovementOutQueries.AddDraftLine.Value;
                object addVars = new GraphQLVariables()
                    .For(addFragment, "input", new
                    {
                        stockMovementId = _model.Id,
                        itemId = args.Item.Id,
                        quantity = args.TotalQuantity,
                        unitCost,
                        displayOrder = dto.DisplayOrder
                    })
                    .Build();
                AddLineResponse? addResponseObj = await _service.MutationContextAsync<AddLineResponse>(addQuery, addVars);
                StockMovementLineMutationPayload? addPayload = addResponseObj?.CreateResponse;
                if (addPayload == null || !addPayload.Success || addPayload.StockMovementLine == null)
                {
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    Lines.Remove(dto);
                    _notificationService.ShowError(
                        StockMovementErrorFormatter.Format(addPayload?.Message, addPayload?.Errors, "No se pudo agregar la línea."),
                        durationMs: 8000);
                    return;
                }
                int realId = addPayload.StockMovementLine.Id;

                bool dimsOk = await SetDimensionsAsync(realId, args, unitCost);
                if (!dimsOk)
                {
                    await DeleteLineSilentAsync(realId);
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    Lines.Remove(dto);
                    return;
                }

                await _joinableTaskFactory.SwitchToMainThreadAsync();
                dto.Id = realId;
                dto.Status = OperationStatus.Saved;
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                Lines.Remove(dto);
                ThemedMessageBox.Show("Atención!",
                    $"{nameof(PersistNewLineAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<bool> SetDimensionsAsync(int lineId, LineCompletedEventArgs args, decimal unitCost)
        {
            if (args.Lots.Count > 0)
            {
                var (frag, query) = StockMovementOutQueries.SetLineLots.Value;
                // Salidas: enviar lotId (lote existente). lotNumber/expirationDate
                // sólo aplican a entradas (lote nuevo). unitCost ignorado en salidas.
                object[] lotsArr = args.Lots.Select((l, i) => (object)new
                {
                    lotId = l.LotId,
                    quantity = l.Quantity,
                    displayOrder = i
                }).ToArray();
                object vars = new GraphQLVariables()
                    .For(frag, "input", new { stockMovementLineId = lineId, lots = lotsArr })
                    .Build();
                SetLotsResponse? responseObj = await _service.MutationContextAsync<SetLotsResponse>(query, vars);
                if (responseObj?.UpdateResponse?.Success != true)
                {
                    _notificationService.ShowError(
                        StockMovementErrorFormatter.Format(responseObj?.UpdateResponse?.Message, responseObj?.UpdateResponse?.Errors, "Error guardando lotes."),
                        durationMs: 8000);
                    return false;
                }
            }
            else if (args.Serials.Count > 0)
            {
                var (frag, query) = StockMovementOutQueries.SetLineSerials.Value;
                // Salidas: enviar serialId (serial existente). serialNumber sólo aplica
                // a entradas. unitCost ignorado en salidas.
                object[] serialsArr = args.Serials.Select((s, i) => (object)new
                {
                    serialId = s.SerialId,
                    displayOrder = i
                }).ToArray();
                object vars = new GraphQLVariables()
                    .For(frag, "input", new { stockMovementLineId = lineId, serials = serialsArr })
                    .Build();
                SetSerialsResponse? responseObj = await _service.MutationContextAsync<SetSerialsResponse>(query, vars);
                if (responseObj?.UpdateResponse?.Success != true)
                {
                    _notificationService.ShowError(
                        StockMovementErrorFormatter.Format(responseObj?.UpdateResponse?.Message, responseObj?.UpdateResponse?.Errors, "Error guardando seriales."),
                        durationMs: 8000);
                    return false;
                }
            }
            else if (args.Sizes.Count > 0)
            {
                var (frag, query) = StockMovementOutQueries.SetLineSizes.Value;
                // Salidas: unitCost ignorado por backend. Omitir.
                object[] sizesArr = args.Sizes.Select((s, i) => (object)new
                {
                    sizeId = s.SizeId,
                    quantity = s.Quantity,
                    displayOrder = i
                }).ToArray();
                object vars = new GraphQLVariables()
                    .For(frag, "input", new { stockMovementLineId = lineId, sizes = sizesArr })
                    .Build();
                SetSizesResponse? responseObj = await _service.MutationContextAsync<SetSizesResponse>(query, vars);
                if (responseObj?.UpdateResponse?.Success != true)
                {
                    _notificationService.ShowError(
                        StockMovementErrorFormatter.Format(responseObj?.UpdateResponse?.Message, responseObj?.UpdateResponse?.Errors, "Error guardando tallas."),
                        durationMs: 8000);
                    return false;
                }
            }
            return true;
        }

        // Rollback de línea creada cuando SetDimensions falla. Errores aquí se ignoran
        // intencionalmente: el error original ya se mostró al usuario, y un fallo
        // del rollback dejaría una línea huérfana en el server pero no es UX-blocking.
        private async Task DeleteLineSilentAsync(int lineId)
        {
            try
            {
                var (frag, query) = StockMovementOutQueries.DeleteDraftLine.Value;
                object vars = new GraphQLVariables().For(frag, "id", lineId).Build();
                await _service.MutationContextAsync<DeleteLineResponse>(query, vars);
            }
            catch { /* swallow — best-effort rollback */ }
        }

        #endregion

        #region Remove line

        // Edición in-line: cada línea es DTO observable. Setters disparan LineChanged
        // con old/new value → VM valida → enqueue operation. BackgroundQueueService
        // batchea operaciones del mismo tipo y deduplica por line.Id.

        private void SubscribeLines()
        {
            foreach (StockMovementLineDTO l in Lines) l.LineChanged += OnLineChanged;
        }

        private void UnsubscribeLines()
        {
            foreach (StockMovementLineDTO l in Lines) l.LineChanged -= OnLineChanged;
        }

        private void OnLineChanged(object? sender, LineChangedEventArgs e)
        {
            if (sender is not StockMovementLineDTO dto) return;
            if (!IsDraft || dto.Id <= 0) return;

            if (e.PropertyName == nameof(StockMovementLineDTO.Quantity) && e.NewValue <= 0)
            {
                _notificationService.ShowError("La cantidad debe ser mayor que cero.", "Error de validación");
                dto.SetQuantitySilently(e.OldValue);
                return;
            }
            if (e.PropertyName == nameof(StockMovementLineDTO.Quantity)
                && !dto.HasDimensions
                && dto.Item?.Id is int itemId
                && _baseStockByItemId.TryGetValue(itemId, out (decimal Available, decimal Cost) data)
                && e.NewValue > data.Available)
            {
                string unit = dto.Item.MeasurementUnit?.Abbreviation ?? string.Empty;
                _notificationService.ShowError(
                    $"Stock insuficiente. Disponible: {data.Available:0.##} {unit}",
                    durationMs: 6000);
                dto.SetQuantitySilently(e.OldValue);
                return;
            }
            if (e.PropertyName == nameof(StockMovementLineDTO.UnitCost) && e.NewValue < 0)
            {
                _notificationService.ShowError("El costo unitario no puede ser negativo.", "Error de validación");
                dto.SetUnitCostSilently(e.OldValue);
                return;
            }

            _ = EnqueueLineUpdateAsync(dto);
        }

        private async Task EnqueueLineUpdateAsync(StockMovementLineDTO dto)
        {
            try
            {
                dto.Status = OperationStatus.Pending;
                StockMovementLineUpdateOperation operation = new(_lineService)
                {
                    LineId = dto.Id,
                    StockMovementId = _model.Id,
                    Quantity = dto.Quantity,
                    UnitCost = dto.UnitCost ?? 0m
                };
                _operationLineMapping[operation.OperationId] = dto.Id;
                await _backgroundQueueService.EnqueueOperationAsync(operation);
            }
            catch (InvalidOperationException)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                dto.Status = OperationStatus.Failed;
                _notificationService.ShowError(_backgroundQueueService.GetCriticalErrorMessage());
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                dto.Status = OperationStatus.Failed;
                _notificationService.ShowError($"Error inesperado al actualizar línea #{dto.Id}: {ex.GetErrorMessage()}", durationMs: 6000);
            }
        }

        public Task HandleAsync(OperationCompletedMessage message, CancellationToken cancellationToken)
        {
            if (!_operationLineMapping.TryGetValue(message.OperationId, out int lineId)) return Task.CompletedTask;
            StockMovementLineDTO? dto = Lines.FirstOrDefault(l => l.Id == lineId);
            if (dto == null) return Task.CompletedTask;

            if (message.Success)
            {
                dto.Status = OperationStatus.Saved;
                _operationLineMapping.Remove(message.OperationId);
            }
            else if (message.IsRetrying)
            {
                dto.Status = OperationStatus.Retrying;
                dto.StatusTooltip = message.ErrorDetail;
            }
            else
            {
                dto.Status = OperationStatus.Failed;
                dto.StatusTooltip = message.ErrorDetail ?? message.Exception?.Message;
                _operationLineMapping.Remove(message.OperationId);
                _notificationService.ShowError(
                    $"Error al guardar línea #{dto.Id}: {message.ErrorDetail ?? message.Exception?.Message}",
                    durationMs: 6000);
            }
            return Task.CompletedTask;
        }

        public async Task RemoveLineAsync()
        {
            if (!CanRemoveLine || SelectedLine == null) return;
            if (ThemedMessageBox.Show("Confirmar",
                "¿Eliminar la línea seleccionada?",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                IsBusy = true;
                var (frag, query) = StockMovementOutQueries.DeleteDraftLine.Value;
                object vars = new GraphQLVariables().For(frag, "id", SelectedLine.Id).Build();
                DeleteLineResponse? responseObj = await _service.MutationContextAsync<DeleteLineResponse>(query, vars);
                if (responseObj?.DeleteResponse?.Success != true)
                {
                    _notificationService.ShowError(
                        StockMovementErrorFormatter.Format(responseObj?.DeleteResponse?.Message, responseObj?.DeleteResponse?.Errors, "No se pudo eliminar la línea."),
                        durationMs: 8000);
                    return;
                }
                await ReloadAsync();
                FocusSearchDeferred();
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{nameof(RemoveLineAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsBusy = false; }
        }

        public async Task EditLineDimensionsAsync(StockMovementLineDTO line)
        {
            if (!IsDraft || line == null || line.Id <= 0 || !line.HasDimensions) return;
            int storageId = _model.Storage?.Id ?? 0;
            decimal cost = line.UnitCost ?? 0m;
            try
            {
                IsBusy = true;
                LineCompletedEventArgs? newArgs = null;
                if (line.IsLot)
                {
                    IEnumerable<LotDraft> initial = line.LotRows
                        .Select(r => new LotDraft(r.LotId, r.LotNumber, r.ExpirationDate, r.Quantity));
                    LotsDimensionDialogOutViewModel dialog = new(line.Item, storageId, initial, LoadLotAvailabilityAsync);
                    bool? ok = await _dialogService.ShowDialogAsync(dialog, "Editar lotes");
                    if (ok != true) return;
                    newArgs = new LineCompletedEventArgs
                    {
                        Item = line.Item,
                        Direction = DimensionDirection.Out,
                        Lots = [.. dialog.Result]
                    };
                }
                else if (line.IsSerial)
                {
                    IEnumerable<SerialDraft> initial = line.SerialRows
                        .Select(r => new SerialDraft(r.SerialId, r.SerialNumber));
                    SerialsDimensionDialogViewModel dialog = new(line.Item, DimensionDirection.Out, storageId, initial, LoadSerialAvailabilityAsync);
                    bool? ok = await _dialogService.ShowDialogAsync(dialog, "Editar seriales");
                    if (ok != true) return;
                    newArgs = new LineCompletedEventArgs
                    {
                        Item = line.Item,
                        Direction = DimensionDirection.Out,
                        Serials = dialog.Result
                    };
                }
                else if (line.IsSize)
                {
                    IEnumerable<SizeDraft> initial = line.SizeRows
                        .Select(r => new SizeDraft(r.SizeId, r.SizeName, r.Quantity));
                    SizesDimensionDialogOutViewModel dialog = new(line.Item, storageId, initial, LoadSizeAvailabilityAsync);
                    bool? ok = await _dialogService.ShowDialogAsync(dialog, "Editar tallas");
                    if (ok != true) return;
                    newArgs = new LineCompletedEventArgs
                    {
                        Item = line.Item,
                        Direction = DimensionDirection.Out,
                        Sizes = [.. dialog.Result]
                    };
                }
                if (newArgs == null) return;
                line.Status = OperationStatus.Pending;
                bool ok2 = await SetDimensionsAsync(line.Id, newArgs, cost);
                if (ok2)
                {
                    line.Status = OperationStatus.Saved;
                    await ReloadAsync();
                }
                else
                {
                    line.Status = OperationStatus.Failed;
                }
            }
            catch (Exception ex)
            {
                line.Status = OperationStatus.Failed;
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{nameof(EditLineDimensionsAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Post

        public async Task PostAsync()
        {
            if (!CanPost) return;
            await FlushNoteAsync();
            if (ThemedMessageBox.Show("Confirmar publicación",
                $"¿Confirma que desea publicar este borrador?",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                IsBusy = true;
                var (frag, query) = StockMovementOutQueries.PostMovement.Value;
                object vars = new GraphQLVariables().For(frag, "id", _model.Id).Build();
                PostResponse? responseObj = await _service.MutationContextAsync<PostResponse>(query, vars);
                StockMovementMutationPayload? payload = responseObj?.UpdateResponse;
                if (payload == null || !payload.Success)
                {
                    _notificationService.ShowError(
                        StockMovementErrorFormatter.Format(payload?.Message, payload?.Errors, "No se pudo postear."),
                        durationMs: 8000);
                    return;
                }
                string? assignedNumber = payload.StockMovement?.DocumentNumber;
                string successMsg = string.IsNullOrEmpty(assignedNumber)
                    ? payload.Message
                    : $"{payload.Message} (Documento: {assignedNumber})";
                _notificationService.ShowSuccess(successMsg);
                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new StockMovementPostMessage { PostedStockMovement = payload }, CancellationToken.None);
                RaiseRequestClose();
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{nameof(PostAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsBusy = false; }
        }

        #endregion

        public async Task CloseAsync()
        {
            // Composición con estado del Editor: si hay item seleccionado, ESC
            // limpia primero (UX original — primero deselecciona, segundo cierra).
            // Sin esto, opt-in del verbo Close pisa el handler local del Editor.
            if (Editor?.HasSelectedItem == true)
            {
                Editor.ClearSearch();
                return;
            }
            await FlushNoteAsync();
            RaiseRequestClose();
        }

        // Aliases AppCommands
        public Task DeleteAsync() => RemoveLineAsync();
        public bool CanDelete => CanRemoveLine;
        public Task ConfirmAsync() => TryCommitLineAsync();
        public bool CanConfirm => CanTryCommitLine;
        public Task SaveAsync() => SaveNoteAsync();
        public bool CanSave => CanSaveNote;
        public bool CanClose => true;

        public void Search() => Editor?.FocusSearch();
        public bool CanSearch => IsDraft && Editor != null && !IsBusy;

        public void FocusGrid() => this.FocusFirstRow("LinesGrid");
        public bool CanFocusGrid => Lines.Count > 0;

        public void FocusNote() => this.SetFocus("NoteEditor");
        public bool CanFocusNote => IsDraft;

        /// <summary>Disparado cuando el Detail termina (post o close). El conductor reacciona.</summary>
        public event EventHandler? RequestClose;
        private void RaiseRequestClose() => RequestClose?.Invoke(this, EventArgs.Empty);

        private async Task ReloadAsync()
        {
            await LoadDataForEditAsync(_model.Id);
            // Avisa al master para que se refresque también
            await _eventAggregator.PublishOnCurrentThreadAsync(
                new StockMovementUpdateMessage { UpdatedStockMovement = new StockMovementMutationPayload { StockMovement = _model, Success = true } },
                CancellationToken.None);
        }

        // Wrappers respuestas
        private class AddLineResponse { public StockMovementLineMutationPayload CreateResponse { get; set; } = new(); }
        private class DeleteLineResponse { public StockMovementLineMutationPayload DeleteResponse { get; set; } = new(); }
        private class SetLotsResponse { public StockMovementLineLotsMutationPayload UpdateResponse { get; set; } = new(); }
        private class SetSerialsResponse { public StockMovementLineSerialsMutationPayload UpdateResponse { get; set; } = new(); }
        private class SetSizesResponse { public StockMovementLineSizesMutationPayload UpdateResponse { get; set; } = new(); }
        private class UpdateDraftResponse { public StockMovementMutationPayload UpdateResponse { get; set; } = new(); }
        private class PostResponse { public StockMovementMutationPayload UpdateResponse { get; set; } = new(); }
    }
}
