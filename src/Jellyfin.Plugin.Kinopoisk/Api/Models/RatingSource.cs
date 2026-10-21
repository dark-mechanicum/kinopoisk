namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// Where the community rating comes from.
/// </summary>
public enum RatingSource
{
    /// <summary>The Kinopoisk users' rating.</summary>
    Kinopoisk,

    /// <summary>The IMDb rating.</summary>
    Imdb,
}
