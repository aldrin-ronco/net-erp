using System.Collections.Generic;
using FluentAssertions;
using NetErp.Helpers.GraphQLQueryBuilder;
using Xunit;

namespace NetErp.Tests.Helpers.GraphQLQueryBuilder;

public class GraphQLQueryBuilderTests
{
    private static readonly Dictionary<string, object> Leaf = GraphQLQueryFragment.mapStringDynamicEmptyNode;

    private static GraphQLQueryFragment CreateSimpleFragment(string name, string alias = "", List<GraphQLQueryParameter>? parameters = null)
    {
        var fields = new Dictionary<string, object>
        {
            ["id"] = Leaf,
            ["name"] = Leaf
        };
        return new GraphQLQueryFragment(name, parameters ?? [], fields, alias);
    }

    [Fact]
    public void GetQuery_DefaultQuery_StartsWithQuery()
    {
        var builder = new NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder([CreateSimpleFragment("myQuery")]);

        var query = builder.GetQuery();

        query.Should().StartWith("query");
    }

    [Fact]
    public void GetQuery_Mutation_StartsWithMutation()
    {
        var builder = new NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder([CreateSimpleFragment("myMutation")]);

        var query = builder.GetQuery(GraphQLOperations.MUTATION);

        query.Should().StartWith("mutation");
    }

    [Fact]
    public void GetQuery_MultipleFragments_CombinesAll()
    {
        var fragment1 = CreateSimpleFragment("queryA");
        var fragment2 = CreateSimpleFragment("queryB");
        var builder = new NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder([fragment1, fragment2]);

        var query = builder.GetQuery();

        query.Should().Contain("queryA");
        query.Should().Contain("queryB");
    }

    [Fact]
    public void GetParameters_NoParams_ReturnsEmpty()
    {
        var builder = new NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder([CreateSimpleFragment("q")]);

        var result = builder.GetParameters();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetParameters_MergesAcrossFragments()
    {
        var fragment1 = CreateSimpleFragment("q1", parameters: [new GraphQLQueryParameter("pagination", "PaginationInput!")]);
        var fragment2 = CreateSimpleFragment("q2", parameters: [new GraphQLQueryParameter("filters", "FilterInput!")]);
        var builder = new NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder([fragment1, fragment2]);

        var result = builder.GetParameters();

        result.Should().Contain("$q1Pagination:PaginationInput!");
        result.Should().Contain("$q2Filters:FilterInput!");
    }

    [Fact]
    public void GetQuery_WithParameters_IncludesParametersInHeader()
    {
        var fragment = CreateSimpleFragment("q", parameters: [new GraphQLQueryParameter("input", "CreateInput!")]);
        var builder = new NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder([fragment]);

        var query = builder.GetQuery(GraphQLOperations.MUTATION);

        query.Should().StartWith("mutation (");
        query.Should().Contain("$qInput:CreateInput!");
    }

    [Fact]
    public void GetQuery_EndsWithClosingBrace()
    {
        var builder = new NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder([CreateSimpleFragment("q")]);

        var query = builder.GetQuery();

        query.Should().EndWith("}");
    }
}
