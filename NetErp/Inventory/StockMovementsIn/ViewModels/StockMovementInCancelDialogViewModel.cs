using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Models.Inventory;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Inventory.StockMovementsIn.Helpers;
using System;
using System.Dynamic;
using System.Threading.Tasks;
using System.Windows;

namespace NetErp.Inventory.StockMovementsIn.ViewModels
{
    /// <summary>
    /// Modal pequeño que captura la nota de anulación y dispara <c>cancelStockMovement</c>.
    /// </summary>
    public class StockMovementInCancelDialogViewModel : Screen
    {
        private readonly IRepository<StockMovementGraphQLModel> _service;
        private readonly int _stockMovementId;

        public StockMovementInCancelDialogViewModel(
            IRepository<StockMovementGraphQLModel> service,
            int stockMovementId,
            string documentNumber)
        {
            _service = service;
            _stockMovementId = stockMovementId;
            DocumentNumber = documentNumber ?? string.Empty;
            DialogWidth = 520;
        }

        public double DialogWidth { get; set; }
        public string DocumentNumber { get; }

        public string Note { get; set { if (field != value) { field = value ?? string.Empty; NotifyOfPropertyChange(nameof(Note)); NotifyOfPropertyChange(nameof(CanConfirm)); } } } = string.Empty;
        public bool IsBusy { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(IsBusy)); NotifyOfPropertyChange(nameof(CanConfirm)); } } }
        public bool CanConfirm => !IsBusy && !string.IsNullOrWhiteSpace(Note);

        public StockMovementMutationPayload? Result { get; private set; }

        public async Task ConfirmAsync()
        {
            if (!CanConfirm) return;
            try
            {
                IsBusy = true;
                var (fragment, query) = StockMovementInQueries.CancelMovement.Value;
                dynamic input = new ExpandoObject();
                input.id = _stockMovementId;
                input.note = Note.Trim();
                object variables = new GraphQLVariables().For(fragment, "input", input).Build();
                var responseObj = await _service.MutationContextAsync<CancelResponse>(query, variables);
                var payload = responseObj?.UpdateResponse;
                if (payload == null || !payload.Success)
                {
                    ThemedMessageBox.Show("Error",
                        payload?.Message ?? "No se pudo anular el movimiento.",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                Result = payload;
                await TryCloseAsync(true);
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(ConfirmAsync)} \r\n{ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsBusy = false; }
        }

        public Task CancelAsync() => TryCloseAsync(false);

        private class CancelResponse
        {
            public StockMovementMutationPayload UpdateResponse { get; set; } = new();
        }
    }
}
