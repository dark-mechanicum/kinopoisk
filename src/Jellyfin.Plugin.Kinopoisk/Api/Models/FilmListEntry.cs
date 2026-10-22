namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// The place of a title in a Kinopoisk list, e.g. the Top-250.
/// </summary>
/// <param name="MovieListSlug">The list slug: <c>top250</c> for films, <c>series-top250</c> for series.</param>
/// <param name="Position">The position, starting at 1.</param>
public sealed record FilmListEntry(string? MovieListSlug = null, int? Position = null);
