using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Api;
using Jellyfin.Plugin.Kinopoisk.Configuration;
using Jellyfin.Plugin.Kinopoisk.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.Providers;

/// <summary>
/// Person metadata from Kinopoisk.
/// </summary>
public sealed class PersonMetadataProvider : KinopoiskMetadataProvider<Person, PersonLookupInfo>
{
    private readonly IKinopoiskOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonMetadataProvider"/> class.
    /// </summary>
    /// <param name="client">The Kinopoisk client.</param>
    /// <param name="imageDownloader">The image downloader.</param>
    /// <param name="options">The plugin options.</param>
    /// <param name="logger">The logger.</param>
    public PersonMetadataProvider(
        IKinopoiskClient client, ImageDownloader imageDownloader, IKinopoiskOptions options, ILogger<PersonMetadataProvider> logger)
        : base(client, imageDownloader, logger)
    {
        _options = options;
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<RemoteSearchResult>> SearchAsync(string title, int? year, CancellationToken cancellationToken)
    {
        var persons = await Client.SearchPersonsAsync(title, cancellationToken).ConfigureAwait(false);
        return [.. persons.Select(p => p.ToSearchResult())];
    }

    /// <inheritdoc />
    protected override async Task<RemoteSearchResult> GetSearchResultByIdAsync(int kinopoiskId, CancellationToken cancellationToken)
        => (await Client.GetPersonAsync(kinopoiskId, cancellationToken).ConfigureAwait(false)).ToSearchResult();

    /// <inheritdoc />
    protected override async Task FillAsync(MetadataResult<Person> result, int kinopoiskId, CancellationToken cancellationToken)
        => result.FillFrom(await Client.GetPersonAsync(kinopoiskId, cancellationToken).ConfigureAwait(false), _options);

    /// <inheritdoc />
    protected override IEnumerable<(string Title, int? Year)> GetSearchTerms(PersonLookupInfo info)
    {
        if (!string.IsNullOrWhiteSpace(info.Name))
        {
            yield return (info.Name, null);
        }
    }
}
