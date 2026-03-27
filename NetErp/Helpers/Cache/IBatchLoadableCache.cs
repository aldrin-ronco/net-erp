using NetErp.Helpers.GraphQLQueryBuilder;
using Newtonsoft.Json.Linq;

namespace NetErp.Helpers.Cache
{
    /// <summary>
    /// Interfaz para caches que pueden participar en carga batch.
    /// CacheBatchLoader combina los fragments de múltiples caches en una sola query GraphQL,
    /// ejecuta 1 request HTTP, y rutea cada porción de la respuesta al cache correspondiente.
    /// </summary>
    public interface IBatchLoadableCache : IEntityCache
    {
        /// <summary>
        /// Fragment de la query de carga de este cache.
        /// CacheBatchLoader lo usa para construir la query combinada.
        /// </summary>
        GraphQLQueryFragment LoadFragment { get; }

        /// <summary>
        /// Aplica las variables que este cache necesita para su fragment.
        /// El batchFragment proporcionado tiene alias vacío para generar nombres de variables únicos.
        /// </summary>
        void ApplyVariables(GraphQLVariables variables, GraphQLQueryFragment batchFragment);

        /// <summary>
        /// Recibe la porción de la respuesta batch correspondiente a este cache
        /// y pobla la colección interna. Debe marcar IsInitialized = true.
        /// </summary>
        void PopulateFromBatchResponse(JToken data);
    }
}
