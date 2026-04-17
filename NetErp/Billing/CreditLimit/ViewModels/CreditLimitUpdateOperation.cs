using Common.Interfaces;
using Models.Billing;
using Models.Global;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Helpers.Services;
using System;
using System.Collections.Generic;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Billing.CreditLimit.ViewModels
{
    public class CreditLimitUpdateOperation(IRepository<CreditLimitGraphQLModel> repository) : IDataOperation
    {
        private readonly IRepository<CreditLimitGraphQLModel> _repository = repository;

        public decimal NewLimit { get; set; }
        public int CustomerId { get; set; }

        public object Variables => new
        {
            item = new
            {
                creditLimit = NewLimit,
                customerId = CustomerId
            }
        };

        public static Type OperationResponseType => typeof(CreditLimitGraphQLModel);
        public Type ResponseType => OperationResponseType;
        public Guid OperationId { get; set; } = Guid.NewGuid();
        public string DisplayName => $"Customer #{CustomerId}";
        public int Id => CustomerId;

        public BatchOperationInfo GetBatchInfo()
        {
            return new BatchOperationInfo
            {
                BatchQuery = _batchUpsertCreditLimitMutation.Value,
                ExtractBatchItem = (variables) =>
                {
                    return variables.GetType().GetProperty("item")!.GetValue(variables)!;
                },
                BuildBatchVariables = (batchItems) => new
                {
                    singleItemResponseInput = new
                    {
                        items = batchItems
                    }
                },
                ExecuteBatchAsync = async (query, variables, cancellationToken) =>
                    await _repository.BatchAsync<BatchResultGraphQLModel>(query, variables, cancellationToken)
            };
        }

        private static readonly Lazy<string> _batchUpsertCreditLimitMutation = new(() =>
        {
            var fields = FieldSpec<BatchResultGraphQLModel>
                .Create()
                .Field(f => f.Success)
                .Field(f => f.Message)
                .Field(f => f.TotalAffected)
                .Field(f => f.AffectedIds)
                .SelectList(f => f.Errors!, sq => sq.Field(e => e.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("input", "BatchUpsertCreditLimitsInput!")
            };
            var fragment = new GraphQLQueryFragment("batchUpsertCreditLimits", parameters, fields, "SingleItemResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });
    }
}
