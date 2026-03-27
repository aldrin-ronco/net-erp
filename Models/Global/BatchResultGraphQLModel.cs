using Models.Login;
using System.Collections.Generic;

namespace Models.Global
{
    /// <summary>
    /// Modelo normalizado para respuestas de mutaciones batch (BatchResultPayload del schema).
    /// Todas las mutaciones batch de la API retornan esta estructura.
    /// </summary>
    public class BatchResultGraphQLModel
    {
        public List<int> AffectedIds { get; set; } = [];
        public List<GlobalErrorGraphQLModel>? Errors { get; set; }
        public string? Message { get; set; }
        public bool Success { get; set; }
        public int TotalAffected { get; set; }
    }
}
