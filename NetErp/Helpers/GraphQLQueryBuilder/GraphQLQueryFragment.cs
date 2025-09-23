using System;
using System.Collections.Generic;
using System.Linq;

namespace NetErp.Helpers.GraphQLQueryBuilder
{
    public class GraphQLQueryFragment
    {
        public string Name { get; }
        public List<GraphQLQueryParameter> Parameters { get; }
        public Dictionary<string, object> Fields { get; }
        public string Alias { get; }
        public string graphQlQuery = string.Empty;
        public static readonly Dictionary<string, object> mapStringDynamicEmptyNode = [];
        public List<string> parentStack = [];
        public int nestingLevel = 4;
        public int nestingFactor = 2;

        public GraphQLQueryFragment(
            string name,
            List<GraphQLQueryParameter> parameters,
            Dictionary<string, object> fields,
            string alias = "")
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
            Alias = alias ?? string.Empty;
        }

        public void BuildQuery()
        {
            graphQlQuery = string.Empty;
            foreach (var kv in Fields)
            {
                ReadMap(kv.Key, (IDictionary<string, object>)kv.Value);
            }
            graphQlQuery = $"{Alias}{(string.IsNullOrEmpty(Alias) ? string.Empty : ":")}{Name}{(Parameters.Count > 0 ? GetParameters(addBrackets: true) : string.Empty)} {{\n" +
                           graphQlQuery +
                           new string(' ', 2) + "}";
        }

        public string GetVariableNameOfType(string type)
        {
            return string.IsNullOrEmpty(type)
                ? string.Empty
                : char.ToLower(type[0]) + type[1..];
        }

        // Default variable naming: fragmentCamel (or aliasCamel) + ParamPascal
        private string BuildVariableName(string paramName)
        {
            string baseName = string.IsNullOrEmpty(Alias) ? Name : Alias;
            string fragmentCamel = string.IsNullOrEmpty(baseName)
                ? string.Empty
                : char.ToLower(baseName[0]) + baseName[1..];
            string paramPascal = string.IsNullOrEmpty(paramName)
                ? string.Empty
                : char.ToUpper(paramName[0]) + paramName[1..];
            return fragmentCamel + paramPascal;
        }

        public string GetParameters(bool addBrackets = false, bool escapeDollarSign = false)
        {
            string p = addBrackets ? "(" : string.Empty;
            if (Parameters.Count == 0) return string.Empty;
            foreach (var param in Parameters)
            {
                string varName = BuildVariableName(param.Name);
                p = escapeDollarSign
                    ? $"{p}{param.Name}:\\${varName}, "
                    : $"{p}{param.Name}:${varName}, ";
            }
            return string.Concat(p.AsSpan(0, p.Length - 2), addBrackets ? ")" : string.Empty);
        }

        public string UppercaseFirstChar(string str)
        {
            return string.IsNullOrEmpty(str)
                ? str
                : char.ToUpper(str[0]) + str[1..];
        }

        public string GetHeadersParameters(bool addBrackets = false, bool escapeDollarSign = false)
        {
            string p = addBrackets ? "(" : string.Empty;
            if (Parameters.Count == 0) return string.Empty;
            foreach (var param in Parameters)
            {
                string varName = BuildVariableName(param.Name);
                p = escapeDollarSign
                    ? $"{p}\\${varName}:{param.Type}, "
                    : $"{p}${varName}:{param.Type}, ";
            }
            return string.Concat(p.AsSpan(0, p.Length - 2), addBrackets ? ")" : string.Empty);
        }

        public void ReadMap(string parent, IDictionary<string, object> field)
        {
            if (field.Count == 0 && parentStack.Count == 0)
            {
                graphQlQuery = graphQlQuery + new string(' ', nestingLevel) + parent + "\n";
            }
            else
            {
                if (field.Count != 0)
                {
                    graphQlQuery = graphQlQuery + new string(' ', nestingLevel) + parent + " {\n";
                    parentStack.Add(parent);
                }
                bool idented = false;
                foreach (var kv in field)
                {
                    if (!idented)
                    {
                        nestingLevel += nestingFactor;
                        idented = true;
                    }
                    var valueMap = (IDictionary<string, object>)kv.Value;
                    if (valueMap.Count == 0)
                    {
                        graphQlQuery = graphQlQuery + new string(' ', nestingLevel) + kv.Key + "\n";
                    }
                    ReadMap(kv.Key, valueMap);
                }
                if (parentStack.Any(e => e == parent))
                {
                    nestingLevel -= nestingFactor;
                    graphQlQuery = graphQlQuery + new string(' ', nestingLevel) + "}\n";
                    parentStack.RemoveAll(e => e == parent);
                }
            }
        }
    }
}
