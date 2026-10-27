using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Api;
using Jellyfin.Plugin.Kinopoisk.Api.Models;
using Jellyfin.Plugin.Kinopoisk.Common;
using Jellyfin.Plugin.Kinopoisk.Extensions;
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
    private readonly ImageDownloader _imageDownloader;
    private readonly ILogger<EpisodeMetadataProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EpisodeMetadataProvider"/> class.
    /// </summary>
    /// <param name="client">The Kinopoisk client.</param>
    /// <param name="imageDownloader">The image downloader.</param>
    /// <param name="logger">The logger.</param>
    public EpisodeMetadataProvider(IKinopoiskClient client, ImageDownloader imageDownloader, ILogger<EpisodeMetadataProvider> logger)
    {
        _client = client;
        _imageDownloader = imageDownloader;
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
                KeepLibraryNumbering(result.Item, info);
            }
        }
        catch (KinopoiskApiException ex)
        {
            LookupFailure.Log(_logger, ex, $"S{info.ParentIndexNumber}E{info.IndexNumber}");
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
        => _imageDownloader.GetAsync(url, cancellationToken);

    /// <summary>
    /// Finds the episode: by its stored Kinopoisk id, then by season and episode number in its season (series on the
    /// streaming service), among the latest and upcoming episodes, and finally in the episodes page of the site.
    /// </summary>
    /// <param name="info">The episode info.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The episode, or null.</returns>
    public async Task<FilmEpisode?> FindEpisodeAsync(EpisodeInfo info, CancellationToken cancellationToken)
    {
        var series = new SeriesInfo { ProviderIds = info.SeriesProviderIds };
        if (!series.TryGetKinopoiskId(out var seriesId))
        {
            return null;
        }

        var seasonEpisodes = await GetSeasonEpisodesAsync(series, seriesId, info.ParentIndexNumber, cancellationToken).ConfigureAwait(false);
        var seriesEpisodes = await _client.GetSeriesEpisodesAsync(seriesId, cancellationToken).ConfigureAwait(false);
        var match = FindById(info, [.. seasonEpisodes, .. seriesEpisodes])
            ?? FindByNumber(info, seasonEpisodes)
            ?? FindByNumber(info, seriesEpisodes);
        if (match is not null || info.IndexNumber is null || info.ParentIndexNumber is null)
        {
            return match;
        }

        var guide = await GetEpisodeGuideAsync(seriesId, cancellationToken).ConfigureAwait(false);
        return FindByNumber(info, guide);
    }

    // The episodes page is the last resort; when it is unavailable the episode simply stays unmatched.
    private async Task<IReadOnlyList<FilmEpisode>> GetEpisodeGuideAsync(int seriesId, CancellationToken cancellationToken)
    {
        try
        {
            return await _client.GetEpisodeGuideAsync(seriesId, cancellationToken).ConfigureAwait(false);
        }
        catch (KinopoiskApiException ex)
        {
            LookupFailure.Log(_logger, ex, $"the episodes page of {seriesId}");
            return [];
        }
    }

    // An episode matched by its stored id may be numbered differently on Kinopoisk (e.g. anime with absolute
    // numbering); the library's own numbers win, so a refresh never moves the episode.
    private static void KeepLibraryNumbering(Episode item, EpisodeInfo info)
    {
        item.IndexNumber = info.IndexNumber ?? item.IndexNumber;
        item.ParentIndexNumber = info.ParentIndexNumber ?? item.ParentIndexNumber;
    }

    private static FilmEpisode? FindById(EpisodeInfo info, IEnumerable<FilmEpisode> episodes)
        => info.TryGetKinopoiskId(out var episodeId) ? episodes.FirstOrDefault(e => e.Id == episodeId) : null;

    private static FilmEpisode? FindByNumber(EpisodeInfo info, IEnumerable<FilmEpisode> episodes)
        => episodes.FirstOrDefault(e => e.Number == info.IndexNumber && e.Season?.Number == info.ParentIndexNumber);

    private async Task<IReadOnlyList<FilmEpisode>> GetSeasonEpisodesAsync(
        SeriesInfo series, int seriesId, int? season, CancellationToken cancellationToken)
    {
        if (season is null)
        {
            return [];
        }

        var seriesContentId = await GetContentIdAsync(series, seriesId, cancellationToken).ConfigureAwait(false);
        if (seriesContentId is null)
        {
            return [];
        }

        var page = await _client.GetFilmPageAsync(seriesContentId, cancellationToken).ConfigureAwait(false);
        var seasonContentId = page.Seasons?.Items?.FirstOrDefault(s => s.Number == season)?.ContentId;
        return seasonContentId is null
            ? []
            : await _client.GetSeasonEpisodesAsync(seasonContentId, cancellationToken).ConfigureAwait(false);
    }

    // Series fetched before the content UUID was stored have none yet; Kinopoisk returns it with the series.
    private async Task<string?> GetContentIdAsync(SeriesInfo series, int seriesId, CancellationToken cancellationToken)
    {
        if (series.TryGetContentId(out var storedContentId) && !string.IsNullOrWhiteSpace(storedContentId))
        {
            return storedContentId;
        }

        var seriesInfo = await _client.GetSeriesAsync(seriesId, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(seriesInfo.ContentId) ? null : seriesInfo.ContentId;
    }
}
