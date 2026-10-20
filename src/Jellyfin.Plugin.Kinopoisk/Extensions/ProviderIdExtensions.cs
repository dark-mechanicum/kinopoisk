using System.Globalization;
using Jellyfin.Plugin.Kinopoisk.Common;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Kinopoisk.Extensions;

/// <summary>
/// Reads and writes the Kinopoisk provider ids of any Jellyfin entity.
/// </summary>
public static class ProviderIdExtensions
{
    /// <summary>
    /// Gets the numeric Kinopoisk id.
    /// </summary>
    /// <param name="item">The entity.</param>
    /// <param name="kinopoiskId">The id, when present and valid.</param>
    /// <returns><c>true</c> when a positive id is stored.</returns>
    public static bool TryGetKinopoiskId(this IHasProviderIds item, out int kinopoiskId)
    {
        kinopoiskId = 0;
        return item.TryGetProviderId(Constants.KinopoiskIdKey, out var value)
            && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out kinopoiskId)
            && kinopoiskId > 0;
    }

    /// <summary>
    /// Stores the numeric Kinopoisk id; non-positive ids are ignored.
    /// </summary>
    /// <param name="item">The entity.</param>
    /// <param name="kinopoiskId">The id.</param>
    public static void SetKinopoiskId(this IHasProviderIds item, int kinopoiskId)
    {
        if (kinopoiskId > 0)
        {
            item.SetProviderId(Constants.KinopoiskIdKey, kinopoiskId.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Gets the Kinopoisk content UUID.
    /// </summary>
    /// <param name="item">The entity.</param>
    /// <param name="contentId">The content UUID.</param>
    /// <returns><c>true</c> when a content UUID is stored.</returns>
    public static bool TryGetContentId(this IHasProviderIds item, out string? contentId)
        => item.TryGetProviderId(Constants.ContentIdKey, out contentId);

    /// <summary>
    /// Stores the Kinopoisk content UUID. Jellyfin 12 rejects empty provider ids,
    /// and most titles have no content UUID, so empty values are ignored.
    /// </summary>
    /// <param name="item">The entity.</param>
    /// <param name="contentId">The content UUID.</param>
    public static void SetContentId(this IHasProviderIds item, string? contentId)
    {
        if (!string.IsNullOrWhiteSpace(contentId))
        {
            item.SetProviderId(Constants.ContentIdKey, contentId);
        }
    }
}
