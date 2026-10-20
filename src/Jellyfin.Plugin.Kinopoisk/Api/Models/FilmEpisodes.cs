using System.Collections.Generic;

namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// A page of episodes.
/// </summary>
/// <param name="Items">The episodes.</param>
/// <param name="Total">The total count on the server.</param>
public sealed record FilmEpisodes(IReadOnlyList<FilmEpisode>? Items = null, int? Total = null);
