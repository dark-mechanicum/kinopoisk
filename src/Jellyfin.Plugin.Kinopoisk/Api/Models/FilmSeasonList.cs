using System.Collections.Generic;

namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// A page of seasons.
/// </summary>
/// <param name="Items">The seasons.</param>
public sealed record FilmSeasonList(IReadOnlyList<FilmSeason>? Items = null);
