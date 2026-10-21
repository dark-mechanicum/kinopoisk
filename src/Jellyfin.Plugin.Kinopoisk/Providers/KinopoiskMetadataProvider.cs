using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Api;
using Jellyfin.Plugin.Kinopoisk.Common;
using Jellyfin.Plugin.Kinopoisk.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.Providers;

/// <summary>
/// Shared metadata flow: resolve the Kinopoisk id (tag in the path, stored, or by search), fetch, map.
/// </summary>
/// <typeparam name="TItem">The Jellyfin item type.</typeparam>
/// <typeparam name="TLookup">The lookup info type.</typeparam>
public abstract class KinopoiskMetadataProvider<TItem, TLookup> : IRemoteMetadataProvider<TItem, TLookup>
    where TItem : BaseItem, IHasLookupInfo<TLookup>, new()
    where TLookup : ItemLookupInfo, new()
{
    private readonly ImageDownloader _imageDownloader;

    /// <summary>
    /// Initializes a new instance of the <see cref="KinopoiskMetadataProvider{TItem, TLookup}"/> class.
    /// </summary>
    /// <param name="client">The Kinopoisk client.</param>
    /// <param name="imageDownloader">The image downloader.</param>
    /// <param name="logger">The logger.</param>
    protected KinopoiskMetadataProvider(IKinopoiskClient client, ImageDownloader imageDownloader, ILogger logger)
    {
        Client = client;
        _imageDownloader = imageDownloader;
        Logger = logger;
    }

    /// <inheritdoc />
    public string Name => Constants.ProviderName;

    /// <summary>Gets the Kinopoisk client.</summary>
    protected IKinopoiskClient Client { get; }

    /// <summary>Gets the logger.</summary>
    protected ILogger Logger { get; }

    /// <inheritdoc />
    public async Task<MetadataResult<TItem>> GetMetadata(TLookup info, CancellationToken cancellationToken)
    {
        var result = MetadataResultExtensions.CreateResult<TItem>();
        try
        {
            var (kinopoiskId, queriedById) = await ResolveIdAsync(info, cancellationToken).ConfigureAwait(false);
            result.QueriedById = queriedById;
            if (kinopoiskId > 0)
            {
                await FillAsync(result, kinopoiskId, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (KinopoiskApiException ex)
        {
            LookupFailure.Log(Logger, ex, info.Name);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(TLookup searchInfo, CancellationToken cancellationToken)
    {
        if (searchInfo.TryGetKinopoiskId(out var kinopoiskId))
        {
            return [await GetSearchResultByIdAsync(kinopoiskId, cancellationToken).ConfigureAwait(false)];
        }

        if (string.IsNullOrWhiteSpace(searchInfo.Name))
        {
            return [];
        }

        return await SearchAsync(searchInfo.Name, searchInfo.Year, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        => _imageDownloader.GetAsync(url, cancellationToken);

    /// <summary>
    /// Searches by title.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <param name="year">The year, if known; it ranks the results.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The results, best first.</returns>
    protected abstract Task<IReadOnlyList<RemoteSearchResult>> SearchAsync(string title, int? year, CancellationToken cancellationToken);

    /// <summary>
    /// Builds the search result for a known id.
    /// </summary>
    /// <param name="kinopoiskId">The id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    protected abstract Task<RemoteSearchResult> GetSearchResultByIdAsync(int kinopoiskId, CancellationToken cancellationToken);

    /// <summary>
    /// Fetches the item and fills the result.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="kinopoiskId">The id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    protected abstract Task FillAsync(MetadataResult<TItem> result, int kinopoiskId, CancellationToken cancellationToken);

    /// <summary>
    /// Title and year pairs to search when the item has no Kinopoisk id yet, best first.
    /// </summary>
    /// <param name="info">The lookup info.</param>
    /// <returns>The search terms.</returns>
    protected virtual IEnumerable<(string Title, int? Year)> GetSearchTerms(TLookup info)
    {
        if (!string.IsNullOrWhiteSpace(info.Name))
        {
            yield return (info.Name, info.Year);
        }
    }

    /// <summary>
    /// File and folder names of the item that may carry a <c>kp1234</c> id tag, most specific first.
    /// </summary>
    /// <param name="info">The lookup info.</param>
    /// <returns>The names.</returns>
    protected virtual IEnumerable<string> GetPathNames(TLookup info) => [];

    private async Task<(int Id, bool QueriedById)> ResolveIdAsync(TLookup info, CancellationToken cancellationToken)
    {
        // A tag in the path wins over a stored id, so renaming a folder fixes a wrong match on the next refresh.
        foreach (var name in GetPathNames(info))
        {
            if (name.TryParseKinopoiskId(out var taggedId))
            {
                Logger.LogDebug("Kinopoisk id {Id} taken from '{PathName}'", taggedId, name);
                return (taggedId, true);
            }
        }

        if (info.TryGetKinopoiskId(out var storedId))
        {
            return (storedId, true);
        }

        return (await FindIdAsync(info, cancellationToken).ConfigureAwait(false), false);
    }

    private async Task<int> FindIdAsync(TLookup info, CancellationToken cancellationToken)
    {
        foreach (var (title, year) in GetSearchTerms(info).Distinct())
        {
            var matches = await SearchAsync(title, year, cancellationToken).ConfigureAwait(false);
            if (matches is [var match, ..] && match.TryGetKinopoiskId(out var kinopoiskId))
            {
                Logger.LogInformation(
                    "Kinopoisk matched {Name} by '{Title}' ({Year}) as {Match} ({Id})", info.Name, title, year, match.Name, kinopoiskId);
                return kinopoiskId;
            }
        }

        Logger.LogInformation("Kinopoisk has no match for {Name}", info.Name);
        return 0;
    }
}
