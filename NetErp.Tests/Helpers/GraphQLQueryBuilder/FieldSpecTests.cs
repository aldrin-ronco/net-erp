using System.Collections.Generic;
using FluentAssertions;
using NetErp.Helpers.GraphQLQueryBuilder;
using Xunit;

namespace NetErp.Tests.Helpers.GraphQLQueryBuilder;

public class FieldSpecTests
{
    // Test DTOs
    private class TestModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public TestNested Nested { get; set; } = new();
        public IEnumerable<TestItem> Items { get; set; } = [];
    }

    private class TestNested
    {
        public string Value { get; set; } = "";
        public int Code { get; set; }
    }

    private class TestItem
    {
        public int ItemId { get; set; }
        public string Label { get; set; } = "";
    }

    [Fact]
    public void DefaultCamelCase_LowercasesFirstChar()
    {
        FieldSpec<TestModel>.DefaultCamelCase("Name").Should().Be("name");
        FieldSpec<TestModel>.DefaultCamelCase("Id").Should().Be("id");
    }

    [Fact]
    public void DefaultCamelCase_AlreadyLowercase_NoChange()
    {
        FieldSpec<TestModel>.DefaultCamelCase("name").Should().Be("name");
    }

    [Fact]
    public void DefaultCamelCase_EmptyString_ReturnsEmpty()
    {
        FieldSpec<TestModel>.DefaultCamelCase("").Should().BeEmpty();
    }

    [Fact]
    public void DefaultCamelCase_SingleChar_LowercasesIt()
    {
        FieldSpec<TestModel>.DefaultCamelCase("N").Should().Be("n");
    }

    [Fact]
    public void Field_DefaultCamelCase_ProducesCorrectKey()
    {
        var map = FieldSpec<TestModel>.Create()
            .Field(x => x.Name)
            .Build();

        map.Should().ContainKey("name");
    }

    [Fact]
    public void Field_OverrideName_UsesOverride()
    {
        var map = FieldSpec<TestModel>.Create()
            .Field(x => x.Name, overrideName: "customName")
            .Build();

        map.Should().ContainKey("customName");
        map.Should().NotContainKey("name");
    }

    [Fact]
    public void Field_WithAlias_ProducesAliasColonName()
    {
        var map = FieldSpec<TestModel>.Create()
            .Field(x => x.Name, alias: "myAlias")
            .Build();

        map.Should().ContainKey("myAlias:name");
    }

    [Fact]
    public void Select_CreatesNestedDictionary()
    {
        var map = FieldSpec<TestModel>.Create()
            .Select(x => x.Nested, n => n
                .Field(y => y.Value)
                .Field(y => y.Code))
            .Build();

        map.Should().ContainKey("nested");
        var nested = map["nested"] as Dictionary<string, object>;
        nested.Should().NotBeNull();
        nested.Should().ContainKey("value");
        nested.Should().ContainKey("code");
    }

    [Fact]
    public void SelectList_CreatesNestedDictionary()
    {
        var map = FieldSpec<TestModel>.Create()
            .SelectList(x => x.Items, item => item
                .Field(i => i.ItemId)
                .Field(i => i.Label))
            .Build();

        map.Should().ContainKey("items");
        var nested = map["items"] as Dictionary<string, object>;
        nested.Should().NotBeNull();
        nested.Should().ContainKey("itemId");
        nested.Should().ContainKey("label");
    }

    [Fact]
    public void Build_MultipleFields_ReturnsPopulatedDictionary()
    {
        var map = FieldSpec<TestModel>.Create()
            .Field(x => x.Id)
            .Field(x => x.Name)
            .Build();

        map.Should().HaveCount(2);
        map.Should().ContainKey("id");
        map.Should().ContainKey("name");
    }

    [Fact]
    public void Field_LeafValue_IsEmptyDictionary()
    {
        var map = FieldSpec<TestModel>.Create()
            .Field(x => x.Id)
            .Build();

        map["id"].Should().BeOfType<Dictionary<string, object>>();
        ((Dictionary<string, object>)map["id"]).Should().BeEmpty();
    }

    [Fact]
    public void Create_CustomFormatter_AppliesIt()
    {
        var map = FieldSpec<TestModel>.Create(s => s.ToUpper())
            .Field(x => x.Name)
            .Build();

        map.Should().ContainKey("NAME");
    }
}
