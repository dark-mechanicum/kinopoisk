using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Api;
using Jellyfin.Plugin.Kinopoisk.Extensions;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.Providers;

/// <summary>
/// Movie metadata from Kinopoisk.
/// </summary>
public sealed class MovieMetadataProvider : KinopoiskMetadataProvider<Movie, MovieInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MovieMetadataProvider"/> class.
    /// </summary>
    /// <param name="client">The Kinopoisk client.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public MovieMetadataProvider(IKinopoiskClient client, IHttpClientFactory httpClientFactory, ILogger<MovieMetadataProvider> logger)
        : base(client, httpClientFactory, logger)
    {
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<RemoteSearchResult>> SearchAsync(string keyword, CancellationToken cancellationToken)
    {
        var films = await Client.SearchFilmsAsync(keyword, cancellationToken).ConfigureAwait(false);
        return [.. films.Where(f => f.IsFilm).Select(f => f.ToSearchResult())];
    }

    /// <inheritdoc />
    protected override async Task<RemoteSearchResult> GetSearchResultByIdAsync(int kinopoiskId, CancellationToken cancellationToken)
        => (await Client.GetFilmAsync(kinopoiskId, cancellationToken).ConfigureAwait(false)).ToSearchResult();

    /// <inheritdoc />
    protected override async Task FillAsync(MetadataResult<Movie> result, int kinopoiskId, CancellationToken cancellationToken)
        => result.FillFrom(await Client.GetFilmAsync(kinopoiskId, cancellationToken).ConfigureAwait(false));

    /// <inheritdoc />
    protected override IEnumerable<string> GetKeywords(MovieInfo info)
    {
        foreach (var keyword in base.GetKeywords(info))
        {
            yield return keyword;
        }

        foreach (var (title, year) in info.Path.ParseFileName())
        {
            yield return year is null ? title : $"{title} {year}";
        }
    }
}
