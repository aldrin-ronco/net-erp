using System.Collections.Generic;
using FluentAssertions;
using NetErp.Helpers.GraphQLQueryBuilder;
using Xunit;

namespace NetErp.Tests.Helpers.GraphQLQueryBuilder;

public class GraphQLQueryFragmentTests
{
    private static readonly Dictionary<string, object> Leaf = GraphQLQueryFragment.mapStringDynamicEmptyNode;

    [Fact]
    public void BuildQuery_ScalarFields_ProducesCorrectGraphQL()
    {
        var fields = new Dictionary<string, object>
        {
            ["id"] = Leaf,
            ["name"] = Leaf
        };
        var fragment = new GraphQLQueryFragment("myQuery", [], fields);

        fragment.BuildQuery();

        fragment.graphQlQuery.Should().Contain("myQuery");
        fragment.graphQlQuery.Should().Contain("id");
        fragment.graphQlQuery.Should().Contain("name");
    }

    [Fact]
    public void BuildQuery_NestedFields_ProducesNestedGraphQL()
    {
        var nestedFields = new Dictionary<string, object>
        {
            ["value"] = Leaf
        };
        var fields = new Dictionary<string, object>
        {
            ["id"] = Leaf,
            ["nested"] = nestedFields
        };
        var fragment = new GraphQLQueryFragment("myQuery", [], fields);

        fragment.BuildQuery();

        fragment.graphQlQuery.Should().Contain("nested {");
        fragment.graphQlQuery.Should().Contain("value");
        fragment.graphQlQuery.Should().Contain("}");
    }

    [Fact]
    public void BuildQuery_WithParameters_IncludesParamsInQuery()
    {
        var parameters = new List<GraphQLQueryParameter>
        {
            new("input", "CreateInput!")
        };
        var fields = new Dictionary<string, object>
        {
            ["id"] = Leaf
        };
        var fragment = new GraphQLQueryFragment("createEntity", parameters, fields);

        fragment.BuildQuery();

        fragment.graphQlQuery.Should().Contain("createEntity(");
        fragment.graphQlQuery.Should().Contain("input:$createEntityInput");
    }

    [Fact]
    public void BuildQuery_WithAlias_PrependsAlias()
    {
        var fields = new Dictionary<string, object>
        {
            ["id"] = Leaf
        };
        var fragment = new GraphQLQueryFragment("myQuery", [], fields, alias: "MyAlias");

        fragment.BuildQuery();

        fragment.graphQlQuery.Should().StartWith("MyAlias:myQuery");
    }

    [Fact]
    public void GetVariableName_CombinesAliasAndParam()
    {
        var result = GraphQLQueryFragment.GetVariableName("PageResponse", "pagination");

        result.Should().Be("pageResponsePagination");
    }

    [Fact]
    public void GetVariableName_EmptyAlias_UsesParamOnly()
    {
        var result = GraphQLQueryFragment.GetVariableName("", "pagination");

        result.Should().Be("Pagination");
    }

    [Fact]
    public void GetHeadersParameters_ProducesTypeDeclarations()
    {
        var parameters = new List<GraphQLQueryParameter>
        {
            new("pagination", "PaginationInput!"),
            new("filters", "FilterInput")
        };
        var fragment = new GraphQLQueryFragment("myQuery", parameters, new Dictionary<string, object>());

        var result = fragment.GetHeadersParameters();

        result.Should().Contain("$myQueryPagination:PaginationInput!");
        result.Should().Contain("$myQueryFilters:FilterInput");
    }

    [Fact]
    public void GetParameters_NoParams_ReturnsEmpty()
    {
        var fragment = new GraphQLQueryFragment("myQuery", [], new Dictionary<string, object>());

        var result = fragment.GetParameters();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetParameters_WithBrackets_WrapsInParens()
    {
        var parameters = new List<GraphQLQueryParameter>
        {
            new("input", "CreateInput!")
        };
        var fragment = new GraphQLQueryFragment("myQuery", parameters, new Dictionary<string, object>());

        var result = fragment.GetParameters(addBrackets: true);

        result.Should().StartWith("(");
        result.Should().EndWith(")");
    }

    [Fact]
    public void GetVariableNameOfType_LowercasesFirstChar()
    {
        var fragment = new GraphQLQueryFragment("q", [], new Dictionary<string, object>());

        fragment.GetVariableNameOfType("PaginationInput").Should().Be("paginationInput");
    }

    [Fact]
    public void GetVariableNameOfType_EmptyString_ReturnsEmpty()
    {
        var fragment = new GraphQLQueryFragment("q", [], new Dictionary<string, object>());

        fragment.GetVariableNameOfType("").Should().BeEmpty();
    }

    [Fact]
    public void UppercaseFirstChar_LowercaseInput_UppercasesFirst()
    {
        var fragment = new GraphQLQueryFragment("q", [], new Dictionary<string, object>());

        fragment.UppercaseFirstChar("hello").Should().Be("Hello");
    }

    [Fact]
    public void UppercaseFirstChar_EmptyString_ReturnsEmpty()
    {
        var fragment = new GraphQLQueryFragment("q", [], new Dictionary<string, object>());

        fragment.UppercaseFirstChar("").Should().BeEmpty();
    }
}
