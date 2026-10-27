using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// Age restriction of a film or series.
/// </summary>
/// <param name="Age">The Russian age restriction, e.g. <c>age18</c>.</param>
/// <param name="Mpaa">The MPAA rating, e.g. <c>r</c> or <c>pg13</c>.</param>
public sealed record FilmRestriction(string? Age = null, string? Mpaa = null)
{
    // Only values Jellyfin's own rating tables know, so parental controls work without extra configuration:
    // MPAA ratings are in its "us" table, and "18+"-style ages are read as the number itself.
    private static readonly Dictionary<string, string> _mpaaRatings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["g"] = "G",
        ["pg"] = "PG",
        ["pg13"] = "PG-13",
        ["r"] = "R",
        ["nc17"] = "NC-17",
    };

    private static readonly Dictionary<string, string> _russianRatings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["age0"] = "0+",
        ["age6"] = "6+",
        ["age12"] = "12+",
        ["age16"] = "16+",
        ["age18"] = "18+",
    };

    /// <summary>
    /// Gets the rating in a form Jellyfin understands, from the preferred system with the other one as fallback.
    /// </summary>
    /// <param name="preferred">The preferred rating system.</param>
    /// <returns><c>PG-13</c>, <c>18+</c>... or null when Kinopoisk gives no known rating.</returns>
    public string? GetOfficialRating(AgeRatingSystem preferred) => preferred == AgeRatingSystem.Russian
        ? Find(_russianRatings, Age) ?? Find(_mpaaRatings, Mpaa)
        : Find(_mpaaRatings, Mpaa) ?? Find(_russianRatings, Age);

    private static string? Find(Dictionary<string, string> ratings, string? value)
        => value is not null && ratings.TryGetValue(value.Trim(), out var rating) ? rating : null;
}
