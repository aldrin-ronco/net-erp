using Common.Interfaces;
using NetErp.Helpers.GraphQLQueryBuilder;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;

namespace NetErp.Helpers.Cache
{
    /// <summary>
    /// Combina múltiples caches en una sola query GraphQL para reducir llamadas a la API.
    /// Aprovecha la capacidad nativa de GraphQL de concatenar múltiples queries raíz en un solo request.
    /// </summary>
    public static class CacheBatchLoader
    {
        /// <summary>
        /// Carga múltiples caches en una sola request HTTP.
        /// - Los caches ya inicializados se omiten.
        /// - Los caches que implementan IBatchLoadableCache se combinan en una sola query.
        /// - Si solo queda 1 cache pendiente, se carga individualmente con EnsureLoadedAsync.
        /// </summary>
        public static async Task LoadAsync(
            IGraphQLClient client,
            CancellationToken cancellationToken = default,
            params IBatchLoadableCache[] caches)
        {
            // Filtrar caches que necesitan carga
            var pending = caches.Where(c => !c.IsInitialized).ToList();

            if (pending.Count == 0) return;

            // Si solo 1, no tiene sentido armar query combinada
            if (pending.Count == 1)
            {
                await ((dynamic)pending[0]).EnsureLoadedAsync();
                return;
            }

            // Construir query combinada: cada fragment usa alias vacío para que
            // el Name sea la clave en el JSON de respuesta y los nombres de variables sean únicos
            var fragments = new List<GraphQLQueryFragment>();
            var nameToCache = new Dictionary<string, IBatchLoadableCache>();
            var variables = new GraphQLVariables();

            foreach (var cache in pending)
            {
                var original = cache.LoadFragment;

                // Fragment sin alias: Name será la clave en la respuesta JSON
                // y generará variables como countriesPagePagination, zonesPagePagination, etc.
                var batchFragment = new GraphQLQueryFragment(
                    original.Name,
                    original.Parameters,
                    original.Fields,
                    alias: string.Empty
                );

                fragments.Add(batchFragment);
                nameToCache[original.Name] = cache;
                cache.ApplyVariables(variables, batchFragment);
            }

            var query = new QueryBuilder(fragments).GetQuery();
            var vars = variables.Build();

            // 1 sola request HTTP
            var response = await client.ExecuteQueryAsync<JObject>(query, vars, cancellationToken);

            // Rutear cada porción de la respuesta a su cache
            foreach (var (name, cache) in nameToCache)
            {
                if (response.TryGetValue(name, out JToken? token))
                {
                    cache.PopulateFromBatchResponse(token);
                }
            }
        }
    }
}
