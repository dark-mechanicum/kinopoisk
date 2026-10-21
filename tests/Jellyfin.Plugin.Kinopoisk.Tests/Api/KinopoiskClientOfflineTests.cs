using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Api;
using Jellyfin.Plugin.Kinopoisk.Configuration;
using Jellyfin.Plugin.Kinopoisk.Tests.Fakes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Api;

/// <summary>
/// Search behaviour that live data cannot reproduce on demand (KP-05): what is sent, and a null suggestion list.
/// </summary>
public sealed class KinopoiskClientOfflineTests : IDisposable
{
    private const string NullTop = """{"data":{"suggest":{"top":null}}}""";
    private const string EmptyTop = """{"data":{"suggest":{"top":{"topResult":null,"movies":[],"persons":[]}}}}""";

    private readonly QueuedHttpMessageHandler _handler = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly KinopoiskTransport _transport;
    private readonly KinopoiskClient _client;
    private readonly CancellationToken _token = TestContext.Current.CancellationToken;

    public KinopoiskClientOfflineTests()
    {
        var options = new PluginConfiguration();
        _transport = new KinopoiskTransport(new StubHttpClientFactory(_handler), options, NullLogger<KinopoiskTransport>.Instance);
        _client = new KinopoiskClient(_transport, _cache, options);
    }

    [Fact]
    public async Task SearchFilmsAsync_NullSuggestTop_ReturnsNoResultsAndIsNotCached()
    {
        _handler.Respond(HttpStatusCode.OK, NullTop).Respond(HttpStatusCode.OK, NullTop);

        Assert.Empty(await _client.SearchFilmsAsync("Матрица", null, _token));
        Assert.Empty(await _client.SearchFilmsAsync("Матрица", null, _token));

        Assert.Equal(2, _handler.Calls);
    }

    [Fact]
    public async Task SearchPersonsAsync_SendsYandexCityId()
    {
        _handler.Respond(HttpStatusCode.OK, EmptyTop);

        await _client.SearchPersonsAsync("Киану Ривз", _token);

        Assert.Contains("\"yandexCityId\":213", _handler.RequestBodies[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task SearchPersonsAsync_EmptySuggestTop_IsCached()
    {
        _handler.Respond(HttpStatusCode.OK, EmptyTop);

        await _client.SearchPersonsAsync("Никто", _token);
        await _client.SearchPersonsAsync("Никто", _token);

        Assert.Equal(1, _handler.Calls);
    }

    public void Dispose()
    {
        _transport.Dispose();
        _cache.Dispose();
        _handler.Dispose();
    }
}
