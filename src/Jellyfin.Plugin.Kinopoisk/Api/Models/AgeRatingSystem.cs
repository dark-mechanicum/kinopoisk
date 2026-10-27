namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// Which of the two age ratings Kinopoisk gives is stored as the official rating; the other one is the fallback.
/// </summary>
public enum AgeRatingSystem
{
    /// <summary>The MPAA rating (<c>G</c>, <c>PG</c>, <c>PG-13</c>, <c>R</c>, <c>NC-17</c>).</summary>
    Mpaa,

    /// <summary>The Russian age rating (<c>0+</c>, <c>6+</c>, <c>12+</c>, <c>16+</c>, <c>18+</c>).</summary>
    Russian,
}
