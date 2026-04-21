using Models.Login;
using System.Collections.Generic;

namespace NetErp.Books.AccountingEntries
{
    /// <summary>
    /// Wrapper local para la respuesta de <c>upsertDraftLines</c>.
    /// El schema retorna contadores (insertedCount / updatedCount) y no una entidad,
    /// por lo que no encaja con <see cref="Models.Global.GraphQLResponseTypes.UpsertResponseType{T}"/>.
    /// </summary>
    public class UpsertDraftLinesPayloadWrapper
    {
        public UpsertDraftLinesResponse UpsertResponse { get; set; } = new();

        public class UpsertDraftLinesResponse
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public int InsertedCount { get; set; }
            public int UpdatedCount { get; set; }
            public List<GlobalErrorGraphQLModel> Errors { get; set; } = [];
        }
    }

    /// <summary>
    /// Wrapper local para <c>deleteDraftLines</c> / <c>clearDraftLines</c>.
    /// </summary>
    public class DeleteDraftLinesPayloadWrapper
    {
        public DeleteDraftLinesResponse DeleteResponse { get; set; } = new();

        public class DeleteDraftLinesResponse
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public int DeletedCount { get; set; }
            public List<GlobalErrorGraphQLModel> Errors { get; set; } = [];
        }
    }
}
