using System.Collections.Generic;

namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// A page of crew members.
/// </summary>
/// <param name="Items">The members.</param>
public sealed record FilmCrewMembers(IReadOnlyList<FilmCrewMember>? Items = null);
