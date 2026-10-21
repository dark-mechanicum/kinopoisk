using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Api;
using Jellyfin.Plugin.Kinopoisk.Configuration;
using Jellyfin.Plugin.Kinopoisk.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Api;

/// <summary>
/// Retry and failure classification of the transport (KP-02, KP-06), over a scripted HTTP handler.
/// </summary>
public sealed class KinopoiskTransportRetryTests : IDisposable
{
    private const string FilmBody = """{"data":{"film":{"id":301}}}""";

    private readonly QueuedHttpMessageHandler _handler = new();
    private readonly KinopoiskTransport _transport;
    private readonly CancellationToken _token = TestContext.Current.CancellationToken;

    public KinopoiskTransportRetryTests()
    {
        _transport = new KinopoiskTransport(new StubHttpClientFactory(_handler), new PluginConfiguration(), NullLogger<KinopoiskTransport>.Instance);
    }

    [Fact]
    public async Task QueryAsync_TooManyRequestsThenSuccess_Retries()
    {
        _handler
            .Respond(HttpStatusCode.TooManyRequests, configure: r => r.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero))
            .Respond(HttpStatusCode.OK, FilmBody);

        var film = await QueryFilmAsync(_token);

        Assert.Equal(301, film.Id);
        Assert.Equal(2, _handler.Calls);
    }

    [Fact]
    public async Task GetAsync_NotFound_FailsAtOnceWithNotFoundKind()
    {
        _handler.Respond(HttpStatusCode.NotFound, "Not Found");

        var ex = await Assert.ThrowsAsync<KinopoiskApiException>(() => _transport.GetAsync<Kinopoisk.Api.Models.FilmPerson>("person/1/", _token));

        Assert.Equal(KinopoiskErrorKind.NotFound, ex.Kind);
        Assert.Equal(1, _handler.Calls);
    }

    [Fact]
    public async Task QueryAsync_ServiceUnavailableEveryTime_GivesUpAfterThreeAttemptsAsTransient()
    {
        _handler
            .Respond(HttpStatusCode.ServiceUnavailable)
            .Respond(HttpStatusCode.ServiceUnavailable)
            .Respond(HttpStatusCode.ServiceUnavailable);

        var ex = await Assert.ThrowsAsync<KinopoiskApiException>(() => QueryFilmAsync(_token));

        Assert.Equal(KinopoiskErrorKind.Transient, ex.Kind);
        Assert.Equal(3, _handler.Calls);
    }

    [Fact]
    public async Task QueryAsync_BadRequest_IsNotRetried()
    {
        _handler.Respond(HttpStatusCode.BadRequest, """{"errors":[{"message":"the query is not allowed"}]}""");

        var ex = await Assert.ThrowsAsync<KinopoiskApiException>(() => QueryFilmAsync(_token));

        Assert.Equal(KinopoiskErrorKind.Contract, ex.Kind);
        Assert.Equal(1, _handler.Calls);
    }

    [Fact]
    public async Task QueryAsync_NetworkErrorThenSuccess_Retries()
    {
        _handler.Throw(new HttpRequestException("Resource temporarily unavailable")).Respond(HttpStatusCode.OK, FilmBody);

        var film = await QueryFilmAsync(_token);

        Assert.Equal(301, film.Id);
        Assert.Equal(2, _handler.Calls);
    }

    [Fact]
    public async Task QueryAsync_TimeoutThenSuccess_Retries()
    {
        // HttpClient reports its own timeout as a TaskCanceledException while the caller's token is still live.
        _handler.Throw(new TaskCanceledException("timeout")).Respond(HttpStatusCode.OK, FilmBody);

        var film = await QueryFilmAsync(_token);

        Assert.Equal(301, film.Id);
        Assert.Equal(2, _handler.Calls);
    }

    [Fact]
    public async Task QueryAsync_NetworkErrorEveryTime_FailsAsTransient()
    {
        _handler
            .Throw(new HttpRequestException("down"))
            .Throw(new HttpRequestException("down"))
            .Throw(new HttpRequestException("down"));

        var ex = await Assert.ThrowsAsync<KinopoiskApiException>(() => QueryFilmAsync(_token));

        Assert.Equal(KinopoiskErrorKind.Transient, ex.Kind);
        Assert.IsType<HttpRequestException>(ex.InnerException);
    }

    [Fact]
    public async Task QueryAsync_CallerCancelled_IsNotRetried()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _handler.Respond(HttpStatusCode.OK, FilmBody);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => QueryFilmAsync(cancellation.Token));

        Assert.True(_handler.Calls <= 1);
    }

    [Fact]
    public async Task QueryAsync_GraphQlErrorsWithStatusOk_AreNotRetried()
    {
        _handler.Respond(
            HttpStatusCode.OK,
            """{"errors":[{"message":"There is no film with id 1","path":["film"],"extensions":{"code":"NotFoundError"}}],"data":{"film":null}}""");

        var ex = await Assert.ThrowsAsync<KinopoiskApiException>(() => QueryFilmAsync(_token));

        Assert.Equal(KinopoiskErrorKind.NotFound, ex.Kind);
        Assert.Equal(1, _handler.Calls);
    }

    public void Dispose()
    {
        _transport.Dispose();
        _handler.Dispose();
    }

    private Task<Kinopoisk.Api.Models.FilmInfo> QueryFilmAsync(CancellationToken cancellationToken)
        => _transport.QueryAsync<Kinopoisk.Api.Models.FilmInfo>("FilmBaseInfo", new { filmId = 301 }, "data.film", cancellationToken);
}
