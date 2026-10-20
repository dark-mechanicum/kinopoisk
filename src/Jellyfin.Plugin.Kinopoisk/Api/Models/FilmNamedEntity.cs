namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// A named entity: genre or country.
/// </summary>
/// <param name="Name">The localized name.</param>
public sealed record FilmNamedEntity(string? Name = null);
