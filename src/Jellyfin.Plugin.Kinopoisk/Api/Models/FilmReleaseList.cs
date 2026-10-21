using System.Collections.Generic;

namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// A page of releases.
/// </summary>
/// <param name="Items">The releases.</param>
public sealed record FilmReleaseList(IReadOnlyList<FilmRelease>? Items = null);
