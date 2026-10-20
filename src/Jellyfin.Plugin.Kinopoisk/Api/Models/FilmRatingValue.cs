namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// A rating value.
/// </summary>
public sealed record FilmRatingValue
{
    /// <summary>Gets the value.</summary>
    public float? Value { get; init; }

    /// <summary>Gets the percentage of positive votes (critics).</summary>
    public float? Percent { get; init; }

    /// <summary>Gets a value indicating whether the rating is shown on the site.</summary>
    public bool? IsActive { get; init; }

    /// <summary>Gets the vote count.</summary>
    public int? Count { get; init; }
}
