using Common.Interfaces;
using Models.Global;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Helpers.Cache
{
    public class StringLengthCache : IEntityCache
    {
        private readonly IRepository<EntityStringLengthsGraphQLModel> _repository;
        private readonly object _lock = new();

        // entity (snake_case) → column (snake_case) → maxLength
        private readonly Dictionary<string, Dictionary<string, int>> _data = [];
        private readonly HashSet<string> _loadedEntities = [];

        public bool IsInitialized => _loadedEntities.Count > 0;

        public StringLengthCache(IRepository<EntityStringLengthsGraphQLModel> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Ensures the specified model types have their string lengths loaded from the API.
        /// Idempotent: only fetches entities not yet in cache.
        /// </summary>
        public async Task EnsureEntitiesLoadedAsync(params Type[] modelTypes)
        {
            string[] entityNames;

            lock (_lock)
            {
                entityNames = modelTypes
                    .Select(ResolveEntityName)
                    .Where(name => !_loadedEntities.Contains(name))
                    .Distinct()
                    .ToArray();
            }

            if (entityNames.Length == 0) return;

            var query = BuildQuery();
            dynamic variables = new ExpandoObject();
            variables.listResponseEntities = entityNames.ToList();

            var results = await _repository.GetListAsync(query, variables);
            var misconfiguredFields = new List<string>();

            lock (_lock)
            {
                foreach (var entity in results)
                {
                    var entityKey = entity.Entity;
                    if (!_data.ContainsKey(entityKey))
                    {
                        _data[entityKey] = new Dictionary<string, int>();
                    }

                    foreach (var field in entity.Fields)
                    {
                        if (field.MaxLength <= 0)
                        {
                            misconfiguredFields.Add($"'{field.Column}' de la entidad '{entityKey}'");
                        }
                        else
                        {
                            _data[entityKey][field.Column] = field.MaxLength;
                        }
                    }
                }
            }

            if (misconfiguredFields.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Los siguientes campos no tienen longitud configurada:\n{string.Join("\n", misconfiguredFields)}");
            }

            // Mark as loaded only after successful validation
            lock (_lock)
            {
                foreach (var name in entityNames)
                {
                    _loadedEntities.Add(name);
                }
            }
        }

        /// <summary>
        /// Gets the max length for a property on a model type.
        /// Returns 0 if not found (DevExpress TextEdit.MaxLength = 0 means no limit).
        /// </summary>
        public int GetMaxLength<TModel>(string propertyName)
        {
            var entityName = ResolveEntityName(typeof(TModel));
            var columnName = ToSnakeCase(propertyName);

            lock (_lock)
            {
                if (_data.TryGetValue(entityName, out var columns) &&
                    columns.TryGetValue(columnName, out var maxLength))
                {
                    return maxLength;
                }
            }

            return 0;
        }

        public void Clear()
        {
            lock (_lock)
            {
                _data.Clear();
                _loadedEntities.Clear();
            }
        }

        /// <summary>
        /// Resolves a GraphQL model type to its snake_case entity name.
        /// Example: AccountingEntityGraphQLModel → "accounting_entity"
        /// </summary>
        public static string ResolveEntityName(Type type)
        {
            var name = type.Name.Replace("GraphQLModel", "");
            return ToSnakeCase(name);
        }

        /// <summary>
        /// Converts PascalCase to snake_case.
        /// Examples: "BusinessName" → "business_name", "AccountingEntity" → "accounting_entity"
        /// </summary>
        public static string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                var c = input[i];
                if (char.IsUpper(c))
                {
                    if (i > 0)
                    {
                        sb.Append('_');
                    }
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static string BuildQuery()
        {
            var fields = FieldSpec<EntityStringLengthsGraphQLModel>
                .Create()
                .Field(f => f.Entity)
                .Field(f => f.Domain)
                .Field(f => f.DisplayName)
                .Field(f => f.Schema)
                .Field(f => f.Table)
                .SelectList(f => f.Fields, nested => nested
                    .Field(f => f.Column)
                    .Field(f => f.MaxLength))
                .Build();

            var parameter = new GraphQLQueryParameter("entities", "[String!]");
            var fragment = new GraphQLQueryFragment(
                "stringLengths",
                [parameter],
                fields,
                "ListResponse");

            return new GraphQLQueryBuilder.GraphQLQueryBuilder([fragment]).GetQuery();
        }
    }
}
