using System.Collections.Generic;
using Jellyfin.Plugin.Kinopoisk.Common;
using Jellyfin.Plugin.Kinopoisk.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Kinopoisk.Providers;

/// <summary>
/// Adds the kinopoisk.ru link to movies, series and persons.
/// </summary>
public sealed class KinopoiskExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc />
    public string Name => Constants.PluginName;

    /// <inheritdoc />
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        var section = item switch
        {
            Movie => "film",
            Series => "series",
            Person => "name",
            _ => null,
        };

        if (section is not null && item.TryGetKinopoiskId(out var kinopoiskId))
        {
            yield return $"{Constants.SiteUrl}/{section}/{kinopoiskId}/";
        }
    }
}
