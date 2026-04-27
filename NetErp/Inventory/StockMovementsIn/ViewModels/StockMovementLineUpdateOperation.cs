using Common.Interfaces;
using Models.Global;
using Models.Inventory;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Helpers.Services;
using System;
using System.Collections.Generic;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Inventory.StockMovementsIn.ViewModels
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

        public object Variables => new
        {
            item = new
            {
                id = LineId.ToString(),
                quantity = Quantity,
                unitCost = UnitCost
            },
            stockMovementId = StockMovementId
        };

        public static Type OperationResponseType => typeof(StockMovementLineGraphQLModel);
        public Type ResponseType => OperationResponseType;
        public Guid OperationId { get; set; } = Guid.NewGuid();
        public string DisplayName => $"Línea #{LineId}";
        public int Id => LineId;

        public BatchOperationInfo GetBatchInfo()
        {
            return new BatchOperationInfo
            {
                BatchQuery = _batchUpdateMutation.Value,
                ExtractBatchItem = variables =>
                    variables.GetType().GetProperty("item")!.GetValue(variables)!,
                BuildBatchVariables = batchItems => new
                {
                    singleItemResponseInput = new
                    {
                        stockMovementId = StockMovementId,
                        items = batchItems
                    }
                },
                ExecuteBatchAsync = async (query, variables, cancellationToken) =>
                    await _repository.BatchAsync<BatchResultGraphQLModel>(query, variables, cancellationToken)
            };
        }

        private static readonly Lazy<string> _batchUpdateMutation = new(() =>
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
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });
    }
}
