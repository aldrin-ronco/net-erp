using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
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
        private readonly IRepository<StorageGraphQLModel> _storageService;
        private readonly CostCenterCache _costCenterCache;
        private readonly IEventAggregator _eventAggregator;

        public StockMovementInNewDialogViewModel(
            IRepository<StockMovementGraphQLModel> service,
            IRepository<AccountingSourceGraphQLModel> accountingSourceService,
            IRepository<StorageGraphQLModel> storageService,
            CostCenterCache costCenterCache,
            ObservableCollection<AccountingSourceGraphQLModel> inboundAccountingSources,
            IEventAggregator eventAggregator)
        {
            _service = service;
            _storageService = storageService;
            _costCenterCache = costCenterCache;
            _eventAggregator = eventAggregator;
            AccountingSources = inboundAccountingSources ?? [];
            DialogWidth = 600;
        }

        public double DialogWidth { get; set; }

        public ObservableCollection<CostCenterGraphQLModel> CostCenters { get; } = [];
        public ObservableCollection<AccountingSourceGraphQLModel> AccountingSources { get; }
        public ObservableCollection<StorageGraphQLModel> Storages { get; } = [];

        public CostCenterGraphQLModel? SelectedCostCenter { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(SelectedCostCenter)); NotifyOfPropertyChange(nameof(CanSave)); } } }
        public AccountingSourceGraphQLModel? SelectedAccountingSource { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(SelectedAccountingSource)); NotifyOfPropertyChange(nameof(CanSave)); } } }
        public StorageGraphQLModel? SelectedStorage { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(SelectedStorage)); NotifyOfPropertyChange(nameof(CanSave)); } } }
        public string Note { get; set { if (field != value) { field = value ?? string.Empty; NotifyOfPropertyChange(nameof(Note)); } } } = string.Empty;

        public bool IsBusy { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(IsBusy)); NotifyOfPropertyChange(nameof(CanSave)); } } }

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
                await _costCenterCache.EnsureLoadedAsync();
                CostCenters.Clear();
                foreach (var cc in _costCenterCache.Items) CostCenters.Add(cc);

                await LoadStoragesAsync();
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show("Atención!",
                    $"Error cargando datos. {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                await TryCloseAsync(false);
            }
        }

        private async Task LoadStoragesAsync()
        {
            var (fragment, query) = StockMovementInQueries.Storages.Value;
            object variables = new GraphQLVariables()
                .For(fragment, "pagination", new { Page = 1, PageSize = -1 })
                .For(fragment, "filters", new { Status = "ACTIVE" })
                .Build();
            var page = await _storageService.GetPageAsync(query, variables);
            Storages.Clear();
            foreach (var s in page.Entries.OrderBy(x => x.Name)) Storages.Add(s);
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
                var responseObj = await _service.MutationContextAsync<NewDraftResponse>(query, variables);
                var payload = responseObj?.CreateResponse;

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
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(SaveAsync)} \r\n{ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsBusy = false; }
        }

        public Task CancelAsync() => TryCloseAsync(false);
    }

    internal class NewDraftResponse
    {
        public StockMovementMutationPayload CreateResponse { get; set; } = new();
    }
}
