using System;
using System.Collections.Generic;
using System.Text;

namespace NetErp.Helpers.GraphQLQueryBuilder
{
    public enum GraphQLOperations { QUERY, MUTATION }

    public class GraphQLQueryBuilder
    {
        public List<GraphQLQueryFragment> Fragments { get; }

        public GraphQLQueryBuilder(List<GraphQLQueryFragment> fragments)
        {
            Fragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
        }

        public string GetQuery(GraphQLOperations type = GraphQLOperations.QUERY)
        {
            string operationName = type == GraphQLOperations.QUERY ? "query" : "mutation";
            string query = operationName + GetParameters() + " {\n";
            foreach (var queryFragment in Fragments)
            {
                queryFragment.BuildQuery();
                query += new string(' ', 2) + queryFragment.graphQlQuery + "\n";
            }
            return query + "}";
        }

        public string GetParameters()
        {
            string parameters = string.Empty;
            foreach (var query in Fragments)
            {
                if (query.Parameters.Count > 0)
                {
                    parameters += query.GetHeadersParameters(addBrackets: false) + ", ";
                }
            }
            return string.IsNullOrEmpty(parameters)
                ? string.Empty
                : string.Concat(" (", parameters.AsSpan(0, parameters.Length - 2), ")");
        }
    }
}
