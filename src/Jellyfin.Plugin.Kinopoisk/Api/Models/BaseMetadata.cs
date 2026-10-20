using System.Collections.Generic;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// Common shape of everything the plugin fetches from Kinopoisk.
/// </summary>
public abstract record BaseMetadata
{
    /// <summary>Gets the numeric Kinopoisk id.</summary>
    public int Id { get; init; }

    /// <summary>
    /// Converts the item to a Jellyfin search result.
    /// </summary>
    /// <returns>The search result.</returns>
    public abstract RemoteSearchResult ToSearchResult();

    /// <summary>
    /// Lists the artwork of the item; urls may be null and are filtered by callers.
    /// </summary>
    /// <returns>Image type and url pairs.</returns>
    public abstract IEnumerable<(ImageType Type, string? Url)> GetArtwork();
}
