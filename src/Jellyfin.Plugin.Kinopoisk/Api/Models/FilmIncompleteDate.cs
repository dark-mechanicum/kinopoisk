namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// A date with an accuracy (year, month or day).
/// </summary>
/// <param name="Accuracy">The accuracy.</param>
/// <param name="Date">The ISO date.</param>
public sealed record FilmIncompleteDate(string? Accuracy = null, string? Date = null);
