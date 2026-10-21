using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.Providers;

/// <summary>
/// Person photos from Kinopoisk.
/// </summary>
public sealed class PersonImageProvider : KinopoiskImageProvider<Person>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PersonImageProvider"/> class.
    /// </summary>
    /// <param name="client">The Kinopoisk client.</param>
    /// <param name="imageDownloader">The image downloader.</param>
    /// <param name="logger">The logger.</param>
    public PersonImageProvider(IKinopoiskClient client, ImageDownloader imageDownloader, ILogger<PersonImageProvider> logger)
        : base(client, imageDownloader, logger)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<ImageType> GetSupportedImages(BaseItem item) => [ImageType.Primary];

    /// <inheritdoc />
    protected override async Task<IEnumerable<(ImageType Type, string? Url)>> CollectAsync(
        BaseItem item, int kinopoiskId, CancellationToken cancellationToken)
        => (await Client.GetPersonAsync(kinopoiskId, cancellationToken).ConfigureAwait(false)).GetArtwork();
}
