using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Api;
using Jellyfin.Plugin.Kinopoisk.Configuration;
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
    private readonly IKinopoiskOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MovieMetadataProvider"/> class.
    /// </summary>
    /// <param name="client">The Kinopoisk client.</param>
    /// <param name="imageDownloader">The image downloader.</param>
    /// <param name="options">The plugin options.</param>
    /// <param name="logger">The logger.</param>
    public MovieMetadataProvider(
        IKinopoiskClient client, ImageDownloader imageDownloader, IKinopoiskOptions options, ILogger<MovieMetadataProvider> logger)
        : base(client, imageDownloader, logger)
    {
        _options = options;
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<RemoteSearchResult>> SearchAsync(string title, int? year, CancellationToken cancellationToken)
    {
        // Series are kept: Kinopoisk files some titles that libraries keep as movies (e.g. multi-part cartoons) as series.
        var films = await Client.SearchFilmsAsync(title, year, cancellationToken).ConfigureAwait(false);
        return [.. films.Select(f => f.ToSearchResult())];
    }

    /// <inheritdoc />
    protected override async Task<RemoteSearchResult> GetSearchResultByIdAsync(int kinopoiskId, CancellationToken cancellationToken)
        => (await Client.GetTitleAsync(kinopoiskId, cancellationToken).ConfigureAwait(false)).ToSearchResult();

    /// <inheritdoc />
    protected override async Task FillAsync(MetadataResult<Movie> result, int kinopoiskId, CancellationToken cancellationToken)
        => result.FillFrom(await Client.GetTitleAsync(kinopoiskId, cancellationToken).ConfigureAwait(false), _options);

    /// <inheritdoc />
    protected override IEnumerable<string> GetPathNames(MovieInfo info)
    {
        if (string.IsNullOrWhiteSpace(info.Path))
        {
            yield break;
        }

        // The file itself first (movies in a mixed folder), then the movie's own folder.
        yield return Path.GetFileName(info.Path);

        var folder = Path.GetFileName(Path.GetDirectoryName(info.Path));
        if (!string.IsNullOrEmpty(folder))
        {
            yield return folder;
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<(string Title, int? Year)> GetSearchTerms(MovieInfo info)
        => base.GetSearchTerms(info).Concat(info.Path.ParseFileName());
}
