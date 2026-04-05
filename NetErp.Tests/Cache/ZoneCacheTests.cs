using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Common.Interfaces;
using FluentAssertions;
using Models.Billing;
using NetErp.Helpers.Cache;
using NSubstitute;
using Xunit;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.Cache;

public class ZoneCacheTests
{
    private readonly IRepository<ZoneGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly ZoneCache _cache;

    public ZoneCacheTests()
    {
        _service = Substitute.For<IRepository<ZoneGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _cache = new ZoneCache(_service, _eventAggregator);
    }

    [Fact]
    public void NewCache_IsNotInitialized()
    {
        _cache.IsInitialized.Should().BeFalse();
        _cache.Items.Should().BeEmpty();
    }

    [Fact]
    public void Add_AddsItem_ToItems()
    {
        var zone = new ZoneGraphQLModel { Id = 1, Name = "Norte" };

        _cache.Add(zone);

        _cache.Items.Should().HaveCount(1);
        _cache.Items.First().Name.Should().Be("Norte");
    }

    [Fact]
    public void Add_DuplicateId_DoesNotAddTwice()
    {
        var zone1 = new ZoneGraphQLModel { Id = 1, Name = "Norte" };
        var zone2 = new ZoneGraphQLModel { Id = 1, Name = "Norte Updated" };

        _cache.Add(zone1);
        _cache.Add(zone2);

        _cache.Items.Should().HaveCount(1);
        _cache.Items.First().Name.Should().Be("Norte");
    }

    [Fact]
    public void Update_ExistingItem_ReplacesIt()
    {
        var zone = new ZoneGraphQLModel { Id = 1, Name = "Norte" };
        _cache.Add(zone);

        var updated = new ZoneGraphQLModel { Id = 1, Name = "Norte Actualizado" };
        _cache.Update(updated);

        _cache.Items.Should().HaveCount(1);
        _cache.Items.First().Name.Should().Be("Norte Actualizado");
    }

    [Fact]
    public void Update_NonExistingItem_DoesNothing()
    {
        var zone = new ZoneGraphQLModel { Id = 999, Name = "Ghost" };

        var act = () => _cache.Update(zone);

        act.Should().NotThrow();
        _cache.Items.Should().BeEmpty();
    }

    [Fact]
    public void Remove_ExistingItem_RemovesIt()
    {
        var zone = new ZoneGraphQLModel { Id = 1, Name = "Norte" };
        _cache.Add(zone);

        _cache.Remove(1);

        _cache.Items.Should().BeEmpty();
    }

    [Fact]
    public void Remove_NonExistingId_DoesNothing()
    {
        var act = () => _cache.Remove(999);

        act.Should().NotThrow();
    }

    [Fact]
    public void Clear_ResetsInitializedAndItems()
    {
        _cache.Add(new ZoneGraphQLModel { Id = 1, Name = "Norte" });

        _cache.Clear();

        _cache.Items.Should().BeEmpty();
        _cache.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ZoneCreateMessage_AddsEntity()
    {
        var message = new ZoneCreateMessage
        {
            CreatedZone = new UpsertResponseType<ZoneGraphQLModel>
            {
                Entity = new ZoneGraphQLModel { Id = 1, Name = "Nueva" }
            }
        };

        await _cache.HandleAsync(message, CancellationToken.None);

        _cache.Items.Should().HaveCount(1);
        _cache.Items.First().Name.Should().Be("Nueva");
    }

    [Fact]
    public async Task HandleAsync_ZoneUpdateMessage_UpdatesExisting()
    {
        _cache.Add(new ZoneGraphQLModel { Id = 1, Name = "Original" });
        var message = new ZoneUpdateMessage
        {
            UpdatedZone = new UpsertResponseType<ZoneGraphQLModel>
            {
                Entity = new ZoneGraphQLModel { Id = 1, Name = "Actualizada" }
            }
        };

        await _cache.HandleAsync(message, CancellationToken.None);

        _cache.Items.First().Name.Should().Be("Actualizada");
    }

    [Fact]
    public async Task HandleAsync_ZoneUpdateMessage_NonExisting_AddsEntity()
    {
        var message = new ZoneUpdateMessage
        {
            UpdatedZone = new UpsertResponseType<ZoneGraphQLModel>
            {
                Entity = new ZoneGraphQLModel { Id = 5, Name = "Nueva via Update" }
            }
        };

        await _cache.HandleAsync(message, CancellationToken.None);

        _cache.Items.Should().HaveCount(1);
        _cache.Items.First().Name.Should().Be("Nueva via Update");
    }

    [Fact]
    public async Task HandleAsync_ZoneDeleteMessage_RemovesEntity()
    {
        _cache.Add(new ZoneGraphQLModel { Id = 1, Name = "Norte" });
        var message = new ZoneDeleteMessage
        {
            DeletedZone = new DeleteResponseType { DeletedId = 1 }
        };

        await _cache.HandleAsync(message, CancellationToken.None);

        _cache.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ZoneDeleteMessage_ZeroId_DoesNothing()
    {
        _cache.Add(new ZoneGraphQLModel { Id = 1, Name = "Norte" });
        var message = new ZoneDeleteMessage
        {
            DeletedZone = new DeleteResponseType { DeletedId = 0 }
        };

        await _cache.HandleAsync(message, CancellationToken.None);

        _cache.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task EnsureLoadedAsync_LoadsFromService_MarksInitialized()
    {
        var page = new PageType<ZoneGraphQLModel>
        {
            Entries = new ObservableCollection<ZoneGraphQLModel>
            {
                new() { Id = 1, Name = "Norte" },
                new() { Id = 2, Name = "Sur" }
            }
        };
        _service.GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(page);

        await _cache.EnsureLoadedAsync();

        _cache.IsInitialized.Should().BeTrue();
        _cache.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task EnsureLoadedAsync_AlreadyInitialized_DoesNotCallService()
    {
        var page = new PageType<ZoneGraphQLModel>
        {
            Entries = new ObservableCollection<ZoneGraphQLModel> { new() { Id = 1, Name = "Norte" } }
        };
        _service.GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(page);

        await _cache.EnsureLoadedAsync();
        await _cache.EnsureLoadedAsync(); // second call

        await _service.Received(1).GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
