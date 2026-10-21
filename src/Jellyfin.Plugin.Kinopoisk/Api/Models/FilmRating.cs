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

    /// <summary>Gets the critics rating (0-100).</summary>
    public float? Critics => Active(WorldwideCritics)?.Percent;

    /// <summary>
    /// Gets the community rating (0-10): the preferred source first, the other one as fallback.
    /// </summary>
    /// <param name="preferred">The preferred source.</param>
    /// <returns>The rating, or null when neither is active.</returns>
    public float? GetCommunity(RatingSource preferred) => preferred == RatingSource.Imdb
        ? Active(Imdb)?.Value ?? Active(Kinopoisk)?.Value
        : Active(Kinopoisk)?.Value ?? Active(Imdb)?.Value;

    private static FilmRatingValue? Active(FilmRatingValue? rating) => rating?.IsActive == false ? null : rating;
}
