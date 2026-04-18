using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Common.Interfaces;
using FluentAssertions;
using Models.Treasury;
using NetErp.Helpers.Cache;
using NSubstitute;
using Xunit;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.Cache;

public class FranchiseCacheTests
{
    private readonly IRepository<FranchiseGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly FranchiseCache _cache;

    public FranchiseCacheTests()
    {
        _service = Substitute.For<IRepository<FranchiseGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _cache = new FranchiseCache(_service, _eventAggregator);
    }

    private static FranchiseGraphQLModel Build(int id, string name) =>
        new() { Id = id, Name = name, Type = "CREDIT_CARD" };

    [Fact]
    public void NewCache_IsNotInitialized()
    {
        _cache.IsInitialized.Should().BeFalse();
        _cache.Items.Should().BeEmpty();
    }

    [Fact]
    public void Add_Update_Remove_RoundTrip()
    {
        _cache.Add(Build(1, "Visa"));
        _cache.Items.Should().HaveCount(1);

        _cache.Update(Build(1, "Visa International"));
        _cache.Items.First().Name.Should().Be("Visa International");

        _cache.Remove(1);
        _cache.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task EnsureLoadedAsync_LoadsFromService_MarksInitialized()
    {
        PageType<FranchiseGraphQLModel> page = new()
        {
            Entries = new ObservableCollection<FranchiseGraphQLModel> { Build(1, "Visa"), Build(2, "Mastercard") }
        };
        _service.GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(page);

        await _cache.EnsureLoadedAsync();

        _cache.IsInitialized.Should().BeTrue();
        _cache.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_CreateUpdateDelete_MaintainsCollection()
    {
        FranchiseCreateMessage createMsg = new()
        {
            CreatedFranchise = new UpsertResponseType<FranchiseGraphQLModel> { Entity = Build(1, "Visa") }
        };
        await _cache.HandleAsync(createMsg, CancellationToken.None);
        _cache.Items.Should().HaveCount(1);

        FranchiseUpdateMessage updateMsg = new()
        {
            UpdatedFranchise = new UpsertResponseType<FranchiseGraphQLModel> { Entity = Build(1, "Visa Updated") }
        };
        await _cache.HandleAsync(updateMsg, CancellationToken.None);
        _cache.Items.First().Name.Should().Be("Visa Updated");

        FranchiseDeleteMessage deleteMsg = new() { DeletedFranchise = new DeleteResponseType { DeletedId = 1 } };
        await _cache.HandleAsync(deleteMsg, CancellationToken.None);
        _cache.Items.Should().BeEmpty();
    }
}
