using System;

namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// Age restriction of a film or series.
/// </summary>
/// <param name="Age">The Russian age restriction, e.g. <c>age18</c>.</param>
/// <param name="Mpaa">The MPAA rating, e.g. <c>r</c> or <c>pg13</c>.</param>
public sealed record FilmRestriction(string? Age = null, string? Mpaa = null)
{
    /// <summary>
    /// Gets the rating in a form Jellyfin understands: MPAA (<c>PG-13</c>) when known,
    /// otherwise the Russian age (<c>18+</c>), otherwise null.
    /// </summary>
    public string? OfficialRating
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Mpaa))
            {
                var mpaa = Mpaa.ToUpperInvariant();
                return mpaa switch
                {
                    "PG13" => "PG-13",
                    "NC17" => "NC-17",
                    _ => mpaa,
                };
            }

            if (!string.IsNullOrWhiteSpace(Age))
            {
                return Age.Replace("age", string.Empty, StringComparison.OrdinalIgnoreCase) + "+";
            }

            return null;
        }
    }
}
