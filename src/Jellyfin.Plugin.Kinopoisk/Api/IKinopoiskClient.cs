using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Api.Models;

namespace Jellyfin.Plugin.Kinopoisk.Api;

/// <summary>
/// Typed access to the Kinopoisk endpoints that are reachable behind the Russian white list.
/// </summary>
public interface IKinopoiskClient
{
    /// <summary>
    /// Searches films and series by title; the year only ranks the matches, it never filters them out.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <param name="year">The year, if known.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Matches, best first: released in that year, then with exactly that title, then in Kinopoisk order.</returns>
    Task<IReadOnlyList<FilmInfo>> SearchFilmsAsync(string title, int? year, CancellationToken cancellationToken);

    /// <summary>
    /// Searches persons by keyword.
    /// </summary>
    /// <param name="keyword">The keyword.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Matches, best first.</returns>
    Task<IReadOnlyList<FilmPerson>> SearchPersonsAsync(string keyword, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a film.
    /// </summary>
    /// <param name="filmId">The Kinopoisk id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The film.</returns>
    Task<FilmInfo> GetFilmAsync(int filmId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a TV series.
    /// </summary>
    /// <param name="seriesId">The Kinopoisk id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The series.</returns>
    Task<FilmInfo> GetSeriesAsync(int seriesId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a film, or the series with that id when Kinopoisk has no such film.
    /// </summary>
    /// <param name="id">The Kinopoisk id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The film or series.</returns>
    Task<FilmInfo> GetTitleAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the streaming page of a film or series, which has the best artwork and the season list.
    /// </summary>
    /// <param name="contentId">The content UUID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page.</returns>
    Task<FilmInfo> GetFilmPageAsync(string contentId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets one page of typed images.
    /// </summary>
    /// <param name="id">The Kinopoisk id of a film or series.</param>
    /// <param name="type">The image category.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The images.</returns>
    Task<FilmImages> GetImagesAsync(int id, FilmImageType type, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the latest released and the next announced episodes of a series, with their ids
    /// (Kinopoisk caps each list at 30).
    /// </summary>
    /// <param name="seriesId">The Kinopoisk id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The released episodes newest first, then the announced ones oldest first.</returns>
    Task<IReadOnlyList<FilmEpisode>> GetSeriesEpisodesAsync(int seriesId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets every episode of a series from the episodes page of the site: titles and release dates, without ids.
    /// </summary>
    /// <param name="seriesId">The Kinopoisk id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The episodes by season; empty when Kinopoisk has no episodes page for the title (e.g. a film).</returns>
    Task<IReadOnlyList<FilmEpisode>> GetEpisodeGuideAsync(int seriesId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets every episode of a season available on the streaming service.
    /// </summary>
    /// <param name="seasonContentId">The season content UUID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The episodes in order.</returns>
    Task<IReadOnlyList<FilmEpisode>> GetSeasonEpisodesAsync(string seasonContentId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a person.
    /// </summary>
    /// <param name="personId">The Kinopoisk id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The person.</returns>
    Task<FilmPerson> GetPersonAsync(int personId, CancellationToken cancellationToken);
}
