using System.Collections.ObjectModel;
using System.Linq;
using Common.Helpers;
using FluentAssertions;
using Xunit;

namespace NetErp.Tests.Helpers;

public class ChangeTrackerTests
{
    private readonly ChangeTracker _tracker = new();

    [Fact]
    public void NewTracker_HasNoChanges()
    {
        _tracker.HasChanges.Should().BeFalse();
        _tracker.ChangedProperties.Should().BeEmpty();
    }

    [Fact]
    public void RegisterChange_MarksPropertyAsChanged()
    {
        _tracker.RegisterChange("Name");

        _tracker.HasChanges.Should().BeTrue();
        _tracker.ChangedProperties.Should().Contain("Name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterChange_NullOrWhitespace_IsIgnored(string? propertyName)
    {
        _tracker.RegisterChange(propertyName!);

        _tracker.HasChanges.Should().BeFalse();
    }

    [Fact]
    public void RegisterChange_DuplicateProperty_OnlyAppearsOnce()
    {
        _tracker.RegisterChange("Name");
        _tracker.RegisterChange("Name");

        _tracker.ChangedProperties.Count().Should().Be(1);
    }

    [Fact]
    public void Seed_ThenRegisterChange_SameValue_RemovesFromChanged()
    {
        _tracker.Seed("Name", "Hello");
        _tracker.RegisterChange("Name", "Hello");

        _tracker.HasChanges.Should().BeFalse();
        _tracker.ChangedProperties.Should().NotContain("Name");
    }

    [Fact]
    public void Seed_ThenRegisterChange_DifferentValue_MarksChanged()
    {
        _tracker.Seed("Name", "Hello");
        _tracker.RegisterChange("Name", "World");

        _tracker.HasChanges.Should().BeTrue();
        _tracker.ChangedProperties.Should().Contain("Name");
    }

    [Fact]
    public void Seed_ThenRegisterChange_SameValue_RemovesPreviousChange()
    {
        _tracker.Seed("Name", "Hello");
        _tracker.RegisterChange("Name", "World"); // marked as changed
        _tracker.RegisterChange("Name", "Hello"); // back to seed → removed

        _tracker.ChangedProperties.Should().NotContain("Name");
    }

    [Fact]
    public void AcceptChanges_ClearsChangedButKeepsSeeds()
    {
        _tracker.Seed("Name", "Hello");
        _tracker.RegisterChange("Name", "World");

        _tracker.AcceptChanges();

        _tracker.HasChanges.Should().BeFalse();
        _tracker.SeedValues.Should().ContainKey("Name");
        _tracker.SeedValues["Name"].Should().Be("Hello");
    }

    [Fact]
    public void ClearSeeds_RemovesSeedValues()
    {
        _tracker.Seed("Name", "Hello");
        _tracker.Seed("Age", 25);

        _tracker.ClearSeeds();

        _tracker.SeedValues.Should().BeEmpty();
    }

    [Fact]
    public void SeedValues_ReturnsReadOnlyDictionary()
    {
        _tracker.Seed("A", 1);
        _tracker.Seed("B", "two");

        _tracker.SeedValues.Should().HaveCount(2);
        _tracker.SeedValues["A"].Should().Be(1);
        _tracker.SeedValues["B"].Should().Be("two");
    }

    [Fact]
    public void ObserveCollection_CollectionChange_MarksPropertyChanged()
    {
        var collection = new ObservableCollection<string>();
        _tracker.ObserveCollection("Items", collection);

        collection.Add("item1");

        _tracker.HasChanges.Should().BeTrue();
        _tracker.ChangedProperties.Should().Contain("Items");
    }

    [Fact]
    public void ObserveCollection_NullCollection_Unsubscribes()
    {
        var collection = new ObservableCollection<string>();
        _tracker.ObserveCollection("Items", collection);

        _tracker.ObserveCollection("Items", null);
        collection.Add("item1");

        _tracker.HasChanges.Should().BeFalse();
    }

    [Fact]
    public void ObserveCollection_ReplacesExistingSubscription()
    {
        var collectionA = new ObservableCollection<string>();
        var collectionB = new ObservableCollection<string>();
        _tracker.ObserveCollection("Items", collectionA);

        _tracker.ObserveCollection("Items", collectionB);
        collectionA.Add("should not trigger");

        _tracker.HasChanges.Should().BeFalse();

        collectionB.Add("should trigger");
        _tracker.HasChanges.Should().BeTrue();
    }

    [Fact]
    public void ObserveCollection_RemoveItem_MarksChanged()
    {
        var collection = new ObservableCollection<string>(["a", "b"]);
        _tracker.ObserveCollection("Items", collection);

        collection.Remove("a");

        _tracker.ChangedProperties.Should().Contain("Items");
    }
}
