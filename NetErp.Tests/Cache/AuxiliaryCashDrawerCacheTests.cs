using System.Collections.ObjectModel;
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

public class AuxiliaryCashDrawerCacheTests
{
    private readonly IRepository<CashDrawerGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly AuxiliaryCashDrawerCache _cache;

    public AuxiliaryCashDrawerCacheTests()
    {
        _service = Substitute.For<IRepository<CashDrawerGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _cache = new AuxiliaryCashDrawerCache(_service, _eventAggregator);
    }

    private static CashDrawerGraphQLModel BuildAuxiliary(int id, int parentId) =>
        new() { Id = id, Name = $"Aux{id}", IsPettyCash = false, Parent = new CashDrawerGraphQLModel { Id = parentId } };

    private static CashDrawerGraphQLModel BuildMajor(int id) =>
        new() { Id = id, Name = $"Major{id}", IsPettyCash = false, Parent = null };

    [Fact]
    public void NewCache_IsNotInitialized()
    {
        _cache.IsInitialized.Should().BeFalse();
        _cache.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task EnsureLoadedAsync_FiltersAuxiliariesOnly()
    {
        PageType<CashDrawerGraphQLModel> page = new()
        {
            Entries = new ObservableCollection<CashDrawerGraphQLModel>
            {
                BuildMajor(1),
                BuildAuxiliary(10, parentId: 1),
                BuildMajor(2),
                BuildAuxiliary(11, parentId: 2)
            }
        };
        _service.GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(page);

        await _cache.EnsureLoadedAsync();

        _cache.IsInitialized.Should().BeTrue();
        _cache.Items.Should().HaveCount(2);
        _cache.Items.Should().OnlyContain(x => x.Parent != null);
    }

    [Fact]
    public async Task HandleAsync_Create_AuxiliaryAdded()
    {
        TreasuryCashDrawerCreateMessage message = new()
        {
            CreatedCashDrawer = new UpsertResponseType<CashDrawerGraphQLModel> { Entity = BuildAuxiliary(5, parentId: 1) }
        };

        await _cache.HandleAsync(message, CancellationToken.None);

        _cache.Items.Should().ContainSingle(x => x.Id == 5);
    }

    [Fact]
    public async Task HandleAsync_Create_MajorIgnored()
    {
        TreasuryCashDrawerCreateMessage message = new()
        {
            CreatedCashDrawer = new UpsertResponseType<CashDrawerGraphQLModel> { Entity = BuildMajor(1) }
        };

        await _cache.HandleAsync(message, CancellationToken.None);

        _cache.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Update_ParentRemoved_RemovesFromCache()
    {
        _cache.Add(BuildAuxiliary(5, parentId: 1));

        TreasuryCashDrawerUpdateMessage message = new()
        {
            UpdatedCashDrawer = new UpsertResponseType<CashDrawerGraphQLModel> { Entity = BuildMajor(5) }
        };

        await _cache.HandleAsync(message, CancellationToken.None);

        _cache.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Delete_RemovesEntity()
    {
        _cache.Add(BuildAuxiliary(5, parentId: 1));
        TreasuryCashDrawerDeleteMessage message = new() { DeletedCashDrawer = new DeleteResponseType { DeletedId = 5 } };

        await _cache.HandleAsync(message, CancellationToken.None);

        _cache.Items.Should().BeEmpty();
    }
}
