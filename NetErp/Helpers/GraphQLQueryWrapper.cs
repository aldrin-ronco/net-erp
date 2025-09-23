using GraphQL.Query.Builder;

namespace NetErp.Helpers
{
    public static class GraphQLQueryWrapper
    {
        public static string BuildQuery<T>(IQuery<T> query, string operationType = "query")
        {
            return $"{operationType} {{ {query.Build()} }}";
        }
        
        public static string BuildMutation<T>(IQuery<T> mutation)
        {
            return BuildQuery(mutation, "mutation");
        }
    }
}