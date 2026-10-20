using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.Providers;

/// <summary>
/// Series artwork from Kinopoisk.
/// </summary>
public sealed class SeriesImageProvider : KinopoiskImageProvider<Series>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeriesImageProvider"/> class.
    /// </summary>
    /// <param name="client">The Kinopoisk client.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public SeriesImageProvider(IKinopoiskClient client, IHttpClientFactory httpClientFactory, ILogger<SeriesImageProvider> logger)
        : base(client, httpClientFactory, logger)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<ImageType> GetSupportedImages(BaseItem item) => [ImageType.Primary, ImageType.Backdrop, ImageType.Logo];

    /// <inheritdoc />
    protected override async Task<IEnumerable<(ImageType Type, string? Url)>> CollectAsync(
        BaseItem item, int kinopoiskId, CancellationToken cancellationToken)
    {
        var series = await Client.GetSeriesAsync(kinopoiskId, cancellationToken).ConfigureAwait(false);
        return await FilmImageCollector.CollectAsync(Client, series, cancellationToken).ConfigureAwait(false);
    }
}
