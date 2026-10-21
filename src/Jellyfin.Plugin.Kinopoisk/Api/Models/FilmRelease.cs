using System.Collections.Generic;

namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// One release of a film or series.
/// </summary>
/// <param name="Companies">The companies behind it.</param>
public sealed record FilmRelease(IReadOnlyList<FilmCompany>? Companies = null);
