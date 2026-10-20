using Jellyfin.Plugin.Kinopoisk.Api.Models;

namespace Jellyfin.Plugin.Kinopoisk.Api;

/// <summary>
/// Wrapper of a film entry of <c>SuggestSearch</c>.
/// </summary>
/// <param name="Movie">The film or series.</param>
internal sealed record SuggestMovie(FilmInfo? Movie = null);
