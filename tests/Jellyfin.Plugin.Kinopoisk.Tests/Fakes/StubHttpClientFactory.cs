using System.Net.Http;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Fakes;

/// <summary>
/// Hands out clients over one fake handler, whatever the client name.
/// </summary>
public sealed class StubHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public StubHttpClientFactory(HttpMessageHandler handler)
    {
        _handler = handler;
    }

    public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
}
