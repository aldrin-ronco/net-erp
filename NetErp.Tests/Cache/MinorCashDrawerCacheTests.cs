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

public class MinorCashDrawerCacheTests
{
    private readonly IRepository<CashDrawerGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly MinorCashDrawerCache _cache;

    public MinorCashDrawerCacheTests()
    {
        _service = Substitute.For<IRepository<CashDrawerGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _cache = new MinorCashDrawerCache(_service, _eventAggregator);
    }

    private static CashDrawerGraphQLModel BuildMinor(int id, string name) =>
        new() { Id = id, Name = name, IsPettyCash = true, Parent = null };

    private static CashDrawerGraphQLModel BuildMajor(int id, string name) =>
        new() { Id = id, Name = name, IsPettyCash = false, Parent = null };

    private static CashDrawerGraphQLModel BuildAuxiliary(int id, int parentId) =>
        new() { Id = id, Name = $"Aux{id}", IsPettyCash = false, Parent = new CashDrawerGraphQLModel { Id = parentId } };

    [Fact]
    public void NewCache_IsNotInitialized()
    {
        _cache.IsInitialized.Should().BeFalse();
        _cache.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task EnsureLoadedAsync_LoadsFromService()
    {
        PageType<CashDrawerGraphQLModel> page = new()
        {
            Entries = new ObservableCollection<CashDrawerGraphQLModel> { BuildMinor(1, "Caja menor 1") }
        };
        _service.GetPageAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(page);

        await _cache.EnsureLoadedAsync();

        _cache.IsInitialized.Should().BeTrue();
        _cache.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_Create_MinorAdded()
    {
        TreasuryCashDrawerCreateMessage message = new()
        {
            CreatedCashDrawer = new UpsertResponseType<CashDrawerGraphQLModel> { Entity = BuildMinor(1, "Nueva") }
        };

        await _cache.HandleAsync(message, CancellationToken.None);

        _cache.Items.Should().ContainSingle(x => x.Id == 1);
    }

    [Fact]
    public async Task HandleAsync_Create_MajorIgnored()
    {
        TreasuryCashDrawerCreateMessage message = new()
        {
            CreatedCashDrawer = new UpsertResponseType<CashDrawerGraphQLModel> { Entity = BuildMajor(1, "No soy menor") }
        };

        await _cache.HandleAsync(message, CancellationToken.None);

        _cache.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Create_AuxiliaryIgnored()
    {
        TreasuryCashDrawerCreateMessage message = new()
        {
            CreatedCashDrawer = new UpsertResponseType<CashDrawerGraphQLModel> { Entity = BuildAuxiliary(1, parentId: 99) }
        };

        await _cache.HandleAsync(message, CancellationToken.None);

        _cache.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Update_ToNonMinor_Removes()
    {
        _cache.Add(BuildMinor(1, "Original"));

        TreasuryCashDrawerUpdateMessage message = new()
        {
            UpdatedCashDrawer = new UpsertResponseType<CashDrawerGraphQLModel> { Entity = BuildMajor(1, "Ya no soy menor") }
        };

        await _cache.HandleAsync(message, CancellationToken.None);

        _cache.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Update_FromNonMinorToMinor_Adds()
    {
        TreasuryCashDrawerUpdateMessage message = new()
        {
            UpdatedCashDrawer = new UpsertResponseType<CashDrawerGraphQLModel> { Entity = BuildMinor(5, "Ahora sí") }
        };

        await _cache.HandleAsync(message, CancellationToken.None);

        _cache.Items.Should().ContainSingle(x => x.Id == 5);
    }

    [Fact]
    public async Task HandleAsync_Delete_RemovesEntity()
    {
        _cache.Add(BuildMinor(1, "X"));
        TreasuryCashDrawerDeleteMessage message = new() { DeletedCashDrawer = new DeleteResponseType { DeletedId = 1 } };

        await _cache.HandleAsync(message, CancellationToken.None);

        _cache.Items.Should().BeEmpty();
    }
}
