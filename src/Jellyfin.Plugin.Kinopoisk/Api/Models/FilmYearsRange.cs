namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// A range of years.
/// </summary>
/// <param name="Start">The first year.</param>
/// <param name="End">The last year.</param>
public sealed record FilmYearsRange(int? Start = null, int? End = null);
