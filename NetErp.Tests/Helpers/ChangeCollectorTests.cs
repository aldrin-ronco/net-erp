using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Common.Helpers;
using FluentAssertions;
using NetErp.Tests.TestDoubles;
using Xunit;

namespace NetErp.Tests.Helpers;

public class ChangeCollectorTests : IDisposable
{
    public void Dispose()
    {
        SanitizerRegistry.ResetForTesting();
    }

    [Fact]
    public void CollectChanges_NoTracker_ReturnsEmptyExpando()
    {
        var vm = new FakeViewModelSimple { Name = "test" };

        var result = ChangeCollector.CollectChanges(vm);

        var dict = (IDictionary<string, object?>)result;
        dict.Should().BeEmpty();
    }

    [Fact]
    public void CollectChanges_TrackedProperties_AppearInExpando()
    {
        var vm = new FakeViewModelSimple { Name = "test" };
        vm.SeedValue(nameof(vm.Name), "");
        vm.TrackChange(nameof(vm.Name), "test");

        var result = ChangeCollector.CollectChanges(vm);

        var dict = (IDictionary<string, object?>)result;
        dict.Should().ContainKey("Name");
        dict["Name"].Should().Be("test");
    }

    [Fact]
    public void CollectChanges_WithPrefix_PrependsPrefix()
    {
        var vm = new FakeViewModelSimple { Name = "test" };
        vm.SeedValue(nameof(vm.Name), "");
        vm.TrackChange(nameof(vm.Name), "test");

        var result = ChangeCollector.CollectChanges(vm, prefix: "updateData");

        var dict = (IDictionary<string, object?>)result;
        dict.Should().ContainKey("updateData");
        var nested = (IDictionary<string, object?>)dict["updateData"]!;
        nested.Should().ContainKey("Name");
    }

    [Fact]
    public void CollectChanges_ExpandoPathAttribute_UsesCustomPath()
    {
        var vm = new FakeViewModelWithAttributes { Notes = "my notes" };
        vm.SeedValue(nameof(vm.Notes), "");
        vm.TrackChange(nameof(vm.Notes), "my notes");

        var result = ChangeCollector.CollectChanges(vm);

        var dict = (IDictionary<string, object?>)result;
        dict.Should().ContainKey("data");
        var data = (IDictionary<string, object?>)dict["data"]!;
        data.Should().ContainKey("notes");
        data["notes"].Should().Be("my notes");
    }

    [Fact]
    public void CollectChanges_SerializeAsId_ExtractsIdFromComplex()
    {
        var vm = new FakeViewModelWithAttributes
        {
            Country = new FakeCountry { Id = 42, Name = "Colombia" }
        };
        vm.SeedValue(nameof(vm.Country), null);
        vm.TrackChange(nameof(vm.Country), vm.Country);

        var result = ChangeCollector.CollectChanges(vm);

        var dict = (IDictionary<string, object?>)result;
        dict.Should().ContainKey("data");
        var data = (IDictionary<string, object?>)dict["data"]!;
        data.Should().ContainKey("countryId");
        data["countryId"].Should().Be(42);
    }

    [Fact]
    public void CollectChanges_SeedsOnCreate_IncludesUnchangedSeeds()
    {
        var vm = new FakeViewModelSimple { IsActive = true, Age = 25 };
        vm.SeedValue(nameof(vm.IsActive), true);
        vm.SeedValue(nameof(vm.Age), 25);
        vm.AcceptChanges();

        // prefix contains "create" → seeds should be included
        var result = ChangeCollector.CollectChanges(vm, prefix: "createInput");

        var dict = (IDictionary<string, object?>)result;
        dict.Should().ContainKey("createInput");
        var nested = (IDictionary<string, object?>)dict["createInput"]!;
        nested.Should().ContainKey("IsActive");
        nested.Should().ContainKey("Age");
    }

    [Fact]
    public void CollectChanges_SeedsOnUpdate_ExcludesUnchangedSeeds()
    {
        var vm = new FakeViewModelSimple { IsActive = true, Age = 25 };
        vm.SeedValue(nameof(vm.IsActive), true);
        vm.SeedValue(nameof(vm.Age), 25);
        vm.AcceptChanges();

        // prefix does NOT contain "create" → seeds should be excluded
        var result = ChangeCollector.CollectChanges(vm, prefix: "updateData");

        var dict = (IDictionary<string, object?>)result;
        dict.Should().BeEmpty();
    }

    [Fact]
    public void CollectChanges_ExcludeProperties_OmitsThem()
    {
        var vm = new FakeViewModelSimple { Name = "test", IsActive = true };
        vm.SeedValue(nameof(vm.Name), "");
        vm.SeedValue(nameof(vm.IsActive), false);
        vm.TrackChange(nameof(vm.Name), "test");
        vm.TrackChange(nameof(vm.IsActive), true);

        var result = ChangeCollector.CollectChanges(vm, excludeProperties: [nameof(vm.IsActive)]);

        var dict = (IDictionary<string, object?>)result;
        dict.Should().ContainKey("Name");
        dict.Should().NotContainKey("IsActive");
    }

    [Fact]
    public void CollectChanges_EmptyStringSeed_SkippedOnCreate()
    {
        var vm = new FakeViewModelSimple { Name = "" };
        vm.SeedValue(nameof(vm.Name), "");
        vm.AcceptChanges();

        var result = ChangeCollector.CollectChanges(vm, prefix: "createInput");

        var dict = (IDictionary<string, object?>)result;
        // Empty string seed should be skipped
        if (dict.ContainsKey("createInput"))
        {
            var nested = (IDictionary<string, object?>)dict["createInput"]!;
            nested.Should().NotContainKey("Name");
        }
    }

    [Fact]
    public void CollectChanges_CollectionItemTransformer_TransformsEachItem()
    {
        var vm = new FakeViewModelWithAttributes { Tags = ["a", "b", "c"] };
        vm.SeedValue(nameof(vm.Tags), null);
        vm.TrackChange(nameof(vm.Tags), vm.Tags);

        var transformers = new Dictionary<string, Func<object?, object?>>
        {
            ["Tags"] = item => ((string?)item)?.ToUpper()
        };

        var result = ChangeCollector.CollectChanges(vm, null, transformers);

        var dict = (IDictionary<string, object?>)result;
        dict.Should().ContainKey("Tags");
        var tags = dict["Tags"] as List<object?>;
        tags.Should().NotBeNull();
        tags.Should().Contain("A");
        tags.Should().Contain("B");
        tags.Should().Contain("C");
    }

    [Fact]
    public void CollectChanges_SanitizesValues()
    {
        SanitizerRegistry.RegisterType<string>(s => s?.Trim());

        var vm = new FakeViewModelSimple { Name = "  hello  " };
        vm.SeedValue(nameof(vm.Name), "");
        vm.TrackChange(nameof(vm.Name), "  hello  ");

        var result = ChangeCollector.CollectChanges(vm);

        var dict = (IDictionary<string, object?>)result;
        dict.Should().ContainKey("Name");
        dict["Name"].Should().Be("hello");
    }
}
