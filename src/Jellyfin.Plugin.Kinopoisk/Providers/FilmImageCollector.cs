using System;
using System.Collections.Generic;
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

        foreach (var type in Enum.GetValues<FilmImageType>())
        {
            var typed = await client.GetImagesAsync(baseInfo.Id, type, cancellationToken).ConfigureAwait(false);
            foreach (var item in typed.Items ?? [])
            {
                images.Add((FilmInfo.ToImageType(item.Type), item.Image?.Url));
            }
        }

        return images;
    }
}
