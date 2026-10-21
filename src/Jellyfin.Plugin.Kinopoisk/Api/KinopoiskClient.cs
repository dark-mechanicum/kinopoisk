using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Api.Models;
using Jellyfin.Plugin.Kinopoisk.Common;
using Jellyfin.Plugin.Kinopoisk.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace Jellyfin.Plugin.Kinopoisk.Api;

/// <summary>
/// Kinopoisk client over the GraphQL and site APIs, with a short-lived response cache.
/// </summary>
public sealed class KinopoiskClient : IKinopoiskClient
{
    private const int SuggestLimit = 10;
    private const int ImagesPageSize = 50;
    private const int LatestEpisodesLimit = 30;
    private const int SeasonPageSize = 50;
    private const int MaxSeasonPages = 20;

    private static readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);

    private readonly KinopoiskTransport _transport;
    private readonly IMemoryCache _cache;
    private readonly IKinopoiskOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="KinopoiskClient"/> class.
    /// </summary>
    /// <param name="transport">The transport.</param>
    /// <param name="cache">The cache.</param>
    /// <param name="options">The plugin options.</param>
    public KinopoiskClient(KinopoiskTransport transport, IMemoryCache cache, IKinopoiskOptions options)
    {
        _transport = transport;
        _cache = cache;
        _options = options;
    }

    private int VoiceOverActorsLimit => _options.IncludeVoiceActors ? _options.CrewLimit : 0;

    /// <inheritdoc />
    public async Task<IReadOnlyList<FilmInfo>> SearchFilmsAsync(string title, int? year, CancellationToken cancellationToken)
    {
        // Kinopoisk matches a year in the keyword as text, not as a filter: "title year" drops most titles,
        // and a year that is off by one finds nothing. So the plain title is searched too, and the year only ranks.
        var films = new List<FilmInfo>(await SuggestFilmsAsync(title, cancellationToken).ConfigureAwait(false));
        if (year is not null)
        {
            films.AddRange(await SuggestFilmsAsync($"{title} {year}", cancellationToken).ConfigureAwait(false));
        }

        return [.. films
            .DistinctBy(f => f.Id)
            .OrderByDescending(f => f.IsReleasedIn(year))
            .ThenByDescending(f => f.HasTitle(title))];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FilmPerson>> SearchPersonsAsync(string keyword, CancellationToken cancellationToken)
    {
        var top = await SuggestAsync(keyword, cancellationToken).ConfigureAwait(false);
        var results = new List<FilmPerson>();
        if (top.TopResult?.Global is { IsPerson: true } best)
        {
            results.Add(best.ToPerson());
        }

        results.AddRange(top.Persons.Select(p => p.Person).OfType<FilmPerson>());
        return [.. results.Where(p => p.Id > 0).DistinctBy(p => p.Id)];
    }

    /// <inheritdoc />
    public Task<FilmInfo> GetFilmAsync(int filmId, CancellationToken cancellationToken)
        => CachedAsync($"film:{filmId}", () => _transport.QueryAsync<FilmInfo>(
            "FilmBaseInfo",
            new
            {
                filmId,
                isAuthorized = false,
                actorsLimit = _options.CrewLimit,
                voiceOverActorsLimit = VoiceOverActorsLimit,
                relatedMoviesLimit = 0,
                checkSilentInvoiceAvailability = false,
                withPurchaseOptions = false,
                watchabilityLimit = 0,
                socialArgumentLimit = 0,
            },
            "data.film",
            cancellationToken));

    /// <inheritdoc />
    public Task<FilmInfo> GetSeriesAsync(int seriesId, CancellationToken cancellationToken)
        => CachedAsync($"series:{seriesId}", () => _transport.QueryAsync<FilmInfo>(
            "TvSeriesBaseInfo",
            new
            {
                tvSeriesId = seriesId,
                isAuthorized = false,
                actorsLimit = _options.CrewLimit,
                voiceOverActorsLimit = VoiceOverActorsLimit,
                relatedMoviesLimit = 0,
                checkSilentInvoiceAvailability = false,
                withPurchaseOptions = false,
                watchabilityLimit = 0,
                socialArgumentLimit = 0,
            },
            "data.tvSeries",
            cancellationToken));

    /// <inheritdoc />
    public Task<FilmInfo> GetTitleAsync(int id, CancellationToken cancellationToken)
        => CachedAsync($"title:{id}", () => FetchFilmOrSeriesAsync(id, cancellationToken));

    /// <inheritdoc />
    public Task<FilmInfo> GetFilmPageAsync(string contentId, CancellationToken cancellationToken)
        => CachedAsync($"page:{contentId}", () => _transport.QueryAsync<FilmInfo>(
            "FilmPage",
            new
            {
                contentUuid = contentId,
                isAuthorized = false,
                withPrechosenEpisode = false,
                withUserData = false,
                seasonNumber = 0,
                episodeNumber = 0,
                socialArgumentLimit = 0,
                includeSocialArgumentTypes = Array.Empty<string>(),
            },
            "data.movieByContentUuid",
            cancellationToken));

    /// <inheritdoc />
    public Task<FilmImages> GetImagesAsync(int id, FilmImageType type, CancellationToken cancellationToken)
        => CachedAsync($"images:{id}:{type}", () => _transport.QueryAsync<FilmImages>(
            "MovieImagesItems",
            new { id, type = KinopoiskJson.ToLiteral(type), offset = 0, limit = ImagesPageSize },
            "data.movie.images",
            cancellationToken));

    /// <inheritdoc />
    public async Task<IReadOnlyList<FilmEpisode>> GetLatestEpisodesAsync(int seriesId, CancellationToken cancellationToken)
    {
        var page = await CachedAsync($"latest:{seriesId}", () => _transport.QueryAsync<FilmEpisodes>(
            "TvSeriesEpisodes",
            new { tvSeriesId = seriesId, episodesLimit = LatestEpisodesLimit },
            "data.tvSeries.releasedEpisodes",
            cancellationToken)).ConfigureAwait(false);
        return page.Items ?? [];
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FilmEpisode>> GetSeasonEpisodesAsync(string seasonContentId, CancellationToken cancellationToken)
        => CachedAsync($"season:{seasonContentId}", () => FetchSeasonEpisodesAsync(seasonContentId, cancellationToken));

    /// <inheritdoc />
    public Task<FilmPerson> GetPersonAsync(int personId, CancellationToken cancellationToken)
        => CachedAsync($"person:{personId}", () => _transport.GetAsync<FilmPerson>($"person/{personId}/", cancellationToken));

    private async Task<IReadOnlyList<FilmEpisode>> FetchSeasonEpisodesAsync(string seasonContentId, CancellationToken cancellationToken)
    {
        var episodes = new List<FilmEpisode>();
        for (var page = 0; page < MaxSeasonPages; page++)
        {
            var chunk = await _transport.QueryAsync<FilmEpisodes>(
                "SerialStructureSeason",
                new { contentId = seasonContentId, limit = SeasonPageSize, offset = page * SeasonPageSize, withUserData = false },
                "data.seasonByContentId.episodes",
                cancellationToken).ConfigureAwait(false);

            var items = chunk.Items ?? [];
            episodes.AddRange(items);
            if (items.Count < SeasonPageSize || episodes.Count >= chunk.Total)
            {
                break;
            }
        }

        return episodes;
    }

    private async Task<FilmInfo> FetchFilmOrSeriesAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            return await GetFilmAsync(id, cancellationToken).ConfigureAwait(false);
        }
        catch (KinopoiskApiException ex) when (ex.Kind == KinopoiskErrorKind.NotFound)
        {
            // Kinopoisk files some titles that libraries keep as movies (e.g. multi-part cartoons) as series.
            return await GetSeriesAsync(id, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<IReadOnlyList<FilmInfo>> SuggestFilmsAsync(string keyword, CancellationToken cancellationToken)
    {
        var top = await SuggestAsync(keyword, cancellationToken).ConfigureAwait(false);
        var results = new List<FilmInfo>();
        if (top.TopResult?.Global is { IsPerson: false, Title: not null } best)
        {
            results.Add(best.ToFilm());
        }

        results.AddRange(top.Movies.Select(m => m.Movie).OfType<FilmInfo>());
        return [.. results.Where(f => f.Id > 0)];
    }

    private async Task<SuggestTop> SuggestAsync(string keyword, CancellationToken cancellationToken)
    {
        var key = $"suggest:{keyword}";
        if (_cache.TryGetValue(key, out SuggestTop? cached) && cached is not null)
        {
            return cached;
        }

        var suggest = await _transport.QueryAsync<SuggestResult>(
            "SuggestSearch",
            new { keyword, yandexCityId = Constants.YandexCityId, limit = SuggestLimit },
            "data.suggest",
            cancellationToken).ConfigureAwait(false);

        // A null top is Kinopoisk failing to answer, not a real "nothing found", so it is not cached.
        if (suggest.Top is null)
        {
            return new SuggestTop();
        }

        _cache.Set(key, suggest.Top, _cacheDuration);
        return suggest.Top;
    }

    private async Task<T> CachedAsync<T>(string key, Func<Task<T>> fetch)
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        var value = await fetch().ConfigureAwait(false);
        _cache.Set(key, value, _cacheDuration);
        return value;
    }
}
