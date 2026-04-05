using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Interfaces;
using FluentAssertions;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace NetErp.Tests.Cache;

public class CacheBatchLoaderTests
{
    private readonly IGraphQLClient _client;

    public CacheBatchLoaderTests()
    {
        _client = Substitute.For<IGraphQLClient>();
    }

    private static IBatchLoadableCache CreateMockCache(string fragmentName, bool isInitialized)
    {
        var cache = Substitute.For<IBatchLoadableCache>();
        cache.IsInitialized.Returns(isInitialized);

        var fields = new Dictionary<string, object>
        {
            ["id"] = GraphQLQueryFragment.mapStringDynamicEmptyNode,
            ["name"] = GraphQLQueryFragment.mapStringDynamicEmptyNode
        };
        var param = new GraphQLQueryParameter("pagination", "Pagination");
        var fragment = new GraphQLQueryFragment(fragmentName, [param], fields);

        cache.LoadFragment.Returns(fragment);

        return cache;
    }

    [Fact]
    public async Task LoadAsync_AllInitialized_DoesNothing()
    {
        var cache1 = CreateMockCache("query1", isInitialized: true);
        var cache2 = CreateMockCache("query2", isInitialized: true);

        await CacheBatchLoader.LoadAsync(_client, CancellationToken.None, cache1, cache2);

        await _client.DidNotReceiveWithAnyArgs().ExecuteQueryAsync<JObject>(default!, default!, default);
    }

    [Fact]
    public async Task LoadAsync_MultiplePending_CombinesIntoSingleQuery()
    {
        var cache1 = CreateMockCache("zonesPage", isInitialized: false);
        var cache2 = CreateMockCache("countriesPage", isInitialized: false);

        var response = new JObject
        {
            ["zonesPage"] = JToken.FromObject(new { entries = new[] { new { id = 1, name = "Z1" } } }),
            ["countriesPage"] = JToken.FromObject(new { entries = new[] { new { id = 1, name = "C1" } } })
        };
        _client.ExecuteQueryAsync<JObject>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(response);

        await CacheBatchLoader.LoadAsync(_client, CancellationToken.None, cache1, cache2);

        // Should have called ExecuteQueryAsync exactly once (combined query)
        await _client.Received(1).ExecuteQueryAsync<JObject>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadAsync_RoutesResponseToCorrectCache()
    {
        var cache1 = CreateMockCache("zonesPage", isInitialized: false);
        var cache2 = CreateMockCache("countriesPage", isInitialized: false);

        var response = new JObject
        {
            ["zonesPage"] = JToken.FromObject(new { data = "zones" }),
            ["countriesPage"] = JToken.FromObject(new { data = "countries" })
        };
        _client.ExecuteQueryAsync<JObject>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(response);

        await CacheBatchLoader.LoadAsync(_client, CancellationToken.None, cache1, cache2);

        cache1.Received(1).PopulateFromBatchResponse(Arg.Is<JToken>(t => t.ToString().Contains("zones")));
        cache2.Received(1).PopulateFromBatchResponse(Arg.Is<JToken>(t => t.ToString().Contains("countries")));
    }

    [Fact]
    public async Task LoadAsync_NoPending_DoesNotCallClient()
    {
        // All initialized → no API call at all
        var cache1 = CreateMockCache("zonesPage", isInitialized: true);

        await CacheBatchLoader.LoadAsync(_client, CancellationToken.None, cache1);

        await _client.DidNotReceiveWithAnyArgs().ExecuteQueryAsync<JObject>(default!, default!, default);
        cache1.DidNotReceive().PopulateFromBatchResponse(Arg.Any<JToken>());
    }
}
