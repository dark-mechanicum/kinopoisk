using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Api;
using Jellyfin.Plugin.Kinopoisk.Configuration;
using Jellyfin.Plugin.Kinopoisk.Extensions;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.Providers;

/// <summary>
/// Series metadata from Kinopoisk.
/// </summary>
public sealed class SeriesMetadataProvider : KinopoiskMetadataProvider<Series, SeriesInfo>
{
    private readonly IKinopoiskOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeriesMetadataProvider"/> class.
    /// </summary>
    /// <param name="client">The Kinopoisk client.</param>
    /// <param name="imageDownloader">The image downloader.</param>
    /// <param name="options">The plugin options.</param>
    /// <param name="logger">The logger.</param>
    public SeriesMetadataProvider(
        IKinopoiskClient client, ImageDownloader imageDownloader, IKinopoiskOptions options, ILogger<SeriesMetadataProvider> logger)
        : base(client, imageDownloader, logger)
    {
        _options = options;
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<RemoteSearchResult>> SearchAsync(string title, int? year, CancellationToken cancellationToken)
    {
        var films = await Client.SearchFilmsAsync(title, year, cancellationToken).ConfigureAwait(false);
        return [.. films.Where(f => !f.IsFilm).Select(f => f.ToSearchResult())];
    }

    /// <inheritdoc />
    protected override async Task<RemoteSearchResult> GetSearchResultByIdAsync(int kinopoiskId, CancellationToken cancellationToken)
        => (await Client.GetSeriesAsync(kinopoiskId, cancellationToken).ConfigureAwait(false)).ToSearchResult();

    /// <inheritdoc />
    protected override async Task FillAsync(MetadataResult<Series> result, int kinopoiskId, CancellationToken cancellationToken)
    {
        var series = await Client.GetSeriesAsync(kinopoiskId, cancellationToken).ConfigureAwait(false);
        result.FillFrom(series, _options);
        result.Item.FillSeriesRun(series);
    }

    /// <inheritdoc />
    protected override IEnumerable<string> GetPathNames(SeriesInfo info)
    {
        // A series path is its folder.
        var folder = Path.GetFileName(Path.TrimEndingDirectorySeparator(info.Path ?? string.Empty));
        if (!string.IsNullOrEmpty(folder))
        {
            yield return folder;
        }
    }
}
