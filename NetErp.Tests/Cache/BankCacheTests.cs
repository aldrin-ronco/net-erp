using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Common.Interfaces;
using FluentAssertions;
using Models.Books;
using Models.Treasury;
using NetErp.Helpers.Cache;
using NSubstitute;
using Xunit;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.Cache;

public class BankCacheTests
{
    private readonly IRepository<BankGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly BankCache _cache;

    public BankCacheTests()
    {
        _service = Substitute.For<IRepository<BankGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _cache = new BankCache(_service, _eventAggregator);
    }

    private static BankGraphQLModel Build(int id, string name) =>
        new() { Id = id, Code = id.ToString(), AccountingEntity = new AccountingEntityGraphQLModel { SearchName = name } };

    [Fact]
    public void NewCache_IsNotInitialized()
    {
        _cache.IsInitialized.Should().BeFalse();
        _cache.Items.Should().BeEmpty();
    }

    [Fact]
    public void Add_Update_Remove_RoundTrip()
    {
        _cache.Add(Build(1, "Bancolombia"));
        _cache.Items.Should().HaveCount(1);

        _cache.Add(Build(1, "Dup"));
        _cache.Items.Should().HaveCount(1);

        _cache.Update(Build(1, "Bancolombia S.A."));
        _cache.Items.First().AccountingEntity.SearchName.Should().Be("Bancolombia S.A.");

        _cache.Remove(1);
        _cache.Items.Should().BeEmpty();
    }

    [Fact]
    public void Clear_ResetsInitializedAndItems()
    {
        _cache.Add(Build(1, "X"));
        _cache.Clear();
        _cache.Items.Should().BeEmpty();
        _cache.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public async Task EnsureLoadedAsync_LoadsFromService_MarksInitialized()
    {
        PageType<BankGraphQLModel> page = new()
        {
            Entries = new ObservableCollection<BankGraphQLModel> { Build(1, "A"), Build(2, "B") }
        };
        _service.GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(page);

        await _cache.EnsureLoadedAsync();

        _cache.IsInitialized.Should().BeTrue();
        _cache.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task EnsureLoadedAsync_AlreadyInitialized_DoesNotCallService()
    {
        PageType<BankGraphQLModel> page = new() { Entries = new ObservableCollection<BankGraphQLModel> { Build(1, "A") } };
        _service.GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(page);

        await _cache.EnsureLoadedAsync();
        await _cache.EnsureLoadedAsync();

        await _service.Received(1).GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CreateMessage_AddsEntity()
    {
        BankCreateMessage message = new()
        {
            CreatedBank = new UpsertResponseType<BankGraphQLModel> { Entity = Build(1, "New") }
        };

        await _cache.HandleAsync(message, CancellationToken.None);

        _cache.Items.Should().ContainSingle(x => x.Id == 1);
    }

    [Fact]
    public async Task HandleAsync_UpdateMessage_UpdatesExisting()
    {
        _cache.Add(Build(1, "Original"));
        BankUpdateMessage message = new()
        {
            UpdatedBank = new UpsertResponseType<BankGraphQLModel> { Entity = Build(1, "Updated") }
        };

        await _cache.HandleAsync(message, CancellationToken.None);

        _cache.Items.First().AccountingEntity.SearchName.Should().Be("Updated");
    }

    [Fact]
    public async Task HandleAsync_DeleteMessage_RemovesEntity()
    {
        _cache.Add(Build(1, "X"));
        BankDeleteMessage message = new() { DeletedBank = new DeleteResponseType { DeletedId = 1 } };

        await _cache.HandleAsync(message, CancellationToken.None);

        _cache.Items.Should().BeEmpty();
    }
}
