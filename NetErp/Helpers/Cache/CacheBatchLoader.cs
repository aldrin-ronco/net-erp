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

            // Construir query combinada:
            // - Caso común (un solo cache por root field): alias vacío → Name actúa como clave
            //   JSON y semilla de nombres de variable (banksPagePagination, countriesPagePagination).
            // - Caso con colisión (varios caches apuntan al mismo root field, p.ej. los 3 caches
            //   de cash drawers usan "cashDrawersPage" con filtros distintos): se asigna un alias
            //   único sufijado (cashDrawersPage, cashDrawersPage2, cashDrawersPage3) para que
            //   GraphQL no colisione los campos y cada fragment produzca su propia clave JSON.
            var fragments = new List<GraphQLQueryFragment>();
            var nameToCache = new Dictionary<string, IBatchLoadableCache>();
            var variables = new GraphQLVariables();
            var usedKeys = new HashSet<string>();

            foreach (var cache in pending)
            {
                var original = cache.LoadFragment;

                // Si el Name aún no está tomado, usa alias vacío (ruta estándar).
                // Si ya está tomado, suma sufijo numérico hasta encontrar uno libre.
                string effectiveAlias;
                string responseKey;
                if (usedKeys.Add(original.Name))
                {
                    effectiveAlias = string.Empty;
                    responseKey = original.Name;
                }
                else
                {
                    int suffix = 2;
                    string candidate;
                    do
                    {
                        candidate = $"{original.Name}{suffix++}";
                    } while (!usedKeys.Add(candidate));
                    effectiveAlias = candidate;
                    responseKey = candidate;
                }

                var batchFragment = new GraphQLQueryFragment(
                    original.Name,
                    original.Parameters,
                    original.Fields,
                    alias: effectiveAlias);

                fragments.Add(batchFragment);
                nameToCache[responseKey] = cache;
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
