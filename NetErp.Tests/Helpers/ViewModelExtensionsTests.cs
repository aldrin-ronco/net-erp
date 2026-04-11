using System;
using System.Collections.ObjectModel;
using System.Linq;
using Common.Helpers;
using FluentAssertions;
using Xunit;

namespace NetErp.Tests.Helpers;

[Collection(SanitizerRegistryCollection.Name)]
public class ViewModelExtensionsTests : IDisposable
{
    public void Dispose()
    {
        SanitizerRegistry.ResetForTesting();
    }

    [Fact]
    public void TrackChange_CreatesTrackerImplicitly()
    {
        var vm = new object();

        vm.TrackChange("Name", "value");

        vm.HasChanges().Should().BeTrue();
    }

    [Fact]
    public void SeedValue_ThenTrackChange_SameValue_NoChange()
    {
        var vm = new object();
        vm.SeedValue("Name", "Hello");

        vm.TrackChange("Name", "Hello");

        vm.HasChanges().Should().BeFalse();
    }

    [Fact]
    public void SeedValue_ThenTrackChange_DifferentValue_HasChange()
    {
        var vm = new object();
        vm.SeedValue("Name", "Hello");

        vm.TrackChange("Name", "World");

        vm.HasChanges().Should().BeTrue();
    }

    [Fact]
    public void AcceptChanges_ClearsChanges()
    {
        var vm = new object();
        vm.TrackChange("Name", "value");

        vm.AcceptChanges();

        vm.HasChanges().Should().BeFalse();
    }

    [Fact]
    public void ClearSeeds_ClearsSeeds()
    {
        var vm = new object();
        vm.SeedValue("Name", "Hello");

        vm.ClearSeeds();

        var tracker = vm.GetInternalTracker();
        tracker!.SeedValues.Should().BeEmpty();
    }

    [Fact]
    public void GetChangedProperties_ReturnsTrackedProperties()
    {
        var vm = new object();
        vm.TrackChange("A", 1);
        vm.TrackChange("B", 2);

        var changed = vm.GetChangedProperties().ToList();

        changed.Should().Contain("A");
        changed.Should().Contain("B");
    }

    [Fact]
    public void HasChanges_NoTracker_ReturnsFalse()
    {
        var vm = new object();

        vm.HasChanges().Should().BeFalse();
    }

    [Fact]
    public void TrackChange_WithSanitizer_SanitizesBeforeTracking()
    {
        // Register a string trimmer
        SanitizerRegistry.RegisterType<string>(s => s?.Trim());

        var vm = new object();
        vm.SeedValue("Name", "hello");

        // Track " hello " — after trim it becomes "hello" which matches seed
        vm.TrackChange("Name", " hello ");

        vm.HasChanges().Should().BeFalse("sanitized value matches seed");
    }

    [Fact]
    public void TrackChange_ObservableCollection_ObservesCollectionChanges()
    {
        var vm = new object();
        var collection = new ObservableCollection<int>();

        vm.TrackChange("Items", collection);
        vm.AcceptChanges(); // clear the initial track

        collection.Add(42);

        vm.HasChanges().Should().BeTrue();
        vm.GetChangedProperties().Should().Contain("Items");
    }

    [Fact]
    public void TrackChange_NullAfterCollection_UnsubscribesCollection()
    {
        var vm = new object();
        var collection = new ObservableCollection<int>();

        vm.TrackChange("Items", collection);
        vm.AcceptChanges();

        // Now set to null — should unsubscribe
        vm.TrackChange("Items", null);
        vm.AcceptChanges();

        collection.Add(42);

        // "Items" should only have the null change, not the collection change
        // After AcceptChanges, modifying old collection should not trigger
        vm.HasChanges().Should().BeFalse();
    }

    [Fact]
    public void NullViewModel_ThrowsArgumentNullException()
    {
        object? vm = null;

        var act = () => vm!.TrackChange("Name");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SeedValue_NullViewModel_ThrowsArgumentNullException()
    {
        object? vm = null;

        var act = () => vm!.SeedValue("Name", "value");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HasChanges_NullViewModel_ThrowsArgumentNullException()
    {
        object? vm = null;

        var act = () => vm!.HasChanges();

        act.Should().Throw<ArgumentNullException>();
    }
}
