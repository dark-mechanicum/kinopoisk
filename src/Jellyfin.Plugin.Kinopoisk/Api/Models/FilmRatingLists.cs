namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// The Kinopoisk rating lists a title is in.
/// </summary>
/// <param name="Top250">The place in the Top-250 of films or of series; null when the title is not in it.</param>
public sealed record FilmRatingLists(FilmListEntry? Top250 = null)
{
    /// <summary>Gets a value indicating whether the title is in the Top-250.</summary>
    public bool IsInTop250 => Top250?.Position > 0;
}
