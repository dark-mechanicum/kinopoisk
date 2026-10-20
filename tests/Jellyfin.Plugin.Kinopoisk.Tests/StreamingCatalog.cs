using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Api;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests;

/// <summary>
/// The Kinopoisk streaming catalog (season lists, season episodes) is region-restricted: from some networks,
/// e.g. GitHub's Azure runners, every title is <c>UNWATCHABLE</c> and season data comes back empty.
/// Tests that need it call <see cref="SkipUnlessAvailableAsync"/>; the queries still run first, so a schema change
/// fails the test instead of skipping it.
/// </summary>
public static class StreamingCatalog
{
    public const string UnavailableReason =
        "The Kinopoisk streaming catalog is not available from this network (season data is empty); the plugin falls back to the latest episodes.";

    /// <summary>
    /// Skips the test when the streaming catalog is not visible from the current network.
    /// </summary>
    /// <param name="client">The live client.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public static async Task SkipUnlessAvailableAsync(IKinopoiskClient client, CancellationToken cancellationToken)
    {
        var page = await client.GetFilmPageAsync(KnownTitles.MetodContentId, cancellationToken);
        Assert.SkipUnless(page.Seasons?.Items is { Count: > 0 }, UnavailableReason);
    }
}
