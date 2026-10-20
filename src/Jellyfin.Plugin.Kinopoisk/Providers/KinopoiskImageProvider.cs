using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Api;
using Jellyfin.Plugin.Kinopoisk.Common;
using Jellyfin.Plugin.Kinopoisk.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.Providers;

/// <summary>
/// Shared image flow: collect (type, url) pairs, drop empties and duplicates, convert.
/// </summary>
/// <typeparam name="TItem">The Jellyfin item type.</typeparam>
public abstract class KinopoiskImageProvider<TItem> : IRemoteImageProvider
    where TItem : BaseItem
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="KinopoiskImageProvider{TItem}"/> class.
    /// </summary>
    /// <param name="client">The Kinopoisk client.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    protected KinopoiskImageProvider(IKinopoiskClient client, IHttpClientFactory httpClientFactory, ILogger logger)
    {
        Client = client;
        _httpClientFactory = httpClientFactory;
        Logger = logger;
    }

    /// <inheritdoc />
    public string Name => Constants.ProviderName;

    /// <summary>Gets the Kinopoisk client.</summary>
    protected IKinopoiskClient Client { get; }

    /// <summary>Gets the logger.</summary>
    protected ILogger Logger { get; }

    /// <inheritdoc />
    public bool Supports(BaseItem item) => item is TItem;

    /// <inheritdoc />
    public abstract IEnumerable<ImageType> GetSupportedImages(BaseItem item);

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        if (!item.TryGetKinopoiskId(out var kinopoiskId))
        {
            return [];
        }

        try
        {
            var images = await CollectAsync(item, kinopoiskId, cancellationToken).ConfigureAwait(false);
            return ToRemoteImages(images);
        }
        catch (KinopoiskApiException ex)
        {
            Logger.LogError(ex, "Kinopoisk image lookup failed for {Name} ({Id})", item.Name, kinopoiskId);
            return [];
        }
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        => _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);

    /// <summary>
    /// Converts raw pairs into distinct Jellyfin images, keeping order.
    /// </summary>
    /// <param name="images">The pairs.</param>
    /// <returns>The images.</returns>
    internal static IReadOnlyList<RemoteImageInfo> ToRemoteImages(IEnumerable<(ImageType Type, string? Url)> images)
        => [.. images
            .Where(i => !string.IsNullOrWhiteSpace(i.Url))
            .DistinctBy(i => (i.Type, i.Url))
            .Select(i => new RemoteImageInfo
            {
                Type = i.Type,
                Url = i.Url,
                ProviderName = Constants.ProviderName,
                Language = Constants.MetadataLanguage,
            })];

    /// <summary>
    /// Collects image pairs for an item with a Kinopoisk id.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="kinopoiskId">The id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The pairs.</returns>
    protected abstract Task<IEnumerable<(ImageType Type, string? Url)>> CollectAsync(
        BaseItem item, int kinopoiskId, CancellationToken cancellationToken);
}
