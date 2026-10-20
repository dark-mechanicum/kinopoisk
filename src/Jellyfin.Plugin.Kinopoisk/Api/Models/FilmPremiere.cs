namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// A premiere.
/// </summary>
/// <param name="IncompleteDate">The date.</param>
public sealed record FilmPremiere(FilmIncompleteDate? IncompleteDate = null);
