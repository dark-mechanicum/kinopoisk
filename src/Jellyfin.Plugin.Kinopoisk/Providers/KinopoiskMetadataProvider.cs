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
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.Providers;

/// <summary>
/// Shared metadata flow: resolve the Kinopoisk id (stored, or by search), fetch, map.
/// </summary>
/// <typeparam name="TItem">The Jellyfin item type.</typeparam>
/// <typeparam name="TLookup">The lookup info type.</typeparam>
public abstract class KinopoiskMetadataProvider<TItem, TLookup> : IRemoteMetadataProvider<TItem, TLookup>
    where TItem : BaseItem, IHasLookupInfo<TLookup>, new()
    where TLookup : ItemLookupInfo, new()
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="KinopoiskMetadataProvider{TItem, TLookup}"/> class.
    /// </summary>
    /// <param name="client">The Kinopoisk client.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    protected KinopoiskMetadataProvider(IKinopoiskClient client, IHttpClientFactory httpClientFactory, ILogger logger)
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
    public async Task<MetadataResult<TItem>> GetMetadata(TLookup info, CancellationToken cancellationToken)
    {
        var result = MetadataResultExtensions.CreateResult<TItem>();
        try
        {
            result.QueriedById = info.TryGetKinopoiskId(out var kinopoiskId);
            if (!result.QueriedById)
            {
                kinopoiskId = await FindIdAsync(info, cancellationToken).ConfigureAwait(false);
            }

            if (kinopoiskId > 0)
            {
                await FillAsync(result, kinopoiskId, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (KinopoiskApiException ex)
        {
            Logger.LogError(ex, "Kinopoisk metadata lookup failed for {Name}", info.Name);
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

        var keyword = searchInfo.Year is null ? searchInfo.Name : $"{searchInfo.Name} {searchInfo.Year}";
        return await SearchAsync(keyword, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        => _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);

    /// <summary>
    /// Searches by keyword.
    /// </summary>
    /// <param name="keyword">The keyword.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The results, best first.</returns>
    protected abstract Task<IReadOnlyList<RemoteSearchResult>> SearchAsync(string keyword, CancellationToken cancellationToken);

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
    /// Keywords to try when the item has no Kinopoisk id yet, best first.
    /// </summary>
    /// <param name="info">The lookup info.</param>
    /// <returns>The keywords.</returns>
    protected virtual IEnumerable<string> GetKeywords(TLookup info)
    {
        if (!string.IsNullOrWhiteSpace(info.Name))
        {
            yield return info.Year is null ? info.Name : $"{info.Name} {info.Year}";
        }
    }

    private async Task<int> FindIdAsync(TLookup info, CancellationToken cancellationToken)
    {
        foreach (var keyword in GetKeywords(info).Distinct())
        {
            var matches = await SearchAsync(keyword, cancellationToken).ConfigureAwait(false);
            if (matches is [var match, ..] && match.TryGetKinopoiskId(out var kinopoiskId))
            {
                Logger.LogInformation("Kinopoisk matched {Name} by '{Keyword}' as {Match} ({Id})", info.Name, keyword, match.Name, kinopoiskId);
                return kinopoiskId;
            }
        }

        Logger.LogInformation("Kinopoisk has no match for {Name}", info.Name);
        return 0;
    }
}
