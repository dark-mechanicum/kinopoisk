using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Api;
using Jellyfin.Plugin.Kinopoisk.Api.Models;
using Jellyfin.Plugin.Kinopoisk.Common;
using Jellyfin.Plugin.Kinopoisk.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.Providers;

/// <summary>
/// Episode metadata from Kinopoisk, matched by season and episode number within the series.
/// </summary>
public sealed class EpisodeMetadataProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>
{
    private readonly IKinopoiskClient _client;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EpisodeMetadataProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EpisodeMetadataProvider"/> class.
    /// </summary>
    /// <param name="client">The Kinopoisk client.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public EpisodeMetadataProvider(IKinopoiskClient client, IHttpClientFactory httpClientFactory, ILogger<EpisodeMetadataProvider> logger)
    {
        _client = client;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => Constants.ProviderName;

    /// <inheritdoc />
    public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
    {
        var result = MetadataResultExtensions.CreateResult<Episode>();
        try
        {
            var episode = await FindEpisodeAsync(info, cancellationToken).ConfigureAwait(false);
            if (episode is not null)
            {
                result.FillFrom(episode);
            }
        }
        catch (KinopoiskApiException ex)
        {
            _logger.LogError(ex, "Kinopoisk episode lookup failed for S{Season}E{Episode}", info.ParentIndexNumber, info.IndexNumber);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
    {
        var episode = await FindEpisodeAsync(searchInfo, cancellationToken).ConfigureAwait(false);
        return episode is null ? [] : [episode.ToSearchResult()];
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        => _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);

    /// <summary>
    /// Finds the episode: first in its season (series on the streaming service), then among the latest releases.
    /// </summary>
    /// <param name="info">The episode info.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The episode, or null.</returns>
    public async Task<FilmEpisode?> FindEpisodeAsync(EpisodeInfo info, CancellationToken cancellationToken)
    {
        if (info.IndexNumber is not { } number || info.ParentIndexNumber is not { } season)
        {
            return null;
        }

        var series = new SeriesInfo { ProviderIds = info.SeriesProviderIds };
        if (!series.TryGetKinopoiskId(out var seriesId))
        {
            return null;
        }

        var seasonContentId = await FindSeasonContentIdAsync(series, season, cancellationToken).ConfigureAwait(false);
        if (seasonContentId is not null)
        {
            var seasonEpisodes = await _client.GetSeasonEpisodesAsync(seasonContentId, cancellationToken).ConfigureAwait(false);
            var match = seasonEpisodes.FirstOrDefault(e => e.Number == number);
            if (match is not null)
            {
                return match;
            }
        }

        var latest = await _client.GetLatestEpisodesAsync(seriesId, cancellationToken).ConfigureAwait(false);
        return latest.FirstOrDefault(e => e.Number == number && e.Season?.Number == season);
    }

    private async Task<string?> FindSeasonContentIdAsync(SeriesInfo series, int season, CancellationToken cancellationToken)
    {
        if (!series.TryGetContentId(out var seriesContentId) || seriesContentId is null)
        {
            return null;
        }

        var page = await _client.GetFilmPageAsync(seriesContentId, cancellationToken).ConfigureAwait(false);
        return page.Seasons?.Items?.FirstOrDefault(s => s.Number == season)?.ContentId;
    }
}
