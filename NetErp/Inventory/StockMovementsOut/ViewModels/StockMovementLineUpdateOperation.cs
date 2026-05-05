using Common.Interfaces;
using Models.Global;
using Models.Inventory;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Helpers.Services;
using System;
using System.Collections.Generic;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Inventory.StockMovementsOut.ViewModels
{
    /// <summary>
    /// Operación de actualización in-line de líneas de stock movement (DRAFT).
    /// Ejecutada por <see cref="IBackgroundQueueService"/> y agrupada por tipo
    /// para llamar <c>batchUpdateStockMovementDraftLines</c> en una sola
    /// mutación. Operaciones con el mismo <see cref="Id"/> (line.Id) reemplazan
    /// la versión previa en la cola — debounce natural por línea.
    /// </summary>
    public class StockMovementLineUpdateOperation(IRepository<StockMovementLineGraphQLModel> repository) : IDataOperation
    {
        private readonly IRepository<StockMovementLineGraphQLModel> _repository = repository;

        public int LineId { get; set; }
        public int StockMovementId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }

        // Variables expone solo el item (forma intermedia). El runtime de
        // BackgroundQueueService siempre pasa por ExtractBatchItem → BuildBatchVariables
        // antes de llamar al API — Variables nunca se envía tal cual.
        public object Variables => new
        {
            id = LineId.ToString(),
            quantity = Quantity,
            unitCost = UnitCost
        };

        public static Type OperationResponseType => typeof(StockMovementLineGraphQLModel);
        public Type ResponseType => OperationResponseType;
        public Guid OperationId { get; set; } = Guid.NewGuid();
        public string DisplayName => $"Línea #{LineId}";
        public int Id => LineId;

        public BatchOperationInfo GetBatchInfo()
        {
            GraphQLQueryFragment fragment = _batchUpdateMutation.Value.Fragment;
            return new BatchOperationInfo
            {
                BatchQuery = _batchUpdateMutation.Value.Query,
                ExtractBatchItem = variables => variables,
                BuildBatchVariables = batchItems => new GraphQLVariables()
                    .For(fragment, "input", new
                    {
                        stockMovementId = StockMovementId,
                        items = batchItems
                    })
                    .Build(),
                ExecuteBatchAsync = async (query, variables, cancellationToken) =>
                    await _repository.BatchAsync<BatchResultGraphQLModel>(query, variables, cancellationToken)
            };
        }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _batchUpdateMutation = new(() =>
        {
            var fields = FieldSpec<BatchResultGraphQLModel>
                .Create()
                .Field(f => f.Success)
                .Field(f => f.Message)
                .Field(f => f.TotalAffected)
                .Field(f => f.AffectedIds)
                .SelectList(f => f.Errors!, sq => sq.Field(e => e.Message))
                .Build();

            List<GraphQLQueryParameter> parameters =
            [
                new("input", "BatchUpdateStockMovementDraftLinesInput!")
            ];
            GraphQLQueryFragment fragment = new("batchUpdateStockMovementDraftLines",
                parameters, fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });
    }
}
