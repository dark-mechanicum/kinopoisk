namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// A Russian/original title pair.
/// </summary>
/// <param name="Russian">The Russian title.</param>
/// <param name="Original">The original title.</param>
/// <param name="Localized">The localized title (season structure queries).</param>
public sealed record FilmTitle(string? Russian = null, string? Original = null, string? Localized = null)
{
    /// <summary>
    /// Gets the best title for a Russian UI: Russian, then localized, then original.
    /// </summary>
    public string? Display => NullIfEmpty(Russian) ?? NullIfEmpty(Localized) ?? NullIfEmpty(Original);

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
