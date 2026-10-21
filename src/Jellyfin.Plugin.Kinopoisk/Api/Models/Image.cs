using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// An image hosted by Yandex avatars; the size is chosen by URL suffix.
/// </summary>
public sealed record Image
{
    /// <summary>
    /// Width suffix that every Kinopoisk image host serves.
    /// </summary>
    public const string DefaultSize = "576x";

    private static readonly string[] _placeholderMarkers = ["/placeholder", "no-poster", "projector-logo"];

    /// <summary>
    /// Gets the base avatars URL, protocol-relative (<c>//host/path</c>) or absolute.
    /// </summary>
    public string? AvatarsUrl { get; init; }

    /// <summary>
    /// Gets the absolute URL in <see cref="DefaultSize"/>, or null when there is no image.
    /// </summary>
    [JsonIgnore]
    public string? Url => ToAbsoluteUrl(AvatarsUrl, DefaultSize);

    /// <summary>
    /// Builds an absolute https URL from a Kinopoisk image path.
    /// </summary>
    /// <param name="path">The path, protocol-relative or absolute.</param>
    /// <param name="size">Optional size suffix to append.</param>
    /// <returns>The absolute URL, or null for an empty path or a "no photo" placeholder.</returns>
    public static string? ToAbsoluteUrl(string? path, string? size = null)
    {
        if (string.IsNullOrWhiteSpace(path) || IsPlaceholder(path))
        {
            return null;
        }

        var url = path.StartsWith("//", StringComparison.Ordinal) ? "https:" + path : path;
        return size is null ? url : $"{url.TrimEnd('/')}/{size}";
    }

    /// <summary>
    /// Checks whether a URL is the picture Kinopoisk shows when it has none (e.g. <c>projector-logo/placeholder.svg</c>).
    /// </summary>
    /// <param name="url">The url.</param>
    /// <returns><c>true</c> for a placeholder.</returns>
    public static bool IsPlaceholder(string? url)
        => url is not null
            && (url.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
                || _placeholderMarkers.Any(marker => url.Contains(marker, StringComparison.OrdinalIgnoreCase)));
}
