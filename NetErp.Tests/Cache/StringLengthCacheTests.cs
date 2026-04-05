using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Interfaces;
using FluentAssertions;
using Models.Billing;
using Models.Global;
using NetErp.Helpers.Cache;
using NSubstitute;
using Xunit;

namespace NetErp.Tests.Cache;

public class StringLengthCacheTests
{
    private readonly IRepository<EntityStringLengthsGraphQLModel> _repository;
    private readonly StringLengthCache _cache;

    public StringLengthCacheTests()
    {
        _repository = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _cache = new StringLengthCache(_repository);
    }

    [Fact]
    public void GetMaxLength_NotLoaded_ReturnsZero()
    {
        var result = _cache.GetMaxLength<ZoneGraphQLModel>(nameof(ZoneGraphQLModel.Name));

        result.Should().Be(0);
    }

    [Fact]
    public async Task EnsureEntitiesLoadedAsync_LoadsAndCachesLengths()
    {
        var response = new List<EntityStringLengthsGraphQLModel>
        {
            new()
            {
                Entity = "zone",
                Fields =
                [
                    new StringFieldLengthGraphQLModel { Column = "name", MaxLength = 100 }
                ]
            }
        };
        _repository.GetListAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<EntityStringLengthsGraphQLModel>>(response));

        await _cache.EnsureEntitiesLoadedAsync(typeof(ZoneGraphQLModel));

        _cache.GetMaxLength<ZoneGraphQLModel>("Name").Should().Be(100);
        _cache.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureEntitiesLoadedAsync_AlreadyLoaded_SkipsApiCall()
    {
        var response = new List<EntityStringLengthsGraphQLModel>
        {
            new()
            {
                Entity = "zone",
                Fields = [new StringFieldLengthGraphQLModel { Column = "name", MaxLength = 100 }]
            }
        };
        _repository.GetListAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<EntityStringLengthsGraphQLModel>>(response));

        await _cache.EnsureEntitiesLoadedAsync(typeof(ZoneGraphQLModel));
        await _cache.EnsureEntitiesLoadedAsync(typeof(ZoneGraphQLModel)); // second call

        await _repository.Received(1).GetListAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureEntitiesLoadedAsync_EmptyResponse_ThrowsStringLengthNotAvailable()
    {
        _repository.GetListAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<EntityStringLengthsGraphQLModel>>([]));

        var act = () => _cache.EnsureEntitiesLoadedAsync(typeof(ZoneGraphQLModel));

        await act.Should().ThrowAsync<StringLengthNotAvailableException>();
    }

    [Fact]
    public async Task EnsureEntitiesLoadedAsync_ZeroMaxLength_ThrowsInvalidOperation()
    {
        var response = new List<EntityStringLengthsGraphQLModel>
        {
            new()
            {
                Entity = "zone",
                Fields = [new StringFieldLengthGraphQLModel { Column = "name", MaxLength = 0 }]
            }
        };
        _repository.GetListAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<EntityStringLengthsGraphQLModel>>(response));

        var act = () => _cache.EnsureEntitiesLoadedAsync(typeof(ZoneGraphQLModel));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void GetMaxLength_UnknownProperty_ReturnsZero()
    {
        // Cache loaded but this property doesn't exist
        var result = _cache.GetMaxLength<ZoneGraphQLModel>("NonExistentProperty");

        result.Should().Be(0);
    }

    [Fact]
    public async Task Clear_ResetsAllState()
    {
        var response = new List<EntityStringLengthsGraphQLModel>
        {
            new()
            {
                Entity = "zone",
                Fields = [new StringFieldLengthGraphQLModel { Column = "name", MaxLength = 100 }]
            }
        };
        _repository.GetListAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<EntityStringLengthsGraphQLModel>>(response));

        await _cache.EnsureEntitiesLoadedAsync(typeof(ZoneGraphQLModel));
        _cache.Clear();

        _cache.IsInitialized.Should().BeFalse();
        _cache.GetMaxLength<ZoneGraphQLModel>("Name").Should().Be(0);
    }
}
