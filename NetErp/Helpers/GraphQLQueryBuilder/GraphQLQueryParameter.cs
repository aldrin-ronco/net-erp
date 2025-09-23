using System;

namespace NetErp.Helpers.GraphQLQueryBuilder
{
    public class GraphQLQueryParameter
    {
        public string Name { get; }
        public string Type { get; }

        public GraphQLQueryParameter(string name, string type)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }
    }
}
