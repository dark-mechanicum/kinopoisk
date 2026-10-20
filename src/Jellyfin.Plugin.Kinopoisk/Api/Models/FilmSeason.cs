namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// A series season reference.
/// </summary>
/// <param name="Number">The season number.</param>
/// <param name="ContentId">The season content UUID.</param>
public sealed record FilmSeason(int Number, string? ContentId = null);
