namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// A crew list entry.
/// </summary>
/// <param name="Person">The person.</param>
public sealed record FilmCrewMember(FilmPerson? Person = null);
