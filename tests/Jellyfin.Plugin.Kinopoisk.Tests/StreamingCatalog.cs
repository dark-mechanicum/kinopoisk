using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Api;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests;

/// <summary>
/// The Kinopoisk streaming catalog (season lists, season episodes) is region-restricted: from some networks,
/// e.g. GitHub's Azure runners, every title is <c>UNWATCHABLE</c> and season data comes back empty.
/// Tests that need it carry <c>[Trait("Category", StreamingCatalog.Category)]</c> and call <see cref="RequireAvailable"/>
/// or <see cref="SkipUnlessAvailableAsync"/>; the queries still run first, so a schema change fails the test
/// instead of skipping it. With <c>KINOPOISK_REQUIRE_STREAMING_CATALOG=1</c> (the CI job behind a Russian proxy)
/// an unavailable catalog fails instead of skipping.
/// </summary>
public static class StreamingCatalog
{
    public const string Category = "StreamingCatalog";

    public const string UnavailableReason =
        "The Kinopoisk streaming catalog is not available from this network (season data is empty); the plugin falls back to the latest episodes.";

    private const string RequireVariable = "KINOPOISK_REQUIRE_STREAMING_CATALOG";

    /// <summary>
    /// Skips the test when the streaming catalog is not visible from the current network.
    /// </summary>
    /// <param name="client">The live client.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public static async Task SkipUnlessAvailableAsync(IKinopoiskClient client, CancellationToken cancellationToken)
    {
        var page = await client.GetFilmPageAsync(KnownTitles.MetodContentId, cancellationToken);
        RequireAvailable(page.Seasons?.Items is { Count: > 0 });
    }

    /// <summary>
    /// Skips (or, when required, fails) the test when the catalog data is missing.
    /// </summary>
    /// <param name="available">Whether the catalog answered with data.</param>
    public static void RequireAvailable(bool available)
    {
        if (Environment.GetEnvironmentVariable(RequireVariable) == "1")
        {
            Assert.True(available, UnavailableReason);
            return;
        }

        Assert.SkipUnless(available, UnavailableReason);
    }
}
