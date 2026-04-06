using System.Collections.Generic;
using System.Dynamic;
using Common.Helpers;
using FluentAssertions;
using Xunit;

namespace NetErp.Tests.Helpers;

public class ExpandoHelperTests
{
    [Fact]
    public void SetNestedProperty_SingleLevel_SetsValue()
    {
        var expando = new ExpandoObject();

        ExpandoHelper.SetNestedProperty(expando, "name", "test");

        var dict = (IDictionary<string, object?>)expando;
        dict["name"].Should().Be("test");
    }

    [Fact]
    public void SetNestedProperty_TwoLevels_CreatesNestedExpando()
    {
        var expando = new ExpandoObject();

        ExpandoHelper.SetNestedProperty(expando, "a.b", 42);

        var dict = (IDictionary<string, object?>)expando;
        dict.Should().ContainKey("a");
        var nested = (IDictionary<string, object?>)dict["a"]!;
        nested["b"].Should().Be(42);
    }

    [Fact]
    public void SetNestedProperty_ThreeLevels_CreatesFullPath()
    {
        var expando = new ExpandoObject();

        ExpandoHelper.SetNestedProperty(expando, "a.b.c", true);

        var dict = (IDictionary<string, object?>)expando;
        var a = (IDictionary<string, object?>)dict["a"]!;
        var b = (IDictionary<string, object?>)a["b"]!;
        b["c"].Should().Be(true);
    }

    [Fact]
    public void SetNestedProperty_OverwritesExistingValue()
    {
        var expando = new ExpandoObject();
        ExpandoHelper.SetNestedProperty(expando, "a.b", "first");

        ExpandoHelper.SetNestedProperty(expando, "a.b", "second");

        var dict = (IDictionary<string, object?>)expando;
        var a = (IDictionary<string, object?>)dict["a"]!;
        a["b"].Should().Be("second");
    }

    [Fact]
    public void SetNestedProperty_PreservesExistingSiblings()
    {
        var expando = new ExpandoObject();
        ExpandoHelper.SetNestedProperty(expando, "a.b", 1);

        ExpandoHelper.SetNestedProperty(expando, "a.c", 2);

        var dict = (IDictionary<string, object?>)expando;
        var a = (IDictionary<string, object?>)dict["a"]!;
        a["b"].Should().Be(1);
        a["c"].Should().Be(2);
    }

    [Fact]
    public void SetNestedProperty_NullValue_SetsNull()
    {
        var expando = new ExpandoObject();

        ExpandoHelper.SetNestedProperty(expando, "key", null);

        var dict = (IDictionary<string, object?>)expando;
        dict.Should().ContainKey("key");
        dict["key"].Should().BeNull();
    }
}
