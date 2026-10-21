using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Api;
using Jellyfin.Plugin.Kinopoisk.Api.Models;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Kinopoisk.Providers;

/// <summary>
/// Collects artwork of a film or series from all Kinopoisk sources.
/// </summary>
internal static class FilmImageCollector
{
    /// <summary>
    /// Categories requested one by one. A fixed list, so a new enum member never silently adds requests.
    /// <c>PROMO</c> is left out: it repeats the posters.
    /// </summary>
    internal static readonly FilmImageType[] RequestedTypes =
    [
        FilmImageType.Poster,
        FilmImageType.Cover,
        FilmImageType.Still,
        FilmImageType.Wallpaper,
        FilmImageType.Screenshot,
        FilmImageType.FanArt,
    ];

    /// <summary>
    /// Collects images: the base info gallery, the streaming page (if any) and every typed image category.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="baseInfo">The film or series base info.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Image pairs, best first.</returns>
    public static async Task<IEnumerable<(ImageType Type, string? Url)>> CollectAsync(
        IKinopoiskClient client, FilmInfo baseInfo, CancellationToken cancellationToken)
    {
        var images = new List<(ImageType Type, string? Url)>();

        if (!string.IsNullOrWhiteSpace(baseInfo.ContentId))
        {
            var page = await client.GetFilmPageAsync(baseInfo.ContentId, cancellationToken).ConfigureAwait(false);
            images.AddRange(page.GetArtwork());
        }

        images.AddRange(baseInfo.GetArtwork());

        foreach (var type in RequestedTypes)
        {
            var typed = await client.GetImagesAsync(baseInfo.Id, type, cancellationToken).ConfigureAwait(false);
            images.AddRange(ToArtwork(typed));
        }

        return images;
    }

    /// <summary>
    /// Converts typed images, skipping categories the plugin does not know.
    /// </summary>
    /// <param name="images">The images.</param>
    /// <returns>Image pairs.</returns>
    internal static IEnumerable<(ImageType Type, string? Url)> ToArtwork(FilmImages images)
        => from item in images.Items ?? []
           let category = item.Category
           where category is not null
           select (FilmInfo.ToImageType(category.Value), item.Image?.Url);
}
