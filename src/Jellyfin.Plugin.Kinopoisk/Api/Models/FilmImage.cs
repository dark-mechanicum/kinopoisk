using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// A single typed image.
/// </summary>
/// <param name="Type">The raw GraphQL category literal, e.g. <c>FAN_ART</c>; kept as text so new values never break parsing.</param>
/// <param name="Image">The image.</param>
public sealed record FilmImage(string? Type = null, Image? Image = null)
{
    /// <summary>Gets the category, or null when Kinopoisk sent one the plugin does not know.</summary>
    [JsonIgnore]
    public FilmImageType? Category => KinopoiskJson.ParseImageType(Type);
}
