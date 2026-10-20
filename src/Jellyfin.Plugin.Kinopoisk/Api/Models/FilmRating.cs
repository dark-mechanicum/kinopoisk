namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// Ratings of a film or series from several sources.
/// </summary>
public sealed record FilmRating
{
    /// <summary>Gets the IMDb rating.</summary>
    public FilmRatingValue? Imdb { get; init; }

    /// <summary>Gets the Kinopoisk rating.</summary>
    public FilmRatingValue? Kinopoisk { get; init; }

    /// <summary>Gets the worldwide critics rating.</summary>
    public FilmRatingValue? WorldwideCritics { get; init; }

    /// <summary>Gets the community rating (0-10): Kinopoisk first, IMDb as fallback.</summary>
    public float? Community => Active(Kinopoisk)?.Value ?? Active(Imdb)?.Value;

    /// <summary>Gets the critics rating (0-100).</summary>
    public float? Critics => Active(WorldwideCritics)?.Percent;

    private static FilmRatingValue? Active(FilmRatingValue? rating) => rating?.IsActive == false ? null : rating;
}
