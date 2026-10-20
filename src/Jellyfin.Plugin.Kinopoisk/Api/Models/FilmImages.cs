using System.Collections.Generic;

namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// A page of typed images.
/// </summary>
/// <param name="Items">The images.</param>
/// <param name="Total">The total count on the server.</param>
public sealed record FilmImages(IReadOnlyList<FilmImage>? Items = null, int? Total = null);
