namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// The two episode lists of <c>TvSeriesEpisodes</c>; Kinopoisk caps each at 30 episodes.
/// </summary>
/// <param name="ReleasedEpisodes">Aired episodes, newest first.</param>
/// <param name="FutureEpisodes">Announced episodes, oldest first.</param>
public sealed record FilmEpisodeLists(FilmEpisodes? ReleasedEpisodes = null, FilmEpisodes? FutureEpisodes = null);
