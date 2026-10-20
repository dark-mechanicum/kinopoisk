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
    /// Searches films and series by keyword.
    /// </summary>
    /// <param name="keyword">The keyword, optionally followed by a year.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Matches, best first.</returns>
    Task<IReadOnlyList<FilmInfo>> SearchFilmsAsync(string keyword, CancellationToken cancellationToken);

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
    /// Gets the most recently released episodes of a series (Kinopoisk caps the list at 30).
    /// </summary>
    /// <param name="seriesId">The Kinopoisk id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The episodes, newest first.</returns>
    Task<IReadOnlyList<FilmEpisode>> GetLatestEpisodesAsync(int seriesId, CancellationToken cancellationToken);

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
