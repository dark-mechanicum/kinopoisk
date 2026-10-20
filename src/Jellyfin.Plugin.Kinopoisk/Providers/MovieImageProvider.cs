using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.Providers;

/// <summary>
/// Movie artwork from Kinopoisk.
/// </summary>
public sealed class MovieImageProvider : KinopoiskImageProvider<Movie>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MovieImageProvider"/> class.
    /// </summary>
    /// <param name="client">The Kinopoisk client.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public MovieImageProvider(IKinopoiskClient client, IHttpClientFactory httpClientFactory, ILogger<MovieImageProvider> logger)
        : base(client, httpClientFactory, logger)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<ImageType> GetSupportedImages(BaseItem item) => [ImageType.Primary, ImageType.Backdrop, ImageType.Logo];

    /// <inheritdoc />
    protected override async Task<IEnumerable<(ImageType Type, string? Url)>> CollectAsync(
        BaseItem item, int kinopoiskId, CancellationToken cancellationToken)
    {
        var film = await Client.GetFilmAsync(kinopoiskId, cancellationToken).ConfigureAwait(false);
        return await FilmImageCollector.CollectAsync(Client, film, cancellationToken).ConfigureAwait(false);
    }
}
