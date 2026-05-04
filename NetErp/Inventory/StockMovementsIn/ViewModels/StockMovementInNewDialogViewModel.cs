using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using Models.Inventory;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Inventory.StockMovementsIn.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Inventory.StockMovementsIn.ViewModels
{
    /// <summary>
    /// Modal para crear el draft inicial de un Stock Movement de entrada.
    /// El usuario captura: Centro de costo, Fuente contable (kardexFlow=I),
    /// Bodega y Note. Save dispara <c>createStockMovementDraft</c>.
    /// El conductor abre Detail con el id devuelto.
    /// </summary>
    public class StockMovementInNewDialogViewModel : Screen
    {
        private readonly IRepository<StockMovementGraphQLModel> _service;
        private readonly CostCenterCache _costCenterCache;
        private readonly InboundAccountingSourceCache _inboundAccountingSourceCache;
        private readonly StorageCache _storageCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly IEventAggregator _eventAggregator;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        public StockMovementInNewDialogViewModel(
            IRepository<StockMovementGraphQLModel> service,
            CostCenterCache costCenterCache,
            InboundAccountingSourceCache inboundAccountingSourceCache,
            StorageCache storageCache,
            StringLengthCache stringLengthCache,
            IEventAggregator eventAggregator,
            JoinableTaskFactory joinableTaskFactory)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _costCenterCache = costCenterCache ?? throw new ArgumentNullException(nameof(costCenterCache));
            _inboundAccountingSourceCache = inboundAccountingSourceCache ?? throw new ArgumentNullException(nameof(inboundAccountingSourceCache));
            _storageCache = storageCache ?? throw new ArgumentNullException(nameof(storageCache));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _joinableTaskFactory = joinableTaskFactory ?? throw new ArgumentNullException(nameof(joinableTaskFactory));
            DialogWidth = 600;
        }

        public double DialogWidth { get; set; }
        public double DialogHeight { get; } = 380;

        public int NoteMaxLength => _stringLengthCache.GetMaxLength<StockMovementGraphQLModel>(nameof(StockMovementGraphQLModel.Note));

        public ObservableCollection<CostCenterGraphQLModel> CostCenters { get; } = [];
        public ObservableCollection<AccountingSourceGraphQLModel> AccountingSources { get; } = [];
        public ObservableCollection<StorageGraphQLModel> Storages { get; } = [];

        public CostCenterGraphQLModel? SelectedCostCenter
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenter));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public AccountingSourceGraphQLModel? SelectedAccountingSource
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingSource));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public StorageGraphQLModel? SelectedStorage
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedStorage));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public string Note
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value ?? string.Empty;
                    NotifyOfPropertyChange(nameof(Note));
                }
            }
        } = string.Empty;

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool CanSave =>
            !IsBusy &&
            SelectedCostCenter != null && SelectedCostCenter.Id > 0 &&
            SelectedAccountingSource != null && SelectedAccountingSource.Id > 0 &&
            SelectedStorage != null && SelectedStorage.Id > 0;

        /// <summary>Id del draft creado, expuesto al caller tras Save exitoso.</summary>
        public int CreatedId { get; private set; }

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                // Defensivo: si el master ya cargó los caches (caso normal), EnsureLoadedAsync
                // hace no-op. Si el modal se abre sin master previo, esto los inicializa.
                await Task.WhenAll(
                    _costCenterCache.EnsureLoadedAsync(),
                    _inboundAccountingSourceCache.EnsureLoadedAsync(),
                    _storageCache.EnsureLoadedAsync());

                CostCenters.Clear();
                foreach (CostCenterGraphQLModel cc in _costCenterCache.Items) CostCenters.Add(cc);

                AccountingSources.Clear();
                foreach (AccountingSourceGraphQLModel s in _inboundAccountingSourceCache.Items) AccountingSources.Add(s);

                Storages.Clear();
                foreach (StorageGraphQLModel s in _storageCache.Items
                    .Where(x => string.Equals(x.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.Name))
                    Storages.Add(s);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"Error cargando datos. {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                await TryCloseAsync(false);
            }
        }

        public async Task SaveAsync()
        {
            if (!CanSave) return;
            try
            {
                IsBusy = true;
                var (fragment, query) = StockMovementInQueries.CreateDraft.Value;
                dynamic input = new ExpandoObject();
                input.accountingSourceId = SelectedAccountingSource!.Id;
                input.costCenterId = SelectedCostCenter!.Id;
                input.storageId = SelectedStorage!.Id;
                if (!string.IsNullOrWhiteSpace(Note)) input.note = Note.Trim();

                object variables = new GraphQLVariables().For(fragment, "input", input).Build();
                NewDraftResponse? responseObj = await _service.MutationContextAsync<NewDraftResponse>(query, variables);
                StockMovementMutationPayload? payload = responseObj?.CreateResponse;

                if (payload == null || !payload.Success || payload.StockMovement == null)
                {
                    ThemedMessageBox.Show("Error",
                        StockMovementErrorFormatter.Format(payload?.Message, payload?.Errors, "No se pudo crear el draft."),
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                CreatedId = payload.StockMovement.Id;
                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new StockMovementCreateMessage { CreatedStockMovement = payload },
                    CancellationToken.None);
                await TryCloseAsync(true);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(SaveAsync)} \r\n{ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsBusy = false; }
        }

        public Task CancelAsync() => TryCloseAsync(false);

        // Aliases AppCommands
        public Task CloseAsync() => CancelAsync();
        public bool CanClose => true;
    }

    internal class NewDraftResponse
    {
        public StockMovementMutationPayload CreateResponse { get; set; } = new();
    }
}
