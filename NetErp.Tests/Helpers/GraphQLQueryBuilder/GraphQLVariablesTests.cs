using System.Collections.Generic;
using FluentAssertions;
using NetErp.Helpers.GraphQLQueryBuilder;
using Xunit;

namespace NetErp.Tests.Helpers.GraphQLQueryBuilder;

public class GraphQLVariablesTests
{
    private static readonly Dictionary<string, object> Leaf = GraphQLQueryFragment.mapStringDynamicEmptyNode;

    private static GraphQLQueryFragment CreateFragment(string name, string alias = "")
    {
        return new GraphQLQueryFragment(name, [new GraphQLQueryParameter("input", "SomeInput!")], new Dictionary<string, object> { ["id"] = Leaf }, alias);
    }

    [Fact]
    public void For_AddsVariableWithCorrectName()
    {
        var fragment = CreateFragment("createZone");
        var variables = new GraphQLVariables()
            .For(fragment, "input", new { Name = "test" })
            .Build();

        var dict = (IDictionary<string, object>)variables;
        dict.Should().ContainKey("createZoneInput");
    }

    [Fact]
    public void Build_ReturnsExpandoWithAllVariables()
    {
        var fragment = CreateFragment("myQuery");
        var variables = new GraphQLVariables()
            .For(fragment, "pagination", new { Page = 1 })
            .For(fragment, "filters", new { IsActive = true })
            .Build();

        var dict = (IDictionary<string, object>)variables;
        dict.Should().ContainKey("myQueryPagination");
        dict.Should().ContainKey("myQueryFilters");
    }

    [Fact]
    public void For_FragmentWithAlias_UsesAliasForName()
    {
        var fragment = CreateFragment("createZone", alias: "ZoneResponse");
        var variables = new GraphQLVariables()
            .For(fragment, "input", "value")
            .Build();

        var dict = (IDictionary<string, object>)variables;
        dict.Should().ContainKey("zoneResponseInput");
        dict.Should().NotContainKey("createZoneInput");
    }

    [Fact]
    public void For_FragmentWithoutAlias_UsesNameForName()
    {
        var fragment = CreateFragment("createZone");
        var variables = new GraphQLVariables()
            .For(fragment, "input", "value")
            .Build();

        var dict = (IDictionary<string, object>)variables;
        dict.Should().ContainKey("createZoneInput");
    }

    [Fact]
    public void For_FluentChaining_ReturnsItself()
    {
        var fragment = CreateFragment("q");

        var result = new GraphQLVariables()
            .For(fragment, "a", 1)
            .For(fragment, "b", 2);

        result.Should().BeOfType<GraphQLVariables>();
    }
}
