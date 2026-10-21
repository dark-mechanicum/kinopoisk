using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Common;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.Providers;

/// <summary>
/// Downloads Kinopoisk artwork for Jellyfin (<c>GetImageResponse</c> of every provider), with bounded timeouts,
/// the IPv4 preference and one retry of connection failures.
/// </summary>
public sealed class ImageDownloader
{
    /// <summary>Named HttpClient for image downloads.</summary>
    public const string ClientName = "Kinopoisk.Images";

    private const int MaxAttempts = 2;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpRetry _retry;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageDownloader"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public ImageDownloader(IHttpClientFactory httpClientFactory, ILogger<ImageDownloader> logger)
    {
        _httpClientFactory = httpClientFactory;
        _retry = new HttpRetry(MaxAttempts, logger);
    }

    /// <summary>
    /// Downloads an image.
    /// </summary>
    /// <param name="url">The image url.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response; Jellyfin checks the status itself.</returns>
    public Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken)
        => _retry.SendAsync(
            _httpClientFactory.CreateClient(ClientName),
            () => new HttpRequestMessage(HttpMethod.Get, url),
            cancellationToken);
}
