using Models.Global;
using System;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Inventory
{
    /// <summary>
    /// Lote de un ítem lot-tracked.
    /// Mapea al tipo <c>Lot</c> del schema GraphQL.
    /// </summary>
    public class LotGraphQLModel
    {
        public int Id { get; set; }
        public string LotNumber { get; set; } = string.Empty;
        public DateTime? ExpirationDate { get; set; }
        public CompanyGraphQLModel Company { get; set; } = new();
        public ItemGraphQLModel Item { get; set; } = new();
        public SystemAccountGraphQLModel? CreatedBy { get; set; }
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class LotCreateMessage
    {
        public UpsertResponseType<LotGraphQLModel> CreatedLot { get; set; } = new();
    }

    public class LotUpdateMessage
    {
        public UpsertResponseType<LotGraphQLModel> UpdatedLot { get; set; } = new();
    }

    public class LotDeleteMessage
    {
        public DeleteResponseType DeletedLot { get; set; } = new();
    }
}
