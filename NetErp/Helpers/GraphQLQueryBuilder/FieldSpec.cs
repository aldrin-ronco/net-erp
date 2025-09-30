using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Expression = System.Linq.Expressions.Expression;

namespace NetErp.Helpers.GraphQLQueryBuilder
{
    // Helper tipado para construir el mismo Dictionary<string, object>
    // que espera GraphQLQueryFragment.fields, pero con IntelliSense sobre modelos C#.
    public sealed class FieldSpec<T>
    {
        private readonly Dictionary<string, object> _map = [];
        private static readonly Dictionary<string, object> Leaf = GraphQLQueryFragment.mapStringDynamicEmptyNode;
        private readonly Func<string, string> _formatter;

        public FieldSpec(Func<string, string>? formatter = null)
        {
            _formatter = formatter ?? DefaultCamelCase;
        }

        public static FieldSpec<T> Create(Func<string, string>? formatter = null) => new(formatter);

        // Campo escalar (hoja)
        public FieldSpec<T> Field<TProp>(Expression<Func<T, TProp>> selector, string? overrideName = null, string? alias = null)
        {
            var name = overrideName ?? Format(GetMemberName(selector));
            var key = CombineAlias(alias, name);
            _map[key] = Leaf;
            return this;
        }

        // Campo anidado (objeto)
        public FieldSpec<T> Select<TProp>(Expression<Func<T, TProp>> selector, Action<FieldSpec<TProp>> nested, string? overrideName = null, string? alias = null)
        {
            var name = overrideName ?? Format(GetMemberName(selector));
            var key = CombineAlias(alias, name);
            var child = FieldSpec<TProp>.Create(_formatter);
            nested(child);
            _map[key] = child.Build();
            return this;
        }

        // Campo anidado para colecciones (List/Enumerable)
        public FieldSpec<T> SelectList<TItem>(Expression<Func<T, IEnumerable<TItem>>> selector, Action<FieldSpec<TItem>> nested, string? overrideName = null, string? alias = null)
        {
            var name = overrideName ?? Format(GetMemberName(selector));
            var key = CombineAlias(alias, name);
            var child = FieldSpec<TItem>.Create(_formatter);
            nested(child);
            _map[key] = child.Build();
            return this;
        }

        public Dictionary<string, object> Build() => _map;

        private string Format(string name)
        {
            return _formatter != null ? _formatter(name) : name;
        }

        private static string CombineAlias(string? alias, string name)
        {
            return string.IsNullOrWhiteSpace(alias) ? name : $"{alias}:{name}";
        }

        // Formateador por defecto: camelCase (baja solo el primer carácter)
        public static string DefaultCamelCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (char.IsLower(s[0])) return s;
            return char.ToLowerInvariant(s[0]) + s[1..];
        }

        private static string GetMemberName<L, R>(Expression<Func<L, R>> selector)
        {
            Expression body = selector.Body;
            if (body is UnaryExpression u && u.NodeType == ExpressionType.Convert)
                body = u.Operand;

            if (body is MemberExpression m)
                return m.Member.Name;

            throw new InvalidOperationException("Selector must be a simple member access, e.g., x => x.Property");
        }
    }
}
