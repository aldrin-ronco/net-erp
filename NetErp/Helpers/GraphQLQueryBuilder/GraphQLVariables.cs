using System.Collections.Generic;
using System.Dynamic;

namespace NetErp.Helpers.GraphQLQueryBuilder
{
    /// <summary>
    /// Builder fluido para construir variables de queries GraphQL con nombres correctos.
    /// Elimina el error humano al generar nombres de variables que deben coincidir
    /// con los que el GraphQLQueryBuilder produce internamente.
    /// <example>
    /// var variables = new GraphQLVariables()
    ///     .For(customerFragment, "pagination", new { Page = 1, PageSize = 50 })
    ///     .For(customerFragment, "filters", new { isActive = true, matching = "texto" })
    ///     .Build();
    /// </example>
    /// </summary>
    public class GraphQLVariables
    {
        private readonly ExpandoObject _variables = new();

        /// <summary>
        /// Agrega una variable asociada a un fragment y parámetro específico.
        /// El nombre de la variable se genera automáticamente a partir del alias del fragment.
        /// </summary>
        public GraphQLVariables For(GraphQLQueryFragment fragment, string paramName, object value)
        {
            string aliasOrName = string.IsNullOrEmpty(fragment.Alias) ? fragment.Name : fragment.Alias;
            string key = GraphQLQueryFragment.GetVariableName(aliasOrName, paramName);
            ((IDictionary<string, object>)_variables)[key] = value;
            return this;
        }

        /// <summary>
        /// Construye el objeto dinámico con todas las variables registradas.
        /// </summary>
        public dynamic Build() => _variables;
    }
}
